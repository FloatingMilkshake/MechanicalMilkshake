using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class Admin : ApplicationCommandModule
    {
        [SlashCommand("tellraw", "**Admin-only:** Speak through the bot!")]
        [SlashRequireUserPermissions(Permissions.Administrator)]
        public async Task Tellraw(InteractionContext ctx, [Option("message", "What do you wish to say?")] string message)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(message));
        }

        [SlashCommand("restart", "**Admin-only:** Restarts the bot.")]
        [SlashRequireOwner] /* Restarting the bot should be only possible for the bot owner. */
        public async Task Restart(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var yesButton = new DiscordButtonComponent(ButtonStyle.Danger, "yes", "Yes", false, new DiscordComponentEmoji(":white_check_mark:"));
            var noButton = new DiscordButtonComponent(ButtonStyle.Danger, "no", "No", false, new DiscordComponentEmoji(":negative_squared_check_mark:"));

            string checkDockerFile = await File.ReadAllTextAsync("/proc/self/cgroup");
            if (string.IsNullOrWhiteSpace(checkDockerFile))
            {
                var sentMessage = await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The bot may not be running under Docker; this means that `!restart` will behave like `!shutdown`.\n\nAborted. Do you wish to shut the bot down?").AddComponents(yesButton, noButton));

                var waitForReaction = await Bot.InteractivityExtension.WaitForButtonAsync(sentMessage, ctx.User);

                if (waitForReaction.Result.Interaction.Data.CustomId.Equals(yesButton)) 
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Shutting down.."));
                    Environment.Exit(1);
                }
            }
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Restarting.."));
        }
    }
}
