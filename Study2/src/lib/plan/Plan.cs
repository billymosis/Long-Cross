using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using static PLC.Utilities;


namespace PLC
{
    public class Plan
    {


        private readonly Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        private readonly ObjectId LABEL_STYLE = DbHelper.GetTextStyleId("L80");
        public int Start { get; set; }
        public int End { get; set; }
        private readonly Canal data;
        private Dictionary<int, ObjectId> Structures = new Dictionary<int, ObjectId>();

        public Plan(Canal data)
        {
            CreateLayer("TEXT-ELEVATION-TANGGUL", 1);
            CreateLayer("BREAK-LINE-TANGGUL", 1);
            CreateLayer("BREAK-LINE-DASAR", 2);
            CreateLayer("TEXT-ELEVATION-DASAR", 2);
            CreateLayer("TEXT-ELEVATION-PATOK", 3);
            CreateLayer("TEXT-ELEVATION-ALIGNMENT", 4);
            CreateLayer("TEXT-ELEVATION", 5);
            CreateLayer("TEXT-PATOK", 6);
            this.data = data;

            for (int i = 1; i < 20; i++)
            {
                Structures.Add(i, GetBlockID("B" + i));
            }

        }


        public void DrawPlan(bool ValidCrossOnly)
        {
            List<Entity> PlanEntities = new List<Entity>();
            List<Entity> Alignment = DrawAlignment(data);
            List<Entity> Polygon = DrawPolygon(data);
            LinkedList<Nodes> d = new LinkedList<Nodes>();
            if (ValidCrossOnly)
            {
                d = new LinkedList<Nodes>(data.nodes.Where(z => z.ValidCross == true).ToList());
            }
            else
            {
                d = new LinkedList<Nodes>(data.nodes.ToList());
            }
            List<Entity> Cross = DrawCross(d);
            List<Entity> T1 = DrawTanggulDasar(data);
            List<Entity> S = DrawStructure(data);

            PlanEntities.AddRange(Alignment);
            PlanEntities.AddRange(Polygon);
            PlanEntities.AddRange(Cross);
            PlanEntities.AddRange(T1);
            PlanEntities.AddRange(S);
            PlanEntities.AddToCurrentSpace();
        }

        private List<Entity> DrawAlignment(Canal data)
        {
            List<Entity> AlignmentEntities = new List<Entity>();
            Polyline alignment = NoDraw.Pline(data.AlignmentDictionaryAngle.Select(x => x.Key));
            foreach (KeyValuePair<Point3d, double> item in data.AlignmentDictionaryAngle)
            {
                if (item.Key.Z != 0)
                {
                    DBText mid = new DBText()
                    {
                        Rotation = item.Value + (Math.PI / 2),
                        TextString = item.Key.Z.ToString("N2"),
                        Justify = AttachmentPoint.MiddleCenter,
                        AlignmentPoint = item.Key.Flatten(),
                        Height = 4,
                        TextStyleId = LABEL_STYLE,
                        Layer = "TEXT-ELEVATION-ALIGNMENT"

                    };
                    AlignmentEntities.Add(mid);
                }

            }
            AlignmentEntities.Add(alignment);
            return AlignmentEntities;

        }

        private static List<Entity> DrawPolygon(Canal data)
        {
            List<Entity> PolygonEntities = new List<Entity>();
            Polyline polygon = NoDraw.Pline(ConvertP3DC2List(data.PolygonNodes));
            polygon.Linetype = "DASHDOT";
            foreach (Point3d item in data.PolygonNodes)
            {
                Circle circle = NoDraw.Circle(item, 1);
                PolygonEntities.Add(circle);
            }
            PolygonEntities.Add(polygon);
            return PolygonEntities;
        }

