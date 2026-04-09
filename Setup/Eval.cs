namespace MechanicalMilkshake.Setup;

public class Eval
{
    internal static readonly List<string> RestrictedTerms = ["poweroff", "shutdown", "reboot", "halt"];
    internal static readonly string[] Imports = ["System", "System.Collections.Generic", "System.Linq",
            "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.Commands",
            "DSharpPlus.Interactivity", "DSharpPlus.Entities", "Microsoft.Extensions.Logging",
            Assembly.GetExecutingAssembly().GetName().Name];

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

        public DiscordClient Client { get; set; }
        public DiscordMessage Message { get; set; }
        public DiscordChannel Channel { get; set; }
        public DiscordGuild Guild { get; set; }
        public DiscordUser User { get; set; }
        public DiscordMember Member { get; set; }
        public SlashCommandContext Context { get; set; }
        public CancellationToken CToken { get; set; }
    }
}
