using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algae.DAL.Models
{
    public class CompanionProfile
    {
        public int CompanionProfileId { get; set; }
        public int ProfileId { get; set; }
        public int CompanionId { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}