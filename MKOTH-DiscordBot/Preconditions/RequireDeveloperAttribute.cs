using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKOTHDiscordBot
{
    public class RequireDeveloperAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User.Id == ApplicationContext.BotOwner.Id)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("This command can only be used by the developer of the bot."));
            }
        }

        public override string ToString()
        {
            return "Require developer";
        }
    }
}
