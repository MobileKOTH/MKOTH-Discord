using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;

namespace MKOTHDiscordBot.Services
{
    [SingletonService("Display status showm in Discord.", ForceInstantiate = true)]
    public class ActivityCycler
    {
        class HelpHint : IActivity
        {
            public string Name { get; } = ".help for general help";
            public ActivityType Type { get; } = ActivityType.Listening;
        }

        class InfoHint : IActivity
        {
            public string Name { get; } = ".info MKOTH information";
            public ActivityType Type { get; } = ActivityType.Listening;
        }

        class ShowKing : IActivity
        {
            public string Name
            {
                get
                {
                    var kingname = Player.List.First(x => x.Class == PlayerClass.KING).Name;
                    var kingstatus = "King: " + kingname.SliceBack(18);
                    return kingstatus;
                }
            }
            public ActivityType Type { get; } = ActivityType.Watching;
        }

        class SubmissionHint : IActivity
        {
            public string Name { get; } = ".submit to submit series";
            public ActivityType Type { get; } = ActivityType.Listening;
        }

        class GamesCount : IActivity
        {
            public string Name
            {
                get
                {
                    var count = Player.List.Sum(x => (x.Wins + x.Loss + x.Draws) / 2);
                    var gamestatus = count + " total games played";
                    return gamestatus;
                }
            }
            public ActivityType Type { get; } = ActivityType.Streaming;
        }

        private readonly LinkedList<IActivity> ActivitySequence = new LinkedList<IActivity>(new IActivity[]
        {
            new InfoHint(),
            new ShowKing(),
            new GamesCount(),
            new SubmissionHint(),
        });

        private readonly HelpHint helpHint = new HelpHint();

        private DiscordSocketClient client;
        private (IActivity current, IActivity last) activity;
        private Timer statusTimer = new Timer(15000);


        public ActivityCycler(DiscordSocketClient client)
        {
            this.client = client;

            activity = (helpHint, ActivitySequence.Last());
            statusTimer.Elapsed += async (_, __) => await ChangeStatusAsync();

            client.Connected += () =>
            {
                statusTimer.Start();
                Logger.Log("Client Connected", LogType.ClientEvent);
                return Task.CompletedTask;
            };

            client.Disconnected += (_) =>
            {
                statusTimer.Stop();
                Logger.Log("Client Disconnected", LogType.ClientEvent);
                return Task.CompletedTask;
            };

            Logger.Debug("Started", nameof(ActivityCycler));
        }

        public async Task ChangeStatusAsync()
        {
            if (Program.TestMode)
            {
                return;
            }

            try
            {
                Console.WriteLine(activity);

                await client.SetActivityAsync(activity.current);

                if (activity.current is HelpHint)
                {
                    setNextStatus();
                    return;
                }

                activity.last = activity.current;
                activity.current = helpHint;

                void setNextStatus()
                {
                    var targetNode = ActivitySequence.Find(activity.last);
                    activity.current = targetNode.Next == null ? targetNode.List.First.Value : targetNode.Next.Value;
                }
            }
            catch (Exception e)
            {
                await ErrorResolver.SendErrorAndCheckRestartAsync(e);
            }
        }
    }
}
