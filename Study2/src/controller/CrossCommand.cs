using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PLC.Utilities;
namespace PLC.src.controller
{
    public class CrossCommand
    {
        [CommandMethod("ExDraw")]
        public static void ExDraw()
        {
            ProgressMeter pm = new ProgressMeter();
            pm.Start("Processing...");
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
            Cross x = new Cross(data.canal);


            List<Nodes> CrossData = new List<Nodes>();
            if (isCrossOnly)
            {
                CrossData = data.canal.nodes.Where(z => z.NodesType == Nodes.NodesEnum.Cross).ToList();
            }

            pm.SetLimit(CrossData.Count());
            foreach (Nodes item in CrossData)
            {
                if (item.ValidCross)
                {
                    pm.MeterProgress();
                    DeleteBlock(item);
                    x.Draw(item);
                    x.Place(item);
                }
                else
                {
                    ed.WriteMessage("Error Cross: " + item.Patok + " Distance data is not ordered in sequence \n");
                }

            }
            pm.Stop();
            ed.Regen();
        }

        [CommandMethod("DeDraw")]
        public static void DeDraw()
        {
            ProgressMeter pm = new ProgressMeter();
            pm.Start("Processing...");
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
            Cross x = new Cross(data.canal);

            List<Nodes> CrossData = new List<Nodes>();
            if (isCrossOnly)
            {
                CrossData = data.canal.nodes.Where(z => z.NodesType == Nodes.NodesEnum.Cross).ToList();
            }

            pm.SetLimit(CrossData.Count());
            foreach (Nodes item in CrossData)
            {
                if (item.ValidCross)
                {
                    pm.MeterProgress();


                    Point3d CENTER_DESGIN_POINT = item.AsPoint2d;
                    Point3d design1 = new Point3d(CENTER_DESGIN_POINT.X - 10, CENTER_DESGIN_POINT.Y + 1, 0);
                    Point3d design2 = new Point3d(CENTER_DESGIN_POINT.X + 10, CENTER_DESGIN_POINT.Y + 1, 0);
                    Point3d design3 = new Point3d(CENTER_DESGIN_POINT.X, CENTER_DESGIN_POINT.Y, 0);

                    Dictionary<string, Point3d> DummyDesign = new Dictionary<string, Point3d>()
                    {
                        { "RightWall", design2 },
                        { "LeftWall", design1 },
                        { "B10", design3 }
                    };

                    Structure y = new Structure(DummyDesign);

                    x.DrawStructure(item, y, false, EarthWorks.Both);

                }
                else
                {
                    ed.WriteMessage("Error Cross: " + item.Patok + " Distance data is not ordered in sequence \n");
                }

            }
            

            pm.Stop();
            ed.Regen();
        }



        [CommandMethod("EW")]
        public static void EW()
        {
            ProgressMeter pm = new ProgressMeter();
            pm.Start("Processing...");
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
            Cross x = new Cross(data.canal);

            List<Nodes> CrossData = new List<Nodes>();
            if (isCrossOnly)
            {
                CrossData = data.canal.nodes.Where(z => z.NodesType == Nodes.NodesEnum.Cross).ToList();
            }

            pm.SetLimit(CrossData.Count());
            foreach (Nodes item in CrossData)
            {
                if (item.ValidCross && item.Patok == "P.2")
                {
                    pm.MeterProgress();


                    Point3d CENTER_DESGIN_POINT = item.AsPoint2d;
                    Point3d design1 = new Point3d(CENTER_DESGIN_POINT.X - 10, CENTER_DESGIN_POINT.Y + 1, 0);
                    Point3d design2 = new Point3d(CENTER_DESGIN_POINT.X + 10, CENTER_DESGIN_POINT.Y + 1, 0);
                    Point3d design3 = new Point3d(CENTER_DESGIN_POINT.X, CENTER_DESGIN_POINT.Y, 0);
                    Point3d design4 = new Point3d(CENTER_DESGIN_POINT.X + 1.5, CENTER_DESGIN_POINT.Y + 2, 0);

                    Dictionary<string, Point3d> DummyDesign = new Dictionary<string, Point3d>()
                    {
                        { "RightWall", design2 },
                        { "LeftWall", design1 },
                        { "B10", design3 },
                        //{ "FillKanan", design4 }
                    };

                    Structure y = new Structure(DummyDesign);

                    DeleteBlock(item);
                    x.Draw(item);
                    x.Place(item);


                    x.DrawStructure(item, y, false, EarthWorks.Both);
                }
                else
                {
                    ed.WriteMessage("Error Cross: " + item.Patok + " Distance data is not ordered in sequence \n");
                }

            }
            pm.Stop();
            ed.Regen();
        }

    }
}
