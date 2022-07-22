namespace MechanicalMilkshake.Modules
{
    public class Checks
    {
        public class PerServerFeatures
        {
            public static async Task WednesdayCheck()
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] WednesdayCheck running.");
#endif
                if (DateTime.Now.DayOfWeek != DayOfWeek.Wednesday)
                {
                    return;
                }
                else if (!DateTime.Now.ToShortTimeString().Contains("10:00"))
                {
                    return;
                }

                try
                {
                    DiscordChannel channel = await Program.discord.GetChannelAsync(874488354786394192);

                }
                catch (Exception e)
                {
                    Console.WriteLine($"An error occurred! Details: {e}");
                    return;
                }
            }

            public static async Task PizzaTime()
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] PizzaTime running.");
#endif
                if (!DateTime.Now.ToShortTimeString().Contains("12:00"))
                {
                    return;
                }

                try
                {
                    DiscordChannel channel = await Program.discord.GetChannelAsync(932768798224838778);
                    await channel.SendMessageAsync("https://cdn.discordapp.com/attachments/932768798224838778/932768814284812298/IMG_9147.png");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An error occurred! Details: {e}");
                    return;
                }
            }
        }

        public static async Task PackageUpdateCheck()
        {
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] PackageUpdateCheck running.");
#endif
            string updatesAvailableResponse = "";
            string restartRequiredResponse = "A system restart is required to complete package updates.";

            Owner.Private ownerPrivate = new();
            bool updatesAvailable = false;
            bool restartRequired = false;
            foreach (string host in Program.configjson.SshHosts)
            {
#if DEBUG
                Console.WriteLine($"[{DateTime.Now}] [PackageUpdateCheck] Checking for updates on host '{host}'.");
#endif
                string cmdResult = await ownerPrivate.RunCommand($"ssh {host} \"sudo apt update\"");
                if (cmdResult.Contains("packages can be upgraded"))
                {
                    updatesAvailableResponse += $"`{host}`\n";
                    updatesAvailable = true;
                }
                if (cmdResult.Contains("System restart required"))
                {
                    restartRequired = true;
                }
            }
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] [PackageUpdateCheck] Finished checking for updates on all hosts.");
#endif

            if (updatesAvailable || restartRequired)
            {
                if (updatesAvailable)
                {
                    updatesAvailableResponse = "Package updates are available on the following hosts:\n" + updatesAvailableResponse;
                }
                string ownerMention = "";
                foreach (DiscordUser user in Program.discord.CurrentApplication.Owners)
                {
                    ownerMention += user.Mention + " ";
                }

                string response = updatesAvailableResponse + restartRequiredResponse;
                await Program.homeChannel.SendMessageAsync($"{ownerMention.Trim()}\n{response}");
            }
        }
    }
}
