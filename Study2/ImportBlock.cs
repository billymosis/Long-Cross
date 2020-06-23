using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace Study2
{
    public class ImportBlock
    {
        [CommandMethod("ImportBlock")]
        public void ImportBlockx()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ");
            db.Insunits = UnitsValue.Millimeters;
            string path = ed.GetFileNameForOpen(POFO).StringResult;
            using (Database dbs = new Database(false, true))
            {
                dbs.ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, true, "");
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
