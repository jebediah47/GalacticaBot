using GalacticaBot.Api.Endpoints;
using GalacticaBot.Api.Hubs;
using GalacticaBot.Api.Services;
using GalacticaBot.Data;
using GalacticaBot.EnvManager;
using GalacticaBot.Utils;
using Microsoft.EntityFrameworkCore;
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
    await db.Database.EnsureCreatedAsync();
}

app.Run();
