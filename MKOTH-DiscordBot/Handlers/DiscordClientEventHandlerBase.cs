using System;
using System.Linq;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace MKOTHDiscordBot.Handlers
{
    using static ActivatorUtilities;
    using static ApplicationContext;

    public abstract class DiscordClientEventHandlerBase
    {
        protected readonly DiscordSocketClient client;

        public DiscordClientEventHandlerBase(DiscordSocketClient socketClient)
        {
            client = socketClient;
        }

        public static void ConfigureCommonHandlers(IServiceProvider services)
            => SurfaceClasses
            .Where(x => x.BaseType == typeof(DiscordClientEventHandlerBase))
            .ToList()
            .ForEach(x => CreateInstance(services, x));
    }
}
