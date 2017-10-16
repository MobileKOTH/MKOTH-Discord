using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MKOTH_Discord_Bot
{
    public class Management : ModuleBase<SocketCommandContext>
    {
        System.Net.WebClient WebRequester = new System.Net.WebClient();

        [Command("updatemkoth")]
        public async Task Updatemkoth()
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;
            var chatmods = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("Chat Mods"));
            var member = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Members"));
            var peasant = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Peasants"));
            var vassal = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Vassals"));
            var squire = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Squire"));
            var noble = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Nobles"));
            var king = Context.Guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH King"));
            var user = Context.Message.Author as IGuildUser;
            if (user.RoleIds.Contains(349945390193180674UL))
            {
                try
                {
                    embed.Title = "Role Pools";
                    embed.Description = Newtonsoft.Json.JsonConvert.SerializeObject(new string[]
                    {
                        chatmods.Name, member.Name,peasant.Name,vassal.Name,squire.Name,noble.Name,king.Name
                    }, Newtonsoft.Json.Formatting.Indented);
                    msg = await ReplyAsync("Updating Member Roles and Names", embed: embed.Build());
                    var response = WebRequester.DownloadString("https://docs.google.com/spreadsheets/d/e/2PACX-1vSITdXPzQ_5eidATjL9j7uBicp4qvDuhx55IPvbMJ_jor8JU60UWCHwaHdXcR654W8Tp6VIjg-8V7g0/pub?gid=282944341&single=true&output=tsv");
                    Player.InitialiseList(response);
                    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(Player.List, Newtonsoft.Json.Formatting.Indented));
                    int count = 0;
                    foreach (var serveruser in Context.Guild.Users)
                    {
                        count++;
                        var player = Player.Fetch(serveruser.Id);
                        if (player.Name != PlayerStatus.UNKNOWN && player.IsRemoved)
                        {
                            await serveruser.RemoveRoleAsync(member);
                            await msg.ModifyAsync(x =>
                            {
                                x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                embed.Description = embed.Description + "\n" + serveruser.Username + " Remove MKOTH Role ";
                                x.Embed = embed.Build();
                            });
                        }
                        else if
                            (
                            player.Name != PlayerStatus.UNKNOWN && !
                            player.IsRemoved && !
                            player.Name.Equals("Naia Mizugaki") && !
                            player.Name.Equals("Yt Shield"
                            ))
                        {
                            if (serveruser.Nickname != player.Name && serveruser.Username != player.Name && !serveruser.Roles.Contains(chatmods))
                            {
                                await msg.ModifyAsync(x =>
                                {
                                    x.Content = "Changing nicknames and roles" + count + "/" + Context.Guild.Users.Count;
                                    embed.Description = embed.Description + "\n" + serveruser.Username + " => " + player.Name;
                                    x.Embed = embed.Build();
                                });
                                await serveruser.ModifyAsync(x =>
                                {
                                    x.Nickname = player.Name;
                                });
                            }
                            switch (player.Playerclass)
                            {
                                case PlayerClass.KING:
                                    if (!serveruser.Roles.Contains(king))
                                    {
                                        await serveruser.AddRoleAsync(king);
                                        await msg.ModifyAsync(x =>
                                        {
                                            x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                            embed.Description = embed.Description + "\n" + serveruser.Username + " Update Role ";
                                            x.Embed = embed.Build();
                                        });
                                    }
                                    break;

                                case PlayerClass.NOBLE:
                                    if (serveruser.Roles.Contains(king) || serveruser.Roles.Contains(squire) || !serveruser.Roles.Contains(noble))
                                    {
                                        await serveruser.AddRoleAsync(noble);
                                        await serveruser.RemoveRoleAsync(king);
                                        await serveruser.RemoveRoleAsync(squire);
                                        await msg.ModifyAsync(x =>
                                        {
                                            x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                            embed.Description = embed.Description + "\n" + serveruser.Username + " Update Role ";
                                            x.Embed = embed.Build();
                                        });
                                    }
                                    break;

                                case PlayerClass.SQUIRE:
                                    if (serveruser.Roles.Contains(noble) || serveruser.Roles.Contains(vassal) || !serveruser.Roles.Contains(squire))
                                    {
                                        await serveruser.AddRoleAsync(squire);
                                        await serveruser.RemoveRoleAsync(noble);
                                        await serveruser.RemoveRoleAsync(vassal);
                                        await msg.ModifyAsync(x =>
                                        {
                                            x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                            embed.Description = embed.Description + "\n" + serveruser.Username + " Update Role ";
                                            x.Embed = embed.Build();
                                        });
                                    }
                                    break;

                                case PlayerClass.VASSAL:
                                    if (serveruser.Roles.Contains(squire) || serveruser.Roles.Contains(peasant) || !serveruser.Roles.Contains(vassal))
                                    {
                                        await serveruser.AddRoleAsync(vassal);
                                        await serveruser.RemoveRoleAsync(squire);
                                        await serveruser.RemoveRoleAsync(peasant);
                                        await msg.ModifyAsync(x =>
                                        {
                                            x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                            embed.Description = embed.Description + "\n" + serveruser.Username + " Update Role ";
                                            x.Embed = embed.Build();
                                        });
                                    }
                                    break;

                                case PlayerClass.PEASANT:
                                    if (serveruser.Roles.Contains(vassal) || !serveruser.Roles.Contains(peasant))
                                    {
                                        await serveruser.AddRoleAsync(peasant);
                                        await serveruser.RemoveRoleAsync(vassal);
                                        await msg.ModifyAsync(x =>
                                        {
                                            x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                                            embed.Description = embed.Description + "\n" + serveruser.Username + " Update Role ";
                                            x.Embed = embed.Build();
                                        });
                                    }
                                    break;
                            }
                        }
                    }
                    await msg.ModifyAsync(x =>
                    {
                        x.Content = "Changing nicknames and roles" + count + " /" + Context.Guild.Users.Count;
                        embed.Description = embed.Description;
                        x.Embed = embed.Build();
                    });
                }
                catch (Exception e)
                {
                    await ReplyAsync("```" + e.StackTrace + "```");
                }
            }
        }
    }
}
