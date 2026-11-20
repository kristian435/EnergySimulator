using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class Appliance
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public double MaxPowerKw { get; set; }
        public bool IsOn { get; set; } = false;
        public double CurrentPowerKw { get; set; }
    }
}
