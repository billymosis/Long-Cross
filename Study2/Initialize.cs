using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PLC
{
    public class Initialization : IExtensionApplication
    {
        public void Initialize()
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("Billy Plugin");
            ImportBlock();
            LoadLinetype();
        }
        [CommandMethod("AE")]
        public static void AE()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                PromptEntityOptions op = new PromptEntityOptions("\nSelect polyline :");
                op.SetRejectMessage("Must be Polyline");
                op.AddAllowedClass(typeof(Polyline), true);
                op.AllowNone = false;
                PromptEntityResult s = ed.GetEntity(op);
                if (s.Status == PromptStatus.OK)
                {
                    Polyline ent = (Polyline)tr.GetObject(s.ObjectId, OpenMode.ForRead);
                    Line drawLine = new Line();
                    double length = ent.Length;
                    double gap = length / 15.0;
                    double dis = gap;
                    //int run = ((int)gap); <- this is wrong and unusefull
                    var curSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    for (int i = 0; i < 14; i++)
                    {
                        Point3d p1 = ent.GetPointAtDist(dis);
                        Vector3d ang = ent.GetFirstDerivative(ent.GetParameterAtPoint(p1));
                        // scale the vector by 5.0 (so that it's 5 units length)
                        ang = ang.GetNormal() * 5.0;
                        // rotate the vector
                        ang = ang.TransformBy(Matrix3d.Rotation(Math.PI / 2.0, ent.Normal, Point3d.Origin));
                        // create a line by substracting and adding the vector to the point (displacing the point)
                        Line line = new Line(p1 - ang, p1 + ang);
                        curSpace.AppendEntity(line);
                        tr.AddNewlyCreatedDBObject(line, true);
                        dis = dis + gap;
                    }
                }
                tr.Commit();
            }
        }

        [CommandMethod("AW")]
        public static void AW()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross4.csv";
            Plan x = new Plan(s);
            int limit = x.Count;
            for (int i = 480; i < 500; i++)
            {
                x.Draw(i);
            }

        }

        [CommandMethod("CrossDraw")]
        public static void CrossDraw()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ");
            string path = ed.GetFileNameForOpen(POFO).StringResult;
            try
            {
                string s = path;
                Stopwatch stopwatch = new Stopwatch();
                Cross x = new Cross(s);
                ProgressMeter pm = new ProgressMeter();
                pm.Start("Processing Cross");
                pm.SetLimit(x.Count);
                int limit = x.Count;
                limit = 8;
                stopwatch.Start();
                for (int i = 0; i < limit; i++)
                {
                    x.Draw(i);
                    x.Place(i);
                    pm.MeterProgress();
                }

                if (x.crossError.Split(',').Length != 0)
                {
                    ed.WriteMessage($"\nTerdapat {x.crossError.Split(',').Length - 1} kesalahan data: {x.crossError.Remove(x.crossError.Length - 2)}" +
        $"\nProgress selesai dalam waktu {stopwatch.ElapsedMilliseconds} ms\n");
                }
                else
                {
                    ed.WriteMessage($"\nProgress selesai dalam waktu {stopwatch.ElapsedMilliseconds} ms\n");
                }

                Application.SetSystemVariable("XCLIPFRAME", 0);
                Application.SetSystemVariable("PDMODE", 3);
                pm.Stop();
                stopwatch.Stop();
                pm.Dispose();
            }
            catch (System.Exception e)
            {
                ed.WriteMessage(e.Message);
                throw;
            }
        }

        [CommandMethod("WR")]
        public static void WR()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;

            string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross4.csv";
            Stopwatch stopwatch = new Stopwatch();
            Cross x = new Cross(s);
            ProgressMeter pm = new ProgressMeter();
            pm.Start("Processing Cross");
            List<int> numbers = new List<int>() { 52,53,54,55 };
            int limit = numbers.Count;
            pm.SetLimit(limit);
            stopwatch.Start();
            for (int i = 0; i < limit; i++)
            {
                x.Draw(numbers[i]);
                x.Place(numbers[i]);
                pm.MeterProgress();
            }

            if (x.crossError != null && x.crossError.Split(',').Length != 0 )
            {
                ed.WriteMessage($"\nTerdapat {x.crossError.Split(',').Length - 1} kesalahan data: {x.crossError.Remove(x.crossError.Length - 2)}" +
    $"\nProgress selesai dalam waktu {stopwatch.ElapsedMilliseconds} ms\n");
            }
            else
            {
                ed.WriteMessage($"\nProgress selesai dalam waktu {stopwatch.ElapsedMilliseconds} ms\n");
            }

            Application.SetSystemVariable("XCLIPFRAME", 0);
            Application.SetSystemVariable("PDMODE", 3);
            pm.Stop();
            stopwatch.Stop();
            pm.Dispose();

        }


        [CommandMethod("QE")]
        public static void QE()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;

            string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross4.csv";
            Stopwatch stopwatch = new Stopwatch();
            Cross x = new Cross(s);
            ProgressMeter pm = new ProgressMeter();
            pm.Start("Processing Cross");
            pm.SetLimit(x.Count);
            int limit = x.Count;
            //limit = 8;
            stopwatch.Start();
            for (int i = 0; i < limit; i++)
            {
                x.Draw(i);
                x.Place(i);
                pm.MeterProgress();
            }

            if (x.crossError.Split(',').Length != 0)
            {
                ed.WriteMessage($"\nTerdapat {x.crossError.Split(',').Length - 1} kesalahan data: {x.crossError.Remove(x.crossError.Length - 2)}" +
    $"\nProgress selesai dalam waktu {stopwatch.ElapsedMilliseconds} ms\n");
            }
            else
            {
                ed.WriteMessage($"\nProgress selesai dalam waktu {stopwatch.ElapsedMilliseconds} ms\n");
            }

            Application.SetSystemVariable("XCLIPFRAME", 0);
            Application.SetSystemVariable("PDMODE", 3);
            pm.Stop();
            stopwatch.Stop();
            pm.Dispose();

        }

        [CommandMethod("SelectObjectsByCrossingWindow")]
        public static void SelectObjectsByCrossingWindow()
        {
            // Get the current document editor
            Editor acDocEd = Application.DocumentManager.MdiActiveDocument.Editor;

            // Create a crossing window from (2,2,0) to (10,8,0)
            PromptSelectionResult acSSPrompt;
            acSSPrompt = acDocEd.SelectCrossingWindow(new Point3d(2, 2, 0),
                                                        new Point3d(10, 8, 0));

            // If the prompt status is OK, objects were selected
            if (acSSPrompt.Status == PromptStatus.OK)
            {
                SelectionSet acSSet = acSSPrompt.Value;

                Application.ShowAlertDialog("Number of objects selected: " +
                                            acSSet.Count.ToString());
            }
            else
            {
                Application.ShowAlertDialog("Number of objects selected: 0");
            }
        }

        [CommandMethod("CheckObject")]
        public static void CheckObject()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            try
            {
                Point3dCollection points = new Point3dCollection();
                PromptPointOptions ppo = new PromptPointOptions("");
                PromptPointResult ppr = ed.GetPoint(ppo);
                ppo.BasePoint = ppr.Value;
                ppo.UseDashedLine = true;
                ppo.UseBasePoint = true;
                PromptPointResult asd = ed.GetPoint(ppo);
                points.Add(ppr.Value);
                points.Add(asd.Value);
                PromptSelectionResult prSelRes = ed.SelectFence(points);
                if (prSelRes.Status == PromptStatus.OK)
                {
                    using (SelectionSet ss = prSelRes.Value)
                    {
                        if (ss != null)
                        {
                            foreach (ObjectId item in ss.GetObjectIds())
                            {
                                ed.WriteMessage(Environment.NewLine + item.ObjectClass.DxfName.ToString());
                            }
                            ed.WriteMessage("\nThe SS is good and has {0} entities.", ss.Count);
                        }
                        else
                        {
                            ed.WriteMessage("\nThe SS is bad!");
                        }
                    }
                }
                else
                {
                    ed.WriteMessage("\nFence selection failed!");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(Environment.NewLine + ex.ToString());
            }
        }

        public static void LoadLinetype()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Linetype table for read
                LinetypeTable acLineTypTbl;
                acLineTypTbl = acTrans.GetObject(acCurDb.LinetypeTableId,
                                                    OpenMode.ForRead) as LinetypeTable;

                string sLineTypName = "DASHED";

                if (acLineTypTbl.Has(sLineTypName) == false)
                {
                    // Load the Center Linetype
                    acCurDb.LoadLineTypeFile(sLineTypName, "acad.lin");
                }

                // Save the changes and dispose of the transaction
                acTrans.Commit();
            }
        }

        [CommandMethod("INS")]
        public void InterSectionPoint()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Line pl1 = null;
            Line pl2 = null;
            Entity ent = null;
            PromptEntityOptions peo = null;
            PromptEntityResult per = null;

            using (Transaction tx = db.TransactionManager.StartTransaction())
            {
                //Select first polyline
                peo = new PromptEntityOptions("Select firtst line:");
                per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                {
                    return;
                }
                //Get the polyline entity
                ent = (Entity)tx.GetObject(per.ObjectId, OpenMode.ForRead);
                if (ent is Line)
                {
                    pl1 = ent as Line;
                }
                Point3dCollection p3d = new Point3dCollection
                {
                    pl1.StartPoint,
                    pl1.EndPoint
                };
                ed.SelectFence(p3d);
                //Select 2nd polyline
                peo = new PromptEntityOptions("\n Select Second line:");
                per = ed.GetEntity(peo);
                if (per.Status != PromptStatus.OK)
                {
                    return;
                }
                ent = (Entity)tx.GetObject(per.ObjectId, OpenMode.ForRead);
                if (ent is Line)
                {
                    pl2 = ent as Line;
                }
                Point3dCollection pts3D = new Point3dCollection();
                //Get the intersection Points between line 1 and line 2
                pl1.IntersectWith(pl2, Intersect.OnBothOperands, pts3D, IntPtr.Zero, IntPtr.Zero);
                foreach (Point3d pt in pts3D)
                {
                    // ed.WriteMessage("\n intersection point :",pt);
                    //   ed.WriteMessage("Point number: ", pt.X, pt.Y, pt.Z);
                    ed.WriteMessage($"Intersect On \n X: {pt.X.ToString()} \n Y: {pt.Y.ToString()}");

                    // Application.ShowAlertDialog("\n Intersection Point: " + "\nX = " + pt.X + "\nY = " + pt.Y + "\nZ = " + pt.Z);
                }

                tx.Commit();
            }
        }

        private void ImportBlock()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            db.Insunits = UnitsValue.Millimeters;

            using (Database dbs = new Database(false, true))
            {
                dbs.ReadDwgFile(@"E:\AutoCAD Project\Study2\Study2\data\Baloon2.dwg", FileOpenMode.OpenForReadAndAllShare, true, "");
                using (Transaction tr = dbs.TransactionManager.StartTransaction())
                {
                    using (BlockTable bt = tr.GetObject(dbs.BlockTableId, OpenMode.ForRead) as BlockTable)
                    {
                        using (ObjectIdCollection ids = new ObjectIdCollection())
                        {
                            foreach (ObjectId item in bt)
                            {
                                ids.Add(item);
                            }
                            tr.Commit();
                            db.WblockCloneObjects(ids, db.BlockTableId, new IdMapping(), DuplicateRecordCloning.Ignore, false);
                        }
                    }
                }
            }
        }

        public void Terminate()

        {
            Console.WriteLine("Cleaning up...");
        }
    }
}