namespace MechanicalMilkshake.Commands;

[RequireBotCommander]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
internal class EvalCommands
{
    // The idea for this command, and a lot of the code, is taken from DSharpPlus/DSharpPlus.Test. Reference linked below.
    // https://github.com/DSharpPlus/DSharpPlus/blob/3a50fb3/DSharpPlus.Test/TestBotEvalCommands.cs
    [Command("eval")]
    [Description("[Authorized users only] Evaluate C# code!")]
    public static async Task EvalCommandAsync(SlashCommandContext ctx, [Parameter("code"), Description("The code to evaluate.")] string code)
    {
        CancellationToken cancellationToken = default;

        var builder = new DiscordMessageBuilder().WithContent("Working on it...");

        await ctx.RespondAsync(new DiscordInteractionResponseBuilder(builder)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        var msg = await ctx.GetResponseAsync();

        if (Setup.Eval.RestrictedTerms.Any(code.Contains))
        {
            await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("You can't do that."));
            return;
        }

        try
        {
            var scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.WithImports(Setup.Eval.Imports);
            scriptOptions = scriptOptions.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                .Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

            var script = CSharpScript.Create(code, scriptOptions, typeof(Setup.Eval.Globals));

            // Only offer the option to cancel if the code being evaluated supports it.
            if (code.Contains("CToken"))
            {
                builder.AddActionRowComponent(new DiscordActionRowComponent(
                    [new DiscordButtonComponent(DiscordButtonStyle.Danger, "button-callback-eval-cancel", "Cancel")]
                ));

                Setup.State.Caches.CancellationTokens.Add(msg.Id, new CancellationTokenSource());
                cancellationToken = Setup.State.Caches.CancellationTokens[msg.Id].Token;

                await ctx.EditResponseAsync(builder);
            }

            var result = await script.RunAsync(new Setup.Eval.Globals(Setup.State.Discord.Client, ctx, cancellationToken), cancellationToken).ConfigureAwait(false);

            if (result?.ReturnValue is null)
            {
                await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("null"));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                {
                    // Isn't null, so it has to be whitespace
                    await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent($"\"{result.ReturnValue}\""));
                    return;
                }

                var splitOutput = result.ReturnValue.ToString().SplitForDiscord();

                foreach (var part in splitOutput)
                {
                    await ctx.Channel.SendMessageAsync(part);
                }

                if (cancellationToken.IsCancellationRequested)
                    await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("The operation was cancelled."));
                else
                    await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("Done!"));
            }
        }
        catch (Exception e)
        {
            if (cancellationToken.IsCancellationRequested)
                await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("The operation was cancelled."));
            else
                await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent(e.GetType() + ": " + e.Message));
        }

        Setup.State.Caches.CancellationTokens.Remove(msg.Id);
    }
}
