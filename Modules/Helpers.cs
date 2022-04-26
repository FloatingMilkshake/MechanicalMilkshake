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
    }
}
