namespace MechanicalMilkshake.Events;

public class InteractionEvents
{
    public static async Task SlashCommandExecuted(SlashCommandsExtension _, SlashCommandExecutedEventArgs e)
    {
        await LogCmdUsage(e.Context);
    }

    public static async Task ContextMenuExecuted(SlashCommandsExtension _, ContextMenuExecutedEventArgs e)
    {
        await LogCmdUsage(e.Context);
    }
    
    private static async Task LogCmdUsage(BaseContext context)
    {
        // Ignore home server, excluded servers, and authorized users
        if (context.Guild.Id == Program.HomeServer.Id ||
            Program.ConfigJson.Logs.SlashCommands.CmdLogExcludedGuilds.Contains(context.Guild.Id.ToString()) ||
            Program.ConfigJson.Base.AuthorizedUsers.Contains(context.User.Id.ToString()))
            return;

        // Increment count
        if (await Program.Db.HashExistsAsync("commandCounts", context.CommandName))
            await Program.Db.HashIncrementAsync("commandCounts", context.CommandName);
        else
            await Program.Db.HashSetAsync("commandCounts", context.CommandName, 1);

        // Log to log channel if configured
        if (Program.ConfigJson.Logs.SlashCommands.LogChannel is not null)
        {
            var embed = new DiscordEmbedBuilder()
                .WithColor(Program.BotColor)
                .WithAuthor(context.User.Username, null, context.User.AvatarUrl)
                .WithDescription(
                    $"{context.User.Mention} used {SlashCmdMentionHelpers.GetSlashCmdMention(context.CommandName)} in {context.Channel.Mention} (`{context.Channel.Id}`) in \"{context.Guild.Name}\" (`{context.Guild.Id}`)!")
                .WithTimestamp(DateTime.Now);

            try
            {
                await (await context.Client.GetChannelAsync(
                    Convert.ToUInt64(Program.ConfigJson.Logs.SlashCommands.LogChannel))).SendMessageAsync(embed);
            }
            catch (Exception ex) when (ex is UnauthorizedException or NotFoundException)
            {
                Program.Discord.Logger.LogError(Program.BotEventId,
                    "{user} used {command} in {guild} but it could not be logged because the log channel cannot be accessed.",
                    context.User.Id, context.CommandName, context.Guild.Id);
            }
            catch (FormatException)
            {
                Program.Discord.Logger.LogError(Program.BotEventId,
                    "{user} used {command} in {guild} but it could not be logged because the log channel ID is invalid.",
                    context.User.Id, context.CommandName, context.Guild.Id);
            }
        }
    }
}