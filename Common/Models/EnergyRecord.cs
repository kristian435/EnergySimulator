using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class EnergyRecord
    {
        public DateTime Timestamp { get; set; }
        public double SolarProductionKw { get; set; }
        public double ConsumptionKw { get; set; }
        public double BatteryLevelPercent { get; set; }
        public double GridImportKw { get; set; }
        public double GridExportKw { get; set; }
        public double ApplianceConsumptionKw { get; set; }
    }
}
