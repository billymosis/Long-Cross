using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using AcRx = Autodesk.AutoCAD.Runtime;
namespace PLC
{


    public static class Utilities
    {
        public static void SetAttributes(string TagName, string Value, ObjectId BlockId)
        {
            BlockId.QOpenForWrite<BlockReference>
            (x =>
            {
                foreach (ObjectId item in x.AttributeCollection)
                {
                    item.QOpenForWrite<AttributeReference>
                    (
                        y =>
                        {
                            if (y.Tag == TagName)
                            {
                                y.TextString = Value;
                            }
                        }
                    );
                }
            }
            );
        }

        public static double GetRegionArea(string LayerName, ObjectId BlockId)
        {
            ObjectId btrid = BlockId.QOpenForRead<BlockReference>().BlockTableRecord;
            List<ObjectId> ids = new List<ObjectId>();
            double Area = 0;
            foreach (ObjectId item in btrid.QOpenForRead<BlockTableRecord>())
            {
                ids.Add(item);
            }
            ids.QForEach<Entity>(x =>
            {
                if (x.Layer == LayerName && x.GetType() == typeof(Region))
                {
                    Area += (x as Region).Area;
                }

            }
            );
            return Area;
        }

        private static readonly RXClass attDefClass = RXClass.GetClass(typeof(AttributeDefinition));

        public static void SynchronizeAttributes(this BlockTableRecord target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            Transaction tr = target.Database.TransactionManager.TopTransaction;
            if (tr == null)
            {
                throw new AcRx.Exception(ErrorStatus.NoActiveTransactions);
            }

            List<AttributeDefinition> attDefs = target.GetAttributes(tr);
            foreach (ObjectId id in target.GetBlockReferenceIds(true, false))
            {
                BlockReference br = (BlockReference)tr.GetObject(id, OpenMode.ForWrite);
                br.ResetAttributes(attDefs, tr);
            }
            if (target.IsDynamicBlock)
            {
                target.UpdateAnonymousBlocks();
                foreach (ObjectId id in target.GetAnonymousBlockIds())
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    attDefs = btr.GetAttributes(tr);
                    foreach (ObjectId brId in btr.GetBlockReferenceIds(true, false))
                    {
                        BlockReference br = (BlockReference)tr.GetObject(brId, OpenMode.ForWrite);
                        br.ResetAttributes(attDefs, tr);
                    }
                }
            }
        }

        private static List<AttributeDefinition> GetAttributes(this BlockTableRecord target, Transaction tr)
        {
            List<AttributeDefinition> attDefs = new List<AttributeDefinition>();
            foreach (ObjectId id in target)
            {
                if (id.ObjectClass == attDefClass)
                {
                    AttributeDefinition attDef = (AttributeDefinition)tr.GetObject(id, OpenMode.ForRead);
                    attDefs.Add(attDef);
                }
            }
            return attDefs;
        }

        private static void ResetAttributes(this BlockReference br, List<AttributeDefinition> attDefs, Transaction tr)
        {
            Dictionary<string, string> attValues = new Dictionary<string, string>();
            foreach (ObjectId id in br.AttributeCollection)
            {
                if (!id.IsErased)
                {
                    AttributeReference attRef = (AttributeReference)tr.GetObject(id, OpenMode.ForWrite);
                    attValues.Add(attRef.Tag,
                        attRef.IsMTextAttribute ? attRef.MTextAttribute.Contents : attRef.TextString);
                    attRef.Erase();
                }
            }
            foreach (AttributeDefinition attDef in attDefs)
            {
                AttributeReference attRef = new AttributeReference();
                attRef.SetAttributeFromBlock(attDef, br.BlockTransform);
                if (attDef.Constant)
                {
                    attRef.TextString = attDef.IsMTextAttributeDefinition ?
                        attDef.MTextAttributeDefinition.Contents :
                        attDef.TextString;
                }
                else if (attValues.ContainsKey(attRef.Tag))
                {
                    attRef.TextString = attValues[attRef.Tag];
                }
                br.AttributeCollection.AppendAttribute(attRef);
                tr.AddNewlyCreatedDBObject(attRef, true);
            }
        }

