namespace MechanicalMilkshake.Commands;

[RequireBotCommander]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
internal class EvalCommands
{
    // The idea for this command, and a lot of the code, is taken from Erisa's Lykos. References are linked below.
    // https://github.com/Erisa/Lykos/blob/5f9c17c/src/Modules/Owner.cs#L116-L144
    // https://github.com/Erisa/Lykos/blob/822e9c5/src/Modules/Helpers.cs#L36-L82
    [Command("shellcommand")]
    [Description("[Authorized users only] Run a shell command on the machine the bot's running on!")]
    public static async Task ShellCommandCommandAsync(SlashCommandContext ctx,
        [Parameter("command"), Description("The command to run, including any arguments.")]
        string command)
    {
        await ctx.RespondAsync(new DiscordMessageBuilder().WithContent("Working on it...")
            .AddActionRowComponent(new DiscordActionRowComponent(
                [new DiscordButtonComponent(DiscordButtonStyle.Danger, "button-callback-eval-cancel", "Cancel")]
            ))
        );

        if (Setup.Eval.RestrictedTerms.Any(command.Contains))
            if (!Setup.State.Discord.Client.CurrentApplication.Owners.Contains(ctx.User))
            {
                await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
                return;
            }

        // hardcode protection for SSH on my instance of the bot
        if (Setup.State.Discord.Client.CurrentUser.Id == 863140071980924958
            && ctx.User.Id != 455432936339144705
            && command.Contains("ssh"))
        {
            await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
            return;
        }

        var msg = await ctx.GetResponseAsync();
        Setup.State.Caches.CancellationTokens.Add(msg.Id, new CancellationTokenSource());
        var cancellationToken = Setup.State.Caches.CancellationTokens[msg.Id].Token;

        var cmdResponse = await RunShellCommandAsync(command, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            await msg.ModifyAsync(new DiscordMessageBuilder().WithContent("The operation was cancelled."));
            Setup.State.Caches.CancellationTokens.Remove(msg.Id);
            return;
        }

        var splitOutput = await StringHelpers.SplitStringAsync($"```\n{cmdResponse.Output}\n{cmdResponse.Error}\n```");

        foreach (var part in splitOutput)
        {
            await ctx.Channel.SendMessageAsync(part);
        }
        await msg.ModifyAsync(new DiscordMessageBuilder().WithContent($"\nFinished with exit code `{cmdResponse.ExitCode}`."));

        Setup.State.Caches.CancellationTokens.Remove(msg.Id);
    }

    // The idea for this command, and a lot of the code, is taken from DSharpPlus/DSharpPlus.Test. Reference linked below.
    // https://github.com/DSharpPlus/DSharpPlus/blob/3a50fb3/DSharpPlus.Test/TestBotEvalCommands.cs
    [Command("eval")]
    [Description("[Authorized users only] Evaluate C# code!")]
    public static async Task EvalCommandAsync(SlashCommandContext ctx, [Parameter("code"), Description("The code to evaluate.")] string code)
    {
        CancellationToken cancellationToken = default;

        var builder = new DiscordMessageBuilder().WithContent("Working on it...");

        await ctx.RespondAsync(builder);
        var msg = await ctx.GetResponseAsync();

        if (Setup.Eval.RestrictedTerms.Any(code.Contains))
        {
            await msg.ModifyAsync(new DiscordMessageBuilder().WithContent("You can't do that."));
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

                await msg.ModifyAsync(builder);
            }

            var result = await script.RunAsync(new Setup.Eval.Globals(Setup.State.Discord.Client, ctx, cancellationToken), cancellationToken).ConfigureAwait(false);

            if (result?.ReturnValue is null)
            {
                await msg.ModifyAsync(new DiscordMessageBuilder().WithContent("null"));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                {
                    // Isn't null, so it has to be whitespace
                    await msg.ModifyAsync(new DiscordMessageBuilder().WithContent($"\"{result.ReturnValue}\""));
                    return;
                }

                var splitOutput = await StringHelpers.SplitStringAsync(HideSensitiveInfo(result.ReturnValue.ToString()));

                foreach (var part in splitOutput)
                {
                    await ctx.Channel.SendMessageAsync(part);
                }

                if (cancellationToken.IsCancellationRequested)
                    await msg.ModifyAsync(new DiscordMessageBuilder().WithContent("The operation was cancelled."));
                else
                    await msg.ModifyAsync(new DiscordMessageBuilder().WithContent("Done!"));
            }
        }
        catch (Exception e)
        {
            if (cancellationToken.IsCancellationRequested)
                await msg.ModifyAsync(new DiscordMessageBuilder().WithContent("The operation was cancelled."));
            else
                await msg.ModifyAsync(new DiscordMessageBuilder().WithContent(e.GetType() + ": " + e.Message));
        }

        Setup.State.Caches.CancellationTokens.Remove(msg.Id);
    }

    private static async Task<Setup.Types.ShellCommandResponse> RunShellCommandAsync(string command, CancellationToken cancellationToken)
    {
        var osDescription = RuntimeInformation.OSDescription;
        string fileName;
        string args;
        var escapedArgs = command.Replace("\"", "\\\"");

        if (osDescription.Contains("Windows"))
        {
            fileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
            args = $"-Command \"{escapedArgs} 2>&1\"";
        }
        else
        {
            // Assume Linux if OS is not Windows because I'm too lazy to bother with specific checks right now, might implement that later
            fileName = Environment.GetEnvironmentVariable("SHELL");
            if (!File.Exists(fileName)) fileName = "/bin/sh";

            args = $"-c \"{escapedArgs}\"";
        }

        Process proc = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            }
        };

        proc.Start();
        string result;
        try
        {
            result = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
            await proc.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            result = "The operation was cancelled.";
        }
        if (cancellationToken.IsCancellationRequested)
            proc.Kill();

        return new Setup.Types.ShellCommandResponse(proc.ExitCode, HideSensitiveInfo(result));
    }

    private static string HideSensitiveInfo(string input)
    {
        const string redacted = "[redacted]";
        var output = input.Replace(Setup.Configuration.ConfigJson.BotToken, redacted);
        if (!string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.WolframAlphaAppId))
            output = output.Replace(Setup.Configuration.ConfigJson.WolframAlphaAppId, redacted);
        if (!string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.DbotsApiToken))
            output = output.Replace(Setup.Configuration.ConfigJson.DbotsApiToken, redacted);

        return output;
    }
}
