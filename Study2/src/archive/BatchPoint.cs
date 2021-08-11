using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Microsoft.VisualBasic.FileIO;

namespace Study2
{
    public class BatchPoint

    {
        [CommandMethod("BatchPoint")]
        public static void SelectFile()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ");
            string path = ed.GetFileNameForOpen(POFO).StringResult;
            BatchPointx(path);
        }

        private static void BatchPointx(string path)
        {
            using (TextFieldParser tfp = new TextFieldParser(path))
            {
                tfp.SetDelimiters(new string[] { "," });
                while (!tfp.EndOfData)
                {
                    string[] q = tfp.ReadFields();
                    string S = (q[0]);
                    double X = double.Parse(q[1]);
                    double Y = double.Parse(q[2]);
                    double Z = double.Parse(q[3]);
                    string Desc = q[4];
                    DrawPoint(X, Y, Z);
                    BlockAt("Test", X, Y, Z);

                }
            }
        }


        private static void BlockAt(string s, double x, double y, double z)
        {
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                //bt = refrence block yg ada di document
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    //btrr = object di dalam block bt[s]
                    using (BlockTableRecord btrr = tr.GetObject(bt[s], OpenMode.ForWrite) as BlockTableRecord)
                    {
                        // Bikin blok di koordinat
                        using (BlockReference Block = new BlockReference(new Point3d(x, y, z), bt[s]))
                        {
                            //Buka model space
                            using (BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                            {
                                btr.AppendEntity(Block);
                                tr.AddNewlyCreatedDBObject(Block, true);
                                foreach (ObjectId item in btrr)
                                {
                                    DBObject dbobj = tr.GetObject(item, OpenMode.ForWrite) as DBObject;
                                    if (dbobj is AttributeDefinition)
                                    {
                                        AttributeDefinition acAtt = dbobj as AttributeDefinition;
                                        using (AttributeReference acAttRef = new AttributeReference())
                                        {
                                            acAttRef.SetAttributeFromBlock(acAtt, Block.BlockTransform);
                                            if (acAttRef.Tag == "X")
                                            {
                                                acAttRef.TextString = x.ToString();
                                            }
                                            if (acAttRef.Tag == "Y")
                                            {
                                                acAttRef.TextString = y.ToString();
                                            }
                                            if (acAttRef.Tag == "Z")
                                            {
                                                acAttRef.TextString = z.ToString();
                                            }
                                            Block.AttributeCollection.AppendAttribute(acAttRef);
                                            tr.AddNewlyCreatedDBObject(acAttRef, true);
                                        }

                                    }
                                }
                            }
                        }
                        
                    }
                }
                tr.Commit();
            }
        }
        private static void TextAt(string S, double X, double Y, double Z)
        {
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                using (DBText t1 = new DBText())
                {
                    t1.TextString = S;
                    t1.Position = new Point3d(X, Y, Z);
                    btr.AppendEntity(t1);
                    tr.AddNewlyCreatedDBObject(t1, true);
                }
                tr.Commit();
            }
        }

        private static void DrawPoint(double X, double Y, double Z)
        {
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                using (DBPoint p = new DBPoint(new Point3d(X, Y, Z)))
                {
                    btr.AppendEntity(p);
                    tr.AddNewlyCreatedDBObject(p, true);
                }
                tr.Commit();
            }
        }
    }

}