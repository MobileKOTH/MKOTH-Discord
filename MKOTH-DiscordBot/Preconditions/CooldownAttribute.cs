using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord.Commands;

using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot
{
    class CommandCooldown : RateLimiterBase<ulong>
    {
        public static Dictionary<string, CommandCooldown> SharedInstances { get; } = new Dictionary<string, CommandCooldown>();

        private ICommandContext lastContext;

        public CommandCooldown(double fairUsageinterval, double triggeredColdown, int burstLimitTimes) 
            : base(fairUsageinterval, triggeredColdown, burstLimitTimes) 
        {
            limiterDebugName = "Cooldown Attribute";
        }

        public static CommandCooldown GetOrCreateInstance(string name, Func<CommandCooldown> initialiser)
        {
            var output = SharedInstances.GetValueOrDefault(name);

            if (output != null)
            {
                return output;
            }

            output = initialiser();

            SharedInstances.Add(name, output);

            Logger.Debug(name, "Created cooldown shared instance");

            return output;
        }

        public TimeSpan GetCoolDown(ulong id)
        {
            return (limitedEntities.FirstOrDefault(x => x.Id == id)?.StartTime.AddMilliseconds(cooldown) ?? DateTime.Now) - DateTime.Now;
        }

        public bool Audit(ICommandContext context)
        {
            if (context == lastContext)
            {
                return IsLimited(context.User.Id);
            }
            else
            {
                if (lastContext == null)
                {
                    lastContext = context;
                }
                else
                {
                    lock (lastContext)
                    {
                        lastContext = context;
                    }
                }
                return base.Audit(context.User.Id);
            }
        }
    }

    public class CooldownAttribute : PreconditionAttribute
    {
        private int CoolDownMS { get; }
        private int Bursts { get;  }
        private CommandCooldown CommandCooldown { get; }
        public CooldownAttribute(int cooldownMs, int bursts = 1, string instanceName = null) 
        {
            CommandCooldown buildCooldown() => new CommandCooldown(cooldownMs, cooldownMs, bursts);

            CoolDownMS = cooldownMs;
            Bursts = bursts;

            if (instanceName != null)
            {
                CommandCooldown = CommandCooldown.GetOrCreateInstance(instanceName, buildCooldown);
            }
            else
            {
                CommandCooldown = buildCooldown();
            }
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            Logger.Debug($"Checking cooldown: {context.User.Username} {command.Name}");
            if (CommandCooldown.Audit(context))
            {
                return Task.FromResult(
                    PreconditionResult.FromError(
                        $"{context.User.Mention}, you have to wait for another {CommandCooldown.GetCoolDown(context.User.Id).AsRoundedDuration()} to use this command."));
            }
            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        public override string ToString()
        {
            return $"Cooldown: {TimeSpan.FromMilliseconds(CoolDownMS).AsRoundedDuration()} for every {Bursts} use(s)";
        }
    }
}
