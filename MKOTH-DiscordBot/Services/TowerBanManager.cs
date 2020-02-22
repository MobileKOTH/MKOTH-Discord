﻿using System;
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

    [Flags]
    public enum Tower
    {
        Dart = 1,
        Farm,
        Ace,
        Ice,
        Wiz,
        Heli,
        Sub,
        Boat,
        Spike,
        Glue,
        Sniper,
        Mortar,
        Boomer,
        Ninja,
        Engi,
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
        private Timer timer;

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
            for (int i = 0; i < session.Users.Length; i++)
            {
                var user = session.Users[i];
                if (!user.Choice.HasValue)
                {
                    session.InitiateChannel.SendMessageAsync($"{user.User.Mention} has failed to respond in time for a ban tower session.");
                }
            }
            sessions.Remove(session);
        }

        public bool StartSession(IUser userA, IUser userB, ITextChannel textChannel)
        {
            if (sessions.Any(x => x.Users.Any(u => u.User.Id == userA.Id || u.User.Id == userB.Id)))
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
            session.InitiateChannel.SendMessageAsync($"The tower ban between {session.Users.Select(x => x.User.Mention).JoinLines(" and ")} will be " +
                $"{session.Users.Select(x => x.Choice.Value).Distinct().Select(x => x.ToString("g")).JoinLines(",")}");

            sessions.Remove(session);
        }

        public string ListTowners()
        {
            return Enum.GetValues(typeof(Tower)).Cast<Tower>().Select(x => $"{x.ToString("d")}. {x.ToString("g")}").JoinLines();
        }
    }
}