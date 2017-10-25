using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MKOTH_Discord_Bot
{
    public enum LogType { DIRECTMESSAGE, ERROR, TRASHREPLYTIME , NOREPLYFOUND};

    public class Logger
    {

        public static void Log(string log, LogType type)
        {
            switch (type)
            {
                case LogType.TRASHREPLYTIME:
                case LogType.NOREPLYFOUND:
                    using (StreamWriter sw = File.AppendText(Utilities.ContextPools.DataPath + "TrashLogs.txt"))
                    {
                        sw.WriteLineAsync(DateTime.Now.ToLocalTime().ToString().AddTab() + type);
                        sw.WriteLineAsync(log);
                        sw.WriteLineAsync();
                        Console.WriteLine(DateTime.Now.ToLocalTime().ToString().AddTab() + type.ToString().AddLine() + log.AddLine());
                    }
                    break;

                default:
                    using (StreamWriter sw = File.AppendText(Utilities.ContextPools.DataPath + "Logs.txt"))
                    {
                        sw.WriteLineAsync(DateTime.Now.ToLocalTime().ToString().AddTab() + type);
                        sw.WriteLineAsync(log);
                        sw.WriteLineAsync();
                        Console.WriteLine(DateTime.Now.ToLocalTime().ToString().AddTab() + type.ToString().AddLine() + log.AddLine());
                    }
                    break;
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
                Console.WriteLine("Error generating object data".AddTab() + 
                    obj.GetType().ToString().AddTab() + variablename.AddLine()
                    + e.Message.AddLine() + e.StackTrace);
            }
        }
    }
}
