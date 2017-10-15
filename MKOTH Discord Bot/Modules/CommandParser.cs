using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Diagnostics;
using System.Net;

namespace MKOTH_Discord_Bot
{
    public class CommandParser : ModuleBase<SocketCommandContext>
    {
        static int banlimit = 3;
        WebClient WebRequester = new WebClient();

        [Command("info")]
        [Alias("stats")]
        public async Task Info()
        {
            EmbedBuilder embed = new EmbedBuilder();

            string prcName = Process.GetCurrentProcess().ProcessName;
            var counter = new PerformanceCounter("Process", "Working Set - Private", prcName);

            embed.WithTitle("Information");
            embed.WithDescription("Official MKOTH Management Bot. In early development and testing phase.");
            embed.WithUrl("https://mobilekoth.wordpress.com/");
            embed.WithThumbnailUrl("https://cdn.discordapp.com/attachments/341163606605299716/360336022745382912/13615239_1204861226212220_2613382245523520956_n.png");

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.WithName("Created by Cerlancism CY");
            author.WithIconUrl("https://cdn.discordapp.com/avatars/234242692303814657/536e902dca1564f8f49afdc2113e7ce0.png");
            embed.WithAuthor(author);

            embed.AddField("Commands Help", "`.mkothhelp`", false);
            embed.AddField("Library", "Discord.Net v1.0.2", true);
            embed.AddField("Memory", string.Format("{0:N2} MB", ((double)(counter.RawValue / 1024)) / 1024), true);

            embed.WithImageUrl("https://cdn.discordapp.com/attachments/271109067261476866/330727796647395330/Untitled12111.jpg");

            embed.WithFooter(a => a.Text =
            "Copyright 2017 © Mobile Koth");
            embed.WithCurrentTimestamp();
            embed.WithColor(Color.Orange);

            await ReplyAsync(string.Empty, embed: embed);
        }

        [Command("ping")]
        public async Task Ping()
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            msg = await ReplyAsync("`loading...`");

            await msg.ModifyAsync(x =>
            {
                x.Content = "`Bot delay: " + (msg.Timestamp - Context.Message.Timestamp).TotalMilliseconds + " ms`\n";
                x.Embed = new EmbedBuilder().WithDescription("Pong!\n" + "Server ID: " + Context.Guild.Id).Build();
            });
        }

        [Command("settest")]
        public async Task Settest()
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            if (Context.Message.Author.Id != 234242692303814657L)
            {
                return;
            }

            Program.ReplyToTestServer = !Program.ReplyToTestServer;

