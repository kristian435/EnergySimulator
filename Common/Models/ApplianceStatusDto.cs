using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class ApplianceStatusDto
    {
        public List<Appliance> Appliances { get; set; } = new();
        public double TotalConsumptionFromAppliancesKw { get; set; }
    }
}
