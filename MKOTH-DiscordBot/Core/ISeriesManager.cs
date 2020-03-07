using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;

using MKOTHDiscordBot.Models;

namespace MKOTHDiscordBot.Core
{
    public interface ISeriesManager
    {
        IQueryable<ulong> AllPlayers { get; }
        IQueryable<Series> SeriesHistory { get; }
        bool Ready { get; }
        event Func<Task> Updated;
        Series MakeSeries(ulong winner, ulong loser, int wins, int losses, int draws, string replay);
        void AddPending(Series series);
        bool HasNewPlayer(Series series);
        bool CanApprove(Series series, IGuildUser user);
        Task ApproveAsync(int id);
        Task AdminCreateAsync(Series series);
        Task RemoveAsync(int id);
        IEnumerable<string> PrintSeriesHistoryLines(IEnumerable<Series> seriesSet);
    }
}