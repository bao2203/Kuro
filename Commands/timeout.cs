using Kurohana.Helpers;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Humanizer;

namespace Kurohana.Commands
{
    public class Timeout : ApplicationCommandModule<ApplicationCommandContext>
    {
        [SlashCommand("timeout", "select a user to timeout")]
        public async Task timeout(
            [SlashCommandParameter(Name = "user", Description = "select the user to timeout")] GuildUser TargetUser,
            [SlashCommandParameter(Name = "duration", Description = "select the duration of timeout")] string DurationInput,
            string? reason = "No reason provided."
            )
        {
            // we need these variables for our timeout command: min and max duration, interaction & guild shorthand, perms and bot's id to get its user
            double TimeoutDurationMaxLimit = TimeSpan.FromDays(28).TotalMilliseconds;
            double TimeoutDurationMinLimit = TimeSpan.FromMinutes(1).TotalMilliseconds;
            var interaction = Context.Interaction;
            ulong BotId = Context.Client.Id;
            RestGuild guild = Context.Guild;
            var RequiredPermission = Permissions.ModerateUsers;

            if (guild is null) // check if command is used in a servwe
            {
                await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ This command can only be used in a server!"));
                return;
            }
            if (TargetUser.IsBot) // timeout is allergic to bots so we check if the user is bot
            {
                await interaction.SendResponseAsync(InteractionCallback.Message("> ⚠ You cannot timeout a bot!"));
                return;
            }

            try
            {
                await interaction.SendResponseAsync(InteractionCallback.DeferredMessage()); // defer the response to avoid timeout

                // getting the user and bot user, check for perms and role position
                var user = Context.User as GuildUser;
                var BotUser = await guild.GetUserAsync(BotId);

                bool BotHaspermission = BotUser.GetPermissions(guild).HasFlag(RequiredPermission);
                bool UserHasPermission = user.GetPermissions(guild).HasFlag(RequiredPermission);

                int CurrentUserPosition = user.GetRoles(guild).Any() ? user.GetRoles(guild).Max(r => r.Position) : 0;
                int BotUserPosition = BotUser.GetRoles(guild).Any() ? BotUser.GetRoles(guild).Max(r => r.Position) : 0;
                int TargetUserPosition = TargetUser.GetRoles(guild).Any() ? TargetUser.GetRoles(guild).Max(r => r.Position) : 0;

                if (user.Id == guild.OwnerId && user != TargetUser) // checking whether the invoker is owner
                {
                    CurrentUserPosition = TargetUserPosition + 1;
                }

                if (!TimeParserHelper.TryParseTime(DurationInput, out var duration)) // checking whether the input is valid and return a TimeSpan variable
                {
                    await LogHelper.LogWarningAsync($"Invalid duration format: {DurationInput}");
                    await interaction.ModifyResponseAsync(opts => opts.Content = "> ⚠ Invalid duration input!");
                    return;
                }

                var totalMilliseconds = duration.TotalMilliseconds;
                DateTimeOffset TimeoutUntil = DateTimeOffset.UtcNow.Add(duration); // time until the timeout ends

                if (totalMilliseconds > TimeoutDurationMaxLimit) // checking for min & max duration (timeout supports maximum 28 days and minimum 1 minute)
                {
                    await LogHelper.LogWarningAsync($"Exceeded timeout duration limit: {totalMilliseconds} is more than 28 days");
                    await interaction.ModifyResponseAsync(opts => opts.Content = "> ⚠ Timeout duration exceeds the limit of 28 days!");
                    return;
                } else if (totalMilliseconds < TimeoutDurationMinLimit)
                {
                    await LogHelper.LogWarningAsync($"Timeout duration is less than 1 minute: {totalMilliseconds}");
                    await interaction.ModifyResponseAsync(opts => opts.Content = "> ⚠ Timeout duration must be at least 1 minute!");
                    return;
                }

                if (!BotHaspermission) // checking for permissions
                {
                    await interaction.ModifyResponseAsync(opts => opts.Content = "> ⚠ I do not have enough permission to timeout users!");
                    return;
                }
                else if (!UserHasPermission)
                {
                    await interaction.ModifyResponseAsync(opts => opts.Content = "> ⚠ You don't have the required permission to timeout user!");
                    return;
                }

                if (CurrentUserPosition <= TargetUserPosition) // checking for role position
                {
                    await interaction.ModifyResponseAsync(opts => opts.Content = "> ⚠ You cannot timeout this user!");
                    return;
                }
                else if (BotUserPosition <= TargetUserPosition)
                {
                    await interaction.ModifyResponseAsync(opts => opts.Content = "> ⚠ I cannot timeout this user!");
                    return;
                }

                if (BotHaspermission && CurrentUserPosition > TargetUserPosition && BotUserPosition > TargetUserPosition) // execute if conditions are met
                {
                    await TargetUser.TimeOutAsync(TimeoutUntil, new RestRequestProperties { AuditLogReason = reason });
                    await interaction.ModifyResponseAsync(opts => opts.Content = $"> ⏳ User {TargetUser.Username} has been timed out for {duration.Humanize(precision: 3, collectionSeparator: " and ")}.\n> -# Reason: {reason}");
                }
                return;
            }
            catch (Exception ex) // handling error gracefully
            {
                await LogHelper.LogErrorAsync($"Failed to timeout user: {ex.Message}", ex);
                await interaction.ModifyResponseAsync(opts => opts.Content = "> ⚠ Failed to timeout user!");
                return;
            }
        }    
    }
}