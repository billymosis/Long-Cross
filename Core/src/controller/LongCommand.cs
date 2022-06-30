using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC.src.controller
{
    public class LongCommand
    {
        [CommandMethod("LongDraw")]
        public static void LongDraw()
        {
            const bool isCrossOnly = true;
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
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
            Long x = new Long(data.canal);
            x.DrawLong();
            ed.Regen();

        }
    }
}
