using Config.Net;
using DotNetEnv;
using GalacticaBot.Configuration;
using GalacticaBot.Utils;
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

Console.WriteLine(botConfig.BotActivity);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(botConfig);

builder
    .Services.AddDiscordGateway(options =>
    {
        options.Token = Environment.GetEnvironmentVariable("GALACTICA_TOKEN");
        options.Intents = GatewayIntents.AllNonPrivileged;
    })
    .AddGatewayHandlers(typeof(Program).Assembly)
    .AddApplicationCommands<ApplicationCommandInteraction, ApplicationCommandContext>();

builder.Services.AddSingleton<IRandomColor, RandomColor>();

var host = builder.Build();

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();
