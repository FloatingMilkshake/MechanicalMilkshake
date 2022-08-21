﻿using MechanicalMilkshake.Refs;

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

            foreach (var reminder in userReminders)
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

                output += $"`{reminder.ReminderId}`:\n"
                          + $"> {reminder.ReminderText}\n"
                          + $"[Set <t:{setTime}:R>]({reminderLink}) to go off <t:{reminderTime}:R> in {guildName}";

                if (guildName != "DMs") output += $" <#{reminder.ChannelId}>";

                output += "\n\n";
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(output).AsEphemeral());
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
            string reminderToModify,
            [Option("time", "When do you want to be reminded? Leave this blank if you don't want to change it.")]
            string time = null,
            [Option("text", "What should the reminder say? Leave this blank if you don't want to change it.")]
            string text = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            Regex idRegex = new("[0-9]+");
            if (!idRegex.IsMatch(reminderToModify))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "The reminder ID you provided isn't correct! You can get a reminder ID with `/reminder list`. It should look something like this: `1001230509856260276`")
                    .AsEphemeral());
                return;
            }

            if (text == null && time == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Reminder unchanged.")
                    .AsEphemeral());
                return;
            }

            var reminder =
                JsonConvert.DeserializeObject<Reminder>(
                    await Program.db.HashGetAsync("reminders", reminderToModify));
            if (text != null) reminder.ReminderText = text;

            if (time != null) reminder.ReminderTime = HumanDateParser.HumanDateParser.Parse(time);

            await Program.db.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));

            var reminderChannel = await Program.discord.GetChannelAsync(reminder.ChannelId);
            var reminderMessage = await reminderChannel.GetMessageAsync(reminder.MessageId);

            var unixTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();
            await reminderMessage.ModifyAsync($"Reminder set for <t:{unixTime}:F> (<t:{unixTime}:R>)!");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Reminder modified successfully.").AsEphemeral());
        }
    }
}