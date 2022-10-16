namespace MechanicalMilkshake.Commands;

public class Reminders : ApplicationCommandModule
{
    [SlashCommandGroup("reminder", "Set, modify and delete reminders.")]
    public class ReminderCmds
    {
        [SlashCommand("set", "Set a reminder.")]
        public static async Task SetReminder(InteractionContext ctx,
            [Option("time", "When do you want to be reminded?")]
            string time,
            [Option("text", "What should the reminder say?")] [MaximumLength(1000)]
            string text,
            [Option("private", "Whether to keep this reminder private. It will be sent in DMs.")]
            bool isPrivate = false)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral(isPrivate));


            DateTime reminderTime;
            try
            {
                reminderTime = HumanDateParser.HumanDateParser.Parse(time);
            }
            catch
            {
                // Parse error, either because the user did it wrong or because HumanDateParser is weird

                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        $"I couldn't parse \"{time}\" as a time! Please try again.").AsEphemeral(isPrivate));
                return;
            }

            if (reminderTime <= DateTime.Now)
            {
                // If user says something like "4pm" and its past 4pm, assume they mean "4pm tomorrow"
                if (reminderTime.Date == DateTime.Now.Date && reminderTime.TimeOfDay < DateTime.Now.TimeOfDay)
                {
                    reminderTime = reminderTime.AddDays(1);
                }
                else
                {
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent("You can't set a reminder to go off in the past!").AsEphemeral(isPrivate));
                    return;
                }
            }

            var guildId = ctx.Channel.IsPrivate ? "@me" : ctx.Guild.Id.ToString();

            Random random = new();
            var reminderId = random.Next(1000, 9999);

            var reminders = await Program.Db.HashGetAllAsync("reminders");
            foreach (var rem in reminders)
                while (rem.Name == reminderId)
                    reminderId = random.Next(1000, 9999);
            // This is to avoid the potential for duplicate reminders
            Reminder reminder = new()
            {
                UserId = ctx.User.Id,
                ChannelId = ctx.Channel.Id,
                GuildId = guildId,
                ReminderId = reminderId,
                ReminderText = text,
                ReminderTime = reminderTime,
                SetTime = DateTime.Now,
                IsPrivate = isPrivate
            };

            var unixTime = ((DateTimeOffset)reminderTime).ToUnixTimeSeconds();
            var message = await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder()
                    .WithContent($"Reminder set for <t:{unixTime}:F> (<t:{unixTime}:R>)!").AsEphemeral(isPrivate));
            reminder.MessageId = message.Id;

            await Program.Db.HashSetAsync("reminders", reminderId, JsonConvert.SerializeObject(reminder));
        }

        [SlashCommand("list", "List your reminders.")]
        public static async Task ListReminders(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var reminders = await Program.Db.HashGetAllAsync("reminders");
            var userReminders = reminders.Select(reminder => JsonConvert.DeserializeObject<Reminder>(reminder.Value))
                .Where(reminderData => reminderData!.UserId == ctx.User.Id).ToList();

            var output = "";

            // Now we have a list of only the reminders that belong to the user using the command.

            if (userReminders.Count == 0)
            {
                var reminderCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        reminderCmd is null
                            ? "You don't have any reminders!"
                            : $"You don't have any reminders! Set one with </{reminderCmd.Name} set:{reminderCmd.Id}>.")
                    .AsEphemeral());
                return;
            }

            foreach (var reminder in userReminders.OrderBy(r => r.ReminderTime))
            {
                var setTime = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();
                var reminderTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

                Regex idRegex = new("[0-9]+");
                string guildName;
                if (idRegex.IsMatch(reminder.GuildId))
                {
                    var targetGuild =
                        await Program.Discord.GetGuildAsync(Convert.ToUInt64(reminder.GuildId));
                    guildName = targetGuild.Name;
                }
                else
                {
                    guildName = "DMs";
                }

                var reminderLink =
                    $"<https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}>";

                var reminderText = reminder.ReminderText.Length > 350
                    ? $"{reminder.ReminderText.Truncate(350)} *(truncated)*"
                    : reminder.ReminderText;

                output += $"`{reminder.ReminderId}`:\n"
                          + $"> {reminderText}\n"
                          + $"[Set <t:{setTime}:R>]({reminderLink}) to go off <t:{reminderTime}:R> in {guildName}";

                if (guildName != "DMs") output += $" <#{reminder.ChannelId}>";

                output += "\n\n";
            }

            DiscordEmbedBuilder embed = new()
            {
                Title = "Reminders",
                Color = Program.BotColor
            };

            if (output.Length > 4096)
            {
                embed.WithColor(DiscordColor.Red);

                var reminderCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");

                var desc =
                    reminderCmd is null
                        ? "You have too many reminders to list here! Here are the IDs of each one.\n\n"
                        : $"You have too many reminders to list here! Here are the IDs of each one. Use </{reminderCmd.Name} show:{reminderCmd.Id}> for details.\n\n";
                foreach (var reminder in userReminders.OrderBy(r => r.ReminderTime))
                {
                    var setTime = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();
                    var reminderTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

                    desc += $"`{reminder.ReminderId}` - set <t:{setTime}:R> to go off <t:{reminderTime}:R>\n";
                }

                embed.WithDescription(desc.Trim());
            }
            else
            {
                embed.WithDescription(output);
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral());
        }

        [SlashCommand("delete", "Delete a reminder using its unique ID.")]
        public static async Task DeleteReminder(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var options = new List<DiscordSelectComponentOption>();

            var reminders = await Program.Db.HashGetAllAsync("reminders");

            var userHasReminders = false;
            foreach (var reminder in reminders)
            {
                var reminderDetails = JsonConvert.DeserializeObject<Reminder>(reminder.Value);

                if (reminderDetails is null) continue;
                if (reminderDetails.UserId != ctx.User.Id) continue;
                userHasReminders = true;
                options.Add(new DiscordSelectComponentOption(reminderDetails.ReminderId.ToString(),
                    reminderDetails.ReminderId.ToString(),
                    reminderDetails.ReminderText.Truncate(100)));
            }

            if (!userHasReminders)
            {
                var reminderCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        reminderCmd is null
                            ? "You don't have any reminders!"
                            : $"You don't have any reminders! Set one with </{reminderCmd.Name} set:{reminderCmd.Id}>.")
                    .AsEphemeral());
                return;
            }

            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent("Please choose a reminder to delete.")
                    .AddComponents(new DiscordSelectComponent("reminder-delete-dropdown", null, options))
                    .AsEphemeral());
        }

        [SlashCommand("modify", "Modify an existing reminder using its unique ID.")]
        public static async Task ModifyReminder(InteractionContext ctx,
            [Option("reminder", "The ID of the reminder to modify. You can get this with `/reminder list`.")]
            long reminderToModify,
            [Option("time", "When do you want to be reminded? Leave this blank if you don't want to change it.")]
            string time = null,
            [Option("text", "What should the reminder say? Leave this blank if you don't want to change it.")]
            [MaximumLength(1000)]
            string text = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            Regex idRegex = new("[0-9]+");
            if (!idRegex.IsMatch(reminderToModify.ToString()))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "The reminder ID you provided isn't correct! You can get a reminder ID with `/reminder list`. It should look something like this: `1234`")
                    .AsEphemeral());
                return;
            }

            var currentReminders = await Program.Db.HashGetAllAsync("reminders");
            var keys = currentReminders.Select(item => item.Name.ToString()).ToList();

            if (!keys.Contains(reminderToModify.ToString()))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "A reminder with that ID doesn't exist! Make sure you've got the right ID. You can get it with `/reminder list`. It should look something like this: `1234`")
                    .AsEphemeral());
                return;
            }

            var reminder =
                JsonConvert.DeserializeObject<Reminder>(
                    await Program.Db.HashGetAsync("reminders", reminderToModify));

            if (reminder!.UserId != ctx.User.Id)
            {
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "Only the person who set that reminder can modify it!").AsEphemeral());
                return;
            }

            if (text == null && time == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Reminder unchanged.")
                    .AsEphemeral());
                return;
            }

            if (text != null) reminder.ReminderText = text;

            if (time != null) reminder.ReminderTime = HumanDateParser.HumanDateParser.Parse(time);

            await Program.Db.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));

            if (!reminder.IsPrivate)
            {
                var reminderChannel = await Program.Discord.GetChannelAsync(reminder.ChannelId);
                var reminderMessage = await reminderChannel.GetMessageAsync(reminder.MessageId);

                var unixTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

                if (reminderMessage.Content.Contains("pushed back"))
                    await reminderMessage.ModifyAsync(
                        $"[Reminder](https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}) pushed back to <t:{unixTime}:F> (<t:{unixTime}:R>)!");
                else
                    await reminderMessage.ModifyAsync($"Reminder set for <t:{unixTime}:F> (<t:{unixTime}:R>)!");
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Reminder modified successfully.").AsEphemeral());
        }

        [SlashCommand("pushback", "Push back a reminder that just went off.")]
        public static async Task PushBackReminder(InteractionContext ctx,
            [Option("message", "The message for the reminder to push back. Accepts message IDs.")]
            string msgId,
            [Option("time", "When do you want to be reminded?")]
            string time,
            [Option("private", "Whether to keep this reminder private. It will be sent in DMs.")]
            bool isPrivate = false)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral(isPrivate));

            DiscordMessage message;
            try
            {
                message = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(msgId));
            }
            catch
            {
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        $"I couldn't parse \"{msgId}\" as a message ID! Please try again." +
                        "\n\nIf you think I messed up or need help, contact the bot owner (if you don't know who that is, see `/about`!)."));
                return;
            }

            if (message.Author.Id != Program.Discord.CurrentUser.Id ||
                !message.Content.Contains("I have a reminder for you") || message.Embeds.Count < 1)
            {
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "That message doesn't look like a reminder! Please try again." +
                        "\n\nIf you think I messed up or need help, contact the bot owner (if you don't know who that is, see `/about`!)."));
                return;
            }

            DateTime reminderTime;
            try
            {
                reminderTime = HumanDateParser.HumanDateParser.Parse(time);
            }
            catch
            {
                // Parse error, either because the user did it wrong or because HumanDateParser is weird

                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        $"I couldn't parse \"{time}\" as a time! Please try again."));
                return;
            }

            if (reminderTime <= DateTime.Now)
            {
                // If user says something like "4pm" and its past 4pm, assume they mean "4pm tomorrow"
                if (reminderTime.Date == DateTime.Now.Date && reminderTime.TimeOfDay < DateTime.Now.TimeOfDay)
                {
                    reminderTime = reminderTime.AddDays(1);
                }
                else
                {
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent("You can't set a reminder to go off in the past!").AsEphemeral(isPrivate));
                    return;
                }
            }

            Regex userIdRegex = new("[0-9]+");

            var origUserId = Convert.ToUInt64(userIdRegex.Matches(message.Content)[0].ToString());

            if (origUserId != ctx.User.Id)
            {
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "Only the person who set that reminder can push it back!"));
                return;
            }

            /*
              Compare reminder to be pushed back against list of user's current reminders so that if
              the user is trying to push back a reminder that might have already been pushed back,
              they are warned first in an attempt to prevent unwanted duplicates.
            */

            var reminders = await Program.Db.HashGetAllAsync("reminders");

            foreach (var rem in reminders)
            {
                var reminderData = JsonConvert.DeserializeObject<Reminder>(rem.Value);

                if (reminderData!.UserId != ctx.User.Id) continue;
                if (reminderData.ReminderText != message.Embeds[0].Description) continue;
                // Reminder is a potential duplicate

                var reminderCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    reminderCmd is null
                        ? "Warning: you might have already pushed back this reminder! Another reminder already exists with the same content." +
                          "\n\nIf you still want to create this reminder, please set it manually."
                        : "Warning: you might have already pushed back this reminder! Another reminder already exists with the same content." +
                          $"\n\nTo see details, use </{reminderCmd.Name} show:{reminderCmd.Id}> and set `id` to `{reminderData.ReminderId}`." +
                          $"\n\nIf you still want to create this reminder, use </{reminderCmd.Name} set:{reminderCmd.Id}>."));
                return;
            }

            var guildId = ctx.Channel.IsPrivate ? "@me" : ctx.Guild.Id.ToString();

            Random random = new();
            var reminderId = random.Next(1000, 9999);

            // This is to avoid the potential for duplicate reminders
            foreach (var rem in reminders)
                while (rem.Name == reminderId)
                    reminderId = random.Next(1000, 9999);
            Reminder reminder = new()
            {
                UserId = ctx.User.Id,
                ChannelId = ctx.Channel.Id,
                GuildId = guildId,
                ReminderId = reminderId,
                ReminderText = message.Embeds[0].Description,
                ReminderTime = reminderTime,
                SetTime = DateTime.Now,
                IsPrivate = isPrivate
            };

            var unixTime = ((DateTimeOffset)reminderTime).ToUnixTimeSeconds();

            var response = await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent(
                        $"[Reminder](https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{msgId}) pushed back to <t:{unixTime}:F> (<t:{unixTime}:R>)!")
                    .AsEphemeral(isPrivate));
            reminder.MessageId = response.Id;

            await Program.Db.HashSetAsync("reminders", reminderId, JsonConvert.SerializeObject(reminder));
        }

        [SlashCommand("show", "Show the details for a reminder.")]
        public static async Task ReminderShow(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var options = new List<DiscordSelectComponentOption>();

            var reminders = await Program.Db.HashGetAllAsync("reminders");

            var userHasReminders = false;
            foreach (var reminder in reminders)
            {
                var reminderDetails = JsonConvert.DeserializeObject<Reminder>(reminder.Value);

                if (reminderDetails!.UserId != ctx.User.Id) continue;
                userHasReminders = true;
                options.Add(new DiscordSelectComponentOption(reminderDetails.ReminderId.ToString(),
                    reminderDetails.ReminderId.ToString(),
                    reminderDetails.ReminderText.Truncate(100)));
            }

            if (!userHasReminders)
            {
                var reminderCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        reminderCmd is null
                            ? "You don't have any reminders!"
                            : $"You don't have any reminders! Set one with </{reminderCmd.Name} set:{reminderCmd.Id}>.")
                    .AsEphemeral());
                return;
            }

            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent("Please choose a reminder to show details for.")
                    .AddComponents(new DiscordSelectComponent("reminder-show-dropdown", null, options))
                    .AsEphemeral());
        }
    }
}