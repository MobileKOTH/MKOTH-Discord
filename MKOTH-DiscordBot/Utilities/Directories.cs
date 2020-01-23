using System;
using System.IO;
using System.Reflection;

namespace MKOTHDiscordBot
{
    public static class Directories
    {
        static Directories()
        {
            Console.WriteLine(Assembly.GetExecutingAssembly().Location);
        }

        private static class FoldersNames
        {
            public const string
                DATA = @"\Data\",
                LOGS = @"\Logs\";
        }

        private static class FileNames
        {
            public const string
                CREDENTIALS_JSON = "Credentials.json",
                GENERALLOGS_MD = "General Logs.md",
                ERRORLOGS_MD = "Error Logs.md",
                USERTAGS_TXT = "UserTags.txt",
                USERDATA_TXT = "UserData.txt",
                CHATLOGS_TXT = "Chat Logs.md",
                CHATHISTORY_DAT = "ChatHistory.dat";
        }

        /// <summary>
        /// Full directory string paths for an application folder.
        /// </summary>
        public static readonly string
            Root = Directory.GetParent(Directory.GetCurrentDirectory()).FullName + @"\",
            DataFolder = Root + FoldersNames.DATA,
            LogsFolder = Root + FoldersNames.LOGS;

        /// <summary>
        /// Full directory string path of an application file.
        /// </summary>
        public static readonly string
            UserTagsFile = DataFolder + FileNames.USERTAGS_TXT,
            UserDataFile = DataFolder + FileNames.USERDATA_TXT,
            Credentials = Root + FileNames.CREDENTIALS_JSON,
            GeneralLogsFile = LogsFolder + FileNames.GENERALLOGS_MD,
            ErrorLogsFile = LogsFolder + FileNames.ERRORLOGS_MD,
            ChatLogsFile = LogsFolder + FileNames.CHATLOGS_TXT,
            ChatHistoryFile = DataFolder + FileNames.CHATHISTORY_DAT;
    }
}
