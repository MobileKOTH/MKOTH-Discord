using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MKOTHDiscordBot.Modules
{
    using static Globals.MKOTHGuild;

    [Summary("Performs user moderations for MKOTH chat.")]
    [Remarks("Module D")]
    [RequireMKOTHMember]
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        static int banlimit = 3;

        [Command("Mute")]
        [Summary("Starts a petition to mute someone. Any MKOTH Member can initiate a mute with mute time of 10 minutes, along with a 67% approval rate. " +
            "A MKOTH chat moderator can have longer mute time and only needs 32% approval rate.")]
        [RequireDeveloper]
        public async Task Mute(IGuildUser user, int muteTimeMinutes, [Remainder]string reason)
        {
            var isMod = ChatMods.Members.Count(x => x.Id == Context.User.Id) > 0 ? true : false;
            muteTimeMinutes = isMod ? muteTimeMinutes : 10;

            var vote = new Vote(
                Context,
                "Mute User Vote",
                "**" + user.GetDisplayName() + $"** has been demanded to be muted for {muteTimeMinutes} minutes, cast your opinions by reacting below. " +
                $"This vote is initiated by a {(isMod ? "***moderator***" : "***member***")}, it needs a {(isMod ? "32" : "67")}% approval rate. " +
                "Only MKOTH Members' vote casts are considered.");

            await Task.CompletedTask;
        }

        [Command("Ban")]
        [Summary("Bans a user in MKOTH Server.")]
        [RequireMKOTHGuild]
        [RequireMKOTHMod]
        public async Task Ban(IGuildUser user, [Remainder] string reason = "no reason")
        {
            await BanAsync(user, reason, false);
        }

        [Command("SuperBan")]
        [Summary("Bans a user in MKOTH Server and prune their messages from the past 1 day.")]
        [RequireMKOTHGuild]
        [RequireMKOTHMod]
        public async Task SuperBan(IGuildUser user, [Remainder] string reason = "no reason")
        {
            await BanAsync(user, reason, true);
        }

        [Command("Kick")]
        [Summary("Kicks a user from the MKOTH server. Cannot kick a MKOTH Member.")]
        [RequireMKOTHGuild]
        [RequireMKOTHMod]
        public async Task Kick(IGuildUser user, [Remainder] string reason = "Not provided.")
        {
            if (IsModImmuneUser(user) || user.RoleIds.Contains(Member.Id))
            {
                await ReplyAsync("Cannot kick a moderator, a bot, a VIP or a MKOTH Member.");
                return;
            }

            await user.KickAsync(reason);
            await SendModResponseAsync(user, reason, "**kicked**");
        }

        [Command("ShowBanLimit")]
        [Summary("Shows the remaining amount of MKOTH Members the chat mods can ban.")]
        [RequireMKOTHMod]
        public async Task Showbanlimit()
        {
            await ReplyAsync("Ban limit: " + banlimit);
        }

        [Command("ResetBan")]
        [Summary("Resets the ban limit.")]
        [RequireDeveloper]
        public async Task ResetBan()
        {
            banlimit = 3;
            await ReplyAsync("Ban limit reset.");
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
                    var inviteLink = "https://discord.me/MKOTH";
                    await user.SendMessageAsync($"You are banned from the MKOTH Server by **{Context.User.Username}** for {para}. " +
                        $"If your ban is lifted, join back using the invite link below:\n\n" + inviteLink);
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
            await SendModResponseAsync(user, para, "**banned**");

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
            string text = $"User {type}: " + user.Mention.AddSpace() + user;
            await ReplyAsync(text, embed: embed.Build());
            await ((ITextChannel)ModLog).SendMessageAsync(text, embed: embed.Build());
        }
    }
}
