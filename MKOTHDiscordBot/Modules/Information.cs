using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Provides the various information about MKOTH.")]
    [Remarks("Module B")]
    public class Information : ModuleBase<SocketCommandContext>
    {
        [Command("Invite")]
        [Alias("InviteLink")]
        [Summary("Gets the Discord invite link to this server.")]
        public async Task Invite() 
            => await ReplyAsync("https://discord.me/MKOTH");
    }
}
