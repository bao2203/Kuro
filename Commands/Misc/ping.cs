using Kurohana.Helpers;
using NetCord;
using NetCord.Gateway.LatencyTimers;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Kurohana.Commands.Misc
{
    public class Ping : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("ping", "Check the bot's latency", Contexts = [InteractionContextType.Guild, InteractionContextType.DMChannel, InteractionContextType.BotDMChannel])]
        public async Task PingAsync()
        {
            var latencyTimer = new LatencyTimer();
            latencyTimer.Start();
            double WS_latency = Context.Client.Latency.TotalMilliseconds;
            await LogHelper.LogInfoAsync($"/ping command executed by {Context.User.Username} ({Context.User.Discriminator}, ID: {Context.User.Id})", true);
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message($"Pong!\n> -# WebSocket Latency: {WS_latency}ms\n> -# API latency: {latencyTimer.Elapsed.TotalMilliseconds}ms"));
        }
    }
}
