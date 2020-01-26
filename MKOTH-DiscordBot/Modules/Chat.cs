using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cerlancism.ChatSystem.Core;
using MKOTHDiscordBot.Common;
using MKOTHDiscordBot.Services;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

using UwuTranslator = MKOTHDiscordBot.Utilities.UwuTranslator;
using TrashChat = Cerlancism.ChatSystem.Chat;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Chat, reply and translation system.")]
    public class Chat : ModuleBase<SocketCommandContext>, IDisposable
    {
        private readonly LazyDisposable<Task<DiscordWebhookClient>> webhookLoader;
        private readonly LazyDisposable<ChatService> lazyChatService;

        private ChatService ChatService => lazyChatService.Value;

        public Chat(IServiceProvider services)
        {
            webhookLoader = new LazyDisposable<Task<DiscordWebhookClient>>(async () =>
            {
                var webhooks = await Context.Guild.GetWebhooksAsync();
                var webhookInfo = webhooks?.FirstOrDefault(x => x.ChannelId == Context.Channel.Id)
                    ?? await ((ITextChannel)Context.Channel).CreateWebhookAsync("Chat Reply");
                return new DiscordWebhookClient(webhookInfo);
            });

            lazyChatService = new LazyDisposable<ChatService>(() =>
            {
                return services.GetRequiredService<ChatService>();
            });
        }

        [Command("Reply")]
        [RequireBotPermission(GuildPermission.ManageWebhooks)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async Task Reply(ulong messageId, [Remainder] string reply)
        {
            await ReplyAs(Context.User as IGuildUser, messageId, reply);
        }

        [Command("ImpersonateReply")]
        [Alias("ReplyAs")]
        [RequireBotPermission(GuildPermission.ManageWebhooks)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async Task ReplyAs(IGuildUser user, ulong messageId, [Remainder] string reply)
        {
            var message = await Context.Channel.GetMessageAsync(messageId);
            if (message == null)
            {
                throw new Exception("Message not found.");
            }

            var loadWebhook = webhookLoader.Value;
            var embedLink = message.Embeds.Count > 0 ? $"\n{Format.Bold("Embed Content")}\n{message.Embeds.First().Author}: {message.Embeds.First().Description}" : "";
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithAuthor(message.Author)
                .WithTitle("Replying to")
                .WithUrl(message.GetJumpUrl())
                .WithDescription(message.Content?.AddLine() + embedLink)
                .WithTimestamp(message.Timestamp)
                .WithFooter($"#{message.Channel.Name}");

            var webHook = await loadWebhook;
            _ = webHook.SendMessageAsync(reply, false, new Embed[] { embed.Build() }, user.GetDisplayName(), user.GetAvatarUrl());
            _ = Context.Message.DeleteAsync();
        }

        [Command("Impersonate")]
        [RequireBotPermission(GuildPermission.ManageWebhooks)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async Task Impersonate(IGuildUser user, [Remainder] string reply)
        {
            var webhook = await webhookLoader.Value;
            _ = webhook.SendMessageAsync(reply, false, null, user.GetDisplayName(), user.GetAvatarUrl());
            _ = Context.Message.DeleteAsync();
        }

        [Command("Translate", RunMode = RunMode.Sync)]
        [Alias("t")]
        [Summary("Auto detect translate any text to English")]
        public async Task Translate([Remainder] string input)
            => await TranslateInternal(input);

        [Command("TranslateFrom", RunMode = RunMode.Sync)]
        [Alias("tfrom")]
        public async Task TranslateFrom(string from, [Remainder] string input)
            => await TranslateInternal(input, from);

        [Command("TranslateTo", RunMode = RunMode.Sync)]
        [Alias("tto")]
        public async Task TranslateTo(string to, [Remainder] string input)
            => await TranslateInternal(input, "", to);

        [Command("TranslateFromTo", RunMode = RunMode.Sync)]
        [Alias("tfromto")]
        public async Task TranslateFromTo(string from, string to, [Remainder] string input)
            => await TranslateInternal(input, from, to);

        internal async Task TranslateInternal(string input, string from = "", string to = "en")
        {
            var apiBase = "https://script.google.com/macros/s/AKfycbzgXXIUc8PGq0-h-aZkZ9gfGBnBLi-BPn3JJ9cjV5B7ZbLu2eY/exec";
            var resource = "translate";
            var uri = $"{apiBase}?resource={resource}&from={from}&to={to}&input={input}";
            var request = WebRequest.Create(uri);
            var response = request.GetResponse() as HttpWebResponse;
            var json = string.Empty;
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                json = sr.ReadToEnd();
            }
            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(json);
                await ReplyAsync(result.response.Value);
            }
            catch (Exception)
            {
                await ReplyAsync("Bad Arguments.");
            }
        }

        [Command("TrashInfo", RunMode = RunMode.Async)]
        [Summary("Displays the possible response the bot will give when being pinged with the content.")]
        [Alias("ti")]
        public async Task TrashInfo([Remainder] string message)
        {
            DateTime start = DateTime.Now;
            var chatSystem = ChatService.ChatSystem;
            var purgedMessage = TrashChat.PurgeMessage(message);
            var (wordCount, analysis) = await chatSystem.AnalyseAsync(purgedMessage);
            var results = chatSystem.GetResults(wordCount, analysis).ToArray();
            var takeResults = results.Take(25).ToArray();
            var embed = new EmbedBuilder();

            if (results.First().Score == 0)
            {
                await ReplyAsync($"No results for: ```{purgedMessage.SliceBack(1900)}```");
                return;
            }

            foreach (var item in takeResults)
            {
                embed.AddField(
                    string.Format("{0:N2}%",
                    item.Score * 100),
                    $"`#{item.Trigger.Id}` {item.Trigger.Message.SliceBack(100)}\n" +
                    $"`#{item.Rephrase.Id}` {item.Rephrase.Message.SliceBack(100)}\n" +
                    $"`#{item.Response.Id}` {item.Response.Message.SliceBack(100)}");

                var omission = takeResults.Length < results.Length ? results.Length - takeResults.Length : 0;
                embed.WithFooter($"Results: {takeResults.Length}" + (omission > 0 ? $", omitted: {omission}" : ""));
            }

            embed.Title = "Trigger, rephrase and reply pool";
            embed.Description = "**Match %** `#ID Trigger` `#ID Rephrase` `#ID Reply`";
            await ReplyAsync($"`Process time: {(DateTime.Now - start).TotalMilliseconds.ToString()} ms`\nTrash info for:\n\"{purgedMessage.SliceBack(100)}\"", false, embed.Build());
            analysis = null;
            results = null;
            takeResults = null;
        }

        [Command("TrashMessage", RunMode = RunMode.Async)]
        [Summary("Displays the message content of the trash Id.")]
        [Alias("tm")]
        public async Task TrashMessage(int id)
        {
            var entry = await ChatService.ChatSystem.GetEntryByIdAsync(id);
            await ReplyEntry(entry);
        }

        [Command("LastMessage", RunMode = RunMode.Async)]
        [Summary("Displays the last recorded message.")]
        [Alias("lm")]
        public async Task LastMessage()
        {
            var entry = await ChatService.ChatSystem.GetLastEntryAsync();
            await ReplyEntry(entry);
        }

        private async Task ReplyEntry(Entry entry)
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription(entry?.Message.SliceBack(1900) ?? "`No message found.`");
            await ReplyAsync($"`Message Id: #{entry?.Id}`", embed: embed.Build());
        }

        [Command("QueryLength", RunMode = RunMode.Async)]
        [Summary("Find messages with certain character length.")]
        [Alias("ql")]
        public async Task QueryLength(int length)
        {
            var history = ChatService.ChatSystem.HistoryCache;
            var results = history.Where(x => x.Message.Length >= length).ToArray();
            var takeResults = results.Take(25).ToArray();
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange);

            if (results.Count() > 0)
            {
                embed.WithDescription($"Messages with length of `{length}`: ");
                var omission = takeResults.Length < results.Length ? results.Length - takeResults.Length : 0;
                embed.WithFooter($"Results: {takeResults.Length}" + (omission > 0 ? $", omitted: {omission}" : ""));
            }
            else
            {
                embed.WithDescription($"No results with length of `{length}`");
            }

            foreach (var item in takeResults)
            {
                embed.AddField($"{item.Message.Length}", $"`#{item.Id}` {item.Message.SliceBack(100)}");
            }

            await ReplyAsync(embed: embed.Build());
        }

        [Command("TrashReply", RunMode = RunMode.Async)]
        [Summary("Get a reply against the input content")]
        public async Task Reply([Remainder] string input)
        {
            await ChatService.ReplyAsync(Context, input);
        }

        [Command("AltCase", RunMode = RunMode.Async)]
        [Alias("ac")]
        [Summary("Convert text into a sequence which is alternate in casing.")]
        public async Task AltCase([Remainder] string input)
        {
            var skip = 0;
            if (char.IsLower(input[0]))
            {
                input = char.ToUpper(input[0]) + input.Substring(1);
                skip = 1;
            }
            var skipped = input.Take(skip).ToArray();
            var switcher = true;
            var rest = input.Skip(skip).Select((x, i) => 
            {
                if (!char.IsLetter(x))
                {
                    return x;
                }
                
                char outputChar;

                if (switcher)
                {
                    outputChar = char.ToLower(x);
                }
                else
                {
                    outputChar = char.ToUpper(x);
                }

                switcher = !switcher;

                return outputChar;
            }).ToArray();
            var output = new string(skipped) + new string(rest);
            await ReplyAsync(output);
        }

        [Command("SentenceCase", RunMode = RunMode.Async)]
        [Alias("sc")]
        [Summary("Convert text into sentence case.")]
        public async Task SentenceCase([Remainder] string input)
        {
            // start by converting entire string to lower case
            var lowerCase = input.ToLower();
            // matches the first sentence of a string, as well as subsequent sentences
            var r = new Regex(@"(^[a-z])|[?!.:;]\s+(.)", RegexOptions.ExplicitCapture);
            // MatchEvaluator delegate defines replacement of setence starts to uppercase
            var result = r.Replace(lowerCase, s => s.Value.ToUpper());
            await ReplyAsync(result);
        }

        [Command("uwu")]
        public async Task Uwu([Remainder] string input)
        {
            var output = UwuTranslator.Translate(input);
            await ReplyAsync(output);
        }

        public void Dispose()
        {
            lazyChatService.Dispose();
            webhookLoader.Dispose();
        }
    }
}
