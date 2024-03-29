﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKOTHDiscordBot
{
    public enum LogType
    {
        DirectMessage,
        Error,
        TrashReply,
        NoTrashReplyFound,
        ClientEvent,
        AnimeListLoadTime
    };

    public static class Logger
    {
        public static void Log(object obj, LogType type)
        {
            Log(JsonConvert.SerializeObject(obj, Formatting.Indented), type);
        }
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

        public static void Debug(object obj, string description = "[DEBUG]")
        {
            string type = $"<{obj.GetType().ToString()}>";
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
    }
}
