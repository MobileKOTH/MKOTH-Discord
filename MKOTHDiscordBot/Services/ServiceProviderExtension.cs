using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Collections.Immutable;
using System.Linq;

namespace MKOTHDiscordBot.Services
{
    public static class ServiceProviderExtension
    {
        public static IServiceCollection ConfigureSingletonServices(this IServiceCollection services)
        {
            var serviesRunners = ApplicationContext.CommonClasses
                .Where(x => x.GetCustomAttribute<SingletonServiceAttribute>() != null)
                .ToImmutableArray();

            foreach (var item in serviesRunners)
            {
                services.AddSingleton(item);
            }

            return services;
        }
    }
}