        public static List<Entity> GetFillRegion(Region nodes, Region objects, Point3d basePosition)
        {
            List<Entity> fillRegion = new List<Entity>();
            string Layer = objects.Layer;
            Region cross = nodes;
            Point3d BottomBound = cross.Bounds.Value.MinPoint;
            Point3d Left = objects.Bounds.Value.MinPoint;
            Point3d Right = objects.Bounds.Value.MaxPoint;
            Region reg1 = GetIntersectedArea(cross.Clone() as Region, objects.Clone() as Region) as Region;
            if (basePosition.Y >= BottomBound.Y)
            {
                bool isRegion = false;
                double BaseStructureElevation = Left.Y <= BottomBound.Y ? Left.Y : BottomBound.Y;
                Polyline Bounds = NoDraw.Rectang(new Point3d(Left.X, BaseStructureElevation, 0), new Point3d(Right.X, Right.Y, 0));
                if (Bounds.Area != 0)
                {
                    Region BoundReg = CreateRegionFromPolyline(Bounds);
                    Region sub = GetSubtractedArea(BoundReg, objects.Clone() as Region);
                    sub = GetSubtractedArea(sub.Clone() as Region, cross.Clone() as Region);
                    sub = GetSubtractedArea(sub.Clone() as Region, objects.Clone() as Region);
                    if (sub.Area == 0)
                    {
                        return fillRegion;
                    }
                    DBObjectCollection result = new DBObjectCollection();
                    sub.Explode(result);

                    foreach (Entity res in result)
                    {
                        if (res.GetType() == typeof(Region))
                        {
                            isRegion = true;
                        }
                    }
                    if (isRegion)
                    {
                        foreach (Region regi in result)
                        {
                            if (regi.Bounds.Value.MinPoint.Y < basePosition.Y)
                            {

                                fillRegion.Add(regi);
                            }
                        }
                    }
                    else
                    {
                        if (sub.Bounds.Value.MinPoint.Y < basePosition.Y)
                        {
                            fillRegion.Add(sub);
                        }
                    }

                }

            }
            foreach (Entity item in fillRegion)
            {
                if (string.IsNullOrEmpty(item.Layer))
                {
                    item.Layer = "Fill-Region";
                }
            }
            return fillRegion;

        }

        public static List<Entity> GetCutRegion(Region nodes, Region objects, Point3d basePosition)
        {
            List<Entity> cutRegion = new List<Entity>();
            string Layer = objects.Layer;
            Region cross = nodes;
            Point3d TopBound = cross.Bounds.Value.MaxPoint;
            Point3d BottomBound = cross.Bounds.Value.MinPoint;
            Point3d Left = objects.Bounds.Value.MinPoint;
            Point3d Right = objects.Bounds.Value.MaxPoint;
            Region reg1 = GetIntersectedArea(cross.Clone() as Region, objects.Clone() as Region) as Region;
            if (reg1.Area != 0)
            {
                bool isRegion = false;
                bool isInnerRegion = false;
                double BaseStructureElevation = Right.Y <= BottomBound.Y ? Right.Y : TopBound.Y;
                Polyline Bounds = NoDraw.Rectang(Left, new Point3d(Right.X, BaseStructureElevation, 0));
                DBObjectCollection InnerResults = new DBObjectCollection();
                (reg1.Clone() as Entity).Explode(InnerResults);
                foreach (Entity InnerResult in InnerResults)
                {
                    if (InnerResult.GetType() == typeof(Region))
                    {
                        isInnerRegion = true;
                    }
                }
                if (isInnerRegion)
                {
                    foreach (Entity regi in InnerResults)
                    {
                        cutRegion.Add(regi);
                    }
                }
                else
                {
                    cutRegion.Add(reg1);
                }

                if (Bounds.Area != 0)
                {
                    Region BoundReg = CreateRegionFromPolyline(Bounds);
                    Region sub = GetSubtractedArea(BoundReg, objects.Clone() as Region);
                    sub = GetSubtractedArea(cross.Clone() as Region, sub.Clone() as Region);
                    sub = GetSubtractedArea(cross.Clone() as Region, sub.Clone() as Region);
                    if (sub.Area != 0)
                    {
                        DBObjectCollection result = new DBObjectCollection();
                        sub.Explode(result);
                        foreach (Entity res in result)
                        {
                            if (res.GetType() == typeof(Region))
                            {
                                isRegion = true;
                            }
                        }
                        if (isRegion)
                        {
                            foreach (Entity regi in result)
                            {
                                if (regi.Bounds.Value.MinPoint.Y >= basePosition.Y)
                                {
                                    cutRegion.Add(regi);
                                }
                            }
                        }
                        else
                        {
                            cutRegion.Add(sub);
                        }

                    }

                }
            }
            foreach (Entity item in cutRegion)
            {
                if (string.IsNullOrEmpty(item.Layer))
                {
                    item.Layer = "Cut-Region";
                }
            }
            return cutRegion;
        }

