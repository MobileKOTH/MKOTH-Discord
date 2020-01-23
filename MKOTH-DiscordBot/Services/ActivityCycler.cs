using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;

namespace MKOTHDiscordBot.Services
{
    public class ActivityCycler
    {
        class HelpHint : IActivity
        {
            public string Name => ".op help for general help";
            public ActivityType Type => ActivityType.Listening;
        }

        class ShowMemory : IActivity
        {
            public string Name => ApplicationManager.GetResourceUsage()
                .Forward(x => $"RAM: {x.ramUsageMB.ToString("N3")}MB CPU: {x.cpuUsagePercent}%");

            public ActivityType Type => ActivityType.Watching;
        }

        private readonly HelpHint helpHint = new HelpHint();

        private readonly DiscordSocketClient client;

        private (IActivity current, IActivity last) activity;
        private Timer changeTimer = new Timer(15000);


        public ActivityCycler(DiscordSocketClient client)
        {
            this.client = client;

            activity = (helpHint, GetActivityList().First());
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

        public LinkedList<IActivity> GetActivityList()
            => new LinkedList<IActivity>(new IActivity[]
            {
                new ShowMemory(),
            });

        public async Task ChangeActivityAsync()
        {
            if (Program.TestMode)
            {
                return;
            }

            try
            {
                await client.SetActivityAsync(activity.current);
                

                if (activity.current is StreamingGame)
                {
                    changeTimer.Interval = 30000;
                }
                else
                {
                    changeTimer.Interval = 15000;
                }

                if (activity.current is HelpHint)
                {
                    setNextActivity();
                    return;
                }

                activity.last = activity.current;
                activity.current = helpHint;

                void setNextActivity()
                {
                    var activitySequence = GetActivityList();
                    var targetNode = activitySequence.Find(activitySequence.Single(x => x.GetType() == activity.last.GetType()));
                    activity.current = targetNode.Next == null ? targetNode.List.First.Value : targetNode.Next.Value;
                }
            }
            catch (Exception e)
            {
                await ErrorResolver.Handle(e);
            }
        }
    }
}
