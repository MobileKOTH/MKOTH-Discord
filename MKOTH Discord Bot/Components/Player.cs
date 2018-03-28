using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            REMOVED = "Removed",
            UNKNOWN = "Unknown";
    }

    public class Player
    {
        protected string name = "";
        protected ulong discordid = 0;
        string playerclass = PlayerClass.SQUIRE ;
        bool isHoliday = false;
        bool isRemoved = false;

        public string Name { get => name; set => name = value; }
        public string Playerclass { get => playerclass; set => playerclass = value; }
        public ulong Discordid { get => discordid; set => discordid = value; }
        public bool IsHoliday { get => isHoliday; set => isHoliday = value; }
        public bool IsRemoved { get => isRemoved; set => isRemoved = value; }
        public int Wins { get; set; }
        public int Loss { get; set; }
        public int Draws { get; set; }

        public static List<Player> List = new List<Player>();

        public Player()
        {
            name = PlayerStatus.UNKNOWN;
        }

        public Player(string name, string playerclass, int wins, int loss, int draws, ulong discordid, bool isHoliday, bool isRemoved)
        {
            this.name = name;
            this.playerclass = playerclass;
            this.discordid = discordid;
            this.isHoliday = isHoliday;
            this.isRemoved = isRemoved;

            Wins = wins;
            Loss = loss;
            Draws = draws;

            List.Add(this);
        }

        public Player(string name, ulong discordid)
        {
            this.name = name;
            this.discordid = discordid;
        }

        public static Player Fetch(ulong playerlid)
        {
            foreach (var item in List)
            {
                if (item.discordid == playerlid)
                {
                    return item;
                }
            }
            return new Player();
        }

        public static Player Fetch(string playername)
        {
            foreach (var item in List)
            {
                if (item.name == playername)
                {
                    return item;
                }
            }
            return new Player();
        }

        public static void InitialiseList(string tsv)
        {
            List.Clear();
            var lines = tsv.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 1; i < lines.Length; i++)
            {
                var item = lines[i].Split('\t');
                string discordidstr = item[9];
                if (!ulong.TryParse(discordidstr, out ulong discordid))
                {
                    discordid = 0;
                }
                var player = new Player(item[0], item[2].Replace(" (Knight)", ""), int.Parse(item[3]), int.Parse(item[4]), int.Parse(item[5]), discordid, (item[10] == PlayerStatus.HOLIDAY) ? true : false, (item[10] == PlayerStatus.REMOVED) ? true : false);
            }
        }
    }

    public class PlayerCode : Player
    {
        int codeid;

        public static List<PlayerCode> CodeList = new List<PlayerCode>();

        public PlayerCode(string name, ulong discordid, int codeid) : base(name, discordid)
        {
            this.name = name;
            this.discordid = discordid;
            this.codeid = codeid;
            CodeList.Add(this);
        }

        public static async Task Load()
        {
            try
            {
                var starttime = DateTime.Now;
                var response = await new System.Net.WebClient().DownloadStringTaskAsync("https://docs.google.com/spreadsheets/d/e/2PACX-1vSITdXPzQ_5eidATjL9j7uBicp4qvDuhx55IPvbMJ_jor8JU60UWCHwaHdXcR654W8Tp6VIjg-8V7g0/pub?gid=282944341&single=true&output=tsv");
                Player.InitialiseList(response);
                var channel = Globals.MKOTHGuild.PlayerID as ISocketMessageChannel;
                var messages = await channel.GetMessagesAsync(100, CacheMode.AllowDownload, null).FlattenAsync();
                if (messages.Count() <= 1) return;
                CodeList.Clear();
                foreach (var msg in messages)
                {
                    var embed = msg.Embeds.First();
                    foreach (var field in embed.Fields)
                    {
                        var player = Player.Fetch(field.Name);
                        PlayerCode playercode = new PlayerCode(field.Name, player.Discordid, int.Parse(field.Value));
                    }
                }
                Logger.Log("Time used: " + (DateTime.Now - starttime).TotalMilliseconds + " ms", LogType.PLAYERDATALOAD);
            }
            catch (Exception e)
            {
                Logger.Log(e.Message.AddLine() + e.StackTrace, LogType.ERROR);
            }
        }

        public static int FetchCode(ulong discordid, DiscordSocketClient client)
        {
            int code = 0;
            if (CodeList.Count < 1)
            {
                Load().RunSynchronously();
            }
            foreach (var item in CodeList)
            {
                if (item.discordid == discordid)
                {
                    return item.codeid;
                }
            }
            return code;
        }

        public int Codeid { get => codeid; set => codeid = value; }
    }
}
