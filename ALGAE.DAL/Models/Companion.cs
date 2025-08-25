using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algae.DAL.Models
{
    public class Companion
    {
        public int CompanionId { get; set; }  // Primary key aligned with the schema
        public int? GameId { get; set; }  // NULL means applies to all games, specific ID means game-specific
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string PathOrURL { get; set; } = string.Empty;
        public string? LaunchHelper { get; set; }
        public string? Browser { get; set; }
        public bool OpenInNewWindow { get; set; }
    }
}