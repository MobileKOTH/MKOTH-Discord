using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord.WebSocket;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot
{
    public enum LogType
    {
        DirectMessage,
        Error,
        TrashReply ,
        NoTrashReplyFound,
        ChatSaveTime,
        PlayerDataLoad,
        ClientEvent
    };

    public static class Logger
    {
        public static int ResponderErrors = 0;

        public static void Log(string log, LogType type)
        {
            switch (type)
            {
                case LogType.TrashReply:

                case LogType.NoTrashReplyFound:
                    writeLog(Directories.ChatLogsFile);
                    break;

                case LogType.Error:
                    writeLog(Directories.ErrorLogsFile);
                    break;

                default:
                    writeLog(Directories.GeneralLogsFile);
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
                catch (DirectoryNotFoundException error)
                {
                    Console.WriteLine($"Catched: {error.Message}");
                    new FileInfo(directory).Directory.Create();
                    writeLog(directory);
                }
                catch (Exception error)
                {
                    Console.WriteLine(error.Message.AddLine() + error.StackTrace);
                }
            }
        }

        public static void LogError(Exception error)
        {
            Log("### " + error.Message.AddMarkDownLine() + 
                error.StackTrace.MarkdownCodeBlock("diff"), LogType.Error);
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

        public static async Task SendErrorAsync(Exception error)
        {
            try
            {
                string stacktrace = error.StackTrace ?? "Null Stacktrace";
                stacktrace = stacktrace.SliceFront(1800);
                await ResponseService.Instance.SendToChannelAsync(ApplicationContext.TestGuild.BotTest, error.Message + stacktrace.MarkdownCodeBlock("yaml"));
                if (++ResponderErrors > 3)
                {
                    ResponderErrors = 0;
                    throw new TooManyErrorsException();
                }
            }
            catch (TooManyErrorsException e)
            {
                await SendErrorAsync(e);
                ApplicationManager.RestartApplication(ApplicationContext.TestGuild.BotTest.Id);
            }
            catch (Exception e)
            {
                LogError(e);
            }
            finally
            {
                LogError(error);
            }
        }

        private class TooManyErrorsException : Exception
        {
            public override string Message => "The application has experienced too many errors and is attempting to auto restart";

            public TooManyErrorsException()
            {

            }
        }
    }
}
