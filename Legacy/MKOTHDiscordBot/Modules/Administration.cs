using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MKOTHDiscordBot.Services;
using Cerlancism.TieredEloRankingSystem;
using System.IO;
using Cerlancism.TieredEloRankingSystem.Actions;

namespace MKOTHDiscordBot.Modules
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireBotPermission(GuildPermission.ManageRoles)]
    [RequireBotPermission(GuildPermission.ManageNicknames)]
    [RequireBotPermission(GuildPermission.ManageMessages)]
    [RequireContext(ContextType.Guild)]
    [Summary("Facilitates the set ups and management of the ranking system for administrators.")]
    [Remarks("Module B")]
    public class Administration : ModuleBase<SocketCommandContext>, IDisposable
    {
        private Lazy<RankingSystem> rankingSystemLazy;
        private RankingSystem RankingSystem => rankingSystemLazy.Value;

        public Administration(IServiceProvider service)
        {
            rankingSystemLazy = new Lazy<RankingSystem>(() => new RankingSystem(Context.Guild.Id, service));
        }

        [Command("ResetRanking")]
        [Alias("rr")]
        [Summary("Destroy and reset all data for this guild.")]
        [RequireDeveloper]
        public async Task ResetRankSystem(bool sendFile)
        {
            var (fileName, streamArray) = RankingSystem.Reset();
            if (sendFile)
            {
                var stream = new MemoryStream(streamArray);
                await Context.Channel.SendFileAsync(stream, fileName, $"Deleted and dumped Database.");
            }
            else
            {
                await ReplyAsync($"Deleted Database.");
            }
        }

        [Command("Initialise")]
        [Alias("init")]
        [Summary("Initialise the guild for ranking system.")]
        public async Task InitGuild()
        {
            RankingSystem.Processor.InitialiseGuild(Context.Channel.Id);
            await ReplyAsync($"Initialised guild with {Context.Channel.GetMention()} as default log channels.");
        }

        [Command("LastAction")]
        [Alias("la")]
        public async Task LastAction()
        {
            var action = RankingSystem.Processor.LastAction;
            var properties = action.GetType().GetProperties().Select(x => $"{x.Name}: {x.GetValue(action)}");

            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription(string.Join("\n", properties).MarkdownCodeBlock("yaml"))
                .WithFooter($"Action Id: {action.Id}")
                .WithTimestamp(action.TimeStamp);

            await ReplyAsync($"Last action: {action.GetType().Name}", embed: embed.Build());
        }
        
        [Command("AddPlayer")]
        [Alias("ap")]
        [Summary("Add player to the ranking system.")]
        public async Task AddPlayer(IGuildUser user, [Remainder] string playerName)
        {
            RankingSystem.Processor.AddPlayer(playerName, user.Id);
            await ReplyAsync($"Added player {Format.Bold(playerName)}");
        }

        [Command("RemovePlayer")]
        [Alias("rp")]
        public async Task RemovePlayer(IGuildUser user)
        {
            var action = RankingSystem.Processor.RemovePlayer(user.Id);
            await ReplyAsync($"Removed player {Format.Bold((action as RemovePlayerAction).Player.Name)} {(user.GetDisplayNameWithDiscriminator() + user.Discriminator).MarkdownCodeLine()}");
        }

        [Command("undo")]
        public async Task Undo()
        {
            var action = RankingSystem.Processor.UndoLastAction();
            await ReplyAsync($"Undid Action Id: `{action.Id}`.");
        }

        [Command("redo")]
        public async Task Redo()
        {
            var action = RankingSystem.Processor.RedoLastUndo();
            if (action != null)
            {
                await ReplyAsync($"Redid Action Id: `{action.Id}`.");
            }
            else
            {
                await ReplyAsync($"Nothing to redo.");
            }
        }

        public void Dispose()
        {
            if (rankingSystemLazy.IsValueCreated)
            {
                rankingSystemLazy.Value.Dispose();
            }
        }
    }
}
