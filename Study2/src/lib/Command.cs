using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using static PLC.Utilities;

namespace PLC
{

    public class Command
    {

        [CommandMethod("ipLine")]
        public void IntersectTestIn()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                PromptEntityOptions peo = new PromptEntityOptions("\nSelect a Line >>");
                peo.SetRejectMessage("\nYou have to select the Line only >>");
                peo.AddAllowedClass(typeof(Line), false);
                PromptEntityResult res;
                res = ed.GetEntity(peo);
                if (res.Status != PromptStatus.OK)
                    return;
                Entity ent = (Entity)tr.GetObject(res.ObjectId, OpenMode.ForRead);
                if (ent == null)
                    return;
                Line line = (Line)ent as Line;
                if (line == null) return;
                peo = new PromptEntityOptions("\nSelect a Rectangle: ");
                peo.SetRejectMessage("\nYou have to select the Polyline only >>");
                peo.AddAllowedClass(typeof(Polyline), false);
                res = ed.GetEntity("\nSelect a Rectangle: ");
                if (res.Status != PromptStatus.OK)
                    return;
                ent = (Entity)tr.GetObject(res.ObjectId, OpenMode.ForRead);
                if (ent == null)
                    return;
                Polyline pline = (Polyline)ent as Polyline;
                if (line == null) return;
                Point3dCollection pts = new Point3dCollection();
                pline.IntersectWith(line, Intersect.ExtendArgument, pts, IntPtr.Zero, IntPtr.Zero);
                if (pts.Count == 0) return;
                List<Point3d> points = new List<Point3d>();
                points.Add(line.StartPoint);
                points.Add(line.EndPoint);
                foreach (Point3d p in pts)
                    points.Add(p);
                DBObjectCollection objs = line.GetSplitCurves(pts);
                List<DBObject> lstobj = new List<DBObject>();
                foreach (DBObject dbo in objs)
                    lstobj.Add(dbo);
                points.Sort(delegate (Point3d p1, Point3d p2)
                {
                    return Convert.ToDouble(
                        Convert.ToDouble(line.GetParameterAtPoint(p1))).CompareTo(
                    Convert.ToDouble(line.GetParameterAtPoint(p2)));
                }
       );
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                Line ln0 = lstobj[1] as Line;// middle
                Line ln1 = new Line(points[0], points[1]);
                Line ln2 = new Line(points[2], points[3]);
                if (!ln0.IsWriteEnabled)
                    ln0.UpgradeOpen();
                ln0.Dispose();

                if (!ln1.IsWriteEnabled)
                    ln1.UpgradeOpen();
                btr.AppendEntity(ln1);
                tr.AddNewlyCreatedDBObject(ln1, true);
                if (!ln2.IsWriteEnabled)
                    ln2.UpgradeOpen();