        public static void CreateLayer(string layName, short ColorIndex)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Transaction tr = db.TransactionManager.StartTransaction();
            using (tr)
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                SymbolUtilityServices.ValidateSymbolName(layName, false);
                if (!lt.Has(layName))
                {
                    LayerTableRecord ltr = new LayerTableRecord()
                    {
                        Name = layName,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, ColorIndex)
                    };
                    lt.UpgradeOpen();
                    ObjectId ltId = lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                    tr.Commit();
                }
            }

        }

        public static Hatch HatchEntity(Nodes nodes, Entity reg1, string hatchName, double angle, string layer)
        {
            Hatch hatch = new Hatch();
            hatch.SetHatchPattern(HatchPatternType.PreDefined, hatchName);
            hatch.PatternAngle = angle;
            hatch.HatchStyle = HatchStyle.Normal;
            hatch.PatternScale = 1;
            hatch.Layer = layer;
            ObjectId[] x = new List<Entity>() { reg1 as Entity }.ToArray().ModifyBlock(nodes.Patok);
            hatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection(x));
            return hatch;
        }

        public static bool Collinear(Line line, Point3d p)
        {
            double y1 = line.StartPoint.Y;
            double y2 = line.EndPoint.Y;
            double y3 = p.Y;
            double x1 = line.StartPoint.X;
            double x2 = line.EndPoint.X;
            double x3 = p.X;
            return Math.Abs((y1 - y2) * (x1 - x3) - (y1 - y3) * (x1 - x2)) <= 1e-9;
        }

        public static Region GetSubtractedArea(Polyline pline1, Polyline pline2)
        {
            Region region1 = CreateRegionFromPolyline(pline1);
            Region region2 = CreateRegionFromPolyline(pline2);
            region1.BooleanOperation(BooleanOperationType.BoolSubtract, region2);
            return region1;
        }

        public static Region GetIntersectedArea(Polyline pline1, Polyline pline2)
        {
            Region region1 = CreateRegionFromPolyline(pline1);
            Region region2 = CreateRegionFromPolyline(pline2);
            region1.BooleanOperation(BooleanOperationType.BoolIntersect, region2);
            return region1;
        }

        public static Region GetSubtractedArea(Region region1, Region region2)
        {
            region1.BooleanOperation(BooleanOperationType.BoolSubtract, region2);
            return region1.Clone() as Region;
        }

        public static Region GetIntersectedArea(Region region1, Region region2)
        {
            region1.BooleanOperation(BooleanOperationType.BoolIntersect, region2);
            return region1.Clone() as Region;
        }

        public static Region CreateRegionFromPolyline(Polyline pline)
        {
            DBObjectCollection source = new DBObjectCollection
            {
                pline
            };
            DBObjectCollection regions = Region.CreateFromCurves(source);
            return regions[0] as Region;
        }

        public static Point3dCollection GetIntersectionPoint(Line bli, List<Line> OutterLines)
        {
            Point3dCollection InnerIntersections = new Point3dCollection();
            foreach (Entity item in OutterLines)
            {
                Point3dCollection g1 = new Point3dCollection();

                bli.IntersectWith(item, Intersect.OnBothOperands, g1, IntPtr.Zero, IntPtr.Zero);
                foreach (Point3d gg1 in g1)
                {
                    InnerIntersections.Add(gg1);
                }
            }
            return InnerIntersections;
        }

        public static Point3dCollection GetIntersectionPoint(Line bli, Line bri, List<Line> OutterLines)
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

        public static bool IsOrdered<T>(this IList<T> list, IComparer<T> comparer = null)
        {
            if (comparer == null)
            {
                comparer = Comparer<T>.Default;
            }

            if (list.Count > 1)
            {
                for (int i = 1; i < list.Count; i++)
                {
                    if (comparer.Compare(list[i - 1], list[i]) > 0)
                    {
                        return false;
                    }
                }
            }
            return true;

        }

        public static Line2d ConvertLine2d(Line l)
        {
            Point2d p1 = ConvertPoint2d(l.StartPoint);
            Point2d p2 = ConvertPoint2d(l.EndPoint);
            return new Line2d(p1, p2);
        }

        public static Line Flatten(this Line x)
        {
            Point3d p1 = new Point3d(x.StartPoint.X, x.StartPoint.Y, 0);
            Point3d p2 = new Point3d(x.EndPoint.X, x.EndPoint.Y, 0);
            Line y = new Line(p1, p2);
            x = y;
            return y;
        }

        public static Point3d Flatten(this Point3d x)
        {
            return new Point3d(x.X, x.Y, 0);
        }


        public static void Print(string msg)
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            ed.WriteMessage(msg);
        }

        public static void Alert(string msg)
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Application.ShowAlertDialog(msg);
        }

        public static ObjectId GetBlockID(string blkName)
        {

            ObjectId blkId = ObjectId.Null;

            if (Application.DocumentManager.MdiActiveDocument.Database == null)
            {
                return ObjectId.Null;
            }

            if (string.IsNullOrWhiteSpace(blkName))
            {
                return ObjectId.Null;
            }

            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead);
                if (bt.Has(blkName))
                {
                    blkId = bt[blkName];
                }

                tr.Commit();
            }
            return blkId;
        }

        public static bool EraseBlkRefs(ObjectId blkId)
        {
            bool blkRefsErased = false;

            if (blkId.IsNull)
            {
                return false;
            }

            Database db = blkId.Database;
            if (db == null)
            {
                return false;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord blk = (BlockTableRecord)tr.GetObject(blkId, OpenMode.ForRead);
                ObjectIdCollection blkRefs = blk.GetBlockReferenceIds(true, true);
                if (blkRefs != null && blkRefs.Count > 0)
                {
                    foreach (ObjectId blkRefId in blkRefs)
                    {
                        BlockReference blkRef = (BlockReference)tr.GetObject(blkRefId, OpenMode.ForWrite);
                        blkRef.Erase();
                    }
                    blkRefsErased = true;
                }
                tr.Commit();
            }
            return blkRefsErased;
        }

        public static bool EraseBlk(ObjectId blkId)
        {
            bool blkIsErased = false;

            if (blkId.IsNull)
            {
                return false;
            }

            Database db = blkId.Database;
            if (db == null)
            {
                return false;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                BlockTableRecord blk = (BlockTableRecord)tr.GetObject(blkId, OpenMode.ForRead);
                ObjectIdCollection blkRefs = blk.GetBlockReferenceIds(true, true);
                if (blkRefs == null || blkRefs.Count == 0)
                {
                    blk.UpgradeOpen();
                    blk.Erase();
                    blkIsErased = true;
                }
                tr.Commit();
            }
            return blkIsErased;
        }

        public static double ConvertDegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        public static void DeleteBlock(Nodes item)
        {
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    if (bt.Has(item.Patok))
                    {
                        ObjectId blkId = bt[item.Patok];
                        EraseBlkRefs(blkId);
                        EraseBlk(blkId);
                    }
                }
                tr.Commit();
            }
        }

        public static ObjectId[] GetBlockContent(string blockName)
        {
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    ObjectId x = bt[blockName];
                    using (BlockTableRecord btr = tr.GetObject(x, OpenMode.ForRead) as BlockTableRecord)
                    {
                        foreach (ObjectId item in btr)
                        {
                            ids.Add(item);
                        }
                    }
                }
                tr.Commit();
                return ids.ToArray();
            }
        }

        public static ObjectId InsertBlock(string source, Point3d basePoint)
        {
            ObjectId id;
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    using (BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                    {

                        using (BlockReference brf = new BlockReference(basePoint, bt[source]))
                        {
                            id = btr.AppendEntity(brf);
                        }

                    }
                }
                tr.Commit();
            }
            return id;
        }

        public static ObjectId InsertBlockToBlock(string source, string target, Point3d basePoint)
        {
            ObjectId id;
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    ObjectId x = bt[target];
                    using (BlockTableRecord btr = tr.GetObject(x, OpenMode.ForWrite) as BlockTableRecord)
                    {

                        using (BlockReference brf = new BlockReference(basePoint, bt[source]))
                        {
                            id = btr.AppendEntity(brf);
                        }

                    }

                }
                tr.Commit();
            }
            return id;
        }

        public static ObjectId[] ModifyBlock(this IEnumerable<Entity> entities, string blockName, Point3d origin = new Point3d())
        {
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    ObjectId x = bt[blockName];
                    using (BlockTableRecord btr = tr.GetObject(x, OpenMode.ForWrite) as BlockTableRecord)
                    {
                        foreach (Entity item in entities)
                        {
                            ObjectId id = btr.AppendEntity(item);
                            ids.Add(id);
                            tr.AddNewlyCreatedDBObject(item, true);
                        }


                    }
                }
                tr.Commit();
                return ids.ToArray();
            }
        }

        public static ObjectId AddToBlock(this IEnumerable<Entity> entities, string blockName, Point3d origin = new Point3d())
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            List<ObjectId> ids = new List<ObjectId>();
            ObjectId idx;
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    using (BlockTableRecord btr = new BlockTableRecord())
                    {
                        btr.Name = blockName;

                        foreach (Entity item in entities)
                        {
                            ObjectId id = btr.AppendEntity(item);
                            ids.Add(id);
                        }
                        btr.Origin = origin;
                        bt.Add(btr);
                        idx = btr.Id;

                    }
                }
                tr.Commit();

            }
            return idx;
        }

        public static void Bubble(string myTitle, string myMessage)
        {
            TrayItemBubbleWindow bw = new TrayItemBubbleWindow
            {
                Title = myTitle,
                //HyperLink = "https://billymosis.com",
                //HyperText = "Mantab Bang",

                Text = myMessage,
                //Text2 = "",
                IconType = IconType.Information,
            };
            bw.Closed += Bw_Closed;
            Global.MyTray.ShowBubbleWindow(bw);
        }

        private static void Bw_Closed(object sender, TrayItemBubbleWindowClosedEventArgs e)
        {
            TrayItemBubbleWindow x = sender as TrayItemBubbleWindow;
            x.Dispose();
        }

        public static void ImportBlock()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            db.Insunits = UnitsValue.Millimeters;

            using (Database dbs = new Database(false, true))
            {
                dbs.ReadDwgFile(@"E:\AutoCAD Project\Study2\Study2\data\Baloon2.dwg", FileOpenMode.OpenForReadAndAllShare, true, "");
                using (Transaction tr = dbs.TransactionManager.StartTransaction())
                {
                    using (BlockTable bt = tr.GetObject(dbs.BlockTableId, OpenMode.ForRead) as BlockTable)
                    {
                        using (ObjectIdCollection ids = new ObjectIdCollection())
                        {
                            foreach (ObjectId item in bt)
                            {
                                ids.Add(item);
                            }
                            tr.Commit();
                            db.WblockCloneObjects(ids, db.BlockTableId, new IdMapping(), DuplicateRecordCloning.Ignore, false);
                        }
                    }
                }
            }
        }

        public static void LoadStyle()
        {
            Document newDoc = Application.DocumentManager.MdiActiveDocument;

            Dictionary<string, double> styleList = new Dictionary<string, double>()
            {
               {"L40", 0.102},
               {"L50", 0.127},
               {"L60", 0.152 },
               {"L80", 0.203 },
               {"L100", 0.254 },
               {"L120", 0.305 },
               {"L140", 0.356 },
               {"L175", 0.445 },
               {"L200", 0.508 },
               {"L240", 0.610 },
               {"L290", 0.737 },
               {"L350", 0.889 },
            }
            ;

            using (Transaction newTransaction = newDoc.Database.TransactionManager.StartTransaction())
            {
                BlockTable newBlockTable;
                newBlockTable = newTransaction.GetObject(newDoc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord newBlockTableRecord;
                newBlockTableRecord = (BlockTableRecord)newTransaction.GetObject(newBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                TextStyleTable newTextStyleTable = newTransaction.GetObject(newDoc.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

                foreach (KeyValuePair<string, double> entry in styleList)
                {
                    if (!newTextStyleTable.Has(entry.Key))  //The TextStyle is currently not in the database
                    {
                        newTextStyleTable.UpgradeOpen();
                        TextStyleTableRecord newTextStyleTableRecord = new TextStyleTableRecord
                        {
                            FileName = "simplex.shx",
                            Name = entry.Key,
                            TextSize = entry.Value
                        };
                        newTextStyleTable.Add(newTextStyleTableRecord);
                        newTransaction.AddNewlyCreatedDBObject(newTextStyleTableRecord, true);
                    }
                }
                newTransaction.Commit();
            }
        }

        public static void LoadLinetype()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Linetype table for read
                LinetypeTable acLineTypTbl;
                acLineTypTbl = acTrans.GetObject(acCurDb.LinetypeTableId,
                                                    OpenMode.ForRead) as LinetypeTable;
                string[] linetypes = {"DASHED", "DASHDOT" };
                foreach (string sLineTypName in linetypes)
                {
                    if (acLineTypTbl.Has(sLineTypName) == false)
                    {
                        // Load the Center Linetype
                        acCurDb.LoadLineTypeFile(sLineTypName, "acad.lin");
                    }
                }


                // Save the changes and dispose of the transaction
                acTrans.Commit();
            }
        }

        public static Point2d ConvertPoint2d(Point3d p)
        {
            return new Point2d(p.X, p.Y);
        }

        public static Point3d ConvertPoint3d(Point2d p)
        {
            return new Point3d(p.X, p.Y, 0);
        }

        public static bool IsLeft(Point3d p1, Point3d p2)
        {
            return p1.X < p2.X;
        }

        public static bool IsLeft(Point2d p1, Point2d p2)
        {
            return p1.X < p2.X;
        }

        public static bool IsRight(Point3d p1, Point3d p2)
        {
            return p1.X > p2.X;
        }

        public static bool IsRight(Point2d p1, Point2d p2)
        {
            return p1.X > p2.X;
        }

        public static Point2d GetMidPoint(Point2d first, Point2d second)
        {
            return new Point2d(Math.Abs(first.X + second.X) / 2, Math.Abs(first.Y + second.Y) / 2);
        }

        public static Point2d FindIntersection2d(Point3d s1, Point3d e1, Point3d s2, Point3d e2)
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

        public static Point2d FindIntersection2d(Point2d s1, Point2d e1, Point2d s2, Point2d e2)
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

        public static int ClosestTo(this IEnumerable<int> collection, int target)
        {
            // NB Method will return int.MaxValue for a sequence containing no elements.
            // Apply any defensive coding here as necessary.
            int closest = int.MaxValue;
            int minDifference = int.MaxValue;
            foreach (int element in collection)
            {
                long difference = Math.Abs((long)element - target);
                if (minDifference > difference)
                {
                    minDifference = (int)difference;
                    closest = element;
                }
            }

            return closest;
        }

        public static double CR2D(double radians)
        {
            return (180 / Math.PI) * radians;
        }

        public static Point2d PolarPoints(Point2d pPt, double dAng, double dDist)
        {
            return new Point2d(pPt.X + dDist * Math.Cos(dAng),
                                 pPt.Y + dDist * Math.Sin(dAng));
        }

        public static Point3d PolarPoints(Point3d pPt, double dAng, double dDist)
        {
            return new Point3d(pPt.X + dDist * Math.Cos(dAng),
                                 pPt.Y + dDist * Math.Sin(dAng),
                                 pPt.Z);
        }

        public static double GetSlope(Point2d first, Point2d second)
        {
            return (second.Y - first.Y) / (second.X - first.X);
        }

        public static double GetReciprocal(double value)
        {
            return (1 / value) * -1;
        }

        public static double GetConstant(double X, double Y, double Reciprocal)
        {
            return Y - (Reciprocal * X);
        }

        public static int GetQuadrant(Point3d first, Point3d second)
        {
            int Quadrant = 0;
            if (second.X > first.X && second.Y > first.Y)
            {
                Quadrant = 1;
            }
            else if (second.X < first.X && second.Y > first.Y)
            {
                Quadrant = 2;
            }
            else if (second.X < first.X && second.Y < first.Y)
            {
                Quadrant = 3;
            }
            else if (second.X > first.X && second.Y < first.Y)
            {
                Quadrant = 4;
            }

            return Quadrant;
        }

        public static int[] BothQuadrant(Point3d Before, Point3d Now, Point3d Next)
        {
            int[] array = new int[] { GetQuadrant(Before, Now), GetQuadrant(Next, Now) };
            return array;
        }

        public static List<Point3d> ConvertP3DC2List(Point3dCollection P3DC)
        {
            List<Point3d> P3D = new List<Point3d>();
            foreach (Point3d item in P3DC)
            {
                P3D.Add(item);
            }
            return P3D;
        }

        public static DateTime GetNistTime()
        {
            DateTime result;
            try
            {
                TcpClient client = new TcpClient("time.nist.gov", 13);
                using (StreamReader streamReader = new StreamReader(client.GetStream()))
                {
                    string response = streamReader.ReadToEnd();
                    string utcDateTimeString = response.Substring(7, 17);
                    result = DateTime.ParseExact(utcDateTimeString, "yy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                    return result;
                }
            }
            catch (SocketException SE)
            {
                Utilities.Bubble("Warning", "Request Timeout, Please connect to the internet.\n Error Message: " + SE.Message);
                throw;
            }
        }

        public static string GetDllPath()
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filename = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
            string filePathRelativeToAssembly = Path.Combine(assemblyPath, @"..\SomeFolder\SomeRelativeFile.txt");
            string normalizedPath = Path.GetFullPath(filePathRelativeToAssembly);

            return assemblyPath;
        }

        public static void WriteFile(string[] content, string filename)
        {
            using (StreamWriter file = new StreamWriter(GetDllPath() + @"\" + filename, false))
            {
                foreach (string item in content)
                {
                    file.WriteLine(item);
                }

            }
        }

        public static void WriteFile(string content, string filename)
        {
            using (StreamWriter file = new StreamWriter(filename, false))
            {

                file.WriteLine(content);


            }
        }

        public static void DrawLine(Point3d Awal, Point3d Akhir, BlockTableRecord btr)
        {
            using (Line L = new Line())
            {
                L.StartPoint = Awal;
                L.EndPoint = Akhir;
                btr.AppendEntity(L);

            }
        }

        public static void DrawLine(Point3d Awal, Point3d Akhir, BlockTableRecord btr, int WARNA)
        {
            using (Line L = new Line())
            {
                L.StartPoint = Awal;
                L.EndPoint = Akhir;
                L.ColorIndex = WARNA;
                btr.AppendEntity(L);

            }
        }

    }
}