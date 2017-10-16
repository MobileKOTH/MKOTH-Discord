﻿using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MKOTH_Discord_Bot
{
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        static int banlimit = 3;

        [Command("ban")]
        public async Task Ban([Remainder] string para)
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            para += " ";
            var user = Context.Message.Author as IGuildUser;

            foreach (var role in user.RoleIds)
            {
                if (role == 349945390193180674L)
                {
                    if (Context.Message.MentionedUsers.Count != 1)
                    {
                        msg = await ReplyAsync("Invalid user mention");
                        return;
                    }

                    var banuser = Context.Message.MentionedUsers.First();
                    foreach (var banuserrole in (banuser as IGuildUser).RoleIds)
                    {
                        if (banuserrole == role)
                        {
                            await ReplyAsync("Cannot ban a moderator!");
                            return;
                        }
                    }
                    foreach (var banuserrole in (banuser as IGuildUser).RoleIds)
                    {
                        if (banuserrole == 347261976600248320L)
                        {
                            if (banlimit > 0)
                            {
                                embed.Title = "Reason: " + para.Replace(para.Substring(0, para.IndexOf(" ")), "");
                                embed.Description = "Moderator: " + Context.User.Mention + " " + Context.User.ToString();
                                await Context.Guild.AddBanAsync(banuser, 1, para.Replace(para.Substring(0, para.IndexOf(" ")), ""), null);
                                msg = await ReplyAsync("User Banned " + banuser, embed: embed);
                                banlimit--;
                                return;
                            }
                            else
                            {
                                await ReplyAsync("Ban limit for MKOTH members reached!");
                                return;
                            }
                        }
                    }

                    embed.Title = "Reason: " + para.Replace(para.Substring(0, para.IndexOf(" ")), "");
                    embed.Description = "Moderator: " + Context.User.Mention + " " + Context.User.ToString();
                    await Context.Guild.AddBanAsync(banuser, 1, para.Replace(para.Substring(0, para.IndexOf(" ")), ""), null);
                    msg = await ReplyAsync("User banned: " + banuser, embed: embed);
                    return;
                }
            }
            await ReplyAsync("You do not have the permission to ban!");
            return;
        }

        [Command("kick")]
        public async Task Kick([Remainder] string para)
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            para += " ";
            var user = Context.Message.Author as IGuildUser;

            foreach (var role in user.RoleIds)
            {
                if (role == 349945390193180674L)
                {
                    if (Context.Message.MentionedUsers.Count != 1)
                    {
                        msg = await ReplyAsync("Invalid user mention");
                        return;
                    }

                    var kickuser = Context.Message.MentionedUsers.First();
                    foreach (var kickuserrole in (kickuser as IGuildUser).RoleIds)
                    {
                        if (kickuserrole == role)
                        {
                            await ReplyAsync("Cannot kick a moderator!");
                            return;
                        }
                    }
                    foreach (var kickuserrole in (kickuser as IGuildUser).RoleIds)
                    {
                        if (kickuserrole == 349945390193180674L)
                        {
                            await ReplyAsync("Cannot kick a moderator!");
                            return;
                        }
                        if (kickuserrole == 347261976600248320L)
                        {
                            await ReplyAsync("No kicking of MKOTH members!");
                            return;
                        }
                    }

                    await (kickuser as IGuildUser).KickAsync();
                    embed.Title = "Reason: " + para.Replace(para.Substring(0, para.IndexOf(" ")), "");
                    embed.Description = "Moderator: " + Context.User.Mention + " " + Context.User.ToString();
                    msg = await ReplyAsync("User kicked: " + kickuser, embed: embed);
                    return;
                }
            }
            await ReplyAsync("You do not have the permission to kick!");
            return;
        }

        [Command("resetban")]
        [RequireOwner]
        public async Task ResetBan()
        {
            banlimit = 3;
            await ReplyAsync("Ban limit reset.");
        }

        [Command("showbanlimit")]
        [RequireOwner]
        public async Task Showbanlimit()
        {
            await ReplyAsync("Ban limit: " + banlimit);
        }
    }
}
