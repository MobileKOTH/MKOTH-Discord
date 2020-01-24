using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MKOTHDiscordBot.Handlers
{
    public class LeaverHandler : DiscordClientEventHandlerBase
    {
        private ResponseService responseService;
        private Dictionary<IGuild, ITextChannel> announceGuilds;
        private readonly Settings settings;

        public LeaverHandler(DiscordSocketClient client, ResponseService responseService, IOptions<AppSettings> settings) : base (client)
        {
            this.responseService = responseService;
            this.settings = settings.Value.Settings;
            this.client.UserLeft += Handle;
            this.client.Ready += Init;
        }

        private Task Init()
        {
            announceGuilds = settings.LeaveAnnounceGuilds.ToDictionary(x => (client.GetChannel(x) as IGuildChannel).Guild, x => client.GetChannel(x) as ITextChannel);
            return Task.CompletedTask;
        }

        Task Handle(IGuildUser user)
        {
            try
            {
                if (!announceGuilds.ContainsKey(user.Guild))
                {
                    return Task.CompletedTask;
                }

                var bans = user.Guild.GetBansAsync().Result;

                if (false)//Player.List.Exists(x => x.DiscordId == user.Id && !x.IsRemoved))
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

                    _ = responseService.SendToChannelAsync(announceGuilds.Single(x => x.Key == user.Guild).Value, string.Empty, embed.Build());
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
