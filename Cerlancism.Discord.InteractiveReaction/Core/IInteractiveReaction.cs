using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Cerlancism.Discord.InteractiveReaction.Core
{
    public interface IInteractiveReaction
    {
        IUserMessage SourceMessage { get; }
        Emote[] Emotes { get; }
        Dictionary<Emote, IUser[]> RawResult { get; }

        void HandleReaction(IUserMessage message, SocketReaction reaction);
        void Deactivate();
    }
}
