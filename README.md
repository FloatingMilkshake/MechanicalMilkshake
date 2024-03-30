# MechanicalMilkshake
A multipurpose Discord bot.

Note that I'm not developing this bot with any particular features in mind (for example, moderation tools). Also, some features of this bot are somewhat specific and might not be useful to you, but I add them because they're useful to me or others. Feel free to suggest a feature if you want something the bot doesn't already have! If it's not super complicated, I'm happy to add whatever features you might want.

## How can I add the bot to my server?

There are two ways you can do this!

### Adding the Public Bot
The first and easiest way to get the bot into one of your servers is by adding it to your server with [this link](https://discord.com/api/oauth2/authorize?client_id=863140071980924958&permissions=1099847182358&scope=applications.commands%20bot)!

Note that if you want to play around with owner commands (`/debug`, `/link`, `/cdn`, etc.), you will not be able to do that this way.

### Running the Bot Yourself
This is the other way to get the bot into one of your servers, and is best for development. It also allows you to access all commands or use some commands with your own configuration. Instructions are below!

## Setup/Usage

> ### Note
> These instructions assume you know how to create a bot application through Discord's Developer Portal and how to obtain a bot token, channel ID, etc. If you need help with any of this, feel free to reach out to me with any of the contact methods listed on [my website](https://floatingmilkshake.com)!

### With Docker
This is the easiest way to run the bot yourself if you just want to run it and don't need to work on development.

First, you must have Docker installed. If you do not have it installed already, follow the instructions [here](https://docs.docker.com/engine/install/) to install Docker. Once Docker is installed:

- Clone the repo
- Copy `config.example.json` to `config.json`
- In `config.json`, provide values for at least `botToken`, `homeChannel` and `homeServerId`. Other values are optional, but some functionality may not work without them (the bot should tell you what's missing though if you try to use a feature that requires a value you didn't set). If you're not sure about a value, see [the wiki page on config.json](https://github.com/FloatingMilkshake/MechanicalMilkshake/wiki/Configuration#configjson) or feel free to contact me!
- In `docker-compose.yml`, comment out or adjust the [bind mount](https://github.com/FloatingMilkshake/MechanicalMilkshake/blob/main/docker-compose.yml#L17-L20) for `id_ed25519` if necessary (for example, if you will not be utilizing the package update check feature of the bot, or if you do not use an SSH key for this or use one in a different format)
- Run `docker-compose up -d`

(If you see an error about `docker-compose` not being recognized as a command, try removing the hyphen (so `docker compose up -d`). If you're still having issues, you may need to [install Docker Compose](https://docs.docker.com/compose/install/) separately.)

### Without Docker (for development)
This is the way to go if you intend on working on development. Note that you will need to have Redis installed to run the bot this way - if you do not already have it installed, I recommend [this guide](https://redis.io/docs/getting-started/installation/install-redis-on-linux) for Linux, [this guide](https://redis.io/docs/getting-started/installation/install-redis-on-mac-os) for macOS, and [this port](https://github.com/tporadowski/redis) for Windows. Once you have Redis installed:

- Clone the repo
- Copy `config.example.json` to `config.dev.json`
- In `config.dev.json`, provide values for at least `botToken`, `homeChannel` and `homeServerId` (other values are optional, but some functionality may not work without them - see the [wiki page](https://github.com/FloatingMilkshake/MechanicalMilkshake/wiki/Configuration#configjson) for more information on config values)
- Make sure Redis is running (you may need to run a command in your terminal)
- Run the bot with your IDE

## Contributing
I'm not the best at this, so feel free to open an issue or PR if you notice anything that seems wrong or if you have a suggestion! I'm all ears. However, please note that I have some [Contribution Guidelines](CONTRIBUTING.md).

## Credits

### Dependencies
This bot depends on a few projects and services to provide the features that it has. They are listed below!
- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus/)
- [Redis](https://redis.io/)
- [WolframAlpha](https://products.wolframalpha.com/api/)
- [Minio](https://github.com/minio/minio-dotnet/)
- [HumanDateParser](https://github.com/jacksonrakena/human-date-parser/)
- [HTTP Cats API](https://http.cat/)
- [Random Useless Facts API](https://uselessfacts.jsph.pl/)
- [The Cat API](https://thecatapi.com/)
- [Dog CEO's Dog API](https://dog.ceo/dog-api/)

### Special Thanks
Without these people I wouldn't be where I am today, and this project wouldn't be here.
- [Erisa](https://erisa.uk/) for the help and the answers to all my questions - especially when I was first starting out with this. You've played a huge role in helping me learn more about this and I really, really appreciate it. Thank you so much. ♥
- [auravoid](https://auravoid.dev/) for the feedback, contributions, and ideas; and for helping to further my interest in technology over the years (and for basically being my personal QA tester :P). Thank you!