using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PLC.Utilities;
namespace PLC.src.controller
{
    public class PlanCommand
    {
        [CommandMethod("PlanDraw")]
        public static void PlanDraw()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            CreateLayer("BANGUNAN", 1);
            PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ")
            {
                Filter = "Cross File (*.csv;*.txt)|*.csv;*.txt"
            };
            string path;
            path = Global.dataPath ?? ed.GetFileNameForOpen(POFO).StringResult;
            Global.dataPath = path;
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            string s = path;
            Data data = new Data(s);
            Plan x = new Plan(data.canal);

            x.DrawPlan(false);
            ed.Regen();
        }
    }
}
