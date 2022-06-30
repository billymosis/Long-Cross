using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PLC.Utilities;

namespace PLC
{
    public class Structure
    {
        public Dictionary<string, Point3d> Design = new Dictionary<string, Point3d>();
        public Dictionary<Region, Point3d> structureDictionary = new Dictionary<Region, Point3d>();
        public bool useBlock = false;

        public Structure(Dictionary<string, Point3d> design)
        {
            useBlock = true;
            Design = design;
            structureDictionary = GetStructure(design);
        }

        public Structure(Dictionary<Region, Point3d> structureDictionary)
        {
            useBlock = false;
            this.structureDictionary = structureDictionary;
        }

        private static Dictionary<Region, Point3d> GetStructure(Dictionary<string, Point3d> Design)
        {
            Dictionary<Region, Point3d> structureDictionary = new Dictionary<Region, Point3d>();
            foreach (KeyValuePair<string, Point3d> item in Design)
            {
                structureDictionary.Add(GetBlockContent(item.Key).QOpenForRead<Entity>().Where(x => x.GetType() == typeof(Region)).First().Clone() as Region, item.Value);
            }
            return structureDictionary;
        }


    }
}
