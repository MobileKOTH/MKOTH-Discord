using Discord.Commands;
using Discord;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Management;

namespace MKOTHDiscordBot
{
    public class Information : ModuleBase<SocketCommandContext>
    {
        /*
        [Command("")]
        [Alias("")]
        [Summary("")]
        public async Task Test([Remainder] string para)
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            msg = await ReplyAsync("Test");
        }
        */

        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

        [Command("info")]
        [Alias("stats")]
        [Summary("Display bot information and statistics.")]
        public async Task Info()
        {
            ulong memorySize = 0;
            ObjectQuery winQuery = new ObjectQuery("SELECT * FROM CIM_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(winQuery);
            foreach (ManagementObject item in searcher.Get())
            {
                memorySize = (ulong)item["TotalVirtualMemorySize"];
            }

            string prcName = Process.GetCurrentProcess().ProcessName;
            var counter = new PerformanceCounter("Process", "Working Set - Private", prcName);

            var embed = 
                new EmbedBuilder()
                .WithTitle("Information")
                .WithDescription("Official MKOTH Management Bot. In early development and testing phase.")
                .WithUrl("https://mobilekoth.wordpress.com/")
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/341163606605299716/360336022745382912/13615239_1204861226212220_2613382245523520956_n.png")
                .WithAuthor(
                    new EmbedAuthorBuilder()
                    .WithName("Developed by " + Globals.BotOwner.Username)
                    .WithIconUrl(Globals.BotOwner.GetAvatarUrl()))
                .AddField("Help Command", "```.MKOTHHelp```")
                .AddField("Library", "```Discord.Net v2.0.0```", true)
                .AddField("Memory Usage", string.Format("```{0:N2} MB```", ((double)counter.RawValue) / 1024 / 1024), true)
                .AddField("Build", $"```v{Globals.BuildVersion}```", true)
                .AddField("System", string.Format("```Free RAM: {0:N2} / {1:N2} GB\nCPU Load: {2}%```", ramCounter.NextValue() / 1024, memorySize / 1024 / 1024, cpuCounter.NextValue()))
                .AddField("Up Time", $"```{(DateTime.Now - Globals.DeploymentTime)}```")
                .WithImageUrl("https://cdn.discordapp.com/attachments/271109067261476866/330727796647395330/Untitled12111.jpg")
                .WithFooter(a => a.Text = "Copyright 2018 © Mobile Koth")
                .WithCurrentTimestamp()
                .WithColor(Color.Orange)
                .Build();

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
                x.Embed = new EmbedBuilder()
                .WithDescription("Pong!")
                .WithColor(Color.Orange).Build();
            });
        }

        [Command("ping")]
        public async Task Ping([Remainder] string para)
        {
            EmbedBuilder embed = new EmbedBuilder();
            await ReplyAsync("`Bot client latency: " + Context.Client.Latency + " ms`\n", false,
                embed.WithDescription("Pong!")
                .AddField("Reflect", para)
                .WithColor(Color.Orange).Build());
        }

        [Command("settest")]
        public async Task Settest()
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            if (Program.TestMode) return;
            if (Context.Message.Author.Id != Program.OwnerID) return;
            Program.ReplyToTestServer = false;
            msg = await ReplyAsync("Disabled replying to test server");
        }
    }
}
