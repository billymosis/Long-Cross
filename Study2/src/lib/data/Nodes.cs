using Autodesk.AutoCAD.Geometry;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PLC.Utilities;
namespace PLC
{
    public enum NodesEnum
    {
        Patok,
        Cross
    }

    public class Node
    {
        public double X;
        public double Y;
        public double Z;
        public double Distance;
        public double Elevation;
        public string Description;

        public Node(double elevation, double distance, string description)
        {
            Elevation = elevation;
            Distance = distance;
            Description = description;
        }
    }

    public class Nodes
    {
        public readonly double X_BASE;
        public readonly double Y_BASE;
        public readonly double Z_BASE;
        public double Mid_Horizontal;
        public double Intersect_X;
        public double Intersect_Y;
        public double Lowest_Elevation;
        public double Maximum_Elevation;
        public double angle;
        public NodesEnum NodesType;
        public string Patok;
        public LinkedList<Node> node = new LinkedList<Node>();

        public Nodes(double x_base, double y_base, double z_base, LinkedList<Node> _nodes, string _patok)
        {
            if (_nodes.Count == 1)
            {
                NodesType = NodesEnum.Patok;
            }
            else
            {
                NodesType = NodesEnum.Cross;
            }

            X_BASE = x_base;
            Y_BASE = y_base;
            Z_BASE = z_base;
            Patok = _patok;
            node = _nodes;
            Lowest_Elevation = LowestElevation(_nodes);
            Maximum_Elevation = MaximumElevation(_nodes);
            switch (NodesType)
            {
                case NodesEnum.Patok:
                    break;
                case NodesEnum.Cross:
                    Mid_Horizontal = MidCalculate(_nodes);
                    Node first_base = node.Where(x => x.Description.Contains('D')).First();
                    Node last_base = node.Where(x => x.Description.Contains('D')).Last();
                    Point2d IntersectionPoint = FindIntersection2d(new Point2d(first_base.Distance, first_base.Elevation), new Point2d(last_base.Distance, last_base.Elevation), new Point2d(Mid_Horizontal, Maximum_Elevation), new Point2d(Mid_Horizontal, Lowest_Elevation));
                    Intersect_X = IntersectionPoint.X;
                    Intersect_Y = IntersectionPoint.Y;
                    break;
                default:
                    break;
            }

        }

        private Point2d FindIntersection2d(Point2d s1, Point2d e1, Point2d s2, Point2d e2)
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

        private double MidCalculate(LinkedList<Node> node)
        {
            Node first_base = node.Where(x => x.Description.Contains('D')).First();
            Node last_base = node.Where(x => x.Description.Contains('D')).Last();

            return (first_base.Distance + last_base.Distance) / 2;
        }

        private double LowestElevation(LinkedList<Node> node)
        {
            return node.Min(x => x.Elevation);
        }

        private double MaximumElevation(LinkedList<Node> node)
        {
            return node.Max(x => x.Elevation);
        }
    }

    public class Canal
    {
        public LinkedList<Nodes> nodes = new LinkedList<Nodes>();
        public Point3dCollection AlignmentNodes = new Point3dCollection();
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
                AlignmentNodes.Add(AlignmentPoint);

                foreach (Node node in current.node)
                {
                    Point3d CalculatedPoint = PolarPoints(new Point3d(current.X_BASE, current.Y_BASE, current.Z_BASE), angleCurrent, node.Distance);
                    CalculatedPoint = new Point3d(CalculatedPoint.X, CalculatedPoint.Y, node.Elevation);
                    node.X = CalculatedPoint.X;
                    node.Y = CalculatedPoint.Y;
                    node.Z = CalculatedPoint.Z;
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
