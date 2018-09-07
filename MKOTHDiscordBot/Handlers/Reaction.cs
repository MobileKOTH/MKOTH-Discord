using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MKOTHDiscordBot.Components.InteractiveReaction;

namespace MKOTHDiscordBot.Handlers
{
    public static class Reaction
    {
        public static Task Handle(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
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
