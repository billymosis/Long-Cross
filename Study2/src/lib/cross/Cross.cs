using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static PLC.Utilities;



namespace PLC
{
    public struct Positions
    {
        public Point3d Point { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public Positions(Point3d point, int row, int column)
        {
            Point = point;
            Row = row;
            Column = column;
        }
    }

    public class Cross
    {
        [CommandMethod("CrossDraw")]
        public static void CrossDraw()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ")
            {
                Filter = "Cross File (*.csv;*.txt)|*.csv;*.txt"
            };
            string path = ed.GetFileNameForOpen(POFO).StringResult;
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            string s = path;
            Stopwatch stopwatch = new Stopwatch();
            Data data = new Data(s);
            Cross x = new Cross(data.canal);
            ProgressMeter pm = new ProgressMeter();
            pm.Start("Processing...");
            pm.SetLimit(data.canal.nodes.Count);
            Nodes yo = data.canal.nodes.Where(z => z.Patok == "P.2").Select(y => y).First();
            foreach (Nodes item in data.canal.nodes)
            {
                if (item.NodesType == Nodes.NodesEnum.Cross)
                {
                    x.Draw(item);
                    x.Place(item);
                    pm.MeterProgress();
                }

            }
            pm.Stop();
        }



        private readonly ObjectId LABEL_STYLE = DbHelper.GetTextStyleId("L80");
        private const double LABEL_SPACE = 0.05;
        private readonly Canal data;
        private const int COLUMN_SPACE_HORIZONTAL = 50;
        private const int COLUMN_SPACE_VERTICAL = 50;
        private const int ROW_AMOUNT = 4;
        private const double BOUNDARY_WIDTH = 30;
        private const double MAXIMUM_TEXT_LABEL_WIDTH = 0.6;
        public Dictionary<string, Positions> positions = new Dictionary<string, Positions>();

        public Cross(Canal data)
        {
            this.data = data;
            int row = 0;
            int column = 0;
            data.nodes.ToList().ForEach(x =>
            {
                positions.Add(x.Patok, new Positions(new Point3d(COLUMN_SPACE_HORIZONTAL * column, -COLUMN_SPACE_VERTICAL * (row % ROW_AMOUNT), 0), row, column));
                row++;
                if (row % ROW_AMOUNT == 0)
                {
                    column++;
                }
            });
        }

