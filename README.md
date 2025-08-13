# MechanicalMilkshake
A multipurpose Discord bot.

## How can I add the bot to my server?

There are two ways you can do this!

### Adding the Public Bot
The first and easiest way to get the bot into one of your servers is by adding it to your server with [this link](https://discord.com/api/oauth2/authorize?client_id=863140071980924958&permissions=1099847182358&scope=applications.commands%20bot)!

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
- In `config.json`, provide values for at least `botToken`, `homeChannel` and `homeServer`. Other settings are optional, but some functionality may not work without them. If you're not sure about something, see the [configuration documentation](CONFIG.md) or feel free to contact me!
- Run `docker compose up -d`

### Without Docker (for development)
This is the way to go if you intend on working on development. Note that you will need to have Redis installed to run the bot this way - if you do not already have it installed, I recommend [this guide](https://redis.io/docs/latest/operate/oss_and_stack/install/archive/install-redis/install-redis-on-linux/) for Linux, [this guide](https://redis.io/docs/latest/operate/oss_and_stack/install/archive/install-redis/install-redis-on-mac-os/) for macOS, and [this port](https://github.com/tporadowski/redis) for Windows. Once you have Redis installed:

- Clone the repo
- Copy `config.example.json` to `config.dev.json`
- In `config.json`, provide values for at least `botToken`, `homeChannel` and `homeServer`. Other settings are optional, but some functionality may not work without them. If you're not sure about something, see the [configuration documentation](CONFIG.md) or feel free to contact me!
- Make sure Redis is running (probably `sudo systemctl start redis-server` on Linux, `brew services start redis` on macOS, or `sc start redis` on Windows)
- Run the bot with your IDE

## Contributing
Feel free to open an issue or PR if you notice anything that seems wrong, or if you have a suggestion! I'm all ears. However, please note that I have some [Contribution Guidelines](CONTRIBUTING.md).

## Special Thanks
Without these people, I wouldn't be where I am today, and this project wouldn't be here.
- [Erisa](https://erisa.uk/) for the help and the answers to all my questions - especially when I was first starting out with this. You've played a huge role in helping me learn more about this and I really, really appreciate it. Thank you so much. ♥
- [auravoid](https://aura.is-a.dev/) for the feedback, contributions, and ideas; and for helping to further my interest in technology over the years (and for basically being my personal QA tester :P). Thank you! ♥
