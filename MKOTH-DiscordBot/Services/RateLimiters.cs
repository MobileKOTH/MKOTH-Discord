using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;

namespace MKOTHDiscordBot.Services
{
    public class UsageRateLimiter : RateLimiterBase<ulong>
    {
        public UsageRateLimiter() : base(60000, 30000, 6)
        {
            limiterDebugName = "All Usage";
        }

        public bool Audit(ICommandContext context, ResponseService responser)
            => base.Audit(context.User.Id, () =>
            {
                var time = new TimeSpan(0, 0, (int)cooldown / 1000).AsRoundedDuration();
                _ = responser.SendToContextAsync(context, $"{context.User.Mention}, you are sending commands too fast, you will be ignored for the next {time}.");
            });
    }

    public abstract class RateLimiterBase<T> where T : IEquatable<T>
    {
        private readonly Timer refresher;
        private readonly int burstLimit;
        private readonly List<T> watchList = new List<T>();
        protected readonly HashSet<LimitedEntity> limitedEntities = new HashSet<LimitedEntity>();
        protected readonly double cooldown;

        protected string limiterDebugName = "";

        public RateLimiterBase(double fairUsageinterval, double triggeredColdown, int burstLimitTimes)
        {
            cooldown = triggeredColdown;
            burstLimit = burstLimitTimes;
            refresher = new Timer(fairUsageinterval);
            refresher.Start();
            refresher.Elapsed += HandleRefresh;
        }

        private void HandleRefresh(object _, EventArgs __)
        {
            if (watchList.Count > 0)
            {
                Logger.Debug(watchList, $"[{limiterDebugName}] Rate Limiter Refresh");
                var distinct = watchList.Distinct();
                distinct.ToList().ForEach(x => watchList.Remove(x));
            }
        }

        protected bool IsLimited(T watchId)
        {
            return watchList.Count(x => x.Equals(watchId)) > burstLimit;
        }

        public virtual bool Audit(T watchId, Action rateLimiteResponder = null)
        {
            var id = watchId;
            watchList.Add(id);
            if (IsLimited(watchId))
            {
                var limitedEntity = new LimitedEntity
                {
                    Id = id,
                    StartTime = DateTime.Now,
                    Timer = new Timer
                    {
                        Interval = cooldown,
                        AutoReset = false
                    }
                };
                if (limitedEntities.Add(limitedEntity))
                {
                    limitedEntity.Timer.Start();
                    limitedEntity.Timer.Elapsed += (sender, _) => HandleLimitedEntityRelease(sender as Timer, limitedEntity);
                    Logger.Debug(limitedEntity, $"[{limiterDebugName}] Rate Limting");
                    rateLimiteResponder?.Invoke();
                    return true;
                }
            }

            return limitedEntities.Any(x => x.Id.Equals(id));
        }

        private void HandleLimitedEntityRelease(Timer timer, LimitedEntity entity)
        {
            timer.Dispose();
            watchList.RemoveAll(x => x.Equals(entity.Id));
            limitedEntities.RemoveWhere(x => x.Id.Equals(entity.Id));
        }

        protected class LimitedEntity
        {
            public T Id;
            public DateTime StartTime;
            public Timer Timer;

            public override bool Equals(object obj)
            {
                return obj is LimitedEntity entity &&
                       EqualityComparer<T>.Default.Equals(Id, entity.Id);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id);
            }

            public static bool operator ==(LimitedEntity left, LimitedEntity right)
            {
                return EqualityComparer<LimitedEntity>.Default.Equals(left, right);
            }

            public static bool operator !=(LimitedEntity left, LimitedEntity right)
            {
                return !(left == right);
            }
        }
    }
}
