using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    using static ApplicationContext.MKOTHGuild;

    public class RequireMKOTHModAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var guildUser = Guild.GetUser(context.User.Id);
            if (guildUser == null)
            {
                goto error;
            }

            if (ChatMods.Members.Any(x => x.Id == guildUser.Id))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            error:
            return Task.FromResult(PreconditionResult.FromError("You do not have the permission to do that."));
        }

        public override string ToString()
            => "Require MKOTH Mod";
    }
}
