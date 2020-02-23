using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.Options;
using MKOTHDiscordBot.Properties;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;

namespace MKOTHDiscordBot.Services
{
    public class ErrorResolver
    {
        public int CriticalErrors = 0;
        public int Threshold = 3;
        private readonly DiscordLogger logger;

        public ErrorResolver(DiscordLogger discordLogger)
        {
            logger = discordLogger;
        }

        public async Task Handle(Exception error, bool countAsRestartableCritical = true)
        {
            try
            {
                string stacktrace = error.StackTrace ?? "Null Stacktrace";
                stacktrace = stacktrace.SliceFront(1800);
                await logger.LogAsync(error.Message + stacktrace.MarkdownCodeBlock("yaml"));
                if (countAsRestartableCritical && ++CriticalErrors > Threshold)
                {
                    throw new TooManyErrorsException();
                }
            }
            catch (TooManyErrorsException e)
            {
                await Handle(e, false);
                ApplicationManager.RestartApplication(logger.LogChannel.Id);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            finally
            {
                Logger.LogError(error);
            }
        }

        private class TooManyErrorsException : Exception
        {
            public override string Message => "The application has experienced too many errors and is attempting to auto restart";
        }
    }
}
