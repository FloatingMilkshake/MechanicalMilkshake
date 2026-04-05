using static MechanicalMilkshake.Helpers.ReminderHelpers;

namespace MechanicalMilkshake.Commands;

[Command("reminder")]
[Description("Set, modify and delete reminders.")]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)] // TODO: this one needs to be tested
[InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
public partial class ReminderCmds
{
    [Command("set")]
    [Description("Set a reminder.")]
    public static async Task SetReminder(SlashCommandContext ctx,
        [Parameter("time"), Description("When do you want to be reminded?")]
        string time,
        [Parameter("text"), Description("What should the reminder say?")] [MinMaxLength(maxLength: 1000)]
        string text = "",
        [Parameter("private"), Description("Whether to keep this reminder private. It will be sent in DMs.")]
        bool isPrivate = false)
    {
        if (ctx.Guild is null)
            isPrivate = true;

        await ctx.DeferResponseAsync(isPrivate);

        var (parsedTime, error) = await ValidateReminderTimeAsync(time);
        if (parsedTime is null)
        {
            await ctx.RespondAsync(error, ephemeral: true);
            return;
        }

        Reminder reminder = new()
        {
            UserId = ctx.User.Id,
            ChannelId = ctx.Channel.Id,
            GuildId = ctx.Guild is null ? "@me" : ctx.Guild.Id.ToString(),
            ReminderId = await GenerateUniqueReminderIdAsync(),
            ReminderText = text,
            ReminderTime = parsedTime,
            SetTime = DateTime.Now,
            IsPrivate = isPrivate
        };

        var unixTime = ((DateTimeOffset)parsedTime).ToUnixTimeSeconds();

        if (isPrivate && ctx.Guild is not null)
            // Try to DM user. If DMs are closed, private reminders will not work; we should let them know now instead of having
            // the bot throw an error (not shown to the them), leaving them wondering where their reminder is.
            try
            {
                await ctx.User.SendMessageAsync(
                    $"Hi! This is a confirmation for your reminder, due for <t:{unixTime}:F> (<t:{unixTime}:R>)!");
            }
            catch
            {
                // User has DMs disabled or has blocked the bot. Alert them of this to prevent issues sending the reminder later.
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "You have DMs disabled or have me blocked, so I won't be able to send you this reminder privately!" +
                        "\n\nReminder creation cancelled. Please enable your DMs and/or unblock me and try again.")
                    .AsEphemeral());
                return;
            }

        var message = await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Reminder set for <t:{unixTime}:F> (<t:{unixTime}:R>)!" +
                                $"\nReminder ID: `{reminder.ReminderId}`")
                .AsEphemeral(isPrivate));
        reminder.MessageId = message.Id;

        await Program.Db.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));
    }

    [Command("list")]
    [Description("List your reminders.")]
    public static async Task ListReminders(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(true);

        var userReminders = await GetUserRemindersAsync(ctx.User.Id);

        if (userReminders.Count == 0)
        {
            var reminderCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    reminderCmd is null
                        ? "You don't have any reminders!"
                        : $"You don't have any reminders! Set one with </{reminderCmd.Name} set:{reminderCmd.Id}>.")
                .AsEphemeral());
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .AddEmbed(await CreateReminderListEmbedAsync(userReminders))
            .AsEphemeral());
    }

    [Command("delete")]
    [Description("Delete a reminder.")]
    public static async Task DeleteReminder(SlashCommandContext ctx)
    {
        // We can't defer this!! Want to respond with a modal if the user has >25 reminders.

        var userReminders = await GetUserRemindersAsync(ctx.User.Id);
        if (userReminders.Count == 0)
        {
            var reminderCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");

            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    reminderCmd is null
                        ? "You don't have any reminders!"
                        : $"You don't have any reminders! Set one with </{reminderCmd.Name} set:{reminderCmd.Id}>.")
                .AsEphemeral());
            return;
        }
        else if (userReminders.Count <= 25)
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Please choose a reminder to delete.")
                .AddActionRowComponent(CreateSelectComponentFromReminders(userReminders, "reminder-delete-dropdown"))
                .AsEphemeral());
        }
        else
        {
            // User has more than 25 reminders. Show a modal where they are prompted to enter the ID for the reminder they want to delete.
            // I wanted to paginate a select menu instead, but Discord and D#+ limitations make that really difficult for now. (cba writing my own pagination)

            var modalText = "You have a lot of reminders! Please enter the ID of the reminder you wish to delete.";
            var reminderListCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");
            if (reminderListCmd is not null)
                modalText += $" You can see reminder IDs with </reminder list:{reminderListCmd.Id}>.";

            await ctx.RespondWithModalAsync(new DiscordModalBuilder().WithCustomId("reminder-delete-modal").WithTitle("Delete a Reminder")
                .AddTextDisplay(modalText)
                .AddTextInput(new DiscordTextInputComponent("reminder-delete-id-input"), "Reminder ID"));
        }
    }

    [Command("modify")]
    [Description("Modify a reminder.")]
    public static async Task ModifyReminder(SlashCommandContext ctx)
    {
        // We can't defer this!! Want to respond with a modal if the user has >25 reminders.

        var userReminders = await GetUserRemindersAsync(ctx.User.Id);
        if (userReminders.Count == 0)
        {
            var reminderCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");

            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    reminderCmd is null
                        ? "You don't have any reminders!"
                        : $"You don't have any reminders! Set one with </{reminderCmd.Name} set:{reminderCmd.Id}>.")
                .AsEphemeral());
            return;
        }
        else if (userReminders.Count <= 25)
        {
            await ctx.RespondAsync(
            new DiscordInteractionResponseBuilder().WithContent("Please choose a reminder to modify.")
                .AddActionRowComponent(CreateSelectComponentFromReminders(userReminders, "reminder-modify-dropdown"))
                .AsEphemeral());
        }
        else
        {
            // User has more than 25 reminders. Show a modal where they are prompted to enter the ID for the reminder they want to modify.
            // I wanted to paginate a select menu instead, but Discord and D#+ limitations make that really difficult for now. (cba writing my own pagination)

            var modalText = "You have a lot of reminders! Please enter the ID of the reminder you wish to modify.";
            var reminderListCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");
            if (reminderListCmd is not null)
                modalText += $" You can see reminder IDs with </reminder list:{reminderListCmd.Id}>.";

            await ctx.RespondWithModalAsync(new DiscordModalBuilder().WithCustomId("reminder-modify-modal").WithTitle("Modify a Reminder")
                .AddTextDisplay(modalText)
                .AddTextInput(new DiscordTextInputComponent("reminder-modify-id-input"), "Reminder ID")
                .AddTextInput(new DiscordTextInputComponent("reminder-modify-time-input", required: false), "(Optional) Enter the new reminder time:")
                .AddTextInput(new DiscordTextInputComponent("reminder-modify-text-input", required: false), "(Optional) Enter the new reminder text:"));
        }
    }

    [Command("delay")]
    [Description("Delay a reminder that just triggered.")]
    public static async Task DelayReminder(SlashCommandContext ctx,
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

        if (message.Author.Id != Program.Discord.CurrentUser.Id ||
            !message.Content.Contains("I have a reminder for you") ||
            message.Embeds.Count < 1)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("That message doesn't look like a reminder! Please try again."));
            return;
        }

        var (reminderTime, error) = await ValidateReminderTimeAsync(time);
        if (reminderTime is null)
        {
            await ctx.RespondAsync(error, ephemeral: isPrivate);
            return;
        }

        Reminder reminder = new()
        {
            UserId = ctx.User.Id,
            ChannelId = ctx.Channel.Id,
            MessageId = message.Id,
            GuildId = ctx.Guild is null ? "@me" : ctx.Guild.Id.ToString(),
            ReminderId = await GenerateUniqueReminderIdAsync(),
            ReminderText = message.Embeds[0].Description,
            ReminderTime = reminderTime,
            SetTime = DateTime.Now,
            IsPrivate = isPrivate
        };

        var unixTime = ((DateTimeOffset)reminderTime).ToUnixTimeSeconds();

        var response = await ctx.FollowupAsync(
            new DiscordFollowupMessageBuilder().WithContent(
                    $"[Reminder](https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId})" +
                    $" pushed back to <t:{unixTime}:F> (<t:{unixTime}:R>)!" +
                    $"\nReminder ID: `{reminder.ReminderId}`")
                .AsEphemeral(isPrivate));
        reminder.MessageId = response.Id;

        await Program.Db.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));
    }

    [Command("show")]
    [Description("Show the details for a reminder.")]
    public static async Task ReminderShow(SlashCommandContext ctx, [Parameter("id"), Description("The ID of the reminder to show.")] string id)
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
            Color = Program.BotColor
        };

        if (reminder.GuildId != "@me" && reminder.ReminderTime is not null)
        {
            embed.AddField("Server",
                $"{(await Program.Discord.GetGuildAsync(Convert.ToUInt64(reminder.GuildId))).Name}");
            embed.AddField("Channel", $"<#{reminder.ChannelId}>");
        }

        var jumpLink = reminder.IsPrivate
            ? $"This reminder was set privately, so the message where it was set is unavailable. Here is a link to the surrounding context: https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}"
            : $"https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}";

        embed.AddField("Jump Link", jumpLink);

        var setTime = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();

        long reminderTime = default;
        if (reminder.ReminderTime is not null)
            reminderTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

        embed.AddField("Set At", $"<t:{setTime}:F> (<t:{setTime}:R>)");

        if (reminder.ReminderTime is not null)
            embed.AddField("Set For", $"<t:{reminderTime}:F> (<t:{reminderTime}:R>)");
        else
            embed.WithFooter(
                "This reminder will not be sent automatically.");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral());
    }
}
