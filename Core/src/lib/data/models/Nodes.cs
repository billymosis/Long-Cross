﻿using Autodesk.AutoCAD.DatabaseServices;
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

    public enum Bangunan
    {
        NoStructure = 0,
        Bendung = 1,
        Bagi = 2,
        Sadap = 3,
        Terjun = 4,
        GrPembawa = 5,
        GrPembuang = 6,
        Jembatan = 7,
        Talang = 8,
        Siphon = 9,
        GotMiring = 10,
        PelimpahSamping = 11,
        BoxTersier = 12,
        BoxKwarter = 13,
        JembatanOrg = 14,
        TempatCuci = 15,
        TempatMandiHewan = 16,
        AlurPembuang = 17,
        BangunanUkur = 18,
        Suplesi = 19,
        BangunanPembuang = 20
    }

    public enum Pengukuran
    {
        Tanggul,
        Dasar
    }

    public enum ErrorType
    {
        DistanceNotOrdered,
        TanggulDasarError,
        WrongInput
    }

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
        public double station;
        public NodesEnum NodesType;
        public Bangunan StructureType;
        public ErrorType errorType;
        public string NamaBangunan;
        public string Patok;
        public LinkedList<Node> NodeList = new LinkedList<Node>();
        public Point3d AsPoint = new Point3d();
        public Point3d AsPoint2d = new Point3d();
        public List<KeyValuePair<Point3d, string>> NodePoint = new List<KeyValuePair<Point3d, string>>();
        public Point3dCollection FlatNodePoint = new Point3dCollection();
        public List<Segments> segments = new List<Segments>();
        public bool ValidCross = false;
        public Point3d PointPatok = new Point3d();
        public string errorList;

        public Nodes(double x_base, double y_base, double z_base, LinkedList<Node> _nodes, string _patok, Bangunan bangunan, string namaBangunan, Pengukuran pengukuran)
        {
            string pengukuranValue = "";
            switch (pengukuran)
            {
                case Pengukuran.Tanggul:
                    pengukuranValue = "T";
                    break;
                case Pengukuran.Dasar:
                    pengukuranValue = "D";
                    break;
                default:
                    break;
            }
            if (_nodes.Count == 1)
            {
                NodesType = NodesEnum.Patok;
            }
            else
            {
                NodesType = NodesEnum.Cross;
                List<double> Distances = new List<double>();
                List<double> Elevation = new List<double>();
                List<string> Description = new List<string>();
                foreach (Node item in _nodes)
                {
                    Distances.Add(item.Distance);
                    Elevation.Add(item.Elevation);
                    Description.Add(item.Description);
                }
                bool condition1 = Distances.IsOrdered();
                bool condition2 = Distances.Count == Elevation.Count && Distances.Count == Description.Count && Elevation.Count == Distances.Count;
                bool condition3 = Description.Where(x => x.Contains("T")).Count() % 2 == 0 && Description.Where(x => x.Contains("D")).Count() % 2 == 0;
                ValidCross = condition1 && condition2 && condition3;
                if (!condition1)
                {
                    errorType = ErrorType.DistanceNotOrdered;
                    errorList = ("Error Cross: " + _patok + " Data jarak tidak berurutan dari lebih kecil dari kiri ke kanan \n");
                }
                if (!condition2)
                {
                    errorType = ErrorType.WrongInput;
                    errorList = ("Error Cross: " + _patok + " Terdapat titik yang salah antara distance,elevation dan description \n");
                }
                if (!condition3)
                {
                    errorType = ErrorType.TanggulDasarError;
                    errorList = ("Error Cross: " + _patok + " Jumlah [T]anggul / [D]asar tidak genap, tidak bisa mendapatkan nilai interploasi \n");

                }
            }

            StructureType = bangunan;
            NamaBangunan = namaBangunan;

            X_BASE = x_base;
            Y_BASE = y_base;
            Z_BASE = z_base;
            PointPatok = new Point3d(X_BASE, Y_BASE, Z_BASE);
            Patok = _patok;
            NodeList = _nodes;
            Lowest_Elevation = LowestElevation(_nodes);
            Maximum_Elevation = MaximumElevation(_nodes);
            switch (NodesType)
            {
                case NodesEnum.Patok:
                    break;
                case NodesEnum.Cross:
                    Node first_base = NodeList.Where(x => x.Description.Contains(pengukuranValue)).First();
                    Node last_base = NodeList.Where(x => x.Description.Contains(pengukuranValue)).Last();

                    Mid_Horizontal = (first_base.Distance + last_base.Distance) / 2;
                    Line y = NoDraw.Line(new Point3d(Mid_Horizontal, Maximum_Elevation, 0), new Point3d(Mid_Horizontal, Datum, 0));
                    Polyline crossLine = NoDraw.Pline(_nodes.Select(n => n.Point2d).ToList());
                    Point3dCollection p3dc = new Point3dCollection();
                    crossLine.IntersectWith(y, Intersect.ExtendArgument, p3dc, IntPtr.Zero, IntPtr.Zero);
                    Intersect_X = p3dc[0].X;
                    Intersect_Y = p3dc[0].Y;
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
