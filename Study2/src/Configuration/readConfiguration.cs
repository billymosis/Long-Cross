using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;
using System.IO;

namespace PLC.src.Configuration
{
    internal class readConfiguration
    {
        const string CONFIG_PATH = @"C:\config.toml";
        void read ()
        {
            string fileName = CONFIG_PATH;
            string config = File.ReadAllText(fileName);

            var model = Toml.ToModel(config);


        }
    }
}
