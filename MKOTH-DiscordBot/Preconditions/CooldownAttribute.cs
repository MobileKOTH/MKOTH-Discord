using System;
using System.Linq;
using System.Threading.Tasks;

using Discord.Commands;

using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot
{
    class CommandCooldown : RateLimiterBase<ulong>
    {
        public CommandCooldown(double fairUsageinterval, double triggeredColdown, int burstLimitTimes) : base(fairUsageinterval, triggeredColdown, burstLimitTimes)
        {
            limiterDebugName = "Cooldown Attribute";
        }

        public TimeSpan GetCoolDown(ulong id)
        {
            return (limitedEntities.FirstOrDefault(x => x.Id == id)?.StartTime.AddMilliseconds(cooldown) ?? DateTime.Now) - DateTime.Now;
        }
    }

    public class CooldownAttribute : PreconditionAttribute
    {
        private int CoolDownMS { get; }
        private int Bursts { get;  }
        private CommandCooldown CommandCooldown { get; }
        public CooldownAttribute(int cooldownMs, int bursts = 1) 
        {
            CoolDownMS = cooldownMs;
            Bursts = bursts;
            CommandCooldown = new CommandCooldown(cooldownMs, cooldownMs, bursts);
            Logger.Debug($"Cooldown Attibute: {cooldownMs}");
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (CommandCooldown.Audit(context.User.Id))
            {
                return Task.FromResult(
                    PreconditionResult.FromError(
                        $"You have to wait for another {CommandCooldown.GetCoolDown(context.User.Id).AsRoundedDuration()} to use this command."));
            }
            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        public override string ToString()
        {
            return $"Cooldown {TimeSpan.FromMilliseconds(CoolDownMS).AsRoundedDuration()} for every {Bursts} use(s).";
        }
    }
}
