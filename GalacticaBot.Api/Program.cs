using GalacticaBot.Api.Endpoints;
using GalacticaBot.Api.Hubs;
using GalacticaBot.Api.Services;
using GalacticaBot.Data;
using GalacticaBot.EnvManager;
using GalacticaBot.Utils;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

EnvManager.EnsureEnvironment(builder.Environment);

// Configure Kestrel for mTLS if enabled
var mtlsEnabled = Environment.GetEnvironmentVariable("MTLS_ENABLED")?.ToLower() == "true";
if (mtlsEnabled)
{
    var certPath = Environment.GetEnvironmentVariable("CERT_PATH") ?? "/app/certs";
    var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "galactica-bot.api";
    var serverCertPath = Path.Combine(certPath, $"{serviceName}.crt");
    var serverKeyPath = Path.Combine(certPath, $"{serviceName}.key");
    var rootCaPath = Path.Combine(certPath, "root_ca.crt");

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureHttpsDefaults(httpsOptions =>
        {
            // Load server certificate
            if (File.Exists(serverCertPath) && File.Exists(serverKeyPath))
            {
                httpsOptions.ServerCertificate = X509Certificate2.CreateFromPemFile(serverCertPath, serverKeyPath);
            }

            // Allow client certificates (optional, not required at Kestrel level)
            // Authentication will be enforced at the hub level
            httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            httpsOptions.CheckCertificateRevocation = false;

            // Configure client certificate validation
            httpsOptions.ClientCertificateValidation = (certificate, chain, errors) =>
            {
                if (chain == null)
                {
                    return false;
                }

                // Load the root CA certificate
                if (File.Exists(rootCaPath))
                {
                    using var rootCert = X509Certificate2.CreateFromPemFile(rootCaPath);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                    return chain.Build(certificate);
                }

                return false;
            };
        });
    });

    // Add certificate authentication
    builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
        .AddCertificate(options =>
        {
            options.AllowedCertificateTypes = CertificateTypes.All;
            options.RevocationMode = X509RevocationMode.NoCheck;

            options.Events = new CertificateAuthenticationEvents
            {
                OnCertificateValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogInformation(
                        "Client certificate validated: Subject={Subject}, Issuer={Issuer}",
                        context.ClientCertificate.Subject,
                        context.ClientCertificate.Issuer);

                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogError(
                        context.Exception,
                        "Client certificate authentication failed");

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // Policy that requires certificate authentication
        options.AddPolicy("RequireCertificate", policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AuthenticationSchemes.Add(CertificateAuthenticationDefaults.AuthenticationScheme);
        });
    });
}

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Database
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(databaseUrl))
{
    throw new InvalidOperationException(
        "DATABASE_URL environment variable is not set. Configure your Postgres connection string."
    );
}
var normalizedConnectionString = ToKvIfUri.Convert(databaseUrl);

builder.Services.AddPooledDbContextFactory<GalacticaDbContext>(options =>
    options.UseNpgsql(normalizedConnectionString).UseSnakeCaseNamingConvention()
);

// SignalR
builder.Services.AddSignalR();

// Add services
builder.Services.AddScoped<GuildConfigService>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Use authentication and authorization if mTLS is enabled
if (mtlsEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseHttpsRedirection();

// Map SignalR hubs with mTLS requirement if enabled
if (mtlsEnabled)
{
    app.MapHub<BotConfigHub>("/hubs/botconfig").RequireAuthorization("RequireCertificate");
    app.MapHub<GuildConfigHub>("/hubs/guildconfig").RequireAuthorization("RequireCertificate");
}
else
{
    app.MapHub<BotConfigHub>("/hubs/botconfig");
    app.MapHub<GuildConfigHub>("/hubs/guildconfig");
}

// Map health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

// Map API endpoints
app.MapBotConfigEndpoints();
app.MapGuildConfigsEndpoints();

// Ensure database exists
var dbFactory = app.Services.GetRequiredService<IDbContextFactory<GalacticaDbContext>>();
await using (var db = await dbFactory.CreateDbContextAsync())
{
    await db.Database.MigrateAsync();
}

app.Run();
