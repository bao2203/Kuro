using System;
using Kurohana.Helpers;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;


namespace Kurohana.Commands.Misc
{
    public class Say : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("say", "let the bot say it for you", Contexts = [InteractionContextType.DMChannel, InteractionContextType.Guild])]
        public async Task say(
            [SlashCommandParameter(Name = "content", Description = "tell what the bot has to say")] string msg
            )
        {
            InteractionCallback callback = InteractionCallback.DeferredMessage(MessageFlags.Ephemeral);
            ApplicationCommandInteraction interaction = Context.Interaction;
            await LogHelper.LogInfoAsync(msg, false);
            await interaction.SendResponseAsync(callback); // defer the response to avoid timeout

            await LogHelper.LogInfoAsync($"{Context.User.Discriminator} has used /say command with the following content: {msg}");
            await interaction.ModifyResponseAsync(m => m.Content = "sent.");
            await interaction.SendFollowupMessageAsync(msg); // send the message to the channel where the command was invoked
        }
    }
}
