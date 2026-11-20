using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class Weather
    {
        public double Temperature { get; set; }
        public int Sunlight { get; set; }        // 0-100%
        public double WindSpeed { get; set; }
    }
}
