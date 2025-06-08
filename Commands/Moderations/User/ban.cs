using NetCord.Services.ApplicationCommands;
using NetCord;
using NetCord.Rest;
using Kurohana.Helpers;

namespace Kurohana.Commands.Moderations.User
{
    public class Ban : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("ban", "Ban a user from the server.")]
        public async Task ban(
            [SlashCommandParameter(Name = "user", Description = "select the user to ban")] GuildUser TargetUser,
            string? reason = "No reason provided."
            )
        {
            var interaction = Context.Interaction;
            RestGuild guild = Context.Guild;
            var RequiredPermission = Permissions.BanUsers;
            ulong BotId = Context.Client.Id;

            if (Context.Guild is null)
            {
                await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ This command can only be used in a server!"));
                return;
            }

            if (TargetUser.GuildId != Context.Guild.Id)
            {
                await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ This user is not in the same server!"));
                return;
            }

            GuildUser user = Context.User as GuildUser;
            GuildUser BotUser = await Context.Guild.GetUserAsync(BotId);

            bool BotPermission = BotUser.GetPermissions(guild).HasFlag(RequiredPermission);
            bool CurrentUserPermission = user.GetPermissions(guild).HasFlag(RequiredPermission);

            int CurrentUserPosition = user.GetRoles(guild).Any() ? user.GetRoles(guild).Max(r => r.Position) : 0;
            int BotUserPosition = BotUser.GetRoles(guild).Any() ? BotUser.GetRoles(guild).Max(r => r.Position) : 0;
            int TargetUserPosition = TargetUser.GetRoles(guild).Any() ? TargetUser.GetRoles(guild).Max(r => r.Position) : 0;

            if (user.Id == Context.Guild.OwnerId && user != TargetUser)
            {
                CurrentUserPosition = TargetUserPosition + 1;
            }

            try
            {
                if (!BotPermission) // check if bot or invoker has required permission
                {
                    await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ I do not have permission to ban users!"));
                    return;
                }
                else if (!CurrentUserPermission)
                {
                    await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ You don't have the required permission to ban user!"));
                    return;
                }

                if (CurrentUserPosition <= TargetUserPosition) // comparing role hiearchy of bot and invoker to target user
                {
                    await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ You cannot ban this user!"));
                    return;
                }
                else if (BotUserPosition <= TargetUserPosition)
                {
                    await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ I cannot ban this user!"));
                    return;
                }

                if (BotPermission && CurrentUserPosition > TargetUserPosition && BotUserPosition > TargetUserPosition) // initiate command if met conditions
                {
                    await TargetUser.BanAsync(5, new RestRequestProperties { AuditLogReason = reason });
                    await interaction.SendResponseAsync(InteractionCallback.Message($"> ✈️ <@{TargetUser.Id}> has been banned for the following reason:\n> {reason}"));
                    await LogHelper.LogInfoAsync($"{TargetUser.Username} (ID: {TargetUser.Id}) has been banned from the server '{Context.Guild.Name}'", true);
                }
                return;
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync($"> ❌ Error: {ex.Message}, please contact @donoteatmydimsum if this error persist.");
                await LogHelper.LogErrorAsync("Encountered a fatal error in /ban command!", ex);
                return;
            }
        }
    }
}