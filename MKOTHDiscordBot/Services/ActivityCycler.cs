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
                    var kingname = "No king yet";
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
                    var count = 0;
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
        private Timer changeTimer = new Timer(15000);


        public ActivityCycler(DiscordSocketClient client)
        {
            this.client = client;

            activity = (helpHint, ActivitySequence.Last());
            changeTimer.Elapsed += async (_, __) => await ChangeActivityAsync();

            client.Connected += () =>
            {
                changeTimer.Start();
                Logger.Log("Client Connected", LogType.ClientEvent);
                return Task.CompletedTask;
            };

            client.Disconnected += (_) =>
            {
                changeTimer.Stop();
                Logger.Log("Client Disconnected", LogType.ClientEvent);
                return Task.CompletedTask;
            };

            Logger.Debug("Started", nameof(ActivityCycler));
        }

        public async Task ChangeActivityAsync()
        {
            if (Program.TestMode)
            {
                return;
            }

            try
            {
                await client.SetActivityAsync(activity.current);

                if (activity.current is HelpHint)
                {
                    setNextActivity();
                    return;
                }

                activity.last = activity.current;
                activity.current = helpHint;

                void setNextActivity()
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
