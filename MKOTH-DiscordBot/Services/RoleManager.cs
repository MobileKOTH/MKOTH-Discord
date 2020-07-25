using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MKOTHDiscordBot.Core;
using MKOTHDiscordBot.Properties;

namespace MKOTHDiscordBot.Services
{
    public class RoleManager
    {
        private readonly DiscordSocketClient client;
        private readonly IRankingManager rankingService;

        private readonly Lazy<IGuild> lazyguild;
        private readonly Lazy<IRole> lazyMemberRole;

        private IGuild Guild => lazyguild.Value;
        private IRole MemberRole => lazyMemberRole.Value;

        private Dictionary<string, IRole> TierRoles => lazyTierRoles.Value;

        private Lazy<Dictionary<string, IRole>> lazyTierRoles;

        private IRole King => TierRoles["King"];
        private IRole Nobles => TierRoles["Nobles"];
        private IRole Squires => TierRoles["Squires"];
        private IRole Vassal => TierRoles["Vassal"];
        private IRole Peasant => TierRoles["Peasant"];

        public RoleManager(IServiceProvider services, IOptions<AppSettings> appSettings)
        {
            client = services.GetService<DiscordSocketClient>();
            rankingService = services.GetService<IRankingManager>(); 
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
            // TODO: Now all users are not guaranteed to be complete, especially offline users.
            var members = await Guild.GetUsersAsync();

            if (members.Count != (Guild as SocketGuild).MemberCount)
            {
                await Guild.DownloadUsersAsync();
                members = await Guild.GetUsersAsync();
            }

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
                    try
                    {
                        await UpdateTierRole(member, player.Elo);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
                else if (member.RoleIds.Any(x => x == MemberRole.Id))
                {
                    await member.RemoveRoleAsync(MemberRole);
                    await member.RemoveRolesAsync(TierRoles.Select(x => x.Value));
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
            await UpdateTierRole(user, player.Elo);
        }

        public IRole TierRole(Tiers tier) => tier switch
        {
            Tiers.King => King,
            Tiers.Nobles => Nobles,
            Tiers.Squires => Squires,
            Tiers.Vassals => Vassal,
            _ => Peasant,
        };

        public async Task UpdateTierRole(IGuildUser user, double elo)
        {
            var role = TierRole(rankingService.SeriesPlayers.PlayerTeir(user.Id.ToString()));

            var badTiers = TierRoles.Select(x => x.Value.Id).Where(x => x != role.Id);
            var intersects = badTiers.Intersect(user.RoleIds);

            if (intersects.Any())
            {
                Logger.Debug("Remove role for " + user.Username, $"[{intersects.Select(x => TierRoles.Values.First(y => y.Id == x).Name).JoinLines(", ")}]");
                await user.RemoveRolesAsync(TierRoles.Values.Where(x => intersects.Any(y => x.Id == y)));
            }
            if (!user.RoleIds.Contains(role.Id))
            {
                Logger.Debug("Add role for " + user.Username, $"[{role.Name}]");
                await user.AddRoleAsync(role);
            }
        }
    }
}