        private static List<Entity> DrawTanggulDasar(Canal data)
        {
            List<Entity> PolygonEntities = new List<Entity>();
            foreach (Nodes current in data.nodes)
            {
                LinkedListNode<Nodes> currentNodes = data.nodes.Find(current);
                Nodes next = currentNodes.Next?.Value;

                if (next != null)
                {
                    IEnumerable<KeyValuePair<Point3d, string>> tc = current.NodePoint.Where(x => x.Value.Contains("T1"));
                    IEnumerable<KeyValuePair<Point3d, string>> dc = current.NodePoint.Where(x => x.Value.Contains("D1"));
                    IEnumerable<KeyValuePair<Point3d, string>> tn = next.NodePoint.Where(x => x.Value.Contains("T1"));
                    IEnumerable<KeyValuePair<Point3d, string>> dn = next.NodePoint.Where(x => x.Value.Contains("D1"));
                    if (tc.Count() % 2 == 0 && tn.Count() % 2 == 0 && tc.Count() > 0 && tn.Count() > 0)
                    {
                        Point3d cf = tc.First().Key;
                        Point3d cl = tc.Last().Key;
                        Point3d nf = tn.First().Key;
                        Point3d nl = tn.Last().Key;
                        Line l = NoDraw.Line(cf, nf);
                        Line ll = NoDraw.Line(cl, nl);
                        l.Layer = "BREAK-LINE-TANGGUL";
                        ll.Layer = "BREAK-LINE-TANGGUL";
                        PolygonEntities.Add(l);
                        PolygonEntities.Add(ll);
                    }

                    if (dc.Count() % 2 == 0 && dn.Count() % 2 == 0 && dc.Count() > 0 && dn.Count() > 0)
                    {
                        Point3d cf = dc.First().Key;
                        Point3d cl = dc.Last().Key;
                        Point3d nf = dn.First().Key;
                        Point3d nl = dn.Last().Key;
                        Line l = NoDraw.Line(cf, nf);
                        Line ll = NoDraw.Line(cl, nl);
                        l.Layer = "BREAK-LINE-DASAR";
                        ll.Layer = "BREAK-LINE-DASAR";
                        PolygonEntities.Add(l);
                        PolygonEntities.Add(ll);
                    }

                }
            }
            return PolygonEntities;
        }

        private List<Entity> DrawStructure(Canal data)
        {
            List<Entity> StructureEntities = new List<Entity>();
            foreach (Nodes item in data.nodes)
            {
                if (item.StructureType != Bangunan.NoStructure)
                {
                    BlockReference x = NoDraw.Insert(Structures[(int)item.StructureType], item.AsPoint.Flatten(), item.angle + (Math.PI / 2));
                    x.Layer = "BANGUNAN";
                    StructureEntities.Add(x);
                }
                
                
            }

            return StructureEntities;
        }

        private List<Entity> DrawCross(LinkedList<Nodes> data)
        {
            List<Entity> CrossEntities = new List<Entity>();
            foreach (Nodes item in data)
            {
                Line crossline = NoDraw.Line(item.NodePoint.First().Key.Flatten(), item.NodePoint.Last().Key.Flatten());
                crossline.Linetype = "DASHED";
                crossline.LinetypeScale = 20;
                Vector3d vec = crossline.Delta.GetNormal() * 20;
                crossline.StartPoint = crossline.StartPoint - vec;
                foreach (Node val in item.NodeList)
                {
                    DBText patok = new DBText()
                    {
                        Rotation = item.angle + (Math.PI / 2),
                        TextString = item.Patok,
                        Justify = AttachmentPoint.BaseCenter,
                        AlignmentPoint = new Point3d(crossline.StartPoint.X - 2, crossline.StartPoint.Y + 2, 0),
                        Height = 4,
                        TextStyleId = LABEL_STYLE,
                        Layer = "TEXT-PATOK"

                    };
                    CrossEntities.Add(patok);

                    DBText desc = new DBText()
                    {
                        Rotation = item.angle + (Math.PI / 2),
                        TextString = val.Elevation.ToString("N2"),
                        Justify = AttachmentPoint.MiddleCenter,
                        AlignmentPoint = val.Point3d.Flatten(),
                        Height = 4,
                        TextStyleId = LABEL_STYLE
                    };

                    if (val.Description.Contains("T"))
                    {
                        desc.Layer = "TEXT-ELEVATION-TANGGUL";
                    }
                    else if (val.Description.Contains("D"))
                    {
                        desc.Layer = "TEXT-ELEVATION-DASAR";
                    }
                    else if (val.Distance == 0)
                    {
                        desc.Layer = "TEXT-ELEVATION-PATOK";
                    }
                    else
                    {
                        desc.Layer = "TEXT-ELEVATION";
                    }
                    CrossEntities.Add(desc);

                }
                CrossEntities.Add(crossline);


            }
            return CrossEntities;
        }
    }

}