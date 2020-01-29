using System;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.Options;

using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Handlers
{
    public class LeaverHandler : DiscordClientEventHandlerBase
    {
        private readonly ResponseService responder;
        private readonly Settings settings;
        private readonly Lazy<IGuild> lazyguild;
        private readonly Lazy<ITextChannel> lazyChannel;
        private readonly Lazy<IRole> lazyRole;
        private IGuild guild => lazyguild.Value;
        private ITextChannel channel => lazyChannel.Value;
        private IRole role => lazyRole.Value;

        public LeaverHandler(DiscordSocketClient client, ResponseService responseService, IOptions<AppSettings> appSettings) : base (client)
        {
            responder = responseService;
            settings = appSettings.Value.Settings;
            client.UserLeft += Handle;

            lazyguild = new Lazy<IGuild>(() => client.GetGuild(this.settings.ProductionGuild.Id));
            lazyChannel = new Lazy<ITextChannel>(() => client.GetChannel(this.settings.ProductionGuild.Leave) as ITextChannel);
            lazyRole = new Lazy<IRole>(() => guild.GetRole(this.settings.ProductionGuild.MemberRole));
        }

        Task Handle(IGuildUser user)
        {
            try
            {
                if (user.GuildId != guild.Id)
                {
                    return Task.CompletedTask;
                }

                var bans = user.Guild.GetBansAsync().Result;

                if (user.RoleIds.Any(x => x == role.Id))
                {
                    if (bans.ToList().Exists(x => x.User.Id == user.Id))
                    {
                        SendLeaveMessage("a MKOTH Member has left and **banned** from the server.");
                    }
                    else
                    {
                        SendLeaveMessage("a MKOTH Member has left from the server.");
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
                    }
                }

                void SendLeaveMessage(string message)
                {
                    var embed = new EmbedBuilder()
                        .WithColor(Color.Orange)
                        .WithAuthor($"{user.GetDisplayName()}#{user.DiscriminatorValue}", user.GetAvatarUrl())
                        .WithDescription($"{user.Mention}, {message}");

                    _ = responder.SendToChannelAsync(channel, string.Empty, embed.Build());
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
