using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Discord;
using Microsoft.Extensions.DependencyInjection;
namespace MKOTHDiscordBot.Services
{
    public class TowerBanUser
    {
        public IUser User { get; set; }
        public Tower? Choice { get; set; }
    }
    public class TowerBanSession
    {
        public DateTime StartTime { get; } = DateTime.Now;
        public TowerBanUser[] Users { get; set; } = new TowerBanUser[2];
        public ITextChannel InitiateChannel { get; set; }
    }

    public enum Tower
    {
        Dart = 1,
        Farm,
        Ace,
        Ice,
        Wizard,
        Heli,
        Sub,
        Boat,
        Spike,
        Glue,
        Sniper,
        Mortar,
        Boomerang,
        Ninja,
        Engineer,
        Village,
        Dartling,
        Cobra,
        Tack,
        Bomb,
    }

    public class TowerBanManager
    {
        public const int MAX_SESSION_SECONDS = 60;

        private readonly List<TowerBanSession> sessions;
        private readonly Timer timer;

        private readonly ResponseService responseService;
        public TowerBanManager(IServiceProvider services)
        {
            responseService = services.GetService<ResponseService>();

            sessions = new List<TowerBanSession>();
            timer = new Timer(MAX_SESSION_SECONDS / 10);
            timer.Elapsed += HandleTimer;
            timer.Start();
        }

        private void HandleTimer(object sender, ElapsedEventArgs e)
        {
            foreach (var item in sessions.Reverse<TowerBanSession>())
            {
                if ((DateTime.Now - item.StartTime).TotalSeconds > MAX_SESSION_SECONDS)
                {
                    ExpireSession(item);
                }
            }
        }

        private void ExpireSession(TowerBanSession session)
        {
            foreach (var user in session.Users)
            {
                if (!user.Choice.HasValue)
                {
                    session.InitiateChannel.SendMessageAsync($"{user.User.Mention} has failed to respond in time for a tower banning session.");
                }
            }
            sessions.Remove(session);
        }

        public bool IsInSession(params IUser[] users)
        {
            return sessions.Any(x => x.Users.Select(y => y.User.Id).Intersect(users.Select(y => y.Id)).Any());
        }

        public bool StartSession(IUser userA, IUser userB, ITextChannel textChannel)
        {
            if (IsInSession(userA, userB))
            {
                return false;
            }

            var session = new TowerBanSession();
            session.Users[0] = new TowerBanUser { User = userA };
            session.Users[1] = new TowerBanUser { User = userB };
            session.InitiateChannel = textChannel;

            sessions.Add(session);

            return true;
        }

        public TowerBanSession ProcessChoice(IUser user, Tower choice)
        {
            var session = sessions.SingleOrDefault(x => x.Users.Any(u => u.User.Id == user.Id));

            if (session == null)
            {
                return null;
            }

            var sessionUser = session.Users.Single(x => x.User.Id == user.Id);
            sessionUser.Choice = (Tower?)choice;

            if (session.Users.All(x => x.Choice.HasValue))
            {
                CompleteSession(session);
            }

            return session;
        }

        private void CompleteSession(TowerBanSession session)
        {
            session.InitiateChannel.SendMessageAsync($"The banned tower(s) between {session.Users.Select(x => x.User.Mention).JoinLines(" and ")} will be " +
                $"{session.Users.Select(x => x.Choice.Value).Distinct().Select(x => x.ToString("f")).JoinLines(" and ")}");

            sessions.Remove(session);
        }

        public string ListTowners()
        {
            return Enum.GetValues(typeof(Tower)).Cast<Tower>().Select(x => $"{x.ToString("d")}. {x.ToString("g")}").JoinLines();
        }
    }
}
