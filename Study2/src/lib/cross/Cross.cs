using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static PLC.Utilities;




namespace PLC
{


    public class Cross
    {
        private Canal data;
        public Cross(Canal data)
        {
            this.data = data;

        }
    }
}