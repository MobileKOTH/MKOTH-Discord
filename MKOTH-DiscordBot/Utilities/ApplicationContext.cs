using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Discord;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    using System.Runtime;
    using System.Runtime.InteropServices;
    using static Assembly;
    using static DateTime;

    public static class ApplicationContext
    {
        public static IReadOnlyList<Type> SurfaceClasses 
            => GetEntryAssembly()
            .GetTypes()
            .Where(x => x.IsClass && !x.IsNested)
            .ToImmutableArray();

        public static Version AssemblyVersion 
            => GetExecutingAssembly().GetName().Version;

        public static Version DiscordVersion
            => typeof(DiscordSocketClient).Assembly.GetName().Version;

        public static readonly DateTime DeploymentTime = Now;

        public static DiscordSocketClient DiscordClient;
        public static IUser BotOwner;

        public static class MKOTHHQGuild
        {
            public static SocketGuild Guild => DiscordClient.GetGuild(270838709287387136);
            public static SocketTextChannel Test => Guild.TextChannels.Single(x => x.Id == 395982667876663306);
            public static SocketTextChannel Log => Guild.TextChannels.Single(x => x.Id == 360352712619065345);
        }

        /// <summary>
        /// Return correct .NET Core product name like ".NET Core 2.1.0" instead of ".NET Core 4.6.26515.07" returning by RuntimeInformation.FrameworkDescription
        /// </summary>
        /// <returns></returns>
        public static string GetFrameworkDescription()
        {
            // ".NET Core 4.6.26515.07" => ".NET Core 2.1.0"
            var parts = RuntimeInformation.FrameworkDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var i = 0;
            for (; i < parts.Length; i++)
            {
                if (char.IsDigit(parts[i][0]))
                {
                    break;
                }
            }
            var productName = string.Join(' ', parts, 0, i);
            return string.Join(' ', productName, GetNetCoreVersion());
        }

        public static string GetNetCoreVersion()
        {
            var assembly = typeof(GCSettings).GetTypeInfo().Assembly;
            var assemblyPath = assembly.CodeBase.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            int netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
            if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2)
                return assemblyPath[netCoreAppIndex + 1];
            return null;
        }
    }
}
