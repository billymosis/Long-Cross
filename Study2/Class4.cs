using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Study2
{

    public class BatchPL

    {
        [CommandMethod("qw")]
        public static void QW()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross.csv";
            PromptIntegerResult x = ed.GetInteger("Choose which cross to draw :");
            CreateCross(s, x.Value);

        }

        private static void CreateCross(string path, int CrossN)
        {
            List<string> Elev = new List<string>();
            List<string> Dist = new List<string>();
            List<string> Desc = new List<string>();
            char[] LineSeparator = new char[] { '\r', '\n' };
            char[] charSeparators = new char[] { ',' };
            using (StreamReader reader = new StreamReader(path))
            {
                for (int i = 0; i < CrossN * 3; i++)
                {
                    reader.ReadLine();
                }
                while (!reader.EndOfStream)
                {

                    string line1 = reader.ReadLine();
                    string[] val1 = line1.Split(charSeparators);
                    foreach (string item in val1)
                    {
                        Elev.Add(item);
                    }
                    string line2 = reader.ReadLine();
                    string[] val2 = line2.Split(charSeparators);
                    foreach (string item in val2)
                    {
                        Dist.Add(item);
                    }
                    string line3 = reader.ReadLine();
                    string[] val3 = line3.Split(charSeparators);
                    foreach (string item in val3)
                    {
                        Desc.Add(item);
                    }
                    break;


                }
            }
            Polyline PX = CreatePL(Elev, Dist, Desc);
            PX.Dispose();

        }



        private static Polyline CreatePL(List<string> Elev, List<string> Dist, List<string> Desc)
        {
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    List<double> DElev = new List<double>();
                    List<double> DDistance = new List<double>();

                    int limit = Elev.IndexOf("");


                    Desc.RemoveRange(limit, Desc.Count - limit);
                    Polyline PL = new Polyline();
                    DBObjectCollection dboj = new DBObjectCollection();
                    for (int i = 0; i < limit; i++)
                    {
                        DDistance.Add(double.Parse(Dist[i]));
                        DElev.Add(double.Parse(Elev[i]));
                        PL.AddVertexAt(i, new Point2d(DDistance[i], DElev[i]), 0, 0, 0);
                        
                        using (DBText tx = new DBText())
                        {
                            tx.Position = new Point3d(DDistance[i], DElev[i], 0);
                            tx.TextString = Desc[i];
                            btr.AppendEntity(tx);
                            tr.AddNewlyCreatedDBObject(tx, true);
                        }


                    }
                    btr.AppendEntity(PL);
                    tr.AddNewlyCreatedDBObject(PL, true);
                    tr.Commit();
                    return PL;
                }

            }

        }

    }
}