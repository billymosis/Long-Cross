using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using static PLC.Utilities;


namespace PLC
{
    public class Plan
    {
        private readonly Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        public int Start { get; set; }
        public int End { get; set; }


        public List<string> namaPatok = new List<string>();
        public List<string> descriptionList = new List<string>();
        public Data myData;

        public Plan(Data d)
        {
            myData = d;
        }


        public void DrawPlanCross(int index)
        {
            DataPlan DP = myData.PlanData;
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    using (BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                    {
                        using (DBText tx = new DBText())
                        {
                            tx.Position = DP.PatokPoint[index];
                            tx.TextString = DP.namaPatok[index];
                            btr.AppendEntity(tx);
                        }


                        for (int i = 0; i < DP.RAWSurfaceData[index].Count; i++)
                        {
                            using (DBText tx = new DBText())
                            {
                                tx.Position = DP.RAWSurfaceData[index][i];
                                tx.TextString = "oke"; //desc!!!
                                btr.AppendEntity(tx);

                            }
                        }

                        //HEC-RAS 2d PolyLine

                        //using (Polyline PL = new Polyline())
                        //{
                        //    Point2d coordinateTanggulKiri2D = ConvertPoint2d(Cross[Patok.TanggulKiriIndex]);
                        //    Point2d coordinateTanggulKanan2D = ConvertPoint2d(Cross[Patok.TanggulKananIndex]);
                        //    Point2d midPoint = GetMidPoint(coordinateTanggulKiri2D, coordinateTanggulKanan2D);
                        //    // OPTIONAL Point2d midPoint = ConvertPoint2d(AlignmentPoint)
                        //    PL.AddVertexAt(0, coordinateTanggulKiri2D, 0, 0, 0);
                        //    PL.AddVertexAt(1, coordinateTanggulKanan2D, 0, 0, 0);
                        //    btr.AppendEntity(PL);

                        //};

                        //CROSS SECTION
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DP.RAWSurfaceData[index], false))
                        {
                            btr.AppendEntity(PL);
                        };
                    }
                }
                tr.Commit();
            }

        }

        public void DrawPolygon()
        {
            DataPlan DP = myData.PlanData;
            //if (CrossNumber > 620)
            //{
            //    ed.WriteMessage(Patok.NamaPatok + " : " + " Angle " + CR2D(AngleBefore).ToString() + " " + CR2D(AngleNext).ToString() + " " + CR2D(Angle).ToString() + "\n");
            //    ed.WriteMessage(Patok.NamaPatok + " : " + " Slope " + SlopeBefore.ToString() + " " + SlopeNext.ToString() + "\n");
            //    ed.WriteMessage(Patok.NamaPatok + " : " + " Quadrant " + SQuadrant + "\n");
            //}

            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    using (BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                    {
                        //using (Line L = new Line())
                        //{
                        //    L.StartPoint = PolarPoints(Point, Angle, Patok.MaxDist);
                        //    L.EndPoint = PolarPoints(Point, Angle, Patok.MinDist);
                        //    L.ColorIndex = 1;
                        //    btr.AppendEntity(L);
                        //    tr.AddNewlyCreatedDBObject(L, true);
                        //}


                        //MID ALIGNMENT


                        // ALIGNMENT ADALAH KOORDINAT MID POINT
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DP.PointAlignment, false))
                        {
                            PL.ColorIndex = 1;
                            btr.AppendEntity(PL);
                        };

                        // P3DC ADALAH KOORDINAT PATOK
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DP.PatokPoint, false))
                        {
                            PL.ColorIndex = 2;
                            btr.AppendEntity(PL);
                        };

                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DP.PointTanggulKiri, false))
                        {
                            btr.AppendEntity(PL);
                        };
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DP.PointTanggulKanan, false))
                        {
                            btr.AppendEntity(PL);
                        };
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DP.PointDasarKiri, false))
                        {
                            btr.AppendEntity(PL);
                        };
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DP.PointDasarKanan, false))
                        {
                            btr.AppendEntity(PL);
                        };


                    }
                }
                tr.Commit();
            }
        }

    }

}