using Kurohana.Helpers;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Kurohana.Commands.Moderation.UserSlashCommands
{
    public class Kick : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("kick", "Kick a user from the server.", DefaultGuildUserPermissions = Permissions.KickUsers)]

        public async Task KickUser(
            [SlashCommandParameter(Name = "user", Description = "select the user to kick")] GuildUser TargetUser,
            string? reason = "No reason provided."
            )
        {
            // retrieve the guild and interaction data, and bot's user id
            var interaction = Context.Interaction;
            ulong BotId = Context.Client.Id;
            RestGuild guild = Context.Guild;

            if (Context.Guild is null)
            {
                await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ This command can only be used in a server!"));
                return;
            }

            // get the user from the command invoker and bot
            var user = Context.User as GuildUser;
            var BotUser = await Context.Interaction.Guild.GetUserAsync(BotId);

            // check whether both bot and user has required permission
            var BotPermission = BotUser.GetPermissions(guild).HasFlag(Permissions.KickUsers);
            var CurrentUserPermission = user.GetPermissions(guild).HasFlag(Permissions.KickUsers);

            if (TargetUser.GuildId != Context.Guild.Id) // check if the user is in the same guild as the bot
            {
                await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ This user is not in the same server!"));
                return;
            }

            // get the role hiearchy of each users
            int CurrentUserPosition = user.GetRoles(guild).Any() ? user.GetRoles(guild).Max(r => r.Position) : 0;
            int TargetUserPosition = TargetUser.GetRoles(guild).Any() ? TargetUser.GetRoles(guild).Max(r => r.Position) : 0;
            int BotUserPosition = BotUser.GetRoles(guild).Any() ? BotUser.GetRoles(guild).Max(r => r.Position) : 0;

            // check if the user is owner, owner always has highest priority
            if (user.Id == Context.Guild.OwnerId && user != TargetUser)
            {
                CurrentUserPosition = TargetUserPosition + 1;
            }

            try
            {
                if (!BotPermission) // check if bot or invoker has required permission
                {
                    await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ I do not have permission to kick users!"));
                    return;
                }
                else if (!CurrentUserPermission)
                {
                    await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ You don't have the required permission to kick user!"));
                    return;
                }

                if (CurrentUserPosition <= TargetUserPosition) // comparing role hiearchy of bot and invoker to target user
                {
                    await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ You cannot kick this user!"));
                    return;
                }
                else if (BotUserPosition <= TargetUserPosition)
                {
                    await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ I cannot kick this user!"));
                    return;
                }

                if (BotPermission && CurrentUserPosition > TargetUserPosition && BotUserPosition > TargetUserPosition) // initiate command if met conditions
                {
                    await TargetUser.KickAsync(new RestRequestProperties { AuditLogReason = reason });
                    await interaction.SendResponseAsync(InteractionCallback.Message($">  👟💥 <@{TargetUser.Id}> has been kicked for the following reason:\n> {reason}"));
                    await LogHelper.LogInfoAsync($"{TargetUser.Username} (ID: {TargetUser.Id}) has been kicked from the server '{Context.Guild.Name}'", true);
                }
                return;
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync($"> ❌ Error: {ex.Message}, please contact @donoteatmydimsum if this error persist.");
                await LogHelper.LogErrorAsync("Encountered a fatal error in /kick command!", ex);
                return;
            }
        }
    }
}
