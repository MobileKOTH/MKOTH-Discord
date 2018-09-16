using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord.WebSocket;

namespace MKOTHDiscordBot.Services
{
    [SingletonService("Display status showm in Discord.", ForceInstantiate = true)]
    public class StatusCycler
    {
        public enum StatusMessageType
        {
            Help,
            Info,
            King,
            GamesCount,
            SeriesSubmission
        };

        private readonly List<StatusMessageType> statusSequence = new List<StatusMessageType>
        {
            StatusMessageType.Info,
            StatusMessageType.King,
            StatusMessageType.GamesCount,
            StatusMessageType.SeriesSubmission,
        };

        private DiscordSocketClient client;
        private (StatusMessageType current, StatusMessageType last) status;
        private Timer statusTimer = new Timer(15000);


        public StatusCycler(DiscordSocketClient client)
        {
            this.client = client;

            status = (StatusMessageType.Help, statusSequence.Last());
            statusTimer.Elapsed += async (_, __) => await ChangeStatusAsync();

            client.Connected += () =>
            {
                statusTimer.Start();
                Logger.Log("CLient Connected", LogType.ClientEvent);
                return Task.CompletedTask;
            };

            client.Disconnected += (_) =>
            {
                statusTimer.Stop();
                Logger.Log("Client Disconnected", LogType.ClientEvent);
                return Task.CompletedTask;
            };

            Logger.Debug("Started", "Status Cycler Service");
        }

        public async Task ChangeStatusAsync()
        {
            if (Program.TestMode)
            {
                return;
            }

            try
            {
                Console.WriteLine(status);
                switch (status.current)
                {
                    case StatusMessageType.Help:
                        await client.SetGameAsync("| .help for general help");
                        setNextStatus();
                        return;

                    case StatusMessageType.Info:
                        await client.SetGameAsync("| .info MKOTH information");
                        break;

                    case StatusMessageType.SeriesSubmission:
                        await client.SetGameAsync("| .submit to submit series");
                        break;

                    case StatusMessageType.King:
                        var kingname = Player.List.First(x => x.Class == PlayerClass.KING).Name;
                        var kingstatus = "King: " + kingname.SliceBack(18);
                        await client.SetGameAsync(kingstatus);
                        break;

                    case StatusMessageType.GamesCount:
                        var count = Player.List.Sum(x => (x.Wins + x.Loss + x.Draws) / 2);
                        var gamestatus = count + " total games played";
                        await client.SetGameAsync(gamestatus);
                        break;
                }
                status.last = status.current;
                status.current = StatusMessageType.Help;

                void setNextStatus()
                {
                    var currentIndex = statusSequence.IndexOf(status.last);
                    currentIndex = currentIndex == statusSequence.Count - 1 ? 0 : currentIndex + 1;
                    status.current = statusSequence[currentIndex];
                }
            }
            catch (Exception e)
            {
                await Logger.SendErrorAsync(e);
            }
        }
    }
}
