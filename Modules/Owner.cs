using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MimeTypes;
using Minio.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MechanicalMilkshake.Modules
{
    [SlashRequireOwner]
    public class Owner : ApplicationCommandModule
    {
        [SlashCommandGroup("link", "[Bot owner only] Set, update, or delete a short link with Cloudflare worker-links.")]
        public class Link
        {
            [SlashCommand("set", "[Bot owner only] Set or update a short link with Cloudflare worker-links.")]
            public async Task SetLink(InteractionContext ctx, [Option("key", "Set a custom key for the short link.")] string key, [Option("url", "The URL the short link should point to.")] string url)
            {
                if (url.Contains('<'))
                {
                    url = url.Replace("<", "");
                }
                if (url.Contains('>'))
                {
                    url = url.Replace(">", "");
                }

                string baseUrl;
                if (Program.configjson.WorkerLinks.BaseUrl == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set.").AsEphemeral(true));
                    return;
                }
                else
                {
                    baseUrl = Program.configjson.WorkerLinks.BaseUrl;
                }

                using HttpClient httpClient = new()
                {
                    BaseAddress = new Uri(baseUrl)
                };

                HttpRequestMessage request = null;

                if (key == "null" || key == "random" || key == "rand")
                {
                    request = new HttpRequestMessage(HttpMethod.Post, "") { };
                }
                else
                {
                    request = new HttpRequestMessage(HttpMethod.Put, key) { };
                }

                string secret;
                if (Program.configjson.WorkerLinks.Secret == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set.").AsEphemeral(true));
                    return;
                }
                else
                {
                    secret = Program.configjson.WorkerLinks.Secret;
                }

                request.Headers.Add("Authorization", secret);
                request.Headers.Add("URL", url);

                HttpResponseMessage response = await httpClient.SendAsync(request);
                int httpStatusCode = (int)response.StatusCode;
                string httpStatus = response.StatusCode.ToString();
                string responseText = await response.Content.ReadAsStringAsync();
                if (responseText.Length > 1940)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. Think about connecting this to a pastebin-like service.").AsEphemeral(true));
                }
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```").AsEphemeral(true));
            }

            [SlashCommand("delete", "[Bot owner only] Delete a short link with Cloudflare worker-links.")]
            public async Task DeleteWorkerLink(InteractionContext ctx, [Option("link", "The key or URL of the short link to delete.")] string url)
            {
                string baseUrl;
                if (Program.configjson.WorkerLinks.BaseUrl == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set.").AsEphemeral(true));
                    return;
                }
                else
                {
                    baseUrl = Program.configjson.WorkerLinks.BaseUrl;
                }
                using HttpClient httpClient = new()
                {
                    BaseAddress = new Uri(baseUrl)
                };

                if (!url.Contains(baseUrl))
                {
                    url = $"{baseUrl}/{url}";
                }

                string secret;
                if (Program.configjson.WorkerLinks.Secret == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set.").AsEphemeral(true));
                    return;
                }
                else
                {
                    secret = Program.configjson.WorkerLinks.Secret;
                }

                HttpRequestMessage request = new(HttpMethod.Delete, url) { };
                request.Headers.Add("Authorization", secret);

                HttpResponseMessage response = await httpClient.SendAsync(request);
                int httpStatusCode = (int)response.StatusCode;
                string httpStatus = response.StatusCode.ToString();
                string responseText = await response.Content.ReadAsStringAsync();
                if (responseText.Length > 1940)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. Think about connecting this to a pastebin-like service.").AsEphemeral(true));
                }
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```").AsEphemeral(true));
            }

            [SlashCommand("list", "[Bot owner only] List all short links configured with Cloudflare worker-links.")]
            public async Task ListWorkerLinks(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                if (Program.configjson.WorkerLinks.ApiKey == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error: missing Cloudflare API Key! Make sure the apiKey field under workerLinks in your config.json file is set.").AsEphemeral(true));
                    return;
                }

                if (Program.configjson.WorkerLinks.NamespaceId == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error: missing KV Namespace ID! Make sure the namespaceId field under workerLinks in your config.json file is set.").AsEphemeral(true));
                    return;
                }

                if (Program.configjson.WorkerLinks.AccountId == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error: missing Cloudflare Account ID! Make sure the accountId field under workerLinks in your config.json file is set.").AsEphemeral(true));
                }

                if (Program.configjson.WorkerLinks.Email == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error: missing email address for Cloudflare! Make sure the email field under workerLinks in your config.json file is set.").AsEphemeral(true));
                }

                string requestUri = $"https://api.cloudflare.com/client/v4/accounts/{Program.configjson.WorkerLinks.AccountId}/storage/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}/keys";
                HttpRequestMessage request = new(HttpMethod.Get, requestUri);

                request.Headers.Add("X-Auth-Key", Program.configjson.WorkerLinks.ApiKey);
                request.Headers.Add("X-Auth-Email", Program.configjson.WorkerLinks.Email);
                HttpResponseMessage response = await Program.httpClient.SendAsync(request);

                string responseText = await response.Content.ReadAsStringAsync();

                var parsedResponse = JsonConvert.DeserializeObject<CloudflareResponse>(responseText);

                string kvListResponse = "";

                foreach (KVEntry item in parsedResponse.Result)
                {
                    string key = item.Name.Replace("/", "%2F");

                    string valueRequestUri = $"https://api.cloudflare.com/client/v4/accounts/{Program.configjson.WorkerLinks.AccountId}/storage/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}/values/{key}";
                    HttpRequestMessage valueRequest = new(HttpMethod.Get, valueRequestUri);

                    valueRequest.Headers.Add("X-Auth-Key", Program.configjson.WorkerLinks.ApiKey);
                    valueRequest.Headers.Add("X-Auth-Email", Program.configjson.WorkerLinks.Email);
                    HttpResponseMessage valueResponse = await Program.httpClient.SendAsync(valueRequest);

                    string value = await valueResponse.Content.ReadAsStringAsync();
                    value = value.Replace(value, $"<{value}>");
                    kvListResponse += $"`{item.Name}`: {value}\n\n";
                }

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(kvListResponse).AsEphemeral(true));
            }

            public class CloudflareResponse
            {
                [JsonProperty("result")]
                public List<KVEntry> Result { get; set; }
            }

            public class KVEntry
            {
                [JsonProperty("name")]
                public string Name { get; set; }
            }
        }

        [SlashCommandGroup("cdn", "[Bot owner only] Manage files uploaded to Amazon S3-compatible cloud storage.")]
        public class Cdn
        {
            [SlashCommand("upload", "[Bot owner only] Upload a file to Amazon S3-compatible cloud storage.")]
            public async Task Upload(InteractionContext ctx, [Option("name", "The name for the uploaded file.")] string name, [Option("link", "A link to a file to upload.")] string link)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

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
                    Dictionary<string, string> meta = new() { };

                    meta["x-amz-acl"] = "public-read";

                    string bucket = null;
                    if (Program.configjson.S3.Bucket == null)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error: S3 bucket info missing! Make sure the bucket field under s3 in your config.json file is set."));
                        return;
                    }
                    else
                    {
                        bucket = Program.configjson.S3.Bucket;
                    }

                    Regex urlRemovalPattern = new(@".*\/\/.*\/");
                    Match urlRemovalMatch = urlRemovalPattern.Match(link);
                    link = link.Replace(urlRemovalMatch.ToString(), "");

                    // At this point 'link' will probably look like a filename (example.png), but if a link was provided that had parameters then 'link' might look more like (example.png?someparameter=something)

                    Regex parameterRemovalPattern = new(@".*\?");
                    Match parameterRemovalMatch = parameterRemovalPattern.Match(link);
                    if (parameterRemovalMatch != null && parameterRemovalMatch.ToString() != "")
                    {
                        link = parameterRemovalMatch.ToString();
                    }
                    string fileNameAndExtension = link.Replace("?", "");

                    // From here on out we can be sure that 'fileNameAndExtension' is in the format (example.png).

                    string extension = Path.GetExtension(fileNameAndExtension);

                    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

                    if (name == "random" || name == "generate")
                    {
                        fileName = new string(Enumerable.Repeat(chars, 10).Select(s => s[Program.random.Next(s.Length)]).ToArray()) + extension;
                    }
                    else if (name == "existing" || name == "preserve" || name == "keep")
                    {
                        fileName = fileNameAndExtension;
                    }
                    else
                    {
                        fileName = name + extension;
                    }

                    await Program.minio.PutObjectAsync(bucket, fileName, memStream, memStream.Length, MimeTypeMap.GetMimeType(extension), meta);
                }
                catch (MinioException e)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"An API error occured while uploading!```\n{e.Message}```"));
                    return;
                }
                catch (Exception e)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"An unexpected error occured while uploading!```\n{e.Message}```"));
                    return;
                }

                string cdnUrl;
                if (Program.configjson.S3.CdnBaseUrl == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Upload successful!\nThere's no CDN URL set in your config.json, so I can't give you a link. But your file was uploaded as {fileName}."));
                }
                else
                {
                    cdnUrl = Program.configjson.S3.CdnBaseUrl;
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Upload successful!\n<{cdnUrl}/{fileName}>"));
                }
            }

            [SlashCommand("delete", "[Bot owner only] Delete a file from Amazon S3-compatible cloud storage.")]
            public async Task DeleteUpload(InteractionContext ctx, [Option("file", "The file to delete.")] string fileToDelete)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                if (fileToDelete.Contains('<'))
                {
                    fileToDelete = fileToDelete.Replace("<", "");
                }
                if (fileToDelete.Contains('>'))
                {
                    fileToDelete = fileToDelete.Replace(">", "");
                }

                string bucket;
                if (Program.configjson.S3.Bucket == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error: S3 bucket info missing! Make sure the bucket field under s3 in your config.json file is set."));
                    return;
                }
                else
                {
                    bucket = Program.configjson.S3.Bucket;
                }

                string fileName;
                if (!fileToDelete.Contains($"{Program.configjson.S3.CdnBaseUrl}/"))
                {
                    fileName = fileToDelete;
                }
                else
                {
                    fileName = fileToDelete.Replace($"{Program.configjson.S3.CdnBaseUrl}/", "");
                }

                try
                {
                    await Program.minio.RemoveObjectAsync(bucket, fileName);
                }
                catch (MinioException e)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"An API error occured while attempting to delete the file!```\n{e.Message}```"));
                    return;
                }
                catch (Exception e)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"An unexpected error occured while attempting to delete the file!```\n{e.Message}```"));
                    return;
                }

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("File deleted successfully!\nAttempting to purge Cloudflare cache..."));

                string cloudflareUrlPrefix;
                if (Program.configjson.Cloudflare.UrlPrefix != null)
                {
                    cloudflareUrlPrefix = Program.configjson.Cloudflare.UrlPrefix;
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("File deleted successfully!\nError: missing Zone ID for Cloudflare. Unable to purge cache! Check the urlPrefix field under cloudflare in your config.json file."));
                    return;
                }

                // This code is (mostly) taken from https://github.com/Sankra/cloudflare-cache-purger/blob/master/main.csx#L113.
                // (Note that I originally found it here: https://github.com/Erisa/Lykos/blob/1f32e03/src/Modules/Owner.cs#L232)

                CloudflareContent content = new(new List<string>() { cloudflareUrlPrefix + fileName });
                string cloudflareContentString = JsonConvert.SerializeObject(content);
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
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("File deleted successfully!\nError: missing Zone ID for Cloudflare. Unable to purge cache! Check the urlPrefix field under cloudflare in your config.json file."));
                        return;
                    }

                    string cloudflareToken;
                    if (Program.configjson.Cloudflare.Token != null)
                    {
                        cloudflareToken = Program.configjson.Cloudflare.Token;
                    }
                    else
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("File deleted successfully!\nError: missing token for Cloudflare. Unable to purge cache! Check the token field under cloudflare in your config.json file."));
                        return;
                    }

                    HttpRequestMessage request = new(HttpMethod.Delete, "client/v4/zones/" + zoneId + "/purge_cache")
                    {
                        Content = new StringContent(cloudflareContentString, Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("Authorization", $"Bearer {cloudflareToken}");

                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"File deleted successfully!\nSuccesssfully purged the Cloudflare cache for `{fileName}`!"));
                    }
                    else
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"File deleted successfully!\nAn API error occured when purging the Cloudflare cache: ```json\n{responseText}```"));
                    }
                }
                catch (Exception e)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"File deleted successfully!\nAn unexpected error occured when purging the Cloudflare cache: ```json\n{e.Message}```"));
                }
            }

            [SlashCommand("preview", "[Bot owner only] Preview an image stored on Amazon S3-compatible cloud storage.")]
            public async Task CdnPreview(InteractionContext ctx, [Option("name", "The name (or link) of the file to preview.")] string name, [Option("ephemeralresponse", "Whether my response should be ephemeral. Defaults to True.")] bool isEphemeral = true)
            {
                if (!name.Contains('.'))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Hmm, it doesn't look like you included a file extension. Be sure to include the proper file extension so I can find the image you're looking for.").AsEphemeral(true));
                    return;
                }

                HttpRequestMessage request = new(HttpMethod.Get, $"{Program.configjson.S3.CdnBaseUrl}/{name}");
                HttpResponseMessage response = await Program.httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Hmm, it looks like that file doesn't exist! If you're sure it does, perhaps you got the extension wrong.").AsEphemeral(true));
                    return;
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{Program.configjson.S3.CdnBaseUrl}/{name}").AsEphemeral(isEphemeral));
                    return;
                }
            }
        }

        [SlashCommandGroup("debug", "[Bot owner only] Commands for checking if the bot is working properly.")]
        public class DebugCmds : ApplicationCommandModule
        {
            [SlashCommand("info", "[Bot owner only] Show debug information about the bot.")]
            public async Task DebugInfo(InteractionContext ctx, [Option("ephemeralresponse", "Whether my response should be ephemeral. Defaults to False.")] bool isEphemeral = false)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Debug Information:\n" + Program.GetDebugInfo()).AsEphemeral(isEphemeral));
            }

            [SlashCommand("uptime", "[Bot owner only] Check the bot's uptime (from the time it connects to Discord).")]
            public async Task Uptime(InteractionContext ctx)
            {
                long connectUnixTime = ((DateTimeOffset)Program.connectTime).ToUnixTimeSeconds();

                DateTime startTime = Convert.ToDateTime(Program.processStartTime);
                long startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Process started at <t:{startUnixTime}:F> (<t:{startUnixTime}:R>).\n\nLast connected to Discord at <t:{connectUnixTime}:F> (<t:{connectUnixTime}:R>).").AsEphemeral(true));
            }

            [SlashCommand("timecheck", "[Bot owner only] Return the current time on the machine the bot is running on.")]
            public async Task TimeCheck(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Seems to me like it's currently `{DateTime.Now}`."
                    + $"\n(Short Time: `{DateTime.Now.ToShortTimeString()}`)").AsEphemeral(true));
            }

            [SlashCommand("shutdown", "[Bot owner only] Shut down the bot.")]
            public async Task Shutdown(InteractionContext ctx)
            {
                DiscordButtonComponent button = new(ButtonStyle.Primary, "shutdown-button", "Shut Down");

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Are you sure you want to shut down the bot?\n\n(To cancel, click `Dismiss message` below.)").AddComponents(button).AsEphemeral(true));
            }

            // This method is called from Program.cs when the user interacts with the button created in the above method.
            public static void ShutdownConfirmed(DiscordInteraction interaction)
            {
                interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("**Warning: The bot is now shutting down. This action is permanent.**").AsEphemeral(true));
                Program.discord.DisconnectAsync();
                Environment.Exit(0);
            }

            [SlashCommand("restart", "[Bot owner only] Restart the bot.")]
            public async Task Restart(InteractionContext ctx)
            {
                try
                {
                    string dockerCheckFile = File.ReadAllText("/proc/self/cgroup");
                    if (string.IsNullOrWhiteSpace(dockerCheckFile))
                    {
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("The bot may not be running under Docker; this means that `/debug restart` will behave like `/debug shutdown`."
                            + "\n\nOperation aborted. Use `/debug shutdown` if you wish to shut down the bot.").AsEphemeral(true));
                        return;
                    }
                }
                catch
                {
                    // /proc/self/cgroup could not be found, which means the bot is not running in Docker.
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("The bot may not be running under Docker; this means that `/debug restart` will behave like `/debug shutdown`.)"
                            + "\n\nOperation aborted. Use `/debug shutdown` if you wish to shut down the bot.").AsEphemeral(true));
                    return;
                }

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Restarting...").AsEphemeral(true));
                Environment.Exit(1);
            }
        }

        [SlashCommand("setactivity", "[Bot owner only] Set the bot's activity.")]
        public async Task SetActivity(InteractionContext ctx, [Option("status", "The bot's online status. Defaults to 'online'.")] string status = "online", [Option("type", "The type of status (playing, watching, etc). Defaults to 'playing'.")] string type = "playing", [Option("activityName", "The bot's activity (for example, watching '!help'). Defaults to nothing.")] string activityName = null)
        {
            DiscordActivity activity = new();
            ActivityType activityType = default;
            activity.Name = activityName;
            if (activityType != ActivityType.Streaming)
            {
                activityType = type.ToLower() switch
                {
                    "playing" => ActivityType.Playing,
                    "watching" => ActivityType.Watching,
                    "competing" => ActivityType.Competing,
                    "listening" => ActivityType.ListeningTo,
                    "listeningto" => ActivityType.ListeningTo,
                    _ => ActivityType.Playing,
                };
                activity.ActivityType = activityType;
            }

            UserStatus userStatus = status.ToLower() switch
            {
                "online" => UserStatus.Online,
                "idle" => UserStatus.Idle,
                "dnd" => UserStatus.DoNotDisturb,
                "offline" => UserStatus.Invisible,
                "invisible" => UserStatus.Invisible,
                _ => UserStatus.Online,
            };

            await ctx.Client.UpdateStatusAsync(activity, userStatus);

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Activity set successfully!").AsEphemeral(true));
        }

        [SlashCommand("resetactivity", "[Bot owner only] Reset the bot's activity (sets its status to online with no activity).")]
        public async Task ResetStatus(InteractionContext ctx)
        {
            await SetActivity(ctx, "online");
        }

        // The idea for this command, and a lot of the code, is taken from Erisa's Lykos. References are linked below.
        // https://github.com/Erisa/Lykos/blob/5f9c17c/src/Modules/Owner.cs#L116-L144
        // https://github.com/Erisa/Lykos/blob/822e9c5/src/Modules/Helpers.cs#L36-L82
        [SlashCommand("runcommand", "[Bot owner only] Run a shell command on the machine the bot's running on!")]
        public async Task RunCommand(InteractionContext ctx, [Option("command", "The command to run, including any arguments.")] string command, [Option("ephemeralresponse", "Whether my response should be ephemeral. Defaults to False.")] bool isEphemeral = false)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(isEphemeral));
            string osDescription = RuntimeInformation.OSDescription;
            string fileName;
            string args;
            string escapedArgs = command.Replace("\"", "\\\"");

            if (osDescription.Contains("Windows"))
            {
                fileName = "C:\\Windows\\System32\\cmd.exe";
                args = $"/C \"{escapedArgs} 2>&1\"";
            }
            else
            {
                // Assume Linux if OS is not Windows because I'm too lazy to bother with specific checks right now, might implement that later
                fileName = Environment.GetEnvironmentVariable("SHELL");
                if (!File.Exists(fileName))
                {
                    fileName = "/bin/sh";
                }
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
            string result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (result.Length > 1947)
            {
                Console.WriteLine(result);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Finished with exit code `{proc.ExitCode}`! It was too long to post here though; see the console for the full output.").AsEphemeral(isEphemeral));
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Finished with exit code `{proc.ExitCode}`! Output: ```\n{result}```").AsEphemeral(isEphemeral));
            }
        }

        // This code is taken from https://github.com/Sankra/cloudflare-cache-purger/blob/master/main.csx#L197.
        // (Note that I originally found it here: https://github.com/Erisa/Lykos/blob/3335c38/src/Modules/Owner.cs#L313)
        readonly struct CloudflareContent
        {
            public CloudflareContent(List<string> urls)
            {
                Files = urls;
            }

            public List<string> Files { get; }
        }
    }
}
