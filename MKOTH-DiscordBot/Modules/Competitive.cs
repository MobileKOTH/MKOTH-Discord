using System;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Contains the commands for MKOTH series and player activities.")]
    [Remarks("Module B")]
    public class Competitive : InteractiveBase
    {
        private readonly DiscordSocketClient client;
        private readonly SubmissionRateLimiter submissionRateLimiter;
        private readonly SeriesService seriesService;
        public Competitive(IServiceProvider services)
        {
            client = services.GetService<DiscordSocketClient>();
            seriesService = services.GetService<SeriesService>();
            submissionRateLimiter = services.GetService<SubmissionRateLimiter>();
        }
        [Command("Submit")]
        public async Task Submit()
        {
            if (submissionRateLimiter.Audit(Context))
            {
                return;
            }

            await ReplyAsync("Test Submit");
        }

        [Command("reaction")]
        public async Task Test_ReactionReply()
        {
            var msg = await ApplicationContext.MKOTHHQGuild.Log.SendMessageAsync("test");
            await msg.AddReactionAsync(new Emoji("👍"));
            await msg.AddReactionAsync(new Emoji("👎"));
            var callback = new InlineReactionCallback(Interactive, Context, new ReactionCallbackData("text", null, false, true)
                .WithCallback(new Emoji("👍"), (c, r) => c.Channel.SendMessageAsync($"{r.User.Value.Mention} replied with 👍"))
                .WithCallback(new Emoji("👎"), (c, r) => c.Channel.SendMessageAsync($"{r.User.Value.Mention} replied with 👎")));
            Interactive.AddReactionCallback(msg, callback);
        }

        [Command("IsAdmin")]
        public async Task IsAdmin(IGuildUser user = null)
        {
            user ??= Context.User as IGuildUser;
            var embed = new EmbedBuilder()
                .WithDescription($"{user.Mention} is {(user.GuildPermissions.Administrator ? "" : "not ")} an admin.");
            await ReplyAsync(embed: embed.Build());
        }
    }
}
