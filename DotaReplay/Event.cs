using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDota.DotaReplay
{
    public class Event
    {
        public float Start { get; set; }
        public float End { get; set; }
        public string clock_start { get; set; }
        public string Slot { get; set; }

        public object Attackers { get; set; }


    }
}
