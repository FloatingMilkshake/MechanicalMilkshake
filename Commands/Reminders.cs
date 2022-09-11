namespace MechanicalMilkshake.Commands;

public class Reminders : ApplicationCommandModule
{
    [SlashCommandGroup("reminder", "Set, modify and delete reminders.")]
    public class ReminderCmds
    {
        [SlashCommand("set", "Set a reminder.")]
        public async Task SetReminder(InteractionContext ctx,
            [Option("time", "When do you want to be reminded?")]
            string time,
            [Option("text", "What should the reminder say?")]
            string text)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder());

            if (text.Length > 1000)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Reminders can't be over 1000 characters long! Try shortening your reminder."));
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
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "You can't set a reminder to go off in the past!"));
                return;
            }

            string guildId;
            if (ctx.Channel.IsPrivate)
                guildId = "@me";
            else
                guildId = ctx.Guild.Id.ToString();

            Random random = new();
            var reminderId = random.Next(1000, 9999);

            var reminders = await Program.db.HashGetAllAsync("reminders");
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
                SetTime = DateTime.Now
            };

            var unixTime = ((DateTimeOffset)reminderTime).ToUnixTimeSeconds();
            var message = await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent(
                    $"Reminder set for <t:{unixTime}:F> (<t:{unixTime}:R>)!"));
            reminder.MessageId = message.Id;

            await Program.db.HashSetAsync("reminders", reminderId, JsonConvert.SerializeObject(reminder));
        }

        [SlashCommand("list", "List your reminders.")]
        public async Task ListReminders(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            List<Reminder> userReminders = new();

            var reminders = await Program.db.HashGetAllAsync("reminders");
            foreach (var reminder in reminders)
            {
                var reminderData = JsonConvert.DeserializeObject<Reminder>(reminder.Value);

                if (reminderData.UserId == ctx.User.Id)
                    // Reminder belongs to user. Add to list.
                    userReminders.Add(reminderData);
            }

            var output = "";

            // Now we have a list of only the reminders that belong to the user using the command.

            if (userReminders.Count == 0)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You don't have any reminders! Set one with `/reminder set`.").AsEphemeral());
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
                        await Program.discord.GetGuildAsync(Convert.ToUInt64(reminder.GuildId));
                    guildName = targetGuild.Name;
                }
                else
                {
                    guildName = "DMs";
                }

                var reminderLink =
                    $"<https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}>";

                string reminderText;
                if (reminder.ReminderText.Length > 350)
                    reminderText = $"{reminder.ReminderText.Truncate(350)} *(truncated)*";
                else
                    reminderText = reminder.ReminderText;

                Regex urlRegex = new(@"http(?:s)?://.*\..*");

                if (urlRegex.IsMatch(reminderText))
                    // Output has URLs. Surround with <> to suppress embeds.
                    foreach (var match in urlRegex.Matches(reminderText).Cast<Match>())
                        reminderText = reminderText.Replace(match.ToString(), $"<{match}>");

                output += $"`{reminder.ReminderId}`:\n"
                          + $"> {reminderText}\n"
                          + $"[Set <t:{setTime}:R>]({reminderLink}) to go off <t:{reminderTime}:R> in {guildName}";

                if (guildName != "DMs") output += $" <#{reminder.ChannelId}>";

                output += "\n\n";
            }

            DiscordEmbedBuilder embed = new()
            {
                Title = "Reminders",
                Color = new DiscordColor("#FAA61A")
            };

            if (output.Length > 4096)
            {
                embed.WithColor(DiscordColor.Red);

#if DEBUG
                var slashCmds = await Program.discord.GetGuildApplicationCommandsAsync(Program.configjson.HomeServerId);
#else
                var slashCmds = await Program.discord.GetGlobalApplicationCommandsAsync();
#endif
                var reminderCmd = slashCmds.FirstOrDefault(c => c.Name == "reminder");
                var reminderShowCmd = reminderCmd.Options.FirstOrDefault(c => c.Name == "show");

                var desc =
                    $"You have too many reminders to list here! Here are the IDs of each one. Use </{reminderCmd.Name} {reminderShowCmd.Name}:{reminderCmd.Id}> for details.\n\n";
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
        public async Task DeleteReminder(InteractionContext ctx,
            [Option("reminder", "The ID of the reminder to delete. You can get this with `/reminder list`.")]
            long reminderToDelete)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var reminders = await Program.db.HashGetAllAsync("reminders");
            var reminderExists = false;
            foreach (var reminder in reminders)
                if (reminder.Name.ToString().Contains(reminderToDelete.ToString()))
                {
                    reminderExists = true;
                    break;
                }

            if (reminderExists)
            {
                var reminder =
                    JsonConvert.DeserializeObject<Reminder>(
                        await Program.db.HashGetAsync("reminders", reminderToDelete));
                if (reminder.UserId != ctx.User.Id)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("Only the person who set that reminder can delete it!").AsEphemeral());
                    return;
                }

                var reminderChannel = await Program.discord.GetChannelAsync(reminder.ChannelId);
                var reminderMessage = await reminderChannel.GetMessageAsync(reminder.MessageId);

                await reminderMessage.ModifyAsync("This reminder was deleted.");

                await Program.db.HashDeleteAsync("reminders", reminderToDelete);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("Reminder deleted successfully.").AsEphemeral());
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "I couldn't find a reminder with that ID! Make sure it's correct. You can check the IDs of your reminders with `/reminder list`.")
                    .AsEphemeral());
            }
        }

        [SlashCommand("modify", "Modify an existing reminder using its unique ID.")]
        public async Task ModifyReminder(InteractionContext ctx,
            [Option("reminder", "The ID of the reminder to modify. You can get this with `/reminder list`.")]
            long reminderToModify,
            [Option("time", "When do you want to be reminded? Leave this blank if you don't want to change it.")]
            string time = null,
            [Option("text", "What should the reminder say? Leave this blank if you don't want to change it.")]
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

            var currentReminders = await Program.db.HashGetAllAsync("reminders");
            List<string> keys = new();
            foreach (var item in currentReminders)
            {
                var key = item.Name.ToString();
                keys.Add(key);
            }

            if (!keys.Contains(reminderToModify.ToString()))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "A reminder with that ID doesn't exist! Make sure you've got the right ID. You can get it with `/reminder list`. It should look something like this: `1234`")
                    .AsEphemeral());
                return;
            }

            var reminder =
                JsonConvert.DeserializeObject<Reminder>(
                    await Program.db.HashGetAsync("reminders", reminderToModify));

            if (reminder.UserId != ctx.User.Id)
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

            await Program.db.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));

            var reminderChannel = await Program.discord.GetChannelAsync(reminder.ChannelId);
            var reminderMessage = await reminderChannel.GetMessageAsync(reminder.MessageId);

            var unixTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

            if (reminderMessage.Content.Contains("pushed back"))
                await reminderMessage.ModifyAsync(
                    $"[Reminder](https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}) pushed back to <t:{unixTime}:F> (<t:{unixTime}:R>)!");
            else
                await reminderMessage.ModifyAsync($"Reminder set for <t:{unixTime}:F> (<t:{unixTime}:R>)!");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Reminder modified successfully.").AsEphemeral());
        }

        [SlashCommand("pushback", "Push back a reminder that just went off.")]
        public async Task PushBackReminder(InteractionContext ctx,
            [Option("message", "The message for the reminder to push back. Accepts message IDs.")]
            string msgId,
            [Option("time", "When do you want to be reminded?")]
            string time)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordMessage message;
            try
            {
                message = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(msgId));
            }
            catch
            {
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        $"I couldn't parse \"{msgId}\" as a message ID! Please try again."));
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
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "You can't set a reminder to go off in the past!"));
                return;
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

            string guildId;
            if (ctx.Channel.IsPrivate)
                guildId = "@me";
            else
                guildId = ctx.Guild.Id.ToString();

            Random random = new();
            var reminderId = random.Next(1000, 9999);

            var reminders = await Program.db.HashGetAllAsync("reminders");
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
                ReminderText = message.Embeds[0].Description,
                ReminderTime = reminderTime,
                SetTime = DateTime.Now
            };

            var unixTime = ((DateTimeOffset)reminderTime).ToUnixTimeSeconds();

            var response = await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent(
                    $"[Reminder](https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{msgId}) pushed back to <t:{unixTime}:F> (<t:{unixTime}:R>)!"));
            reminder.MessageId = response.Id;

            await Program.db.HashSetAsync("reminders", reminderId, JsonConvert.SerializeObject(reminder));
        }

        [SlashCommand("show", "Show the details for a reminder.")]
        public async Task ReminderShow(InteractionContext ctx,
            [Option("id", "The ID of the reminder to show details for. You can get this with /reminder list.")]
            long id)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            Regex idRegex = new("[0-9]+");
            if (!idRegex.IsMatch(id.ToString()))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "The reminder ID you provided isn't correct! You can get a reminder ID with `/reminder list`. It should look something like this: `1234`")
                    .AsEphemeral());
                return;
            }

            var currentReminders = await Program.db.HashGetAllAsync("reminders");
            List<string> keys = new();
            foreach (var item in currentReminders)
            {
                var key = item.Name.ToString();
                keys.Add(key);
            }

            if (!keys.Contains(id.ToString()))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "A reminder with that ID doesn't exist! Make sure you've got the right ID. You can get it with `/reminder list`. It should look something like this: `1234`")
                    .AsEphemeral());
                return;
            }

            var reminder =
                JsonConvert.DeserializeObject<Reminder>(
                    await Program.db.HashGetAsync("reminders", id));

            DiscordEmbedBuilder embed = new()
            {
                Title = $"Reminder `{id}`",
                Description = reminder.ReminderText,
                Color = new DiscordColor("#FAA61A")
            };
            embed.AddField("Server",
                $"{(await Program.discord.GetGuildAsync(Convert.ToUInt64(reminder.GuildId))).Name}");
            embed.AddField("Channel", $"<#{reminder.ChannelId}>");
            embed.AddField("Jump Link", $"https://discord.com/channels/{reminder.ChannelId}/{reminder.MessageId}/");

            var setTime = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();
            var reminderTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

            embed.AddField("Set At", $"<t:{setTime}:F> (<t:{setTime}:R>)");
            embed.AddField("Set For", $"<t:{reminderTime}:F> (<t:{reminderTime}:R>)");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral());
        }
    }
}