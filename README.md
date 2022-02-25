# MechanicalMilkshake
My first Discord bot, written in C# with [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)!

Note that this is a multipurpose bot. I'm not developing it with any particular features in mind (for example, moderation tools).

Huge thank you to [Erisa](https://github.com/Erisa) for putting up with my constant questions as I figure things out! You have no idea how much I appreciate it. ♥

## Running The Bot

> These instructions assume you know how to create a bot application through Discord's Developer Portal and how to obtain a bot token, channel ID, etc. If you need help with any of this, feel free to reach out to me with any of the contact methods listed on [my website](https://floatingmilkshake.com)!

- Clone the repo
- Copy `config.example.json` to `config.json`
- In `config.json`, provide values for at least `botToken` and `homeChannel`. Other variables are optional, but some functionality may not work without them (the bot should tell you what's missing though if I did things right).
- Run `docker-compose up -d`

## `config.json` Keys
| Key                             | What it is                                                                                                                                                                                                             |
| ------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| botToken                        | The token for your bot.                                                                                                                                                                                                |
| homeChannel                     | The ID of the channel where your bot will send messages at startup, like [this one](https://cdn.floatingmilkshake.com/o3QvkP5PES.png).                                                                                 |
| devServerId                     | If you have a server you use for development, its ID goes here. This is used to register slash commands only for that server if you're developing the bot so you don't have to wait an hour for Discord to cache them. |
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
| authorizedUsers                 | An array of users (user IDs) authorized to run certain commands. Currently this is only used for `/runcommand`. There can be as many users here as you'd like.                                                         |

## Contributing
I'm not the best at this, so feel free to open an issue or PR if you notice anything that seems wrong or if you have a suggestion! I'm all ears. However, please note that I have some [Contribution Guidelines](CONTRIBUTING.md).
