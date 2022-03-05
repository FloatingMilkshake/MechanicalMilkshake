using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using MechanicalMilkshake.Modules;
using Minio;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MechanicalMilkshake
{
    class Program
    {
        public static DiscordClient discord;
        public static MinioClient minio;
        public static Random random = new();
        public static DateTime connectTime;
        public static HttpClient httpClient = new();
        public static ConfigJson configjson;
        public static readonly string processStartTime = DateTime.Now.ToString();

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public class CustomHelpFormatter : BaseHelpFormatter
        {
            protected DiscordEmbedBuilder _embed;

            public CustomHelpFormatter(CommandContext ctx) : base(ctx)
            {
                _embed = new DiscordEmbedBuilder();
            }

            public override BaseHelpFormatter WithCommand(Command command)
            {
                return this;
            }

            public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> cmds)
            {
                _embed.Title = "Help";
                _embed.Description = $"No commands are available with this prefix.\n\nAll commands have been moved to slash (`/`) commands. Type `/` and select {discord.CurrentUser.Username} to see available commands.";
                _embed.Color = DiscordColor.Red;

                return this;
            }

            public override CommandHelpMessage Build()
            {
                return new CommandHelpMessage(embed: _embed);
            }
        }

        internal static async Task MainAsync()
        {
            var json = "";

            string configFile = "config.json";
#if DEBUG
            configFile = "config.dev.json";
#endif

            using (var fs = File.OpenRead(configFile))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            configjson = JsonConvert.DeserializeObject<ConfigJson>(json);

            if (configjson.BotToken == null || configjson.BotToken == "")
            {
                Console.WriteLine("ERROR: No token provided! Make sure the botToken field in your config.json file is set.");
                Environment.Exit(1);
            }

            discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = configjson.BotToken,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All
            });
            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30)
            });

            var slash = discord.UseSlashCommands();

            slash.SlashCommandErrored += async (SlashCommandsExtension scmds, SlashCommandErrorEventArgs e) =>
            {
                if (e.Exception is SlashExecutionChecksFailedException)
                {
                    await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Hmm, it looks like one of the checks for this command failed. Make sure you and I both have the permissions required to use it, and that you're using it properly. Contact the bot owner if you need help or think I messed up.").AsEphemeral(true));
                    return;
                }

                List<Exception> exs = new();
                if (e.Exception is AggregateException ae)
                    exs.AddRange(ae.InnerExceptions);
                else
                    exs.Add(e.Exception);

                foreach (Exception ex in exs)
                {
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "An exception occurred when executing a slash command",
                        Description = $"`{ex.GetType()}` occurred when executing `{e.Context.CommandName}`.",
                        Timestamp = DateTime.UtcNow
                    };
                    embed.AddField("Message", ex.Message);
                    embed.AddField("Debug Info", $"If you'd like to contact the bot owner about this, include this debug info:\n```{ex}\n```");

                    // I don't know how to tell whether the command response was deferred or not, so we're going to try both an interaction response and follow-up so that the interaction doesn't time-out.
                    try
                    {
                        await e.Context.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()).AsEphemeral(true));
                    }
                    catch
                    {
                        await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed.Build()).AsEphemeral(true));
                    }
                }
            };

            CommandsNextExtension commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "!", "~", "-", "mm" }
            });

            commands.SetHelpFormatter<CustomHelpFormatter>();

            minio = new MinioClient
            (
                configjson.S3.Endpoint,
                configjson.S3.AccessKey,
                configjson.S3.SecretKey,
                configjson.S3.Region
            ).WithSSL();

            // Most of the code for this exception handler is from Erisa's Cliptok: https://github.com/Erisa/Cliptok/blob/aabf8aa/Program.cs#L488-L527

            async Task CommandsNextService_CommandErrored(CommandsNextExtension cnext, CommandErrorEventArgs e)
            {
                if (e.Exception is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                    return;

                List<Exception> exs = new();
                if (e.Exception is AggregateException ae)
                    exs.AddRange(ae.InnerExceptions);
                else
                    exs.Add(e.Exception);

                foreach (Exception ex in exs)
                {
                    if (ex is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                        return;

                    if (ex is ChecksFailedException)
                    {
                        await e.Context.RespondAsync("Hmm, it looks like one of the checks for this command failed. Make sure you and I both have the permissions required to use it, and that you're using it properly. Contact the bot owner if you need help or think I messed up.");
                        return;
                    }

                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "An exception occurred when executing a command",
                        Description = $"`{ex.GetType()}` occurred when executing `{e.Command.QualifiedName}`.",
                        Timestamp = DateTime.UtcNow
                    };
                    embed.AddField("Message", ex.Message);
                    embed.AddField("Debug Info", $"If you'd like to contact the bot owner about this, include this debug info:\n```{ex}\n```");
                    if (ex.GetType().ToString() == "System.ArgumentException")
                    {
                        embed.AddField("What's that mean?", "This usually means that you used the command incorrectly.\n" +
                            $"Please run `help {e.Command.QualifiedName}` for information about what you need to provide for the `{e.Command.QualifiedName}` command.");
                    }
                    if (e.Command.QualifiedName == "upload")
                    {
                        embed.AddField("Did you forget to include a file name?", "`upload` requires that you specify a name for the file as the first argument. If you'd like to use a randomly-generated file name, use the name `random`.");
                    }
                    if (e.Command.QualifiedName == "tellraw" && ex.GetType().ToString() == "System.ArgumentException")
                    {
                        await e.Context.RespondAsync("An error occurred! Either you did not specify a target channel, or I do not have permission to see that channel.");
                    }
                    if (ex.GetType().ToString() == "System.InvalidOperationException" && ex.Message.Contains("this group is not executable"))
                    {
                        await e.Context.RespondAsync($"Did you mean to run a command inside this group? `{e.Command.QualifiedName}` cannot be run on its own. Try `help {e.Command.QualifiedName}` to see what commands are available.");
                    }
                    else
                    {
                        await e.Context.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
                    }
                }
            }

            discord.ComponentInteractionCreated += async (s, e) =>
            {
                if (e.Id == "shutdown-button")
                {
                    Owner.DebugCmds.ShutdownConfirmed(e.Interaction);
                }
            };

#if DEBUG // Register slash commands for dev server only when debugging in VS; no need to wait an hour for changes to apply
            slash.RegisterCommands<Owner>(configjson.DevServerId);
            slash.RegisterCommands<Fun>(configjson.DevServerId);
            slash.RegisterCommands<Mod>(configjson.DevServerId);
            slash.RegisterCommands<Utility>(configjson.DevServerId);
            Console.WriteLine("Slash commands registered for debugging.");
#else // Register slash commands globally for 'production' bot
            slash.RegisterCommands<Owner>();
            slash.RegisterCommands<Fun>();
            slash.RegisterCommands<Mod>();
            slash.RegisterCommands<Utility>();
            Console.WriteLine("Slash commands registered globally.");
#endif
            commands.RegisterCommands<PerServerFeatures>();
            commands.CommandErrored += CommandsNextService_CommandErrored;

            if (configjson.HomeChannel == null)
            {
                Console.WriteLine("ERROR: No home channel provided! Make sure the homeChannel field in your config.json file is set.");
                Environment.Exit(1);
            }

            ulong homeChannelId = Convert.ToUInt64(configjson.HomeChannel);

            DiscordChannel homeChannel = await discord.GetChannelAsync(homeChannelId);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            async Task OnReady(DiscordClient client, ReadyEventArgs e)
            {
                Task.Run(async () =>
                {
                    connectTime = DateTime.Now;

                    await homeChannel.SendMessageAsync($"Connected!\n{GetDebugInfo()}");
                });
            }

            async Task MessageCreated(DiscordClient client, MessageCreateEventArgs e)
            {
                Task.Run(async () =>
                {
                    if (!e.Channel.IsPrivate)
                        return;

                    if (e.Author.IsCurrent)
                        return;

                    if (client.CurrentApplication.Owners.Contains(e.Author) && e.Message.ReferencedMessage != null && e.Message.ReferencedMessage.Author.IsCurrent && e.Message.ReferencedMessage.Embeds.Count != 0 && e.Message.ReferencedMessage.Embeds[0].Title.Contains("DM received from"))
                    {
                        // If these conditions are true, a bot owner has replied to a forwarded message. Now we need to forward that reply.

                        DiscordEmbedField userIdField = e.Message.ReferencedMessage.Embeds[0].Fields.Where(f => f.Name == "User ID").First();
                        ulong userId = Convert.ToUInt64(userIdField.Value.Replace("`", ""));

                        DiscordEmbedField mutualServersField = e.Message.ReferencedMessage.Embeds[0].Fields.Where(f => f.Name == "Mutual Servers").First();

                        Regex mutualIdPattern = new(@"[0-9]*;");
                        ulong firstMutualId = Convert.ToUInt64(mutualIdPattern.Match(mutualServersField.Value).ToString().Replace(";", "").Replace("`", ""));

                        DiscordGuild mutualServer = await client.GetGuildAsync(firstMutualId);
                        DiscordMember member = await mutualServer.GetMemberAsync(userId);

                        DiscordEmbedField messageIdField = e.Message.ReferencedMessage.Embeds[0].Fields.Where(f => f.Name == "Message ID").First();
                        ulong messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

                        string attachmentUrls = "";
                        string messageToSend = "";
                        if (e.Message.Attachments.Count != 0)
                        {
                            foreach (var attachment in e.Message.Attachments)
                            {
                                attachmentUrls += $"{attachment.Url}\n";
                            }
                            messageToSend = $"{e.Message.Content}\n{attachmentUrls}";
                        }
                        else
                        {
                            messageToSend = e.Message.Content;
                        }

                        var replyBuilder = new DiscordMessageBuilder().WithContent(messageToSend).WithReply(messageId);

                        DiscordMessage reply = await member.SendMessageAsync(replyBuilder);

                        var messageBuilder = new DiscordMessageBuilder().WithContent($"Sent! (`{reply.Id}` in `{reply.Channel.Id}`)").WithReply(e.Message.Id);
                        await e.Channel.SendMessageAsync(messageBuilder);
                    }
                    else
                    {
                        try
                        {
                            foreach (DiscordUser owner in client.CurrentApplication.Owners)
                            {
                                foreach (var guildPair in client.Guilds)
                                {
                                    DiscordGuild guild = await client.GetGuildAsync(guildPair.Key);

                                    if (guild.Members.ContainsKey(owner.Id))
                                    {
                                        DiscordMember ownerMember = await guild.GetMemberAsync(owner.Id);

                                        DiscordEmbedBuilder embed = new()
                                        {
                                            Color = DiscordColor.Yellow,
                                            Title = $"DM received from {e.Author.Username}#{e.Author.Discriminator}!",
                                            Description = $"{e.Message.Content}",
                                            Timestamp = DateTime.UtcNow
                                        };

                                        embed.AddField("User ID", $"`{e.Author.Id}`", true);
                                        embed.AddField("User Mention", $"{e.Author.Mention}", true);
                                        embed.AddField("User Avatar URL", $"[Link]({e.Author.AvatarUrl})", true);
                                        embed.AddField("Channel ID", $"`{e.Channel.Id}`", true);
                                        embed.AddField("Message ID", $"`{e.Message.Id}`", true);

                                        string attachmentUrls = "";
                                        if (e.Message.Attachments.Count != 0)
                                        {
                                            foreach (var attachment in e.Message.Attachments)
                                            {
                                                attachmentUrls += $"{attachment.Url}\n";
                                            }
                                            embed.AddField("Attachments", attachmentUrls, true);
                                        }

                                        string mutualServers = "";

                                        foreach (var guildId in client.Guilds)
                                        {
                                            DiscordGuild server = await client.GetGuildAsync(guildId.Key);

                                            if (server.Members.ContainsKey(e.Author.Id))
                                            {
                                                mutualServers += $"- `{server}`\n";
                                            }
                                        }

                                        embed.AddField("Mutual Servers", mutualServers, false);

                                        var messageBuilder = new DiscordMessageBuilder().WithEmbed(embed.Build());

                                        if (e.Message.ReferencedMessage != null)
                                        {
                                            messageBuilder = messageBuilder.WithReply(e.Message.ReferencedMessage.Id);
                                        }

                                        await ownerMember.SendMessageAsync(messageBuilder);
                                        return;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.Now}] A DM was received, but could not be forwarded!\nException Details: {ex.GetType}: {ex.Message}\nMessage Content: {e.Message.Content}");
                            return;
                        }
                    }
                }
                );
            }

            await discord.ConnectAsync();
            discord.Ready += OnReady;
            discord.MessageCreated += MessageCreated;

            while (true)
            {
                PerServerFeatures.WednesdayCheck();
                PerServerFeatures.PizzaTime();
                await Task.Delay(60000);
            }
        }

        public static string GetDebugInfo()
        {
            string commitHash = "";
            if (File.Exists("CommitHash.txt"))
            {
                StreamReader readHash = new("CommitHash.txt");
                commitHash = readHash.ReadToEnd().Trim();
            }
            if (commitHash == "")
            {
                commitHash = "dev";
            }

            string commitTime = "";
            string commitTimeDescription = "";
            if (File.Exists("CommitTime.txt"))
            {
                StreamReader readTime = new("CommitTime.txt");
                commitTime = readTime.ReadToEnd();
                commitTimeDescription = "Commit timestamp:";
            }
            if (commitTime == "")
            {
                commitTime = connectTime.ToString();
                commitTimeDescription = "Last connected to Discord at";
            }

            string commitMessage = "";
            if (File.Exists("CommitMessage.txt"))
            {
                StreamReader readMessage = new("CommitMessage.txt");
                commitMessage = readMessage.ReadToEnd();
            }
            if (commitMessage == "")
            {
                commitMessage = $"Running in development mode; process started at {processStartTime}";
            }

            return $"\nFramework: `{RuntimeInformation.FrameworkDescription}`"
                + $"\nPlatform: `{RuntimeInformation.OSDescription}`"
                + $"\nLibrary: `DSharpPlus {discord.VersionString}`"
                + "\n"
                + $"\nLatest commit: `{commitHash}`"
                + $"\n{commitTimeDescription} `{commitTime}`"
                + $"\nLatest commit message:\n```\n{commitMessage}\n```";
        }
    }
}
