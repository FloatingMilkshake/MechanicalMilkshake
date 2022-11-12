namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands;

public class CdnCommands : ApplicationCommandModule
{
    private static async void FailOnMissingInfo(InteractionContext ctx, bool followUp)
    {
        const string failureMsg =
            "CDN commands are disabled! Please make sure you have provided values for all of the keys under `s3` and `cloudflare` in the config file.";

        if (followUp)
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(failureMsg));
        else
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(failureMsg));
    }

    [SlashCommandGroup("cdn", "Manage files uploaded to Amazon S3-compatible cloud storage.")]
    public class Cdn
    {
        [SlashCommand("upload", "Upload a file to Amazon S3-compatible cloud storage.")]
        public static async Task Upload(InteractionContext ctx,
            [Option("name", "The name for the uploaded file.")]
            string name,
            [Option("link", "A link to a file to upload.")]
            string link = null,
            [Option("file", "A direct file to upload. This will override a link if both are provided!")]
            DiscordAttachment file = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (Program.DisabledCommands.Contains("cdn"))
            {
                FailOnMissingInfo(ctx, true);
                return;
            }

            if (file is null && link is null)
            {
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent("You must provide a link or file to upload!"));
                return;
            }

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

            MemoryStream memStream = new(await Program.HttpClient.GetByteArrayAsync(link));

            try
            {
                Dictionary<string, string> meta = new();

                //meta["x-amz-acl"] = "public-read";

                if (Program.ConfigJson.S3.Bucket == null)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "Error: S3 bucket info missing! Make sure the bucket field under s3 in your config.json file is set."));
                    return;
                }

                var bucket = Program.ConfigJson.S3.Bucket;

                Regex urlRemovalPattern = new(@".*\/\/.*\/");
                var urlRemovalMatch = urlRemovalPattern.Match(link);
                link = link.Replace(urlRemovalMatch.ToString(), "");

                // At this point 'link' will probably look like a filename (example.png), but if a link was provided that had parameters then 'link' might look more like (example.png?someparameter=something)

                Regex parameterRemovalPattern = new(@".*\?");
                var parameterRemovalMatch = parameterRemovalPattern.Match(link);

                if (!string.IsNullOrWhiteSpace(parameterRemovalMatch.ToString()))
                    link = link.Replace(parameterRemovalMatch.ToString(), "");

                var fileNameAndExtension = link.Replace("?", "");

                // From here on out we can be sure that 'fileNameAndExtension' is in the format (example.png).

                var extension = Path.GetExtension(fileNameAndExtension);

                const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

                fileName = name switch
                {
                    "random" => new string(Enumerable.Repeat(chars, 10)
                        .Select(s => s[Program.Random.Next(s.Length)])
                        .ToArray()) + extension,
                    "generate" => new string(Enumerable.Repeat(chars, 10)
                        .Select(s => s[Program.Random.Next(s.Length)])
                        .ToArray()) + extension,
                    "preserve" => fileNameAndExtension,
                    _ => name + extension
                };

                var args = new PutObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(fileName)
                    .WithStreamData(memStream)
                    .WithObjectSize(memStream.Length)
                    .WithContentType(MimeTypeMap.GetMimeType(extension))
                    .WithHeaders(meta);

                await Program.Minio.PutObjectAsync(args);
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

            if (Program.ConfigJson.S3.CdnBaseUrl == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Upload successful!\nThere's no CDN URL set in your config.json, so I can't give you a link. But your file was uploaded as {fileName}."));
            }
            else
            {
                var cdnUrl = Program.ConfigJson.S3.CdnBaseUrl;
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        $"Upload successful!\n<{cdnUrl}/{fileName}>"));
            }
        }

        [SlashCommand("delete", "Delete a file from Amazon S3-compatible cloud storage.")]
        public static async Task DeleteUpload(InteractionContext ctx,
            [Option("file", "The file to delete.")]
            string fileToDelete)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder());

            if (Program.DisabledCommands.Contains("cdn"))
            {
                FailOnMissingInfo(ctx, true);
                return;
            }

            if (fileToDelete.Contains('<')) fileToDelete = fileToDelete.Replace("<", "");

            if (fileToDelete.Contains('>')) fileToDelete = fileToDelete.Replace(">", "");

            if (Program.ConfigJson.S3.Bucket == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Error: S3 bucket info missing! Make sure the bucket field under s3 in your config.json file is set."));
                return;
            }

            var bucket = Program.ConfigJson.S3.Bucket;

            var fileName = !fileToDelete.Contains($"{Program.ConfigJson.S3.CdnBaseUrl}/")
                ? fileToDelete
                : fileToDelete.Replace($"{Program.ConfigJson.S3.CdnBaseUrl}/", "");

            try
            {
                var args = new RemoveObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(fileName);

                await Program.Minio.RemoveObjectAsync(args);
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
            if (Program.ConfigJson.Cloudflare.UrlPrefix != null)
            {
                cloudflareUrlPrefix = Program.ConfigJson.Cloudflare.UrlPrefix;
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
                if (Program.ConfigJson.Cloudflare.ZoneId != null)
                {
                    zoneId = Program.ConfigJson.Cloudflare.ZoneId;
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                        "File deleted successfully!\nError: missing Zone ID for Cloudflare. Unable to purge cache! Check the urlPrefix field under cloudflare in your config.json file."));
                    return;
                }

                string cloudflareToken;
                if (Program.ConfigJson.Cloudflare.Token != null)
                {
                    cloudflareToken = Program.ConfigJson.Cloudflare.Token;
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
        public static async Task CdnPreview(InteractionContext ctx,
            [Option("name", "The name (or link) of the file to preview.")]
            string name)
        {
            if (Program.DisabledCommands.Contains("cdn"))
            {
                FailOnMissingInfo(ctx, false);
                return;
            }

            if (!name.Contains('.'))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "Hmm, it doesn't look like you included a file extension. Be sure to include the proper file extension so I can find the image you're looking for."));
                return;
            }

            HttpRequestMessage request = new(HttpMethod.Get, $"{Program.ConfigJson.S3.CdnBaseUrl}/{name}");
            var response = await Program.HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "Hmm, it looks like that file doesn't exist! If you're sure it does, perhaps you got the extension wrong."));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(
                    $"{Program.ConfigJson.S3.CdnBaseUrl}/{name}"));
        }
    }

    // This code is taken from https://github.com/Sankra/cloudflare-cache-purger/blob/master/main.csx#L197.
    // (Note that I originally found it here: https://github.com/Erisa/Lykos/blob/3335c38/src/Modules/Owner.cs#L313)
    private readonly struct CloudflareContent
    {
        public CloudflareContent(List<string> urls)
        {
            Files = urls;
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        // ReSharper disable once MemberCanBePrivate.Local
        public List<string> Files { get; }
    }
}