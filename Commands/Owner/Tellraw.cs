namespace MechanicalMilkshake.Commands.Owner
{
    public class Tellraw : ApplicationCommandModule
    {
        [SlashCommand("tellraw", "[Authorized users only] Speak through the bot!")]
        [SlashRequireAuth]
        public async Task TellrawCommand(InteractionContext ctx,
            [Option("message", "The message to have the bot send.")]
            string message,
            [Option("channel", "The channel to send the message in.")]
            DiscordChannel channel = null)
        {
            DiscordChannel targetChannel;
            if (channel != null)
                targetChannel = channel;
            else
                targetChannel = ctx.Channel;

            try
            {
                await targetChannel.SendMessageAsync(message);
            }
            catch (UnauthorizedException)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                    .WithContent("I don't have permission to send messages in that channel!").AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"I sent your message to {targetChannel.Mention}.").AsEphemeral());
        }
    }
}
