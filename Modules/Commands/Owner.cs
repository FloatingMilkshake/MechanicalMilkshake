using DSharpPlus.Exceptions;

namespace MechanicalMilkshake.Modules.Commands;

public class Owner : ApplicationCommandModule
{
    [SlashCommand("tellraw", "[Authorized users only] Speak through the bot!")]
    [SlashRequireAuth]
    public async Task Tellraw(InteractionContext ctx,
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

    [RequireAuth]
    public class DebugCmds : BaseCommandModule
    {
        [Command("restart")]
        [Description("Restart the bot if something is broken.")]
        public async Task RestartCmd(CommandContext ctx)
        {
            try
            {
                var dockerCheckFile = File.ReadAllText("/proc/self/cgroup");
                if (string.IsNullOrWhiteSpace(dockerCheckFile))
                {
                    await ctx.RespondAsync(
                        "The bot may not be running under Docker; this means that `/debug restart` will behave like `/debug shutdown`."
                        + "\n\nOperation aborted. Use `/debug shutdown` if you wish to shut down the bot.");
                    return;
                }
            }
            catch
            {
                // /proc/self/cgroup could not be found, which means the bot is not running in Docker.
                await ctx.RespondAsync(
                    "The bot may not be running under Docker; this means that `/debug restart` will behave like `/debug shutdown`.)"
                    + "\n\nOperation aborted. Use `/debug shutdown` if you wish to shut down the bot.");
                return;
            }

            await ctx.RespondAsync("Restarting...");
            Environment.Exit(1);
        }
    }

    [SlashRequireAuth]
    public class Private : ApplicationCommandModule
    {
        // The idea for this command, and a lot of the code, is taken from Erisa's Lykos. References are linked below.
        // https://github.com/Erisa/Lykos/blob/5f9c17c/src/Modules/Owner.cs#L116-L144
        // https://github.com/Erisa/Lykos/blob/822e9c5/src/Modules/Helpers.cs#L36-L82
        [SlashCommand("runcommand", "Run a shell command on the machine the bot's running on!")]
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
        [SlashCommand("eval", "Evaluate C# code!")]
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
                    "DSharpPlus.Interactivity", "Microsoft.Extensions.Logging");
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

        [SlashCommandGroup("link", "Set, update, or delete a short link with Cloudflare worker-links.")]
        public class Link
        {
            [SlashCommand("set", "Set or update a short link with Cloudflare worker-links.")]
            public async Task SetLink(InteractionContext ctx,
                [Option("key", "Set a custom key for the short link.")]
                string key,
                [Option("url", "The URL the short link should point to.")]
                string url)
            {
                if (url.Contains('<')) url = url.Replace("<", "");

                if (url.Contains('>')) url = url.Replace(">", "");

                string baseUrl;
                if (Program.configjson.WorkerLinks.BaseUrl == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            "Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set."));
                    return;
                }

                baseUrl = Program.configjson.WorkerLinks.BaseUrl;

                using HttpClient httpClient = new()
                {
                    BaseAddress = new Uri(baseUrl)
                };

                HttpRequestMessage request = null;

                if (key == "null" || key == "random" || key == "rand")
                    request = new HttpRequestMessage(HttpMethod.Post, "");
                else
                    request = new HttpRequestMessage(HttpMethod.Put, key);

                string secret;
                if (Program.configjson.WorkerLinks.Secret == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            "Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set."));
                    return;
                }

                secret = Program.configjson.WorkerLinks.Secret;

                request.Headers.Add("Authorization", secret);
                request.Headers.Add("URL", url);

                var response = await httpClient.SendAsync(request);
                var httpStatusCode = (int)response.StatusCode;
                var httpStatus = response.StatusCode.ToString();
                var responseText = await response.Content.ReadAsStringAsync();
                if (responseText.Length > 1940)
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            $"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. Think about connecting this to a pastebin-like service."));

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```"));
            }

            [SlashCommand("delete", "Delete a short link with Cloudflare worker-links.")]
            public async Task DeleteWorkerLink(InteractionContext ctx,
                [Option("link", "The key or URL of the short link to delete.")]
                string url)
            {
                string baseUrl;
                if (Program.configjson.WorkerLinks.BaseUrl == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            "Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set."));
                    return;
                }

                baseUrl = Program.configjson.WorkerLinks.BaseUrl;

                using HttpClient httpClient = new()
                {
                    BaseAddress = new Uri(baseUrl)
                };

                if (!url.Contains(baseUrl)) url = $"{baseUrl}/{url}";

                string secret;
                if (Program.configjson.WorkerLinks.Secret == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            "Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set."));
                    return;
                }

                secret = Program.configjson.WorkerLinks.Secret;

                HttpRequestMessage request = new(HttpMethod.Delete, url);
                request.Headers.Add("Authorization", secret);

                var response = await httpClient.SendAsync(request);
                var httpStatusCode = (int)response.StatusCode;
                var httpStatus = response.StatusCode.ToString();
                var responseText = await response.Content.ReadAsStringAsync();
                if (responseText.Length > 1940)
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            $"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. Think about connecting this to a pastebin-like service."));

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```"));
            }

            [SlashCommand("list", "List all short links configured with Cloudflare worker-links.")]
            public async Task ListWorkerLinks(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AsEphemeral());

                if (Program.configjson.WorkerLinks.ApiKey == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            "Error: missing Cloudflare API Key! Make sure the apiKey field under workerLinks in your config.json file is set.")
                        .AsEphemeral());
                    return;
                }

                if (Program.configjson.WorkerLinks.NamespaceId == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            "Error: missing KV Namespace ID! Make sure the namespaceId field under workerLinks in your config.json file is set.")
                        .AsEphemeral());
                    return;
                }

                if (Program.configjson.WorkerLinks.AccountId == null)
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            "Error: missing Cloudflare Account ID! Make sure the accountId field under workerLinks in your config.json file is set.")
                        .AsEphemeral());

                if (Program.configjson.WorkerLinks.Email == null)
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            "Error: missing email address for Cloudflare! Make sure the email field under workerLinks in your config.json file is set.")
                        .AsEphemeral());

                var requestUri =
                    $"https://api.cloudflare.com/client/v4/accounts/{Program.configjson.WorkerLinks.AccountId}/storage/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}/keys";
                HttpRequestMessage request = new(HttpMethod.Get, requestUri);

                request.Headers.Add("X-Auth-Key", Program.configjson.WorkerLinks.ApiKey);
                request.Headers.Add("X-Auth-Email", Program.configjson.WorkerLinks.Email);
                var response = await Program.httpClient.SendAsync(request);

                var responseText = await response.Content.ReadAsStringAsync();

                var parsedResponse = JsonConvert.DeserializeObject<CloudflareResponse>(responseText);

                var kvListResponse = "";

                foreach (var item in parsedResponse.Result)
                {
                    var key = item.Name.Replace("/", "%2F");

                    var valueRequestUri =
                        $"https://api.cloudflare.com/client/v4/accounts/{Program.configjson.WorkerLinks.AccountId}/storage/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}/values/{key}";
                    HttpRequestMessage valueRequest = new(HttpMethod.Get, valueRequestUri);

                    valueRequest.Headers.Add("X-Auth-Key", Program.configjson.WorkerLinks.ApiKey);
                    valueRequest.Headers.Add("X-Auth-Email", Program.configjson.WorkerLinks.Email);
                    var valueResponse = await Program.httpClient.SendAsync(valueRequest);

                    var value = await valueResponse.Content.ReadAsStringAsync();
                    value = value.Replace(value, $"<{value}>");
                    kvListResponse += $"`{item.Name}`: {value}\n\n";
                }

                DiscordEmbedBuilder embed = new()
                {
                    Title = "Link List"
                };
                try
                {
                    embed.Description = kvListResponse;
                }
                catch (ArgumentException ex) when (ex.Message.Contains("length cannot exceed"))
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I couldn't send the list of links here! It's too long to fit into a message. You can see the full list on Cloudflare's website [here](https://dash.cloudflare.com/{Program.configjson.WorkerLinks.AccountId}/workers/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}).")
                        .AsEphemeral());
                    return;
                }
                catch (Exception ex)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"Hmm, I couldn't send the list of links here! You can see the full list on Cloudflare's website [here](https://dash.cloudflare.com/{Program.configjson.WorkerLinks.AccountId}/workers/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}). Exception details are below.\n```\n{ex.GetType()}: {ex.Message}\n```")
                        .AsEphemeral());
                    return;
                }

                try
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral());
                }
                catch (Exception ex)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"Hmm, I couldn't send the list of links here! You can see the full list on Cloudflare's website [here](https://dash.cloudflare.com/{Program.configjson.WorkerLinks.AccountId}/workers/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}). Exception details are below.\n```\n{ex.GetType()}: {ex.Message}\n```")
                        .AsEphemeral());
                }
            }

            public class CloudflareResponse
            {
                [JsonProperty("result")] public List<KVEntry> Result { get; set; }
            }

            public class KVEntry
            {
                [JsonProperty("name")] public string Name { get; set; }
            }
        }

        [SlashCommandGroup("cdn", "Manage files uploaded to Amazon S3-compatible cloud storage.")]
        public class Cdn
        {
            [SlashCommand("upload", "Upload a file to Amazon S3-compatible cloud storage.")]
            public async Task Upload(InteractionContext ctx,
                [Option("name", "The name for the uploaded file.")]
                string name,
                [Option("link", "A link to a file to upload.")]
                string link = null,
                [Option("file", "A direct file to upload. This will override a link if both are provided!")]
                DiscordAttachment file = null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder());

                if (file != null) link = file.Url;

                if (link.Contains('<'))
                {
                    link = link.Replace("<", "");
                    link = link.Replace("<", "");
                }

                if (link.Contains('>'))
                {
                    link = link.Replace(">", "");
                    link = link.Replace(">", "");
                }

                string fileName;

                MemoryStream memStream = new(await Program.httpClient.GetByteArrayAsync(link));

                try
                {
                    Dictionary<string, string> meta = new();

                    meta["x-amz-acl"] = "public-read";

                    string bucket = null;
                    if (Program.configjson.S3.Bucket == null)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                            "Error: S3 bucket info missing! Make sure the bucket field under s3 in your config.json file is set."));
                        return;
                    }

                    bucket = Program.configjson.S3.Bucket;

                    Regex urlRemovalPattern = new(@".*\/\/.*\/");
                    var urlRemovalMatch = urlRemovalPattern.Match(link);
                    link = link.Replace(urlRemovalMatch.ToString(), "");

                    // At this point 'link' will probably look like a filename (example.png), but if a link was provided that had parameters then 'link' might look more like (example.png?someparameter=something)

                    Regex parameterRemovalPattern = new(@".*\?");
                    var parameterRemovalMatch = parameterRemovalPattern.Match(link);
                    if (parameterRemovalMatch != null && parameterRemovalMatch.ToString() != "")
                        link = parameterRemovalMatch.ToString();

                    var fileNameAndExtension = link.Replace("?", "");

                    // From here on out we can be sure that 'fileNameAndExtension' is in the format (example.png).

                    var extension = Path.GetExtension(fileNameAndExtension);

                    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

                    if (name == "random" || name == "generate")
                        fileName = new string(Enumerable.Repeat(chars, 10)
                            .Select(s => s[Program.random.Next(s.Length)]).ToArray()) + extension;
                    else if (name == "existing" || name == "preserve" || name == "keep")
                        fileName = fileNameAndExtension;
                    else
                        fileName = name + extension;

                    var args = new PutObjectArgs()
                        .WithBucket(bucket)
                        .WithObject(fileName)
                        .WithStreamData(memStream)
                        .WithObjectSize(memStream.Length)
                        .WithContentType(MimeTypeMap.GetMimeType(extension))
                        .WithHeaders(meta);

                    await Program.minio.PutObjectAsync(args);
                }
                catch (MinioException e)
                {
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent(
                            $"An API error occured while uploading!```\n{e.Message}```"));
                    return;
                }
                catch (Exception e)
                {
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent(
                            $"An unexpected error occured while uploading!```\n{e.Message}```"));
                    return;
                }

                string cdnUrl;
                if (Program.configjson.S3.CdnBaseUrl == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        $"Upload successful!\nThere's no CDN URL set in your config.json, so I can't give you a link. But your file was uploaded as {fileName}."));
                }
                else
                {
                    cdnUrl = Program.configjson.S3.CdnBaseUrl;
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent(
                            $"Upload successful!\n<{cdnUrl}/{fileName}>"));
                }
            }

            [SlashCommand("delete", "Delete a file from Amazon S3-compatible cloud storage.")]
            public async Task DeleteUpload(InteractionContext ctx,
                [Option("file", "The file to delete.")]
                string fileToDelete)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder());

                if (fileToDelete.Contains('<')) fileToDelete = fileToDelete.Replace("<", "");

                if (fileToDelete.Contains('>')) fileToDelete = fileToDelete.Replace(">", "");

                string bucket;
                if (Program.configjson.S3.Bucket == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "Error: S3 bucket info missing! Make sure the bucket field under s3 in your config.json file is set."));
                    return;
                }

                bucket = Program.configjson.S3.Bucket;

                string fileName;
                if (!fileToDelete.Contains($"{Program.configjson.S3.CdnBaseUrl}/"))
                    fileName = fileToDelete;
                else
                    fileName = fileToDelete.Replace($"{Program.configjson.S3.CdnBaseUrl}/", "");

                try
                {
                    var args = new RemoveObjectArgs()
                        .WithBucket(bucket)
                        .WithObject(fileName);

                    await Program.minio.RemoveObjectAsync(args);
                }
                catch (MinioException e)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        $"An API error occured while attempting to delete the file!```\n{e.Message}```"));
                    return;
                }
                catch (Exception e)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        $"An unexpected error occured while attempting to delete the file!```\n{e.Message}```"));
                    return;
                }

                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "File deleted successfully!\nAttempting to purge Cloudflare cache..."));

                string cloudflareUrlPrefix;
                if (Program.configjson.Cloudflare.UrlPrefix != null)
                {
                    cloudflareUrlPrefix = Program.configjson.Cloudflare.UrlPrefix;
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                        "File deleted successfully!\nError: missing Zone ID for Cloudflare. Unable to purge cache! Check the urlPrefix field under cloudflare in your config.json file."));
                    return;
                }

                // This code is (mostly) taken from https://github.com/Sankra/cloudflare-cache-purger/blob/master/main.csx#L113.
                // (Note that I originally found it here: https://github.com/Erisa/Lykos/blob/1f32e03/src/Modules/Owner.cs#L232)

                CloudflareContent content = new(new List<string> { cloudflareUrlPrefix + fileName });
                var cloudflareContentString = JsonConvert.SerializeObject(content);
                try
                {
                    using HttpClient httpClient = new()
                    {
                        BaseAddress = new Uri("https://api.cloudflare.com/")
                    };

                    string zoneId;
                    if (Program.configjson.Cloudflare.ZoneId != null)
                    {
                        zoneId = Program.configjson.Cloudflare.ZoneId;
                    }
                    else
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                            "File deleted successfully!\nError: missing Zone ID for Cloudflare. Unable to purge cache! Check the urlPrefix field under cloudflare in your config.json file."));
                        return;
                    }

                    string cloudflareToken;
                    if (Program.configjson.Cloudflare.Token != null)
                    {
                        cloudflareToken = Program.configjson.Cloudflare.Token;
                    }
                    else
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                            "File deleted successfully!\nError: missing token for Cloudflare. Unable to purge cache! Check the token field under cloudflare in your config.json file."));
                        return;
                    }

                    HttpRequestMessage request =
                        new(HttpMethod.Delete, "client/v4/zones/" + zoneId + "/purge_cache/files")
                        {
                            Content = new StringContent(cloudflareContentString, Encoding.UTF8, "application/json")
                        };
                    request.Headers.Add("Authorization", $"Bearer {cloudflareToken}");

                    var response = await httpClient.SendAsync(request);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                            $"File deleted successfully!\nSuccesssfully purged the Cloudflare cache for `{fileName}`!"));
                    else
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                            $"File deleted successfully!\nAn API error occured when purging the Cloudflare cache: ```json\n{responseText}```"));
                }
                catch (Exception e)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                        $"File deleted successfully!\nAn unexpected error occured when purging the Cloudflare cache: ```json\n{e.Message}```"));
                }
            }

            [SlashCommand("preview", "Preview an image stored on Amazon S3-compatible cloud storage.")]
            public async Task CdnPreview(InteractionContext ctx,
                [Option("name", "The name (or link) of the file to preview.")]
                string name)
            {
                if (!name.Contains('.'))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            "Hmm, it doesn't look like you included a file extension. Be sure to include the proper file extension so I can find the image you're looking for."));
                    return;
                }

                HttpRequestMessage request = new(HttpMethod.Get, $"{Program.configjson.S3.CdnBaseUrl}/{name}");
                var response = await Program.httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            "Hmm, it looks like that file doesn't exist! If you're sure it does, perhaps you got the extension wrong."));
                    return;
                }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"{Program.configjson.S3.CdnBaseUrl}/{name}"));
            }
        }

        [SlashCommandGroup("debug", "Commands for checking if the bot is working properly.")]
        public class DebugCmds : ApplicationCommandModule
        {
            [SlashCommand("info", "Show debug information about the bot.")]
            public async Task DebugInfo(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(
                    new DiscordInteractionResponseBuilder().WithContent("Debug Information:\n" +
                                                                        DebugInfoHelper.GetDebugInfo()));
            }

            [SlashCommand("uptime", "Check the bot's uptime (from the time it connects to Discord).")]
            public async Task Uptime(InteractionContext ctx)
            {
                var connectUnixTime = ((DateTimeOffset)Program.connectTime).ToUnixTimeSeconds();

                var startTime = Convert.ToDateTime(Program.processStartTime);
                var startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    $"Process started at <t:{startUnixTime}:F> (<t:{startUnixTime}:R>).\n\nLast connected to Discord at <t:{connectUnixTime}:F> (<t:{connectUnixTime}:R>)."));
            }

            [SlashCommand("timecheck", "Return the current time on the machine the bot is running on.")]
            public async Task TimeCheck(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    $"Seems to me like it's currently `{DateTime.Now}`."
                    + $"\n(Short Time: `{DateTime.Now.ToShortTimeString()}`)"));
            }

            [SlashCommand("shutdown", "Shut down the bot.")]
            public async Task Shutdown(InteractionContext ctx)
            {
                DiscordButtonComponent shutdownButton = new(ButtonStyle.Danger, "shutdown-button", "Shut Down");
                DiscordButtonComponent cancelButton = new(ButtonStyle.Primary, "shutdown-cancel-button", "Cancel");

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                    .WithContent("Are you sure you want to shut down the bot? This action cannot be undone.")
                    .AddComponents(shutdownButton, cancelButton));
            }

            [SlashCommand("restart", "Restart the bot.")]
            public async Task Restart(InteractionContext ctx)
            {
                try
                {
                    var dockerCheckFile = File.ReadAllText("/proc/self/cgroup");
                    if (string.IsNullOrWhiteSpace(dockerCheckFile))
                    {
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                            "The bot may not be running under Docker; this means that `/debug restart` will behave like `/debug shutdown`."
                            + "\n\nOperation aborted. Use `/debug shutdown` if you wish to shut down the bot."));
                        return;
                    }
                }
                catch
                {
                    // /proc/self/cgroup could not be found, which means the bot is not running in Docker.
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                        "The bot may not be running under Docker; this means that `/debug restart` will behave like `/debug shutdown`.)"
                        + "\n\nOperation aborted. Use `/debug shutdown` if you wish to shut down the bot."));
                    return;
                }

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Restarting..."));
                Environment.Exit(1);
            }
        }

        [SlashCommandGroup("activity", "Configure the bot's activity!")]
        public class ActivityCmds
        {
            [SlashCommand("add",
                "Add a custom status message to the list that the bot cycles through, or modify an existing entry.")]
            public async Task AddActivity(InteractionContext ctx,
                [Option("type", "The type of status (playing, watching, etc).")]
                [Choice("Playing", "playing")]
                [Choice("Watching", "watching")]
                [Choice("Competing in", "competing")]
                [Choice("Listening to", "listening")]
                string type,
                [Option("message", "The message to show after the status type.")]
                string message)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                await Program.db.HashSetAsync("customStatusList", message, type);

                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent("Activity added successfully!"));
            }

            [SlashCommand("list", "List the custom status messages that the bot cycles through.")]
            public async Task ListActivity(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var dbList = await Program.db.HashGetAllAsync("customStatusList");
                if (dbList.Length == 0)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "There are no custom status messages in the list! Add some with `/activity add`."));
                    return;
                }

                var output = "";
                var index = 1;
                foreach (var item in dbList)
                {
                    output +=
                        $"{index}: **{item.Value.ToString().First().ToString().ToUpper() + item.Value.ToString()[1..]}** {item.Name}\n";
                    index++;
                }

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(output));
            }

            [SlashCommand("choose", "Choose a custom status message from the list to set now.")]
            public async Task ChooseActvity(InteractionContext ctx,
                [Option("id", "The ID number of the status to set. You can get this with /activity list.")]
                long id)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var dbList = await Program.db.HashGetAllAsync("customStatusList");
                var index = 1;
                foreach (var item in dbList)
                {
                    if (id == index)
                    {
                        DiscordActivity activity = new()
                        {
                            Name = item.Name
                        };
                        string targetActivityType = item.Value;
                        // TODO: make this a helper or something
                        switch (targetActivityType.ToLower())
                        {
                            case "playing":
                                activity.ActivityType = ActivityType.Playing;
                                break;
                            case "watching":
                                activity.ActivityType = ActivityType.Watching;
                                break;
                            case "listening":
                                activity.ActivityType = ActivityType.ListeningTo;
                                break;
                            case "listening to":
                                activity.ActivityType = ActivityType.ListeningTo;
                                break;
                            case "competing":
                                activity.ActivityType = ActivityType.Competing;
                                break;
                            case "competing in":
                                activity.ActivityType = ActivityType.Competing;
                                break;
                            case "streaming":
                                DiscordEmbedBuilder streamingErrorEmbed = new()
                                {
                                    Color = new DiscordColor("FF0000"),
                                    Title = "An error occurred while processing a custom status message",
                                    Description = "The activity type \"Streaming\" is not currently supported.",
                                    Timestamp = DateTime.UtcNow
                                };
                                streamingErrorEmbed.AddField("Custom Status Message", item.Name);
                                streamingErrorEmbed.AddField("Target Activity Type", targetActivityType);
                                await ctx.FollowUpAsync(
                                    new DiscordFollowupMessageBuilder().AddEmbed(streamingErrorEmbed));
                                return;
                            default:
                                DiscordEmbedBuilder invalidErrorEmbed = new()
                                {
                                    Color = new DiscordColor("FF0000"),
                                    Title = "An error occurred while processing a custom status message",
                                    Description = "The target activity type was invalid.",
                                    Timestamp = DateTime.UtcNow
                                };
                                invalidErrorEmbed.AddField("Custom Status Message", item.Name);
                                invalidErrorEmbed.AddField("Target Activity Type", targetActivityType);
                                await ctx.FollowUpAsync(
                                    new DiscordFollowupMessageBuilder().AddEmbed(invalidErrorEmbed));
                                return;
                        }

                        if (activity.Name.Contains("{uptime}"))
                        {
                            var uptime = DateTime.Now.Subtract(Convert.ToDateTime(Program.processStartTime));
                            activity.Name = activity.Name.Replace("{uptime}", uptime.Humanize());
                        }

                        if (activity.Name.Contains("{userCount}"))
                        {
                            List<DiscordUser> uniqueUsers = new();
                            foreach (var guild in Program.discord.Guilds)
                            foreach (var member in guild.Value.Members)
                            {
                                var user = await Program.discord.GetUserAsync(member.Value.Id);
                                if (!uniqueUsers.Contains(user)) uniqueUsers.Add(user);
                            }

                            activity.Name = activity.Name.Replace("{userCount}", uniqueUsers.Count.ToString());
                        }

                        if (activity.Name.Contains("{serverCount}"))
                            activity.Name = activity.Name.Replace("{serverCount}",
                                Program.discord.Guilds.Count.ToString());

                        await Program.discord.UpdateStatusAsync(activity, UserStatus.Online);
                        await ctx.FollowUpAsync(
                            new DiscordFollowupMessageBuilder().WithContent("Activity updated successfully!"));
                        break;
                    }

                    index++;
                }
            }

            [SlashCommand("remove", "Remove a custom status message from the list that the bot cycles through.")]
            public async Task RemoveActivity(InteractionContext ctx,
                [Option("id",
                    "The ID number of the status to remove. You can get this with /activity list.")]
                long id)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var dbList = await Program.db.HashGetAllAsync("customStatusList");
                var index = 1;
                foreach (var item in dbList)
                {
                    if (id == index)
                    {
                        await Program.db.HashDeleteAsync("customStatusList", item.Name);
                        break;
                    }

                    index++;
                }

                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent("Activity removed successfully."));
            }

            [SlashCommand("randomize", "Choose a random custom status message from the list.")]
            public async Task RandomizeActivity(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                await CustomStatusHelper.SetCustomStatus();

                var list = await Program.db.HashGetAllAsync("customStatusList");
                if (list.Length == 0 && Program.discord.CurrentUser.Presence.Activity.Name == null)
                {
                    // Activity was cleared; list is empty
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent(
                            "There are no custom status messages in the list! Activity cleared."));
                    return;
                }

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Activity randomized!"));
            }

            [SlashCommand("set",
                "Set the bot's activity. This overrides the list of status messages to cycle through.")]
            public async Task SetActivity(InteractionContext ctx,
                [Option("status", "The bot's online status.")]
                [Choice("Online", "online")]
                [Choice("Idle", "idle")]
                [Choice("Do Not Disturb", "dnd")]
                [Choice("Invisible", "invisible")]
                string status,
                [Option("type", "The type of status (playing, watching, etc).")]
                [Choice("Playing", "playing")]
                [Choice("Watching", "watching")]
                [Choice("Competing in", "competing")]
                [Choice("Listening to", "listening")]
                string type = null,
                [Option("message", "The message to show after the status type.")]
                string message = null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var customStatusDisabled = await Program.db.StringGetAsync("customStatusDisabled");
                if (customStatusDisabled == "true")
                {
                    // Custom status messages are disabled; warn user and don't bother going through with the rest of this command
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "Custom status messages are disabled! Use `/activity enable` to enable them first."));
                    return;
                }

                DiscordActivity activity = new();
                ActivityType activityType = default;
                activity.Name = message;
                if (type != null)
                {
                    if (activityType != ActivityType.Streaming)
                    {
                        activityType = type.ToLower() switch
                        {
                            "playing" => ActivityType.Playing,
                            "watching" => ActivityType.Watching,
                            "competing" => ActivityType.Competing,
                            "listening" => ActivityType.ListeningTo,
                            _ => default
                        };
                        activity.ActivityType = activityType;
                    }
                }
                else
                {
                    activity.ActivityType = default;
                }

                var userStatus = status.ToLower() switch
                {
                    "online" => UserStatus.Online,
                    "idle" => UserStatus.Idle,
                    "dnd" => UserStatus.DoNotDisturb,
                    "offline" => UserStatus.Invisible,
                    "invisible" => UserStatus.Invisible,
                    _ => UserStatus.Online
                };

                await Program.db.HashSetAsync("customStatus", "activity", JsonConvert.SerializeObject(activity));
                await Program.db.HashSetAsync("customStatus", "userStatus",
                    JsonConvert.SerializeObject(userStatus));

                if (activity.Name is not null)
                {
                    if (activity.Name.Contains("{uptime}"))
                    {
                        var uptime = DateTime.Now.Subtract(Convert.ToDateTime(Program.processStartTime));
                        activity.Name = activity.Name.Replace("{uptime}", uptime.Humanize());
                    }

                    if (activity.Name.Contains("{userCount}"))
                    {
                        List<DiscordUser> uniqueUsers = new();
                        foreach (var guild in Program.discord.Guilds)
                        foreach (var member in guild.Value.Members)
                        {
                            var user = await Program.discord.GetUserAsync(member.Value.Id);
                            if (!uniqueUsers.Contains(user)) uniqueUsers.Add(user);
                        }

                        activity.Name = activity.Name.Replace("{userCount}", uniqueUsers.Count.ToString());
                    }

                    if (activity.Name.Contains("{serverCount}"))
                        activity.Name = activity.Name.Replace("{serverCount}",
                            Program.discord.Guilds.Count.ToString());
                }

                await ctx.Client.UpdateStatusAsync(activity, userStatus);

                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent("Activity set successfully!"));
            }

            [SlashCommand("reset",
                "Reset the bot's activity; it will cycle through the list of custom status messages.")]
            public async Task ResetActivity(InteractionContext ctx)
            {
                await SetActivity(ctx, "online");
            }

            [SlashCommand("disable", "Clear the bot's status and stop it from cycling through the list.")]
            public async Task DisableActivity(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                await Program.discord.UpdateStatusAsync(new DiscordActivity(), UserStatus.Online);

                await Program.db.StringSetAsync("customStatusDisabled", "true");

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Custom status messages disabled. Note that the bot's status may not be cleared right away due to caching."));
            }

            [SlashCommand("enable",
                "Allow the bot to cycle through its list of custom status messages or use one set with /activity set.")]
            public async Task EnableActivity(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                await Program.db.StringSetAsync("customStatusDisabled", "false");

                await CustomStatusHelper.SetCustomStatus();

                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent("Custom status messages enabled."));
            }
        }

        public class Globals
        {
            public DiscordClient Client;

            public Globals(DiscordClient client, InteractionContext ctx)
            {
                Client = client;
                Channel = ctx.Channel;
                Guild = ctx.Guild;
                User = ctx.User;
                if (Guild != null)
                    Member = Guild.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();

                Context = ctx;
            }

            public DiscordMessage Message { get; set; }
            public DiscordChannel Channel { get; set; }
            public DiscordGuild Guild { get; set; }
            public DiscordUser User { get; set; }
            public DiscordMember Member { get; set; }
            public InteractionContext Context { get; set; }
        }

        // This code is taken from https://github.com/Sankra/cloudflare-cache-purger/blob/master/main.csx#L197.
        // (Note that I originally found it here: https://github.com/Erisa/Lykos/blob/3335c38/src/Modules/Owner.cs#L313)
        private readonly struct CloudflareContent
        {
            public CloudflareContent(List<string> urls)
            {
                Files = urls;
            }

            public List<string> Files { get; }
        }
    }
}