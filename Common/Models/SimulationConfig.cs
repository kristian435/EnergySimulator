using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class SimulationConfig
    {
        public DateTime StartTime { get; set; } = DateTime.Now;
        public long Speed { get; set; } = 60;
    }
}
