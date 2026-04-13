namespace MechanicalMilkshake.Setup;

public static class Eval
{
    internal static readonly List<string> RestrictedTerms = ["poweroff", "shutdown", "reboot", "halt"];
    internal static readonly string[] Imports = ["System", "System.Collections.Generic", "System.Linq",
            "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.Commands",
            "DSharpPlus.Interactivity", "DSharpPlus.Entities", "Microsoft.Extensions.Logging",
            "MechanicalMilkshake"];

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

        private Globals() { }

        public DiscordClient Client { get; private set; }
        public DiscordMessage Message { get; private set; }
        public DiscordChannel Channel { get; private set; }
        public DiscordGuild Guild { get; private set; }
        public DiscordUser User { get; private set; }
        public DiscordMember Member { get; private set; }
        public SlashCommandContext Context { get; private set; }
        public CancellationToken CToken { get; private set; }
    }
}
