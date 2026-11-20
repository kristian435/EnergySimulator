using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class TimeResponseDto
    {
        public DateTime RealTime { get; set; }           // Oikea kellonaika
        public DateTime SimulatedTime { get; set; }     // Simuloitu aika
        public long SimulationSpeed { get; set; }       // esim. 60 = 60x nopeus
        public double SimulatedHoursPerRealSecond { get; set; }
    }
}
