using System;
using System.IO;

namespace MKOTHDiscordBot
{
    public enum LogType { DIRECTMESSAGE, ERROR, TRASHREPLYTIME , NOREPLYFOUND, CHATSAVETIME, PLAYERDATALOAD};

    public class Logger
    {

        public static void Log(string log, LogType type)
        {
            switch (type)
            {
                case LogType.TRASHREPLYTIME:

                case LogType.NOREPLYFOUND:
                    writeLog("TrashLogs.txt");
                    break;

                default:
                    writeLog("Logs.txt");
                    break;
            }

            void writeLog(string filepath)
            {
                using (StreamWriter sw = File.AppendText(Utilities.ContextPools.DataPath + filepath))
                {
                    sw.WriteLine(DateTime.Now.ToLocalTime().ToString().AddTab() + type);
                    sw.WriteLine(log);
                    sw.WriteLine("");
                    Console.WriteLine(DateTime.Now.ToLocalTime().ToString().AddTab() + type.ToString().AddLine() + log.AddLine());
                }
            }
        }

        public static void Debug(object obj, string variablename)
        {
            string type = obj.GetType().ToString();
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
                Console.WriteLine(type.AddSpace() + variablename.AddTab().AddLine() + json);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Error generating object data".AddTab() + obj.GetType().ToString().AddTab() + variablename.AddLine() + 
                    e.Message.AddLine() + 
                    e.StackTrace);
            }
        }
    }
}
