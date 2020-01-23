using System;
using System.IO;
using MKOTHDiscordBot.Properties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace MKOTHDiscordBot
{
    static class ConfigurationHelper
    {
        public static T GetScoppedSettings<T>(this IServiceProvider services) where T : class, new()
        {
            using (var scope = services.CreateScope())
            {
                return scope.ServiceProvider.GetService<IOptionsSnapshot<T>>().Value;
            }
        }

        public static void SetAppSettings(this IServiceProvider services, Action<AppSettings> setter)
        {
            var setting = GetScoppedSettings<AppSettings>(services);

            setter(setting);

            var file = "./Properties/appsettings.json";
            var output = JsonConvert.SerializeObject(setting, Formatting.Indented);

            Logger.Debug(setting, "Config Save");

            File.WriteAllText(file, output);
        }
    }
}
