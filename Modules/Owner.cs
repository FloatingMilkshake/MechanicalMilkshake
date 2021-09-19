using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Minio.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class Owner : BaseCommandModule
    {
        [Command("shutdown")]
        [Description("Shuts down the bot.")]
        [RequireOwner]
        public async Task Shutdown(CommandContext ctx, [Description("This must be \"I am sure\" for the command to run."), RemainingText] string areYouSure)
        {
            if (areYouSure == "I am sure")
            {
                var msg = await ctx.RespondAsync("**Warning**: The bot is now shutting down. This action is permanent.");
                await ctx.Client.DisconnectAsync();
                Environment.Exit(0);
            }
            else
            {
                await ctx.RespondAsync("Are you sure?");
            }
        }

        [Command("link")]
        [Aliases("wl", "links")]
        [Description("Set/update/delete a short link with Cloudflare worker-links.")]
        public async Task Link(CommandContext ctx, [Description("(Optional) Set a custom key for the short link.")] string key, [Description("The URL the short link should point to.")] string url)
        {

            string baseUrl;
            if (Environment.GetEnvironmentVariable("WORKER_LINKS_BASE_URL") == null)
            {
                await ctx.RespondAsync("Error: No base URL provided! Make sure the environment variable `WORKER_LINKS_BASE_URL` is set.");
                return;
            }
            else
            {
                baseUrl = Environment.GetEnvironmentVariable("WORKER_LINKS_BASE_URL");
            }

            using HttpClient httpClient = new()
            {
                BaseAddress = new Uri(baseUrl)
            };

            HttpRequestMessage request;

            if (key == "null" || key == "random")
            {
                request = new HttpRequestMessage(HttpMethod.Post, "") { };
            }
            else if (key == "delete" || key == "del")
            {
                request = new HttpRequestMessage(HttpMethod.Delete, url) { };
            }
            else
            {
                request = new HttpRequestMessage(HttpMethod.Put, key) { };
            }

            string secret;
            if (Environment.GetEnvironmentVariable("WORKER_LINKS_SECRET") == null)
            {
                await ctx.RespondAsync("Error: No secret provided! Make sure the environment variable `WORKER_LINKS_secret` is set.");
                return;
            }
            else
            {
                secret = Environment.GetEnvironmentVariable("WORKER_LINKS_SECRET");
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

        [Command("upload")]
        [Description("Upload a file to Amazon S3-compatible cloud storage.")]
        public async Task Upload(CommandContext ctx)
        {
            var msg = await ctx.RespondAsync("Uploading...");

            if (ctx.Message.Attachments.Count == 0)
            {
                await msg.ModifyAsync("Plese attach a file to upload!");
                return;
            }

            string fileName;
            string extension;
            try
            {
                Dictionary<string, string> meta = new() { };

                meta["x-amz-acl"] = "public-read";

                string bucket = null;
                if (Environment.GetEnvironmentVariable("S3_BUCKET") == null)
                {
                    await msg.ModifyAsync("Error: S3 bucket info missing! Please check the `S3_BUCKET` environment variable.");
                }
                else
                {
                    bucket = Environment.GetEnvironmentVariable("S3_BUCKET");
                }

                if (ctx.Message.Attachments[0].FileName.Contains(".png"))
                {
                    extension = "png";
                }
                else if (ctx.Message.Attachments[0].FileName.Contains(".jpg"))
                {
                    extension = "jpg";
                }
                else if (ctx.Message.Attachments[0].FileName.Contains(".gif"))
                {
                    extension = "gif";
                }
                else if (ctx.Message.Attachments[0].FileName.Contains(".mov"))
                {
                    extension = "mov";
                }
                else
                {
                    await msg.ModifyAsync("File extension not supported! Please add to `Owner.cs`.");
                    return;
                }

                const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                fileName = new string(Enumerable.Repeat(chars, 10).Select(s => s[Program.random.Next(s.Length)]).ToArray()) + "." + extension;

                await Program.minio.PutObjectAsync(bucket, fileName, fileName, $"image/{extension}", meta);
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
            if (Environment.GetEnvironmentVariable("CDN_BASE_URL") == null)
            {
                await msg.ModifyAsync($"Upload successful!\nThere's no CDN URL set in your environment file, so I can't give you a link. But your file was uploaded as {fileName}.{extension}.");
            }
            else
            {
                cdnUrl = Environment.GetEnvironmentVariable("CDN_BASE_URL");
                await msg.ModifyAsync($"Upload successful!\n<{cdnUrl}/{fileName}.{extension}>");
            }
        }
    }
}
