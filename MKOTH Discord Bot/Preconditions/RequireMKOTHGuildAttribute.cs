using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    using static Globals.MKOTHGuild;

    public class RequireMKOTHGuildAttribute : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return await Task.Run(() =>
            {
                if (context.Guild.Id == Guild.Id)
                {
                    return PreconditionResult.FromSuccess();
                }
                else
                {
                    return PreconditionResult.FromError("This command can only be used in MKOTH Server.");
                }
            });
        }
    }
}
