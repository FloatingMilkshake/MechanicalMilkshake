namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands;

public class CdnCommands : ApplicationCommandModule
{
    [SlashCommandGroup("cdn", "Manage files uploaded to Amazon S3-compatible cloud storage.")]
    public class Cdn
    {
        [SlashCommand("upload", "Upload a file to Amazon S3-compatible cloud storage.")]
        public static async Task Upload(InteractionContext ctx,
            [Option("name", "The name for the uploaded file.")]
            string name = "preserve",
            [Option("link", "A link to a file to upload.")]
            string link = null,
            [Option("file", "A direct file to upload. This will override a link if both are provided!")]
            DiscordAttachment file = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (Program.DisabledCommands.Contains("cdn"))
            {
                await CommandHandlerHelpers.FailOnMissingInfo(ctx, true);
                return;
            }

            if (file is null && link is null)
            {
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent("You must provide a link or file to upload!"));
                return;
            }

            if (file is not null) link = file.Url;

            link = link.Replace("<", "");
            link = link.Replace(">", "");

            string fileName;

            // Get file, where 'link' is the URL
            MemoryStream memStream = new(await Program.HttpClient.GetByteArrayAsync(link));

            try
            {
                var bucket = Program.ConfigJson.S3.Bucket;

                // Strip the URL down to just the file name

                // Regex partially taken from https://stackoverflow.com/a/26253039
                Regex fileNamePattern = new(@"[^/\\&\?#]+\.\w*(?=([\?&#].*$|$))");

                var fileNameAndExtension = fileNamePattern.Match(link).Value;

                // From here on out we can be sure that 'fileNameAndExtension' is in the format 'example.png'.

                var extension = Path.GetExtension(fileNameAndExtension);

                const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

                fileName = name switch
                {
                    "random" or "generate" => new string(Enumerable.Repeat(chars, 10)
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
                    .WithContentType(MimeTypeMap.GetMimeType(extension));

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

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"Upload successful!\n<{Program.ConfigJson.S3.CdnBaseUrl}/{fileName}>"));
        }

        [SlashCommand("delete", "Delete a file from Amazon S3-compatible cloud storage.")]
        public static async Task DeleteUpload(InteractionContext ctx,
            [Option("file", "The file to delete.")]
            string fileToDelete)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (Program.DisabledCommands.Contains("cdn"))
            {
                await CommandHandlerHelpers.FailOnMissingInfo(ctx, true);
                return;
            }

            fileToDelete = fileToDelete.Replace("<", "").Replace(">", "");

            var fileName = fileToDelete.Replace($"{Program.ConfigJson.S3.CdnBaseUrl}/", "");

            try
            {
                var args = new RemoveObjectArgs()
                    .WithBucket(Program.ConfigJson.S3.Bucket)
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

            var cloudflareUrlPrefix = Program.ConfigJson.Cloudflare.UrlPrefix;

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

                HttpRequestMessage request =
                    new(HttpMethod.Delete, $"client/v4/zones/{Program.ConfigJson.Cloudflare.ZoneId}/purge_cache/files")
                    {
                        Content = new StringContent(cloudflareContentString, Encoding.UTF8, "application/json")
                    };
                request.Headers.Add("Authorization", $"Bearer {Program.ConfigJson.Cloudflare.Token}");

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
                await CommandHandlerHelpers.FailOnMissingInfo(ctx, false);
                return;
            }
            
            // Credit to @Erisa for this line of regex. https://github.com/Erisa/Cliptok/blob/a80e700/Constants/RegexConstants.cs#L8
            Regex urlRx = new("(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]");

            if (urlRx.IsMatch(name) && !name.Contains(Program.ConfigJson.S3.CdnBaseUrl))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "I can only preview images on the configured CDN!" +
                        " Please be sure the domain you are providing matches the one set in the configuration file" +
                        " under `s3` > `cdnBaseUrl`."));
                return;
            }

            if (!name.Contains('.'))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "Hmm, it doesn't look like you included a file extension. Be sure to include the proper file" +
                        " extension so I can find the image you're looking for."));
                return;
            }

            HttpRequestMessage request = new(HttpMethod.Get, $"{Program.ConfigJson.S3.CdnBaseUrl}/{name}");
            var response = await Program.HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "Hmm, it looks like that file doesn't exist! If you're sure it does, perhaps you got the" +
                        " extension wrong."));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(
                    $"{Program.ConfigJson.S3.CdnBaseUrl}/{name}"));
        }
    }

    // This code is taken from https://github.com/Sankra/cloudflare-cache-purger/blob/master/main.csx#L197.
    // (minus the ReSharper disable comments)
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