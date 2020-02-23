using System;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Handlers
{
    public class JoinHandler : DiscordClientEventHandlerBase
    {
        private readonly RoleManager roleManager;
        private readonly Settings settings;
        private readonly Lazy<IGuild> lazyguild;
        private IGuild Guild => lazyguild.Value;

        public JoinHandler(IServiceProvider serviceProvider, IOptions<AppSettings> appSettings) : base(serviceProvider)
        {
            roleManager = services.GetService<RoleManager>();
            settings = appSettings.Value.Settings;
            client.UserJoined += Handle;

            lazyguild = new Lazy<IGuild>(() => client.GetGuild(settings.ProductionGuild.Id));
        }

        async Task Handle(IGuildUser user)
        {
            if (user.GuildId == Guild.Id)
            {
                _ = roleManager.HandleUserJoin(user);
            }

            await Task.CompletedTask;
        }
    }
}
