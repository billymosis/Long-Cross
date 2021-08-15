using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PLC.Utilities;
namespace PLC
{


    public class Nodes
    {
        public readonly double X_BASE;
        public readonly double Y_BASE;
        public readonly double Z_BASE;
        public double Mid_Horizontal;
        public double Intersect_X;
        public double Intersect_Y;
        private double lowest_elevation;
        public double Lowest_Elevation
        {
            get => lowest_elevation;
            set
            {
                lowest_elevation = value;
                Datum = Math.Round(value) - 2;
            }
        }
        public double Maximum_Elevation;
        public double Datum;
        public double angle;
        public NodesEnum NodesType;
        public string Patok;
        public LinkedList<Node> NodeList = new LinkedList<Node>();
        public Point3d AsPoint = new Point3d();
        public Point3d AsPoint2d = new Point3d();
        public Point3dCollection NodePoint = new Point3dCollection();
        public Point3dCollection FlatNodePoint = new Point3dCollection();
        public List<Segments> segments = new List<Segments>();
        public bool ValidCross = false;
        public enum NodesEnum
        {
            Patok,
            Cross
        }

        public enum SectionType
        {
            Jalan,
            Normal,
            Lining,
        }
        public class Segments
        {
            public Line Lines;
            public int Section;
            public SectionType sectionTypes;
            public Node[] nodes;

            public Segments(Line lines, int section, SectionType sectionTypes, Node[] nodes)
            {
                this.nodes = nodes;
                Lines = lines;
                Section = section;
                this.sectionTypes = sectionTypes;
            }
        }



        public Nodes(double x_base, double y_base, double z_base, LinkedList<Node> _nodes, string _patok)
        {
            if (_nodes.Count == 1)
            {
                NodesType = NodesEnum.Patok;
            }
            else
            {
                NodesType = NodesEnum.Cross;
                List<double> Distances = new List<double>();
                foreach (Node item in _nodes)
                {
                    Distances.Add(item.Distance);
                }
                ValidCross = Distances.IsOrdered();
                //foreach (Node item in _nodes)
                //{

                //    if (_nodes.Find(item).Next != null)
                //    {
                //        Node prev = _nodes.Find(item).Next.Previous.Value;
                //        if (item.Distance >= prev.Distance)
                //        {
                //            ValidCross = true;
                //        }
                //        else
                //        {
                //            break;
                //        }
                //    }
                //    else if (_nodes.Find(item).Next != null && _nodes.Find(item).Previous != null)
                //    {
                //        Node next = _nodes.Find(item).Next.Value;
                //        Node prev = _nodes.Find(item).Next.Previous.Value;
                //        if (item.Distance >= prev.Distance && item.Distance <= next.Distance)
                //        {
                //            ValidCross = true;
                //        }
                //        else
                //        {
                //            break;
                //        }
                //    }
                //}
            }


            X_BASE = x_base;
            Y_BASE = y_base;
            Z_BASE = z_base;
            Patok = _patok;
            NodeList = _nodes;
            Lowest_Elevation = LowestElevation(_nodes);
            Maximum_Elevation = MaximumElevation(_nodes);
            switch (NodesType)
            {
                case NodesEnum.Patok:
                    break;
                case NodesEnum.Cross:
                    Node first_base = NodeList.Where(x => x.Description.Contains('D')).First();
                    Node last_base = NodeList.Where(x => x.Description.Contains('D')).Last();

                    Mid_Horizontal = (first_base.Distance + last_base.Distance) / 2;
                    Point2d IntersectionPoint = FindIntersection2d
                        (
                        new Point2d(first_base.Distance, first_base.Elevation),
                        new Point2d(last_base.Distance, last_base.Elevation),
                        new Point2d(Mid_Horizontal, Maximum_Elevation),
                        new Point2d(Mid_Horizontal, Lowest_Elevation)
                        );
                    Intersect_X = IntersectionPoint.X;
                    Intersect_Y = IntersectionPoint.Y;
                    AsPoint2d = new Point3d(Intersect_X, Intersect_Y, 0);
                    int counter = 0;
                    foreach (Node current in NodeList)
                    {
                        LinkedListNode<Node> currentNodes = NodeList.Find(current);
                        if (currentNodes.Next != null)
                        {
                            Node next = currentNodes.Next.Value;
                            Line line = NoDraw.Line(current.Point2d, next.Point2d);
                            SectionType types = new SectionType();
                            if (current.Description.Contains('J') && next.Description.Contains('J'))
                            {
                                types = SectionType.Jalan;
                            }
                            else if (current.Description.Contains('L') && next.Description.Contains('L'))
                            {
                                types = SectionType.Lining;
                            }
                            else
                            {
                                types = SectionType.Normal;
                            }
                            segments.Add(new Segments(line, counter, types, new Node[] { current, currentNodes.Next.Value }));
                        }
                        counter++;
                    }
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

        private double LowestElevation(LinkedList<Node> node)
        {
            return node.Min(x => x.Elevation);
        }

        private double MaximumElevation(LinkedList<Node> node)
        {
            return node.Max(x => x.Elevation);
        }
    }


}
