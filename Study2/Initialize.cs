using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using LongCross;
using System;

namespace Study2
{
    public class Initialization : IExtensionApplication

    {

        public void Initialize()

        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("Billy Plugin");
            ImportBlock();
        }

        [CommandMethod("QE")]
        public static void QE()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross4.csv";

            Cross x = new Cross(s);
            for (int i = 0; i < x.DataCollection.Count; i++)
            {
                x.Draw(i);
                x.Place(i);
            }
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