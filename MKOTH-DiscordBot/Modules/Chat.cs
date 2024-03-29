﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Cerlancism.ChatSystem.Model;

using Discord;
using Discord.Commands;
using Discord.Webhook;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MKOTHDiscordBot.Common;
using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;

using RestSharp;

using TrashChat = Cerlancism.ChatSystem.Chat;
using UwuTranslator = MKOTHDiscordBot.Utilities.UwuTranslator;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Utilities for chat, conversation reply and translation.")]
    [Remarks("Module C")]
    public class Chat : ModuleBase<SocketCommandContext>, IDisposable
    {
        private readonly ResponseService responseService;
        private readonly LazyDisposable<Task<DiscordWebhookClient>> webhookLoader;
        private readonly LazyDisposable<ChatService> lazyChatService;
        private readonly Lazy<string> lazyTranslationScriptId;
        private readonly ulong developmentGuild;
        private readonly ulong officialGuild;
        private readonly ulong officialChat;

        private ChatService ChatService => lazyChatService.Value;

        public Chat(IServiceProvider services, IOptions<AppSettings> appSettings)
        {
            responseService = services.GetService<ResponseService>();
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

            lazyTranslationScriptId = new Lazy<string>(() => services.GetScoppedSettings<Credentials>().TranslationScriptId);

            developmentGuild = appSettings.Value.Settings.DevelopmentGuild.Id;
            officialGuild = appSettings.Value.Settings.ProductionGuild.Id;
            officialChat = appSettings.Value.Settings.ProductionGuild.Official;
        }

        [Command("RawMessage")]
        public async Task RawMessage(ITextChannel channel, ulong messageId)
        {
            var msg = await channel.GetMessageAsync(messageId);
            await ReplyAsync(Format.Sanitize(msg.Content.SliceBack(2000 - 7)).MarkdownCodeBlock());
        }

        [Command("CopyMessage")]
        [Alias("cm")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CopyMessasge(ITextChannel channel, params ulong[] messageIds)
        {
            foreach (var item in messageIds)
            {
                await CopyMessasge(channel, item);
            }
        }

        [Command("CopyMessage")]
        [Alias("cm")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task CopyMessasge(ITextChannel channel, ulong messageId)
        {
            var loadWebhook = webhookLoader.Value;
            var msg = await channel.GetMessageAsync(messageId) as IUserMessage;
            var user = msg.Author as IGuildUser;
            var webHook = await loadWebhook;
            
            _ = webHook.SendMessageAsync(msg.Content.Replace("@", "`@`") + "\n" + msg.Attachments.Select(x => x.Url).JoinLines(), false, msg.Embeds.Select(x => x as Embed), user.GetDisplayName(), user.GetAvatarUrl());
        }

        [Command("Reply")]
        [RequireBotPermission(GuildPermission.ManageWebhooks)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async Task Reply(ulong messageId, [Remainder] string reply)
        {
            await ReplyAs(Context.User as IGuildUser, messageId, reply.Replace("@", "`@`"));
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
            _ = webHook.SendMessageAsync(reply.Replace("@", "`@`"), false, new Embed[] { embed.Build() }, user.GetDisplayName(), user.GetAvatarUrl());
            _ = Context.Message.DeleteAsync();
        }

        [Command("Impersonate")]
        [RequireBotPermission(GuildPermission.ManageWebhooks)]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireContext(ContextType.Guild)]
        public async Task Impersonate(IGuildUser user, [Remainder] string reply)
        {
            var webhook = await webhookLoader.Value;
            _ = webhook.SendMessageAsync(reply.Replace("@", "`@`"), false, null, user.GetDisplayName(), user.GetAvatarUrl());
            _ = Context.Message.DeleteAsync();
        }

        //[Command("Translate", RunMode = RunMode.Sync)]
        //[Alias("t")]
        //[Summary("Auto detect translate any text to English")]
        //public async Task Translate([Remainder] string input)
        //    => await TranslateInternal(input);

        //[Command("TranslateFrom", RunMode = RunMode.Sync)]
        //[Alias("tfrom")]
        //public async Task TranslateFrom(string from, [Remainder] string input)
        //    => await TranslateInternal(input, from);

        //[Command("TranslateTo", RunMode = RunMode.Sync)]
        //[Alias("tto")]
        //public async Task TranslateTo(string to, [Remainder] string input)
        //    => await TranslateInternal(input, "", to);

        //[Command("TranslateFromTo", RunMode = RunMode.Sync)]
        //[Alias("tfromto")]
        //public async Task TranslateFromTo(string from, string to, [Remainder] string input)
        //    => await TranslateInternal(input, from, to);

        //internal async Task TranslateInternal(string input, string from = "", string to = "en")
        //{
        //    var apiBase = $"https://script.google.com/macros/s/{lazyTranslationScriptId.Value}/exec";
        //    var request = new RestRequest()
        //        .AddQueryParameter("resource", "translate")
        //        .AddQueryParameter("from", from)
        //        .AddQueryParameter("to", to)
        //        .AddQueryParameter("input", input);

        //    try
        //    {
        //        var response = await new RestClient(apiBase).GetAsync<dynamic>(request);
        //        await ReplyAsync((response["response"] as string).Replace("@", "`@`"));
        //    }
        //    catch (Exception e)
        //    {
        //        await ReplyAsync($"{e.Message}");
        //    }
        //}

        //[Command("TrashInfo", RunMode = RunMode.Async)]
        //[Summary("Displays the possible response the bot will give when being pinged with the content.")]
        //[Alias("ti")]
        //public async Task TrashInfo([Remainder] string message)
        //{
        //    DateTime start = DateTime.Now;
        //    var chatSystem = ChatService.ChatSystem;
        //    var purgedMessage = TrashChat.PurgeMessage(message);
        //    var (wordCount, analysis) = await chatSystem.AnalyseAsync(purgedMessage);
        //    var results = chatSystem.GetResults(wordCount, analysis).ToArray();
        //    var takeResults = results.Take(25).ToArray();
        //    var embed = new EmbedBuilder();

        //    if (results.First().Score == 0)
        //    {
        //        await ReplyAsync($"No results for: ```{purgedMessage.SliceBack(1900)}```");
        //        return;
        //    }

        //    foreach (var item in takeResults)
        //    {
        //        embed.AddField(
        //            string.Format("{0:N2}%",
        //            item.Score * 100),
        //            $"`#{item.Trigger.Id}` {item.Trigger.Message.SliceBack(100)}\n" +
        //            $"`#{item.Rephrase.Id}` {item.Rephrase.Message.SliceBack(100)}\n" +
        //            $"`#{item.Response.Id}` {item.Response.Message.SliceBack(100)}");

        //        var omission = takeResults.Length < results.Length ? results.Length - takeResults.Length : 0;
        //        embed.WithFooter($"Results: {takeResults.Length}" + (omission > 0 ? $", omitted: {omission}" : ""));
        //    }

        //    embed.Title = "Trigger, rephrase and reply pool";
        //    embed.Description = "**Match %** `#ID Trigger` `#ID Rephrase` `#ID Reply`";
        //    await ReplyAsync($"`Process time: {(DateTime.Now - start).TotalMilliseconds.ToString()} ms`\nTrash info for:\n\"{purgedMessage.SliceBack(100)}\"", false, embed.Build());
        //}

        //[Command("TrashMessage", RunMode = RunMode.Async)]
        //[Summary("Displays the message content of the trash Id.")]
        //[Alias("tm")]
        //public async Task TrashMessage(int id)
        //{
        //    var entry = await ChatService.ChatSystem.GetEntryByIdAsync(id);
        //    await ReplyEntry(entry);
        //}

        //[Command("LastMessage", RunMode = RunMode.Async)]
        //[Summary("Displays the last recorded message.")]
        //[Alias("lm")]
        //public async Task LastMessage()
        //{
        //    var entry = await ChatService.ChatSystem.GetLastEntryAsync();
        //    await ReplyEntry(entry);
        //}

        //private async Task ReplyEntry(Entry entry)
        //{
        //    var embed = new EmbedBuilder()
        //        .WithColor(Color.Orange)
        //        .WithDescription(entry?.Message.SliceBack(1900) ?? "`No message found.`");
        //    await ReplyAsync($"`Message Id: #{entry?.Id}`", embed: embed.Build());
        //}

        //[Command("QueryLength", RunMode = RunMode.Async)]
        //[Summary("Find messages with certain character length.")]
        //[Alias("ql")]
        //public async Task QueryLength(int length)
        //{
        //    var history = ChatService.ChatSystem.HistoryCache;
        //    var results = history.Where(x => x.Message.Length >= length).ToArray();
        //    var takeResults = results.Take(25).ToArray();
        //    var embed = new EmbedBuilder()
        //        .WithColor(Color.Orange);

        //    if (results.Count() > 0)
        //    {
        //        embed.WithDescription($"Messages with length of `{length}`: ");
        //        var omission = takeResults.Length < results.Length ? results.Length - takeResults.Length : 0;
        //        embed.WithFooter($"Results: {takeResults.Length}" + (omission > 0 ? $", omitted: {omission}" : ""));
        //    }
        //    else
        //    {
        //        embed.WithDescription($"No results with length of `{length}`");
        //    }

        //    foreach (var item in takeResults)
        //    {
        //        embed.AddField($"{item.Message.Length}", $"`#{item.Id}` {item.Message.SliceBack(100)}");
        //    }

        //    await ReplyAsync(embed: embed.Build());
        //}

        [Command("TrashReply", RunMode = RunMode.Async)]
        [Summary("Get a reply against the input content")]
        [Cooldown(60000, 3)]
        [RequireContext(ContextType.Guild)]
        public async Task Reply([Remainder] string input)
        {
            if (Context.IsPrivate)
            {
                await ReplyAsync("I can only chat in MKOTH Official Chat.");
                return;
            }
#if !DEBUG
            if (Context.Channel.Id != officialChat && Context.Guild.Id != developmentGuild && Context.Message.Author.Id != ApplicationContext.BotOwner.Id)
            {
                await ReplyAsync("I can only chat in MKOTH Official Chat.");
                return;
            }
#endif
            foreach (var user in Context.Message.MentionedUsers)
            {
                var guildUser = user as IGuildUser;
                var displayName = guildUser?.GetDisplayName() ?? user.Username;
                input = input.Replace("<@" + user.Id.ToString(), "<@!" + user.Id.ToString());
                input = input.Replace(user.Mention, displayName);
            }
            var typing = responseService.StartTypingAsync(Context);
            try
            {
                await ChatService.ReplyAsync(Context, input);
            }
            catch (Exception e)
            {
                var embed = new EmbedBuilder()
                    .WithDescription($"Error occured:\n```{e.Message}```")
                    .Build();
                await responseService.SendToChannelAsync(Context.Channel as ITextChannel, null, embed);
            }
            typing.Dispose();

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
            await ReplyAsync(output.Replace("@", "`@`"));
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
            await ReplyAsync(result.Replace("@", "`@`"));
        }

        [Command("uwu")]
        public async Task Uwu([Remainder] string input)
        {
            var output = UwuTranslator.Translate(input);
            await ReplyAsync(output.Replace("@", "`@`"));
        }

        [Command("prune", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Prune(int quantity)
        {
            var channel = ((ITextChannel)Context.Channel);
            var messages = await Context.Channel.GetMessagesAsync(quantity).FlattenAsync();
            await channel.DeleteMessagesAsync(messages);
        }

        public void Dispose()
        {
            lazyChatService.Dispose();
            webhookLoader.Dispose();
        }
    }
}
