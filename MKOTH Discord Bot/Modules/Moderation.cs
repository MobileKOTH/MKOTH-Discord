using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MKOTHDiscordBot
{
    using static Globals.MKOTHGuild;

    [RequireMKOTHMod]
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        static int banlimit = 3;

        [Command("Ban")]
        [Summary("Ban an eligable user in MKOTH Server.")]
        [RequireMKOTHGuild]
        public async Task Ban(IGuildUser user, [Remainder] string reason = "Not Provided.")
        {
            await BanAsync(user, reason, false);
        }

        [Command("SuperBan")]
        [Summary("Ban an eligable user in MKOTH Server and prune their messages from the past 1 day.")]
        [RequireMKOTHGuild]
        public async Task SuperBan(IGuildUser user, [Remainder] string reason = "Not Provided.")
        {
            await BanAsync(user, reason, true);
        }

        [RequireMKOTHGuild]
        [Command("Kick")]
        [Summary("Kick a user from the MKOTH server.")]
        public async Task Kick(IGuildUser user, [Remainder] string reason = "Not Provided.")
        {
            if (IsModImmuneUser(user) || user.RoleIds.Contains(Member.Id))
            {
                await ReplyAsync("Cannot kick a moderator, a bot, a VIP or a MKOTH Member.");
                return;
            }

            await user.KickAsync(reason);
            await SendModResponseAsync(user, reason, "Kicked");
            return;
        }

        [Command("ResetBan")]
        [RequireOwner]
        public async Task ResetBan()
        {
            banlimit = 3;
            await ReplyAsync("Ban limit reset.");
        }

        [Command("ShowBanLimit")]
        [RequireOwner]
        public async Task Showbanlimit()
        {
            await ReplyAsync("Ban limit: " + banlimit);
        }

        private async Task BanAsync(IGuildUser user, string para, bool prune)
        {
            int daystoprune = prune ? 1 : 0;

            if (IsModImmuneUser(user))
            {
                await ReplyAsync("Cannot ban a moderator, a bot or a VIP.");
                return;
            }

            if (user.RoleIds.Contains(Member.Id))
            {
                if (banlimit > 0)
                {
                    banlimit--;
                    goto banProcedure;
                }
                else
                {
                    await ReplyAsync("Ban limit for MKOTH members has reached!");
                    return;
                }
            }

            banProcedure:
            await Context.Guild.AddBanAsync(user, daystoprune, para);
            await SendModResponseAsync(user, para, "Banned");

            return;
        }

        private bool IsModImmuneUser(IGuildUser user)
        {
            if (user.IsBot || user.RoleIds.Intersect(new ulong[] { ChatMods.Id, VIP.Id}).Count() > 0)
            {
                return true;
            }
            return false;
        }

        private async Task SendModResponseAsync(IGuildUser user, string para, string type)
        {
            EmbedBuilder embed = new EmbedBuilder
            {
                Title = "Reason: " + para,
                Description = "Moderator: " + Context.User.Mention + " " + Context.User.ToString(),
                Color = Color.Red
            };
            await ReplyAsync($"User {type} " + user.Mention.AddSpace() + user, embed: embed.Build());
            await ((ITextChannel)ModLog).SendMessageAsync($"User {type} " + user, embed: embed.Build());
        }
    }
}
