using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using static Utilities;

public class Plan
{
    private readonly Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
    private readonly List<Data> DataCollection;
    public int Start { get; set; }
    public int End { get; set; }

    private Point3dCollection p3dc = new Point3dCollection();
    public Point3dCollection TanggulKiri = new Point3dCollection();
    public Point3dCollection TanggulKanan = new Point3dCollection();
    public Point3dCollection DasarKiri = new Point3dCollection();
    public Point3dCollection DasarKanan = new Point3dCollection();
    public Point3dCollection Alignment = new Point3dCollection();
    public List<Point2dCollection> CutLineData = new List<Point2dCollection>();
    public List<Point3dCollection> SurfaceData = new List<Point3dCollection>();


    public int Count => DataCollection.Count;
    public Plan(string Path)
    {
        Data d = new Data();
        DataCollection = d.GetData(Path);
    }

    public void Draw(int CrossNumber)
    {

        Data Patok = DataCollection[CrossNumber];
        Data PatokNext = CrossNumber + 1 == Count ? Patok : DataCollection[CrossNumber + 1];
        Data PatokBefore = CrossNumber == 0 ? Patok : DataCollection[CrossNumber - 1];

        bool isOnRight = Patok.Intersect.X > 0 ? true : false;

        double AngleNext = 0;
        double AngleBefore = 0;


        List<double> Elev = Patok.Elevation.ConvertAll(x => double.Parse(x));
        List<double> Dist = Patok.Distance.ConvertAll(x => double.Parse(x));
        List<string> Desc = Patok.Description;

        Point3d Point = new Point3d(Patok.KX, Patok.KY, Patok.KZ);
        Point3d PointNext = new Point3d(PatokNext.KX, PatokNext.KY, PatokNext.KZ);
        Point3d PointBefore = new Point3d(PatokBefore.KX, PatokBefore.KY, PatokBefore.KZ);



        Point3dCollection Cross = new Point3dCollection();


        double SlopeNext = GetSlope(ConvertPoint2d(PointNext), ConvertPoint2d(Point));
        double SlopeBefore = GetSlope(ConvertPoint2d(Point), ConvertPoint2d(PointBefore));

        double ReciprocalNext = GetReciprocal(SlopeNext);
        double ReciprocalBefore = GetReciprocal(SlopeBefore);
        double Reciprocal = (ReciprocalNext + ReciprocalBefore);

        AngleNext = Math.Atan(ReciprocalNext);
        AngleBefore = Math.Atan(ReciprocalBefore);

        if (double.IsNaN(AngleNext))
        {
            AngleNext = AngleBefore;
        }
        if (double.IsNaN(AngleBefore))
        {
            AngleBefore = AngleNext;
        }

        double Angle = (AngleNext + AngleBefore) / 2;

        string SQuadrant = string.Concat(BothQuadrant(PointBefore, Point, PointNext));

        switch (SQuadrant)
        {
            case ("12"):
                Angle = Angle + (3 * Math.PI / 2); // 270 degree
                break;
            case ("21"):
                Angle = Angle + (1 * Math.PI / 2); // 90 degree
                break;
            case ("31"):
                Angle = Angle + (1 * Math.PI); // 180 degree
                break;
            case ("34"):
                Angle = Angle + (1 * Math.PI / 2); // 90 degree
                break;
            case ("42"):
                Angle = Angle + (1 * Math.PI); // 180 degree
                break;
            case ("43"):
                Angle = Angle + (3 * Math.PI / 2); // 270 degree
                break;
        }

        Point3d AlignmentPoint = PolarPoints(Point, Angle, Patok.Intersect.X);
        AlignmentPoint = new Point3d(AlignmentPoint.X, AlignmentPoint.Y, Patok.Intersect.Y);

        p3dc.Add(Point);


        if (Patok.PatokSaja)
        {
            if (CrossNumber == 0)
            {
                Alignment.Add(AlignmentPoint);
            }
            else { Alignment.Add(Alignment[CrossNumber - 1]); }
        }
        else
        {
            Alignment.Add(AlignmentPoint);
        }



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

                    if (CrossNumber == End - 1)
                    {
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, Alignment, false))
                        {
                            btr.AppendEntity(PL);
                        };
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, p3dc, false))
                        {
                            btr.AppendEntity(PL);
                        };
                    }

                    using (DBText tx = new DBText())
                    {
                        tx.Position = Point;
                        tx.TextString = Patok.NamaPatok;
                        btr.AppendEntity(tx);
                    }

                    for (int i = 0; i < Dist.Count; i++)
                    {
                        using (DBText tx = new DBText())
                        {
                            tx.Position = PolarPoints(Point, Angle, Dist[i]);
                            tx.Position = new Point3d(tx.Position.X, tx.Position.Y, Elev[i]);
                            Cross.Add(tx.Position);
                            tx.TextString = Desc[i];
                            btr.AppendEntity(tx);
                            if (Patok.PatokSaja)
                            {
                                if (CrossNumber == 0)
                                {
                                    if (i == Patok.TanggulKiriIndex)
                                    {
                                        TanggulKiri.Add(tx.Position);
                                    }
                                    if (i == Patok.TanggulKananIndex)
                                    {
                                        TanggulKanan.Add(tx.Position);
                                    }
                                    if (i == Patok.DasarKiriIndex)
                                    {
                                        DasarKiri.Add(tx.Position);
                                    }
                                    if (i == Patok.DasarKananIndex)
                                    {
                                        DasarKanan.Add(tx.Position);
                                    }
                                }
                                else
                                {
                                    TanggulKiri.Add(TanggulKiri[CrossNumber - 1]);
                                    TanggulKanan.Add(TanggulKanan[CrossNumber - 1]);
                                    DasarKiri.Add(DasarKiri[CrossNumber - 1]);
                                    DasarKanan.Add(DasarKanan[CrossNumber - 1]);
                                }

                            }
                            else
                            {
                                if (i == Patok.TanggulKiriIndex)
                                {
                                    TanggulKiri.Add(tx.Position);
                                }
                                if (i == Patok.TanggulKananIndex)
                                {
                                    TanggulKanan.Add(tx.Position);
                                }
                                if (i == Patok.DasarKiriIndex)
                                {
                                    DasarKiri.Add(tx.Position);
                                }
                                if (i == Patok.DasarKananIndex)
                                {
                                    DasarKanan.Add(tx.Position);
                                }
                            }

                        }
                    }

                    if (CrossNumber == End - 1)
                    {
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, TanggulKiri, false))
                        {
                            btr.AppendEntity(PL);
                        };
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, TanggulKanan, false))
                        {
                            btr.AppendEntity(PL);
                        };
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DasarKiri, false))
                        {
                            btr.AppendEntity(PL);
                        };
                        using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DasarKanan, false))
                        {
                            btr.AppendEntity(PL);
                        };
                    }

                    //HEC-RAS 2d PolyLine
                    using (Polyline PL = new Polyline())
                    {
                        Point2d first = ConvertPoint2d(Cross[Patok.TanggulKiriIndex]);
                        Point2d second = ConvertPoint2d(Cross[Patok.TanggulKananIndex]);
                        PL.AddVertexAt(0, first, 0, 0, 0);
                        PL.AddVertexAt(1, second, 0, 0, 0);
                        btr.AppendEntity(PL);
                        Point2dCollection PointCut = new Point2dCollection
                        {
                            first,
                            GetMidPoint(first, second),
                            second
                        };
                        CutLineData.Add(PointCut);
                    };

                    //CROSS SECTION
                    using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, Cross, false))
                    {
                        btr.AppendEntity(PL);
                        List<Point3d> Mantab = new List<Point3d>();
                        foreach (Point3d item in Cross)
                        {
                            Mantab.Add(item);
                        }
                        Mantab.RemoveRange(0, Patok.TanggulKiriIndex);
                        int UpdatePatokKanan = Patok.TanggulKananIndex - Patok.TanggulKiriIndex + 1;
                        Mantab.RemoveRange(UpdatePatokKanan, Mantab.Count - UpdatePatokKanan);

                        Cross.Clear();
                        foreach (Point3d item in Mantab)
                        {
                            Cross.Add(item);
                        }
                        SurfaceData.Add(Cross);
                    };
                }
            }
            tr.Commit();
        }
    }

}

