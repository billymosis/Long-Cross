using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC
{
    public class Long
    {
        private readonly Canal data;
        private readonly ObjectId LABEL_STYLE = DbHelper.GetTextStyleId("L80");
        private const double LABEL_SPACE = 0.05;
        private const double HORIZONTAL_SCALE = 0.05;
        private double datum;
        public Long(Canal data)
        {
            this.data = data;
        }

        public void DrawLong()
        {
            List<Entity> LongEntities = new List<Entity>();
            LinkedList<KeyValuePair<Point3d, double>> Alignments = new LinkedList<KeyValuePair<Point3d, double>>(data.AlignmentDistance.Where(x => x.Key.Z != 0).ToList());
            Point3d PointStart = new Point3d(Alignments.First().Value, Alignments.First().Key.Y, 0);
            Point3d PointFinish = new Point3d(Alignments.Last().Value, Alignments.Last().Key.Y, 0);
            datum = Alignments.Last().Key.Z - 1;

            foreach (KeyValuePair<Point3d, double> current in Alignments)
            {
                LinkedListNode<KeyValuePair<Point3d, double>> currentNodes = Alignments.Find(current);
                KeyValuePair<Point3d, double>? next = currentNodes.Next?.Value;
                if (next != null)
                {
                    double currentScaledX = HORIZONTAL_SCALE * current.Value;
                    double nextScaledX = HORIZONTAL_SCALE * next.Value.Value;
                    Line L = NoDraw.Line(new Point3d(currentScaledX, current.Key.Z, 0), new Point3d(nextScaledX, next.Value.Key.Z, 0));
                    LongEntities.Add(L);
                }

            }

            foreach (Nodes nodes in data.nodes)
            {
                LinkedListNode<Nodes> currentNodes = data.nodes.Find(nodes);
                Nodes next = currentNodes.Next?.Value;
                double scaledStation = nodes.station * HORIZONTAL_SCALE;

                LongEntities.Add(TextLabel(datum - 1, scaledStation, nodes.Patok));
                LongEntities.Add(TextElevation(datum - 2, scaledStation, nodes.station));
                if (next != null)
                {
                    double nextScaledStation = next.station * HORIZONTAL_SCALE;
                    LongEntities.Add(TextLabel(datum - 2, (scaledStation + nextScaledStation) / 2, (next.station - nodes.station).ToString("N2")));
                }
                if (nodes.ValidCross)
                {
                    LongEntities.Add(TextElevation(datum - 3, scaledStation, nodes.NodeList.Where(x => x.Description.Contains("T")).First().Elevation));
                    LongEntities.Add(TextElevation(datum - 4, scaledStation, nodes.NodeList.Where(x => x.Description.Contains("T")).Last().Elevation));
                    LongEntities.Add(TextElevation(datum - 5, scaledStation, nodes.Intersect_Y));
                }

            }

            Draw.Line(new Point3d(PointStart.X * HORIZONTAL_SCALE, datum, 0), new Point3d(PointFinish.X * HORIZONTAL_SCALE, datum, 0));

            LongEntities.AddRange(DrawLine(data.TanggulKiri, datum - 20, HORIZONTAL_SCALE));
            LongEntities.AddRange(DrawLine(data.TanggulKanan, datum - 40, HORIZONTAL_SCALE));
            LongEntities.AddRange(DrawVerticalHelper(data.TanggulTertinggi, datum, HORIZONTAL_SCALE));


            LongEntities.AddToCurrentSpace();
        }

        private List<Entity> DrawLine(List<KeyValuePair<Point3d, double>> data, double LabelHeight, double horizontalScale)
        {
            List<Entity> entities = new List<Entity>();
            LinkedList<KeyValuePair<Point3d, double>> Point = new LinkedList<KeyValuePair<Point3d, double>>(data);
            foreach (KeyValuePair<Point3d, double> current in Point)
            {
                LinkedListNode<KeyValuePair<Point3d, double>> currentNodes = Point.Find(current);
                KeyValuePair<Point3d, double>? next = currentNodes.Next?.Value;
                if (next != null)
                {
                    double currentScaledX = horizontalScale * current.Value;
                    double nextScaledX = horizontalScale * next.Value.Value;
                    Line L = NoDraw.Line(new Point3d(currentScaledX, current.Key.Z, 0), new Point3d(nextScaledX, next.Value.Key.Z, 0));
                    entities.Add(L);
                }
            }
            return entities;
        }



        private List<Entity> DrawVerticalHelper(List<KeyValuePair<Point3d, double>> data, double LowHeight, double horizontalScale)
        {
            List<Entity> entities = new List<Entity>();
            foreach (KeyValuePair<Point3d, double> current in data)
            {
                Line L = NoDraw.Line(new Point3d(current.Value * horizontalScale, current.Key.Z, 0), new Point3d(current.Value * horizontalScale, LowHeight, 0));
                L.ColorIndex = 1;
                L.Linetype = "DASHED";
                entities.Add(L);
            }
            return entities;
        }

        private DBText TextLabel(double verticalPosition, double horizontalPosition, string text)
        {
            return new DBText()
            {
                Rotation = Math.PI * 2,
                TextString = text,
                Justify = AttachmentPoint.MiddleCenter,
                AlignmentPoint = new Point3d(horizontalPosition, verticalPosition, 0),
                TextStyleId = LABEL_STYLE
            };
        }

        private DBText TextElevation(double verticalPosition, double horizontalPosition, double text)
        {
            return new DBText()
            {
                Rotation = Math.PI / 2,
                TextString = Math.Round(text, 2).ToString("N2"),
                Justify = AttachmentPoint.MiddleCenter,
                AlignmentPoint = new Point3d(horizontalPosition, verticalPosition, 0),
                TextStyleId = LABEL_STYLE
            };
        }
    }
}
