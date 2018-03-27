using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MKOTHDiscordBot.Utilities;

namespace MKOTHDiscordBot
{
    public class Management : ModuleBase<SocketCommandContext>
    {
        [Command("updatemkoth", RunMode = RunMode.Async)]
        public async Task Updatemkoth()
        {
            var chatmods = Globals.MKOTHGuild.ChatMods;
            var user = (SocketGuildUser)Context.User;
            if (user.Roles.Contains(chatmods))
            {
                await UpdateMKOTH(Context);
            }
        }

        public static async Task UpdateMKOTH(SocketCommandContext context)
        {
            var starttime = DateTime.Now;

            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg = null;
            var MKOTHGuild = Globals.MKOTHGuild.Guild;
            var chatmods = Globals.MKOTHGuild.ChatMods;
            var stupid = Globals.MKOTHGuild.Stupid;
            var member = Globals.MKOTHGuild.Member;
            var peasant = Globals.MKOTHGuild.Peasant;
            var vassal = Globals.MKOTHGuild.Vassal;
            var squire = Globals.MKOTHGuild.Squire;
            var noble = Globals.MKOTHGuild.Noble;
            var king = Globals.MKOTHGuild.King;

            try
            {
                embed.Title = "Role Pools";
                embed.Description = $"{chatmods.Name}\n{member.Name}\n{peasant.Name}\n{vassal.Name}\n{squire.Name}\n{noble.Name}\n{king.Name}\n";
                if (context != null)
                {
                    msg = await context.Channel.SendMessageAsync("Updating Member Roles and Names", embed: embed.Build());
                    await PlayerCode.Load();
                    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(Player.List, Newtonsoft.Json.Formatting.Indented));
                }

                int count = 0;
                foreach (var serveruser in MKOTHGuild.Users)
                {
                    count++;
                    var player = Player.Fetch(serveruser.Id);
                    if (player.Name != PlayerStatus.UNKNOWN && !player.IsRemoved && !serveruser.Roles.Contains(member))
                    {
                        await serveruser.AddRoleAsync(member);
                        if (context != null)
                        {
                            await msg.ModifyAsync(x =>
                            {
                                x.Content = $"Updating nicknames and roles {count}/{MKOTHGuild.Users.Count}";
                                embed.Description = embed.Description.AddLine() + serveruser.Username + " Add MKOTH Role ";
                                x.Embed = embed.Build();
                            });
                        }
                    }
                    if (player.Name != PlayerStatus.UNKNOWN && player.IsRemoved && serveruser.Roles.Contains(member))
                    {
                        await serveruser.RemoveRoleAsync(member);
                        if (context != null)
                        {
                            await msg.ModifyAsync(x =>
                            {
                                x.Content = $"Updating nicknames and roles {count}/{MKOTHGuild.Users.Count}";
                                embed.Description = embed.Description.AddLine() + serveruser.Username + " Remove MKOTH Role ";
                                x.Embed = embed.Build();
                            });
                        }
                    }
                    else if(player.Name != PlayerStatus.UNKNOWN && !player.IsRemoved && !serveruser.Roles.Contains(stupid))
                    {
                        if (serveruser.GetDisplayName() != player.Name && !serveruser.Roles.Contains(chatmods))
                        {
                            if (context != null)
                            {
                                await msg.ModifyAsync(x =>
                                {
                                    x.Content = $"Updating nicknames and roles {count}/{MKOTHGuild.Users.Count}";
                                    embed.Description = embed.Description.AddLine() + serveruser.Username + " => " + player.Name;
                                    x.Embed = embed.Build();
                                });
                            }
                            await serveruser.ModifyAsync(x => { x.Nickname = player.Name; });
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
                            if (context != null)
                            {
                                await msg.ModifyAsync(x =>
                                {
                                    x.Content = $"Updating nicknames and roles {count}/{MKOTHGuild.Users.Count}";
                                    embed.Description = embed.Description.AddLine() + serveruser.Username + " Update Role";
                                    x.Embed = embed.Build();
                                });
                            }
                        }
                    }
                }
                if (context != null )
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Content = "Updating nicknames and roles COMPLETED!";
                        embed.Description = embed.Description;
                        x.Embed = embed.Build();
                    });
                }
                Logger.Debug((DateTime.Now - starttime).TotalMilliseconds + " ms", "Update MKOTH Run");
            }
            catch (Exception e)
            {
                string stacktrace = e.StackTrace;
                if (stacktrace.Length >= 1800)
                {
                    stacktrace = stacktrace.Substring(0, 1800) + "...";
                }
                await Responder.SendToChannel((SocketTextChannel)Globals.TestGuild.BotTest, e.Message + "```" + stacktrace + "```");
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
                Logger.Log("Sent code to " + user.Username.AddTab() + user.Nickname.AddTab().AddLine() + code.ToString(), LogType.DIRECTMESSAGE);
            }
            else
            {
                await Context.User.SendMessageAsync("Your Identification is not found, please dm an admin for assistance");
                Logger.Log("Sent code to " + user.Username.AddTab() + user.Nickname.AddTab().AddLine() + "Code not found", LogType.DIRECTMESSAGE);
            }
        }

        [Command("missingMembers")]
        [Alias("listMissingMembers")]
        [Summary("List the MKOTH Members who are missing from the discord server")]
        public async Task MissingMembers()
        {
            try
            {
                EmbedBuilder embed = new EmbedBuilder();
                IUserMessage msg;
                var playerlist = Player.List.Where(x => !x.IsRemoved).ToList();
                foreach (var user in Globals.MKOTHGuild.Guild.Users)
                {
                    var index = playerlist.FindIndex(x => x.Discordid == user.Id);
                    if (index > -1)
                    {
                        playerlist.RemoveAt(index);
                    }
                }

                var activemissinglist = playerlist.Where(x => !x.IsHoliday).ToList();
                var holidaymissinglist = playerlist.Where(x => x.IsHoliday).ToList();

                string activemisinglistfield = "";
                string holidaymisinglistfield = "";
                foreach (var item in activemissinglist)
                {
                    activemisinglistfield += $"{item.Playerclass}: {item.Name}\n";
                }
                foreach (var item in holidaymissinglist)
                {
                    holidaymisinglistfield += $"{item.Playerclass}: {item.Name}\n";
                }
                embed.Title = "Missing MKOTH Members from MKOTH discord server";
                embed.Description = "MKOTH Members who are not in the discord server but still remain active or in holiday in the MKOTH Ranking.";
                embed.AddField(activemissinglist.Count + " Active Members", $"```{activemisinglistfield}```");
                embed.AddField(holidaymissinglist.Count + " Holiday Members", $"```{holidaymisinglistfield}```");
                embed.Color = Color.Orange;

                msg = await ReplyAsync(string.Empty, false, embed: embed);
            }
            catch (Exception e)
            {
                string stacktrace = e.StackTrace;
                if (stacktrace.Length >= 1800)
                {
                    stacktrace = stacktrace.Substring(0, 1800) + "...";
                }
                await Responder.SendToChannel((SocketTextChannel)Globals.TestGuild.BotTest, e.Message + "```" + stacktrace + "```");
            }
        }
    }
}
