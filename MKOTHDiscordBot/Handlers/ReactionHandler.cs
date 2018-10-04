using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MKOTHDiscordBot.Handlers
{
    public class ReactionHandler : DiscordClientEventHandlerBase
    {
        public ReactionHandler(DiscordSocketClient client) : base(client)
        {
            this.client.ReactionAdded += Handle;
            this.client.ReactionRemoved += Handle;
        }

        Task Handle(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            // TODO

            return Task.CompletedTask;
        }
    }
}
