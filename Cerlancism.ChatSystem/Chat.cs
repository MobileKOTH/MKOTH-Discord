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

    using static Utilities.DatabaseUtilities;

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
                    RemovedCallback = x => Task.Run(async () =>
                    {
                        historyCache = null;
                        GC.Collect();
                        await Task.Delay(5000);
                        GC.Collect();
                    })
                };
                historyCache = cache.AddOrGetExisting(HistoryCacheKey, () => ChatCollection.FindAll().ToList(), cachePolicy);
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

        private LiteDatabase ChatDatabase => _chatDatabase ?? GetAndOutDataBase(out _chatDatabase, connectionString);
        private LiteCollection<Entry> ChatCollection => _chatCollection ?? GetAndOutCollection(ChatDatabase, out _chatCollection);

        private LiteDatabase _chatDatabase;
        private LiteCollection<Entry> _chatCollection;

        public Chat(string connectionString)
        {   
            this.connectionString = connectionString;
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
                await UpdateMessageAsync(message);
                return;
            }

            await AddMessageAsync(userId, message);
        }

        private async Task UpdateMessageAsync(string message)
        {
            var lastMessage = await GetLastChatHistoryAsync();
            lastMessage.Message += " " + message;
            UpdateCache(x => x.Last().Message = lastMessage.Message);
            await Task.FromResult(ChatCollection.Update(lastMessage));
        }

        private async Task AddMessageAsync(ulong userId, string message)
        {
            var entry = new Entry
            {
                Message = message
            };
            lastId = await Task.FromResult(ChatCollection.Insert(entry));
            UpdateCache(x => x.Add(entry));

            previousUserId = userId;
            previousMessage = message;
        }

        public async Task<Entry> GetChatHistoryByIdAsync(int id)
            => await Task.FromResult(ChatCollection.FindById(id));

        public async Task<Entry> GetLastChatHistoryAsync()
            => lastId == default ? (await Task.FromResult(ChatCollection.FindOne(Query.All(Query.Descending))))
            .Forward(x =>
            {
                lastId = x.Id;
                Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    GC.Collect();
                });

                return x;
            }) : 
            await GetChatHistoryByIdAsync(lastId);

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
            _chatDatabase?.Dispose();
            _chatDatabase = null;
            _chatCollection = null;
        }
    }
}
