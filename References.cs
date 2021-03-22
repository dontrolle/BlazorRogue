using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorRogue
{
    public static class References
    {
        public static Map Map { get; internal set; }
        public static Configuration Configuration { get; internal set; }
        public static SoundManager SoundManager { get; internal set; }
    }
}
