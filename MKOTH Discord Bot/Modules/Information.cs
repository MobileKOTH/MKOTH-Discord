using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MKOTH_Discord_Bot
{
    public class Information : ModuleBase<SocketCommandContext>
    {
        [Command("info")]
        [Alias("stats")]
        [Summary ("Display bot information and statistics.")]
        public async Task Info()
        {
            EmbedBuilder embed = new EmbedBuilder();

            string prcName = Process.GetCurrentProcess().ProcessName;
            var counter = new PerformanceCounter("Process", "Working Set - Private", prcName);

            embed.WithTitle("Information");
            embed.WithDescription("Official MKOTH Management Bot. In early development and testing phase.");
            embed.WithUrl("https://mobilekoth.wordpress.com/");
            embed.WithThumbnailUrl("https://cdn.discordapp.com/attachments/341163606605299716/360336022745382912/13615239_1204861226212220_2613382245523520956_n.png");

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.WithName("Created by Cerlancism CY");
            author.WithIconUrl("https://cdn.discordapp.com/avatars/234242692303814657/536e902dca1564f8f49afdc2113e7ce0.png");
            embed.WithAuthor(author);

            embed.AddField("Help Command", "`.mkothhelp`", false);
            embed.AddField("Library", "Discord.Net v1.0.2", true);
            embed.AddField("Memory", string.Format("{0:N2} MB", ((double)(counter.RawValue / 1024)) / 1024), true);

            embed.WithImageUrl("https://cdn.discordapp.com/attachments/271109067261476866/330727796647395330/Untitled12111.jpg");

            embed.WithFooter(a => a.Text = "Copyright 2017 © Mobile Koth");
            embed.WithCurrentTimestamp();
            embed.WithColor(Color.Orange);

            await ReplyAsync(string.Empty, embed: embed);
        }

        [Command("ping")]
        public async Task Ping()
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            msg = await ReplyAsync("`loading...`");

            await msg.ModifyAsync(x =>
            {
                x.Content = "`Bot delay: " + (msg.Timestamp - Context.Message.Timestamp).TotalMilliseconds + " ms`\n";
                x.Embed = new EmbedBuilder().
                WithDescription("Pong!\n" + "Server ID: " + Context.Guild.Id).
                WithColor(Color.Orange).Build();
            });
        }

        [Command("settest")]
        public async Task Settest()
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            if (Context.Message.Author.Id != 234242692303814657L)
            {
                return;
            }

            Program.ReplyToTestServer = !Program.ReplyToTestServer;

            if (Program.ReplyToTestServer)
            {
                msg = await ReplyAsync("Replying to test server");
            }
            else
            {
                msg = await ReplyAsync("Disabled replying to test server");
            }
        }
    }
}
