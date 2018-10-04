using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

using HttpUtility = System.Web.HttpUtility;

using static MKOTHDiscordBot.ApplicationContext.MKOTHGuild;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Contains the utilities for MKOTH needs and management.")]
    [Remarks("Module C")]
    public class Management : ModuleBase<SocketCommandContext>
    {
        [Command("Alt")]
        [Summary("Checks the user's registration and server join date.")]
        public async Task Alt(IGuildUser user = null)
        {
            user = user ?? Context.User as IGuildUser;
            var resgistrationDate = user.CreatedAt;
            var joinedDate = user.JoinedAt.Value;
            var difference = joinedDate - resgistrationDate;
            var activity = user.Activity;

            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithAuthor(user)
                .WithDescription($"**Registered:** {resgistrationDate.ToString("R")}\n" +
                $"**Joined:** {joinedDate.ToString("R")}\n" +
                $"**Difference:** {difference.AsRoundedDuration()}");

            if (activity != null)
            {
                embed.Description += $"\n\nThe user is currently **{Enum.GetName(typeof(ActivityType), activity.Type).ToLower()}:** {activity.Name}";
            }

            await ReplyAsync(user == Context.User ? "Checking yourself." : string.Empty, embed: embed.Build());
        }
    }
}
