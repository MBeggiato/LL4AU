using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LL4AU {
    public class Settings {
        public string Version { get; set; }
        public string Token { get; set; }

        public Settings() {
            Version = "1.0.0";
            Token = "<Your Token>";
        }
    }
}
