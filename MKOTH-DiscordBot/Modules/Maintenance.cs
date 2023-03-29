using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

using Microsoft.Extensions.DependencyInjection;

using MKOTHDiscordBot.Common;
using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Modules
{
    using static Utilities.SnowFlakeUtils;

    [Summary("Run diagnostics and show technical information of the bot environment.")]
    [Remarks("Module Z")]
    public class Maintenance : ModuleBase<SocketCommandContext>, IDisposable
    {
        private readonly ErrorResolver resolver;
        private readonly LazyDisposable<IssueTracker> lazyIssueTracker;
        private readonly string prefix;

        public Maintenance(IServiceProvider services, ErrorResolver errorResolver)
        {
            resolver = errorResolver;
            lazyIssueTracker = new LazyDisposable<IssueTracker>(() => services.GetRequiredService<IssueTracker>());
            prefix = services.GetScoppedSettings<AppSettings>().Settings.DefaultCommandPrefix;
        }

        [Command("BotInfo", RunMode = RunMode.Async)]
        [Alias("BotStats", "SystemInfo", "sys", "system", "stats")]
        [Summary("Displays the bot information and statistics.")]
        [Cooldown(10000, 2, "SystemCommand")]
        public async Task BotInfo()
        {
            var msgTask = ReplyAsync(embed: buildEmbed());

            var (ramUsageMB, freeRamGB, ramSizeGB, cpuUsagePercent) = ApplicationManager.GetResourceUsage();

            await msgTask.Result.ModifyAsync(x => x.Embed = buildEmbed(
                $"{ramUsageMB:N2} MB",
                $"Free RAM: {freeRamGB:N2} / {ramSizeGB:N2} GB\nCPU Load: {cpuUsagePercent}%"));

            Embed buildEmbed(string ramUsage = "Loading...", string systemInfo = "Loading...")
            {
                return new EmbedBuilder()
                .WithTitle("Click Here for Official MKOTH Website")
                .WithDescription("**System Information**\n\nOfficial MKOTH Management Bot. In early development and testing phase.")
                .WithUrl("https://mobilekoth.github.io/")
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/341163606605299716/360336022745382912/13615239_1204861226212220_2613382245523520956_n.png")
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName("Developed by " + ApplicationContext.BotOwner.Username)
                    .WithIconUrl(ApplicationContext.BotOwner.GetAvatarUrl()))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Help")
                    .WithValue($"{prefix}help".MarkdownCodeBlock("yaml")))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Library")
                    .WithValue($"Discord.Net v{ApplicationContext.DiscordVersion}".MarkdownCodeBlock("yaml"))
                    .WithIsInline(true))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Memory")
                    .WithValue(ramUsage.MarkdownCodeBlock("yaml"))
                    .WithIsInline(true))
                .AddField(new EmbedFieldBuilder()
                    .WithName("Build")
                    .WithValue($"v{ApplicationContext.AssemblyVersion}".MarkdownCodeBlock("yaml"))
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
                    "GitHub: [MKOTH-GSuite](https://github.com/MobileKOTH/MKOTH-GSuite)\n" +
                    "GitHub: [MKOTH-Discord](https://github.com/MobileKOTH/MKOTH-Discord)"))
                .WithImageUrl("https://cdn.discordapp.com/attachments/271109067261476866/330727796647395330/Untitled12111.jpg")
                .WithFooter(text: "Copyright 2018 - Present © Mobile King of the Hill")
                .WithCurrentTimestamp()
                .WithColor(Color.Orange)
                .Build();
            }
        }

        [Command("Ping", RunMode = RunMode.Async)]
        [Summary("Checks the bot's realtime connection and command response.")]
        [Cooldown(10000, 2, "SystemCommand")]
        public async Task Ping()
        {
            var msg = await ReplyAsync("`loading...`");
            await msg.ModifyAsync(x =>
            {
                var (contextId, messageId) = (Context.Message.Id, msg.Id);
                var (context, message) = getContextSet(getTimeMilliseconds);
                var latency = message - context;
                var Worker = getContextSet(getWorker);
                var Process = getContextSet(getProcess);
                var Increment = getContextSet(getIncrement);

                (ulong context, ulong message) getContextSet(Func<ulong, ulong> converter) => (converter(contextId), converter(messageId));

                string getLabledField(Func<(ulong context, ulong message), ulong> selector)
                    => getField(selector(Worker), selector(Process), selector(Increment));


                x.Content = "`Direct latency: " + latency.ToString("N0") + " ms`\n";
                x.Embed = new EmbedBuilder()
                    .WithDescription("Pong!")
                    .AddField("Command", getLabledField(y => y.context), true)
                    .AddField("Response", getLabledField(y => y.message), true)
                    .WithColor(Color.Orange)
                    .Build();
            });
        }

        [Command("Pong", RunMode = RunMode.Async)]
        [Summary("Gets the client latency, with an input `<reflection>`(any text) to reflect the input from you.")]
        [Cooldown(10000, 2, "SystemCommand")]
        public async Task Pong([Remainder] string reflection = null)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription("Ping!");
            await ReplyAsync("`Discord latency: " + Context.Client.Latency + " ms`\n",
                false,
                reflection != null ? embed.AddField("Reflect", reflection).Build() : embed.Build());
        }

        [Command("DiscordStats", RunMode = RunMode.Async)]
        [Alias("ds")]
        [RequireDeveloper]
        public async Task DiscordStats(ITextChannel channel = null, int limit = 100)
        {
            channel ??= (ITextChannel)Context.Channel;
            var messages = await channel.GetMessagesAsync(limit).FlattenAsync();
            var (avgWorker, avgProcess, avgIncrement) = (getAverage(getWorker), getAverage(getProcess), getAverage(getIncrement));
            var load = (avgWorker * avgProcess * avgIncrement) * 1000;
            var first = getMinMax(getTimeMilliseconds, x => x.OrderBy(y => y.message.Id)).message;
            var last = getMinMax(getTimeMilliseconds, x => x.OrderByDescending(y => y.message.Id)).message;
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Discord Global Stats by Snowflake Sampling")
                .WithDescription($"Period: {(last.Timestamp - first.Timestamp).AsRoundedDuration()}\nSample Size: {messages.Count()}\t")
                .AddField("Max Worker", getMaxField(getWorker))
                .AddField("Max Process", getMaxField(getProcess))
                .AddField("Max Increment", getMaxField(getIncrement))
                .AddField("Average Workers", $"`{avgWorker}`")
                .AddField("Average Processes", $"`{avgProcess}`")
                .AddField("Average Increments", $"`{avgIncrement}`")
                .AddField("Estimated Average Load", $"`{load}` messages/second")
                .WithFooter($"From")
                .WithTimestamp(first.Timestamp);
            _ = ReplyAsync(embed: embed.Build());

            double getAverage(Func<ulong, ulong> coverter) => messages.Average(x => (int)coverter(x.Id) + 1);

            (IMessage message, ulong value) getMinMax(
                Func<ulong, ulong> converter,
                Func<IEnumerable<(IMessage message, ulong value)>, IEnumerable<(IMessage message, ulong value)>> selector)
                => messages.Select(x => (messages: x, value: converter(x.Id)))
                    .Forward(x => selector(x))
                    .FirstOrDefault();

            string getStamped(IMessage message, ulong value) => $"`{value}`\nId:` {message.Id}`\nTimeStamp: {message.Timestamp.LocalDateTime}";

            string getMaxField(Func<ulong, ulong> converter)
                => getMinMax(converter, x => x.OrderByDescending(y => y.value))
                    .Forward(x => getStamped(x.message, x.value));

        }

        [Command("Logs")]
        [Summary("Dumps the latest general system logs.")]
        [RequireDeveloper]
        public async Task Logs(int count = 20)
        {
            var blocks = File.ReadAllText(Directories.GeneralLogsFile).Split(new string[1] { "\r\n\r\n" }, StringSplitOptions.None)
                .Reverse()
                .Where(x => x != string.Empty)
                .Take(count);
            string output = string.Join("\n\n", blocks).SliceBackByLine(1900);

            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("System Logs")
                .WithDescription(output);

            await ReplyAsync(embed: embed.Build());
        }

        [Command("ThrowError")]
        [RequireDeveloper]
        public Task ThrowError(bool toCatch = true)
        {
            if (toCatch)
            {
                try
                {
                    throw new Exception("This is a test error");
                }
                catch (Exception e)
                {
                    _ = resolver.Handle(e);
                }
            }
            else
            {
                throw new Exception("This is a test error");
            }
            return Task.CompletedTask;
        }

        [Command("SetTest")]
        [RequireDeveloper]
        public async Task SetTest()
        {
            if (Program.TestMode) return;
            Handlers.MessageHandler.ReplyToTestServer = false;
            _ = await ReplyAsync("Disabled replying to test server");
        }

        [Command("ErrorThreshold")]
        [RequireDeveloper]
        public async Task ErrorThreshold(int? threshold = null)
        {
            if (threshold.HasValue)
            {
                var thresHoldValue = threshold.Value;

                if (thresHoldValue > 3)
                {
                    resolver.Threshold = thresHoldValue;
                    await ReplyAsync($"Restart threshold set to: `{thresHoldValue}`.");
                }
                else
                {
                    await ReplyAsync("Please enter more than 3");
                }
            }
            else
            {
                await ReplyAsync($"Current Error Count: `{resolver.CriticalErrors}`\nRestart threshold: `{resolver.Threshold}`");
            }
        }

        [Command("ChangeBotUserName")]
        [RequireDeveloper]
        public async Task ChangeUserName([Remainder] string userName)
        {
            await Context.Client.CurrentUser.ModifyAsync(x => x.Username = userName);
            await ReplyAsync($"My username has changed to: {Format.Bold(userName)}");
        }

        [Command("DownloadUsers")]
        [RequireDeveloper]
        public async Task DownloadUsers(ulong? guildId)
        {
            var guild = guildId != null ? Context.Client.GetGuild(guildId.Value) : Context.Guild;

            var replyTask = ReplyAsync("Downloading users for: " + guild.Name);
            var before = guild.Users.ToArray();
            
            await guild.DownloadUsersAsync();
            await replyTask;

            var after = guild.Users.ToArray();

            var except = after.Select(x => x.Id).Except(before.Select(x => x.Id)).Select(x => guild.GetUser(x));

            await ReplyAsync(
                $"Done! Before Members: {before.Length} Downloaded: {after.Length}", embed: new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Cache misses")
                .WithDescription(except.Select(x => x.Mention).JoinLines())
                .Build());
        }

        [Command("GarbageCollection", RunMode = RunMode.Async)]
        [Alias("GC")]
        [RequireDeveloper]
        public async Task GarbageCollection()
        {
            var msgTask = ReplyAsync("Initiating Garbage Collection".MarkdownCodeLine());
            var ramusage = ApplicationManager.GetResourceUsage().ramUsageMB.ToString("N2");
            var beforeMessage = $"Before GC: `{ramusage} MB`";
            var msg = await msgTask;
            var modTask = msg.ModifyAsync(x => x.Content = beforeMessage);

            var stopWatch = Stopwatch.StartNew();
            GC.Collect();
            stopWatch.Stop();

            var collectionMessage = $"`Garbage Collection Took: {stopWatch.Elapsed.TotalMilliseconds} ms`";
            var afterMessage = $"After GC: `{ApplicationManager.GetResourceUsage().ramUsageMB:N2} MB`";

            await modTask;

            _ = msg.ModifyAsync(x => x.Content = $"{collectionMessage}\n{beforeMessage}\n{afterMessage}");
        }

        [Command("CreateIssue")]
        [RequireDeveloper]
        public async Task CreateIssue(string title, string content)
        {
            var tracker = lazyIssueTracker.Value;
            tracker.CreateIssue(title, content);
            await Issues();
        }

        [Command("UpdateIssue")]
        [RequireDeveloper]
        public async Task UpdateIssue(int id, string title, string content)
        {
            var tracker = lazyIssueTracker.Value;
            var success = tracker.UpdateIssue(id, title, content);
            if (success)
            {
                await Issues();
            }
            else
            {
                await ReplyAsync($"Issue #{id} is not found.");
            }
        }

        [Command("DeleteIssue")]
        [RequireDeveloper]
        public async Task DeleteIssue(int id)
        {
            var tracker = lazyIssueTracker.Value;
            var success = tracker.DeleteId(id);
            if (success)
            {
                await ReplyAsync($"Deleted issue #{id}");
                await Issues();
            }
            else
            {
                await ReplyAsync($"Issue #{id} is not found.");
            }
        }

        [Command("Issues")]
        public async Task Issues()
        {
            var tracker = lazyIssueTracker.Value;
            var issues = tracker.GetIssues().Take(25);
            var fields = issues.Select(x => new EmbedFieldBuilder().WithName($"#{x.Id}  {x.Title}").WithValue($"{Format.Bold(x.Date.ToShortDateString())}\n" + x.Content.SliceBack(900)));
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Issues and Feature Requests");
            embed.Fields = fields.ToList();
            await ReplyAsync(embed: embed.Build());
        }

        [Command("KillProcess")]
        [Summary("Kills the bot application process.")]
        [RequireDeveloper]
        public async Task ShutDown()
        {
            await ReplyAsync("Exiting...");
            _ = Task.Run(() => ApplicationManager.ShutDownApplication());
        }

        public void Dispose()
        {
            lazyIssueTracker.Dispose();
        }
    }
}
