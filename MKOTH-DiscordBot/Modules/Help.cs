using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Utilities;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Provides the user guide for this bot.")]
    [Remarks("Module A")]
    public class Help : InteractiveBase
    {
        private readonly CommandService commands;
        private readonly IServiceProvider services;
        private readonly string prefix;

        public Help(CommandService commandsService, IServiceProvider serviceProvider)
        {
            commands = commandsService;
            services = serviceProvider;
            
            prefix = services.GetScoppedSettings<AppSettings>().Settings.DefaultCommandPrefix;
        }

        public async Task HelpCommand()
        {
            var timeoutSeconds = 60;
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("❓ User Guide")
                .WithDescription("Here is the list of command modules.\n" +
                "A module is a catergory for a group of related commands.\n" +
                $"Press the corresponding emote or enter `{prefix}help <module>` to view the commands in a module.\n")
                .WithFooter($"Press the respective emote to expand the module list (expire in {timeoutSeconds} seconds).");

            var moduleEmoteOrders = commands.Modules
                .OrderBy(x => x.Remarks ?? "Module Z")
                .Zip(EmojiPresets.Numbers.Skip(1), (x, y) => new KeyValuePair<Emoji, ModuleInfo>(y, x));

            embed.Fields = getOriginalFields();

            IUserMessage msg = default;
            var reactionCallbacksData = new ReactionCallbackData(string.Empty, embed.Build(), false, false, TimeSpan.FromSeconds(timeoutSeconds), c => onExpire());

            foreach (var item in moduleEmoteOrders)
            {
                reactionCallbacksData = reactionCallbacksData.WithCallback(item.Key, (c, r) =>
                {
                    embed.Description = $"Enter `{prefix}help {prefix}<command>` to view the details of a command";
                    embed.Fields = getOriginalFields();
                    embed.Fields.Single(x => x.Name.Contains(item.Value.Name)).Value = GetCommnadListFormatted(moduleEmoteOrders.Single(x => x.Key.Name == r.Emote.Name).Value);
                    modifyHelp(c, r.MessageId, embed.Build(), r);
                    return Task.CompletedTask;
                });
            }
            msg = await InlineReactionReplyAsync(reactionCallbacksData);

            List<EmbedFieldBuilder> getOriginalFields()
            => moduleEmoteOrders.Select(x => new EmbedFieldBuilder()
                .WithName($"{x.Key.Name} {x.Value.Name}")
                .WithValue(x.Value.Summary ?? "In Development".MarkdownCodeBlock()))
                .ToList();

            async Task onExpire()
            {
                var expireEmbed = Context.Channel.GetMessageAsync(msg.Id).Result.Embeds.First()
                    .ToEmbedBuilder()
                    .WithFooter("Emote interactive expired");
                expireEmbed.Description = expireEmbed.Description.Replace("Press the corresponding emote or e", "E");
                _ = msg.RemoveAllReactionsAsync();
                await msg.ModifyAsync(x => x.Embed = expireEmbed.Build());
            }

            void modifyHelp(SocketCommandContext modifyingContext, ulong messageId, Embed expandedEmbed, SocketReaction reaction)
            {
                var reactionMsg = (IUserMessage)modifyingContext.Channel.GetMessageAsync(messageId).Result;
                reactionMsg.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                reactionMsg.ModifyAsync(x =>
                {
                    x.Embed = expandedEmbed;
                });
            }
        }

        [Command("Help")]
        [Alias("H", "Info")]
        [Summary("Use with an input `<para>`(module or command name) to find the details and usage about a module or a command.")]
        public async Task HelpCommand([Remainder]string para = null)
        {
            if (para == null)
            {
                await HelpCommand();
                return;
            }
            para = para.ToLower();
            var embed = new EmbedBuilder().WithColor(Color.Orange);

            var module = commands.Modules
                .FirstOrDefault(x => x.Name.StartsWithIgnoreCase(para))
                ?? (int.TryParse(para, out int result) ? commands.Modules
                .OrderBy(x => x.Remarks ?? "Module Z")
                .ElementAtOrDefault(result - 1)
                : null);
            if (module != null)
            {
                var commandList = GetCommnadListFormatted(module);
                embed.WithAuthor("📦 Module Information")
                    .WithTitle(module.Name)
                    .WithDescription(module.Summary ?? "In Development".MarkdownCodeBlock())
                    .AddField("Commands", commandList);
                goto helpReplyProcedure;
            }

            para = para.StartsWith(prefix) ? para.Substring(prefix.Length) : para;
            var command = commands.Commands
                .Where(x => x.Name.EqualsIgnoreCase(para) || x.Aliases.Any(y => y.EqualsIgnoreCase(para)))
                .ToList();
            if (command.Count > 0)
            {
                command.Sort((a, b) => a.Name == para ? -1 : 1);
                var baseCommand = command.First();
                string commandDescription = "";
                command.ForEach(x =>
                {
                    commandDescription =
                    !commandDescription.Contains(x.Summary ?? "") ?
                    commandDescription + (x.Summary == null ? "" : x.Summary.AddSpace()) : commandDescription;
                });
                commandDescription = commandDescription == "" ? "In Development".MarkdownCodeBlock() : commandDescription;
                embed.WithAuthor("📃 Command Information")
                    .WithTitle(baseCommand.Name)
                    .WithDescription(commandDescription);
                if (baseCommand.Aliases.Count > 0)
                {
                    var alias = baseCommand.Aliases.Select(x => $"{(prefix + x).MarkdownCodeLine()}\t");
                    embed.AddField("Alias", string.Join(" ", alias));
                }
                string restrictions = null;
                baseCommand.Module.Preconditions
                    .ToList()
                    .ForEach(x => restrictions += x.GetDescription().MarkdownCodeLine().AddLine());
                baseCommand.Preconditions
                    .ToList()
                    .ForEach(x => restrictions += x.GetDescription().MarkdownCodeLine().AddLine());
                if (restrictions != null)
                {
                    embed.AddField("Restrictions", restrictions);
                }

                var usages = command.Select(x => $"{prefix}{x.Name.AddSpace() + x.GetCommandParametersInfo()}");
                embed.AddField("Usage", string.Join("\n", usages).MarkdownCodeBlock("css"));

                string example = "";
                command.ForEach(x => example += x.Remarks != null ? x.Remarks.AddLine() : "");
                if (example != "")
                {
                    embed.AddField("Example", example.MarkdownCodeBlock("ts"));
                }

                embed.WithFooter($"📦 {baseCommand.Module.Name} module");

                goto helpReplyProcedure;
            }

            embed.WithDescription("🔎 module / command not found.");

        helpReplyProcedure:
            await ReplyAsync(embed: embed.Build());
        }

        private string GetCommnadListFormatted(ModuleInfo module)
        {
            string commandList = string.Join("\n", module.Commands
                   .Select(x => $"{prefix}{x.Name.AddSpace() + x.GetCommandParametersInfo()}"));
            return commandList.MarkdownCodeBlock("css");
        }

        [Command("Info")]
        [Summary("Information about this bot.")]
        public async Task Info()
        {
            await commands.Commands
                    .Single(x => x.Name == "BotInfo")
                    .ExecuteAsync(Context, new object[] { }, null, services);
        }

        [Command("IsAdmin")]
        public async Task IsAdmin(IGuildUser user = null)
        {
            user ??= Context.User as IGuildUser;
            var embed = new EmbedBuilder()
                .WithDescription($"{user.Mention} is {(user.GuildPermissions.Administrator ? "" : "not ")} an admin.");
            await ReplyAsync(embed: embed.Build());
        }

        [Command("User")]
        [Alias("alt")]
        [Summary("Checks the user's registration and server join date.")]
        [RequireContext(ContextType.Guild)]
        public async Task User(IGuildUser user = null)
        {
            user ??= Context.User as IGuildUser;
            var isThisBot = user.Id == Context.Client.CurrentUser.Id;
            if (!isThisBot)
            {
                user = await user.Guild.GetUserAsync(user.Id, CacheMode.AllowDownload);
            }
            var resgistrationDate = user.CreatedAt;
            var joinedDate = user.JoinedAt.Value;
            var difference = joinedDate - resgistrationDate;
            var activity = !isThisBot ? user.Activity : Context.Client.Activity;

            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithAuthor(user)
                .WithDescription($"**Registered:** {resgistrationDate.ToString("R")}\n" +
                $"**Joined:** {joinedDate.ToString("R")}\n" +
                $"**Difference:** {difference.AsRoundedDuration()}");

            if (activity != null)
            {
                var type = activity.Type;
                var typeName = Enum.GetName(typeof(ActivityType), type)?.ToLower();
                if (typeName != null)
                {
                    var name = activity.Name;
                    if (activity is StreamingGame stream)
                    {
                        name = $"[{stream.Name}]({stream.Url})";
                    }
                    if (activity is RichGame game)
                    {
                        name = $"{game.Name} ({game.State} - {game.Details})";
                        if (game.LargeAsset != null && game.SmallAsset != null)
                        {
                            embed.WithImageUrl(game.LargeAsset.GetImageUrl())
                                .WithThumbnailUrl(game.SmallAsset.GetImageUrl())
                                .WithFooter($"{game.SmallAsset.Text} | {game.LargeAsset.Text}");
                        }
                    }
                    embed.Description += $"\n\nThe user is currently **{typeName}:** {name}";
                }
            }

            await ReplyAsync(user == Context.User ? "Checking yourself." : string.Empty, embed: embed.Build());
        }

        [Command("Ban")]
        [RequireContext(ContextType.Guild)]
        [Summary("In Development: Future moderation tools.")]
        public async Task Ban(IGuildUser user)
        {
            await ReplyAsync("This command is reserved for future chat moderation uses.\n" +
                $"Do you mean `{prefix}Challenge` to challenge someone and start a tower banning session?");
        }
    }
}
