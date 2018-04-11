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

        [Command("info", RunMode = RunMode.Async)]
        [Alias("stats")]
        [Summary("Display bot information and statistics.")]
        public async Task Info()
        {
            var msgTask = ReplyAsync(string.Empty, embed: buildEmbed());

            var prcName = Process.GetCurrentProcess().ProcessName;
            var RamUsageMB = (float) new PerformanceCounter("Process", "Working Set - Private", prcName).RawValue / 1024 / 1024;
            var freeRAMGB = new PerformanceCounter("Memory", "Available MBytes").NextValue() / 1024;
            ulong ramSizeGB = 0;
            ushort cpuUsagePercent = 0;

            var ramQuery = new ObjectQuery("SELECT * FROM CIM_PhysicalMemory");
            var searcherRAM = new ManagementObjectSearcher(ramQuery);
            var cpuQuery = new ObjectQuery("SELECT * FROM CIM_Processor");
            var searcherCPU = new ManagementObjectSearcher(cpuQuery);
            foreach (var item in searcherRAM.Get()) ramSizeGB += (ulong)item["Capacity"] / 1024 / 1024 / 1024;
            foreach (var item in searcherCPU.Get()) cpuUsagePercent = (ushort)item["LoadPercentage"];

            await msgTask.Result.ModifyAsync(x => x.Embed = buildEmbed(
                string.Format("```{0:N2} MB```", RamUsageMB),
                string.Format("```Free RAM: {0:N2} / {1:N2} GB\nCPU Load: {2}%```", freeRAMGB, ramSizeGB, cpuUsagePercent)));

            Embed buildEmbed(string ramUsage = "```Loading...```", string systemInfo = "```Loading...```")
            {
                return new EmbedBuilder()
                .WithTitle("Information")
                .WithDescription("Official MKOTH Management Bot. In early development and testing phase.")
                .WithUrl("https://mobilekoth.wordpress.com/")
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/341163606605299716/360336022745382912/13615239_1204861226212220_2613382245523520956_n.png")
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName("Developed by " + Globals.BotOwner.Username)
                    .WithIconUrl(Globals.BotOwner.GetAvatarUrl()))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Help")
                    .WithValue("```.Help```"))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Library")
                    .WithValue("```Discord.Net v2.0.0```")
                    .WithIsInline(true))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Memory")
                    .WithValue(ramUsage)
                    .WithIsInline(true))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Build")
                    .WithValue($"```v{Globals.BuildVersion}```")
                    .WithIsInline(true))
                .AddField(new EmbedFieldBuilder()
                    .WithName("System")
                    .WithValue(systemInfo))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Uptime")
                    .WithValue($"```{(DateTime.Now - Globals.DeploymentTime)}```"))
                .WithImageUrl("https://cdn.discordapp.com/attachments/271109067261476866/330727796647395330/Untitled12111.jpg")
                .WithFooter(text: "Copyright 2018 © Mobile Koth")
                .WithCurrentTimestamp()
                .WithColor(Color.Orange)
                .Build();
            }
        }

        [Command("ping")]
        public async Task Ping()
        {
            var msg = await ReplyAsync("`loading...`");
            await msg.ModifyAsync(x =>
                {
                    x.Content = "`Bot delay: " + (msg.Timestamp - Context.Message.Timestamp).TotalMilliseconds + " ms`\n";
                    x.Embed = new EmbedBuilder()
                    .WithDescription("Pong!")
                    .WithColor(Color.Orange)
                    .Build();
                });
        }

        [Command("ping")]
        public async Task Ping([Remainder] string reflection)
        {
            EmbedBuilder embed = new EmbedBuilder();
            await ReplyAsync("`Bot client latency: " + Context.Client.Latency + " ms`\n", false,
                embed.WithDescription("Pong!")
                .AddField("Reflect", reflection)
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
