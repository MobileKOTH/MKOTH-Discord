using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cerlancism.TieredEloRankingSystem;
using System.IO;

namespace MKOTHDiscordBot.Services
{
    public class RankingSystem : IDisposable
    {
        private string ConnectionString => ConfigurationManager.ConnectionStrings["TieredEloDatabase"].ConnectionString.Replace(".db", "+" + guildId + ".db");

        public RankingProcessor Processor;
        private ulong guildId;

        public RankingSystem(ulong guildId, IServiceProvider services)
        {
            this.guildId = guildId;
            Processor = new RankingProcessor(guildId, ConnectionString);
        }

        public (string fileName, byte[] fileBytes) Reset()
        {
            var path = ConnectionString.Replace("FileName=", "");
            var fileName = Path.GetFullPath(path);
            var bytes = File.ReadAllBytes(path);
            File.Delete(path);
            return (fileName, bytes);
        }

        public void Dispose()
        {
            Processor.Dispose();
            Logger.Debug("Disposed", nameof(RankingSystem));
        }
    }
}
