using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot.Components.InteractiveReaction
{
    public abstract class InteractiveReactionBase : IInteractiveReaction
    {
        public static HashSet<IInteractiveReaction> Actives { get; private set; } = new HashSet<IInteractiveReaction>();
        public static HashSet<ulong> ActiveChannels { get; private set; } = new HashSet<ulong>();

        public abstract Emote[] Emotes { get; }

        protected abstract void ProcessReaction(SocketReaction reaction);

        public IUserMessage SourceMessage { get; private set; }
        public ulong MessageId => SourceMessage.Id;
        public Dictionary<Emote, IUser[]> RawResult
            => SourceMessage.Reactions
            .Where(x => Emotes.Any(y => y.Id == ((Emote)x.Key).Id))
            .ToDictionary(x => (Emote)x.Key, x => SourceMessage.GetReactionUsersAsync(x.Key).Result.ToArray());

        protected InteractiveReactionBase(SocketCommandContext context)
        {
            Actives.Add(this);
            ActiveChannels.Add(context.Channel.Id);
        }

        public virtual void Deactivate()
        {
            Actives.Remove(this);
            ActiveChannels.RemoveWhere(x => Actives.All(y => y.SourceMessage.Channel.Id != x));
        }

        public void HandleReaction(IUserMessage message, SocketReaction reaction)
        {
            if (reaction.MessageId == MessageId && Emotes.Any(x => x.Id == ((Emote)reaction.Emote).Id))
            {
                SourceMessage = message;
                ProcessReaction(reaction);
            }
        }

        public override bool Equals(object obj)
        {
            var @base = obj as InteractiveReactionBase;
            return @base != null &&
                   MessageId == @base.MessageId;
        }

        public override int GetHashCode()
        {
            return 212258449 + MessageId.GetHashCode();
        }

        public static bool operator ==(InteractiveReactionBase base1, InteractiveReactionBase base2)
        {
            return EqualityComparer<InteractiveReactionBase>.Default.Equals(base1, base2);
        }

        public static bool operator !=(InteractiveReactionBase base1, InteractiveReactionBase base2)
        {
            return !(base1 == base2);
        }
    }
}
