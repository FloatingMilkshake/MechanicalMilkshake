namespace MechanicalMilkshake.Refs;

public class Globals
{
    // ReSharper disable once SuggestBaseTypeForParameterInConstructor
    public Globals(InteractionContext ctx)
    {
        Guild = ctx.Guild;
        User = ctx.User;
        Guild?.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private DiscordMessage Message { get; set; }
    private DiscordGuild Guild { get; }
    private DiscordUser User { get; }
}