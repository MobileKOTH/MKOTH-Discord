using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKOTHDiscordBot
{
    public static class ErrorResolver
    {
        public static int CriticalErrors = 0;
        public static int Threshold = 3;

        public static async Task Handle(Exception error, bool countAsRestartableCritical = true)
        {
            try
            {
                string stacktrace = error.StackTrace ?? "Null Stacktrace";
                stacktrace = stacktrace.SliceFront(1800);
                await ApplicationContext.MKOTHHQGuild.Log.SendMessageAsync(error.Message + stacktrace.MarkdownCodeBlock("yaml"));
                if (countAsRestartableCritical && ++CriticalErrors > Threshold)
                {
                    throw new TooManyErrorsException();
                }
            }
            catch (TooManyErrorsException e)
            {
                await Handle(e, false);
                ApplicationManager.RestartApplication(ApplicationContext.MKOTHHQGuild.Log.Id);
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
