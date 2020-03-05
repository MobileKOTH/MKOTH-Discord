using System;
using System.Threading.Tasks;

using Discord.Commands;

using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Preconditions
{
    class CommandCooldown : RateLimiterBase<ulong>
    {
        public CommandCooldown(double fairUsageinterval, double triggeredColdown, int burstLimitTimes) : base(fairUsageinterval, triggeredColdown, burstLimitTimes)
        {
            limiterDebugName = "Cooldown Attribute";
        }
    }

    public class CooldownAttributeAttribute : PreconditionAttribute
    {
        private int CoolDownMS { get; }
        private CommandCooldown CommandCooldown { get; }
        public CooldownAttributeAttribute(int cooldownMs)
        {
            CoolDownMS = cooldownMs;
            CommandCooldown = new CommandCooldown(cooldownMs, cooldownMs, 1);
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"Cooldown {TimeSpan.FromMilliseconds(CoolDownMS).AsRoundedDuration()}";
        }
    }
}
