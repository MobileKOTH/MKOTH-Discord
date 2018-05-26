using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MKOTHDiscordBot.Utilities;

namespace MKOTHDiscordBot.Modules
{
    using static Globals.MKOTHGuild;

    [Summary("Contains the utilities for MKOTH needs and management.")]
    [Remarks("Module C")]
    public class Management : ModuleBase<SocketCommandContext>
    {
        [Command("UpdateMKOTH", RunMode = RunMode.Async)]
        [Summary("To be deprecated. Manually refresh the MKOTH member roles and nicknames.")]
        [RequireMKOTHMod]
        public async Task Updatemkoth()
        {
            await UpdateMKOTHAsync(Context);
        }

        [Command("MyId")]
        [Alias("MyFormId", "MySubmissionId", "MySubmissionCode", "What is My Id", "What is My Id?", "What is My MKOTH Id", "What is My MKOTH Id?")]
        [Summary("Sends your unique personal MKOTH series submission form identification code, if you are a MKOTH Member.")]
        public async Task MyID()
        {
            int code = Player.FetchCode(Context.User.Id);

            if (Player.Fetch(Context.User.Id).IsUnknown)
            {
                await ReplyAsync(Context.User.Mention + ", You are not a MKOTH Member!");
                return;
            }
            if (!Context.IsPrivate)
            {
                await ReplyAsync(Context.User.Mention + ", your code will now be sent to your direct message.");
            }
            if (code != 0)
            {
                await Context.User.SendMessageAsync("Your Identification for submission form is below. Please keep the code secret.");
                await Context.User.SendMessageAsync(code.ToString());
                Logger.Log("Sent code to " + Context.User.Username.AddTab().AddMarkDownLine() + $"Discord Id: {Context.User.Id}".AddMarkDownLine() + code.ToString(), LogType.DIRECTMESSAGE);
            }
            else
            {
                await Context.User.SendMessageAsync("Your Identification is not found, please dm an admin for assistance");
                Logger.Log("Sent code to " + Context.User.Username.AddTab().AddMarkDownLine() + $"Discord Id: {Context.User.Id}" + "Code not found/not member", LogType.DIRECTMESSAGE);
            }
        }

        [Command("MissingMembers")]
        [Alias("ListMissingMembers", "mm")]
        [Summary("Lists the MKOTH Members who are missing from the discord server.")]
        public async Task MissingMembers()
        {
            try
            {
                EmbedBuilder embed = new EmbedBuilder();
                IUserMessage msg;
                var playerlist = Player.List.Where(x => !x.IsRemoved).ToList();
                foreach (var user in Guild.Users)
                {
                    var index = playerlist.FindIndex(x => x.DiscordId == user.Id);
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
                    activemisinglistfield += $"{item.Class}: {item.Name}\n";
                }
                foreach (var item in holidaymissinglist)
                {
                    holidaymisinglistfield += $"{item.Class}: {item.Name}\n";
                }
                embed.Title = "Missing MKOTH Members from MKOTH Discord Server";
                embed.Description = "MKOTH Members who are not in the discord server but still remain active or in holiday in the MKOTH Ranking.";
                embed.AddField(activemissinglist.Count + " Active Members", $"```{activemisinglistfield}```");
                embed.AddField(holidaymissinglist.Count + " Holiday Members", $"```{holidaymisinglistfield}```");
                embed.Color = Color.Orange;

                msg = await ReplyAsync(string.Empty, embed: embed.Build());
            }
            catch (Exception e)
            {
                await Logger.SendError(e);
            }
        }

        [Command("Alt")]
        [Summary("Checks the user's registration and server join date.")]
        public async Task Alt(IGuildUser user)
        {
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

            await ReplyAsync(string.Empty, embed: embed.Build());
        }

        [Command("SubmitKing")]
        [Alias("sk")]
        [Summary("Gets a prefilled series submission form with a valid series input, you only have to answer the Maths question.")]
        [Remarks(".submitking @User#1234 2 1 ABCDEFG")]
        public async Task SubmitKing(IUser opponent, int wins, int loss, string inviteCode = "NOT PROVIDED")
        {
            await Submit("King", opponent, wins, loss, inviteCode);
        }

        [Command("SubmitKnight")]
        [Alias("sn")]
        [Summary("Gets a prefilled series submission form with a valid series input, you only have to answer the Maths question. " +
            "However, the players for a knight vs knight series may not be properly ordered.")]
        [Remarks(".submitknight @User#1234 2 1 ABCDEFG")]
        public async Task SubmitKnight(IUser opponent, int wins, int loss, string inviteCode = "NOT PROVIDED")
        {
            await Submit("Knight", opponent, wins, loss, inviteCode);
        }

        [Command("SubmitRanked")]
        [Alias("sr", "submitrank")]
        [Summary("Gets a prefilled series submission form with a valid series input, you only have to answer the Maths question.")]
        [Remarks(".submitranked @User#1234 2 1 ABCDEFG")]
        public async Task SubmitRank(IUser opponent, int wins, int loss, string inviteCode = "NOT PROVIDED")
        {
            await Submit("Ranked", opponent, wins, loss, inviteCode);
        }

