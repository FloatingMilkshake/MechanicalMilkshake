using DSharpPlus.Commands.EventArgs;

namespace MechanicalMilkshake.Events;

public class InteractionEvents
{
    public static async Task CommandExecuted(CommandsExtension _, CommandExecutedEventArgs e)
    {
        await LogCmdUsage(e.Context);
    }

    private static async Task LogCmdUsage(CommandContext context)
    {
        try
        {
            // Ignore home server, excluded servers, and authorized users
            if (context.Guild is not null && (context.Guild.Id == Program.HomeServer.Id || context.Guild.Id == 1342179809618559026 ||
                Program.ConfigJson.SlashCommandLogExcludedGuilds.Contains(context.Guild.Id.ToString())) ||
                Program.ConfigJson.BotCommanders.Contains(context.User.Id.ToString()))
                return;

            // Increment count
            if (await Program.Db.HashExistsAsync("commandCounts", context.Command.FullName))
                await Program.Db.HashIncrementAsync("commandCounts", context.Command.FullName);
            else
                await Program.Db.HashSetAsync("commandCounts", context.Command.FullName, 1);

            // Log to log channel if configured
            if (Program.ConfigJson.SlashCommandLogChannel is not null)
            {
                var description = context.Channel.IsPrivate
                    ? $"{context.User.Username} (`{context.User.Id}`) used {SlashCmdMentionHelpers.GetSlashCmdMention(context.Command.FullName)} in DMs."
                    : $"{context.User.Username} (`{context.User.Id}`) used {SlashCmdMentionHelpers.GetSlashCmdMention(context.Command.FullName)} in `{context.Channel.Name}` (`{context.Channel.Id}`) in \"{context.Guild.Name}\" (`{context.Guild.Id}`).";
                
                var embed = new DiscordEmbedBuilder()
                    .WithColor(Program.BotColor)
                    .WithAuthor(context.User.Username, null, context.User.AvatarUrl)
                    .WithDescription(description)
                    .WithTimestamp(DateTime.Now);

                try
                {
                    await (await context.Client.GetChannelAsync(
                        Convert.ToUInt64(Program.ConfigJson.SlashCommandLogChannel))).SendMessageAsync(embed);
                }
                catch (Exception ex) when (ex is UnauthorizedException or NotFoundException)
                {
                    Program.Discord.Logger.LogError(Program.BotEventId,
                        "{User} used {Command} in {Guild} but it could not be logged because the log channel cannot be accessed",
                        context.User.Id, context.Command.FullName, context.Guild.Id);
                }
                catch (FormatException)
                {
                    Program.Discord.Logger.LogError(Program.BotEventId,
                        "{User} used {Command} in {Guild} but it could not be logged because the log channel ID is invalid",
                        context.User.Id, context.Command.FullName, context.Guild.Id);
                }
            }
        }
        catch (Exception ex)
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = "An exception was thrown when logging a slash command",
                Description =
                    $"An exception was thrown when {context.User.Mention} used `/{context.Command.FullName}`. Details are below.",
                Color = DiscordColor.Red
            };
            embed.AddField("Exception Details",
                $"```{ex.GetType()}: {ex.Message}:\n{ex.StackTrace}".Truncate(1020) + "\n```");
            
            await Program.HomeChannel.SendMessageAsync(embed);
        }
        
    }
}