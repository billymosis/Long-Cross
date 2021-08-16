using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PLC.Utilities;

namespace PLC
{
    public class StructureCommand
    {
        [CommandMethod("EC")]
        public static void EC()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;

            if (Autodesk.AutoCAD.Internal.AcAeUtilities.IsInBlockEditor())
            {
                const bool isCrossOnly = true;

                string blockName = Autodesk.AutoCAD.Internal.AcAeUtilities.GetBlockName();
                ed.WriteMessage("\nWe are in block editor of block " + blockName);

                PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ")
                {
                    Filter = "Cross File (*.csv;*.txt)|*.csv;*.txt"
                };
                string path;
                path = Global.dataPath ?? ed.GetFileNameForOpen(POFO).StringResult;
                Global.dataPath = path;
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                string s = path;
                Data data = new Data(s);
                Cross x = new Cross(data.canal);


                List<Nodes> CrossData = new List<Nodes>();
                if (isCrossOnly)
                {
                    CrossData = data.canal.nodes.Where(z => z.NodesType == Nodes.NodesEnum.Cross).ToList();
                }

                foreach (Nodes item in CrossData)
                {
                    if (item.ValidCross && item.Patok == blockName)
                    {
                        Point3d CENTER_DESGIN_POINT = item.AsPoint2d;
                        Point3d design1 = new Point3d(CENTER_DESGIN_POINT.X - 10, CENTER_DESGIN_POINT.Y + 1, 0);
                        Point3d design2 = new Point3d(CENTER_DESGIN_POINT.X + 10, CENTER_DESGIN_POINT.Y + 1, 0);
                        Point3d design3 = new Point3d(CENTER_DESGIN_POINT.X, CENTER_DESGIN_POINT.Y, 0);

                        Dictionary<string, Point3d> DummyDesign = new Dictionary<string, Point3d>()
                        {
                            { "RightWall", design2 },
                            { "LeftWall", design1 },
                            { "B10", design3 }
                        };

                        Structure y = new Structure(DummyDesign);

                        x.DrawStructure(item, y, true, EarthWorks.Both);
                    }

                }
                ed.Regen();
            }
            else
            {
                ed.WriteMessage("\nWe are NOT in block editor");
            }

        }

        [CommandMethod("TOREGION")]
        public static void TOREGION()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;

