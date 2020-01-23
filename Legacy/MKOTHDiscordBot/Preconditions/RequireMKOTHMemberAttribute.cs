using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    public class RequireMKOTHMemberAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return Task.FromResult(PreconditionResult.FromSuccess());

            error:
            return Task.FromResult(PreconditionResult.FromError("You need to be MKOTH Member in order to do that."));
        }

        public override string ToString()
            => "Require MKOTH Member";
    }
}
