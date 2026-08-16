# Configuration

## Environment Variables

Here are explanations for each of the settings configured via enviromnent variables:

| Key                           | Required? | What it is                                                                                                          |
| BOT_TOKEN                     | Yes       | The token for your bot. Get this from the Discord Developer Dashboard: https://discord.com/developers/applications  |
| WOLFRAM_ALPHA_APP_ID          | No        | Your App ID from WolframAlpha. This is like an API key, and is required for the `/wolframalpha` command.            |
| UPTIME_KUMA_HEARTBEAT_URL     | No        | An Uptime Kuma heartbeat URL for a "push" type monitor, if you want to use Uptime Kuma to monitor the bot's uptime. |
| DBOTS_API_TOKEN               | No        | Your API key for the Discord Bots bot list. Required if you want to send statistics.                                |

## config.json

Here are explanations for each of the settings in `config.json`:

| Key                           | Required? | What it is                                                                                                                                                                                                                                                                                                    |
|-------------------------------|-----------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| homeChannel                   | Yes       | The ID of the channel where your bot will send messages at startup, like this one: https://cdn.floatingmilkshake.com/mechanicalmilkshake/startup_message.png. Some errors are also sent here.                                                                                                                 |
| homeServer                    | Yes       | Owner commands are only available in this server. Additionally, slash commands are registered only for this server if the bot is running in debug mode.                                                                                                                                                       |
| grafanaLokiUrl                | No        | The URL of a Grafana Loki instance to send logs to. If you don't know what Grafana Loki is, you can ignore this.                                                                                                                                                                                              |
| botCommanders                 | No        | A list of users (use user IDs) authorized to run owner commands.                                                                                                                                                                                                                                              |
| useServerSpecificFeatures     | No        | Whether to use server-specific features, found in `ServerSpecificFeatures.cs`. Most likely you will not use this.                                                                                                                                                                                             |
| feedbackChannel               | No        | The ID of the channel that `/feedback` sends feedback into.                                                                                                                                                                                                                                                   |
| rateLimitCautionChannels      | No        | A list of channels that the bot will try to reduce API requests in when processing keyword tracking. If your server has any particularly spammy channels and you use keyword tracking, you may want to put those channels' IDs here. This might reduce the reliability of keyword tracking in these channels. |
| slashCommandLogChannel        | No        | The ID of the channel to log slash command usage to.                                                                                                                                                                                                                                                          |
| slashCommandLogExcludedGuilds | No        | IDs for servers you wish to exclude from slash command logs.                                                                                                                                                                                                                                                  |
| guildLogChannel               | No        | The ID of the channel to log guild joins and leaves to.                                                                                                                                                                                                                                                       |
| doDbotsStatsPosting           | No        | Whether to send statistics about the bots to the Discord Bots (https://discord.bots.gg) bot list.                                                                                                                                                                                                             |