            if (Autodesk.AutoCAD.Internal.AcAeUtilities.IsInBlockEditor())
            {
                ObjectId[] ids = Interaction.GetSelection("\nSelect polyline", "LWPOLYLINE");
                List<Entity> Regs = new List<Entity>();
                if (ids.Count() == 0)
                {
                    return;
                }
                ids.QForEach<Polyline>(poly =>
                {
                    Region region = CreateRegionFromPolyline(poly);
                    Regs.Add(region);
                });
                string key = Interaction.GetKeywords("Cut, Fill or Both", new string[] { "Cut", "Fill", "Both" }, 2);
                switch (key)
                {
                    case "Cut":
                        foreach (Entity item in Regs)
                        {
                            item.AddToCurrentSpace().SetLayer("Cut-Region");
                        }
                        break;
                    case "Fill":
                        foreach (Entity item in Regs)
                        {
                            item.AddToCurrentSpace().SetLayer("Fill-Region");
                        }
                        break;
                    case "Both":
                        foreach (Entity item in Regs)
                        {
                            item.AddToCurrentSpace().SetLayer("Both-Region");
                        }
                        break;
                }
            }
        }

        [CommandMethod("EQ")]
        public static void EQ()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;

            if (Autodesk.AutoCAD.Internal.AcAeUtilities.IsInBlockEditor())
            {
                const bool isCrossOnly = true;

                string blockName = Autodesk.AutoCAD.Internal.AcAeUtilities.GetBlockName();
                ed.WriteMessage("\nWe are in block editor of block " + blockName);

                PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ")
                {
                    Filter = "Cross File (*.csv;*.txt)|*.csv;*.txt"
                };
                string path;
                path = Global.dataPath ?? ed.GetFileNameForOpen(POFO).StringResult;
                Global.dataPath = path;
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                string s = path;
                Data data = new Data(s);
                Cross x = new Cross(data.canal);


                List<Nodes> CrossData = new List<Nodes>();
                if (isCrossOnly)
                {
                    CrossData = data.canal.nodes.Where(z => z.NodesType == Nodes.NodesEnum.Cross).ToList();
                }

                foreach (Nodes item in CrossData)
                {
                    if (item.ValidCross && item.Patok == blockName)
                    {
                        Dictionary<Region, Point3d> DummyDesign = new Dictionary<Region, Point3d>();
                        List<Region> Regs = new List<Region>();
                        ObjectId[] ids = Interaction.GetSelection("\nSelect polyline", "LWPOLYLINE");
                        if (ids.Count() == 0)
                        {
                            return;
                        }
                        ids.QForEach<Polyline>(poly =>
                        {
                            Region region = CreateRegionFromPolyline(poly);

                            Regs.Add(region);

                        });
                        Structure y = new Structure(DummyDesign);
                        string key = Interaction.GetKeywords("Cut, Fill or Both", new string[] { "Cut", "Fill", "Both" }, 2);
                        foreach (Region reg in Regs)
                        {
                            switch (key)
                            {
                                case "Cut":
                                    reg.Layer = "Cut-Region";
                                    break;
                                case "Fill":
                                    reg.Layer = "Fill-Region";
                                    break;
                                case "Both":
                                    reg.Layer = "Both-Region";
                                    break;
                                default:
                                    break;
                            }
                            Point3d basePoint = new Point3d((reg.Bounds.Value.MinPoint.X + reg.Bounds.Value.MaxPoint.X) / 2, reg.Bounds.Value.MinPoint.Y, 0);
                            DummyDesign.Add(reg, basePoint);
                        }
                        switch (key)
                        {
                            case "Cut":
                                x.DrawStructure(item, y, true, EarthWorks.Cut);
                                break;
                            case "Fill":
                                x.DrawStructure(item, y, true, EarthWorks.Fill);
                                break;
                            case "Both":
                                x.DrawStructure(item, y, true, EarthWorks.Both);
                                break;
                            default:
                                break;
                        }

                    }

                }
                ed.Regen();
            }
            else
            {
                ed.WriteMessage("\nWe are NOT in block editor");
            }

        }


        [CommandMethod("TestBEDIT")]
        public void TestBEdit()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;

            if (Autodesk.AutoCAD.Internal.AcAeUtilities.IsInBlockEditor())
            {
                string blockName = Autodesk.AutoCAD.Internal.AcAeUtilities.GetBlockName();
                ed.WriteMessage("\nWe are in block editor of block " + blockName);

            }
            else
            {
                ed.WriteMessage("\nWe are NOT in block editor");
            }

        }

        /// <summary>
        /// SYNCBLOCK
        /// </summary>
        [CommandMethod("EE")]
        public void EE()
        {


            ObjectId id = Interaction.GetEntity("\nSelect Block", typeof(BlockReference));
            if (id == ObjectId.Null)
            {
                return;
            }

            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction())
            {
                BlockReference blk = tr.GetObject(id, OpenMode.ForWrite) as BlockReference;
                BlockTableRecord btr = tr.GetObject(blk.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;
                btr.SynchronizeAttributes();
                tr.Commit();
            }

        }

        /// <summary>
        /// CALCULATE
        /// </summary>
        [CommandMethod("ER")]
        public void ER()
        {
            ObjectId[] ids = Interaction.GetSelection("\nSelect Block", "INSERT");
            foreach (ObjectId id in ids)
            {
                if (id == ObjectId.Null)
                {
                    return;
                }
                double Cut = GetRegionArea("Cut", id);
                double Fill = GetRegionArea("Fill", id);
                if (Cut > 0)
                {
                    SetAttributes("GALIAN", Cut.ToString("N2"), id);
                }
                if (Fill > 0)
                {
                    SetAttributes("TIMBUNAN", Fill.ToString("N2"), id);
                }

            }

        }
        [CommandMethod("ERA")]
        public void ERA()
        {
            ObjectId[] ids = QuickSelection.SelectAll("INSERT");
            foreach (ObjectId blockID in ids)
            {
                double Cut = GetRegionArea("Cut", blockID);
                double Fill = GetRegionArea("Fill", blockID);

                if (Cut > 0)
                {
                    SetAttributes("GALIAN", Cut.ToString("N2"), blockID);
                }
                if (Fill > 0)
                {
                    SetAttributes("TIMBUNAN", Fill.ToString("N2"), blockID);
                }
            }
        }


    }

}





