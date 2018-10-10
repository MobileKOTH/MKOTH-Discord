using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Addons.Interactive;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Provides the guidance of using the MKOTH Discord Bot.")]
    [Remarks("Module A")]
    public class Help : InteractiveBase
    {
        private CommandService commands;
        private IEnumerable<Emoji> NumberEmotes
        {
            get
            {
                yield return new Emoji("1⃣");
                yield return new Emoji("2⃣");
                yield return new Emoji("3⃣");
                yield return new Emoji("4⃣");
                yield return new Emoji("5⃣");
                yield return new Emoji("6⃣");
                yield return new Emoji("7⃣");
                yield return new Emoji("8⃣");
                yield return new Emoji("9⃣");
                yield return new Emoji("🔟");
            }
        }

        public Help(CommandService commands) 
            => this.commands = commands;

        [Command("Help")]
        [Alias("H", "Manual")]
        [Summary("Displays the general help information.")]
        public async Task HelpCommand()
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("❓ Manual")
                .WithDescription("Here is the list of command modules.\n" +
                "A module is a catergory for a group of related commands.\n" +
                "Press the corresponding emote or enter `.help <module>` to view the commands in a module.\n")
                .WithFooter("Press the respective emote to expand the module list.");

            var moduleEmoteOrders = commands.Modules
                .OrderBy(x => x.Remarks ?? "Module Z")
                .Zip(NumberEmotes, (x, y) => new KeyValuePair<Emoji, ModuleInfo>(y, x));

            embed.Fields = getOriginalFields();

            IUserMessage msg = default;
            var reactionCallbacksData = new ReactionCallbackData(string.Empty, embed.Build(), false, false, TimeSpan.FromSeconds(60), c => onExpire());

            foreach (var item in moduleEmoteOrders)
            {
                reactionCallbacksData = reactionCallbacksData.WithCallback(item.Key, (c, r) =>
                {
                    embed.Description = "Enter `.help .<command>` to view the details of a command";
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
                var expireEmbed = msg.Embeds.First()
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
        public async Task HelpCommand([Remainder]string para)
        {
            para = para.ToLower();
            var embed = new EmbedBuilder().WithColor(Color.Orange);

            var module = commands.Modules
                .FirstOrDefault(x => x.Name.ToLower().StartsWith(para))
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

            para = para.StartsWith(".") ? para.TrimStart('.') : para;
            var command = commands.Commands
                .Where(x => x.Name.ToLower() == para || x.Aliases.Any(y => y.ToLower() == para))
                .ToList();
            if (command.Count > 0)
            {
                command.Sort((a, b) =>
                {
                    if (a.Name == para)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                });
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
                    var alias = baseCommand.Aliases.Select(x => $"{("." + x).MarkdownCodeLine()}\t");
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

                var usages = command.Select(x => $".{x.Name.AddSpace() + x.GetCommandParametersInfo()}");
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
            await ReplyAsync(string.Empty, false, embed.Build());
        }

        private string GetCommnadListFormatted(ModuleInfo module)
        {
            string commandList = string.Join("\n", module.Commands
                   .Select(x => $".{x.Name.AddSpace() + x.GetCommandParametersInfo()}"));
            return commandList.MarkdownCodeBlock("css");
        }

        [Command("Info")]
        [Alias("MkothHelp", "Mkoth Help", "Mkoth Info", "Information", "Mkoth Information", "MkothInfo")]
        [Summary("What is MKOTH all about.")]
        public async Task Info() 
            => await ReplyAsync("The competitive community for BTD Battles.");
    }
}
