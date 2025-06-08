using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services;
using NetCord.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Kurohana.Helpers;
using Kurohana.Services;

namespace Kurohana
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder() // accessing appsettings.json as config
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var builder = Host.CreateApplicationBuilder(args); // creating a minimal apis app builder
            builder.Services
                .AddDiscordShardedGateway(options =>
                {
                    options.Intents = GatewayIntents.Guilds | GatewayIntents.MessageContent | GatewayIntents.AllNonPrivileged;
                    options.Token = config["Discord:Token"]; // insert bot token here

                }) // using Discord's Gateway (sharded)
                .AddShardedGatewayEventHandlers(typeof(Program).Assembly) // add the event handler for the gateway
                .AddApplicationCommands(); // add Application Commands
            builder.Services.AddSingleton<BotServices>(); // add the bot services

            var host = builder.Build();

            var AppLifeTime = host.Services.GetRequiredService<IHostApplicationLifetime>(); // get the lifetime service
            var cancellationToken = AppLifeTime.ApplicationStopping; // get the cancellation token

            var botServices = host.Services.GetRequiredService<BotServices>(); // get the bot services

            _ = Task.Run(async () =>
            {
                try { await LogHelper.StartBufferFlusherAsync(cancellationToken); }
                catch (Exception ex)
                {
                    await LogHelper.LogErrorAsync("Failed to start buffer flusher: {Message}", ex);
                }
            }); // since our method is async, we gotta find a way to do fire-and-forget safely

            await botServices.StartAsync(cancellationToken); // start bot services

            host.AddModules(typeof(Program).Assembly);
            host.UseShardedGatewayEventHandlers(); // now we are using the discord's gateway

            
            await host.RunAsync(); // run the host -> start the bot
        }
    }
}