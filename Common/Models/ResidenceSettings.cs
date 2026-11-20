using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class ResidenceSettings
    {
        public int People { get; set; } = 4;
        public string HeatingMethod { get; set; } = "heat pump";
        public double Insulation { get; set; } = 0.75;
        public string CarType { get; set; } = "electric";
        public double MonthlyFee { get; set; } = 5.0;
    }
}
