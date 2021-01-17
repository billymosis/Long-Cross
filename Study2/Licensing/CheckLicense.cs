using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.Licensing
{
    class CheckLicense
    {
        public bool Licensed {get; set;}
        public string FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public CheckLicense()
        {

        }
    }
}
