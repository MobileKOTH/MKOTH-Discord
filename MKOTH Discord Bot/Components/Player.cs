﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
        internal string name = "";
        internal ulong discordid = 0;
        string playerclass = PlayerClass.SQUIRE ;
        bool isHoliday = false;
        bool isRemoved = false;

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
                var player = new Player(item[0], item[2], discordid, (item[10] == PlayerStatus.HOLIDAY) ? true : false, (item[10] == PlayerStatus.REMOVED) ? true : false);
            }
        }

        public string Name { get => name; set => name = value; }
        public string Playerclass { get => playerclass; set => playerclass = value; }
        public ulong Discordid { get => discordid; set => discordid = value; }
        public bool IsHoliday { get => isHoliday; set => isHoliday = value; }
        public bool IsRemoved { get => isRemoved; set => isRemoved = value; }
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

        public static void Load(DiscordSocketClient client)
        {
            var response = new System.Net.WebClient().DownloadString("https://docs.google.com/spreadsheets/d/e/2PACX-1vSITdXPzQ_5eidATjL9j7uBicp4qvDuhx55IPvbMJ_jor8JU60UWCHwaHdXcR654W8Tp6VIjg-8V7g0/pub?gid=282944341&single=true&output=tsv");
            Player.InitialiseList(response);
            CodeList.Clear();
            var channel = client.GetGuild(271109067261476866UL).GetChannel(357201006301282309UL) as ISocketMessageChannel;
            var messages = channel.GetMessagesAsync(100, CacheMode.AllowDownload, null).Flatten().GetAwaiter().GetResult();
            foreach (var msg in messages)
            {
                string[] codeliststrarr = msg.Content.Split('\n');
                codeliststrarr[0] = codeliststrarr[0].Replace("**", "");
                var player = Player.Fetch(codeliststrarr[0]);
                PlayerCode playercode = new PlayerCode(codeliststrarr[0], player.discordid, int.Parse(codeliststrarr[1]));
            }
        }

        public static int FetchCode(ulong discordid, DiscordSocketClient client)
        {
            int code = 0;
            if (CodeList.Count < 1)
            {
                Load(client);
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