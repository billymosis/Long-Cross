using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using CsvHelper;
using Dreambuild;
using Dreambuild.AutoCAD;
using Dreambuild.AutoCAD.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PLC
{
    public class CodePackTest
    {

        [CommandMethod("qwe")]
        public static void qwe()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // First Polyline
            Polyline pl1 = NoDraw.Rectang(new Point3d(0, 0, 0), new Point3d(30, 30, 0));
            Line pl2 = NoDraw.Line(new Point3d(-10, 10, 0), new Point3d(15, 10, 0));

            // Intersection Points Collection 
            Point3dCollection intPoints = new Point3dCollection();
            pl1.IntersectWith(pl2, Intersect.OnBothOperands, intPoints, IntPtr.Zero, IntPtr.Zero);
            DBObjectCollection x = pl2.GetSplitCurves(intPoints);
            ed.WriteMessage("x: " + x.Count);
            foreach (Line item in x)
            {
                Point3d y = item.StartPoint;
                ed.WriteMessage("min:" + y.ToString() + "\n");
                item.AddToCurrentSpace();
            }
            pl1.AddToCurrentSpace();
            pl2.AddToCurrentSpace();
            ed.WriteMessage("\nNo. Of Intersection Points: {0}", intPoints.Count);
            for (int i = 0; i < intPoints.Count; i++)
            {
                ed.WriteMessage("\n Point No. {0} X:{1} Y:{2}", i.ToString(), intPoints[i].X, intPoints[i].Y);
            }
        }

        #region Commands that you can provide out of the box in your application

        /// <summary>
        /// View or edit custom dictionaries of DWG.
        /// </summary>
        [CommandMethod("ViewGlobalDict")]
        public static void ViewGlobalDict()
        {
            DictionaryViewer dv = new DictionaryViewer(
                CustomDictionary.GetDictionaryNames,
                CustomDictionary.GetEntryNames,
                CustomDictionary.GetValue,
                CustomDictionary.SetValue
            );
            Application.ShowModalWindow(dv);
        }

        /// <summary>
        /// View or edit custom dictionaries of entity.
        /// </summary>
        [CommandMethod("ViewObjectDict")]
        public static void ViewObjectDict()
        {
            ObjectId id = Interaction.GetEntity("\nSelect entity");
            if (id == ObjectId.Null)
            {
                return;
            }
            DictionaryViewer dv = new DictionaryViewer(  // Currying
                () => CustomObjectDictionary.GetDictionaryNames(id),
                dict => CustomObjectDictionary.GetEntryNames(id, dict),
                (dict, key) => CustomObjectDictionary.GetValue(id, dict, key),
                (dict, key, value) => CustomObjectDictionary.SetValue(id, dict, key, value)
            );
            Application.ShowModalWindow(dv);
        }

        /// <summary>
        /// Eliminates zero-length polylines.
        /// </summary>
        [CommandMethod("PolyClean0", CommandFlags.UsePickSet)]
        public static void PolyClean0()
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect polyline", "LWPOLYLINE");
            int n = 0;
            ids.QForEach<Polyline>(poly =>
            {
                if (poly.Length == 0)
                {
                    poly.Erase();
                    n++;
                }
            });
            Interaction.WriteLine("{0} eliminated.", n);
        }

        /// <summary>
        /// Removes duplicate vertices on polyline.
        /// </summary>
        [CommandMethod("PolyClean", CommandFlags.UsePickSet)]
        public static void PolyClean()
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect polyline", "LWPOLYLINE");
            int m = 0;
            int n = 0;
            ids.QForEach<Polyline>(poly =>
            {
                int count = Algorithms.PolyClean_RemoveDuplicatedVertex(poly);
                if (count > 0)
                {
                    m++;
                    n += count;
                }
            });
            Interaction.WriteLine("{1} vertex removed from {0} polyline.", m, n);
        }

        private static double _polyClean2Epsilon = 1;

        /// <summary>
        /// Removes vertices close to others on polyline.
        /// </summary>
        [CommandMethod("PolyClean2", CommandFlags.UsePickSet)]
        public static void PolyClean2()
        {
            double epsilon = Interaction.GetValue("\nEpsilon", _polyClean2Epsilon);
            if (double.IsNaN(epsilon))
            {
                return;
            }
            _polyClean2Epsilon = epsilon;

            ObjectId[] ids = Interaction.GetSelection("\nSelect polyline", "LWPOLYLINE");
            int m = 0;
            int n = 0;
            ids.QForEach<Polyline>(poly =>
            {
                int count = Algorithms.PolyClean_ReducePoints(poly, epsilon);
                if (count > 0)
                {
                    m++;
                    n += count;
                }
            });
            Interaction.WriteLine("{1} vertex removed from {0} polyline.", m, n);
        }

        /// <summary>
        /// Fits arc segs of polyline with line segs.
        /// </summary>
        [CommandMethod("PolyClean3", CommandFlags.UsePickSet)]
        public static void PolyClean3()
        {
            double value = Interaction.GetValue("\nNumber of segs to fit an arc, 0 for smart determination", 0);
            if (double.IsNaN(value))
            {
                return;
            }
            int n = (int)value;

            ObjectId[] ids = Interaction.GetSelection("\nSelect polyline", "LWPOLYLINE");
            List<Polyline> entsToAdd = new List<Polyline>();
            ids.QForEach<Polyline>(poly =>
            {
                IEnumerable<Point3d> pts = poly.GetPolylineFitPoints(n);
                Polyline poly1 = NoDraw.Pline(pts);
                poly1.Layer = poly.Layer;
                try
                {
                    poly1.ConstantWidth = poly.ConstantWidth;
                }
                catch
                {
                }
                poly1.XData = poly.XData;
                poly.Erase();
                entsToAdd.Add(poly1);
            });
            entsToAdd.ToArray().AddToCurrentSpace();
            Interaction.WriteLine("{0} handled.", entsToAdd.Count);
        }

        /// <summary>
        /// Regulates polyline direction.
        /// </summary>
        [CommandMethod("PolyClean4", CommandFlags.UsePickSet)]
        public static void PolyClean4()
        {
            double value = Interaction.GetValue("\nDirection：1-R to L；2-B to T；3-L to R；4-T to B");
            if (double.IsNaN(value))
            {
                return;
            }
            int n = (int)value;
            if (!new int[] { 1, 2, 3, 4 }.Contains(n))
            {
                return;
            }
            Algorithms.Direction dir = (Algorithms.Direction)n;

            ObjectId[] ids = Interaction.GetSelection("\nSelect polyline", "LWPOLYLINE");
            int m = 0;
            ids.QForEach<Polyline>(poly =>
            {
                if (Algorithms.PolyClean_SetTopoDirection(poly, dir))
                {
                    m++;
                }
            });
            Interaction.WriteLine("{0} handled.", m);
        }

        /// <summary>
        /// Removes unnecessary colinear vertices on polyline.
        /// </summary>
        [CommandMethod("PolyClean5", CommandFlags.UsePickSet)]
        public static void PolyClean5()
        {
            Interaction.WriteLine("Not implemented yet");
            ObjectId[] ids = Interaction.GetSelection("\nSelect polyline", "LWPOLYLINE");
            ids.QForEach<Polyline>(poly =>
            {
                Algorithms.PolyClean_RemoveColinearPoints(poly);
            });
        }

        /// <summary>
        /// Breaks polylines at their intersecting points.
        /// </summary>
        [CommandMethod("PolySplit", CommandFlags.UsePickSet)]
        public static void PolySplit()
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect polyline", "LWPOLYLINE");
            List<Polyline> newPolys = new List<Polyline>();
            ProgressMeter pm = new ProgressMeter();
            pm.Start("Processing...");
            pm.SetLimit(ids.Length);
            ids.QOpenForWrite<Polyline>(list =>
            {
                foreach (Polyline poly in list)
                {
                    Point3dCollection intersectPoints = new Point3dCollection();
                    foreach (Polyline poly1 in list)
                    {
                        if (poly1 != poly)
                        {
                            poly.IntersectWith(poly1, Intersect.OnBothOperands, intersectPoints, IntPtr.Zero, IntPtr.Zero);
                        }
                    }
                    double[] ipParams = intersectPoints
                        .Cast<Point3d>()
                        .Select(ip => poly.GetParamAtPointX(ip))
                        .OrderBy(param => param)
                        .ToArray();
                    if (intersectPoints.Count > 0)
                    {
                        DBObjectCollection curves = poly.GetSplitCurves(new DoubleCollection(ipParams));
                        foreach (object curve in curves)
                        {
                            newPolys.Add(curve as Polyline);
                        }
                    }
                    else // mod 20130227 Add to newPolys regardless of whether an intersection exists, otherwise dangling lines would be gone.
                    {
                        newPolys.Add(poly.Clone() as Polyline);
                    }
                    pm.MeterProgress();
                }
            });
            pm.Stop();
            if (newPolys.Count > 0)
            {
                newPolys.ToArray().AddToCurrentSpace();
                ids.QForEach(entity => entity.Erase());
            }
            Interaction.WriteLine("Broke {0} to {1}.", ids.Length, newPolys.Count);
        }

        private static double _polyTrimExtendEpsilon = 20;

        /// <summary>
        /// Handles polylines that are by a small distance longer than, shorter than, or mis-intersecting with each other.
        /// </summary>
        [CommandMethod("PolyTrimExtend", CommandFlags.UsePickSet)]
        public static void PolyTrimExtend() // mod 20130228
        {
            double epsilon = Interaction.GetValue("\nEpsilon", _polyTrimExtendEpsilon);
            if (double.IsNaN(epsilon))
            {
                return;
            }
            _polyTrimExtendEpsilon = epsilon;

            List<string> visibleLayers = DbHelper
                .GetAllLayerIds()
                .QOpenForRead<LayerTableRecord>()
                .Where(layer => !layer.IsHidden && !layer.IsFrozen && !layer.IsOff)
                .Select(layer => layer.Name)
                .ToList();

            ObjectId[] ids = Interaction
                .GetSelection("\nSelect polyline", "LWPOLYLINE")
                .QWhere(pline => visibleLayers.Contains(pline.Layer) && pline.Visible)
                .ToArray(); // newly 20130729

            ProgressMeter pm = new ProgressMeter();
            pm.Start("Processing...");
            pm.SetLimit(ids.Length);
            ids.QOpenForWrite<Polyline>(list =>
            {
                foreach (Polyline poly in list)
                {
                    int[] indices = { 0, poly.NumberOfVertices - 1 };
                    foreach (int index in indices)
                    {
                        Point3d end = poly.GetPoint3dAt(index);
                        foreach (Polyline poly1 in list)
                        {
                            if (poly1 != poly)
                            {
                                Point3d closest = poly1.GetClosestPointTo(end, false);
                                double dist = closest.DistanceTo(end);
                                double dist1 = poly1.StartPoint.DistanceTo(end);
                                double dist2 = poly1.EndPoint.DistanceTo(end);

                                double distance = poly1.GetDistToPoint(end);
                                if (poly1.GetDistToPoint(end) > 0)
                                {
                                    if (dist1 <= dist2 && dist1 <= dist && dist1 < epsilon)
                                    {
                                        poly.SetPointAt(index, new Point2d(poly1.StartPoint.X, poly1.StartPoint.Y));
                                    }
                                    else if (dist2 <= dist1 && dist2 <= dist && dist2 < epsilon)
                                    {
                                        poly.SetPointAt(index, new Point2d(poly1.EndPoint.X, poly1.EndPoint.Y));
                                    }
                                    else if (dist <= dist1 && dist <= dist2 && dist < epsilon)
                                    {
                                        poly.SetPointAt(index, new Point2d(closest.X, closest.Y));
                                    }
                                }
                            }
                        }
                    }
                    pm.MeterProgress();
                }
            });
            pm.Stop();
        }

        /// <summary>
        /// Saves selection for later use.
        /// </summary>
        [CommandMethod("SaveSelection", CommandFlags.UsePickSet)]
        public static void SaveSelection()
        {
            ObjectId[] ids = Interaction.GetPickSet();
            if (ids.Length == 0)
            {
                Interaction.WriteLine("No entity selected.");
                return;
            }
            string name = Interaction.GetString("\nSelection name");
            if (name == null)
            {
                return;
            }
            if (CustomDictionary.GetValue("Selections", name) != string.Empty)
            {
                Interaction.WriteLine("Selection with the same name already exists.");
                return;
            }
            string[] handles = ids.QSelect(entity => entity.Handle.Value.ToString()).ToArray();
            string dictValue = string.Join("|", handles);
            CustomDictionary.SetValue("Selections", name, dictValue);
        }

        /// <summary>
        /// Loads previously saved selection.
        /// </summary>
        [CommandMethod("LoadSelection")]
        public static void LoadSelection()
        {
            string name = Gui.GetChoice("Which selection to load?", CustomDictionary.GetEntryNames("Selections").ToArray());
            if (name == string.Empty)
            {
                return;
            }
            string dictValue = CustomDictionary.GetValue("Selections", name);
            List<Handle> handles = dictValue.Split('|').Select(value => new Handle(Convert.ToInt64(value))).ToList();
            List<ObjectId> ids = new List<ObjectId>();
            handles.ForEach(value =>
            {
                ObjectId id = ObjectId.Null;
                if (HostApplicationServices.WorkingDatabase.TryGetObjectId(value, out id))
                {
                    ids.Add(id);
                }
            });
            Interaction.SetPickSet(ids.ToArray());
        }

        /// <summary>
        /// Converts MText to Text.
        /// </summary>
        [CommandMethod("MT2DT", CommandFlags.UsePickSet)]
        public static void MT2DT() // newly 20130815
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect MText", "MTEXT");
            DBText[] mts = ids.QOpenForRead<MText>().Select(mt =>
            {
                DBText dt = NoDraw.Text(mt.Text, mt.TextHeight, mt.Location, mt.Rotation, false, mt.TextStyleName);
                dt.Layer = mt.Layer;
                return dt;
            }).ToArray();
            ids.QForEach(mt => mt.Erase());
            mts.AddToCurrentSpace();
        }

        /// <summary>
        /// Converts Text to MText.
        /// </summary>
        [CommandMethod("DT2MT", CommandFlags.UsePickSet)]
        public static void DT2MT() // newly 20130815
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect Text", "TEXT");
            MText[] dts = ids.QOpenForRead<DBText>().Select(dt =>
            {
                MText mt = NoDraw.MText(dt.TextString, dt.Height, dt.Position, dt.Rotation, false);
                mt.Layer = dt.Layer;
                return mt;
            }).ToArray();
            ids.QForEach(dt => dt.Erase());
            dts.AddToCurrentSpace();
        }

        /// <summary>
        /// Shows a rectangle indicating the extents of selected entities.
        /// </summary>
        [CommandMethod("ShowExtents", CommandFlags.UsePickSet)]
        public static void ShowExtents() // newly 20130815
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect entity");
            Extents3d extents = ids.GetExtents();
            ObjectId rectId = Draw.Rectang(extents.MinPoint, extents.MaxPoint);
            Interaction.GetString("\nPress ENTER to exit...");
            rectId.QOpenForWrite(rect => rect.Erase());
        }

        /// <summary>
        /// Closes a polyline by adding a vertex at the same position as the start rather than setting IsClosed=true.
        /// </summary>
        [CommandMethod("ClosePolyline", CommandFlags.UsePickSet)]
        public static void ClosePolyline()
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect polyline", "LWPOLYLINE");
            if (ids.Length == 0)
            {
                return;
            }
            if (Interaction.TaskDialog(
                mainInstruction: ids.Count().ToString() + " polyline(s) selected. Make sure what you select is correct.",
                yesChoice: "Yes, I promise.",
                noChoice: "No, I want to double check.",
                title: "AutoCAD",
                content: "All polylines in selection will be closed.",
                footer: "Abuse can mess up the drawing.",
                expanded: "Commonly used before export."))
            {
                //polys.QForEach(poly => LogManager.Write((poly as Polyline).Closed));
                ids.QForEach<Polyline>(poly =>
                {
                    if (poly.StartPoint.DistanceTo(poly.EndPoint) > 0)
                    {
                        poly.AddVertexAt(poly.NumberOfVertices, poly.StartPoint.ToPoint2d(), 0, 0, 0);
                    }
                });
            }
        }

        /// <summary>
        /// Detects non-simple polylines that intersect with themselves.
        /// </summary>
        [CommandMethod("DetectSelfIntersection")]
        public static void DetectSelfIntersection() // mod 20130202
        {
            ObjectId[] ids = QuickSelection.SelectAll("LWPOLYLINE").ToArray();
            ProgressMeter meter = new ProgressMeter();
            meter.Start("Detecting...");
            meter.SetLimit(ids.Length);
            List<ObjectId> results = ids.QWhere(pline =>
            {
                bool result = (pline as Polyline).IsSelfIntersecting();
                meter.MeterProgress();
                System.Windows.Forms.Application.DoEvents();
                return result;
            }).ToList();
            meter.Stop();
            if (results.Count() > 0)
            {
                Interaction.WriteLine("{0} detected.", results.Count());
                Interaction.ZoomHighlightView(results);
            }
            else
            {
                Interaction.WriteLine("0 detected.");
            }
        }

        /// <summary>
        /// Finds entity by handle value.
        /// </summary>
        [CommandMethod("ShowObject")]
        public static void ShowObject()
        {
            ObjectId[] ids = QuickSelection.SelectAll().ToArray();
            double handle1 = Interaction.GetValue("Handle of entity");
            if (double.IsNaN(handle1))
            {
                return;
            }
            long handle2 = Convert.ToInt64(handle1);
            ObjectId id = HostApplicationServices.WorkingDatabase.GetObjectId(false, new Handle(handle2), 0);
            ObjectId[] col = new ObjectId[] { id };
            Interaction.HighlightObjects(col);
            Interaction.ZoomObjects(col);
        }

        /// <summary>
        /// Shows the shortest line to link given point to existing lines, polylines, or arcs.
        /// </summary>
        [CommandMethod("PolyLanding")]
        public static void PolyLanding()
        {
            ObjectId[] ids = QuickSelection.SelectAll("*LINE,ARC").ToArray();
            List<ObjectId> landingLineIds = new List<ObjectId>();
            while (true)
            {
                Point3d p = Interaction.GetPoint("\nSpecify a point");
                if (p.IsNull())
                {
                    break;
                }
                Point3d[] landings = ids.QSelect(entity => (entity as Curve).GetClosestPointTo(p, false)).ToArray();
                double minDist = landings.Min(point => point.DistanceTo(p));
                Point3d landing = landings.First(point => point.DistanceTo(p) == minDist);
                Interaction.WriteLine("Shortest landing distance of point ({0:0.00},{1:0.00}) is {2:0.00}.", p.X, p.Y, minDist);
                landingLineIds.Add(Draw.Line(p, landing));
            }
            landingLineIds.QForEach(entity => entity.Erase());
        }

        /// <summary>
        /// Shows vertics info of a polyline.
        /// </summary>
        [CommandMethod("PolylineInfo")]
        public static void PolylineInfo() // mod by WY 20130202
        {
            ObjectId id = Interaction.GetEntity("\nSpecify a polyline", typeof(Polyline));
            if (id == ObjectId.Null)
            {
                return;
            }
            Polyline poly = id.QOpenForRead<Polyline>();
            for (int i = 0; i <= poly.EndParam; i++)
            {
                Interaction.WriteLine("[Point {0}] coord: {1}; bulge: {2}", i, poly.GetPointAtParameter(i), poly.GetBulgeAt(i));
            }
            List<ObjectId> txtIds = new List<ObjectId>();
            double height = poly.GeometricExtents.MaxPoint.DistanceTo(poly.GeometricExtents.MinPoint) / 50.0;
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                txtIds.Add(Draw.MText(i.ToString(), height, poly.GetPointAtParameter(i), 0, true));
            }
            Interaction.GetString("\nPress ENTER to exit");
            txtIds.QForEach(mt => mt.Erase());
        }

        /// <summary>
        /// Selects entities on given layer.
        /// </summary>
        [CommandMethod("SelectByLayer")]
        public static void SelectByLayer()
        {
            string[] availableLayerNames = DbHelper.GetAllLayerNames();
            string[] selectedLayerNames = Gui.GetChoices("Specify layers", availableLayerNames);
            if (selectedLayerNames.Length < 1)
            {
                return;
            }

            ObjectId[] ids = QuickSelection
                .SelectAll(FilterList.Create().Layer(selectedLayerNames))
                .ToArray();

            Interaction.SetPickSet(ids);
        }

        /// <summary>
        /// Marks layer names of selected entities on the drawing by MText.
        /// </summary>
        [CommandMethod("ShowLayerName")]
        public static void ShowLayerName()
        {
            double height = 10;
            string[] range = { "By entities", "By layers" };
            int result = Gui.GetOption("Choose one way", range);
            if (result == -1)
            {
                return;
            }
            ObjectId[] ids;
            if (result == 0)
            {
                ids = Interaction.GetSelection("\nSelect entities");
                ids
                    .QWhere(entity => !entity.Layer.Contains("_Label"))
                    .QSelect(entity => entity.Layer)
                    .Distinct()
                    .Select(layer => $"{layer}_Label")
                    .ForEach(labelLayer => DbHelper.GetLayerId(labelLayer));
            }
            else
            {
                string[] layers = DbHelper.GetAllLayerNames().Where(layer => !layer.Contains("_Label")).ToArray();
                string layerName = Gui.GetChoice("Select a layer", layers);
                ids = QuickSelection
                    .SelectAll(FilterList.Create().Layer(layerName))
                    .ToArray();

                DbHelper.GetLayerId($"{layerName}_Label");
            }
            List<MText> texts = new List<MText>();
            ids.QForEach<Entity>(entity =>
            {
                string layerName = entity.Layer;
                if (!layerName.Contains("_Label"))
                {
                    Point3d center = entity.GetCenter();
                    MText text = NoDraw.MText(layerName, height, center, 0, true);
                    text.Layer = $"{layerName}_Label";
                    texts.Add(text);
                }
            });
            texts.ToArray().AddToCurrentSpace();
        }

        /// <summary>
        /// Inspects object in property palette.
        /// </summary>
        [CommandMethod("InspectObject")]
        public static void InspectObject()
        {
            ObjectId id = Interaction.GetEntity("\nSelect objects");
            if (id.IsNull)
            {
                return;
            }
            Gui.PropertyPalette(id.QOpenForRead());
        }

        #endregion

        #region Commands just for showing API usage

        [CommandMethod("TestBasicDrawing")]
        public void TestBasicDrawing()
        {
            // TODO: author this test.
        }

        [CommandMethod("TestTransform")]
        public void TestTransform()
        {
            // TODO: author this test.
        }

        [CommandMethod("TestBlock")]
        public void TestBlock()
        {
            ObjectId bId = Draw.Block(QuickSelection.SelectAll(), "test");
            // TODO: complete this test.
        }

        [CommandMethod("TestCustomDictionary")]
        public void TestCustomDictionary()
        {
            CustomDictionary.SetValue("dict1", "A", "apple");
            CustomDictionary.SetValue("dict1", "B", "orange");
            CustomDictionary.SetValue("dict1", "A", "banana");
            CustomDictionary.SetValue("dict2", "A", "peach");
            foreach (string dict in CustomDictionary.GetDictionaryNames())
            {
                Interaction.WriteLine(dict);
            }
            Interaction.WriteLine(CustomDictionary.GetValue("dict1", "A"));
        }

        [CommandMethod("TestDimension")]
        public void TestDimension()
        {
            Point3d a = Interaction.GetPoint("\nPoint 1");
            Point3d b = Interaction.GetPoint("\nPoint 2");
            Point3d c = Interaction.GetPoint("\nPoint of label");
            Draw.Dimlin(a, b, c);
        }

        [CommandMethod("TestTaskDialog")]
        public void TestTaskDialog()
        {
            if (Interaction.TaskDialog("I prefer", "Work", "Life", "WLB poll", "You have to choose one...", "...and only choose one.", "No other choices."))
            {
                Interaction.WriteLine("You are promoted.");
            }
            else
            {
                Interaction.WriteLine("You are fired.");
            }
        }

        [CommandMethod("TestZoom")]
        public void TestZoom()
        {
            Interaction.ZoomExtents();
        }

        [CommandMethod("TestWipe")]
        public void TestWipe()
        {
            ObjectId id = Interaction.GetEntity("\nEntity");
            Draw.Wipeout(id);
        }

        [CommandMethod("TestRegion")]
        public void TestRegion()
        {
            ObjectId id = Interaction.GetEntity("\nEntity");
            Draw.Region(id);
            Point3d point = Interaction.GetPoint("\nPick one point");
            Draw.Boundary(point, BoundaryType.Region);
        }

        [CommandMethod("TestOffset")]
        public void TestOffset()
        {
            ObjectId id = Interaction.GetEntity("\nPolyline");
            Polyline poly = id.QOpenForRead<Polyline>();
            double value = Interaction.GetValue("\nOffset");
            poly.OffsetPoly(Enumerable.Range(0, poly.NumberOfVertices).Select(index => value).ToArray()).AddToCurrentSpace();
        }

        [CommandMethod("TestSelection")]
        public void TestSelection()
        {
            Point3d point = Interaction.GetPoint("\nPoint");
            double value = Interaction.GetDistance("\nSize");
            Vector3d size = new Vector3d(value, value, 0);
            ObjectId[] ids = Interaction.GetWindowSelection(point - size, point + size);
            Interaction.WriteLine("{0} entities selected.", ids.Count());
        }

        [CommandMethod("TestGraph")]
        public void TestGraph()
        {
            GraphOption option = new GraphOption { xDelta = 20, yDelta = 0.5, yRatio = 0.5, SampleCount = 500 };
            GraphPlotter graphPlotter = new GraphPlotter(option);
            graphPlotter.Plot(Math.Sin, new Interv(5, 102));
            graphPlotter.Plot(x => Math.Cos(x) + 1, new Interv(10, 90), 3);
            ObjectId graph = graphPlotter.GetGraphBlock();
            BlockReference blockReference = new BlockReference(Point3d.Origin, graph);
            Point3d first = Interaction.GetPoint("\nSpecify extent point 1");
            Interaction.InsertScalingEntity(blockReference, first, "\nSpecify extent point 2");
        }

        [CommandMethod("TestJigDrag")]
        public void TestJigDrag()
        {
            Circle circle = new Circle(new Point3d(), Vector3d.ZAxis, 10.0);
            PromptResult promptResult = Interaction.StartDrag("\nCenter:", result =>
            {
                circle.Center = result.Value;
                return circle;
            });
            if (promptResult.Status != PromptStatus.OK)
            {
                return;
            }
            promptResult = Interaction.StartDrag(new JigPromptDistanceOptions("\nRadius:"), (PromptDoubleResult result) =>
            {
                circle.Radius = result.Value == 0.0 ? 1e-6 : result.Value;
                return circle;
            });
            if (promptResult.Status == PromptStatus.OK)
            {
                circle.AddToCurrentSpace();
            }
        }

        [CommandMethod("TestQOpen")]
        public void TestQOpen()
        {
            ObjectId[] ids = QuickSelection.SelectAll("LWPOLYLINE").QWhere(pline => pline.GetCode() == "parcel").ToArray();
            ids.QForEach<Polyline>(poly =>
            {
                poly.ConstantWidth = 2;
                poly.ColorIndex = 0;
            });
        }

        [CommandMethod("TestSetLayer")]
        public void TestSetLayer()
        {
            ObjectId lineId = Draw.Line(Point3d.Origin, Point3d.Origin + Vector3d.XAxis);
            lineId.SetLayer("aaa");
        }

        [CommandMethod("TestGroup")]
        public void TestGroup()
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect entities");
            ids.Group();
            DBDictionary groupDict = HostApplicationServices.WorkingDatabase.GroupDictionaryId.QOpenForRead<DBDictionary>();
            Interaction.WriteLine("{0} groups", groupDict.Count);
        }

        [CommandMethod("TestUngroup")]
        public void TestUngroup()
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect entities");
            Modify.Ungroup(ids);
            DBDictionary groupDict = HostApplicationServices.WorkingDatabase.GroupDictionaryId.QOpenForRead<DBDictionary>();
            Interaction.WriteLine("{0} groups.", groupDict.Count);
        }

        [CommandMethod("TestHatch")]
        public void TestHatch()
        {
            Draw.Hatch(new[] { new Point3d(0, 0, 0), new Point3d(100, 0, 0), new Point3d(0, 100, 0) });
        }

        [CommandMethod("TestHatch2")]
        public void TestHatch2()
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect entities");
            Draw.Hatch(ids);
        }

        [CommandMethod("TestArc")]
        public void TestArc()
        {
            Point3d point1 = Interaction.GetPoint("\nStart");
            Draw.Circle(point1, 5);
            Point3d point2 = Interaction.GetPoint("\nMid");
            Draw.Circle(point2, 5);
            Point3d point3 = Interaction.GetPoint("\nEnd");
            Draw.Circle(point3, 5);
            Draw.Arc3P(point1, point2, point3);
        }

        [CommandMethod("TestArc2")]
        public void TestArc2()
        {
            Point3d start = Interaction.GetPoint("\nStart");
            Draw.Circle(start, 5);
            Point3d center = Interaction.GetPoint("\nCenter");
            Draw.Circle(center, 5);
            double angle = Interaction.GetValue("\nAngle");
            Draw.ArcSCA(start, center, angle);
        }

        [CommandMethod("TestEllipse")]
        public void TestEllipse()
        {
            Point3d center = Interaction.GetPoint("\nCenter");
            Draw.Circle(center, 5);
            Point3d endX = Interaction.GetPoint("\nEnd of one axis");
            Draw.Circle(endX, 5);
            double radiusY = Interaction.GetValue("\nRadius of another axis");
            Draw.Ellipse(center, endX, radiusY);
        }

        [CommandMethod("TestSpline")]
        public void TestSpline()
        {
            List<Point3d> points = new List<Point3d>();
            while (true)
            {
                Point3d point = Interaction.GetPoint("\nSpecify a point");
                if (point.IsNull())
                {
                    break;
                }
                points.Add(point);
                Draw.Circle(point, 5);
            }
            Draw.SplineCV(points.ToArray(), true);
        }

        [CommandMethod("TestPolygon")]
        public void TestPolygon()
        {
            int n;
            while (true)
            {
                double d = Interaction.GetValue("\nNumber of edges");
                if (double.IsNaN(d))
                {
                    return;
                }
                n = (int)d;
                if (n > 2)
                {
                    break;
                }
            }
            Point3d center = Interaction.GetPoint("\nCenter");
            Draw.Circle(center, 5);
            Point3d end = Interaction.GetPoint("\nOne vertex");
            Draw.Circle(end, 5);
            Draw.Polygon(n, center, end);
        }

        [CommandMethod("ViewSpline")]
        public void ViewSpline()
        {
            ObjectId id = Interaction.GetEntity("\nSelect a spline", typeof(Spline));
            Spline spline = id.QOpenForRead<Spline>();
            DoubleCollection knots = spline.NurbsData.GetKnots();
            List<Point3d> knotPoints = knots.Cast<double>().Select(k => spline.GetPointAtParam(k)).ToList();
            knotPoints.ForEach(p => Draw.Circle(p, 5));
        }

        [CommandMethod("TestText")]
        public void TestText()
        {
            Modify.TextStyle("Tahoma", 100, 5 * Math.PI / 180, 0.8);
            Draw.Text("FontAbc", 100, Point3d.Origin, 0, true);
        }

        [CommandMethod("TestTable")]
        public void TestTable()
        {
            List<List<string>> contents = new List<List<string>>
            {
                new List<string>{ "1", "4", "9" },
                new List<string>{ "1", "8", "27" },
                new List<string>{ "1", "16", "81" },
            };
            Draw.Table(Point3d.Origin, "Numbers", contents, 5, 20, 2.5);
        }

        //[CommandMethod("PythonConsole")]
        //public void PythonConsole()
        //{
        //    var pcw = new PyConsole();
        //    pcw.Show();
        //}

        [CommandMethod("TestLayout")]
        public void TestLayout()
        {
            // TODO: verify the layout API change.
            Layout layout = LayoutManager.Current.CreateLayout("TestLayout").QOpenForRead<Layout>();
            LayoutManager.Current.CurrentLayout = "TestLayout";
            ObjectIdCollection vps = layout.GetViewports();
            if (vps.Count > 1)
            {
                ObjectId vpId = vps[1];
                Layouts.SetViewport(vpId, 100, 100, new Point3d(80, 80, 0), Point3d.Origin, 1000);
            }
        }

        [CommandMethod("TestMeasure")]
        public void TestMeasure()
        {
            ObjectId id = Interaction.GetEntity("\nSelect curve");
            Curve cv = id.QOpenForRead<Curve>();
            double length = Interaction.GetValue("\nInterval");
            Draw.Measure(cv, length, new DBPoint());
        }

        [CommandMethod("TestDivide")]
        public void TestDivide()
        {
            ObjectId id = Interaction.GetEntity("\nSelect curve");
            Curve cv = id.QOpenForRead<Curve>();
            int num = (int)Interaction.GetValue("\nNumbers");
            Draw.Divide(cv, num, new DBPoint());
        }

        [CommandMethod("TestBoundary")]
        public void TestBoundary()
        {
            Point3d point = Interaction.GetPoint("\nPick one point");
            Draw.Boundary(point, BoundaryType.Polyline);
        }

        [CommandMethod("TestHatch3")]
        public void TestHatch3()
        {
            Point3d seed = Interaction.GetPoint("\nPick one point");
            Draw.Hatch("SOLID", seed);
        }

        [CommandMethod("TestHatch4")]
        public void TestHatch4()
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect entities");
            Entity[] ents = ids.QSelect(entity => entity).ToArray();
            Draw.Hatch("SOLID", ents);
        }

        [CommandMethod("TestPolygonMesh")]
        public void TestPolygonMesh()
        {
            int m = 100;
            int n = 100;
            double f(double x, double y) => 10 * Math.Cos((x * x + y * y) / 1000);

            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double x = 10 * i + 10;
                    double y = 10 * j + 10;
                    double z = f(x, y);
                    points.Add(new Point3d(x, y, z));
                }
            }

            Draw.PolygonMesh(points, m, n);
        }

        [CommandMethod("TestAddAttribute")]
        public void TestAppendAttribute()
        {
            ObjectId id = Draw.Insert("test", Point3d.Origin);
            AttributeReference attribute = new AttributeReference
            {
                Tag = "test",
                TextString = "test value",
                Position = Point3d.Origin
            };

            id.QOpenForWrite<BlockReference>(br =>
            {
                // using default overwrite and createIfMissing
                br.AppendAttribute(attribute);
            });
        }

        [CommandMethod("TestKeywords")]
        public void TestKeywords()
        {
            string[] keys = { "A", "B", "C", "D" };
            string key = Interaction.GetKeywords("\nChoose an option", keys, 3);
            Interaction.WriteLine("You chose {0}.", key);
        }

        #endregion
    }

}
