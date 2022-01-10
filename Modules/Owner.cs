using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Minio.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MechanicalMilkshake.Modules
{
    public class Owner : ApplicationCommandModule
    {
        [SlashCommand("link", "[Bot owner only] Set, update, or delete a short link with Cloudflare worker-links.")]
        public async Task Link(InteractionContext ctx, [Option("key", "Set a custom key for the short link.")] string key, [Option("url", "The URL the short link should point to.")] string url = "")
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
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set."));
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
            else if (key == "delete" || key == "del")
            {
                if (!url.Contains("https://link.floatingmilkshake.com/"))
                {
                    url = $"https://link.floatingmilkshake.com/{url}";
                }
                await DeleteWorkerLink(ctx, url, httpClient);
                return;
            }
            else if (key == "list")
            {
                await ListWorkerLinks(ctx);
                return;
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Put, key) { };
            }

            string secret;
            if (Program.configjson.WorkerLinks.Secret == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set."));
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
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. Think about connecting this to a pastebin-like service."));
            }
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```"));
        }

        public async Task DeleteWorkerLink(InteractionContext ctx, string url, HttpClient httpClient)
        {
            string secret;
            if (Program.configjson.WorkerLinks.Secret == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set."));
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
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. Think about connecting this to a pastebin-like service."));
            }
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```"));
        }

        public async Task ListWorkerLinks(InteractionContext ctx)
        {
            if (Program.configjson.WorkerLinks.ApiKey == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: missing Cloudflare API Key! Make sure the apiKey field under workerLinks in your config.json file is set."));
                return;
            }

            if (Program.configjson.WorkerLinks.NamespaceId == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: missing KV Namespace ID! Make sure the namespaceId field under workerLinks in your config.json file is set."));
                return;
            }

            if (Program.configjson.WorkerLinks.AccountId == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: missing Cloudflare Account ID! Make sure the accountId field under workerLinks in your config.json file is set."));
            }

            if (Program.configjson.WorkerLinks.Email == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error: missing email address for Cloudflare! Make sure the email field under workerLinks in your config.json file is set."));
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Working..."));

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

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(kvListResponse));
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

        [SlashCommand("upload", "[Bot owner only] Upload a file to Amazon S3-compatible cloud storage.")]
        public async Task Upload(InteractionContext ctx, [Option("name", "The name for the uploaded file.")] string name, [Option("link", "A link to a file to upload.")] string link)
        {
            string linkToFile = null;
            linkToFile = link;
            if (link.Contains('<'))
            {
                link = link.Replace("<", "");
                linkToFile = link.Replace("<", "");
            }
            if (link.Contains('>'))
            {
                link = link.Replace(">", "");
                linkToFile = link.Replace(">", "");
            }
            if (link == null)
            {
                // No link was provided; nothing was provided to upload.
                // In this case we will assume the user wants to have the bot respond with the image they named. However, if that file doesn't exist, send an error (perhaps the user mistyped a filename?)

                if (!name.Contains('.'))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Hmm. If you're trying to upload a file, make sure it was uploaded or linked correctly. If you're trying to preview an image, make sure you included the file extension."));
                    return;
                }

                HttpRequestMessage request = new(HttpMethod.Get, $"{Program.configjson.S3.CdnBaseUrl}/{name}");
                HttpResponseMessage response = await Program.httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Hmm, it looks like that file doesn't exist! If you're sure it does, perhaps you got the extension wrong."));
                    return;
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{Program.configjson.S3.CdnBaseUrl}/{name}"));
                    return;
                }
            }

            if (name == "delete" || name == "del")
            {
                await DeleteUpload(ctx, linkToFile);
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Working..."));

            string fileName;
            string extension;

            MemoryStream memStream = new(await Program.httpClient.GetByteArrayAsync(linkToFile));

            try
            {
                Dictionary<string, string> meta = new() { };

                meta["x-amz-acl"] = "public-read";

                string bucket = null;
                if (Program.configjson.S3.Bucket == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: S3 bucket info missing! Make sure the bucket field under s3 in your config.json file is set."));
                    return;
                }
                else
                {
                    bucket = Program.configjson.S3.Bucket;
                }

                Regex urlRemovalPattern = new(@".*\/\/.*\/");
                Match urlRemovalMatch = urlRemovalPattern.Match(linkToFile);
                linkToFile = linkToFile.Replace(urlRemovalMatch.ToString(), "");

                Regex parameterRemovalPattern = new(@".*\?");
                Match parameterRemovalMatch = parameterRemovalPattern.Match(linkToFile);
                if (parameterRemovalMatch != null && parameterRemovalMatch.ToString() != "")
                {
                    linkToFile = parameterRemovalMatch.ToString();
                }
                linkToFile = linkToFile.Replace("?", "");

                Regex extPattern = new(@"\..*");
                Match extMatch = extPattern.Match(linkToFile);
                extension = extMatch.ToString();

                const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

                if (name == "random" || name == "generate")
                {
                    fileName = new string(Enumerable.Repeat(chars, 10).Select(s => s[Program.random.Next(s.Length)]).ToArray()) + extension;
                }
                else if (name == "existing" || name == "preserve" || name == "keep")
                {
                    fileName = linkToFile;
                }
                else
                {
                    fileName = name + extension;
                }

                string contentType;
                if (extension == ".png")
                {
                    contentType = "image/png";
                }
                else if (extension == ".jpg" || extension == ".jpeg")
                {
                    contentType = "image/jpeg";
                }
                else if (extension == ".txt")
                {
                    contentType = "text/plain";
                }
                else
                {
                    contentType = "application/octet-stream"; // Using application/octet-stream will force the file to be downloaded, which is what we want to happen if it doesn't match any of the file extensions above.
                }

                await Program.minio.PutObjectAsync(bucket, fileName, memStream, memStream.Length, contentType, meta);
            }
            catch (MinioException e)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"An API error occured while uploading!```\n{e.Message}```"));
                return;
            }
            catch (Exception e)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"An unexpected error occured while uploading!```\n{e.Message}```"));
                return;
            }

            string cdnUrl;
            if (Program.configjson.S3.CdnBaseUrl == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Upload successful!\nThere's no CDN URL set in your config.json, so I can't give you a link. But your file was uploaded as {fileName}."));
            }
            else
            {
                cdnUrl = Program.configjson.S3.CdnBaseUrl;
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Upload successful!\n<{cdnUrl}/{fileName}>"));
            }
        }

        // This code used to be a command, but is now called from Upload if the name argument is set to "delete". It could be merged into Upload, but this is the easy/lazy way to do it. It works, so I'm happy with it.
        public async Task DeleteUpload(InteractionContext ctx, string fileToDelete)
        {
            if (fileToDelete.Contains('<'))
            {
                fileToDelete = fileToDelete.Replace("<", "");
            }
            if (fileToDelete.Contains('>'))
            {
                fileToDelete = fileToDelete.Replace(">", "");
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Working on it..."));

            string bucket;
            if (Program.configjson.S3.Bucket == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Error: S3 bucket info missing! Make sure the bucket field under s3 in your config.json file is set."));
                return;
            }
            else
            {
                bucket = Program.configjson.S3.Bucket;
            }

            string fileName;
            if (!fileToDelete.Contains("https://cdn.floatingmilkshake.com/"))
            {
                fileName = fileToDelete;
            }
            else
            {
                fileName = fileToDelete.Replace("https://cdn.floatingmilkshake.com/", "");
            }

            try
            {
                await Program.minio.RemoveObjectAsync(bucket, fileName);
            }
            catch (MinioException e)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"An API error occured while attempting to delete the file!```\n{e.Message}```"));
                return;
            }
            catch (Exception e)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"An unexpected error occured while attempting to delete the file!```\n{e.Message}```"));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("File deleted successfully!\nAttempting to purge Cloudflare cache..."));

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

        [SlashCommandGroup("debug", "[Bot owner only] Commands for checking if the bot is working properly.")]
        [SlashRequireOwner]
        public class DebugCmds : ApplicationCommandModule
        {
            [SlashCommand("info", "[Bot owner only] Show debug information about the bot.")]
            public async Task DebugInfo(InteractionContext ctx)
            {
                string commitHash = "";
                if (File.Exists("CommitHash.txt"))
                {
                    StreamReader readHash = new("CommitHash.txt");
                    commitHash = readHash.ReadToEnd();
                }
                if (commitHash == "")
                {
                    commitHash = "dev";
                }

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("**Debug Information:**\n"
                    + $"\n**Version:** `{commitHash.Trim()}`"
                    + $"\n**Framework:** `{RuntimeInformation.FrameworkDescription}`"
                    + $"\n**Platform:** `{RuntimeInformation.OSDescription}`"
                    + $"\n**Library:** `DSharpPlus {Program.discord.VersionString}`"));
            }

            [SlashCommand("uptime", "[Bot owner only] Check the bot's uptime (from the time it connects to Discord).")]
            public async Task Uptime(InteractionContext ctx)
            {
                long unixTime = ((DateTimeOffset)Program.connectTime).ToUnixTimeSeconds();
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"<t:{unixTime}:F> (<t:{unixTime}:R>)"));
            }

            [SlashCommand("timecheck", "[Bot owner only] Return the current time on the machine the bot is running on.")]
            public async Task TimeCheck(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Seems to me like it's currently `{DateTime.Now}`."));
            }

            [SlashCommand("shutdown", "[Bot owner only] Shut down the bot.")]
            public async Task Shutdown(InteractionContext ctx)
            {
                DiscordButtonComponent button = new(ButtonStyle.Primary, "shutdown-button", "Shut Down");

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Are you sure you want to shut down the bot?").AddComponents(button).AsEphemeral(true));
            }

            [SlashCommand("restart", "[Bot owner only] Restart the bot.")]
            public async Task Restart(InteractionContext ctx)
            {
                try
                {
                    string dockerCheckFile = File.ReadAllText("/proc/self/cgroup");
                    if (string.IsNullOrWhiteSpace(dockerCheckFile))
                    {
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("The bot may not be running under Docker; this means that `!restart` will behave like `!shutdown`."
                            + "\n\nAborted. Use `!shutdown` if you wish to shut down the bot."));
                        return;
                    }
                }
                catch
                {
                    // /proc/self/cgroup could not be found, which means the bot is not running in Docker.
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("The bot may not be running under Docker; this means that `!restart` will behave like `!shutdown`.)"
                            + "\n\nAborted. Use `!shutdown` if you wish to shut down the bot."));
                    return;
                }

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Restarting..."));
                Environment.Exit(1);
            }
        }

        [SlashCommand("setactivity", "[Bot owner only] Set the bot's activity.")]
        public async Task SetActivity(InteractionContext ctx, [Option("status", "The bot's online status.")] string status = "online", [Option("type", "The type of status (playing, watching, etc).")] string type = "playing", [Option("activityName", "The bot's activity (for example, watching '!help').")] string activityName = null)
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

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Activity set successfully!"));
        }

        [SlashCommand("resetactivity", "Reset the bot's activity (sets its status to online with no activity).")]
        [Hidden]
        public async Task ResetStatus(InteractionContext ctx)
        {
            await SetActivity(ctx, "online");
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
