namespace MechanicalMilkshake.Modules
{
    public class Owner : ApplicationCommandModule
    {
        [SlashCommand("tellraw", "[Authorized users only] Speak through the bot!")]
        [SlashRequireAuth]
        public async Task Tellraw(InteractionContext ctx, [Option("message", "The message to have the bot send.")] string message, [Option("channel", "The channel to send the message in.")] DiscordChannel channel = null)
        {
            DiscordChannel targetChannel;
            if (channel != null)
            {
                targetChannel = channel;
            }
            else
            {
                targetChannel = ctx.Channel;
            }

            try
            {
                await targetChannel.SendMessageAsync(message);
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("I don't have permission to send messages in that channel!").AsEphemeral(true));
                return;
            }
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"I sent your message to {targetChannel.Mention}.").AsEphemeral(true));
        }
    }
}
