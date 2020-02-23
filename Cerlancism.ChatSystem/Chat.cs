using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Cerlancism.ChatSystem.Core;
using LiteDB;

namespace Cerlancism.ChatSystem
{
    using static Extensions.StringExtensions;
    using static Extensions.ObjectCacheExtensions;
    using static Extensions.FuncExtensions;

    public partial class Chat : IDisposable
    {
        public event Action<string> Log;

        private static ObjectCache cache = MemoryCache.Default;
        private static List<Entry> historyCache;
        private static CacheItemPolicy cachePolicy;
        private static string HistoryCacheKey => "historyCache";
        public ReadOnlyCollection<Entry> HistoryCache
        {
            get
            {
                cachePolicy = new CacheItemPolicy
                {
                    SlidingExpiration = TimeSpan.FromSeconds(20),
                    RemovedCallback = _ => Task.Run(async () =>
                    {
                        historyCache = null;
                        GC.Collect();
                        await Task.Delay(5000);
                        GC.Collect();
                    })
                };
                historyCache = cache.AddOrGetExisting(HistoryCacheKey, () => ChatCollection.Value.FindAll().ToList(), cachePolicy);
                return historyCache.AsReadOnly();
            }
        }

        private void UpdateCache(Action<List<Entry>> update)
        {
            if (historyCache != null)
            {
                update(historyCache);
                var cached = cache.GetCacheItem(HistoryCacheKey);
                cached.Value = historyCache;
                cache.Set(cached, cachePolicy);
            }
        }

        private static int lastId = default;
        private static ulong previousUserId = default;
        private static string previousMessage = default;

        private static readonly string[] startWithIgnoreList = { ".", ">", "?", "!", "/" };

        private string connectionString;

        private Lazy<LiteDatabase> ChatDatabase;
        private Lazy<LiteCollection<Entry>> ChatCollection;

        public Chat(string connectionString)
        {   
            this.connectionString = connectionString;
            ChatDatabase = new Lazy<LiteDatabase>(() => new LiteDatabase(connectionString));
            ChatCollection = new Lazy<LiteCollection<Entry>>(() => ChatDatabase.Value.GetCollection<Entry>());
        }

        public async Task AddAsync(ulong userId, string message)
        {
            if (startWithIgnoreList.Any(x => message.StartsWith(x)) || message.IsNullOrEmptyOrWhiteSpace())
            {
                return;
            }

            if (previousMessage == message)
            {
                return;
            }

            if (previousUserId == userId)
            {
                await UpdateEntryAsync(message);
                return;
            }

            await AddEntryAsync(userId, message);
        }

        private async Task UpdateEntryAsync(string message)
        {
            var lastMessage = await GetLastEntryAsync();
            lastMessage.Message += " " + message;
            UpdateCache(x => x.Last().Message = lastMessage.Message);
            await Task.FromResult(ChatCollection.Value.Update(lastMessage));
        }

        private async Task AddEntryAsync(ulong userId, string message)
        {
            var entry = new Entry
            {
                Message = message
            };
            lastId = await Task.FromResult(ChatCollection.Value.Insert(entry));
            UpdateCache(x => x.Add(entry));

            previousUserId = userId;
            previousMessage = message;
        }

        public async Task<Entry> GetEntryByIdAsync(int id)
            => await Task.FromResult(ChatCollection.Value.FindById(id));

        public async Task<Entry> GetLastEntryAsync()
            => lastId == default ? ChatCollection.Value.FindOne(Query.All(Query.Descending))
            .Forward(x =>
            {
                lastId = x.Id;
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    GC.Collect();
                });

                return x;
            })
            : await GetEntryByIdAsync(lastId);

        public async Task<string> ReplyAsync(string message)
        {
            var stopWatch = Stopwatch.StartNew();
            var (wordCount, analysis) = await AnalyseAsync(message);
            var results = GetResults(wordCount, analysis);
            var choosen = GetRandomReply(wordCount, results, out Analysis result);

            stopWatch.Stop();

            LogMessage(new
            {
                Message = message,
                Result = result,
                Possibilities = ((Analysis[])results).Length,
                Reply = choosen,
                TimeUsedMs = stopWatch.Elapsed.TotalMilliseconds
            });

            return choosen;
        }
        //=> Funcify<string, string>(RemovePunctuationsAndLower)
        //.Then(Analyse)
        //.Then(GetResults)
        //.Then(GetRandomReply)(message);

        //=> TrimMessage(message)
        //.ToLower()
        //.Forward(Analyse)
        //.Forward(x => GetResults(x, message))
        //.Forward(x => x.SelectRandom())
        //.Forward(x => GetTriggerOrResponse(x, message));

        //=> GetTriggerOrResponse(GetResults(Analyse(TrimMessage(message).ToLower()), message).SelectRandom(), message);

        public void Dispose()
        {
            if (ChatDatabase.IsValueCreated)
            {
                ChatDatabase.Value.Dispose();
            }
            ChatDatabase = null;
            ChatCollection = null;
        }
    }
}
