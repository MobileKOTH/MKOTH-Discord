using System;
using System.Threading.Tasks;

namespace MKOTHDiscordBot
{
    public class ErrorResolver
    {
        public static int CriticalErrors = 0;

        public static async Task SendErrorAndCheckRestartAsync(Exception error, bool countAsRestartableCritical = true)
        {
            try
            {
                string stacktrace = error.StackTrace ?? "Null Stacktrace";
                stacktrace = stacktrace.SliceFront(1800);
                await ApplicationContext.TestGuild.BotTest.SendMessageAsync(error.Message + stacktrace.MarkdownCodeBlock("yaml"));
                if (countAsRestartableCritical && ++CriticalErrors > 3)
                {
                    throw new TooManyErrorsException();
                }
            }
            catch (TooManyErrorsException e)
            {
                await SendErrorAndCheckRestartAsync(e, false);
                ApplicationManager.RestartApplication(ApplicationContext.TestGuild.BotTest.Id);
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
