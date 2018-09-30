using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Contains the diagnostics and maintenance information of the bot.")]
    public class System : ModuleBase<SocketCommandContext>
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

        [Command("BotInfo", RunMode = RunMode.Async)]
        [Alias("BotStats", "SystemInfo", "sys", "system")]
        [Summary("Displays the bot information and statistics.")]
        public async Task BotInfo()
        {
            var msgTask = ReplyAsync(string.Empty, embed: buildEmbed());

            var (ramUsageMB, freeRamGB, ramSizeGB, cpuUsagePercent) = ApplicationManager.GetResourceUsage();

            await msgTask.Result.ModifyAsync(x => x.Embed = buildEmbed(
                string.Format("{0:N2} MB", ramUsageMB),
                string.Format("Free RAM: {0:N2} / {1:N2} GB\nCPU Load: {2}%", freeRamGB, ramSizeGB, cpuUsagePercent)));

            Embed buildEmbed(string ramUsage = "Loading...", string systemInfo = "Loading...")
                => new EmbedBuilder()
                .WithTitle("System Information")
                .WithDescription("[__**Official MKOTH Website**__](https://MobileKOTH.github.io)\n\nOfficial MKOTH Management Bot. In early development and testing phase.")
                .WithUrl("https://github.com/MobileKOTH")
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/341163606605299716/360336022745382912/13615239_1204861226212220_2613382245523520956_n.png")
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName("Developed by " + ApplicationContext.BotOwner.Username)
                    .WithIconUrl(ApplicationContext.BotOwner.GetAvatarUrl())
                    .WithUrl("https://github.com/Cerlancism"))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Help")
                    .WithValue(".Help".MarkdownCodeBlock("yaml")))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Library")
                    .WithValue("Discord.Net v2.0.0".MarkdownCodeBlock("yaml"))
                    .WithIsInline(true))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Memory")
                    .WithValue(ramUsage.MarkdownCodeBlock("yaml"))
                    .WithIsInline(true))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Build")
                    .WithValue($"v{ApplicationContext.BuildVersion}".MarkdownCodeBlock("yaml"))
                    .WithIsInline(true))
                .AddField(new EmbedFieldBuilder()
                    .WithName("System")
                    .WithValue(systemInfo.MarkdownCodeBlock("yaml")))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Uptime")
                    .WithValue($"{(DateTime.Now - ApplicationContext.DeploymentTime).AsRoundedDuration()}".MarkdownCodeBlock("yaml")))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Repositories")
                    .WithValue(
                    "[**GitHub:** MKOTH-GSuite](https://github.com/MobileKOTH/MKOTH-GSuite)\n" +
                    "[**GitHub:** MKOTH-Discord](https://github.com/MobileKOTH/MKOTH-Discord)"))
                .WithImageUrl("https://cdn.discordapp.com/attachments/271109067261476866/330727796647395330/Untitled12111.jpg")
                .WithFooter(text: "Copyright 2018 © Mobile Koth")
                .WithCurrentTimestamp()
                .WithColor(Color.Orange)
                .Build();
        }

        [Command("Ping")]
        [Summary("Checks the bot's connection and command response, use with an input `<echo>`(any text) to echo the input from you.")]
        public async Task Ping([Remainder] string echo = null)
        {
            var msg = await ReplyAsync("`loading...`");
            await msg.ModifyAsync(x =>
            {
                x.Content = Format.Code("Bot delay: " + (msg.Timestamp - Context.Message.Timestamp).TotalMilliseconds + " ms").AddLine() +
                "`Bot client latency: " + Context.Client.Latency + " ms`\n";

                var embed = new EmbedBuilder()
                .WithDescription("Pong!")
                .WithColor(Color.Orange);
                if (echo != null)
                {
                    embed.AddField("Echo", echo);
                }
                x.Embed = embed.Build();
            });
        }

        [Command("Logs")]
        [Summary("Dumps the latest general system logs")]
        [RequireDeveloper]
        public async Task Logs()
        {
            var blocks = File.ReadAllText(Directories.GeneralLogsFile).Split(new string[1] { "\r\n\r\n" }, StringSplitOptions.None)
                .Reverse()
                .Take(20);
            var output = string.Join("\n\n", blocks)
                .SliceBack(1900);
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("System Logs")
                .WithDescription(output);

            await ReplyAsync(string.Empty, embed: embed.Build());
        }

        [Command("SetTest")]
        [RequireDeveloper]
        public async Task SetTest()
        {
            if (Program.TestMode) return;
            Handlers.MessageHandler.ReplyToTestServer = false;
            await ReplyAsync("Disabled replying to test server");
        }

        [Command("SendError")]
        [RequireDeveloper]
        public Task SendError()
        {
            try
            {
                throw new TestError();
            }
            catch (TestError e)
            {
                _ = ErrorResolver.SendErrorAndCheckRestartAsync(e);
            }

            return Task.CompletedTask;
        }

        class TestError : Exception
        {
            public override string Message => "This is a test error";
        }

        [Command("Restart")]
        [Alias("Reboot")]
        [Summary("Restarts the bot application.")]
        [RequireDeveloper]
        public async Task Restart()
        {
            await ReplyAsync("Restarting...");
            _ = Task.Run(() => ApplicationManager.RestartApplication(Context.Channel.Id));
        }

        [Command("ShutDown")]
        [Summary("Shuts down the bot application.")]
        [RequireDeveloper]
        public async Task ShutDown()
        {
            await ReplyAsync("Shutting Down...");
            _ = Task.Run(() => ApplicationManager.ShutDownApplication());
        }
    }
}
