using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;



namespace DumpEntityProperties
{
    public class Commands
    {
        [CommandMethod("Dump")]
        public void Dump()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            var entRes = ed.GetEntity("\nSelect object: ");
            if (entRes.Status == PromptStatus.OK)
            {
                PrintDump(entRes.ObjectId, ed);

            }
        }

        private void PrintDump(ObjectId id, Editor ed)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            using (var tr = id.Database.TransactionManager.StartTransaction())
            {
                var dbObj = tr.GetObject(id, OpenMode.ForRead);
                var types = new List<Type>
                {
                    dbObj.GetType()
                };
                while (true)
                {
                    var type = types[0].BaseType;
                    types.Insert(0, type);
                    if (type == typeof(RXObject))
                        break;
                }
                foreach (Type t in types)
                {
                    ed.WriteMessage($"\n\n - {t.Name} -");
                    foreach (var prop in t.GetProperties(flags))
                    {
                        ed.WriteMessage("\n{0,-40}: ", prop.Name);
                        try
                        {
                            ed.WriteMessage("{0}", prop.GetValue(dbObj, null));
                        }
                        catch (System.Exception e)
                        {
                            ed.WriteMessage(e.Message);
                        }
                    }
                }
                tr.Commit();
            }
        }
    }
}