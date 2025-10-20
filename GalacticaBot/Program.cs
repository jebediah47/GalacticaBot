using GalacticaBot.Data;
using GalacticaBot.EnvManager;
using GalacticaBot.Services;
using GalacticaBot.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Services.ApplicationCommands;

var builder = Host.CreateApplicationBuilder(args);

EnvManager.EnsureEnvironment(builder.Environment);

builder.Services.AddSingleton<PresenceManager>();
builder.Services.AddHttpClient();

// Database
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(databaseUrl))
{
    throw new InvalidOperationException(
        "DATABASE_URL environment variable is not set. Configure your Postgres connection string."
    );
}

var normalizedConnectionString = ToKvIfUri.Convert(databaseUrl);

// Add pooled factory for high-throughput, short-lived contexts used by background/services
builder.Services.AddPooledDbContextFactory<GalacticaDbContext>(options =>
    options.UseNpgsql(normalizedConnectionString).UseSnakeCaseNamingConvention()
);

// Services (must be after DbContextFactory registration)
builder.Services.AddSingleton<BotConfigService>();
builder.Services.AddSingleton<LevelingService>();
builder.Services.AddHostedService<BotConfigSyncService>();

builder
    .Services.AddDiscordGateway(options =>
    {
        options.Token = Environment.GetEnvironmentVariable("GALACTICA_TOKEN");
        options.Intents = GatewayIntents.AllNonPrivileged | GatewayIntents.MessageContent;
    })
    .AddGatewayHandlers(typeof(Program).Assembly)
    .AddApplicationCommands<ApplicationCommandInteraction, ApplicationCommandContext>();

var host = builder.Build();

// Ensure database is created (no migrations yet)
var dbFactory = host.Services.GetRequiredService<IDbContextFactory<GalacticaDbContext>>();
await using (var db = await dbFactory.CreateDbContextAsync())
{
    await db.Database.EnsureCreatedAsync();
}

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();
