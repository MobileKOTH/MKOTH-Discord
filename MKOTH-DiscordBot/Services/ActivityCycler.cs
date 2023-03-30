using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using MKOTHDiscordBot.Properties;

namespace MKOTHDiscordBot.Services
{
    public class ActivityCycler
    {
        class HelpHint : IActivity
        {
            private readonly string prefix;

            public HelpHint(string prefix)
            {
                this.prefix = prefix;
            }
            public string Name => $"{prefix}help for general help";
            public ActivityType Type => ActivityType.Listening;

            //public ActivityProperties Flags => ActivityProperties.None;
            public string Details { get; }
        }

        class ShowMemory : IActivity
        {
            public string Name => ApplicationManager.GetResourceUsage()
                .Forward(x => $"RAM: {x.ramUsageMB.ToString("N3")}MB CPU: {x.cpuUsagePercent}%");

            public ActivityType Type => ActivityType.Watching;

            //public ActivityProperties Flags => ActivityProperties.None;
            public string Details { get; }
        }


        private readonly DiscordSocketClient client;
        private readonly ErrorResolver resolver;
        private readonly string commandPrefix;
        private (IActivity current, IActivity last) activity;
        private Timer changeTimer = new Timer(15000);

        private readonly HelpHint helpHint;

        public ActivityCycler(DiscordSocketClient socketClient, ErrorResolver errorResolver, IOptions<AppSettings> setting)
        {
            client = socketClient;
            resolver = errorResolver;
            commandPrefix = setting.Value.Settings.DefaultCommandPrefix;
            helpHint = new HelpHint(commandPrefix);


            activity = (helpHint, activityList.First.Value);
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


        private readonly LinkedList<IActivity> activityList = new LinkedList<IActivity>(new IActivity[]
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
                    var targetNode = activityList.Find(activityList.Single(x => x.GetType() == activity.last.GetType()));
                    activity.current = targetNode.Next == null ? targetNode.List.First.Value : targetNode.Next.Value;
                }
            }
            catch (Exception e)
            {
                await resolver.Handle(e);
            }
        }
    }
}
