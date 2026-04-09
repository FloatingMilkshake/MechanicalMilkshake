namespace MechanicalMilkshake.Attributes;

internal class RequireBotCommanderAttribute : ContextCheckAttribute;

internal class RequireBotCommanderCheck : IContextCheck<RequireBotCommanderAttribute>
{
    public ValueTask<string> ExecuteCheckAsync(RequireBotCommanderAttribute _, CommandContext ctx) =>
        ValueTask.FromResult(Setup.Configuration.ConfigJson.BotCommanders.Contains(ctx.User.Id.ToString())
            ? null
            : "The user is not authorized to use this command.");
}
