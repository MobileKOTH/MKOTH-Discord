using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MKOTHDiscordBot.Properties;
using Discord.WebSocket;
using MKOTHDiscordBot.Models;

namespace MKOTHDiscordBot.Services
{
    public class RoleManager
    {
        private readonly DiscordSocketClient client;
        private readonly ISeriesService seriesService;
        private readonly IRankingService rankingService;

        private readonly Lazy<IGuild> lazyguild;
        private readonly Lazy<IRole> lazyMemberRole;

        private IGuild Guild => lazyguild.Value;
        private IRole MemberRole => lazyMemberRole.Value;

        private Dictionary<string, IRole> TierRoles => lazyTierRoles.Value;

        private Lazy<Dictionary<string, IRole>> lazyTierRoles;

        private IRole King => TierRoles.GetValueOrDefault("King");
        private IRole Nobles => TierRoles.GetValueOrDefault("Nobles");
        private IRole Squires => TierRoles.GetValueOrDefault("Squires");
        private IRole Vassal => TierRoles.GetValueOrDefault("Vassal");
        private IRole Peasant => TierRoles.GetValueOrDefault("Peasant");

        public RoleManager(IServiceProvider serivces, IOptions<AppSettings> appSettings)
        {
            client = serivces.GetService<DiscordSocketClient>();
            seriesService = serivces.GetService<SeriesService>();
            rankingService = serivces.GetService<RankingService>(); 
            rankingService.Updated += HandleSeriesUpdate;

            var settings = appSettings.Value.Settings;

            lazyguild = new Lazy<IGuild>(() => client.GetGuild(settings.ProductionGuild.Id));
            lazyMemberRole = new Lazy<IRole>(() => Guild.GetRole(settings.ProductionGuild.MemberRole));
            lazyTierRoles = new Lazy<Dictionary<string, IRole>>(() => new Dictionary<string, IRole> 
            {
                { "King", Guild.Roles.First(x => x.Name.Contains("MKOTH King")) },
                { "Nobles", Guild.Roles.First(x => x.Name.Contains("MKOTH Nobles")) },
                { "Squires", Guild.Roles.First(x => x.Name.Contains("MKOTH Squires")) },
                { "Vassal", Guild.Roles.First(x => x.Name.Contains("MKOTH Vassal")) },
                { "Peasant", Guild.Roles.First(x => x.Name.Contains("MKOTH Peasant")) },
            });
        }

        private async Task HandleSeriesUpdate()
        {
            Logger.Debug("Updating Member Roles", "Update");
            var members = await Guild.GetUsersAsync();
            var playerSet = rankingService.SeriesPlayers.ToDictionary(x => ulong.Parse(x.Id));

            foreach (var member in members)
            {
                if (playerSet.ContainsKey(member.Id))
                {
                    var player = playerSet.GetValueOrDefault(member.Id);

                    if (!member.RoleIds.Any(x => x == MemberRole.Id))
                    {
                        await member.AddRoleAsync(MemberRole);
                    }
                    await UpdateTierRole(member, player.Elo, rankingService.SeriesPlayers.First().Id == player.Id);
                }
                else if (member.RoleIds.Any(x => x == MemberRole.Id))
                {
                    await member.RemoveRoleAsync(MemberRole);
                }
            }

            Logger.Debug("Updated Member Roles", "Update");
        }

        public async Task HandleUserJoin(IGuildUser user)
        {
            var player = rankingService.SeriesPlayers.SingleOrDefault(x => x.Id == user.Id.ToString());

            if (player == default)
            {
                return;
            }

            await user.AddRoleAsync(MemberRole);
            await UpdateTierRole(user, player.Elo, rankingService.SeriesPlayers.First().Id == player.Id);
        }

        public async Task UpdateTierRole(IGuildUser user, double elo, bool isFirstPosition)
        {
            // Noble and King
            if (elo >= 1400)
            {
                if (!user.RoleIds.Contains(Nobles.Id))
                {
                    await user.AddRoleAsync(Nobles);
                }
                if (user.RoleIds.Contains(Squires.Id))
                {
                    await user.RemoveRoleAsync(Squires);
                }
                if (isFirstPosition)
                {
                    // The king is a special title awarded to the top ranking player on the condition that there are at least 2 noblemen present on the rankings.
                    if (!user.RoleIds.Contains(King.Id) && rankingService.SeriesPlayers.Count(x => x.Elo >= 1400) > 2)
                    {
                        await user.AddRoleAsync(King);
                    }
                }
                else if (user.RoleIds.Contains(King.Id))
                {
                    await user.RemoveRoleAsync(King);
                }
            }
            // Squires
            else if (elo >= 1300)
            {
                if (user.RoleIds.Contains(Nobles.Id))
                {
                    await user.RemoveRoleAsync(Nobles);
                }
                if (!user.RoleIds.Contains(Squires.Id))
                {
                    await user.AddRoleAsync(Squires);
                }
                if (user.RoleIds.Contains(Vassal.Id))
                {
                    await user.RemoveRoleAsync(Vassal);
                }
            }
            // Vassals
            else if (elo >= 1250)
            {
                if (user.RoleIds.Contains(Squires.Id))
                {
                    await user.RemoveRoleAsync(Squires);
                }
                if (!user.RoleIds.Contains(Vassal.Id))
                {
                    await user.AddRoleAsync(Vassal);
                }
                if (user.RoleIds.Contains(Peasant.Id))
                {
                    await user.RemoveRoleAsync(Peasant);
                }
            }
            // Peasants
            else
            {
                if (user.RoleIds.Contains(Vassal.Id))
                {
                    await user.RemoveRoleAsync(Vassal);
                }
                if (!user.RoleIds.Contains(Peasant.Id))
                {
                    await user.AddRoleAsync(Peasant);
                }
            }
        }
    }
}
