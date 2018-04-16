using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    public static class PlayerClass
    {
        public const string
            KING = "King",
            NOBLE = "Nobleman",
            SQUIRE = "Squire",
            VASSAL = "Vassal",
            PEASANT = "Peasant";
    }

    public static class PlayerStatus
    {
        public const string
            ACTIVE = "Active",
            HOLIDAY = "Holiday",
            REMOVED = "Removed";
    }

    public class Player
    {
        public string Name { get; set; }
        public string PlayerClass { get; set; }
        public ulong DiscordId { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsRemoved { get; set; }
        public bool IsUnknown { get; set; } = false;
        public int Rank { get; set; }
        public int Wins { get; set; }
        public int Loss { get; set; }
        public int Draws { get; set; }
        public int Points { get; set; }

        public string ELOString { get; set; }

        public int CodeId { get; set; }

        public static List<Player> List = new List<Player>();

        public Player()
        {
            IsUnknown = true;
        }

        public Player(int rank, string name, string playerclass, int points, string eloString, int wins, int loss, int draws, ulong discordid, bool isHoliday, bool isRemoved, int codeId)
        {
            Rank = rank;
            Name = name;
            PlayerClass = playerclass;
            Points = points;
            ELOString = eloString;
            DiscordId = discordid;
            IsHoliday = isHoliday;
            IsRemoved = isRemoved;

            Wins = wins;
            Loss = loss;
            Draws = draws;

            CodeId = codeId;

            List.Add(this);
        }

        public static Player Fetch(ulong playerId)
        {
            return List.Find(x => x.DiscordId == playerId) ?? new Player();
        }

        public static Player Fetch(string playerName)
        {
            return List.Find(x => x.Name == playerName) ?? new Player();
        }

        public static int FetchCode(ulong discordId)
        {
            int code = 0;
            var player = List.Find(x => x.DiscordId == discordId);
            if (player != null)
            {
                code = player.CodeId;
            }
            return code;
        }

        public string GetRankFieldString(bool boldName = false, bool hideRank = false)
        {
            string name = (boldName ? $"**{Name}**" : Name).SliceBack(28);
            string rank = hideRank ? $"{PlayerClass}" : Rank.ToString().PadLeft(2, '0');
            return $"`#{rank}`\t`{ELOString.Replace(",", "").Replace(":", ": ")}`\t`{Points.ToString().PadRight(3, ' ')}p`\t {name}\n";
        }

        public static async Task Load()
        {
            try
            {
                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                var playerDataTsvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vSITdXPzQ_5eidATjL9j7uBicp4qvDuhx55IPvbMJ_jor8JU60UWCHwaHdXcR654W8Tp6VIjg-8V7g0/pub?gid=282944341&single=true&output=tsv";
                var playerRankingJsonUrl = "https://script.google.com/macros/s/AKfycbzgXXIUc8PGq0-h-aZkZ9gfGBnBLi-BPn3JJ9cjV5B7ZbLu2eY/exec?resource=mkoth&item=ranking";
                var playerDataTask = new System.Net.WebClient().DownloadStringTaskAsync(playerDataTsvUrl);
                var playerRankingTask = new System.Net.WebClient().DownloadStringTaskAsync(playerRankingJsonUrl);

                var messagesTask = Globals.MKOTHGuild.PlayerID.GetMessagesAsync(100).FlattenAsync();
                await Task.WhenAll(playerDataTask, messagesTask, playerRankingTask);

                var playerData = await playerDataTask;
                var messages = await messagesTask;
                var playerRanking = await playerRankingTask;

                List<(string playerName, int codeId)> codeList = new List<(string playerName, int codeId)>();
                if (messages.Count() > 1)
                {
                    foreach (var msg in messages)
                    {
                        var embed = msg.Embeds.First();
                        foreach (var field in embed.Fields)
                        {
                            codeList.Add((field.Name, int.Parse(field.Value)));
                        }
                    }
                }

                Initialise(playerData, playerRanking, codeList);

                stopwatch.Stop();
                Logger.Log("**Time used:** `" + stopwatch.ElapsedMilliseconds + " ms`", LogType.PLAYERDATALOAD);

            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private static void Initialise(string tsv, string rankingJson, List<(string playerName, int codeId)> codeList)
        {
            List.Clear();
            var lines = tsv.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            var rankingList = JsonConvert.DeserializeObject<RankingResponse.Rootobject>(rankingJson).response.ToList();

            for (int i = 1; i < lines.Length; i++)
            {
                var item = lines[i].Split('\t');
                string discordidstr = item[9];
                if (!ulong.TryParse(discordidstr, out ulong discordid))
                {
                    discordid = 0;
                }
                var (playerName, codeId) = codeList.Find(x => x.playerName == item[0]);
                int rank = -1;
                int points = 0;
                string eloString = "Unknown";
                var player = rankingList.Find(x => x.Player_Name == item[0]);
                if (player != null)
                {
                    int.TryParse(player.Rank, out rank);
                    points = int.Parse(player.Points);
                    eloString = player.Main_ELO;
                }

                new Player(
                    rank,
                    item[0],
                    item[2].Replace(" (Knight)", ""),
                    points,
                    eloString,
                    int.Parse(item[3]), int.Parse(item[4]),
                    int.Parse(item[5]),
                    discordid,
                    (item[10] == PlayerStatus.HOLIDAY) ? true : false,
                    (item[10] == PlayerStatus.REMOVED) ? true : false,
                    codeId);
            }
        }

        public struct RankingResponse
        {

            public class Rootobject
            {
                public Request request { get; set; }
                public Response[] response { get; set; }
            }

            public class Request
            {
                public Parameter parameter { get; set; }
                public string contextPath { get; set; }
                public int contentLength { get; set; }
                public string queryString { get; set; }
                public Parameters parameters { get; set; }
            }

            public class Parameter
            {
                public string item { get; set; }
                public string resource { get; set; }
            }

            public class Parameters
            {
                public string[] item { get; set; }
                public string[] resource { get; set; }
            }

            public class Response
            {
                public string Rank { get; set; }
                public string Player_Name { get; set; }
                public string Class { get; set; }
                public string Points { get; set; }
                public string Main_ELO { get; set; }
            }
        }
    }
}
