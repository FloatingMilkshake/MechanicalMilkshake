namespace MechanicalMilkshake.Setup;

public static class Eval
{
    internal static readonly List<string> RestrictedTerms = ["poweroff", "shutdown", "reboot", "halt"];
    internal static readonly string[] Imports = ["System", "System.Collections.Generic", "System.Linq",
            "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.Commands",
            "DSharpPlus.Interactivity", "DSharpPlus.Entities", "Microsoft.Extensions.Logging",
            "MechanicalMilkshake", "MechanicalMilkshake.Setup.Eval", "MechanicalMilkshake.Setup.Eval.Utilities"];

    public class Globals
    {
        internal Globals(DiscordClient client, SlashCommandContext ctx, CancellationToken ctoken)
        {
            Context = ctx;
            Client = client;
            Channel = ctx.Channel;
            Guild = ctx.Guild;
            User = ctx.User;
            if (Guild is not null) Member = Guild.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();
            CToken = ctoken;
        }

        public DiscordClient Client { get; private set; }
        public DiscordMessage Message { get; private set; }
        public DiscordChannel Channel { get; private set; }
        public DiscordGuild Guild { get; private set; }
        public DiscordUser User { get; private set; }
        public DiscordMember Member { get; private set; }
        public SlashCommandContext Context { get; private set; }
        public CancellationToken CToken { get; private set; }
    }

    public static class Utilities
    {
        public static string Jsonify(object input)
        {
            if (input is null)
                return null;
            return $"```json\n{JsonConvert.SerializeObject(input, Formatting.Indented)}\n```";
        }

        public static async Task<string> ListGuildRolesAsync(DiscordGuild guild)
        {
            return string.Join("\n", guild.Roles.Values.OrderByDescending(r => r.Position).Select(r => r.Name));
        }

        public static async Task<string> ListGuildChannelsAsync(DiscordGuild guild)
        {
            var sortedChannelsWithoutCategories = guild.Channels.Values.Where(ch => !ch.IsCategory).OrderBy(ch => ch.Position);

            List<long> listedParents = [];
            string output = "";
            foreach (DiscordChannel channel in sortedChannelsWithoutCategories)
            {
                long parentId = channel.ParentId is null ? -1 : (long)channel.ParentId.Value;
                if (!listedParents.Contains(parentId))
                {
                    listedParents.Add(parentId);
                    if (parentId != -1)
                        output += $"**{guild.Channels[(ulong)parentId]}**\n";
                }

                output += channel.ToString() + "\n";
            }

            return output;
        }
    }
}
