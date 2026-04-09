namespace MechanicalMilkshake.Attributes;

internal class RequireHomeServerAttribute : ContextCheckAttribute;

internal class RequireHomeServerCheck : IContextCheck<RequireHomeServerAttribute>
{
    public ValueTask<string> ExecuteCheckAsync(RequireHomeServerAttribute _, CommandContext ctx) =>
        ValueTask.FromResult(Setup.Configuration.ConfigJson.BotCommanders.Contains(ctx.User.Id.ToString())
            ? null
            : "This command cannot be registered outside of the home server.");
}
