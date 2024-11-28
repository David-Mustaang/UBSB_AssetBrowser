using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UBSB_AssetBrowser
{
    public class Root
    {
        public List<Assets> assets { get; set; }
        public int lastRevision { get; set; }
    }

    public class Assets
    {
        public string username { get; set; }
        public string kuid { get; set; }
        public string sha1 { get; set; }
        public string fileId { get; set; }
        public int revision { get; set; }
    }
}
