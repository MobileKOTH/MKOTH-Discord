using System;
using System.Linq;
using System.Text;
using System.Timers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace MKOTHDiscordBot.Utilities
{
    class SpamWatch
    {
        private static List<ulong> userList = new List<ulong>();
        private static HashSet<BlockedUser> blockedUsers = new HashSet<BlockedUser>();
        private static Timer refresher = new Timer(2000);

        public static void Start()
        {
            refresher.Start();
            refresher.Elapsed += (sender, evt) =>
            {
                if (userList.Count > 0)
                {
                    userList.RemoveAt(0);
                }
            };
        }

        public static async Task<bool> Watch(SocketCommandContext context)
        {
            var userId = context.User.Id;
            userList.Add(userId);
            if (userList.Count(x => x == userId) > 5)
            {
                var blockuser = new BlockedUser
                {
                    Id = userId,
                    Timer = new Timer
                    {
                        Interval = 10000,
                        AutoReset = true
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
                    await Responder.SendToContext(context, "You are now rate limited.");
                    return true;
                }
            }

            if (blockedUsers.Count(x => x.Id == userId) > 0)
            {
                return true;
            }
            return false;
        }


        private struct BlockedUser
        {
            public ulong Id;
            public Timer Timer;

            public override bool Equals(object obj)
            {
                var other = (BlockedUser)obj;
                return Id == other.Id;
            }

            public override int GetHashCode()
            {
                return 2108858624 + Id.GetHashCode();
            }
        }
    }
}
