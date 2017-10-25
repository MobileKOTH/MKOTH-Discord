using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using MKOTH_Discord_Bot.Utilities;

namespace MKOTH_Discord_Bot
{
    public class Management : ModuleBase<SocketCommandContext>
    {
        System.Net.WebClient WebRequester = new System.Net.WebClient();

        [Command("updatemkoth", RunMode = RunMode.Async)]
        public async Task Updatemkoth()
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;
            var chatmods = ContextPools.MKOTHGuild.ChatMods;
            var member = ContextPools.MKOTHGuild.Member;
            var peasant = ContextPools.MKOTHGuild.Peasant;
            var vassal = ContextPools.MKOTHGuild.Vassal;
            var squire = ContextPools.MKOTHGuild.Squire;
            var noble = ContextPools.MKOTHGuild.Noble;
            var king = ContextPools.MKOTHGuild.King;
            var user = (SocketGuildUser)Context.User;
            if (user.Roles.Contains(chatmods))
            {
                try
                {
                    embed.Title = "Role Pools";
                    embed.Description = $"{chatmods.Name}\n{member.Name}\n{peasant.Name}\n{vassal.Name}\n{squire.Name}\n{noble.Name}\n{king.Name}\n";
                    msg = await ReplyAsync("Updating Member Roles and Names", embed: embed.Build());

                    PlayerCode.Load(Context.Client);
                    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(Player.List, Newtonsoft.Json.Formatting.Indented));

                    int count = 0;
                    foreach (var serveruser in Context.Guild.Users)
                    {
                        count++;
                        var player = Player.Fetch(serveruser.Id);
                        if (player.Name != PlayerStatus.UNKNOWN && !player.IsRemoved && !serveruser.Roles.Contains(member))
                        {
                            await serveruser.AddRoleAsync(member);
                            await msg.ModifyAsync(x =>
                            {
                                x.Content = $"Updating nicknames and roles {count}/{Context.Guild.Users.Count}";
                                embed.Description = embed.Description.AddLine() + serveruser.Username + " Add MKOTH Role ";
                                x.Embed = embed.Build();
                            });
                        }
                        if (player.Name != PlayerStatus.UNKNOWN && player.IsRemoved && serveruser.Roles.Contains(member))
                        {
                            await serveruser.RemoveRoleAsync(member);
                            await msg.ModifyAsync(x =>
                            {
                                x.Content = $"Updating nicknames and roles {count}/{Context.Guild.Users.Count}";
                                embed.Description = embed.Description.AddLine() + serveruser.Username + " Remove MKOTH Role ";
                                x.Embed = embed.Build();
                            });
                        }
                        else 
                        if 
                            ( 
                            player.Name != PlayerStatus.UNKNOWN && !
                            player.IsRemoved && !
                            player.Name.Equals("Naia Mizugaki") && !
                            player.Name.Equals("Yt Shield")
                            )
                        {
                            if (serveruser.Nickname != player.Name && serveruser.Username != player.Name && !serveruser.Roles.Contains(chatmods))
                            {
                                await msg.ModifyAsync(x =>
                                {
                                    x.Content = $"Updating nicknames and roles {count}/{Context.Guild.Users.Count}";
                                    embed.Description = embed.Description.AddLine() + serveruser.Username + " => " + player.Name;
                                    x.Embed = embed.Build();
                                });
                                await serveruser.ModifyAsync(x => { x.Nickname = player.Name;});
                            }
                            switch (player.Playerclass)
                            {
                                case PlayerClass.KING:
                                    if (!serveruser.Roles.Contains(king))
                                    {
                                        await serveruser.AddRoleAsync(king);
                                        updateRoleProgressStatus();
                                    }
                                    break;

                                case PlayerClass.NOBLE:
                                    if (serveruser.Roles.Contains(king) || serveruser.Roles.Contains(squire) || !serveruser.Roles.Contains(noble))
                                    {
                                        await serveruser.AddRoleAsync(noble);
                                        await serveruser.RemoveRoleAsync(king);
                                        await serveruser.RemoveRoleAsync(squire);
                                        updateRoleProgressStatus();
                                    }
                                    break;

                                case PlayerClass.SQUIRE:
                                    if (serveruser.Roles.Contains(noble) || serveruser.Roles.Contains(vassal) || !serveruser.Roles.Contains(squire))
                                    {
                                        await serveruser.AddRoleAsync(squire);
                                        await serveruser.RemoveRoleAsync(noble);
                                        await serveruser.RemoveRoleAsync(vassal);
                                        updateRoleProgressStatus();
                                    }
                                    break;

                                case PlayerClass.VASSAL:
                                    if (serveruser.Roles.Contains(squire) || serveruser.Roles.Contains(peasant) || !serveruser.Roles.Contains(vassal))
                                    {
                                        await serveruser.AddRoleAsync(vassal);
                                        await serveruser.RemoveRoleAsync(squire);
                                        await serveruser.RemoveRoleAsync(peasant);
                                        updateRoleProgressStatus();
                                    }
                                    break;

                                case PlayerClass.PEASANT:
                                    if (serveruser.Roles.Contains(vassal) || !serveruser.Roles.Contains(peasant))
                                    {
                                        await serveruser.AddRoleAsync(peasant);
                                        await serveruser.RemoveRoleAsync(vassal);
                                        updateRoleProgressStatus();
                                    }
                                    break;
                            }

                            async void updateRoleProgressStatus()
                            {
                                await msg.ModifyAsync(x =>
                                {
                                    x.Content = $"Updating nicknames and roles {count}/{Context.Guild.Users.Count}";
                                    embed.Description = embed.Description.AddLine() + serveruser.Username + " Update Role";
                                    x.Embed = embed.Build();
                                });
                            }
                        }
                    }
                    await msg.ModifyAsync(x =>
                    {
                        x.Content = "Updating nicknames and roles COMPLETED!";
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

        [Command("myId")]
        [Alias("myformid", "mysubmissionid", "mysubmissioncode")]
        public async Task MyID()
        {
            int code = PlayerCode.FetchCode(Context.User.Id, Context.Client);

            if (Player.Fetch(Context.User.Id).Name == PlayerStatus.UNKNOWN)
            {
                await ReplyAsync(Context.User.Mention + ", You are not a MKOTH Member!");
                return;
            }
            if (Context.IsPrivate)
            {
                if (code != 0)
                {
                    await Context.User.SendMessageAsync("Your Identification for submission form is below. Please keep the code secret.");
                    await Context.User.SendMessageAsync(code.ToString());
                    Logger.Log("Sent code to " + Context.User.Username.AddTab().AddLine() + code.ToString(), LogType.DIRECTMESSAGE);
                }
                else
                {
                    await Context.User.SendMessageAsync("Your Identification is not found, please dm an admin for assistance");
                    Logger.Log("Sent code to " + Context.User.Username.AddTab().AddLine() + "Code not found/not member", LogType.DIRECTMESSAGE);
                }
                return;
            }

            var user = (SocketGuildUser)Context.User;
            await ReplyAsync(Context.User.Mention + ", your code will now be sent to your direct message.");
            if (code != 0)
            {
                await Context.User.SendMessageAsync("Your Identification for submission form is below. Please keep the code secret.");
                await Context.User.SendMessageAsync(code.ToString());
                Logger.Log("Sent code to " + user.Nickname.AddTab().AddLine() + code.ToString(), LogType.DIRECTMESSAGE);
            }
            else
            {
                await Context.User.SendMessageAsync("Your Identification is not found, please dm an admin for assistance");
                Logger.Log("Sent code to " + user.Nickname.AddTab().AddLine() + "Code not found", LogType.DIRECTMESSAGE);
            }
        }
    }
}
