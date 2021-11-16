using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
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
    [RequireOwner]
    public class Owner : BaseCommandModule
    {
        [Command("link")]
        [Aliases("wl", "links")]
        [Description("Set/update/delete a short link with Cloudflare worker-links.")]
        public async Task Link(CommandContext ctx, [Description("Set a custom key for the short link.")] string key, [Description("The URL the short link should point to.")] string url = "")
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
                await ctx.RespondAsync("Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set.");
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
                request = new HttpRequestMessage(HttpMethod.Delete, url) { };
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
                await ctx.RespondAsync("Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set.");
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
                await ctx.Channel.SendMessageAsync($"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. Think about connecting this to a pastebin-like service.");
            }
            await ctx.Channel.SendMessageAsync($"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```");
        }

        // referenced in above command. this is in a separate method to make things a little easier to work with while still being able to have it as part of the link command.
        public async Task ListWorkerLinks(CommandContext ctx)
        {
            if (Program.configjson.WorkerLinks.ApiKey == null)
            {
                await ctx.RespondAsync("Error: missing Cloudflare API Key! Make sure the apiKey field under workerLinks in your config.json file is set.");
                return;
            }

            if (Program.configjson.WorkerLinks.NamespaceId == null)
            {
                await ctx.RespondAsync("Error: missing KV Namespace ID! Make sure the namespaceId field under workerLinks in your config.json file is set.");
                return;
            }

            if (Program.configjson.WorkerLinks.AccountId == null)
            {
                await ctx.RespondAsync("Error: missing Cloudflare Account ID! Make sure the accountId field under workerLinks in your config.json file is set.");
            }

            if (Program.configjson.WorkerLinks.Email == null)
            {
                await ctx.RespondAsync("Error: missing email address for Cloudflare! Make sure the email field under workerLinks in your config.json file is set.");
            }

            DiscordMessage msg = await ctx.RespondAsync("Working...");

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

                DiscordMessage targetMsg = await msg.Channel.GetMessageAsync(msg.Id);
                string value = await valueResponse.Content.ReadAsStringAsync();
                value = value.Replace(value, $"<{value}>");
                kvListResponse += $"`{item.Name}`: {value}\n\n";
            }

            await msg.ModifyAsync(kvListResponse);
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

        [Command("upload")]
        [Aliases("up")]
        [Description("Upload a file to Amazon S3-compatible cloud storage. Accepts an uploaded file.")]
        public async Task Upload(CommandContext ctx, [Description("The name for the uploaded file. Set to `preserve` to keep the name of the file you want to upload, or `random` to generate a random name.")] string name, [Description("(Optional) A link to a file to upload. This will take priority over a file uploaded to Discord! (If you leave this empty and do not upload a file, I will display the image at the name you provided if it exists.)")] string link = null)
        {
            DiscordMessage msg = await ctx.RespondAsync("Working...");

            string linkToFile = null;
            if (link != null)
            {
                linkToFile = link;
                if (link.Contains('<'))
                {
                    link = link.Replace("<", "");
                }
                if (link.Contains('>'))
                {
                    link = link.Replace(">", "");
                }
            }
            else
            {
                try
                {
                    linkToFile = ctx.Message.Attachments[0].Url;
                }
                catch
                {
                    // message has no attachments and a link was not provided

                    if (!name.Contains('.'))
                    {
                        await msg.ModifyAsync("Hmm. If you're trying to upload a file, make sure it was uploaded or linked correctly. If you're trying to preview an image, make sure you included the file extension.");
                        return;
                    }

                    HttpRequestMessage request = new(HttpMethod.Get, $"{Program.configjson.S3.CdnBaseUrl}/{name}");
                    HttpResponseMessage response = await Program.httpClient.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        await msg.ModifyAsync("Hmm, it looks like that file doesn't exist! If you're sure it does, perhaps you got the extension wrong.");
                        return;
                    }
                    else
                    {
                        await msg.ModifyAsync($"{Program.configjson.S3.CdnBaseUrl}/{name}");
                        return;
                    }
                }
            }

            if (name == "delete" || name == "del")
            {
                await DeleteUpload(ctx, linkToFile);
                return;
            }

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
                    await msg.ModifyAsync("Error: S3 bucket info missing! Make sure the bucket field under s3 in your config.json file is set.");
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
                    contentType = "application/octet-stream"; // force download
                }

                await Program.minio.PutObjectAsync(bucket, fileName, memStream, memStream.Length, contentType, meta);
            }
            catch (MinioException e)
            {
                await msg.ModifyAsync($"An API error occured while uploading!```\n{e.Message}```");
                return;
            }
            catch (Exception e)
            {
                await msg.ModifyAsync($"An unexpected error occured while uploading!```\n{e.Message}```");
                return;
            }

            string cdnUrl;
            if (Program.configjson.S3.CdnBaseUrl == null)
            {
                await msg.ModifyAsync($"Upload successful!\nThere's no CDN URL set in your config.json, so I can't give you a link. But your file was uploaded as {fileName}.");
            }
            else
            {
                cdnUrl = Program.configjson.S3.CdnBaseUrl;
                await msg.ModifyAsync($"Upload successful!\n<{cdnUrl}/{fileName}>");
            }
        }

        // this used to be a command, but is now called from `upload` if the `name` argument is set to `delete`
        // it could be merged into `upload`, but this is the easy/lazy way to do it. it works!
        public async Task DeleteUpload(CommandContext ctx, string fileToDelete)
        {
            if (fileToDelete.Contains('<'))
            {
                fileToDelete = fileToDelete.Replace("<", "");
            }
            if (fileToDelete.Contains('>'))
            {
                fileToDelete = fileToDelete.Replace(">", "");
            }

            DiscordMessage msg = await ctx.RespondAsync("Working on it...");

            string bucket;
            if (Program.configjson.S3.Bucket == null)
            {
                await msg.ModifyAsync("Error: S3 bucket info missing! Make sure the bucket field under s3 in your config.json file is set.");
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
                await msg.ModifyAsync($"An API error occured while attempting to delete the file!```\n{e.Message}```");
                return;
            }
            catch (Exception e)
            {
                await msg.ModifyAsync($"An unexpected error occured while attempting to delete the file!```\n{e.Message}```");
                return;
            }

            await msg.ModifyAsync("File deleted successfully!\nAttempting to purge Cloudflare cache...");

            string cloudflareUrlPrefix;
            if (Program.configjson.Cloudflare.UrlPrefix != null)
            {
                cloudflareUrlPrefix = Program.configjson.Cloudflare.UrlPrefix;
            }
            else
            {
                await msg.ModifyAsync("File deleted successfully!\nError: missing Zone ID for Cloudflare. Unable to purge cache! Check the urlPrefix field under cloudflare in your config.json file.");
                return;
            }

            // https://github.com/Sankra/cloudflare-cache-purger/blob/master/main.csx#L113 (https://github.com/Erisa/Lykos/blob/main/src/Modules/Owner.cs#L227)
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
                    await msg.ModifyAsync("File deleted successfully!\nError: missing Zone ID for Cloudflare. Unable to purge cache! Check the urlPrefix field under cloudflare in your config.json file.");
                    return;
                }

                string cloudflareToken;
                if (Program.configjson.Cloudflare.Token != null)
                {
                    cloudflareToken = Program.configjson.Cloudflare.Token;
                }
                else
                {
                    await msg.ModifyAsync("File deleted successfully!\nError: missing token for Cloudflare. Unable to purge cache! Check the token field under cloudflare in your config.json file.");
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
                    await msg.ModifyAsync($"File deleted successfully!\nSuccesssfully purged the Cloudflare cache for `{fileName}`!");
                }
                else
                {
                    await msg.ModifyAsync($"File deleted successfully!\nAn API error occured when purging the Cloudflare cache: ```json\n{responseText}```");
                }
            }
            catch (Exception e)
            {
                await msg.ModifyAsync($"File deleted successfully!\nAn unexpected error occured when purging the Cloudflare cache: ```json\n{e.Message}```");
            }
        }

        [Group("debug")]
        [Description("Commands for checking if the bot is working properly.")]
        class Debug : BaseCommandModule
        {
            [GroupCommand]
            [Description("Shows debug information about the bot.")]
            public async Task DebugInfo(CommandContext ctx)
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

                await ctx.RespondAsync("**Debug Information:**\n"
                    + $"\n**Version:** `{commitHash.Trim()}`"
                    + $"\n**Framework:** `{RuntimeInformation.FrameworkDescription}`"
                    + $"\n**Platform:** `{RuntimeInformation.OSDescription}`"
                    + $"\n**Library:** `DSharpPlus {Program.discord.VersionString}`");
            }

            // this is here to add aliases for the above group command, because apparently the [Aliases] attribute doesn't work on a group command
            // yes this isn't a great way to do it but it does work
            [Command("info")]
            [Aliases("about")]
            public async Task DebugInfoAliases(CommandContext ctx)
            {
                await DebugInfo(ctx);
            }

            [Command("uptime")]
            [Description("Checks uptime of the bot, from the time it connects to Discord.")]
            public async Task Uptime(CommandContext ctx)
            {
                long unixTime = ((DateTimeOffset)Program.connectTime).ToUnixTimeSeconds();
                await ctx.RespondAsync($"<t:{unixTime}:F> (<t:{unixTime}:R>)");
            }

            [Command("timecheck")]
            [Aliases("currenttime", "time")]
            [Description("Returns the current time on the current machine.")]
            public async Task TimeCheck(CommandContext ctx)
            {
                await ctx.RespondAsync($"Seems to me like it's currently `{DateTime.Now}`.");
            }

            [Command("shutdown")]
            [Description("Shuts down the bot.")]
            public async Task Shutdown(CommandContext ctx, [Description("This must be \"I am sure\" for the command to run."), RemainingText] string areYouSure)
            {
                if (areYouSure == "I am sure")
                {
                    await ctx.RespondAsync("**Warning**: The bot is now shutting down. This action is permanent.");
                    await ctx.Client.DisconnectAsync();
                    Environment.Exit(0);
                }
                else
                {
                    await ctx.RespondAsync("Are you sure?");
                }
            }

            [Command("restart")]
            [Description("Restarts the bot.")]
            public async Task Restart(CommandContext ctx)
            {
                string dockerCheckFile = File.ReadAllText("/proc/self/cgroup");
                if (string.IsNullOrWhiteSpace(dockerCheckFile))
                {
                    await ctx.RespondAsync("The bot may not be running under Docker; this means that `!restart` will behave like `!shutdown`."
                        + "\n\nAborted. Use `!shutdown` if you wish to shut down the bot.");
                    return;
                }

                await ctx.RespondAsync("Restarting...");
                Environment.Exit(1);
            }
        }

        [Command("setactivity")]
        [Aliases("setstatus")]
        [Description("Sets the bot's activity.")]
        public async Task SetActivity(CommandContext ctx, string status = "online", string type = "playing", [RemainingText] string activityName = null)
        {
            DiscordActivity activity = new();
            ActivityType activityType = default;
            if (type == "streaming")
            {
                await ctx.RespondAsync("Please send the URL of the stream to use:");
                string streamUrl = null;
                var result = await ctx.Message.GetNextMessageAsync(m =>
                {
                    streamUrl = m.Content.Replace("<", "");
                    streamUrl = streamUrl.Replace(">", "");
                    return true;
                });

                if (!result.TimedOut)
                {
                    activityType = ActivityType.Streaming;
                    activity.ActivityType = activityType;
                    activity.StreamUrl = streamUrl;
                    activity.Name = activityName;
                }
            }
            else
            {
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

            await ctx.RespondAsync("Activity set successfully!");
        }

        [Command("resetactivity")]
        [Aliases("resetstatus", "clearactivity", "clearstatus")]
        [Hidden]
        public async Task ResetStatus(CommandContext ctx)
        {
            await SetActivity(ctx, "online");
        }

        // https://github.com/Sankra/cloudflare-cache-purger/blob/master/main.csx#L197 (https://github.com/Erisa/Lykos/blob/3335c38a52d28820a935f99c53f030805d4da607/src/Modules/Owner.cs#L313)
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
