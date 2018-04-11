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
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return await Task.Run(() =>
            {
                var guildUser = Guild.GetUser(context.User.Id);
                if (guildUser == null)
                {
                    goto error;
                }

                if (guildUser.Roles.Contains(ChatMods))
                {
                    return PreconditionResult.FromSuccess();
                }
                goto error;

                error:
                return PreconditionResult.FromError("You do not have the permission to do that.");
            });
        }
    }
}
