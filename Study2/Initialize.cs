using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
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
            //x.YA(0);
            for (int i = 0; i < x.DataCollection.Count; i++)
            {
                x.Draw(i);
            }



        }
        public void Terminate()

        {

            Console.WriteLine("Cleaning up...");

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
    }
}