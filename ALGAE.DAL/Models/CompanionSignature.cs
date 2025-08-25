using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algae.DAL.Models
{
    public class CompanionSignature
    {
        public int CompanionSignatureId { get; set; }
        public int GameSignatureId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ExecutableName { get; set; } = string.Empty;
        public string? CompanionArgs { get; set; }
        public string? Version { get; set; }
        public string? Publisher { get; set; }
        public string? MetaName { get; set; }
        public bool MatchName { get; set; }
        public bool MatchVersion { get; set; }
        public bool MatchPublisher { get; set; }
    }
}
