namespace MechanicalMilkshake.Commands.Owner;

[RequireAuth]
public class EvalCommands
{
    private static readonly List<string> RestrictedTerms = ["poweroff", "shutdown", "reboot", "halt"];
    public static readonly string[] EvalImports = ["System", "System.Collections.Generic", "System.Linq",
        "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.Commands",
        "DSharpPlus.Interactivity", "DSharpPlus.Entities", "Microsoft.Extensions.Logging",
        Assembly.GetExecutingAssembly().GetName().Name];

    // The idea for this command, and a lot of the code, is taken from Erisa's Lykos. References are linked below.
    // https://github.com/Erisa/Lykos/blob/5f9c17c/src/Modules/Owner.cs#L116-L144
    // https://github.com/Erisa/Lykos/blob/822e9c5/src/Modules/Helpers.cs#L36-L82
    [Command("runcommand")]
    [Description("[Authorized users only] Run a shell command on the machine the bot's running on!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]

    public static async Task RunCommand(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("command"), Description("The command to run, including any arguments.")]
        string command)
    {
        await ctx.DeferResponseAsync();

        if (RestrictedTerms.Any(command.Contains))
            if (!Program.Discord.CurrentApplication.Owners.Contains(ctx.User))
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
                return;
            }

        // hardcode protection for SSH on my instance of the bot
        if (Program.Discord.CurrentUser.Id == 863140071980924958
            && ctx.User.Id != 455432936339144705
            && command.Contains("ssh"))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
            return;
        }


        var cmdResponse = await RunCommand(command);
        string response;
        
        if (cmdResponse.Output.Length > 1947)
        {
            var hasteUploadResult = await HastebinHelpers.UploadToHastebinAsync(cmdResponse.Output);
            response = $"Finished with exit code `{cmdResponse.ExitCode}`!" +
                   $" The result was too long to post here, so it was uploaded to Hastebin here: {hasteUploadResult}";
        }
        else
        {
            response = $"Finished with exit code `{cmdResponse.ExitCode}`! Output: ```\n{HideSensitiveInfo(cmdResponse.Output)}```";
        }
        
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(response));
    }

    private static async Task<ShellCommandResponse> RunCommand(string command)
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
        var result = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();

        return new ShellCommandResponse(proc.ExitCode, HideSensitiveInfo(result));
    }

    // The idea for this command, and a lot of the code, is taken from DSharpPlus/DSharpPlus.Test. Reference linked below.
    // https://github.com/DSharpPlus/DSharpPlus/blob/3a50fb3/DSharpPlus.Test/TestBotEvalCommands.cs
    [Command("eval")]
    [Description("[Authorized users only] Evaluate C# code!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]

    public static async Task Eval(MechanicalMilkshake.SlashCommandContext ctx, [Parameter("code"), Description("The code to evaluate.")] string code)
    {
        await ctx.DeferResponseAsync();

        if (RestrictedTerms.Any(code.Contains))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
            return;
        }

        try
        {
            Globals globals = new(Program.Discord, ctx);

            var scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.WithImports(EvalImports);
            scriptOptions = scriptOptions.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                .Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

            var script = CSharpScript.Create(code, scriptOptions, typeof(Globals));
            script.Compile();
            var result = await script.RunAsync(globals).ConfigureAwait(false);

            if (result?.ReturnValue is null)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("null"));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                {
                    // Isn't null, so it has to be whitespace
                    await ctx.FollowupAsync(
                        new DiscordFollowupMessageBuilder().WithContent($"\"{result.ReturnValue}\""));
                    return;
                }
                
                // Upload to Hastebin if content is too long for Discord
                if (result.ReturnValue.ToString()!.Length > 1947)
                {
                    var hasteUploadResult = await HastebinHelpers.UploadToHastebinAsync(result.ReturnValue.ToString());

                    await ctx.FollowupAsync(
                        new DiscordFollowupMessageBuilder().WithContent(
                            $"The result was too long to post here, so it was uploaded to Hastebin here: {hasteUploadResult}"));
                    return;
                }
                
                // Respond in channel if content length within Discord character limit
                await ctx.FollowupAsync(
                    new DiscordFollowupMessageBuilder().WithContent(HideSensitiveInfo(result.ReturnValue.ToString())));
            }
        }
        catch (Exception e)
        {
            await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder().WithContent(e.GetType() + ": " + e.Message));
        }
    }

    private static string HideSensitiveInfo(string input)
    {
        const string redacted = "[redacted]";
        var output = input.Replace(Program.ConfigJson.BotToken, redacted);
        if (Program.ConfigJson.WolframAlphaAppId != "")
            output = output.Replace(Program.ConfigJson.WolframAlphaAppId, redacted);

        return output;
    }
}

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
public class Globals
{
    public Globals(DiscordClient client, MechanicalMilkshake.SlashCommandContext ctx)
    {
        Context = ctx;
        Client = client;
        Channel = ctx.Channel;
        Guild = ctx.Guild;
        User = ctx.User;
        if (Guild is not null) Member = Guild.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public DiscordClient Client { get; set; }
    public DiscordMessage Message { get; set; }
    public DiscordChannel Channel { get; set; }
    public DiscordGuild Guild { get; set; }
    public DiscordUser User { get; set; }
    public DiscordMember Member { get; set; }
    public MechanicalMilkshake.SlashCommandContext Context { get; set; }
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
