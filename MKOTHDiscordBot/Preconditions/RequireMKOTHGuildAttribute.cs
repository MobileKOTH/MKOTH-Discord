using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    using static ApplicationContext.MKOTHGuild;

    public class RequireMKOTHGuildAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Guild == null)
            {
                goto failedProcedure;
            }
            if (context.Guild.Id == Guild.Id)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            failedProcedure:
            return Task.FromResult(PreconditionResult.FromError("This command can only be used in MKOTH Server."));
        }

        public override string ToString()
            => "Require MKOTH Discord Server";
    }
}