            if (Program.ReplyToTestServer)
            {
                msg = await ReplyAsync("Replying to test server");
            }
            else
            {
                msg = await ReplyAsync("Disabled replying to test server");
            }
        }

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
                            await ReplyAsync("Cant Ban a Moderator!");
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
                    msg = await ReplyAsync("User Banned " + banuser, embed: embed);
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
                            await ReplyAsync("Cannot Kick a Moderator!");
                            return;
                        }
                    }
                    foreach (var kickuserrole in (kickuser as IGuildUser).RoleIds)
                    {
                        if (kickuserrole == 349945390193180674L)
                        {
                            await ReplyAsync("Cannot Kick a Moderator!");
                            return;
                        }
                        if (kickuserrole == 347261976600248320L)
                        {
                            await ReplyAsync("No kicking of MKOTH Members!");
                            return;
                        }
                    }

                    await (kickuser as IGuildUser).KickAsync();
                    embed.Title = "Reason: " + para.Replace(para.Substring(0, para.IndexOf(" ")), "");
                    embed.Description = "Moderator: " + Context.User.Mention + " " + Context.User.ToString();
                    msg = await ReplyAsync("User Kicked " + kickuser, embed: embed);
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

        [Command("updatemkoth")]
        public async Task Updatemkoth()
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            var peasant = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Peasants"));
            var vassal = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains ("MKOTH Vassals"));
            var squire = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Squire"));
            var noble = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Nobles"));
            var king = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH King"));
            var user = Context.Message.Author as IGuildUser;
            foreach (var role in user.RoleIds)
            {
                if (role == 349945390193180674L)
                {
                    msg = await ReplyAsync("Updating MKOTH Member roles and names.");
                    try
                    {
                        var response = WebRequester.DownloadString("https://docs.google.com/spreadsheets/d/e/2PACX-1vSITdXPzQ_5eidATjL9j7uBicp4qvDuhx55IPvbMJ_jor8JU60UWCHwaHdXcR654W8Tp6VIjg-8V7g0/pub?gid=282944341&single=true&output=tsv");
                        Player.InitialiseList(response);
                        int count = 0;
                        foreach (var item in Context.Guild.Users)
                        {
                            count++;
                            var serveruser = item as IGuildUser;
                            bool ismod = false;
                            foreach (var memberroles in serveruser.RoleIds)
                            {
                                if (memberroles == role)
                                {
                                    ismod = true;
                                }
                            }
                            var player = Player.Fetch(serveruser.Id);

                            if (player.Name != PlayerStatus.UNKNOWN)
                            {
                                if (serveruser.Nickname != player.Name && serveruser.Username != player.Name && !ismod)
                                {
                                    await msg.ModifyAsync(x =>
                                    {
                                        x.Content = "Changing nicknames and roles"+ count + "/" + Context.Guild.Users.Count;
                                        embed.Description = embed.Description + "\n" + serveruser.Nickname + " => " + player.Name;
                                        x.Embed = embed.Build();
                                    });
                                    await serveruser.ModifyAsync(x =>
                                    {
                                        x.Nickname = player.Name;
                                    });
                                }
                                if (player.IsRemoved)
                                {
                                    continue;
                                }
                                switch (player.Playerclass)
                                {
                                    case PlayerClass.PEASANT:
                                        foreach (var classrole in serveruser.RoleIds)
                                        {
                                            if (classrole == vassal.Id || serveruser.RoleIds.Count == 2)
                                            {
                                                await serveruser.RemoveRoleAsync(vassal);
                                                await serveruser.AddRoleAsync(peasant);
                                                await msg.ModifyAsync(x =>
                                                {
                                                    x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                                    embed.Description = embed.Description + "\n" + serveruser.Username + " Update Role " + peasant.Name;
                                                    x.Embed = embed.Build();
                                                });
                                                break;
                                            }
                                        }
                                        break;

                                    case PlayerClass.VASSAL:
                                        foreach (var classrole in serveruser.RoleIds)
                                        {
                                            if (classrole == peasant.Id || classrole == squire.Id || serveruser.RoleIds.Count == 2)
                                            {
                                                await serveruser.RemoveRoleAsync(squire);
                                                await serveruser.RemoveRoleAsync(peasant);
                                                await serveruser.AddRoleAsync(vassal);
                                                await msg.ModifyAsync(x =>
                                                {
                                                    x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                                    embed.Description = embed.Description + "\n" + serveruser.Nickname + " Update Role " + vassal.Name;
                                                    x.Embed = embed.Build();
                                                });
                                                break;
                                            }
                                        }
                                        break;

                                    case PlayerClass.SQUIRE:
                                        foreach (var classrole in serveruser.RoleIds)
                                        {
                                            if (classrole == vassal.Id || classrole == noble.Id || serveruser.RoleIds.Count == 2)
                                            {
                                                await serveruser.RemoveRoleAsync(vassal);
                                                await serveruser.RemoveRoleAsync(noble);
                                                await serveruser.AddRoleAsync(squire);
                                                await msg.ModifyAsync(x =>
                                                {
                                                    x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                                    embed.Description = embed.Description + "\n" + serveruser.Nickname + " Update Role " + squire.Name;
                                                    x.Embed = embed.Build();
                                                });
                                                break;
                                            }
                                        }
                                        break;

                                    case PlayerClass.NOBLE:
                                        foreach (var classrole in serveruser.RoleIds)
                                        {
                                            if (classrole == squire.Id || classrole == king.Id || serveruser.RoleIds.Count == 2)
                                            {
                                                await serveruser.RemoveRoleAsync(squire);
                                                await serveruser.RemoveRoleAsync(king);
                                                await serveruser.AddRoleAsync(noble);
                                                await msg.ModifyAsync(x =>
                                                {
                                                    x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                                    embed.Description = embed.Description + "\n" + serveruser.Nickname + " Update Role " + noble.Name;
                                                    x.Embed = embed.Build();
                                                });
                                                break;
                                            }
                                        }
                                        break;

                                    case PlayerClass.KING:
                                        foreach (var classrole in serveruser.RoleIds)
                                        {
                                            if (serveruser.RoleIds.Count == 2)
                                            {
                                                await serveruser.AddRoleAsync(king);
                                                await msg.ModifyAsync(x =>
                                                {
                                                    x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                                    embed.Description = embed.Description + "\n" + serveruser.Nickname + " Update Role " + king.Name;
                                                    x.Embed = embed.Build();
                                                });
                                                break;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                        await msg.ModifyAsync(x =>
                        {
                            x.Content = "Changing nicknames and class roles " + count + "/" + Context.Guild.Users.Count;
                            embed.Description = embed.Description;
                            x.Embed = embed.Build();
                        });
                    }
                    catch (Exception e)
                    {

                        await msg.ModifyAsync(x =>
                        {
                            x.Content = "```" + e.StackTrace + "```";
                        });
                    }
                    
                }
            }
        }
    }
}
