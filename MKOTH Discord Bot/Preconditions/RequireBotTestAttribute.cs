using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    public class RequireBotTestAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return Task.Run(() =>
            {
                if (context.Guild == null)
                {
                    goto failedProcedure;
                }
                if (context.Guild == Globals.TestGuild.Guild)
                {
                    return PreconditionResult.FromSuccess();
                }

                failedProcedure:
                return PreconditionResult.FromError("This command can only be used in the development environment.");
            });
        }

        public override string ToString()
        {
            return "Test environment only";
        }
    }
}
