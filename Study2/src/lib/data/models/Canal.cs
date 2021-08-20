using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PLC.Utilities;

namespace PLC
{
    public class Canal
    {
        public LinkedList<Nodes> nodes = new LinkedList<Nodes>();
        public Dictionary<Point3d, double> AlignmentDictionary = new Dictionary<Point3d, double>();
        public Point3dCollection PolygonNodes = new Point3dCollection();
        public string saluran;
        public Canal()
        {
        }
        public void AddNodes(Nodes _nodes)
        {
            nodes.AddLast(_nodes);
        }

        public void AngleCalculate()
        {
            foreach (Nodes current in nodes)
            {

                LinkedListNode<Nodes> currentNodes = nodes.Find(current);
                Nodes next = currentNodes.Next == null ? current : currentNodes.Next.Value;
                Nodes previous = currentNodes.Previous == null ? current : currentNodes.Previous.Value;
                double slopeNext = Slope(next, current);
                double slopePrevious = Slope(current, previous);
                double angleNext = Math.Atan(Reciprocal(slopeNext));
                double angleBefore = Math.Atan(Reciprocal(slopePrevious));

                current.station = current.PointPatok.DistanceTo(previous.PointPatok) + previous.station;


                if (double.IsNaN(angleNext))
                {
                    angleNext = angleBefore;
                }
                if (double.IsNaN(angleBefore))
                {
                    angleBefore = angleNext;
                }

                double angleCurrent = (angleNext + angleBefore) / 2;
                string SQuadrant = string.Concat(BothQuadrant(previous, current, next));

                switch (SQuadrant)
                {
                    case ("12"):
                        angleCurrent = angleCurrent + (3 * Math.PI / 2); // 270 degree
                        break;
                    case ("21"):
                        angleCurrent = angleCurrent + (1 * Math.PI / 2); // 90 degree
                        break;
                    case ("31"):
                        angleCurrent = angleCurrent + (1 * Math.PI); // 180 degree
                        break;
                    case ("34"):
                        angleCurrent = angleCurrent + (1 * Math.PI / 2); // 90 degree
                        break;
                    case ("42"):
                        angleCurrent = angleCurrent + (1 * Math.PI); // 180 degree
                        break;
                    case ("43"):
                        angleCurrent = angleCurrent + (3 * Math.PI / 2); // 270 degree
                        break;
                }

                current.angle = angleCurrent;
                Point3d AlignmentPoint = PolarPoints(new Point3d(current.X_BASE, current.Y_BASE, current.Z_BASE), angleCurrent, current.Intersect_X);
                AlignmentPoint = new Point3d(AlignmentPoint.X, AlignmentPoint.Y, current.Intersect_Y);
                AlignmentDictionary.Add(AlignmentPoint, angleCurrent);
                current.AsPoint = AlignmentPoint;

                PolygonNodes.Add(current.PointPatok);

                foreach (Node node in current.NodeList)
                {
                    Point3d CalculatedPoint = PolarPoints(new Point3d(current.X_BASE, current.Y_BASE, current.Z_BASE), angleCurrent, node.Distance);
                    CalculatedPoint = new Point3d(CalculatedPoint.X, CalculatedPoint.Y, node.Elevation);
                    node.X = CalculatedPoint.X;
                    node.Y = CalculatedPoint.Y;
                    node.Z = CalculatedPoint.Z;
                    current.NodePoint.Add(new KeyValuePair<Point3d, string>(CalculatedPoint, node.Description));
                    current.FlatNodePoint.Add(node.Point2d);
                }


            }
        }


        private double Slope(Nodes first, Nodes second)
        {
            return (second.Y_BASE - first.Y_BASE) / (second.X_BASE - first.X_BASE);
        }

        private double Reciprocal(double value)
        {
            return (1 / value) * -1;
        }

        public static int GetQuadrant(Nodes first, Nodes second)
        {
            int Quadrant = 0;
            if (second.X_BASE > first.X_BASE && second.Y_BASE > first.Y_BASE)
            {
                Quadrant = 1;
            }
            else if (second.X_BASE < first.X_BASE && second.Y_BASE > first.Y_BASE)
            {
                Quadrant = 2;
            }
            else if (second.X_BASE < first.X_BASE && second.Y_BASE < first.Y_BASE)
            {
                Quadrant = 3;
            }
            else if (second.X_BASE > first.X_BASE && second.Y_BASE < first.Y_BASE)
            {
                Quadrant = 4;
            }

            return Quadrant;
        }

        public static int[] BothQuadrant(Nodes before, Nodes current, Nodes next)
        {
            int[] array = new int[] { GetQuadrant(before, current), GetQuadrant(next, current) };
            return array;
        }
    }
}