        public void Draw(Nodes nodes)
        {
            if (nodes.NodesType == Nodes.NodesEnum.Cross)
            {
                List<Entity> crossEntities = new List<Entity>();
                double boundXl = nodes.Intersect_X - (BOUNDARY_WIDTH / 2) + 1;
                double boundXr = nodes.Intersect_X + (BOUNDARY_WIDTH / 2) - 1;
                double boundYb = nodes.Lowest_Elevation - 0.1;
                double boundYt = nodes.Maximum_Elevation + 0.1;

                Line bli = NoDraw.Line(new Point3d(boundXl, boundYb, 0), new Point3d(boundXl, boundYt, 0));
                Line bri = NoDraw.Line(new Point3d(boundXr, boundYb, 0), new Point3d(boundXr, boundYt, 0));
                Line blo = NoDraw.Line(new Point3d(boundXl - 0.2, boundYb, 0), new Point3d(boundXl - 0.2, boundYt, 0));
                Line bro = NoDraw.Line(new Point3d(boundXr + 0.2, boundYb, 0), new Point3d(boundXr + 0.2, boundYt, 0));

                Node first = nodes.NodeList.First.Value;
                Node last = nodes.NodeList.Last.Value;
                Node firstInner = nodes.NodeList.Where(x => x.Distance >= boundXl).First();
                Node lastInner = nodes.NodeList.Where(x => x.Distance <= boundXr).Last();

                List<Line> Intersected = new List<Line>();
                List<Line> InnerLines = new List<Line>();
                List<Line> OutterLines = new List<Line>();
                List<Line> ExtensionLines = new List<Line>();

                bool isCutted = false;
                foreach (Nodes.Segments segment in nodes.segments)
                {
                    Line line = segment.Lines as Line;
                    double midpoint = (line.StartPoint.X + line.EndPoint.X) / 2;
                    switch (segment.sectionTypes)
                    {
                        case Nodes.SectionType.Jalan:
                            segment.Lines.ColorIndex = 1;
                            break;
                        case Nodes.SectionType.Normal:
                            segment.Lines.ColorIndex = 3;
                            break;
                        case Nodes.SectionType.Lining:
                            segment.Lines.ColorIndex = 2;
                            break;
                        default:
                            break;
                    }
                    if (!(line.StartPoint.X <= boundXl || line.EndPoint.X >= boundXr))
                    {

                        InnerLines.Add(segment.Lines as Line);
                    }
                    else
                    {
                        OutterLines.Add(segment.Lines as Line);
                    }

                }
                if (OutterLines.Count > 0)
                {
                    isCutted = true;
                }
                foreach (Node node in nodes.NodeList)
                {
                    if (node.Distance >= boundXl && node.Distance <= boundXr)
                    {
                        Line pembantu1 = DashLineHelper(nodes.Datum, node.Point2d.X, node.Elevation);
                        Line pembantu2 = LineLabelSeparator(nodes.Datum, node.Point2d.X);

                        DBText ElevationText = TextElevation(nodes.Datum - 0.5, node, null);
                        if (nodes.NodeList.Find(node).Next != null && node != lastInner)
                        {
                            Node next = nodes.NodeList.Find(node).Next.Value;
                            DBText DistanceText = TextDistance(nodes.Datum - 1.5, node, next, null);
                            double Bounds = MAXIMUM_TEXT_LABEL_WIDTH;
                            double limit = next.Distance - node.Distance;
                            if (Bounds >= limit)
                            {
                                DistanceText.Rotation = Math.PI / 2;
                                DistanceText.ColorIndex = 1;
                            }
                            crossEntities.Add(DistanceText);
                        }
                        crossEntities.Add(pembantu1);
                        crossEntities.Add(pembantu2);
                        crossEntities.Add(ElevationText);
                    }


                }

                if (isCutted)
                {
                    Point3dCollection InnerIntersections = GetIntersectionPoint(bli, bri, OutterLines);

                    Line left = OutterLines.MinBy(x => x.StartPoint.X).First();
                    Line right = OutterLines.MaxBy(x => x.EndPoint.X).First();
                    Vector3d leftMove = left.StartPoint.GetVectorTo(new Point3d(boundXl - 1, left.StartPoint.Y, 0));
                    Vector3d rightMove = right.EndPoint.GetVectorTo(new Point3d(boundXr + 1, right.EndPoint.Y, 0));
                    left.TransformBy(Matrix3d.Displacement(leftMove));
                    right.TransformBy(Matrix3d.Displacement(rightMove));
                    ExtensionLines.Add(left);
                    ExtensionLines.Add(right);

                    crossEntities.Add(DashLineHelper(nodes.Datum - 0.5, left.StartPoint.X, first.Elevation));
                    crossEntities.Add(DashLineHelper(nodes.Datum - 0.5, right.EndPoint.X, last.Elevation));
                    crossEntities.Add(LineLabelSeparator(nodes.Datum, left.StartPoint.X));
                    crossEntities.Add(LineLabelSeparator(nodes.Datum, right.EndPoint.X));

                    crossEntities.Add(TextElevation(nodes.Datum - 0.5, first, left.StartPoint.X + 0.25));
                    crossEntities.Add(TextElevation(nodes.Datum - 0.5, last, right.EndPoint.X));
                    crossEntities.Add(TextDistance(nodes.Datum - 1.5, first, firstInner, (left.StartPoint.X + firstInner.Distance) / 2));
                    crossEntities.Add(TextDistance(nodes.Datum - 1.5, lastInner, last, (right.EndPoint.X + lastInner.Distance) / 2));

                    crossEntities.Add(NoDraw.Line(new Point3d(left.StartPoint.X, nodes.Datum, 0), new Point3d(right.EndPoint.X, nodes.Datum, 0)));
                    crossEntities.Add(NoDraw.Line(new Point3d(left.StartPoint.X, nodes.Datum - 1, 0), new Point3d(right.EndPoint.X, nodes.Datum - 1, 0)));
                    crossEntities.Add(NoDraw.Line(new Point3d(left.StartPoint.X, nodes.Datum - 2, 0), new Point3d(right.EndPoint.X, nodes.Datum - 2, 0)));


                    Point3dCollection OutterIntersections = GetIntersectionPoint(blo, bro, ExtensionLines);
                    foreach (Entity item in ExtensionLines)
                    {
                        Point3dCollection g1 = new Point3dCollection();
                        Point3dCollection g2 = new Point3dCollection();

                        blo.IntersectWith(item, Intersect.OnBothOperands, g1, IntPtr.Zero, IntPtr.Zero);
                        bro.IntersectWith(item, Intersect.OnBothOperands, g2, IntPtr.Zero, IntPtr.Zero);
                        foreach (Point3d gg1 in g1)
                        {
                            OutterIntersections.Add(gg1);
                        }
                        foreach (Point3d gg2 in g2)
                        {
                            OutterIntersections.Add(gg2);
                        }
                    }

                    foreach (Point3d point in OutterIntersections)
                    {
                        crossEntities.Add(NoDraw.Line(new Point3d(point.X, point.Y + 0.2, 0), new Point3d(point.X, point.Y - 0.2, 0)));
                        foreach (Line line in ExtensionLines)
                        {
                            if (Collinear(line, point))
                            {
                                line.ColorIndex = 6;
                                foreach (Line item in line.GetSplitCurves(new Point3dCollection(new Point3d[] { point })))
                                {
                                    if (item.StartPoint.X == left.StartPoint.X || item.EndPoint.X == right.EndPoint.X)
                                    {
                                        Intersected.Add(item);
                                    }
                                }
                            }

                        }
                    }
                    foreach (Point3d point in InnerIntersections)
                    {
                        crossEntities.Add(NoDraw.Line(new Point3d(point.X, point.Y + 0.2, 0), new Point3d(point.X, point.Y - 0.2, 0)));
                        foreach (Line line in OutterLines)
                        {
                            if (Collinear(line, point))
                            {
                                line.ColorIndex = 4;

                                foreach (Line item in line.GetSplitCurves(new Point3dCollection(new Point3d[] { point })))
                                {
                                    if (!(item.StartPoint.X < bli.StartPoint.X || item.EndPoint.X > bri.StartPoint.X))
                                    {
                                        Intersected.Add(item);
                                    }
                                }
                            }

                        }
                    }
                }
                else
                {
                    crossEntities.Add(NoDraw.Line(new Point3d(first.Distance, nodes.Datum, 0), new Point3d(last.Distance, nodes.Datum, 0)));
                    crossEntities.Add(NoDraw.Line(new Point3d(first.Distance, nodes.Datum - 1, 0), new Point3d(last.Distance, nodes.Datum - 1, 0)));
                    crossEntities.Add(NoDraw.Line(new Point3d(first.Distance, nodes.Datum - 2, 0), new Point3d(last.Distance, nodes.Datum - 2, 0)));
                }
                DBText patok = new DBText() {
                    Rotation = Math.PI * 2,
                    TextString = nodes.Patok,
                    Height = 1,
                    Justify = AttachmentPoint.MiddleCenter,
                    AlignmentPoint = new Point3d(nodes.Intersect_X, nodes.Datum - 3,0)
                };

                crossEntities.Add(patok);
                crossEntities.AddRange(InnerLines);
                crossEntities.AddRange(Intersected);
                crossEntities.ToArray().AddToBlock(nodes.Patok, nodes.AsPoint2d);
            }


        }

