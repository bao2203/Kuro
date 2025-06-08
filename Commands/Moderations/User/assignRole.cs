using Kurohana.Helpers;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Kurohana.Commands.Moderations.User
{
    public class AssignRole : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("assign-role", "assign a role to a member")]
        public async Task assignRole(
            [SlashCommandParameter(Name = "user", Description = "user to assign")] GuildUser TargetUser,
            [SlashCommandParameter(Name = "role", Description = "role to assign")] Role role
            )
        {
            var interaction = Context.Interaction;
            RestGuild guild = Context.Guild;
            ulong BotId = Context.Client.Id;
            var RequiredPermission = Permissions.ManageRoles;

            if (guild is null)
            {
                await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ This command can only be run in a server"));
                return;
            }
            if (TargetUser.GuildId != guild.Id)
            {
                await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ User is not in the same server!"));
                return;
            }

            await interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

            try
            {
                var user = Context.User as GuildUser;
                var BotUser = await guild.GetUserAsync(BotId);

                bool UserHasPermission = user.GetPermissions(guild).HasFlag(RequiredPermission);
                bool BotHasPermission = BotUser.GetPermissions(guild).HasFlag(RequiredPermission);

                int RequestedRolePosition = role.Position;
                int CurrentUserPosition = user.GetRoles(guild).Any() ? user.GetRoles(guild).Max(r => r.Position) : 0;
                int BotUserPosition = BotUser.GetRoles(guild).Any() ? BotUser.GetRoles(guild).Max(r => r.Position) : 0;
                int TargetUserPosition = TargetUser.GetRoles(guild).Any() ? TargetUser.GetRoles(guild).Max(r => r.Position) : 0;

                if (user.Id == guild.OwnerId)
                {
                    CurrentUserPosition = TargetUserPosition > RequestedRolePosition ? TargetUserPosition + 1 : RequestedRolePosition + 1;
                }

                if (!BotHasPermission) // check if bot or invoker has required permission
                {
                    await interaction.ModifyResponseAsync(m => m.Content = "> ⚠ I do not have permission to assign roles!");
                    return;
                }
                else if (!UserHasPermission)
                {
                    await interaction.ModifyResponseAsync(m => m.Content = "> ⚠ You don't have the required permission to assign roles!");
                    return;
                }

                if (BotUserPosition <= RequestedRolePosition)
                {
                    await interaction.ModifyResponseAsync(m => m.Content = "> ⚠ I can't assign user this role! (this role has higher hierarchy than mine)");
                    return;
                } else if (CurrentUserPosition < RequestedRolePosition)
                {
                    await interaction.ModifyResponseAsync(m => m.Content = "> ⚠ You can't assign user this role! (this role has higher hierarchy than you)");
                    return;
                }

                if (CurrentUserPosition <= TargetUserPosition) // comparing role hiearchy of bot and invoker to target user
                {
                    await interaction.ModifyResponseAsync(m => m.Content = $"> ⚠ You cannot assign role to this user! {CurrentUserPosition}, {TargetUserPosition}");
                    return;
                }
                else if (BotUserPosition <= TargetUserPosition)
                {
                    await interaction.ModifyResponseAsync(m => m.Content = "> ⚠ I cannot assign role to this user!");
                    return;
                }

                await TargetUser.AddRoleAsync(role.Id);
                await interaction.ModifyResponseAsync(m => m.Content = $"> ✅ <@&{role.Id}> has been assigned to <@{TargetUser.Id}> successfully!");
                return;
            }
            catch (Exception ex)
            {
                await LogHelper.LogErrorAsync("/assign-role encountered a fatal error!", ex);
                await interaction.ModifyResponseAsync(m => m.Content = $"> ❌ Failed to assign role to user!");
                return;
            }
        }
    }
}