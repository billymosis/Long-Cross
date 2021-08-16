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
    public class Positions
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

    public enum EarthWorks
    {
        Cut,
        Fill,
        Both
    }

    public class Cross
    {
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
            CreateLayer("Cut", 1);
            CreateLayer("Fill", 2);
            this.data = data;
            int row = 0;
            int column = 0;
            data.nodes.ToList().ForEach(x =>
            {
                if (x.NodesType == Nodes.NodesEnum.Cross)
                {
                    positions.Add(x.Patok, new Positions(new Point3d(COLUMN_SPACE_HORIZONTAL * column, -COLUMN_SPACE_VERTICAL * (row % ROW_AMOUNT), 0), row, column));
                    row++;
                    if (row % ROW_AMOUNT == 0)
                    {
                        column++;
                    }
                }

            });
        }

        public void DrawStructure(Nodes nodes, Structure structure, bool currentSpace, EarthWorks earthWorks)
        {
            Dictionary<Entity, EarthWorks> crossEntities = new Dictionary<Entity, EarthWorks>();
            Dictionary<Region, Point3d> structureDictionary = structure.structureDictionary;
            Dictionary<string, Point3d> Design = structure.Design;
            Region cross = DrawFlatCrossRegion(nodes);

            foreach (KeyValuePair<Region, Point3d> item in structureDictionary)
            {
                Region reg = item.Key;
                string Layer = item.Key.Layer;
                if (structure.useBlock)
                {
                    Vector3d vec = new Point3d().GetVectorTo(item.Value);
                    reg.TransformBy(Matrix3d.Displacement(vec));
                }

                switch (earthWorks)
                {
                    case EarthWorks.Cut:
                        foreach (Entity region in GetCutRegion(cross, item.Key, item.Value))
                        {
                            if (region.Layer == "Cut-Region")
                            {
                                crossEntities.Add(region, EarthWorks.Cut);
                            }
                        }
                        break;
                    case EarthWorks.Fill:
                        foreach (Entity region in GetFillRegion(cross, item.Key, item.Value))
                        {
                            if (region.Layer == "Fill-Region")
                            {
                                crossEntities.Add(region, EarthWorks.Fill);
                            }
                        }
                        break;
                    case EarthWorks.Both:
                        if (Layer == "Both-Region" || Layer == "Cut-Region")
                        {
                            foreach (Entity region in GetCutRegion(cross, item.Key, item.Value))
                            {
                                crossEntities.Add(region, EarthWorks.Cut);
                            }
                        }
                        if (Layer == "Both-Region" || Layer == "Fill-Region")
                        {
                            foreach (Entity region in GetFillRegion(cross, item.Key, item.Value))
                            {
                                crossEntities.Add(region, EarthWorks.Fill);
                            }
                        }



                        break;
                    default:
                        break;
                }


            }

            if (currentSpace)
            {
                List<ObjectId> Cobject = new List<ObjectId>();
                List<ObjectId> Fobject = new List<ObjectId>();
                foreach (KeyValuePair<Entity, EarthWorks> item in crossEntities)
                {
                    switch (item.Value)
                    {
                        case EarthWorks.Cut:
                            ObjectId x = item.Key.AddToCurrentSpace();
                            x.SetLayer("Cut");
                            Cobject.Add(x);
                            break;
                        case EarthWorks.Fill:
                            ObjectId y = item.Key.AddToCurrentSpace();
                            y.SetLayer("Fill");
                            Fobject.Add(y);
                            break;
                        default:
                            break;
                    }
                }
                if (Cobject.Count > 0)
                {
                    Dreambuild.AutoCAD.Draw.Hatch(Cobject.ToArray(), "ANSI31", 1, ConvertDegreesToRadians(135)).SetLayer("Cut");
                }
                if (Fobject.Count > 0)
                {
                    Dreambuild.AutoCAD.Draw.Hatch(Fobject.ToArray(), "ANSI31", 1, ConvertDegreesToRadians(45)).SetLayer("Fill");
                }

            }

            else
            {
                List<Entity> Ents = new List<Entity>();
                foreach (KeyValuePair<Entity, EarthWorks> item in crossEntities)
                {
                    switch (item.Value)
                    {
                        case EarthWorks.Cut:
                            item.Key.Layer = "Cut";
                            Ents.Add(HatchEntity(nodes, item.Key, "ANSI31", ConvertDegreesToRadians(135), "Cut"));
                            break;
                        case EarthWorks.Fill:
                            item.Key.Layer = "Fill";
                            Ents.Add(HatchEntity(nodes, item.Key, "ANSI31", ConvertDegreesToRadians(45), "Fill"));
                            break;
                        default:
                            break;
                    }
                }

                Ents.ToArray().ModifyBlock(nodes.Patok);
            }

            foreach (KeyValuePair<string, Point3d> item in Design)
            {
                if (structure.useBlock)
                {
                    if (currentSpace)
                    {
                        Dreambuild.AutoCAD.Draw.Insert(item.Key, item.Value);
                    }
                    else
                    {
                        InsertBlockToBlock(item.Key, nodes.Patok, item.Value);
                    }
                }

            }
        }

        public void Draw(Nodes nodes)
        {
            if (nodes.NodesType == Nodes.NodesEnum.Cross)
            {
                List<Entity> crossEntities = new List<Entity>();
                double boundXl = nodes.Intersect_X - (BOUNDARY_WIDTH / 2);
                double boundXr = nodes.Intersect_X + (BOUNDARY_WIDTH / 2);
                double boundYb = nodes.Lowest_Elevation - 0.1;
                double boundYt = nodes.Maximum_Elevation + 0.1;

                Line bli = NoDraw.Line(new Point3d(boundXl + 1, boundYb, 0), new Point3d(boundXl + 1, boundYt, 0));
                Line bri = NoDraw.Line(new Point3d(boundXr - 1, boundYb, 0), new Point3d(boundXr - 1, boundYt, 0));
                Line blo = NoDraw.Line(new Point3d(boundXl, boundYb, 0), new Point3d(boundXl, boundYt, 0));
                Line bro = NoDraw.Line(new Point3d(boundXr, boundYb, 0), new Point3d(boundXr, boundYt, 0));

                Node first = nodes.NodeList.First.Value;
                Node last = nodes.NodeList.Last.Value;
                Node firstInner = nodes.NodeList.Where(x => x.Distance >= bli.StartPoint.X).First();
                Node lastInner = nodes.NodeList.Where(x => x.Distance <= bri.StartPoint.X).Last();

                List<Node> innerNode = new List<Node>();
                List<Node> outterNode = new List<Node>();
                List<Line> Intersected = new List<Line>();
                List<Line> InnerLines = new List<Line>();
                List<Line> OutterLines = new List<Line>();
                List<Line> ExtensionLinesRight = new List<Line>();
                List<Line> ExtensionLinesLeft = new List<Line>();

                bool isCutted = false;
                bool cutLeft = false;
                bool cutRight = false;

                foreach (Nodes.Segments segment in nodes.segments)
                {
                    FilterSectionType(segment);
                    FilterSectionPosition(nodes, bli, bri, blo, bro, innerNode, outterNode, InnerLines, OutterLines, segment);
                }

                innerNode = innerNode.Distinct().ToList();
                outterNode = outterNode.Distinct().ToList();

                isCutted = OutterLines.Count > 0 ? true : false;
                if (isCutted)
                {
                    cutLeft = first.Distance < bli.StartPoint.X && first.Distance < blo.StartPoint.X;
                    cutRight = last.Distance > bri.StartPoint.X && last.Distance > bro.StartPoint.X;

                    if (cutLeft)
                    {
                        Point3dCollection LeftInnerIntersection = GetIntersectionPoint(bli, OutterLines);

                        Line leftExtension = OutterLines.MinBy(x => x.StartPoint.X).First();

                        if (LeftInnerIntersection.Count > 0 && innerNode.Count > 0)
                        {
                            Line leftInner = InnerLines.MinBy(x => x.EndPoint.X).First();
                            Line a = nodes.segments.Where(x => x.Lines.EndPoint == leftInner.StartPoint).First().Lines;
                            a.StartPoint = new Point3d(LeftInnerIntersection[0].X, LeftInnerIntersection[0].Y, 0);
                            if (a == leftExtension)
                            {
                                Line b = a.Clone() as Line;
                                crossEntities.Add(b);
                            }
                            else
                            {
                                crossEntities.Add(a);
                            }
                            crossEntities.Add(NoDraw.Line(new Point3d(LeftInnerIntersection[0].X, LeftInnerIntersection[0].Y + 0.2, 0), new Point3d(LeftInnerIntersection[0].X, LeftInnerIntersection[0].Y - 0.2, 0)));
                        }

                        Vector3d leftMove = leftExtension.StartPoint.GetVectorTo(new Point3d(blo.StartPoint.X, leftExtension.StartPoint.Y, 0));
                        leftExtension.TransformBy(Matrix3d.Displacement(leftMove));

                        ExtensionLinesLeft.Add(leftExtension);
                        Vector3d move = bli.StartPoint.GetVectorTo(new Point3d(bli.StartPoint.X - 0.2, bli.StartPoint.Y, 0));
                        bli.TransformBy(Matrix3d.Displacement(move));

                        Point3dCollection LeftOutterIntersection = GetIntersectionPoint(bli, ExtensionLinesLeft);

                        if (LeftOutterIntersection.Count > 0 && outterNode.Count > 0)
                        {
                            leftExtension.EndPoint = new Point3d(LeftOutterIntersection[0].X, LeftOutterIntersection[0].Y, 0);
                        }

                        crossEntities.Add(NoDraw.Line(new Point3d(leftExtension.EndPoint.X, leftExtension.EndPoint.Y + 0.2, 0), new Point3d(leftExtension.EndPoint.X, leftExtension.EndPoint.Y - 0.2, 0)));
                        crossEntities.Add(leftExtension);
                        crossEntities.Add(DashLineHelper(nodes.Datum - 0.5, leftExtension.StartPoint.X, first.Elevation));
                        crossEntities.Add(LineLabelSeparator(nodes.Datum, leftExtension.StartPoint.X));
                        crossEntities.Add(TextElevation(nodes.Datum - 0.5, first, leftExtension.StartPoint.X + 0.25));
                        crossEntities.Add(TextDistance(nodes.Datum - 1.5, first, firstInner, (leftExtension.StartPoint.X + firstInner.Distance) / 2));


                    }
                    if (cutRight)
                    {
                        Point3dCollection RightInnerIntersection = GetIntersectionPoint(bri, OutterLines);

                        Line rightExtension = OutterLines.MaxBy(x => x.EndPoint.X).First();

                        if (RightInnerIntersection.Count > 0 && innerNode.Count > 0)
                        {
                            Line rightInner = InnerLines.MaxBy(x => x.EndPoint.X).First();
                            Line a = nodes.segments.Where(x => x.Lines.StartPoint == rightInner.EndPoint).First().Lines;
                            a.EndPoint = new Point3d(RightInnerIntersection[0].X, RightInnerIntersection[0].Y, 0);
                            if (a == rightExtension)
                            {
                                Line b = a.Clone() as Line;
                                crossEntities.Add(b);
                            }
                            else
                            {
                                crossEntities.Add(a);
                            }
                            crossEntities.Add(NoDraw.Line(new Point3d(RightInnerIntersection[0].X, RightInnerIntersection[0].Y + 0.2, 0), new Point3d(RightInnerIntersection[0].X, RightInnerIntersection[0].Y - 0.2, 0)));
                        }

                        Vector3d rightMove = rightExtension.EndPoint.GetVectorTo(new Point3d(bro.StartPoint.X, rightExtension.EndPoint.Y, 0));
                        rightExtension.TransformBy(Matrix3d.Displacement(rightMove));

                        ExtensionLinesRight.Add(rightExtension);

                        Vector3d move = bri.StartPoint.GetVectorTo(new Point3d(bri.StartPoint.X + 0.2, bri.StartPoint.Y, 0));
                        bri.TransformBy(Matrix3d.Displacement(move));

                        Point3dCollection RightOutterIntersection = GetIntersectionPoint(bri, ExtensionLinesRight);

                        if (RightOutterIntersection.Count > 0 && outterNode.Count > 0)
                        {
                            rightExtension.StartPoint = new Point3d(RightOutterIntersection[0].X, RightOutterIntersection[0].Y, 0);
                        }

                        crossEntities.Add(NoDraw.Line(new Point3d(rightExtension.StartPoint.X, rightExtension.StartPoint.Y + 0.2, 0), new Point3d(rightExtension.StartPoint.X, rightExtension.StartPoint.Y - 0.2, 0)));
                        crossEntities.Add(rightExtension);
                        crossEntities.Add(DashLineHelper(nodes.Datum - 0.5, rightExtension.EndPoint.X, last.Elevation));
                        crossEntities.Add(LineLabelSeparator(nodes.Datum, rightExtension.EndPoint.X));
                        crossEntities.Add(TextElevation(nodes.Datum - 0.5, last, rightExtension.EndPoint.X - 0.05));
                        crossEntities.Add(TextDistance(nodes.Datum - 1.5, lastInner, last, (rightExtension.EndPoint.X + lastInner.Distance) / 2));
                    }
                }


                DBText patok = new DBText()
                {
                    Rotation = Math.PI * 2,
                    TextString = nodes.Patok,
                    Height = 1,
                    Justify = AttachmentPoint.MiddleCenter,
                    AlignmentPoint = new Point3d(nodes.Intersect_X, nodes.Datum - 3, 0)
                };

                Line CL = NoDraw.Line(new Point3d(nodes.Intersect_X, nodes.Datum, 0), new Point3d(nodes.Intersect_X, nodes.Datum + 6, 0));
                CL.Linetype = "DASHED";

                DBText CLText = new DBText()
                {
                    Rotation = Math.PI * 2,
                    TextString = "CL",
                    Height = 0.3,
                    Justify = AttachmentPoint.MiddleCenter,
                    AlignmentPoint = new Point3d(CL.StartPoint.X, CL.EndPoint.Y + 0.2, 0)
                };

                LinkedList<Node> innerNodeLinked = new LinkedList<Node>(innerNode);
                foreach (Node node in innerNodeLinked)
                {
                    bool rightInBetween = (node == innerNodeLinked.Last.Value && node.Distance >= bri.StartPoint.X && node.Distance <= bro.StartPoint.X);
                    bool leftInBetween = (node == innerNodeLinked.First.Value && node.Distance >= blo.StartPoint.X && node.Distance <= bli.StartPoint.X);
                    if ((node.Distance >= bli.StartPoint.X && node.Distance <= bri.StartPoint.X) || rightInBetween || leftInBetween)
                    {
                        Line pembantu1 = DashLineHelper(nodes.Datum, node.Point2d.X, node.Elevation);
                        Line pembantu2 = LineLabelSeparator(nodes.Datum, node.Point2d.X);
                        DBText ElevationText;
                        if (node == innerNodeLinked.First.Value)
                        {
                            ElevationText = TextElevation(nodes.Datum - 0.5, node, node.Distance + 0.25);

                        }
                        else
                        {
                            Node prev = innerNodeLinked.Find(node).Previous.Value;
                            if (node.Distance - prev.Distance < 0.25)
                            {
                                ElevationText = TextElevation(nodes.Datum - 0.5, node, node.Distance + 0.25);
                            }
                            else
                            {
                                ElevationText = TextElevation(nodes.Datum - 0.5, node, null);
                            }
                        }

                        if (innerNodeLinked.Find(node).Next != null)
                        {
                            Node next = innerNodeLinked.Find(node).Next.Value;
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
                double StartLabel = blo.StartPoint.X;
                double EndLabel = bro.StartPoint.X;

                ObjectId[] content = GetBlockContent("Block1");
                Entity[] ents = content.QOpenForRead<Entity>();
                Vector3d vec = new Point3d().GetVectorTo(new Point3d(StartLabel - 5.5, nodes.Datum - 2, 0));

                foreach (Entity ent in ents)
                {
                    Entity entclone = ent.Clone() as Entity;
                    entclone.TransformBy(Matrix3d.Displacement(vec));
                    crossEntities.Add(entclone);
                }

                crossEntities.Add(CL);
                crossEntities.Add(CLText);
                crossEntities.Add(NoDraw.Line(new Point3d(StartLabel, nodes.Datum, 0), new Point3d(EndLabel, nodes.Datum, 0)));
                crossEntities.Add(NoDraw.Line(new Point3d(StartLabel, nodes.Datum - 1, 0), new Point3d(EndLabel, nodes.Datum - 1, 0)));
                crossEntities.Add(NoDraw.Line(new Point3d(StartLabel, nodes.Datum - 2, 0), new Point3d(EndLabel, nodes.Datum - 2, 0)));
                crossEntities.Add(LineLabelSeparator(nodes.Datum, blo.StartPoint.X));
                crossEntities.Add(LineLabelSeparator(nodes.Datum, bro.StartPoint.X));

                crossEntities.Add(patok);
                crossEntities.AddRange(InnerLines);
                crossEntities.AddRange(Intersected);
                crossEntities.ToArray().AddToBlock(nodes.Patok, nodes.AsPoint2d);

            }


        }

        public ObjectId Place(Nodes nodes)
        {

            ObjectId id = InsertBlock(nodes.Patok, positions[nodes.Patok].Point);
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                BlockReference blk = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;
                BlockTableRecord btr = tr.GetObject(blk.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                btr.SynchronizeAttributes();
                tr.Commit();
            }
            return id;
        }

        private static void FilterSectionType(Nodes.Segments segment)
        {
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
        }

        private static void FilterSectionPosition(Nodes nodes, Line bli, Line bri, Line blo, Line bro, List<Node> innerNode, List<Node> outterNode, List<Line> InnerLines, List<Line> OutterLines, Nodes.Segments segment)
        {
            Line line = segment.Lines as Line;
            if (line.EndPoint.X >= bri.StartPoint.X && line.EndPoint.X <= bro.StartPoint.X && segment.Section == nodes.segments.Count - 1)
            {
                InnerLines.Add(segment.Lines as Line);
                innerNode.AddRange(segment.nodes);
            }
            else if (line.StartPoint.X >= blo.StartPoint.X && line.StartPoint.X <= bli.StartPoint.X && segment.Section == 0)
            {
                InnerLines.Add(segment.Lines as Line);
                innerNode.AddRange(segment.nodes);
            }
            else if (line.StartPoint.X <= bli.StartPoint.X || line.EndPoint.X <= bli.StartPoint.X)
            {
                OutterLines.Add(segment.Lines as Line);
                outterNode.AddRange(segment.nodes);
            }
            else if (line.EndPoint.X >= bri.StartPoint.X || line.StartPoint.X >= bri.StartPoint.X)
            {
                OutterLines.Add(segment.Lines as Line);
                outterNode.AddRange(segment.nodes);
            }
            else
            {
                InnerLines.Add(segment.Lines as Line);
                innerNode.AddRange(segment.nodes);
            }
        }

        private static Region DrawFlatCrossRegion(Nodes nodes)
        {
            Polyline Cross = NoDraw.Pline(ConvertP3DC2List(nodes.FlatNodePoint));
            Point2d pstart = Cross.GetPoint2dAt(0);
            Point2d pend = Cross.GetPoint2dAt(Cross.NumberOfVertices - 1);
            Cross.AddVertexAt(0, new Point2d(pstart.X, nodes.Datum), 0, 0, 0);
            Cross.AddVertexAt(Cross.NumberOfVertices, new Point2d(pend.X, nodes.Datum), 0, 0, 0);
            Cross.Closed = true;
            return CreateRegionFromPolyline(Cross);
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

    }
}