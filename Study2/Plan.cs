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

    private Point3dCollection p3dc;
    private Point3dCollection TanggulKiri;
    private Point3dCollection TanggulKanan;
    private Point3dCollection DasarKiri;
    private Point3dCollection DasarKanan;

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

        double AngleNext = (Math.Atan(ReciprocalNext));
        double AngleBefore = double.IsNaN(Math.Atan(ReciprocalBefore)) ? AngleNext : Math.Atan(ReciprocalBefore);

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




        //ed.WriteMessage(Patok.NamaPatok + " : " + " Angle " + CR2D(AngleBefore).ToString() + " " + CR2D(AngleNext).ToString() + " " + CR2D(Angle).ToString() + "\n");
        //ed.WriteMessage(Patok.NamaPatok + " : " + " Slope " +SlopeBefore.ToString() + " " + SlopeNext.ToString() + "\n");
        //ed.WriteMessage(Patok.NamaPatok + " : " + " Quadrant " + SQuadrant + "\n");
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
                    if (p3dc == null)
                    {
                        p3dc = new Point3dCollection
                        {
                            Point
                        };
                    }
                    if (CrossNumber != Start && CrossNumber != End)
                    {
                        p3dc.Add(Point);
                    }
                    if (CrossNumber == End - 1)
                    {
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
                            if (i == Patok.TanggulKiriIndex)
                            {
                                if (TanggulKiri == null)
                                {
                                    TanggulKiri = new Point3dCollection
                                    {
                                        tx.Position
                                    };
                                }
                                if (CrossNumber != Start && CrossNumber != End)
                                {

                                    TanggulKiri.Add(tx.Position);

                                }
                                if (CrossNumber == End - 1)
                                {
                                    using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, TanggulKiri, false))
                                    {
                                        btr.AppendEntity(PL);
                                    };
                                }
                            }
                            if (i == Patok.TanggulKananIndex)
                            {
                                if (TanggulKanan == null)
                                {
                                    TanggulKanan = new Point3dCollection
                                    {
                                        tx.Position
                                    };
                                }
                                if (CrossNumber != Start && CrossNumber != End)
                                {
                                    if (Elev[i] == Patok.TanggulKanan)
                                    {
                                        TanggulKanan.Add(tx.Position);
                                    }
                                }
                                if (CrossNumber == End - 1)
                                {
                                    using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, TanggulKanan, false))
                                    {
                                        btr.AppendEntity(PL);
                                    };
                                }
                            }
                            if (i == Patok.DasarKiriIndex)
                            {
                                if (DasarKiri == null)
                                {
                                    DasarKiri = new Point3dCollection
                                    {
                                        tx.Position
                                    };
                                }
                                if (CrossNumber != Start && CrossNumber != End)
                                {
                                    if (Elev[i] == Patok.DasarKiri)
                                    {
                                        DasarKiri.Add(tx.Position);
                                    }
                                }
                                if (CrossNumber == End - 1)
                                {
                                    using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DasarKiri, false))
                                    {
                                        btr.AppendEntity(PL);
                                    };
                                }
                            }
                            if (i == Patok.DasarKananIndex)
                            {
                                if (DasarKanan == null)
                                {
                                    DasarKanan = new Point3dCollection
                                    {
                                        tx.Position
                                    };
                                }
                                if (CrossNumber != Start && CrossNumber != End)
                                {
                                    if (Elev[i] == Patok.DasarKanan)
                                    {
                                        DasarKanan.Add(tx.Position);
                                    }
                                }
                                if (CrossNumber == End - 1)
                                {
                                    using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, DasarKanan, false))
                                    {
                                        btr.AppendEntity(PL);
                                    };
                                }
                            }
                        }
                    }

                    using (Polyline PL = new Polyline())
                    {


                        PL.AddVertexAt(0, ConvertPoint2d(Cross[Patok.TanggulKiriIndex]), 0, 0, 0);
                        PL.AddVertexAt(1, ConvertPoint2d(Cross[Patok.TanggulKananIndex]), 0, 0, 0);


                        btr.AppendEntity(PL);
                    };

                    using (Polyline3d PL = new Polyline3d(Poly3dType.SimplePoly, Cross, false))
                    {
                        btr.AppendEntity(PL);
                    };
                }
            }
            tr.Commit();
        }
    }

    private double GetSlope(Point2d first, Point2d second)
    {
        return (second.Y - first.Y) / (second.X - first.X);
    }

    private double GetReciprocal(double value)
    {
        return (1 / value) * -1;
    }

    private double GetConstant(double X, double Y, double Reciprocal)
    {
        return Y - (Reciprocal * X);
    }

    private int GetQuadrant(Point3d first, Point3d second)
    {
        int Quadrant = 0;
        if (second.X > first.X && second.Y > first.Y)
        {
            Quadrant = 1;
        }
        else if (second.X < first.X && second.Y > first.Y)
        {
            Quadrant = 2;
        }
        else if (second.X < first.X && second.Y < first.Y)
        {
            Quadrant = 3;
        }
        else if (second.X > first.X && second.Y < first.Y)
        {
            Quadrant = 4;
        }

        return Quadrant;
    }

    private int[] BothQuadrant(Point3d Before, Point3d Now, Point3d Next)
    {
        int[] array = new int[] { GetQuadrant(Before, Now), GetQuadrant(Next, Now) };
        return array;
    }

}

