﻿namespace MechanicalMilkshake.Commands;

public class UserInfo : ApplicationCommandModule
{
    [SlashCommand("userinfo", "Returns information about the provided server member.")]
    public static async Task UserInfoCommand(InteractionContext ctx,
        [Option("user", "The user to look up information for. Defaults to yourself.")]
        DiscordUser user = null)
    {
        DiscordEmbed userInfoEmbed;

        user ??= ctx.User;

        try
        {
            if (ctx.Guild is not null)
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);
                userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(member);
            }
            else
            {
                userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(user);
            }
        }
        catch (NotFoundException)
        {
            userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(user);
        }

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"User Info for **{UserInfoHelpers.GetFullUsername(user)}**").AddEmbed(userInfoEmbed));
    }
}