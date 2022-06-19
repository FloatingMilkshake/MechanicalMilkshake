namespace MechanicalMilkshake.Modules
{
    internal class Helpers
    {
        public static string GetDebugInfo()
        {
            string commitHash = "";
            if (File.Exists("CommitHash.txt"))
            {
                StreamReader readHash = new("CommitHash.txt");
                commitHash = readHash.ReadToEnd().Trim();
            }
            if (commitHash == "")
            {
                commitHash = "dev";
            }

            string commitTime = "";
            string commitTimeDescription = "";
            if (File.Exists("CommitTime.txt"))
            {
                StreamReader readTime = new("CommitTime.txt");
                commitTime = readTime.ReadToEnd();
                commitTimeDescription = "Commit timestamp:";
            }
            if (commitTime == "")
            {
                commitTime = Program.connectTime.ToString();
                commitTimeDescription = "Last connected to Discord at";
            }

            string commitMessage = "";
            if (File.Exists("CommitMessage.txt"))
            {
                StreamReader readMessage = new("CommitMessage.txt");
                commitMessage = readMessage.ReadToEnd();
            }
            if (commitMessage == "")
            {
                commitMessage = $"Running in development mode; process started at {Program.processStartTime}";
            }

            return $"\nFramework: `{RuntimeInformation.FrameworkDescription}`"
                + $"\nPlatform: `{RuntimeInformation.OSDescription}`"
                + $"\nLibrary: `DSharpPlus {Program.discord.VersionString}`"
                + "\n"
                + $"\nLatest commit: `{commitHash}`"
                + $"\n{commitTimeDescription} `{commitTime}`"
                + $"\nLatest commit message:\n```\n{commitMessage}\n```";
        }

        public static async Task KeywordCheck(DiscordMessage message)
        {
            if (message.Author.Id == 455432936339144705)
                return;
            else if (message.Content.Contains("floaty"))
                await SendAlert("floaty", message);
            else if (message.Content.Contains("milkshake"))
                await SendAlert("milkshake", message);


            static async Task SendAlert(string keyword, DiscordMessage message)
            {
                var guild = await Program.discord.GetGuildAsync(799644062973427743);
                var member = await guild.GetMemberAsync(455432936339144705);

                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor("#7287fd"),
                    Title = $"Tracked keyword \"{keyword}\" triggered!",
                    Description = $"{message.Content}"
                };
                embed.AddField("Author ID", $"{message.Author.Id}", true);
                embed.AddField("Author Mention", $"{message.Author.Mention}", true);

                if (message.Channel.IsPrivate)
                    embed.AddField("Channel", $"Message sent in DMs.");
                else
                    embed.AddField("Channel", $"{message.Channel.Mention} in {message.Channel.Guild.Name} | [Jump Link]({message.JumpLink})");

                await member.SendMessageAsync(embed);
            }
        }
    }
}
