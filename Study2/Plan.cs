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
    private readonly int tempX;
    private readonly List<Data> DataCollection;
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

        List<double> Elev = Patok.Elevation.ConvertAll(x => double.Parse(x));
        List<double> Dist = Patok.Distance.ConvertAll(x => double.Parse(x));
        List<string> Desc = Patok.Description;

        Point3d Point = new Point3d(Patok.KX, Patok.KY, Patok.KZ);
        Point3d PointNext = new Point3d(PatokNext.KX, PatokNext.KY, PatokNext.KZ);
        Point3d PointBefore = new Point3d(PatokBefore.KX, PatokBefore.KY, PatokBefore.KZ);

        double SlopeNext = GetSlope(ConvertPoint2d(PointNext), ConvertPoint2d(Point));
        double SlopeBefore = GetSlope(ConvertPoint2d(Point), ConvertPoint2d(PointBefore));

        double ReciprocalNext = GetReciprocal(SlopeNext);
        double ReciprocalBefore = GetReciprocal(SlopeBefore);
        double Reciprocal = (ReciprocalNext + ReciprocalBefore);

        double AngleNext = (Math.Atan(ReciprocalNext));
        double AngleBefore = double.IsNaN(Math.Atan(ReciprocalBefore)) ? AngleNext : Math.Atan(ReciprocalBefore);
        double Angle = (Math.Abs(AngleNext) + Math.Abs(AngleBefore)) / 2;
        //if ((AngleNext > 0 && AngleBefore > 0) || (AngleNext < 0 && AngleBefore < 0))
        //{

        //}
        //else
        //{
        //    Angle = (AngleBefore);
        //}



        ed.WriteMessage(Patok.NamaPatok + " : " + " Angle " + CR2D(AngleBefore).ToString() + " " + CR2D(AngleNext).ToString() + " " + CR2D(Angle).ToString() + "\n");
        using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
        {
            using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable)
            {
                using (BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    using (Line L = new Line())
                    {
                        L.StartPoint = Point;
                        L.EndPoint = PointNext;

                        btr.AppendEntity(L);
                        tr.AddNewlyCreatedDBObject(L, true);
                    }

                    using (DBText tx = new DBText())
                    {
                        tx.Position = Point;
                        tx.TextString = Patok.NamaPatok;
                        btr.AppendEntity(tx);
                    }

                    using (Line L = new Line())
                    {
                        L.StartPoint = PolarPoints(Point, Angle, 5);
                        L.EndPoint = PolarPoints(Point, Angle, -5);
                        L.ColorIndex = 1;
                        btr.AppendEntity(L);
                        tr.AddNewlyCreatedDBObject(L, true);
                    }

                    for (int i = 0; i < Dist.Count; i++)
                    {
                        using (DBText tx = new DBText())
                        {
                            tx.Position = PolarPoints(Point, Angle, Dist[i]);
                            tx.TextString = Desc[i];
                            btr.AppendEntity(tx);
                        }
                    }
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
}

