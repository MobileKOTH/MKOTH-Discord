using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MKOTHDiscordBot.Handlers
{
    public static class DiscordClientEventHandle
    {
        public static void StartCommonHandlers(IServiceProvider services)
        {
            ActivatorUtilities.CreateInstance<MessageHandler>(services);
            ActivatorUtilities.CreateInstance<LeaverHandler>(services);
            ActivatorUtilities.CreateInstance<ReactionHandler>(services);
        }
    }
}
