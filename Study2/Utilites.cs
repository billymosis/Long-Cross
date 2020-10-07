using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using AcRx = Autodesk.AutoCAD.Runtime;
using TransactionManager = Autodesk.AutoCAD.DatabaseServices.TransactionManager;


public static class Utilities
{
    public static Point2d ConvertPoint2d(Point3d p)
    {
        return new Point2d(p.X, p.Y);
    }

    public static Point3d ConvertPoint3d(Point2d p)
    {
        return new Point3d(p.X, p.Y, 0);
    }

    public static bool IsLeft(Point3d p1, Point3d p2)
    {
        return p1.X < p2.X;
    }

    public static bool IsLeft(Point2d p1, Point2d p2)
    {
        return p1.X < p2.X;
    }

    public static bool IsRight(Point3d p1, Point3d p2)
    {
        return p1.X > p2.X;
    }

    public static bool IsRight(Point2d p1, Point2d p2)
    {
        return p1.X > p2.X;
    }

    public static Point2d FindIntersection2d(Point3d s1, Point3d e1, Point3d s2, Point3d e2)
    {
        double a1 = e1.Y - s1.Y;
        double b1 = s1.X - e1.X;
        double c1 = a1 * s1.X + b1 * s1.Y;

        double a2 = e2.Y - s2.Y;
        double b2 = s2.X - e2.X;
        double c2 = a2 * s2.X + b2 * s2.Y;

        double delta = a1 * b2 - a2 * b1;
        //If lines are parallel, the result will be (NaN, NaN).
        return delta == 0 ? new Point2d(double.NaN, double.NaN)
            : new Point2d((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
    }

    public static Point2d FindIntersection2d(Point2d s1, Point2d e1, Point2d s2, Point2d e2)
    {
        double a1 = e1.Y - s1.Y;
        double b1 = s1.X - e1.X;
        double c1 = a1 * s1.X + b1 * s1.Y;

        double a2 = e2.Y - s2.Y;
        double b2 = s2.X - e2.X;
        double c2 = a2 * s2.X + b2 * s2.Y;

        double delta = a1 * b2 - a2 * b1;
        //If lines are parallel, the result will be (NaN, NaN).
        return delta == 0 ? new Point2d(double.NaN, double.NaN)
            : new Point2d((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
    }

    public static int ClosestTo(this IEnumerable<int> collection, int target)
    {
        // NB Method will return int.MaxValue for a sequence containing no elements.
        // Apply any defensive coding here as necessary.
        var closest = int.MaxValue;
        var minDifference = int.MaxValue;
        foreach (var element in collection)
        {
            var difference = Math.Abs((long)element - target);
            if (minDifference > difference)
            {
                minDifference = (int)difference;
                closest = element;
            }
        }

        return closest;
    }

    public static double CR2D(double radians)
    {
        return (180 / Math.PI) * radians;
    }

    public static Point2d PolarPoints(Point2d pPt, double dAng, double dDist)
    {
        return new Point2d(pPt.X + dDist * Math.Cos(dAng),
                             pPt.Y + dDist * Math.Sin(dAng));
    }
    public static Point3d PolarPoints(Point3d pPt, double dAng, double dDist)
    {
        return new Point3d(pPt.X + dDist * Math.Cos(dAng),
                             pPt.Y + dDist * Math.Sin(dAng),
                             pPt.Z);
    }

}





