namespace MechanicalMilkshake.Commands.Owner;

public class Tellraw : ApplicationCommandModule
{
    [SlashCommand("tellraw", "[Authorized users only] Speak through the bot!")]
    [SlashRequireAuth]
    public static async Task TellrawCommand(InteractionContext ctx,
        [Option("message", "The message to have the bot send.")] [MaximumLength(2000)]
        string message,
        [Option("channel", "The channel to send the message in.")]
        DiscordChannel channel = null)
    {
        DiscordChannel targetChannel;
        if (channel != null)
            targetChannel = channel;
        else
            targetChannel = ctx.Channel;

        DiscordMessage sentMessage;
        try
        {
            sentMessage = await targetChannel.SendMessageAsync(message);
        }
        catch (UnauthorizedException)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent("I don't have permission to send messages in that channel!").AsEphemeral());
            return;
        }
        catch (Exception ex)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"I couldn't send that message!\n> {ex.GetType()}: {ex.Message}").AsEphemeral());
            return;
        }

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"I sent your message to {targetChannel.Mention}.").AsEphemeral());

        foreach (var owner in Program.discord.CurrentApplication.Owners)
        {
            if (owner.Id == ctx.User.Id)
                return;

            try
            {
                var member = await (await Program.discord.GetGuildAsync(Program.configjson.Base.HomeServerId))
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