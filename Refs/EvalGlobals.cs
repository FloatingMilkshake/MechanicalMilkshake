namespace MechanicalMilkshake.Refs;

public class Globals
{
    public DiscordClient Client;

    public Globals(DiscordClient client, InteractionContext ctx)
    {
        Client = client;
        Channel = ctx.Channel;
        Guild = ctx.Guild;
        User = ctx.User;
        if (Guild != null)
            Member = Guild.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();

        Context = ctx;
    }

    public DiscordMessage Message { get; set; }
    public DiscordChannel Channel { get; set; }
    public DiscordGuild Guild { get; set; }
    public DiscordUser User { get; set; }
    public DiscordMember Member { get; set; }
    public InteractionContext Context { get; set; }
}