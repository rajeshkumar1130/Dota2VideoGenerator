using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDota.DotaReplay
{
    internal class Data
    {
        public List<Event> data { get; set; } = new List<Event>();
        public List<List<Event>> data1 { get; set; } = new ();
        public bool Success { get; set; }
        public int slot { get; set; }
    }
}
