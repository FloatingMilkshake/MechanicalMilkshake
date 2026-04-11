using static MechanicalMilkshake.Helpers.ReminderHelpers;

namespace MechanicalMilkshake.Commands;

[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
internal class ReminderCommands
{
    [Command("Remind Me About This")]
    [AllowedProcessors(typeof(MessageCommandProcessor))]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    public static async Task RemindMeAboutThisCommandAsync(MessageCommandContext ctx, DiscordMessage targetMessage)
    {
        await ctx.RespondWithModalAsync(new DiscordModalBuilder()
            .WithTitle("Remind Me About This")
            .WithCustomId("modal-callback-remind-me-about-this")
            .AddTextInput(new DiscordTextInputComponent("text-input-callback-remind-me-about-this-time"), "When do you want to be reminded?")
        );

        Setup.State.Caches.ReminderInteractionCache[ctx.User.Id] = targetMessage;
    }

    [Command("reminder")]
    [Description("Set, modify and delete reminders.")]
    public class ReminderSlashCommands
    {
        [Command("set")]
        [Description("Set a reminder.")]
        public static async Task ReminderSetCommandAsync(SlashCommandContext ctx,
            [Parameter("time"), Description("When do you want to be reminded?")]
            string time,
            [Parameter("text"), Description("What should the reminder say?")] [MinMaxLength(maxLength: 1000)]
            string text = "")
        {
            await ctx.DeferResponseAsync();

            var (parsedTime, error) = ValidateReminderTriggerTime(time);
            if (parsedTime is null)
            {
                await ctx.RespondAsync(error, ephemeral: true);
                return;
            }

            Setup.Types.Reminder reminder = new()
            {
                UserId = ctx.User.Id,
                ChannelId = ctx.Channel.Id,
                GuildId = ctx.Guild is null ? "@me" : ctx.Guild.Id.ToString(),
                ReminderId = await GenerateUniqueReminderIdAsync(),
                ReminderText = text,
                TriggerTime = parsedTime.Value,
                SetTime = DateTime.Now
            };

            var reminderTriggerTimeTimestamp = reminder.GetTriggerTimeTimestamp();

            var message = await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Reminder set for <t:{reminderTriggerTimeTimestamp}:F> (<t:{reminderTriggerTimeTimestamp}:R>)!" +
                                    $"\nReminder ID: `{reminder.ReminderId}`"));
            reminder.MessageId = message.Id;

            await Setup.Storage.Redis.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));
        }

        [Command("list")]
        [Description("List your reminders.")]
        public static async Task ReminderListCommandAsync(SlashCommandContext ctx)
        {
            await ctx.DeferResponseAsync(true);

            var userReminders = await GetUserRemindersAsync(ctx.User.Id);

            if (userReminders.Count == 0)
            {

                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"You don't have any reminders! Set one with {CommandHelpers.GetSlashCmdMention("reminder set")}.")
                    .AsEphemeral(true));
                return;
            }

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .AddEmbed(await CreateReminderListEmbedAsync(userReminders))
                .AsEphemeral(true));
        }

        [Command("delete")]
        [Description("Delete a reminder.")]
        public static async Task ReminderDeleteCommandAsync(SlashCommandContext ctx)
        {
            // We can't defer this!! Want to respond with a modal if the user has >25 reminders.

            var userReminders = await GetUserRemindersAsync(ctx.User.Id);
            if (userReminders.Count == 0)
            {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent($"You don't have any reminders! Set one with {CommandHelpers.GetSlashCmdMention("reminder set")}.")
                    .AsEphemeral(true));
                return;
            }
            else if (userReminders.Count <= 25)
            {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Please choose a reminder to delete.")
                    .AddActionRowComponent(CreateSelectComponentFromReminders(userReminders, "selectmenu-callback-reminder-delete"))
                    .AsEphemeral(true));
            }
            else
            {
                // User has more than 25 reminders. Show a modal where they are prompted to enter the ID for the reminder they want to delete.
                // I wanted to paginate a select menu instead, but Discord and D#+ limitations make that really difficult for now. (cba writing my own pagination)

                var modalText = "You have a lot of reminders! Please enter the ID of the reminder you wish to delete." +
                    $" You can see reminder IDs with {CommandHelpers.GetSlashCmdMention("reminder list")}.";

                await ctx.RespondWithModalAsync(new DiscordModalBuilder().WithCustomId("modal-callback-reminder-delete").WithTitle("Delete a Reminder")
                    .AddTextDisplay(modalText)
                    .AddTextInput(new DiscordTextInputComponent("text-input-callback-reminder-delete-reminder-id"), "Reminder ID"));
            }
        }

        [Command("modify")]
        [Description("Modify a reminder.")]
        public static async Task ReminderModifyCommandAsync(SlashCommandContext ctx)
        {
            // We can't defer this!! Want to respond with a modal if the user has >25 reminders.

            var userReminders = await GetUserRemindersAsync(ctx.User.Id);
            if (userReminders.Count == 0)
            {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent($"You don't have any reminders! Set one with {CommandHelpers.GetSlashCmdMention("reminder set")}.")
                    .AsEphemeral(true));
                return;
            }
            else if (userReminders.Count <= 25)
            {
                await ctx.RespondAsync(
                new DiscordInteractionResponseBuilder().WithContent("Please choose a reminder to modify.")
                    .AddActionRowComponent(CreateSelectComponentFromReminders(userReminders, "selectmenu-callback-reminder-modify"))
                    .AsEphemeral(true));
            }
            else
            {
                // User has more than 25 reminders. Show a modal where they are prompted to enter the ID for the reminder they want to modify.
                // I wanted to paginate a select menu instead, but Discord and D#+ limitations make that really difficult for now. (cba writing my own pagination)

                var modalText = "You have a lot of reminders! Please enter the ID of the reminder you wish to modify." +
                    $" You can see reminder IDs with {CommandHelpers.GetSlashCmdMention("reminder list")}.";

                await ctx.RespondWithModalAsync(new DiscordModalBuilder().WithCustomId("modal-callback-reminder-modify").WithTitle("Modify a Reminder")
                    .AddTextDisplay(modalText)
                    .AddTextInput(new DiscordTextInputComponent("text-input-callback-reminder-modify-reminder-id"), "Reminder ID")
                    .AddTextInput(new DiscordTextInputComponent("text-input-callback-reminder-modify-reminder-time", required: false), "(Optional) Enter the new reminder time:")
                    .AddTextInput(new DiscordTextInputComponent("text-input-callback-reminder-modify-reminder-text", required: false), "(Optional) Enter the new reminder text:"));
            }
        }

        [Command("delay")]
        [Description("Delay a reminder that just triggered.")]
        public static async Task ReminderDelayCommandAsync(SlashCommandContext ctx,
            [Parameter("message"), Description("The message for the reminder to delay. Accepts message IDs.")]
            string msgId,
            [Parameter("time"), Description("When do you want to be reminded?")]
            string time,
            [Parameter("private"), Description("Whether to keep this reminder private. It will be sent in DMs.")]
            bool isPrivate = false)
        {
            if (ctx.Guild is null)
                isPrivate = true;

            await ctx.DeferResponseAsync(isPrivate);

            DiscordMessage message;
            try
            {
                message = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(msgId));
            }
            catch
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"I couldn't parse \"{msgId}\" as a message ID! Please try again."));
                return;
            }

            if (message.Author.Id != Setup.State.Discord.Client.CurrentUser.Id ||
                !message.Content.Contains("I have a reminder for you") ||
                message.Embeds.Count < 1)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("That message doesn't look like a reminder! Please try again."));
                return;
            }

            var (triggerTime, error) = ValidateReminderTriggerTime(time);
            if (triggerTime is null)
            {
                await ctx.RespondAsync(error, ephemeral: isPrivate);
                return;
            }

            Setup.Types.Reminder reminder = new()
            {
                UserId = ctx.User.Id,
                ChannelId = ctx.Channel.Id,
                MessageId = message.Id,
                GuildId = ctx.Guild is null ? "@me" : ctx.Guild.Id.ToString(),
                ReminderId = await GenerateUniqueReminderIdAsync(),
                ReminderText = message.Embeds[0].Description,
                TriggerTime = triggerTime.Value,
                SetTime = DateTime.Now
            };

            var triggerTimeTimestamp = reminder.GetTriggerTimeTimestamp();
            var response = await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder().WithContent(
                        $"[Reminder]({reminder.GetJumpLink()})" +
                        $" pushed back to <t:{triggerTimeTimestamp}:F> (<t:{triggerTimeTimestamp}:R>)!" +
                        $"\nReminder ID: `{reminder.ReminderId}`")
                    .AsEphemeral(isPrivate));
            reminder.MessageId = response.Id;

            await Setup.Storage.Redis.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));
        }

        [Command("show")]
        [Description("Show the details for a reminder.")]
        public static async Task ReminderShowCommandAsync(SlashCommandContext ctx, [Parameter("id"), Description("The ID of the reminder to show.")] string id)
        {
            await ctx.DeferResponseAsync(true);

            var (reminder, error) = await GetReminderAsync(id, ctx.User.Id);
            if (reminder is null)
            {
                await ctx.RespondAsync(error, ephemeral: true);
                return;
            }

            DiscordEmbedBuilder embed = new()
            {
                Title = $"Reminder `{id}`",
                Description = reminder.ReminderText,
                Color = Setup.Constants.BotColor
            };

            if (reminder.GuildId != "@me")
            {
                embed.AddField("Server", $"{(await Setup.State.Discord.Client.GetGuildAsync(Convert.ToUInt64(reminder.GuildId))).Name}");
                embed.AddField("Channel", $"<#{reminder.ChannelId}>");
            }

            embed.AddField("Context", reminder.GetJumpLink());

            var setTimeTimestamp = reminder.GetSetTimeTimestamp();
            long triggerTimeTimestamp = reminder.GetTriggerTimeTimestamp();

            embed.AddField("Set At", $"<t:{setTimeTimestamp}:F> (<t:{setTimeTimestamp}:R>)");
            embed.AddField("Set For", $"<t:{triggerTimeTimestamp}:F> (<t:{triggerTimeTimestamp}:R>)");

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral(true));
        }
    }
}
