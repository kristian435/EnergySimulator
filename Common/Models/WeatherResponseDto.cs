using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class WeatherResponseDto
    {
        public DateTime Timestamp { get; set; }
        public double TemperatureC {  get; set; }
        public int SunlightPercent { get; set; }
        public double WindSpeedMs { get; set; }
        public string Condition { get; set; } = "Clear";
    }
}
