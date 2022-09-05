namespace MechanicalMilkshake.Commands.Owner;

[SlashRequireAuth]
public class EvalCommands : ApplicationCommandModule
{
    // The idea for this command, and a lot of the code, is taken from Erisa's Lykos. References are linked below.
    // https://github.com/Erisa/Lykos/blob/5f9c17c/src/Modules/Owner.cs#L116-L144
    // https://github.com/Erisa/Lykos/blob/822e9c5/src/Modules/Helpers.cs#L36-L82
    [SlashCommand("runcommand", "[Authorized users only] Run a shell command on the machine the bot's running on!")]
    public async Task RunCommand(InteractionContext ctx,
        [Option("command", "The command to run, including any arguments.")]
        string command)
    {
        if (!Program.configjson.AuthorizedUsers.Contains(ctx.User.Id.ToString()))
            throw new SlashExecutionChecksFailedException();

        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder());
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(await RunCommand(command)));
    }

    public async Task<string> RunCommand(string command)
    {
        var osDescription = RuntimeInformation.OSDescription;
        string fileName;
        string args;
        var escapedArgs = command.Replace("\"", "\\\"");

        if (osDescription.Contains("Windows"))
        {
            fileName = "C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe";
            args = $"-Command \"{escapedArgs} 2>&1\"";
        }
        else
        {
            // Assume Linux if OS is not Windows because I'm too lazy to bother with specific checks right now, might implement that later
            fileName = Environment.GetEnvironmentVariable("SHELL");
            if (!File.Exists(fileName)) fileName = "/bin/sh";

            args = $"-c \"{escapedArgs} 2>&1\"";
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
        var result = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        if (result.Length > 1947)
        {
            Console.WriteLine(result);
            return
                $"Finished with exit code `{proc.ExitCode}`! It was too long to send in a message though; see the console for the full output.";
        }

        return $"Finished with exit code `{proc.ExitCode}`! Output: ```\n{result}```";
    }

    // The idea for this command, and a lot of the code, is taken from DSharpPlus/DSharpPlus.Test. Reference linked below.
    // https://github.com/DSharpPlus/DSharpPlus/blob/3a50fb3/DSharpPlus.Test/TestBotEvalCommands.cs
    [SlashCommand("eval", "[Authorized users only] Evaluate C# code!")]
    public async Task Eval(InteractionContext ctx, [Option("code", "The code to evaluate.")] string code)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder());

        try
        {
            Globals globals = new(ctx.Client, ctx);

            var scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.WithImports("System", "System.Collections.Generic", "System.Linq",
                "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.SlashCommands",
                "DSharpPlus.Interactivity", "DSharpPlus.Entities", "Microsoft.Extensions.Logging");
            scriptOptions = scriptOptions.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                .Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

            var script = CSharpScript.Create(code, scriptOptions, typeof(Globals));
            script.Compile();
            var result = await script.RunAsync(globals).ConfigureAwait(false);

            if (result == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("null"));
            }
            else
            {
                if (result.ReturnValue == null)
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

                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent(result.ReturnValue.ToString()));
                }
            }
        }
        catch (Exception e)
        {
            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent(e.GetType() + ": " + e.Message));
        }
    }
}