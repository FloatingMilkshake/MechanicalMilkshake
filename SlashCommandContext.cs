namespace MechanicalMilkshake;

// This is DSharpPlus' SlashCommandContext with some modifications to force ephemeral responses for commands used in a user-app context.
// https://github.com/DSharpPlus/DSharpPlus/blob/f42e6b5/DSharpPlus.Commands/Processors/SlashCommands/SlashCommandContext.cs

public record SlashCommandContext : DSharpPlus.Commands.Processors.SlashCommands.SlashCommandContext
{
    public new ValueTask RespondAsync(string content) => RespondAsync(new DiscordMessageBuilder().WithContent(content));

    public new ValueTask RespondAsync(string content, bool ephemeral) => RespondAsync(new DiscordInteractionResponseBuilder().WithContent(content).AsEphemeral(DetermineResponsePrivacy(ephemeral)));

    public new ValueTask RespondAsync(DiscordEmbed embed, bool ephemeral) => RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(DetermineResponsePrivacy(ephemeral)));

    public new ValueTask RespondAsync(string content, DiscordEmbed embed, bool ephemeral) => RespondAsync(new DiscordInteractionResponseBuilder()
        .WithContent(content)
        .AddEmbed(embed)
        .AsEphemeral(DetermineResponsePrivacy(ephemeral)));

    public new async ValueTask RespondAsync(IDiscordMessageBuilder builder)
    {
        if (this.Interaction.ResponseState is DiscordInteractionResponseState.Replied)
        {
            throw new InvalidOperationException("Cannot respond to an interaction twice. Please use FollowupAsync instead.");
        }

        DiscordInteractionResponseBuilder interactionBuilder = builder as DiscordInteractionResponseBuilder ?? new(builder);

        // Don't ping anyone if no mentions are explicitly set
        if (interactionBuilder.Mentions.Count is 0)
        {
            interactionBuilder.AddMentions(Mentions.None);
        }

        interactionBuilder.AsEphemeral(DetermineResponsePrivacy());

        if (this.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
        {
            await this.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, interactionBuilder);
        }
        else if (this.Interaction.ResponseState is DiscordInteractionResponseState.Deferred)
        {
            await this.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder(interactionBuilder));
        }
    }

    public new ValueTask DeferResponseAsync() => DeferResponseAsync(DetermineResponsePrivacy(false));

    public new async ValueTask DeferResponseAsync(bool ephemeral) => await this.Interaction.DeferAsync(DetermineResponsePrivacy(ephemeral));

    public new async ValueTask<DiscordMessage> EditResponseAsync(IDiscordMessageBuilder builder) =>
        await this.Interaction.EditOriginalResponseAsync(builder as DiscordWebhookBuilder ?? new(builder));

    public new ValueTask<DiscordMessage> FollowupAsync(string content, bool ephemeral) =>
        FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(content).AsEphemeral(DetermineResponsePrivacy(ephemeral)));

    public new ValueTask<DiscordMessage> FollowupAsync(DiscordEmbed embed, bool ephemeral) =>
        FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral(DetermineResponsePrivacy(ephemeral)));

    public new ValueTask<DiscordMessage> FollowupAsync(string content, DiscordEmbed embed, bool ephemeral) => FollowupAsync(new DiscordFollowupMessageBuilder()
        .WithContent(content)
        .AddEmbed(embed)
        .AsEphemeral(DetermineResponsePrivacy(ephemeral)));

    public new async ValueTask<DiscordMessage> FollowupAsync(IDiscordMessageBuilder builder)
    {
        DiscordFollowupMessageBuilder followupBuilder = builder is DiscordFollowupMessageBuilder messageBuilder
            ? messageBuilder
            : new DiscordFollowupMessageBuilder(builder);

        DiscordMessage message = await this.Interaction.CreateFollowupMessageAsync(followupBuilder);
        this.followupMessages.Add(message.Id, message);
        return message;
    }

    private bool DetermineResponsePrivacy(bool ephemeral = false)
    {
        return Interaction.Guild is null || ephemeral;
    }
}