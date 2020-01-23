using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using Cerlancism.TieredEloRankingSystem.Actions;
using Cerlancism.TieredEloRankingSystem.Core;
using Cerlancism.TieredEloRankingSystem.Models;
using Cerlancism.TieredEloRankingSystem.Exceptions;
using System.Linq;

namespace Cerlancism.TieredEloRankingSystem
{
    using static Extensions.FuncExtensions;

    public partial class RankingProcessor : IDisposable
    {
        private static Dictionary<ulong, Stack<IAction>> RedoStacks = new Dictionary<ulong, Stack<IAction>>();
        private Stack<IAction> RedoStack { get => RedoStacks[guildId]; set => RedoStacks[guildId] = value; }
        public IAction LastAction => repository.Database.GetCollection<IAction>().FindOne(Query.All(Query.Descending));

        private readonly ulong guildId;
        public LiteDatabase Database => repository.Database;
        public LiteRepository repository;

        public Lazy<GuildSetting> GuildSetting;
        public Lazy<LinkedList<TierConfiguration>> Tiers;

        public RankingProcessor(ulong guildId, string connectionString)
        {
            this.guildId = guildId;
            repository = new LiteRepository(connectionString);

            GuildSetting = new Lazy<GuildSetting>(() => repository.SingleOrDefault<GuildSetting>(x => x.GuildId == guildId) ?? throw new GuildInitialisationException());
            Tiers = new Lazy<LinkedList<TierConfiguration>>(() => new LinkedList<TierConfiguration>(GuildSetting.Value.Tiers));

            if (!RedoStacks.Any(x => x.Key == guildId))
            {
                RedoStacks.Add(guildId, new Stack<IAction>());
            };

            var mappper = BsonMapper.Global;

            mappper.Entity<IPlayer>()
                .DbRef(x => x.Rank);

            mappper.Entity<PlayerRank>()
                .DbRef(x => x.Player, nameof(IPlayer));
        }

        public GuildSetting InitialiseGuild(ulong defaultChannelId)
        {
            if (repository.Query<GuildSetting>().SingleOrDefault() != default)
            {
                throw new GuildInitialisationException("Guild has already initialised.");
            }

            repository.Database.GetCollection<IPlayer>()
                .EnsureIndex(x => x.DiscordId);

            var setting = new GuildSetting(guildId, defaultChannelId);
            repository.Insert(setting);
            return setting;
        }

        public IAction PerformAction(IAction action, bool clearStack = true)
        {
            if (clearStack)
            {
                RedoStack.Clear();
            }
            action.Execute(this);
            repository.Insert(action);
            return action;
        }

        public IAction UndoLastAction()
        {
            var lastAction = LastAction;
            repository.Delete<IAction>(lastAction.Id);
            lastAction.TimeStamp = DateTime.Now;
            var oldId = lastAction.Id;
            lastAction.Id = default;
            RedoStack.Push(lastAction);
            lastAction.Undo(this);
            lastAction.Id = oldId;
            return lastAction;
        }

        public IAction RedoLastUndo()
        {
            if (RedoStack.Count > 0)
            {
                var redoAction = RedoStack.Pop();
                redoAction.TimeStamp = DateTime.Now;
                return PerformAction(redoAction);
            }
            else
            {
                return null;
            }
        }

        public void Dispose()
        {
            repository.Dispose();
        }
    }
}
