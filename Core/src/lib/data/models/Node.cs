using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC
{
    public class Node
    {
        public Point3d Point3d = new Point3d();
        public Point3d Point2d = new Point3d();
        public double X { set => Point3d = new Point3d(value, Point3d.Y, Point3d.Z); }
        public double Y { set => Point3d = new Point3d(Point3d.X, value, Point3d.Z); }
        public double Z { set => Point3d = new Point3d(Point3d.X, Point3d.Y, value); }
        private double distance;
        private double elevation;
        public double Distance
        {
            get => distance;
            set
            {
                Point2d = new Point3d(value, Point2d.Y, 0);
                distance = value;
            }
        }
        public double Elevation
        {
            get => elevation;
            set
            {
                Point2d = new Point3d(Point2d.X, value, 0);
                elevation = value;
            }
        }
        public string Description;
        public Node(double elevation, double distance, string description)
        {
            Elevation = elevation;
            Distance = distance;
            Description = description;
        }
    }
}
