using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MKOTHDiscordBot.Services
{
    public static class ServiceExtensions
    {
        private static ImmutableArray<Type> ServiceRunners => ApplicationContext.CommonClasses
                .Where(x => x.GetCustomAttribute<SingletonServiceAttribute>() != null)
                .ToImmutableArray();

        public static IServiceCollection ConfigureSingletonServices(this IServiceCollection services)
        { 
            foreach (var item in ServiceRunners)
            {
                services.AddSingleton(item);
            }

            return services;
        }

        public static IServiceProvider StartForcedInstances(this IServiceProvider services)
        {
            foreach (var item in ServiceRunners)
            {
                if (item.GetCustomAttribute<SingletonServiceAttribute>().ForceInstantiate)
                {
                    ActivatorUtilities.CreateInstance(services, item);
                }
            }

            return services;
        }
    }
}
