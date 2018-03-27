﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace MKOTHDiscordBot
{
    public static class Extensions
    {
        #region String
        public static string AddLine(this String str)
        {
            str += Environment.NewLine;
            return str;
        }

        public static string AddTab(this String str)
        {
            str += "\t";
            return str;
        }

        public static string AddSpace(this String str)
        {
            str += " ";
            return str;
        }

        public static string Slice(this String str, int lengthtokeep)
        {
            string leftoverCover = "...";
            str = str.Length > lengthtokeep ? str.Take(lengthtokeep - leftoverCover.Length) + leftoverCover : str;
            return str;
        }
        #endregion

        #region IGuildUser
        public static string GetDisplayName(this IGuildUser user)
        {
            return user.Nickname ?? user.Username;
        }
        #endregion
    }
}
