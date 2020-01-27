using System;
using System.Collections.Generic;
using System.Text;

namespace MKOTHDiscordBot.Models
{
    public class Series
    {
        public DateTime Date { get; set; }
        public ulong Winner1Id { get; set; }
        public ulong LoserId { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public string ReplayId { get; set; }
    }
}
