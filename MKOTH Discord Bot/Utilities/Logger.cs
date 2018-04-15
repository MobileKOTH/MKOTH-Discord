using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord.WebSocket;
using MKOTHDiscordBot.Utilities;

namespace MKOTHDiscordBot
{
    public enum LogType { DIRECTMESSAGE, ERROR, TRASHREPLY , NOREPLYFOUND, CHATSAVETIME, PLAYERDATALOAD};

    public class Logger
    {
        public static int ResponderErrors = 0;

        public static void Log(string log, LogType type)
        {
            switch (type)
            {
                case LogType.TRASHREPLY:

                case LogType.NOREPLYFOUND:
                    writeLog(Globals.Directories.ChatLogsFile);
                    break;

                case LogType.ERROR:
                    writeLog(Globals.Directories.ErrorLogsFile);
                    break;

                default:
                    writeLog(Globals.Directories.GeneralLogsFile);
                    break;
            }

            void writeLog(string directory)
            {
                try
                {
                    using (StreamWriter sw = File.AppendText(directory))
                    {
                        string text =
                            "## " + DateTime.Now.ToLocalTime().ToString().AddSpace() + type.ToString().AddLine() +
                            log.AddMarkDownLine();
                        sw.WriteLine(text);
                        Console.ResetColor();
                        Console.WriteLine(text);
                    }
                }
                catch (Exception error)
                {
                    if (error.GetType() == typeof(DirectoryNotFoundException))
                    {
                        new FileInfo(directory).Directory.Create();
                        writeLog(directory);
                    }
                    Console.WriteLine(error.Message.AddLine() + error.StackTrace);
                }
            }
        }

        public static void LogError(Exception error)
        {
            Log("### " + error.Message.AddMarkDownLine() + 
                error.StackTrace.MarkdownCodeBlock("diff"), LogType.ERROR);
        }

        public static void Debug(object obj, string description)
        {
            string type = obj.GetType().ToString();
            try
            {
                string json = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                });
                Console.WriteLine(
                    type.AddSpace() + description.AddTab().AddLine() + 
                    json);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Error generating object data".AddTab() + obj.GetType().ToString().AddTab() + description.AddLine() +
                    e.Message.AddLine() +
                    e.StackTrace);
            }
        }

        public static async Task SendError(Exception error)
        {
            try
            {
                string stacktrace = error.StackTrace;
                stacktrace = stacktrace.SliceFront(1800);
                await Responder.SendToChannel(Globals.TestGuild.BotTest, error.Message + stacktrace.MarkdownCodeBlock("yaml"));
                if (++ResponderErrors > 3)
                {
                    await SendError(new Exception("The application has experienced too many errors and is attempting to auto restart"));
                    Modules.System.RestartStatic();
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
            LogError(error);
        }
    }
}
