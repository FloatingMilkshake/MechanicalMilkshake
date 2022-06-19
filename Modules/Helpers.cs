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

        public static async Task KeywordCheck(MessageCreateEventArgs e)
        {
            if (e.Author.Id == 455432936339144705)
                return;
            else if (e.Message.Content.Contains("floaty"))
                await SendAlert("floaty", e);
            else if (e.Message.Content.Contains("milkshake"))
                await SendAlert("milkshake", e);


            static async Task SendAlert(string keyword, MessageCreateEventArgs e)
            {
                var guild = await Program.discord.GetGuildAsync(799644062973427743);
                var member = await guild.GetMemberAsync(455432936339144705);

                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor("#7287fd"),
                    Title = $"Tracked keyword \"{keyword}\" triggered!",
                    Description = $"{e.Message.Content}"
                };
                embed.AddField("Author ID", $"{e.Author.Id}", true);
                embed.AddField("Author Mention", $"{e.Author.Mention}", true);
                embed.AddField("Channel", $"{e.Channel.Mention} in {e.Guild.Name} | [Jump Link]({e.Message.JumpLink})");

                await member.SendMessageAsync(embed);
            }
        }
    }
}
