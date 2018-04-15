using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    public class RequireDeveloperAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return Task.Run(() =>
            {
                if (context.User.Id == Globals.BotOwner.Id)
                {
                    return PreconditionResult.FromSuccess();
                }
                else
                {
                    return PreconditionResult.FromError("This command can only be used by the developer of the bot.");
                }
            });
        }

        public override string ToString()
        {
            return "Require developer";
        }
    }
}
