using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

namespace Study2
{
    public class Class1
    {
        [CommandMethod("QW")]
        public void QW()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            Editor ed = acDoc.Editor;


            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId,
                                                OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                OpenMode.ForWrite) as BlockTableRecord;
                Circle c = new Circle
                {
                    Radius = 2,
                    Center = new Point3d(5, 2, 0)
                };
                var ent = ed.GetEntity("\nselect");
                acBlkTblRec.AppendEntity(c);
                acTrans.AddNewlyCreatedDBObject(c, true);
                acDoc.Editor.WriteMessage("eyyo univ check");

                // Create a circle that is at 2,3 with a radius of 4.25
                using (Circle acCirc = new Circle())
                {
                    acCirc.Center = new Point3d(2, 3, 0);
                    acCirc.Radius = 4.25;
                   

                    // Add the new object to the block table record and the transaction
                    acBlkTblRec.AppendEntity(acCirc);
                    acTrans.AddNewlyCreatedDBObject(acCirc, true);
                }

                // Save the new object to the database
                acTrans.Commit();
            }
        }
    }
}
