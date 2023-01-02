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
        var targetChannel = channel ?? ctx.Channel;

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