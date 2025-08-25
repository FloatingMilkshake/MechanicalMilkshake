namespace MechanicalMilkshake.Commands.Owner;

public class Tellraw
{
    [Command("tellraw")]
    [Description("???")]
    [RequireAuth]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild)]
    public static async Task TellrawCommand(SlashCommandContext ctx,
        [Parameter("message"), Description("!!!")] [MinMaxLength(maxLength: 2000)]
        string message,
        [Parameter("channel"), Description("~>")]
        DiscordChannel channel = null)
    {
        var targetChannel = channel ?? ctx.Channel;

        DiscordMessage sentMessage;
        try
        {
            sentMessage = await targetChannel.SendMessageAsync(message);
        }
        catch (UnauthorizedException)
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent("not allowed!").AsEphemeral());
            return;
        }
        catch (Exception ex)
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"failed!\n> {ex.GetType()}: {ex.Message}").AsEphemeral());
            return;
        }

        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"OK.").AsEphemeral());

        foreach (var owner in Program.Discord.CurrentApplication.Owners)
        {
            // Don't send tellraw log to owners if an owner sent the message
            if (Program.Discord.CurrentApplication.Owners.Contains(ctx.User)) return;

            try
            {
                var member = await (await Program.Discord.GetGuildAsync(Program.HomeServer.Id))
                    .GetMemberAsync(owner.Id);
                await member.SendMessageAsync(
                    $"{ctx.User.Mention} used tellraw:\n> {message}\n\n{sentMessage.JumpLink}");
            }
            catch
            {
                // Do nothing
            }
        }
    }
}
