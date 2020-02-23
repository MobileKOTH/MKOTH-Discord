using System;
using System.Threading.Tasks;

using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MKOTHDiscordBot.Properties;

namespace MKOTHDiscordBot.Services
{
    public class DiscordLogger
    {
        private readonly DiscordSocketClient client;
        private readonly Developmentguild devGuild;

        public SocketGuild DevGuild => client.GetGuild(devGuild.Id);
        public SocketTextChannel LogChannel => DevGuild.GetTextChannel(devGuild.Log);
        public SocketTextChannel TestGuild => DevGuild.GetTextChannel(devGuild.Test);

        public DiscordLogger(IServiceProvider services, IOptions<AppSettings> appSettings)
        {
            client = services.GetService<DiscordSocketClient>();
            devGuild = appSettings.Value.Settings.DevelopmentGuild;
        }

        public async Task LogAsync(string message)
        {
            await LogChannel.SendMessageAsync(message);
        }

        public void Log(string message)
        {
            _ = LogChannel.SendMessageAsync(message);
        }
    }
}
