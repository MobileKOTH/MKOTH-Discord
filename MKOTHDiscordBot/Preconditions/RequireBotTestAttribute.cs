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
            if (context.Guild == null)
            {
                goto failedProcedure;
            }
            if (context.Guild == ApplicationContext.TestGuild.Guild)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            failedProcedure:
            return Task.FromResult(PreconditionResult.FromError("This command can only be used in the development environment."));
        }

        public override string ToString()
            => "Test environment only";
    }
}
