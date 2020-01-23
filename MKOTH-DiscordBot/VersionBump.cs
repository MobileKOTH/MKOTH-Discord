#if DEBUG
using System;
using System.Linq;
using System.Xml;
using System.IO;

namespace MKOTHDiscordBot
{
    public static class VersionBump
    {
        public static string Bump()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            var projectFile = Directory.GetFiles("../../../", "*.csproj").First();
            var xml = new XmlDocument();
            xml.Load(projectFile);

            try
            {
                var node = xml.SelectSingleNode("/Project/PropertyGroup/AssemblyVersion");
                var text = node.InnerText;
                var versions = text.Split(".");
                var revision = int.Parse(versions[3]) + 1;

                var version = $"{versions[0]}.{versions[1]}.{versions[2]}.{revision}";
                node.InnerText = version;

                xml.Save(projectFile);

                return version;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
#endif