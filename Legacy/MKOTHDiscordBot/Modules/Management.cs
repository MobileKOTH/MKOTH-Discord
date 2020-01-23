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
    [Remarks("Module D")]
    public class Management : ModuleBase<SocketCommandContext>
    {
        [Command("Alt")]
        [Alias("user")]
        [Summary("Checks the user's registration and server join date.")]
        public async Task Alt(IGuildUser user = null)
        {
            user = user ?? Context.User as IGuildUser;
            var isThisBot = user.Id == Context.Client.CurrentUser.Id;
            if (!isThisBot)
            {
                user = await user.Guild.GetUserAsync(user.Id, CacheMode.AllowDownload);
            }
            var resgistrationDate = user.CreatedAt;
            var joinedDate = user.JoinedAt.Value;
            var difference = joinedDate - resgistrationDate;
            var activity = !isThisBot ? user.Activity : Context.Client.Activity;

            var embed = new EmbedBuilder()
                .WithColor(Color.Purple)
                .WithAuthor(user)
                .WithDescription($"**Registered:** {resgistrationDate.ToString("R")}\n" +
                $"**Joined:** {joinedDate.ToString("R")}\n" +
                $"**Difference:** {difference.AsRoundedDuration()}");

            if (activity != null)
            {
                var type = $"{Enum.GetName(typeof(ActivityType), activity.Type).ToLower()}";
                var name = activity.Name;
                if (activity is StreamingGame stream)
                {
                    name = $"[{stream.Name}]({stream.Url})";
                }
                if (activity is RichGame game)
                {
                    name = $"{game.Name} ({game.State} - {game.Details})";
                    embed.WithImageUrl(game.LargeAsset.GetImageUrl())
                        .WithThumbnailUrl(game.SmallAsset.GetImageUrl());
                }
                embed.Description += $"\n\nThe user is currently **{type}:** {name}";
            }

            await ReplyAsync(user == Context.User ? "Checking yourself." : string.Empty, embed: embed.Build());
        }
    }
}
