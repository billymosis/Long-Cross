﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace LongCross
{
    public class Cross : Data

    {
        
        public Cross(string Path)
        {
            GetData(Path);
        }
                          
        public void Draw(int CrossNumber)
        {
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForWrite) as BlockTable;

                using (BlockTableRecord btr = new BlockTableRecord())
                {
                    if (!bt.Has(CrossNumber.ToString()))
                    {
                        List<double> Elev = DataCollection[CrossNumber].Elevation.ConvertAll(x => double.Parse(x));
                        List<double> Dist = DataCollection[CrossNumber].Distance.ConvertAll(x => double.Parse(x));
                        List<string> Desc = DataCollection[CrossNumber].Description;
                        int limit = Elev.Count;
                        int ip = Dist.IndexOf(0);
                        bt.Add(btr);
                        btr.Name = CrossNumber.ToString();
                        for (int i = 0; i < limit; i++)
                        {
                            if (!(i == limit - 1))
                            {
                                using (Line Jalan = new Line())
                                {
                                    Jalan.StartPoint = new Point3d(Dist[i], Elev[i], 0);
                                    Jalan.EndPoint = new Point3d(Dist[i + 1], Elev[i + 1], 0);

                                    if (Desc[i].Contains("J") && Desc[i + 1].Contains("J"))
                                    {
                                        Jalan.ColorIndex = 1;
                                    }

                                    if (Desc[i].Contains("L") && Desc[i + 1].Contains("L"))
                                    {
                                        Jalan.ColorIndex = 2;
                                    }
                                    btr.AppendEntity(Jalan);
                                }

                            }

                            using (DBText tx = new DBText())
                            {
                                tx.Position = new Point3d(Dist[i], Elev[i], 0);
                                tx.TextString = Desc[i];
                                btr.AppendEntity(tx);
                            }

                        }

                        //MID POINT
                        if (!(Desc.FindIndex(s => s.Contains("D")) == -1))
                        {
                            //ds = dasar start
                            //de = dasar end
                            int ds = Desc.FindIndex(s => s.Contains("D"));
                            int de = Desc.FindIndex(ds + 1, s => s.Contains("D"));
                            Vector3d v = new Point3d(Dist[ds], Elev[ds], 0).GetVectorTo(new Point3d(Dist[de], Elev[de], 0));
                            Point3d mid = new Point3d(Dist[ds], Elev[ds], 0) + v * 0.5;
                            using (Line CL = new Line())
                            {
                                CL.StartPoint = mid;
                                CL.EndPoint = new Point3d(mid.X, mid.Y - 5, 0);
                                btr.AppendEntity(CL);
                            }
                            btr.Origin = new Point3d(mid.X, mid.Y, 0);
                        }


                        using (BlockReference brf = new BlockReference(new Point3d(Dist[ip], Elev[ip], 0), bt["Patok"]))
                        {
                            btr.AppendEntity(brf);
                        }


                        tr.AddNewlyCreatedDBObject(btr, true);
                    }

                    tr.Commit();
                }

            }


        }
    }
}