                btr.AppendEntity(ln2);
                tr.AddNewlyCreatedDBObject(ln2, true);
                if (!line.IsWriteEnabled)
                    line.UpgradeOpen();
                line.Erase();
                line.Dispose();
                ed.Regen();
                tr.Commit();
            }
        }
        [CommandMethod("opLine")]
        public void IntersectTest()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                PromptEntityOptions peo = new PromptEntityOptions("\nSelect a Line >>");
                peo.SetRejectMessage("\nYou have to select the Line only >>");
                peo.AddAllowedClass(typeof(Line), false);
                PromptEntityResult res;
                res = ed.GetEntity(peo);
                if (res.Status != PromptStatus.OK)
                    return;
                Entity ent = (Entity)tr.GetObject(res.ObjectId, OpenMode.ForRead);
                if (ent == null)
                    return;
                Line line = (Line)ent as Line;
                if (line == null) return;
                peo = new PromptEntityOptions("\nSelect a Rectangle: ");
                peo.SetRejectMessage("\nYou have to select the Polyline only >>");
                peo.AddAllowedClass(typeof(Polyline), false);
                res = ed.GetEntity("\nSelect a Rectangle: ");
                if (res.Status != PromptStatus.OK)
                    return;
                ent = (Entity)tr.GetObject(res.ObjectId, OpenMode.ForRead);
                if (ent == null)
                    return;
                Polyline pline = (Polyline)ent as Polyline;
                if (line == null) return;
                Point3dCollection pts = new Point3dCollection();
                pline.IntersectWith(line, Intersect.ExtendArgument, pts, IntPtr.Zero, IntPtr.Zero);
                if (pts.Count == 0) return;
                DBObjectCollection lstobj = line.GetSplitCurves(pts);

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                Line ln0 = lstobj[1] as Line;// middle
                Line ln1 = lstobj[0] as Line;

                Line ln2 = lstobj[2] as Line;
                if (!ln0.IsWriteEnabled)
                    ln0.UpgradeOpen();
                btr.AppendEntity(ln0);
                tr.AddNewlyCreatedDBObject(ln0, true);
                if (!ln1.IsWriteEnabled)
                    ln1.UpgradeOpen();
                ln1.Dispose();

                if (!ln2.IsWriteEnabled)
                    ln2.UpgradeOpen();
                ln2.Dispose();

                if (!line.IsWriteEnabled)
                    line.UpgradeOpen();
                line.Erase();
                line.Dispose();
                ed.Regen();
                tr.Commit();
            }
        }

        /// <summary>
        /// Switch Trial and Full Licensed Mode
        /// </summary>
        [CommandMethod("XX")]
        public static void XX()
        {
            if (Global.Licensed)
            {
                Global.Licensed = false;
            }
            else
            {
                Global.Licensed = true;
            }
        }

        [CommandMethod("BUBBLE")]
        public static void Bubble()
        {
            Utilities.Bubble("Test", "This is Bubble");
        }

        private static void Bw_Closed(object sender, TrayItemBubbleWindowClosedEventArgs e)
        {
            TrayItemBubbleWindow x = sender as TrayItemBubbleWindow;
            x.Dispose();
        }

        [CommandMethod("CABOUT")]
        public static void CABOUT()
        {
            GUI.About ab = new GUI.About(Global.CheckLicense);
            Application.ShowModalWindow(Application.MainWindow.Handle, ab, false);
            Initialization init = new Initialization();
        }

        [CommandMethod("CX")]
        public static void CX()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            ed.WriteMessage(string.Join(Environment.NewLine, PLC.Licensing.TrialData.ReadJSON()));
        }

        [CommandMethod("CZ")]
        public static void CZ()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            Global.counter.value = 50;
            ed.WriteMessage($"Global : {Global.counter.ToString()}");
        }

        [CommandMethod("AW")]
        public static void AW()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross4.csv";
            Data d = new Data(s);
            Plan x = new Plan(d);

            x.DrawPolygon();
            for (int i = 0; i < d.TotalDataLine; i++)
            {
                x.DrawPlanCross(i);
            }
            HecRAS hec = new HecRAS(d.DataPlan);
            List<XYZ> records = new List<XYZ>();
            int k = 0;
            foreach (Point3dCollection items in d.DataPlan.RAWSurfaceData)
            {
                int j = 0;
                foreach (Point3d item in items)
                {
                    if (k < d.TotalDataLine)
                    {
                        records.Add(new XYZ { Id = j, Name = d.DataPlan.namaPatok[k], Description = d.DataPlan.descriptionList[k][j], X = item.X, Y = item.Y, Z = item.Z });
                        j++;
                    }
                }
                k++;
            }
            using (StreamWriter writer = new StreamWriter(GetDllPath() + @"\" + "XYZ.csv", false))
            using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
            WriteFile(hec.GEORAS, "oke.geo");
        }

        public class XYZ
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }


        [CommandMethod("DF")]
        public static void Df()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;


            OpenFileDialog OFD = new OpenFileDialog("Select Cross File", null, "csv; txt", "SelectCross", OpenFileDialog.OpenFileDialogFlags.AllowMultiple);

            OFD.ShowDialog();
            string[] fileList = OFD.GetFilenames();
            foreach (string path in fileList)
            {
                string s = path;
                string directoryFolder = Path.GetDirectoryName(path);
                string fileName = Path.GetFileNameWithoutExtension(path);
                Data d = new Data(s);
                Plan x = new Plan(d);
                x.DrawPolygon();
                for (int i = 0; i < d.TotalCrossNumber; i++)
                {
                    x.DrawPlanCross(i);
                }
            }

        }

        [CommandMethod("PlanDraw")]
        public static void PlanDraw()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ")
            {
                Filter = "Cross File (*.csv)|*.csv|" + "Cross File TXT Tab Delimeted(*.txt)|*.txt"
            };
            string path = ed.GetFileNameForOpen(POFO).StringResult;
            string s = path;
            string directoryFolder = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            Data d = new Data(s);
            Plan x = new Plan(d);
            x.DrawPolygon();
            for (int i = 0; i < d.TotalCrossNumber; i++)
            {
                //if (Global.Licensed)
                //{
                //    x.DrawPlanCross(i);
                //}
                //else
                //{
                //    if (Global.counter.value > 0)
                //    {
                //        x.DrawPlanCross(i);
                //        Global.AddCounter(-1);
                //    }
                //    else
                //    {
                //        Utilities.Bubble("Limit",$"You have reached your limit, trial will be reseted tomorrow at {DateTime.Now.AddDays(1).ToShortDateString()}.");
                //    }
                //}
                x.DrawPlanCross(i);

            }

            HecRAS hec = new HecRAS(d.DataPlan);

            List<XYZ> records = new List<XYZ>();
            int k = 0;
            foreach (Point3dCollection items in d.DataPlan.RAWSurfaceData)
            {
                int j = 0;
                foreach (Point3d item in items)
                {
                    if (k < d.TotalCrossNumber)
                    {
                        records.Add(new XYZ { Id = j, Name = d.DataPlan.namaPatok[k], Description = d.DataPlan.descriptionList[k][j], X = item.X, Y = item.Y, Z = item.Z });
                        j++;
                    }
                }
                k++;
            }
            using (StreamWriter writer = new StreamWriter(directoryFolder + @"\" + fileName + "_XYZ.csv", false))
            using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
            WriteFile(hec.GEORAS, directoryFolder + @"\" + fileName + ".geo");
        }




    }
}
