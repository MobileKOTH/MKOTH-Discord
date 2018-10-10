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
        public IAction LastAction => ActionCollection.Value.FindOne(Query.All(Query.Descending));

        private readonly ulong guildId;
        private LiteDatabase database;
        private Lazy<LiteRepository> repository;
        private Lazy<LiteCollection<GuildSetting>> GuildSettings;

        public Lazy<LiteCollection<IPlayer>> PlayerCollection;
        public Lazy<LiteCollection<IAction>> ActionCollection;
        public Lazy<LiteCollection<PlayerRank>> RankingCollection;

        public Lazy<GuildSetting> GuildSetting;
        public Lazy<LinkedList<TierConfiguration>> Tiers;

        public RankingProcessor(ulong guildId, string connectionString)
        {
            this.guildId = guildId;
            database = new LiteDatabase(connectionString);
            repository = new Lazy<LiteRepository>(() => new LiteRepository(connectionString));

            GuildSettings = new Lazy<LiteCollection<GuildSetting>>(() => database.GetCollection<GuildSetting>());
            GuildSetting = new Lazy<GuildSetting>(() => GuildSettings.Value.FindOne(x => x.GuildId == guildId) ?? throw new GuildInitialisationException());
            Tiers = new Lazy<LinkedList<TierConfiguration>>(() => new LinkedList<TierConfiguration>(GuildSetting.Value.Tiers));
            PlayerCollection = new Lazy<LiteCollection<IPlayer>>(() => database.GetCollection<IPlayer>());
            ActionCollection = new Lazy<LiteCollection<IAction>>(() => database.GetCollection<IAction>());
            RankingCollection = new Lazy<LiteCollection<PlayerRank>>(() => database.GetCollection<PlayerRank>());

            if (!RedoStacks.Any(x => x.Key == guildId))
            {
                RedoStacks.Add(guildId, new Stack<IAction>());
            };
        }

        public GuildSetting InitialiseGuild(ulong defaultChannelId)
        {
            if (GuildSettings.Value.Exists(x => x.GuildId == guildId))
            {
                throw new GuildInitialisationException("Guild has already initialised.");
            }

            PlayerCollection.Value.EnsureIndex(x => x.DiscordId);

            var setting = new GuildSetting(guildId, defaultChannelId);
            GuildSettings.Value.Insert(setting);
            return setting;
        }

        public IAction PerformAction(IAction action)
        {
            action.Execute(this);
            ActionCollection.Value.Insert(action);
            return action;
        }

        public IAction UndoLastAction()
        {
            var lastAction = LastAction;
            ActionCollection.Value.Delete(lastAction.Id);
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
            if (repository.IsValueCreated)
            {
                repository.Value.Dispose();
            }
            database.Dispose();
        }
    }
}
