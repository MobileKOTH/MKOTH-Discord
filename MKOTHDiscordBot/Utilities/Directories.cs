﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace MKOTHDiscordBot
{
    public static class Directories
    {
        private static class FoldersNames
        {
            public static string Data => ConfigurationManager.AppSettings["DataFolderName"];
            public static string Logs => ConfigurationManager.AppSettings["LogsFolderName"];
        }

        private static class FileNames
        {
            public static string Config_json => ConfigurationManager.AppSettings["Config.json File"];
            public static string GeneralLogs_md => ConfigurationManager.AppSettings["General Logs.md File"];
            public static string ErrorLogs_md => ConfigurationManager.AppSettings["Error Logs.md File"];
            public static string ChatLogs_md => ConfigurationManager.AppSettings["Chat Logs.md File"];
            public static string ChatHistory_dat => ConfigurationManager.AppSettings["ChatHistory.dat File"];
        }

        /// <summary>
        /// The application root directory.
        /// </summary>
        public static string Root => Directory.GetParent(Directory.GetCurrentDirectory()).FullName + @"\";

        /// <summary>
        /// The full path data folder, resides outside of the root directory.
        /// </summary>
        public static string DataFolder => Root + FoldersNames.Data;

        /// <summary>
        /// The full folder path for logs, resides outside of the root directory.
        /// </summary>
        public static string LogsFolder => Root + FoldersNames.Logs;


        public static string ConfigFile => Root + FileNames.Config_json;
        public static string GeneralLogsFile => LogsFolder + FileNames.GeneralLogs_md;
        public static string ErrorLogsFile => LogsFolder + FileNames.ErrorLogs_md;
        public static string ChatLogsFile => LogsFolder + FileNames.ChatLogs_md;
        public static string ChatHistoryFile => DataFolder + FileNames.ChatHistory_dat;
    }
}