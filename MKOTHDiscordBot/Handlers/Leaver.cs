using Discord;
using Discord.WebSocket;
using MKOTHDiscordBot.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MKOTHDiscordBot.Handlers
{
    public static class Leaver
    {
        public static Task Handle(SocketGuildUser user)
        {
            try
            {
                if (user.Guild.Id != Globals.MKOTHGuild.Guild.Id)
                {
                    return Task.CompletedTask;
                }

                var bans = Globals.MKOTHGuild.Guild.GetBansAsync().Result;

                if (Player.List.Exists(x => x.DiscordId == user.Id && !x.IsRemoved))
                {
                    if (bans.ToList().Exists(x => x.User.Id == user.Id))
                    {
                        SendLeaveMessage("a MKOTH Member has left and **banned** from the server.");
                    }
                    else
                    {
                        SendLeaveMessage("a MKOTH Member has left from the server.");
                        SendDmMessage("You left the MKOTH Server, note that you are still part of the community unless you are officially removed. " + 
                            "You are welcomed join back anytime using the link below:");
                    }
                }
                else
                {
                    if (bans.ToList().Exists(x => x.User.Id == user.Id))
                    {
                        SendLeaveMessage("a public user has left and **banned** from the server.");
                    }
                    else
                    {
                        SendLeaveMessage("a public user has left from the server.");
                        SendDmMessage("Thank you for your interests in MKOTH, if you are keen to join back in the future, use the invite link below:");
                    }
                }

                void SendLeaveMessage(string message)
                {
                    var embed = new EmbedBuilder()
                        .WithColor(Color.Orange)
                        .WithAuthor($"{user.GetDisplayName()}#{user.DiscriminatorValue}", user.GetAvatarUrl())
                        .WithDescription($"{user.Mention}, {message}");

                    _ = Responder.SendToChannel(Globals.MKOTHGuild.Leave, string.Empty, embed.Build());
                }

                void SendDmMessage(string message)
                {
                    var inviteLink = "https://discord.me/MKOTH";

                    _ = user.SendMessageAsync($"{message}\n\n{inviteLink}");
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            return Task.CompletedTask;
        }
    }
}
