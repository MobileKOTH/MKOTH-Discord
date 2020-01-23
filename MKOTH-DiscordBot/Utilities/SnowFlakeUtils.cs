using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKOTHDiscordBot.Utilities
{
    public static class SnowFlakeUtils
    {
        public static ulong getTimeMilliseconds(ulong snowflake) => (snowflake >> 22) + 1420070400000;
        public static ulong getWorker(ulong snowflake) => (snowflake & 0x3E0000) >> 17;
        public static ulong getProcess(ulong snowflake) => (snowflake & 0x1F000) >> 12;
        public static ulong getIncrement(ulong snowflake) => snowflake & 0xFFF;

        public static string getFieldLine(ulong target, string name) => $"{name}: `{target}`";

        public static string getField(ulong Worker, ulong Process, ulong Increment) 
            => string.Join("\n",
                getFieldLine(Worker, nameof(Worker)),
                getFieldLine(Process, nameof(Process)),
                getFieldLine(Increment, nameof(Increment)));
    }
}
