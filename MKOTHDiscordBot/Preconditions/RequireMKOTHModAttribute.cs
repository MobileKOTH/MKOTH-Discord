using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    using static Globals.MKOTHGuild;

    public class RequireMKOTHModAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return Task.Run(() =>
            {
                var guildUser = Guild.GetUser(context.User.Id);
                if (guildUser == null)
                {
                    goto error;
                }

                if (ChatMods.Members.FirstOrDefault(x => x.Id == guildUser.Id) != null)
                {
                    return PreconditionResult.FromSuccess();
                }

                error:
                return PreconditionResult.FromError("You do not have the permission to do that.");
            });
        }

        public override string ToString()
        {
            return "Require MKOTH Mod";
        }
    }
}
