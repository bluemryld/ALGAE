using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algae.DAL.Models
{
    public class Profile
    {
        public int ProfileId { get; set; }
        public int GameId { get; set; }
        public string ProfileName { get; set; } = string.Empty;
        public string? CommandLineArgs { get; set; }
    }
}