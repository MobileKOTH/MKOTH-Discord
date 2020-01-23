using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MKOTHDiscordBot.Services
{
    public class RateLimiter
    {
        private List<ulong> userList = new List<ulong>();
        private HashSet<LimitedUser> limitedUsers = new HashSet<LimitedUser>();
        private Timer refresher = new Timer(5000);

        public RateLimiter()
        {
            refresher.Start();
            refresher.Elapsed += HandleRefresh;
        }

        private void HandleRefresh(object _, EventArgs __)
        {
            if (userList.Count > 0)
            {
                Logger.Debug(userList, "Rate Limiter Refresh");
                var distinct = userList.Distinct();
                distinct.ToList().ForEach(x => userList.Remove(x));
            }
        }

        public bool Audit(ulong watchId, Action rateLimiteResponder = null)
        {
            var userId = watchId;
            userList.Add(userId);
            if (userList.Count(x => x == userId) > 3)
            {
                var limitedUser = new LimitedUser
                {
                    Id = userId,
                    Timer = new Timer
                    {
                        Interval = 10000,
                        AutoReset = false
                    }
                };
                if (limitedUsers.Add(limitedUser))
                {
                    limitedUser.Timer.Start();
                    limitedUser.Timer.Elapsed += (sender, _) => HandleLimitedUserRelease(sender as Timer, limitedUser);
                    Logger.Debug(limitedUser, "Rate Limting");
                    rateLimiteResponder?.Invoke();
                    return true;
                }
            }

            return limitedUsers.Any(x => x.Id == userId);
        }

        private void HandleLimitedUserRelease(Timer timer, LimitedUser user)
        {
            timer.Dispose();
            userList.RemoveAll(x => x == user.Id);
            limitedUsers.RemoveWhere(x => x.Id == user.Id);
        }

        private class LimitedUser
        {
            public ulong Id;
            public Timer Timer;

            public override bool Equals(object obj)
            {
                var user = obj as LimitedUser;
                return user != null &&
                       Id == user.Id;
            }

            public override int GetHashCode()
            {
                var hashCode = 1762019010;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(LimitedUser user1, LimitedUser user2)
            {
                return EqualityComparer<LimitedUser>.Default.Equals(user1, user2);
            }

            public static bool operator !=(LimitedUser user1, LimitedUser user2)
            {
                return !(user1 == user2);
            }
        }
    }
}
