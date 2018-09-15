using System;
using System.Linq;
using System.Text;
using System.Timers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MKOTHDiscordBot.Services
{
    [SingletonService("Rate limiting service to watch users and prevent them spamming to the bot.")]
    public class SpamWatch
    {
        private List<ulong> userList = new List<ulong>();
        private HashSet<BlockedUser> blockedUsers = new HashSet<BlockedUser>();
        private Timer refresher = new Timer(5000);

        public SpamWatch()
        {
            refresher.Start();
            refresher.Elapsed += (sender, evt) =>
            {
                if (userList.Count > 0)
                {
                    Logger.Debug(userList, "Rate Limiter Refresh");
                    var distinct = userList.Distinct();
                    distinct.ToList().ForEach(x => userList.Remove(x));
                }
            };
        }

        public bool Watch(ulong watchId, Action rateLimiteResponder = null)
        {
            var userId = watchId;
            userList.Add(userId);
            if (userList.Count(x => x == userId) > 3)
            {
                var blockuser = new BlockedUser
                {
                    Id = userId,
                    Timer = new Timer
                    {
                        Interval = 10000,
                        AutoReset = false
                    }
                };
                if (blockedUsers.Add(blockuser))
                {
                    blockuser.Timer.Start();
                    blockuser.Timer.Elapsed += (sender, evt) =>
                    {
                        var timer = sender as Timer;
                        timer.Dispose();
                        userList.RemoveAll(x => x == blockuser.Id);
                        blockedUsers.RemoveWhere(x => x.Id == blockuser.Id);
                    };
                    Logger.Debug(blockuser, "Rate Limting");
                    rateLimiteResponder?.Invoke();
                    return true;
                }
            }

            return blockedUsers.Any(x => x.Id == userId);
        }

        private class BlockedUser
        {
            public ulong Id;
            public Timer Timer;

            public override bool Equals(object obj)
            {
                var user = obj as BlockedUser;
                return user != null &&
                       Id == user.Id;
            }

            public override int GetHashCode()
            {
                var hashCode = 1762019010;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(BlockedUser user1, BlockedUser user2)
            {
                return EqualityComparer<BlockedUser>.Default.Equals(user1, user2);
            }

            public static bool operator !=(BlockedUser user1, BlockedUser user2)
            {
                return !(user1 == user2);
            }
        }
    }
}
