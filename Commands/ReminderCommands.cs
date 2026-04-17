namespace MechanicalMilkshake.Commands;

[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
[InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
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
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            var (parsedTime, error) = Setup.Types.Reminder.ParseTriggerTime(time);
            if (parsedTime is null)
            {
                await ctx.FollowupAsync(error, ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true));
                return;
            }

            var reminderId = await Setup.Types.Reminder.GenerateUniqueIdAsync();
            var reminderTriggerTimeTimestamp = parsedTime.Value.ToUnixTimeSeconds();

            var message = await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Reminder set for <t:{reminderTriggerTimeTimestamp}:F> (<t:{reminderTriggerTimeTimestamp}:R>)!" +
                                    $"\nReminder ID: `{reminderId}`")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));

            var reminder = new Setup.Types.Reminder(ctx.User.Id,
                ctx.Channel.Id,
                ctx.Interaction.IsUserInstallContext() ? "@me" : ctx.Guild.Id.ToString(),
                message.Id,
                reminderId,
                text,
                parsedTime.Value,
                DateTime.Now);

            await Setup.Storage.Redis.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));
        }

        [Command("list")]
        [Description("List your reminders.")]
        public static async Task ReminderListCommandAsync(SlashCommandContext ctx)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true));

            var userReminders = await Setup.Types.Reminder.GetUserRemindersAsync(ctx.User.Id);

            if (userReminders.Count == 0)
            {

                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"You don't have any reminders! Set one with {"reminder set".AsSlashCommandMention()}.")
                    .AsEphemeral(true));
                return;
            }

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .AddEmbed(await userReminders.CreateEmbedAsync())
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
        }

        [Command("delete")]
        [Description("Delete a reminder.")]
        public static async Task ReminderDeleteCommandAsync(SlashCommandContext ctx)
        {
            // We can't defer this!! Want to respond with a modal if the user has >25 reminders.

            var userReminders = await Setup.Types.Reminder.GetUserRemindersAsync(ctx.User.Id);
            if (userReminders.Count == 0)
            {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent($"You don't have any reminders! Set one with {"reminder set".AsSlashCommandMention()}.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
                return;
            }
            else if (userReminders.Count <= 25)
            {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Please choose a reminder to delete.")
                    .AddActionRowComponent(userReminders.CreateSelectComponent("selectmenu-callback-reminder-delete"))
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
            }
            else
            {
                // User has more than 25 reminders. Show a modal where they are prompted to enter the ID for the reminder they want to delete.
                // I wanted to paginate a select menu instead, but Discord and D#+ limitations make that really difficult for now. (cba writing my own pagination)

                var modalText = "You have a lot of reminders! Please enter the ID of the reminder you wish to delete." +
                    $" You can see reminder IDs with {"reminder list".AsSlashCommandMention()}.";

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

            var userReminders = await Setup.Types.Reminder.GetUserRemindersAsync(ctx.User.Id);
            if (userReminders.Count == 0)
            {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent($"You don't have any reminders! Set one with {"reminder set".AsSlashCommandMention()}.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
                return;
            }
            else if (userReminders.Count <= 25)
            {
                await ctx.RespondAsync(
                new DiscordInteractionResponseBuilder().WithContent("Please choose a reminder to modify.")
                    .AddActionRowComponent(userReminders.CreateSelectComponent("selectmenu-callback-reminder-modify"))
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
            }
            else
            {
                // User has more than 25 reminders. Show a modal where they are prompted to enter the ID for the reminder they want to modify.
                // I wanted to paginate a select menu instead, but Discord and D#+ limitations make that really difficult for now. (cba writing my own pagination)

                var modalText = "You have a lot of reminders! Please enter the ID of the reminder you wish to modify." +
                    $" You can see reminder IDs with {"reminder list".AsSlashCommandMention()}.";

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
            string time)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            DiscordMessage message;
            try
            {
                message = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(msgId));
            }
            catch (FormatException)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"I couldn't parse \"{msgId}\" as a message ID! Please try again.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }
            catch (NotFoundException)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"I couldn't find that message! Please check the message ID you entered and try again.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }
            catch (UnauthorizedException)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"I couldn't read that message! Please make sure I have permission to read message history and try again.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }

            if (message.Author.Id != Setup.State.Discord.Client.CurrentUser.Id ||
                !message.Content.Contains("I have a reminder for you") ||
                message.Embeds.Count < 1)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("That message doesn't look like a reminder! Please try again.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }

            var (triggerTime, error) = Setup.Types.Reminder.ParseTriggerTime(time);
            if (triggerTime is null)
            {
                await ctx.FollowupAsync(error, ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));
                return;
            }

            var reminderId = await Setup.Types.Reminder.GenerateUniqueIdAsync();
            var triggerTimeTimestamp = triggerTime.Value.ToUnixTimeSeconds();

            var response = await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder().WithContent(
                        $"[Reminder]({message.JumpLink})" +
                        $" pushed back to <t:{triggerTimeTimestamp}:F> (<t:{triggerTimeTimestamp}:R>)!" +
                        $"\nReminder ID: `{reminderId}`")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));

            var reminder = new Setup.Types.Reminder(ctx.User.Id,
                ctx.Channel.Id,
                ctx.Guild is null ? "@me" : ctx.Guild.Id.ToString(),
                response.Id,
                reminderId,
                message.Embeds[0].Description,
                triggerTime.Value,
                DateTime.Now);

            await Setup.Storage.Redis.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));
        }

        [Command("show")]
        [Description("Show the details for a reminder.")]
        public static async Task ReminderShowCommandAsync(SlashCommandContext ctx, [Parameter("id"), Description("The ID of the reminder to show.")] string id)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true));

            var (reminder, error) = await Setup.Types.Reminder.GetReminderAsync(id, ctx.User.Id);
            if (reminder is null)
            {
                await ctx.FollowupAsync(error, ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true));
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

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .AddEmbed(embed)
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
        }
    }
}
