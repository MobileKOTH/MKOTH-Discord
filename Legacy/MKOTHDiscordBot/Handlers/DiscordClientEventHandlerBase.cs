using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace MKOTHDiscordBot.Handlers
{
    public abstract class DiscordClientEventHandlerBase
    {
        protected DiscordSocketClient client;

        public DiscordClientEventHandlerBase(DiscordSocketClient client)
        {
            this.client = client;
        }

        public static void ConfigureCommonHandlers(IServiceProvider services)
        {
            var handlers = ApplicationContext.CommonClasses
                .Where(x => x.BaseType == typeof(DiscordClientEventHandlerBase))
                .ToImmutableArray();

            foreach (var item in handlers)
            {
                ActivatorUtilities.CreateInstance(services, item);
            }
        }
    }
}
