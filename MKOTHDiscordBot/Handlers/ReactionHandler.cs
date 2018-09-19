using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Cerlancism.Discord.InteractiveReaction.Core;

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
            if (cache.HasValue && InteractiveReactionBase.ActiveChannels.Contains(channel.Id))
            {
                InteractiveReactionBase.Actives
                    .SingleOrDefault(x => x.SourceMessage.Id == cache.Value.Id)
                    ?.HandleReaction(cache.Value, reaction);
            }
            return Task.CompletedTask;
        }
    }
}
