using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using MKOTHDiscordBot.Services;
using MKOTHDiscordBot.Common;
using MKOTHDiscordBot.Properties;

namespace MKOTHDiscordBot.Modules
{
    using static Utilities.SnowFlakeUtils;

    [Summary("Run diagnostics and show maintenance information of the bot environment.")]
    [Remarks("Module Z")]
    public class Maintenance : ModuleBase<SocketCommandContext>, IDisposable
    {
        private readonly LazyDisposable<IssueTracker> lazyIssueTracker;
        private readonly string prefix;
        private static Process nodeJS = null;

        public Maintenance(IServiceProvider services)
        {
            lazyIssueTracker = new LazyDisposable<IssueTracker>(() => services.GetRequiredService<IssueTracker>());
            prefix = services.GetScoppedSettings<AppSettings>().Settings.DefaultCommandPrefix;
        }

        [Command("BotInfo", RunMode = RunMode.Async)]
        [Alias("BotStats", "SystemInfo", "sys", "system", "stats")]
        [Summary("Displays the bot information and statistics.")]
        public async Task BotInfo()
        {
            var msgTask = ReplyAsync(embed: buildEmbed());

            var (ramUsageMB, freeRamGB, ramSizeGB, cpuUsagePercent) = ApplicationManager.GetResourceUsage();

            await msgTask.Result.ModifyAsync(x => x.Embed = buildEmbed(
                $"{ramUsageMB.ToString("N2")} MB",
                $"Free RAM: {freeRamGB.ToString("N2")} / {ramSizeGB.ToString("N2")} GB\nCPU Load: {cpuUsagePercent.ToString()}%"));

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
        public async Task DiscordStats(ITextChannel channel = null)
        {
            await DiscordStats(channel, 100);
        }

        [Command("DiscordStats", RunMode = RunMode.Async)]
        [Alias("ds")]
        [RequireDeveloper]
        public async Task DiscordStats(ITextChannel channel = null, int limit = 100)
        {
            channel = channel ?? (ITextChannel)Context.Channel;
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

            string getMaxField(Func<ulong, ulong> converter)
                => getMinMax(converter, x => x.OrderByDescending(y => y.value))
                    .Forward(x
                    => getStamped(x.message, x.value));

            string getStamped(IMessage message, ulong value) => $"`{value}`\nId:` {message.Id}`\nTimeStamp: {message.Timestamp.LocalDateTime}";
        }

        [Command("Logs")]
        [Summary("Dumps the latest general system logs.")]
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
                    _ = ErrorResolver.Handle(e);
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
                    ErrorResolver.Threshold = thresHoldValue;
                    await ReplyAsync($"Restart threshold set to: `{thresHoldValue}`.");
                }
                else
                {
                    await ReplyAsync("Please enter more than 3");
                }
            }
            else
            {
                await ReplyAsync($"Current Error Count: `{ErrorResolver.CriticalErrors}`\nRestart threshold: `{ErrorResolver.Threshold}`");
            }
        }

        [Command("ChangeBotUserName")]
        [RequireDeveloper]
        public async Task ChangeUserName([Remainder] string userName)
        {
            await Context.Client.CurrentUser.ModifyAsync(x => x.Username = userName);
            await ReplyAsync($"My username has changed to: {Format.Bold(userName)}");
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
            var afterMessage = $"After GC: `{ApplicationManager.GetResourceUsage().ramUsageMB.ToString("N2")} MB`";

            await modTask;

            _ = msg.ModifyAsync(x => x.Content = $"{collectionMessage}\n{beforeMessage}\n{afterMessage}");
        }

        [Command("User")]
        [Summary("Checks the user's registration and server join date.")]
        [RequireContext(ContextType.Guild)]
        public async Task User(IGuildUser user = null)
        {
            user ??= Context.User as IGuildUser;
            var isThisBot = user.Id == Context.Client.CurrentUser.Id;
            if (!isThisBot)
            {
                user = await user.Guild.GetUserAsync(user.Id, CacheMode.AllowDownload);
            }
            var resgistrationDate = user.CreatedAt;
            var joinedDate = user.JoinedAt.Value;
            var difference = joinedDate - resgistrationDate;
            var activity = !isThisBot ? user.Activity : Context.Client.Activity;

            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithAuthor(user)
                .WithDescription($"**Registered:** {resgistrationDate.ToString("R")}\n" +
                $"**Joined:** {joinedDate.ToString("R")}\n" +
                $"**Difference:** {difference.AsRoundedDuration()}");

            if (activity != null)
            {
                var type = $"{Enum.GetName(typeof(ActivityType), activity.Type).ToLower()}";
                var name = activity.Name;
                if (activity is StreamingGame stream)
                {
                    name = $"[{stream.Name}]({stream.Url})";
                }
                if (activity is RichGame game)
                {
                    name = $"{game.Name} ({game.State} - {game.Details})";
                    if (game.LargeAsset != null && game.SmallAsset != null)
                    {
                        embed.WithImageUrl(game.LargeAsset.GetImageUrl())
                            .WithThumbnailUrl(game.SmallAsset.GetImageUrl())
                            .WithFooter($"{game.SmallAsset.Text} | {game.LargeAsset.Text}");
                    }
                }
                embed.Description += $"\n\nThe user is currently **{type}:** {name}";
            }

            await ReplyAsync(user == Context.User ? "Checking yourself." : string.Empty, embed: embed.Build());
        }

        [Command("CreateIssue")]
        [RequireDeveloper]
        public async Task CreateIssue([Remainder] string data)
        {
            var tracker = lazyIssueTracker.Value;
            var issue = JsonConvert.DeserializeObject<Issue>(data, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            tracker.CreateIssue(issue.Title, issue.Content);
            await Issues();
        }

        [Command("UpdateIssue")]
        [RequireDeveloper]
        public async Task UpdateIssue(int id, [Remainder] string data)
        {
            var tracker = lazyIssueTracker.Value;
            var issue = JsonConvert.DeserializeObject<Issue>(data, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            var success = tracker.UpdateIssue(id, issue.Title, issue.Content);
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

        [Command("node")]
        [RequireDeveloper]
        public async Task NodeJS([Remainder] string input)
        {
            if (nodeJS == null)
            {
                _ = ReplyAsync("Node process opened");
                nodeJS = new Process();
                nodeJS.StartInfo.FileName = "node";
                nodeJS.StartInfo.UseShellExecute = false;
                nodeJS.StartInfo.RedirectStandardInput = true;
                nodeJS.StartInfo.RedirectStandardOutput = true;
                nodeJS.StartInfo.RedirectStandardError = true;
                nodeJS.Start();
            }

            var writer = nodeJS.StandardInput;
            writer.WriteLine(input);

            await Task.CompletedTask;
        }

        [Command("nodeclose")]
        [RequireDeveloper]
        public async Task NodeJSClose()
        {
            if (nodeJS != null)
            {
                await ReplyAsync("Closed Node Process");
                nodeJS.StandardInput.Close();
                nodeJS.WaitForExit();
                var output = nodeJS.StandardOutput.ReadToEnd();
                await ReplyAsync(output.SliceBack(1900).MarkdownCodeBlock());
                nodeJS.Close();
                nodeJS = null;
            }
            else
            {
                await ReplyAsync("Node Process Not started");
            }
        }

        [Command("Python")]
        [Alias("py")]
        [Summary("Eval Python snippet.")]
        [RequireDeveloper]
        public async Task Python([Remainder] string input)
        {
            await Run("py", $"-c \"{input.Replace("\"", "\\\"")}\"");
        }

        [Command("Javascript")]
        [Alias("js")]
        [Summary("Eval JavasSript snippet.")]
        [RequireDeveloper]
        public async Task JavaScript([Remainder] string input)
        {
            await Run("node", $"-e \"{input.Replace("\"", "\\\"")}\"");
        }

        [Command("Run")]
        [Summary("Command line interface.")]
        [RequireDeveloper]
        public async Task Run(string command)
        {
            await Run(command, null);
        }

        [Command("Run")]
        [Summary("Command line interface.")]
        [RequireDeveloper]
        public async Task Run(string command, [Remainder] string input)
        {
            var process = new Process();
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = command;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            if (input != null)
            {
                process.StartInfo.Arguments = input;
            }
            process.Start();

            process.StandardInput.Close();
            string output = process.StandardOutput.ReadToEnd();
            output += "\n" + process.StandardError.ReadToEnd();
            process.WaitForExit();

            await ReplyAsync(output.SliceBack(1900).MarkdownCodeBlock());
        }

        [Command("Nuke")]
        [RequireDeveloper]
        public async Task Nuke()
        {
            await ReplyAsync("Nuclear Launch Detected...");
            await ReplyAsync("https://www.motherjones.com/wp-content/uploads/2017/10/blog_nuclear_blast.jpg?resize=990,555");
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

        [Command("Update")]
        [Summary("Updates the bot.")]
        [RequireDeveloper]
        public async Task Update()
        {
            await ReplyAsync("Updating...");
            Process.Start("../update.bat", Context.Channel.Id.ToString());
            _ = Task.Run(() => ApplicationManager.ShutDownApplication());
        }

        public void Dispose()
        {
            lazyIssueTracker.Dispose();
        }
    }
}
