using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using static PLC.Utilities;

namespace PLC
{
    public class NextFeature
    {
        [CommandMethod("GETTIME")]
        public static void GETTIME()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            DateTime result = GetNistTime();
            ed.WriteMessage(result.ToString());
        }

        [CommandMethod("GETPATH")]
        public static void GETPATH()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            string s = GetDllPath();
            ed.WriteMessage(s);
        }

        [CommandMethod("WRITETEXT")]
        public static void WRITETEXT()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
        }

        // UNTUK ARSIRAN TANPA HATCH

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
                    BlockTableRecord curSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
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

        // UNTUK DETECT OBJECT DI KOORDINAT TERTENTU

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

        // UNTUK CHECK OBJEK DENGAN FENCE SELECTION

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

        // CHECK INTERSECTION COORDINATE BETWEEN 2 LINES

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
    }
}