        private Point3dCollection GetIntersectionPoint(Line bli, Line bri, List<Line> OutterLines)
        {
            Point3dCollection InnerIntersections = new Point3dCollection();
            foreach (Entity item in OutterLines)
            {
                Point3dCollection g1 = new Point3dCollection();
                Point3dCollection g2 = new Point3dCollection();

                bli.IntersectWith(item, Intersect.OnBothOperands, g1, IntPtr.Zero, IntPtr.Zero);
                bri.IntersectWith(item, Intersect.OnBothOperands, g2, IntPtr.Zero, IntPtr.Zero);
                foreach (Point3d gg1 in g1)
                {
                    InnerIntersections.Add(gg1);
                }
                foreach (Point3d gg2 in g2)
                {
                    InnerIntersections.Add(gg2);
                }
            }
            return InnerIntersections;
        }

        private DBText TextDistance(double verticalPosition, Node node, Node next, double? horizontalPosition)
        {
            double value = horizontalPosition ?? (node.Distance + next.Distance) / 2;
            return new DBText()
            {
                Rotation = Math.PI * 2,
                TextString = Math.Round(next.Distance - node.Distance, 2).ToString("N2"),
                Justify = AttachmentPoint.MiddleCenter,
                AlignmentPoint = new Point3d(value, verticalPosition, 0),
                TextStyleId = LABEL_STYLE
            };
        }

        private DBText TextElevation(double verticalPosition, Node node, double? horizontalPosition)
        {
            double value = horizontalPosition ?? node.Distance - LABEL_SPACE;
            return new DBText()
            {
                Rotation = Math.PI / 2,
                TextString = Math.Round(node.Elevation, 2).ToString("N2"),
                Justify = AttachmentPoint.BaseCenter,
                AlignmentPoint = new Point3d(value, verticalPosition, 0),
                TextStyleId = LABEL_STYLE
            };
        }

        private static Line LineLabelSeparator(double datum, double horizontal)
        {
            return NoDraw.Line(new Point3d(horizontal, datum, 0), new Point3d(horizontal, datum - 2, 0));
        }

        private static Line DashLineHelper(double datum, double horizontal, double elevation)
        {
            Line pembantu1 = NoDraw.Line(new Point3d(horizontal, elevation, 0), new Point3d(horizontal, datum, 0));
            pembantu1.Linetype = "DASHED";
            return pembantu1;
        }

        public bool Collinear(Line line, Point3d p)
        {
            double y1 = line.StartPoint.Y;
            double y2 = line.EndPoint.Y;
            double y3 = p.Y;
            double x1 = line.StartPoint.X;
            double x2 = line.EndPoint.X;
            double x3 = p.X;
            return Math.Abs((y1 - y2) * (x1 - x3) - (y1 - y3) * (x1 - x2)) <= 1e-9;
        }

        public void Place(Nodes nodes)
        {
            InsertBlock(nodes.Patok, positions[nodes.Patok].Point);
        }

    }
}