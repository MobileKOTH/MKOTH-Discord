using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    class Vote
    {
        public static List<Vote> Votes = new List<Vote>();

        public IUserMessage Message { get; private set; }
        public HashSet<ulong> Supporters { get; set; } = new HashSet<ulong>();
        public HashSet<ulong> Opposers { get; set; } = new HashSet<ulong>();
        public Timer TimeLeft = new Timer
        {
            AutoReset = true,
            Interval = 30000,
        };

        public Vote(SocketCommandContext context, string topic, string content)
        {
            Supporters.Add(context.User.Id);
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithAuthor(context.User)
                .WithTitle(topic)
                .WithDescription(content)
                .WithFooter($"Approval: {ApprovalRate}%");

            Message = context.Channel.SendMessageAsync(string.Empty, embed: embed.Build()).Result;
            var emoteTask1 = Message.AddReactionAsync(Globals.MKOTHGuild.UpArrowEmote);
            var emoteTask2 = Message.AddReactionAsync(Globals.MKOTHGuild.DownArrowEmote);
            Task.WaitAll(emoteTask1, emoteTask2);
            TimeLeft.Start();
            TimeLeft.Elapsed += (sender, evt) =>
            {
                TimeLeft.Dispose();
                Message.ModifyAsync(x => x.Content = "This vote has closed.");
                Votes.Remove(this);
            };

            Votes.Add(this);
        }

        public int ApprovalRate
        {
            get
            {
                int rate = 100 / (Supporters.Count + Opposers.Count + 1) * Supporters.Count;
                if (rate.IsInRangeOffset(33, 5) || rate.IsInRangeOffset(66, 5))
                {
                    TimeLeft.Interval = 10000;
                }
                return rate;
            }
        }

        public static async Task HandleReaction(SocketReaction reaction)
        {
            var member = Player.Fetch(reaction.UserId);
            if (Votes.Count(x => x.Message.Id == reaction.MessageId) > 0 && !member.IsUnknown)
            {
                var vote = Votes.Find(x => x.Message.Id == reaction.MessageId);
                var embed = vote.Message.Embeds.First().ToEmbedBuilder();
                var emote = (Emote)reaction.Emote;
                if (emote.Id == Globals.MKOTHGuild.UpArrowEmote.Id)
                {
                    if (vote.Supporters.Add(reaction.UserId))
                    {
                        embed.Footer.Text = $"Approval: {vote.ApprovalRate}%";
                        await vote.UpdateMessage(embed.Build());
                    }
                }
                if (emote.Id == Globals.MKOTHGuild.DownArrowEmote.Id)
                {
                    if (vote.Opposers.Add(reaction.UserId))
                    {
                        embed.Footer.Text = $"Approval: {vote.ApprovalRate}%";
                        await vote.UpdateMessage(embed.Build());
                    }
                }
            }
        }

        public async Task UpdateMessage(Embed embed)
        {
            await Message.ModifyAsync(x =>
            {
                x.Embed = embed;
            });
        }
    }
}
