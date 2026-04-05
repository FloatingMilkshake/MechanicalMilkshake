namespace MechanicalMilkshake.Commands.OwnerCommands;

[RequireAuth]
public class EvalCommands
{
    private static readonly List<string> RestrictedTerms = ["poweroff", "shutdown", "reboot", "halt"];
    public static readonly string[] EvalImports = ["System", "System.Collections.Generic", "System.Linq",
        "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.Commands",
        "DSharpPlus.Interactivity", "DSharpPlus.Entities", "Microsoft.Extensions.Logging",
        Assembly.GetExecutingAssembly().GetName().Name];

    public static readonly Dictionary<ulong, CancellationTokenSource> Cancellations = new();

    // The idea for this command, and a lot of the code, is taken from Erisa's Lykos. References are linked below.
    // https://github.com/Erisa/Lykos/blob/5f9c17c/src/Modules/Owner.cs#L116-L144
    // https://github.com/Erisa/Lykos/blob/822e9c5/src/Modules/Helpers.cs#L36-L82
    [Command("runcommand")]
    [Description("[Authorized users only] Run a shell command on the machine the bot's running on!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]

    public static async Task RunCommand(SlashCommandContext ctx,
        [Parameter("command"), Description("The command to run, including any arguments.")]
        string command)
    {
        await ctx.RespondAsync(new DiscordMessageBuilder().WithContent("Working on it...")
            .AddActionRowComponent(new DiscordActionRowComponent(
                [new DiscordButtonComponent(DiscordButtonStyle.Danger, "eval-cancel-button", "Cancel")]
            ))
        );

        if (RestrictedTerms.Any(command.Contains))
            if (!Program.Discord.CurrentApplication.Owners.Contains(ctx.User))
            {
                await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
                return;
            }

        // hardcode protection for SSH on my instance of the bot
        if (Program.Discord.CurrentUser.Id == 863140071980924958
            && ctx.User.Id != 455432936339144705
            && command.Contains("ssh"))
        {
            await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
            return;
        }

        var msg = await ctx.GetResponseAsync();
        Cancellations.Add(msg.Id, new CancellationTokenSource());
        var cancellationToken = Cancellations[msg.Id].Token;

        var cmdResponse = await RunCommand(command, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            await msg.ModifyAsync(new DiscordMessageBuilder().WithContent("The operation was cancelled."));
            Cancellations.Remove(msg.Id);
            return;
        }

        var splitOutput = await StringHelpers.SplitStringAsync($"```\n{cmdResponse.Output}\n{cmdResponse.Error}\n```");

        foreach (var part in splitOutput)
        {
            await ctx.Channel.SendMessageAsync(part);
        }
        await msg.ModifyAsync(new DiscordMessageBuilder().WithContent($"\nFinished with exit code `{cmdResponse.ExitCode}`."));

        Cancellations.Remove(msg.Id);
    }

    private static async Task<ShellCommandResponse> RunCommand(string command, CancellationToken cancellationToken)
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

        return new ShellCommandResponse(proc.ExitCode, HideSensitiveInfo(result));
    }

    // The idea for this command, and a lot of the code, is taken from DSharpPlus/DSharpPlus.Test. Reference linked below.
    // https://github.com/DSharpPlus/DSharpPlus/blob/3a50fb3/DSharpPlus.Test/TestBotEvalCommands.cs
    [Command("eval")]
    [Description("[Authorized users only] Evaluate C# code!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]

    public static async Task Eval(SlashCommandContext ctx, [Parameter("code"), Description("The code to evaluate.")] string code)
    {
        CancellationToken cancellationToken = default;

        var builder = new DiscordMessageBuilder().WithContent("Working on it...");

        await ctx.RespondAsync(builder);
        var msg = await ctx.GetResponseAsync();

        if (RestrictedTerms.Any(code.Contains))
        {
            await msg.ModifyAsync(new DiscordMessageBuilder().WithContent("You can't do that."));
            return;
        }

        try
        {
            var scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.WithImports(EvalImports);
            scriptOptions = scriptOptions.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                .Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

            var script = CSharpScript.Create(code, scriptOptions, typeof(Globals));

            // Only offer the option to cancel if the code being evaluated supports it.
            if (code.Contains("CToken"))
            {
                builder.AddActionRowComponent(new DiscordActionRowComponent(
                    [new DiscordButtonComponent(DiscordButtonStyle.Danger, "eval-cancel-button", "Cancel")]
                ));

                Cancellations.Add(msg.Id, new CancellationTokenSource());
                cancellationToken = Cancellations[msg.Id].Token;

                await msg.ModifyAsync(builder);
            }

            var result = await script.RunAsync(new Globals(Program.Discord, ctx, cancellationToken), cancellationToken).ConfigureAwait(false);

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

        Cancellations.Remove(msg.Id);
    }

    private static string HideSensitiveInfo(string input)
    {
        const string redacted = "[redacted]";
        var output = input.Replace(Program.ConfigJson.BotToken, redacted);
        if (!string.IsNullOrWhiteSpace(Program.ConfigJson.WolframAlphaAppId))
            output = output.Replace(Program.ConfigJson.WolframAlphaAppId, redacted);
        if (!string.IsNullOrWhiteSpace(Program.ConfigJson.DbotsApiToken))
            output = output.Replace(Program.ConfigJson.DbotsApiToken, redacted);

        return output;
    }
}

public class Globals
{
    public Globals(DiscordClient client, SlashCommandContext ctx, CancellationToken ctoken)
    {
        Context = ctx;
        Client = client;
        Channel = ctx.Channel;
        Guild = ctx.Guild;
        User = ctx.User;
        if (Guild is not null) Member = Guild.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();
        CToken = ctoken;
    }

    public DiscordClient Client { get; set; }
    public DiscordMessage Message { get; set; }
    public DiscordChannel Channel { get; set; }
    public DiscordGuild Guild { get; set; }
    public DiscordUser User { get; set; }
    public DiscordMember Member { get; set; }
    public SlashCommandContext Context { get; set; }

    public CancellationToken CToken { get; set; }
}

internal class ShellCommandResponse
{
    public ShellCommandResponse(int exitCode, string output, string error)
    {
        ExitCode = exitCode;
        Output = output;
        Error = error;
    }

    public ShellCommandResponse(int exitCode, string output)
    {
        ExitCode = exitCode;
        Output = output;
        Error = default;
    }

    public ShellCommandResponse()
    {
        ExitCode = default;
        Output = default;
        Error = default;
    }

    public int ExitCode { get; set; }
    public string Output { get; set; }
    public string Error { get; set; }
}
