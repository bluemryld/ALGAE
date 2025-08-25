using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algae.DAL.Models
{
    public class Game
    {
        public int GameId { get; set; }
        public string ShortName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? GameImage { get; set; }
        public string? ThemeName { get; set; }
        public string InstallPath { get; set; } = string.Empty;
        public string? GameWorkingPath { get; set; }
        public string? ExecutableName { get; set; }
        public string? GameArgs { get; set; }
        public string? Version { get; set; }
        public string? Publisher { get; set; }
    }
}