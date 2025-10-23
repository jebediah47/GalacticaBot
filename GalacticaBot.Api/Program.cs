using System.Text;
using GalacticaBot.Api.Endpoints;
using GalacticaBot.Api.Hubs;
using GalacticaBot.Api.Services;
using GalacticaBot.Data;
using GalacticaBot.EnvManager;
using GalacticaBot.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

EnvManager.EnsureEnvironment(builder.Environment);

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

// JWT Authentication
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException(
        "JWT_SECRET environment variable is not set or is less than 32 characters. "
            + "Provide a secure symmetric key for JWT signing."
    );
}

var key = Encoding.ASCII.GetBytes(jwtSecret);
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        // Extract token from query string for SignalR WebSocket compatibility
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SignalRHub", policy => policy.RequireAuthenticatedUser());
});

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

app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hub
app.MapHub<BotConfigHub>("/hubs/botconfig");
app.MapHub<GuildConfigHub>("/hubs/guildconfig");

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
