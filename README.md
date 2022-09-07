# MechanicalMilkshake
A multipurpose Discord bot, written in C# with [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)! (It also happens to be my first bot! Things have progressed quite a bit since I started!)

Note that I'm not developing this bot with any particular features in mind (for example, moderation tools). Also, some features of this bot are somewhat specific and might not be useful to you, but I add them because they're useful to me or others. Feel free to suggest a feature if you want something the bot doesn't already have! If it's not super complicated, I'm happy to add whatever features you might want.

## Credits
I think providing credit is important, especially for a project like this that holds a lot of value to me. So I've decided to put the credits at the top of this readme. Here they are.

### Dependencies
This bot depends on a few projects and services to provide the features that it has. They are listed below!
- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus/)
- [WolframAlpha](https://products.wolframalpha.com/api/)
- [Minio](https://github.com/minio/minio-dotnet/)

### Special Thanks
Without these people I wouldn't be where I am today, and this project wouldn't be here.
- [Erisa](https://erisa.uk/) for the help and the answers to all my questions - especially when I was first starting out with this. You've played a huge role in helping me learn more about this and I really, really appreciate it. Thank you so much. ♥
- [auravoid](https://auravoid.dev/) for the sponsorship, feedback, and ideas; and for helping to further my interest in technology over the years. Thank you!

## How can I add the bot to my server?

There are two ways you can do this!

### Adding the Public Bot
The first and easiest way to get the bot into one of your servers is by adding it to your server with [this link](https://discord.com/api/oauth2/authorize?client_id=863140071980924958&permissions=1099847182358&scope=applications.commands%20bot)!

Note that if you want to play around with owner commands (`/debug`, `/link`, `/cdn`, etc.), you will not be able to do that this way.

### Running the Bot Yourself
This is the other way to get the bot into one of your servers, and is best for development. It also allows you to access all commands or use some commands with your own configuration. Instructions are below.

> ### Note
> These instructions assume you know how to create a bot application through Discord's Developer Portal and how to obtain a bot token, channel ID, etc. If you need help with any of this, feel free to reach out to me with any of the contact methods listed on [my website](https://floatingmilkshake.com)!
> 
> Also - if you notice the bot throwing errors for `PerServerFeatures.cs` - the easiest way to get around that is probably to remove the file and any references to it. There are some features that are hardcoded for specific servers/channels, so I figured I would isolate them to a single file. Sorry if this is confusing! I'm happy to help out if there's trouble.

- Clone the repo
- Copy `config.example.json` to `config.json`
- In `config.json`, provide values for at least `botToken`, `homeChannel` and `homeServerId`. Other values are optional, but some functionality may not work without them (the bot should tell you what's missing though if you try to use a feature that requires a value you didn't set). If you're not sure about a value, see [the wiki page on config.json](https://github.com/FloatingMilkshake/MechanicalMilkshake/wiki/Configuration#configjson) or feel free to contact me!
- Run `docker-compose up -d`

## Contributing
I'm not the best at this, so feel free to open an issue or PR if you notice anything that seems wrong or if you have a suggestion! I'm all ears. However, please note that I have some [Contribution Guidelines](CONTRIBUTING.md).