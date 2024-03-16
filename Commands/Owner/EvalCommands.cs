namespace MechanicalMilkshake.Commands.Owner;

[SlashRequireAuth]
public class EvalCommands : ApplicationCommandModule
{
    private static readonly List<string> RestrictedTerms = ["poweroff", "shutdown", "reboot", "halt"];

    // The idea for this command, and a lot of the code, is taken from Erisa's Lykos. References are linked below.
    // https://github.com/Erisa/Lykos/blob/5f9c17c/src/Modules/Owner.cs#L116-L144
    // https://github.com/Erisa/Lykos/blob/822e9c5/src/Modules/Helpers.cs#L36-L82
    [SlashCommand("runcommand", "[Authorized users only] Run a shell command on the machine the bot's running on!")]
    public static async Task RunCommand(InteractionContext ctx,
        [Option("command", "The command to run, including any arguments.")]
        string command)
    {
        if (!Program.ConfigJson.Base.AuthorizedUsers.Contains(ctx.User.Id.ToString()))
            throw new SlashExecutionChecksFailedException();

        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder());

        if (RestrictedTerms.Any(command.Contains))
            if (!Program.Discord.CurrentApplication.Owners.Contains(ctx.User))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
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
        
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response));
    }

    public static async Task<ShellCommandResponse> RunCommand(string command)
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
    [SlashCommand("eval", "[Authorized users only] Evaluate C# code!")]
    public static async Task Eval(InteractionContext ctx, [Option("code", "The code to evaluate.")] string code)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder());

        if (RestrictedTerms.Any(code.Contains))
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
            return;
        }

        try
        {
            Globals globals = new(Program.Discord, ctx);

            var scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.WithImports("System", "System.Collections.Generic", "System.Linq",
                "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.SlashCommands",
                "DSharpPlus.Interactivity", "DSharpPlus.Entities", "Microsoft.Extensions.Logging",
                Assembly.GetExecutingAssembly().GetName().Name);
            scriptOptions = scriptOptions.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                .Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

            var script = CSharpScript.Create(code, scriptOptions, typeof(Globals));
            script.Compile();
            var result = await script.RunAsync(globals).ConfigureAwait(false);

            if (result?.ReturnValue is null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("null"));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                {
                    // Isn't null, so it has to be whitespace
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent($"\"{result.ReturnValue}\""));
                    return;
                }
                
                // Upload to Hastebin if content is too long for Discord
                if (result.ReturnValue.ToString()!.Length > 1947)
                {
                    var hasteUploadResult = await HastebinHelpers.UploadToHastebinAsync(result.ReturnValue.ToString());

                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent(
                            $"The result was too long to post here, so it was uploaded to Hastebin here: {hasteUploadResult}"));
                    return;
                }
                
                // Respond in channel if content length within Discord character limit
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(HideSensitiveInfo(result.ReturnValue.ToString())));
            }
        }
        catch (Exception e)
        {
            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent(e.GetType() + ": " + e.Message));
        }
    }

    private static string HideSensitiveInfo(string input)
    {
        const string redacted = "[redacted]";
        var output = input.Replace(Program.ConfigJson.Base.BotToken, redacted);
        if (Program.ConfigJson.Base.WolframAlphaAppId != "")
            output = output.Replace(Program.ConfigJson.Base.WolframAlphaAppId, redacted);
        if (Program.ConfigJson.WorkerLinks.Secret != "")
            output = output.Replace(Program.ConfigJson.WorkerLinks.Secret, redacted);
        if (Program.ConfigJson.WorkerLinks.ApiKey != "")
            output = output.Replace(Program.ConfigJson.WorkerLinks.ApiKey, redacted);
        if (Program.ConfigJson.S3.AccessKey != "")
            output = output.Replace(Program.ConfigJson.S3.AccessKey, redacted);
        if (Program.ConfigJson.S3.SecretKey != "")
            output = output.Replace(Program.ConfigJson.S3.SecretKey, redacted);
        if (Program.ConfigJson.Cloudflare.Token != "")
            output = output.Replace(Program.ConfigJson.Cloudflare.Token, redacted);

        return output;
    }
}