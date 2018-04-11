﻿using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MKOTHDiscordBot
{
    [Summary("Provides the list of available commands.")]
    [Remarks("Module A")]
    public class Help : ModuleBase<SocketCommandContext>
    {
        private CommandService commands;

        public Help(CommandService _commands)
        {
            commands = _commands;
        }

        [Command("Help")]
        [Alias("H", "Manual")]
        [Summary("Display the help information.")]
        public async Task MKOTHHelp()
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithAuthor("❓ Manual")
                .WithDescription("This shows the list of command modules. " +
                "A command module is a catergory for a group of related commands.\n" +
                "Use `.help <module>` to show the commands in the module.\n" +
                "Use `.help <command>` to show the details of the command.\n" +
                "Most commands will also come with alias(abbreviation) to ease typing, e.g `.h` is the same for `.help`. " +
                "Alias for a command can be found in the command details of it.");

            commands.Modules.OrderBy(x => x.Remarks ?? "Module Z").ToList().ForEach(x =>
            {
                embed.AddField(x.Name, x.Summary ?? "In Development".MarkdownCodeBlock());
            });

            await ReplyAsync(string.Empty, false, embed.Build());
        }

        [Command("Help")]
        [Alias("H")]
        [Summary("Display the help information.")]
        public async Task MKOTHHelp([Remainder]string para)
        {
            para = para.ToLower();
            var embed = new EmbedBuilder().WithColor(Color.Orange);

            var module = commands.Modules.ToList().Find(x => x.Name.ToLower() == para);
            if (module != null)
            {
                string commandList = "";
                module.Commands.ToList().ForEach(x => commandList += $".{x.Name.AddSpace() + x.GetCommandParametersInfo()}\n");
                embed.WithAuthor("📦 Module information")
                    .WithTitle(module.Name)
                    .WithDescription(module.Summary ?? "In Development".MarkdownCodeBlock())
                    .AddField("Commands", commandList.MarkdownCodeBlock("css"));
                goto helpReplyProcedure;
            }

            var command = commands.Commands.ToList().Find(x => x.Name.ToLower() == para || x.Aliases.ToList().Find(y => y.ToLower() == para) != null);
            if (command != null)
            {
                embed.WithAuthor("📃 Command Information")
                    .WithTitle(command.Name)
                    .WithDescription(command.Summary ?? "In Development".MarkdownCodeBlock());
                goto helpReplyProcedure;
            }

            embed.WithDescription("🔎 Module / Command not found.");
            await ReplyAsync(string.Empty, false, embed.Build());
            return;

            helpReplyProcedure:
            await ReplyAsync(string.Empty, false, embed.Build());
        }
    }
}
