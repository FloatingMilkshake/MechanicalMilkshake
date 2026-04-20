namespace MechanicalMilkshake.Commands;

internal class ShellCommands
{
    // The idea for this command, and a lot of the code, is taken from Erisa's Lykos. References are linked below.
    // https://github.com/Erisa/Lykos/blob/5f9c17c/src/Modules/Owner.cs#L116-L144
    // https://github.com/Erisa/Lykos/blob/822e9c5/src/Modules/Helpers.cs#L36-L82
    [Command("shell")]
    [Description("[Authorized users only] Run a shell command on the machine the bot's running on!")]
    public static async Task ShellCommandCommandAsync(SlashCommandContext ctx,
        [Parameter("command"), Description("The command to run, including any arguments.")]
        string command)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent("Working on it...")
            .AddActionRowComponent(new DiscordActionRowComponent(
                [new DiscordButtonComponent(DiscordButtonStyle.Danger, "button-callback-eval-cancel", "Cancel")]
            ))
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false))
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
            await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("The operation was cancelled."));
            Setup.State.Caches.CancellationTokens.Remove(msg.Id);
            return;
        }

        var splitOutput = $"```\n{cmdResponse.Output}\n{cmdResponse.Error}\n```".SplitForDiscord();

        foreach (var part in splitOutput)
        {
            await ctx.Channel.SendMessageAsync(part);
        }
        await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent($"\nFinished with exit code `{cmdResponse.ExitCode}`."));

        Setup.State.Caches.CancellationTokens.Remove(msg.Id);
    }

    private static async Task<ShellCommandResult> RunShellCommandAsync(string command, CancellationToken cancellationToken)
    {
        var osDescription = RuntimeInformation.OSDescription;
        string fileName;
        string args;
        var escapedArgs = command.Replace("\"", "\\\"");

        if (osDescription.Contains("Windows"))
        {
            fileName = @"C:\Program Files\PowerShell\7\pwsh.exe";
            args = $"-Command \"$PSStyle.OutputRendering = [System.Management.Automation.OutputRendering]::PlainText ; {escapedArgs} 2>&1\"";
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

        // Wait a bit for the process to be killed
        await Task.Delay(5000, CancellationToken.None);

        return new ShellCommandResult(proc.ExitCode, HideSensitiveInfo(result));
    }

    private static string HideSensitiveInfo(string input)
    {
        const string redacted = "[redacted]";
        var output = input.Replace(Setup.State.Process.Configuration.BotToken, redacted);
        if (!string.IsNullOrWhiteSpace(Setup.State.Process.Configuration.WolframAlphaAppId))
            output = output.Replace(Setup.State.Process.Configuration.WolframAlphaAppId, redacted);
        if (!string.IsNullOrWhiteSpace(Setup.State.Process.Configuration.DbotsApiToken))
            output = output.Replace(Setup.State.Process.Configuration.DbotsApiToken, redacted);

        return output;
    }

    private class ShellCommandResult
    {
        internal ShellCommandResult(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }

        internal ShellCommandResult(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
            Error = default;
        }

        internal int ExitCode { get; private set; }
        internal string Output { get; private set; }
        internal string Error { get; private set; }
    }
}
