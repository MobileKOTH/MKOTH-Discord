using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;

using MKOTHDiscordBot.Models;

namespace MKOTHDiscordBot.Core
{
    public interface IRankingManager
    {
        int ELO_KFactor { get; }
        int InativeDaysToRemove { get; }
        ITextChannel RankingChannel { get; }
        IEnumerable<SeriesPlayer> SeriesPlayers { get; }
        event Func<Task> Updated;
        Task AddSeriesAsync(Series series);
        Task UpdateFullLeaderBoard();
        string GetPlayerMention(string id);
        IEnumerable<string> PrintRankingList(IEnumerable<KeyValuePair<int, SeriesPlayer>> list);
    }
}