        [Command("SubmitPoint")]
        [Alias("sp")]
        [Summary("Gets a prefilled series submission form with a valid series input, you only have to answer the Maths question.")]
        [Remarks(".submitpoint @User#1234 2 1 ABCDEFG")]
        public async Task SubmitPoint(IUser opponent, int wins, int loss, string inviteCode = "NOT PROVIDED")
        {
            await Submit("Point", opponent, wins, loss, inviteCode);
        }

        [Command("Submit")]
        [Alias("s", "submitseries")]
        [Summary("Gets a prefilled series submission form with a valid series input, you only have to answer the Maths question.")]
        [Remarks(".submit king @User#1234 2 1 ABCDEFG\n" +
            ".submit knight @User#1234 2 1 ABCDEFG\n" +
            ".submit ranked @User#1234 2 1 ABCDEFG\n" +
            ".submit point @User#1234 2 1 ABCDEFG\n")]
        public async Task Submit(string seriesType, IUser opponent, int wins, int loss, string inviteCode = "NOT PROVIDED")
        {
            var seriesTypes = new string[4] { "King", "Knight", "Ranked", "Point" };
            if (seriesTypes.Count(x => x.ToLower().StartsWith(seriesType.ToLower())) == 0)
            {
                await ReplyAsync("Invalid series type.");
                return;
            }
            seriesType = seriesTypes.FirstOrDefault(x => x.ToLower().StartsWith(seriesType.ToLower()));

            var winner = Player.Fetch(Context.User.Id);
            var loser = Player.Fetch(opponent.Id);
            if (winner.IsUnknown || loser.IsUnknown || winner.IsRemoved || loser.IsRemoved)
            {
                await ReplyAsync("Unknown player(s).");
                return;
            }
            if (wins < loss || wins > 3 || loss > 3 || wins < 0 || loss < 0)
            {
                await ReplyAsync("Invalid win/loss.");
                return;
            }

            var player1 = seriesType == "Knight" ?
                (winner.IsKnight ? loser : winner) :
                (winner.RankOrClassRank > loser.RankOrClassRank ? winner : loser);
            var player2 = player1 == winner ? loser : winner;
            var player1wins = winner == player1 ? wins : loss;
            var player2wins = winner == player1 ? loss : wins;

            string baseURL = "https://docs.google.com/forms/d/e/1FAIpQLSdGJnCOl0l5HjxuYexVV_sOKPR1iScq3eiSxGiqKULX3zG4-Q/viewform?usp=pp_url&";
            var queryString = global::System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString["entry.1407262204"] = seriesType;
            queryString["entry.920665948"] = player1.Name;
            queryString["entry.1277512719"] = player2.Name;
            queryString["entry.1571047506"] = player1wins.ToString();
            queryString["entry.2093583907"] = player2wins.ToString();
            queryString["entry.2096904446"] = 0.ToString();
            queryString["entry.1027601864"] = winner.CodeId.ToString();
            queryString["entry.164636590"] = inviteCode;

            string filledForm = baseURL + queryString.ToString();
            Logger.Log($"Form sent to: {Context.User}\n ```{filledForm}```", LogType.DIRECTMESSAGE);
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithUrl(filledForm)
                .WithTitle("Prefilled Submission Form")
                .WithDescription("Here is your **partially** completed series submission form. **You still have to submit it** through the google form by clicking the link below.\n" +
                "This feature still undergoing testing, do check the prefilled values and report errors to an admin.\n\n" +
                $"Click [here]({filledForm}) for the submission form.");
            var msg = await Context.User.SendMessageAsync("**DO NOT SHARE THIS LINK AS IT CONTAINS YOUR SUBMISSION ID.**", embed: embed.Build());

            embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription("Please note that you still have to complete the submission from there.\n\n" +
                $"Click [here]({"https://discordapp.com/channels/@me/" + msg.Channel.Id}) to go to our direct message.");

            await ReplyAsync(Context.User.Mention +", your prefilled form has been sent to your direct message.", embed: embed.Build());
        }

        public static async Task UpdateMKOTHAsync(SocketCommandContext context)
        {
            var starttime = DateTime.Now;

            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg = null;

            try
            {
                embed.Title = "Role Pools";
                embed.Description = $"{ChatMods.Name}\n{Member.Name}\n{Peasant.Name}\n{Vassal.Name}\n{Squire.Name}\n{Noble.Name}\n{King.Name}\n{Knight.Name}\n";
                if (context != null)
                {
                    msg = await context.Channel.SendMessageAsync("Updating Member Roles and Names", embed: embed.Build());
                    await Player.Load();
                    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(Player.List, Newtonsoft.Json.Formatting.Indented));
                }

                int count = 0;
                foreach (var serveruser in Guild.Users)
                {
                    count++;
                    var player = Player.Fetch(serveruser.Id);
                    if (!player.IsUnknown && !player.IsRemoved && !serveruser.Roles.Contains(Member))
                    {
                        await serveruser.AddRoleAsync(Member);
                        await serveruser.RemoveRoleAsync(Pending);
                        if (context != null)
                        {
                            await msg.ModifyAsync(x =>
                            {
                                x.Content = $"Updating nicknames and roles {count}/{Guild.Users.Count}";
                                embed.Description = embed.Description.AddLine() + serveruser.Username + " Add MKOTH Role ";
                                x.Embed = embed.Build();
                            });
                        }
                    }
                    if (!player.IsUnknown && player.IsRemoved && serveruser.Roles.Contains(Member))
                    {
                        await serveruser.RemoveRoleAsync(Member);
                        await serveruser.AddRoleAsync(Pending);
                        if (context != null)
                        {
                            await msg.ModifyAsync(x =>
                            {
                                x.Content = $"Updating nicknames and roles {count}/{Guild.Users.Count}";
                                embed.Description = embed.Description.AddLine() + serveruser.Username + " Remove MKOTH Role ";
                                x.Embed = embed.Build();
                            });
                        }
                    }
                    else if (!player.IsUnknown && !player.IsRemoved && !serveruser.Roles.Contains(Stupid))
                    {
                        if (Globals.Config.Moderators.Contains(serveruser.Id) && !serveruser.Roles.Contains(ChatMods))
                        {
                            await serveruser.AddRoleAsync(ChatMods);
                        }
                        if (Globals.Config.Moderators.Contains(serveruser.Id) && !serveruser.GetDisplayName().Contains("ᴹᵒᵈ"))
                        {
                            await serveruser.ModifyAsync(x => { x.Nickname = player.Name + " ᴹᵒᵈ"; });
                        }

                        if (player.IsKnight && !serveruser.Roles.Contains(Knight))
                        {
                            await serveruser.AddRoleAsync(Knight);
                        }
                        else if (!player.IsKnight && serveruser.Roles.Contains(Knight))
                        {
                            await serveruser.RemoveRoleAsync(Knight);
                        }

                        if (serveruser.GetDisplayName() != player.Name && !serveruser.Roles.Contains(ChatMods))
                        {
                            await serveruser.ModifyAsync(x => { x.Nickname = player.Name; });
                            if (context != null)
                            {
                                await msg.ModifyAsync(x =>
                                {
                                    x.Content = $"Updating nicknames and roles {count}/{Guild.Users.Count}";
                                    embed.Description = embed.Description.AddLine() + serveruser.Username + " => " + player.Name;
                                    x.Embed = embed.Build();
                                });
                            }
                        }
                        switch (player.Class)
                        {
                            case PlayerClass.KING:
                                if (!serveruser.Roles.Contains(King))
                                {
                                    await serveruser.AddRoleAsync(King);
                                    updateRoleProgressStatus();
                                }
                                break;

                            case PlayerClass.NOBLE:
                                if (serveruser.Roles.Contains(King) || serveruser.Roles.Contains(Squire) || !serveruser.Roles.Contains(Noble))
                                {
                                    await serveruser.AddRoleAsync(Noble);
                                    await serveruser.RemoveRoleAsync(King);
                                    await serveruser.RemoveRoleAsync(Squire);
                                    updateRoleProgressStatus();
                                }
                                break;

                            case PlayerClass.SQUIRE:
                                if (serveruser.Roles.Contains(Noble) || serveruser.Roles.Contains(Vassal) || !serveruser.Roles.Contains(Squire))
                                {
                                    await serveruser.AddRoleAsync(Squire);
                                    await serveruser.RemoveRoleAsync(Noble);
                                    await serveruser.RemoveRoleAsync(Vassal);
                                    await serveruser.RemoveRoleAsync(Peasant);
                                    updateRoleProgressStatus();
                                }
                                break;

                            case PlayerClass.VASSAL:
                                if (serveruser.Roles.Contains(Squire) || serveruser.Roles.Contains(Peasant) || !serveruser.Roles.Contains(Vassal))
                                {
                                    await serveruser.AddRoleAsync(Vassal);
                                    await serveruser.RemoveRoleAsync(Squire);
                                    await serveruser.RemoveRoleAsync(Peasant);
                                    updateRoleProgressStatus();
                                }
                                break;

                            case PlayerClass.PEASANT:
                                if (serveruser.Roles.Contains(Vassal) || !serveruser.Roles.Contains(Peasant))
                                {
                                    await serveruser.AddRoleAsync(Peasant);
                                    await serveruser.RemoveRoleAsync(Vassal);
                                    await serveruser.RemoveRoleAsync(Squire);
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
                                    x.Content = $"Updating nicknames and roles {count}/{Guild.Users.Count}";
                                    embed.Description = embed.Description.AddLine() + serveruser.Username + " Update Role";
                                    x.Embed = embed.Build();
                                });
                            }
                        }
                    }
                }
                if (context != null)
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
                await Logger.SendError(e);
            }
        }
    }
}
