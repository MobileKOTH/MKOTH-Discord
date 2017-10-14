using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MKOTH_Discord_Bot
{
    public class CommandParser : ModuleBase<SocketCommandContext>
    {
        private EmbedBuilder embed = new EmbedBuilder();
        private IUserMessage msg;

        [Command("info")]
        public async Task Info()
        {
            embed.WithTitle("Information");
            embed.WithDescription("");
            embed.WithUrl("https://mobilekoth.wordpress.com/");
            embed.WithThumbnailUrl("https://cdn.discordapp.com/attachments/341163606605299716/360336022745382912/13615239_1204861226212220_2613382245523520956_n.png");

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.WithName("Created by Cerlancism CY");
            author.WithIconUrl("https://cdn.discordapp.com/attachments/341163606605299716/352269545030942720/mkoth_thumb.jpg");
            embed.WithAuthor(author);

            embed.AddField("Library", "Discord.Net", true);
             
            embed.WithImageUrl("https://cdn.discordapp.com/attachments/271109067261476866/330727796647395330/Untitled12111.jpg");

            embed.WithFooter(a => a.Text =
            "Copyright 2017 © Mobile Koth");
            embed.WithCurrentTimestamp();

            await ReplyAsync(string.Empty, embed: embed);
        }

        [Command("ping")]
        public async Task Ping()
        {
            msg = await ReplyAsync("`loading...`");
            await Context.Client.SetGameAsync(".info for information");
            await msg.ModifyAsync(x => 
            {
                x.Content = "`Bot delay: " + (msg.Timestamp - Context.Message.Timestamp).TotalMilliseconds + " ms`\n";
                x.Embed = new EmbedBuilder().WithDescription("Pong!\n" + "Server ID: " + Context.Guild.Id).Build();
            });
        }
    }
}
