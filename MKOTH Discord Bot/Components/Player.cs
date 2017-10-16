using System;
using System.Collections.Generic;

namespace MKOTH_Discord_Bot
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
        private string name = "";
        private string playerclass = PlayerClass.SQUIRE ;
        private ulong discordid = 0;
        private bool isHoliday = false;
        private bool isRemoved = false;

        public static List<Player> List = new List<Player>();

        public Player()
        {
            name = PlayerStatus.UNKNOWN;
        }

        public Player(string name, string playerclass, ulong discordid, bool isHoliday, bool isRemoved)
        {
            this.name = name;
            this.playerclass = playerclass;
            this.discordid = discordid;
            this.isHoliday = isHoliday;
            this.isRemoved = isRemoved;

            List.Add(this);
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
                var player = new Player(item[0], item[2], discordid, (item[10] == PlayerStatus.HOLIDAY) ? true : false, (item[10] == PlayerStatus.REMOVED) ? true : false);
            }
        }

        public string Name { get => name; set => name = value; }
        public string Playerclass { get => playerclass; set => playerclass = value; }
        public ulong Discordid { get => discordid; set => discordid = value; }
        public bool IsHoliday { get => isHoliday; set => isHoliday = value; }
        public bool IsRemoved { get => isRemoved; set => isRemoved = value; }
    }
}
