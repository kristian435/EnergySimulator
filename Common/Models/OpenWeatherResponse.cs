using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class OpenWeatherResponse
    {
        public Main Main { get; set; } = new();
        public Wind Wind { get; set; } = new();
        public Clouds Clouds { get; set; } = new();
    }

    public class Main { public double Temp { get; set; } }
    public class Wind { public double Speed { get; set; } }
    public class Clouds { public int All { get; set; } }
}
