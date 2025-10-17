using Config.Net;
using DotNetEnv;
using GalacticaBot.Configuration;
using GalacticaBot.Data;
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

Env.TraversePath().Load();

var botConfig = new ConfigurationBuilder<IBotConfig>().UseJsonFile("config.json").Build();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(botConfig);
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

builder.Services.AddDbContext<GalacticaDbContext>(options =>
    options.UseNpgsql(normalizedConnectionString).UseSnakeCaseNamingConvention()
);

// Add pooled factory for high-throughput, short-lived contexts used by background/services
builder.Services.AddPooledDbContextFactory<GalacticaDbContext>(options =>
    options.UseNpgsql(normalizedConnectionString).UseSnakeCaseNamingConvention()
);

// Services (must be after DbContextFactory registration)
builder.Services.AddSingleton<LevelingService>();

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
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GalacticaDbContext>();
    db.Database.EnsureCreated();
}

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();
