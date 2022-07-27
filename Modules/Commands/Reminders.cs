namespace MechanicalMilkshake.Modules.Commands
{
    public class Reminders : ApplicationCommandModule
    {
        [SlashCommandGroup("reminder", "Set, modify and delete reminders.")]
        public class ReminderCmds
        {
            [SlashCommand("set", "Set a reminder.")]
            public async Task SetReminder(InteractionContext ctx,
                [Option("text", "What should the reminder say?")] string text,
                [Option("time", "When do you want to be reminded?")] string time)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());

                DateTime reminderTime;
                try
                {
                    reminderTime = HumanDateParser.HumanDateParser.Parse(time);
                }
                catch
                {
                    // Parse error, either because the user did it wrong or because HumanDateParser is weird

                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"I couldn't parse \"{time}\" as a time! Please try again."));
                    return;
                }
                if (reminderTime <= DateTime.Now)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You can't set a reminder to go off in the past!"));
                    return;
                }

                string guildId;
                if (ctx.Channel.IsPrivate)
                {
                    guildId = "@me";
                }
                else
                {
                    guildId = ctx.Guild.Id.ToString();
                }

                Random random = new();
                int reminderId = random.Next(1000, 9999);

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

                long unixTime = ((DateTimeOffset)reminderTime).ToUnixTimeSeconds();
                DiscordMessage message = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Reminder set for <t:{unixTime}:F> (<t:{unixTime}:R>)!"));
                reminder.MessageId = message.Id;

                await Program.db.HashSetAsync("reminders", reminderId, JsonConvert.SerializeObject(reminder));
            }

            [SlashCommand("list", "List your reminders.")]
            public async Task ListReminders(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                List<Reminder> userReminders = new();

                HashEntry[] reminders = await Program.db.HashGetAllAsync("reminders");
                if (reminders.Length == 0)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You don't have any reminders! Set one with `/reminder set`.").AsEphemeral(true));
                    return;
                }

                foreach (HashEntry reminder in reminders)
                {
                    Reminder reminderData = JsonConvert.DeserializeObject<Reminder>(reminder.Value);

                    if (reminderData.UserId == ctx.User.Id)
                    {
                        // Reminder belongs to user. Add to list.
                        userReminders.Add(reminderData);
                    }
                }

                string output = "";
                // Now we have a list of only the reminders that belong to the user using the command.
                foreach (Reminder reminder in userReminders)
                {
                    long setTime = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();
                    long reminderTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

                    Regex idRegex = new("[0-9]+");
                    string guildName;
                    if (idRegex.IsMatch(reminder.GuildId))
                    {
                        DiscordGuild targetGuild = await Program.discord.GetGuildAsync(Convert.ToUInt64(reminder.GuildId));
                        guildName = targetGuild.Name;
                    }
                    else
                    {
                        guildName = "DMs";
                    }

                    output += $"`{reminder.ReminderId}`:\n"
                        + $"> {reminder.ReminderText}\n"
                        + $"Set <t:{setTime}:R> to go off <t:{reminderTime}:R>\n"
                        + $"Set in {guildName}";

                    if (guildName != "DMs")
                    {
                        output += $" <#{reminder.ChannelId}>";
                    }
                    output += "\n\n";
                }

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(output).AsEphemeral(true));
            }

            [SlashCommand("delete", "Delete a reminder using its unique ID.")]
            public async Task DeleteReminder(InteractionContext ctx, [Option("reminder", "The ID of the reminder to delete. You can get this with `/reminder list`.")] string reminderToDelete)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                ulong reminderId;
                try
                {
                    reminderId = Convert.ToUInt64(reminderToDelete);
                }
                catch
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("The reminder ID you provided isn't correct! You can get a reminder ID with `/reminder list`. It should look something like this: `1001230509856260276`").AsEphemeral(true));
                    return;
                }

                await Program.db.HashDeleteAsync("reminders", reminderToDelete.ToString());
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Reminder deleted successfully.").AsEphemeral(true));
            }

            [SlashCommand("modify", "Modify an existing reminder using its unique ID.")]
            public async Task ModifyReminder(InteractionContext ctx,
                [Option("reminder", "The ID of the reminder to modify. You can get this with `/reminder list`.")] string reminderToModify,
                [Option("text", "What should the reminder say? Leave this blank if you don't want to change it.")] string text = null,
                [Option("time", "When do you want to be reminded? Leave this blank if you don't want to change it.")] string time = null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                Regex idRegex = new("[0-9]+");
                if (!idRegex.IsMatch(reminderToModify))
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("The reminder ID you provided isn't correct! You can get a reminder ID with `/reminder list`. It should look something like this: `1001230509856260276`").AsEphemeral(true));
                    return;
                }

                if (text == null && time == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Reminder unchanged.").AsEphemeral(true));
                    return;
                }

                Reminder reminder = JsonConvert.DeserializeObject<Reminder>(await Program.db.HashGetAsync("reminders", reminderToModify));
                if (text != null)
                {
                    reminder.ReminderText = text;
                }
                if (time != null)
                {
                    reminder.ReminderTime = HumanDateParser.HumanDateParser.Parse(time);
                }

                await Program.db.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Reminder modified successfully.").AsEphemeral(true));
            }
        }
    }


    public class Reminder
    {
        [JsonProperty("userId")]
        public ulong UserId { get; set; }

        [JsonProperty("channelId")]
        public ulong ChannelId { get; set; }

        [JsonProperty("guildId")]
        public string GuildId { get; set; }

        [JsonProperty("messageId")]
        public ulong MessageId { get; set; }

        [JsonProperty("reminderId")]
        public int ReminderId { get; set; }

        [JsonProperty("reminderText")]
        public string ReminderText { get; set; }

        [JsonProperty("reminderTime")]
        public DateTime ReminderTime { get; set; }

        [JsonProperty("setTime")]
        public DateTime SetTime { get; set; }
    }
}
