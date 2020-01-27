using System;
using System.Collections.Generic;
using System.Text;

namespace MKOTHDiscordBot.Models
{
    public class Series
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public ulong WinnerId { get; set; }
        public ulong LoserId { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public string ReplayId { get; set; }
    }
}
