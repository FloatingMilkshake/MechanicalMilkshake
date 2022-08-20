# MechanicalMilkshake
A multipurpose Discord bot, written in C# with [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)! (It also happens to be my first bot! Things have progressed quite a bit since I started!)

Note that I'm not developing this bot with any particular features in mind (for example, moderation tools). Some features of this bot are somewhat specific and might not be useful to you, but I add them because they're useful to me or others. Feel free to suggest a feature if you want something the bot doesn't already have!

Huge thank you to [Erisa](https://github.com/Erisa) for putting up with my constant questions as I figured things out! You have no idea how much I appreciate it. ♥ (go check out Erisa's stuff she's awesome)

## How can I add the bot to my server?

There are two ways you can do this!

### Adding the Public Bot
The first and easiest way to get the bot into one of your servers is by adding it to your server with [this link](https://discord.com/api/oauth2/authorize?client_id=863140071980924958&permissions=1099847182358&scope=applications.commands%20bot)!

Note that if you want to play around with owner commands (`/debug`, `/link`, `/cdn`, etc.), you will not be able to do that this way.

### Running the Bot Yourself
This is the other way to get the bot into one of your servers, and is best for development. It also allows you to access all commands. Instructions are below.

> ### Note
> These instructions assume you know how to create a bot application through Discord's Developer Portal and how to obtain a bot token, channel ID, etc. If you need help with any of this, feel free to reach out to me with any of the contact methods listed on [my website](https://floatingmilkshake.com)!

- Clone the repo
- Copy `config.example.json` to `config.json`
- In `config.json`, provide values for at least `botToken`, `homeChannel` and `homeServerId`. Other values are optional, but some functionality may not work without them (the bot should tell you what's missing though if you try to use a feature that requires a value you didn't set).
- Run `docker-compose up -d`

## `config.json` Keys
Here are explanations for each of the keys in `config.json`. If you need a more detailed explanation on what a key is for or how to get the right value for it, feel free to reach out.

| Key                             | What it is                                                                                                                                                                                                             |
| ------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| botToken                        | The token for your bot.                                                                                                                                                                                                |
| homeChannel                     | The ID of the channel where your bot will send messages at startup, like [this one](https://cdn.floatingmilkshake.com/o3QvkP5PES.png).                                                                                 |
| homeServerId                     | Owner commands are only available in this server. Additionally, slash commands are registered only for this server if you're developing the bot, so you don't have to wait an hour for Discord to cache them.          |
| wolframAlphaAppId               | Your App ID from WolframAlpha. This is like an API key, and is required for the `/wolframalpha` command.                                                                                                               |
| **workerLinks**                 | These values are used for interacting with short links created with [Erisa](https://github.com/Erisa)'s [worker-links](https://github.com/Erisa/worker-links).                                                         |
| **workerLinks** / baseUrl       | Your worker-links base URL. This should look something like `https://link.floatingmilkshake.com`.                                                                                                                      |
| **workerLinks** / secret        | Your worker-links secret.                                                                                                                                                                                              |
| **workerLinks** / namespaceId   | The Namespace ID of the KV namespace you use to store your short links.                                                                                                                                                |
| **workerLinks** / apiKey        | Your Cloudflare API key.                                                                                                                                                                                               |
| **workerLinks** / accountId     | Your Cloudflare Account ID.                                                                                                                                                                                            |
| **workerLinks** / email         | The email address associated with your Cloudflare account. Used to authenticate for worker-links.                                                                                                                      |
| **s3**                          | These values are used for interacting with an Amazon S3-compatible storage service to store files.                                                                                                                     |
| **s3** / bucket                 | The name of the S3 bucket you'd like the bot to use. This should look something like `cdn.floatingmilkshake.com`.                                                                                                      |
| **s3** / cdnBaseUrl             | The base URL for your S3 bucket. This should look something like `https://cdn.floatingmilkshake.com`.                                                                                                                  |
| **s3** / endpoint               | The endpoint URL for your S3 bucket. This can vary. I use Scaleway Elements and mine looks like this: `s3.fr-par.scw.cloud`                                                                                            |
| **s3** / accessKey              | The Access Key for your S3 bucket.                                                                                                                                                                                     |
| **s3** / secretKey              | The Secret Key for your S3 bucket.                                                                                                                                                                                     |
| **s3** / region                 | The region for your S3 bucket. This should look something like `fr-par`, `us-east`, etc.                                                                                                                               |
| **cloudflare**                  | These values are used for clearing Cloudflare's cache of files you delete from an S3 bucket with the bot.                                                                                                              |
| **cloudflare** / urlPrefix      | The URL prefix for your S3 bucket. This should look the same as your `s3 / cdnBaseUrl` (so something like `https://cdn.floatingmilkshake.com`).                                                                        |
| **cloudflare** / token          | A global API token for your Cloudflare account. For some reason a global API token is required to clear this cache, instead of a more specific API key which is what's used for worker-links.                          |
| authorizedUsers                 | An array of users (user IDs) authorized to run Owner commands. There can be as many users here as you'd like.                                                                                                          |
| sshHosts                        | An array of servers (format is user@ip-address) to automatically check for and notify of package updates. Only works with distributions that use apt for now. Checks for updates every 6 hours.                        |

## Contributing
I'm not the best at this, so feel free to open an issue or PR if you notice anything that seems wrong or if you have a suggestion! I'm all ears. However, please note that I have some [Contribution Guidelines](CONTRIBUTING.md).
