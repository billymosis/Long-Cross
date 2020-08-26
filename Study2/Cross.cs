using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

public class Cross

{
    private readonly Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
    private int tempX;
    private List<AttributeDefinition> Attributes = new List<AttributeDefinition>();
    private Polyline crossLine;
    private List<Data> DataCollection;

    public string crossError;
    public int Count => DataCollection.Count;

    public Cross(string Path)
    {
        Data d = new Data();
        DataCollection = d.GetData(Path);
    }

    public void Draw(int CrossNumber)
    {
        using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
        {
            using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForWrite) as BlockTable)
            {
                using (BlockTableRecord btr = new BlockTableRecord())
                {
                    if (!bt.Has("_" + DataCollection[CrossNumber].NamaPatok + "_"))
                    {
                        bt.Add(btr);
                        btr.Name = "_" + DataCollection[CrossNumber].NamaPatok + "_";
                        List<double> Elev = DataCollection[CrossNumber].Elevation.ConvertAll(x => double.Parse(x));
                        List<double> Dist = DataCollection[CrossNumber].Distance.ConvertAll(x => double.Parse(x));
                        List<string> Desc = DataCollection[CrossNumber].Description;
                        int limit = Elev.Count;
                        int ip = Dist.IndexOf(0);

                        //Error Detector
                        if (ip == -1 || (Desc.FindIndex(s => s.Contains("D")) == -1))
                        {
                            crossError += DataCollection[CrossNumber].NamaPatok + ", ";
                        }

                        //Pembuatan Cross
                        crossLine = new Polyline();

                        for (int i = 0; i < limit; i++)
                        {
                            crossLine.AddVertexAt(i, new Point2d(Dist[i], Elev[i]), 0, 0, 0);
                            if (!(i == limit - 1))
                            {
                                using (Line Jalan = new Line())
                                {
                                    Jalan.StartPoint = new Point3d(Dist[i], Elev[i], 0);
                                    Jalan.EndPoint = new Point3d(Dist[i + 1], Elev[i + 1], 0);

                                    if (Desc[i].Contains("J") && Desc[i + 1].Contains("J"))
                                    {
                                        Jalan.ColorIndex = 1;
                                    }

                                    if (Desc[i].Contains("L") && Desc[i + 1].Contains("L"))
                                    {
                                        Jalan.ColorIndex = 2;
                                    }
                                    btr.AppendEntity(Jalan);
                                }

                                using (Line Pembantu1 = new Line())
                                {
                                    Pembantu1.StartPoint = new Point3d(Dist[i], Elev[i], 0);
                                    Pembantu1.EndPoint = new Point3d(Dist[i], DataCollection[CrossNumber].Datum, 0);
                                    Pembantu1.Linetype = "DASHED";
                                    btr.AppendEntity(Pembantu1);
                                }

                                using (Line Pembantu2 = new Line())
                                {
                                    Pembantu2.StartPoint = new Point3d(Dist[i], DataCollection[CrossNumber].Datum, 0);
                                    Pembantu2.EndPoint = new Point3d(Dist[i], DataCollection[CrossNumber].Datum - 2, 0);
                                    btr.AppendEntity(Pembantu2);
                                }

                                // TEXT ELEVATION
                                using (DBText tx = new DBText())
                                {
                                    tx.Rotation = Math.PI / 2;
                                    tx.TextString = (Math.Round(Elev[i], 2)).ToString("N2");
                                    tx.Justify = AttachmentPoint.BaseCenter;
                                    tx.AlignmentPoint = new Point3d(Dist[i], DataCollection[CrossNumber].Datum - 0.5, 0);
                                    btr.AppendEntity(tx);
                                    double Bounds = tx.Bounds.Value.MaxPoint.X - tx.Bounds.Value.MinPoint.X;

                                    if (i != 0 && (Bounds >= Math.Abs(Dist[i] - Dist[i - 1])))
                                    {
                                        tx.Justify = AttachmentPoint.TopCenter;
                                        tx.ColorIndex = 1;
                                    }
                                }

                                // TEXT DISTANCE
                                using (DBText tx = new DBText())
                                {
                                    tx.Rotation = Math.PI * 2;
                                    tx.TextString = Math.Round(Dist[i + 1] - Dist[i], 2).ToString("N2");
                                    tx.Justify = AttachmentPoint.MiddleCenter;
                                    Vector3d v = new Point3d(Dist[i], DataCollection[CrossNumber].Datum - 1.5, 0).GetVectorTo(new Point3d(Dist[i + 1], DataCollection[CrossNumber].Datum - 1.5, 0));
                                    tx.AlignmentPoint = new Point3d(Dist[i], DataCollection[CrossNumber].Datum - 1.5, 0) + v * 0.5;
                                    btr.AppendEntity(tx);
                                    double Bounds = tx.Bounds.Value.MaxPoint.X - tx.Bounds.Value.MinPoint.X;
                                    if (Bounds >= Dist[i + 1] - Dist[i])
                                    {
                                        tx.Rotation = Math.PI / 2;
                                        tx.ColorIndex = 1;
                                    }
                                }
                            }
                            //Pembuatan Text
                            using (DBText tx = new DBText())
                            {
                                tx.Position = new Point3d(Dist[i], Elev[i], 0);
                                tx.TextString = Desc[i];
                                btr.AppendEntity(tx);
                            }
                        }

                        //MID POINT
                        if (!(Desc.FindIndex(s => s.Contains("D")) == -1))
                        {
                            //ds = dasar start
                            //de = dasar end
                            int ds = Desc.FindIndex(s => s.Contains("D"));
                            int de = Desc.FindIndex(ds + 1, s => s.Contains("D"));
                            Point3dCollection points = new Point3dCollection();
                            Vector3d v = new Point3d(Dist[ds], Elev[ds], 0).GetVectorTo(new Point3d(Dist[de], Elev[de], 0));
                            Point3d mid = new Point3d(Dist[ds], Elev[ds], 0) + v * 0.5;
                            using (Line CL = new Line())
                            {
                                CL.StartPoint = new Point3d(mid.X, mid.Y, 0);
                                CL.IntersectWith(crossLine, Intersect.ExtendThis, points, IntPtr.Zero, IntPtr.Zero);
                                DataCollection[CrossNumber].ElvAsDasar = points[0].Y;
                                DataCollection[CrossNumber].DistAsDasar = points[0].X;
                            }
                            using (DBPoint c = new DBPoint())
                            {
                                c.Position = points[0];
                                btr.AppendEntity(c);
                            }
                            using (DBText tx = new DBText())
                            {
                                tx.Position = new Point3d(mid.X, mid.Y - 2.5, 0);
                                tx.Height = 1.5;
                                tx.ColorIndex = 3;
                                tx.TextString = DataCollection[CrossNumber].NamaPatok;
                                btr.AppendEntity(tx);
                            }

                            //btr.Origin = new Point3d(DataCollection[CrossNumber].BoundLeft, mid.Y, 0);
                        }
                        else if (!(ip == -1))
                        {
                            //btr.Origin = new Point3d(Dist[ip], Elev[ip], 0);
                            using (DBText tx = new DBText())
                            {
                                tx.Position = new Point3d(Dist[ip], Elev[ip], 0);
                                tx.TextString = DataCollection[CrossNumber].NamaPatok;
                                btr.AppendEntity(tx);
                            }
                        }
                        else
                        {
                            //btr.Origin = new Point3d(0, 0, 0);
                            using (DBText tx = new DBText())
                            {
                                tx.Position = new Point3d(0, 0, 0);
                                tx.TextString = DataCollection[CrossNumber].NamaPatok;
                                btr.AppendEntity(tx);
                            }
                        }

                        //Tambah Block Patok
                        if (!(ip == -1))
                        {
                            using (BlockReference brf = new BlockReference(new Point3d(Dist[ip], Elev[ip], 0), bt["Patok"]))
                            {
                                btr.AppendEntity(brf);
                            }
                        }
                        else
                        {
                            using (BlockReference brf = new BlockReference(new Point3d(0, 0, 0), bt["Patok"]))
                            {
                                btr.AppendEntity(brf);
                            }
                        }

                        // Set the insertion point for the block
                        btr.Origin = new Point3d(DataCollection[CrossNumber].DistAsDasar - 20.5, DataCollection[CrossNumber].Datum - 2, 0);
                        //btr.AppendEntity(crossLine);
                        crossLine.Dispose();
                    }
                }

                using (BlockTableRecord btr = new BlockTableRecord())
                {
                    if (!bt.Has(DataCollection[CrossNumber].NamaPatok))
                    {
                        Database acCurDb = Application.DocumentManager.MdiActiveDocument.Database;

                        bt.Add(btr);
                        btr.Name = DataCollection[CrossNumber].NamaPatok;
                        btr.Origin = new Point3d(DataCollection[CrossNumber].DistAsDasar - 20.5, DataCollection[CrossNumber].Datum - 2, 0);

                        Point2dCollection ptCol = new Point2dCollection
                            {
                                new Point2d(btr.Origin.X + 5.5, btr.Origin.Y),
                                new Point2d(btr.Origin.X + 5.5 + 30, DataCollection[CrossNumber].ElvMax + 0.2)
                            };

                        Vector3d normal;
                        double elevx = 0;

                        if (acCurDb.TileMode == true)
                        {
                            normal = acCurDb.Ucsxdir.CrossProduct(acCurDb.Ucsydir);
                            elevx = acCurDb.Elevation;
                        }
                        else
                        {
                            normal = acCurDb.Pucsxdir.CrossProduct(acCurDb.Pucsydir);
                            elevx = acCurDb.Pelevation;
                        }

                        //Tambah Block Cross

                        using (BlockReference brf = new BlockReference(new Point3d(DataCollection[CrossNumber].DistAsDasar - 20.5, DataCollection[CrossNumber].Datum - 2, 0), bt["_" + DataCollection[CrossNumber].NamaPatok + "_"]))
                        {
                            btr.AppendEntity(brf);
                            tr.AddNewlyCreatedDBObject(brf, true);

                            using (Autodesk.AutoCAD.DatabaseServices.Filters.SpatialFilter filter = new Autodesk.AutoCAD.DatabaseServices.Filters.SpatialFilter())
                            {
                                Autodesk.AutoCAD.DatabaseServices.Filters.SpatialFilterDefinition filterDef =
                   new Autodesk.AutoCAD.DatabaseServices.Filters.SpatialFilterDefinition(ptCol, normal, elevx, 0, 0, true);
                                filter.Definition = filterDef;

                                // Define the name of the extension dictionary and entry name
                                string dictName = "ACAD_FILTER";
                                string spName = "SPATIAL";

                                // Check to see if the Extension Dictionary exists, if not create it
                                if (brf.ExtensionDictionary.IsNull)
                                {
                                    brf.CreateExtensionDictionary();
                                }

                                // Open the Extension Dictionary for write
                                DBDictionary extDict = tr.GetObject(brf.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;

                                // Check to see if the dictionary for clipped boundaries exists,
                                // and add the spatial filter to the dictionary
                                if (extDict.Contains(dictName))
                                {
                                    DBDictionary filterDict = tr.GetObject(extDict.GetAt(dictName), OpenMode.ForWrite) as DBDictionary;

                                    if (filterDict.Contains(spName))
                                    {
                                        filterDict.Remove(spName);
                                    }

                                    filterDict.SetAt(spName, filter);
                                }
                                else
                                {
                                    using (DBDictionary filterDict = new DBDictionary())
                                    {
                                        extDict.SetAt(dictName, filterDict);

                                        tr.AddNewlyCreatedDBObject(filterDict, true);
                                        filterDict.SetAt(spName, filter);
                                    }
                                }

                                tr.AddNewlyCreatedDBObject(filter, true);
                            }
                        }

                        //Tambah Block Legend

                        using (BlockReference brf = new BlockReference(new Point3d(DataCollection[CrossNumber].DistAsDasar - 20.5, DataCollection[CrossNumber].Datum - 2, 0), bt["Block1"]))
                        {
                            btr.AppendEntity(brf);
                            tr.AddNewlyCreatedDBObject(brf, true);
                            using (DBObjectCollection aaa = new DBObjectCollection())
                            {
                                brf.Explode(aaa);
                                foreach (DBObject item in aaa)
                                {
                                    Entity c = item as Entity;
                                    btr.AppendEntity(c);
                                    tr.AddNewlyCreatedDBObject(c, true);
                                }
                                brf.Erase();
                            }

                            if (btr.HasAttributeDefinitions)
                            {
                                foreach (ObjectId objID in btr)
                                {
                                    DBObject dbObj = tr.GetObject(objID, OpenMode.ForRead) as DBObject;

                                    if (dbObj is AttributeDefinition)
                                    {
                                        AttributeDefinition acAtt = dbObj as AttributeDefinition;

                                        tr.GetObject(objID, OpenMode.ForWrite);

                                        switch (acAtt.Tag)
                                        {
                                            case "_ELEVASI":
                                                acAtt.TextString = "+" + DataCollection[CrossNumber].Datum.ToString("N2");
                                                break;

                                            case "_NAMAPATOK":
                                                acAtt.TextString = DataCollection[CrossNumber].NamaPatok;
                                                break;

                                            default:
                                                break;
                                        }
                                        Attributes.Add(acAtt);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new System.ArgumentException("Duplikasi nama patok", "ERROR, Bikin dokumen baru");
                    }
                    //BLOCK TABLE RECORD
                }
                //BLOCK TABLE
            }
            //TRANSACTION

            tr.Commit();

            //ent.IntersectWith()
        }
    }

    private void ResetAttributes(BlockReference br, List<AttributeDefinition> attDefs, Transaction tm)
    {
        Dictionary<string, string> attValues = new Dictionary<string, string>();
        foreach (ObjectId id in br.AttributeCollection)
        {
            if (!id.IsErased)
            {
                AttributeReference attRef = (AttributeReference)tm.GetObject(id, OpenMode.ForWrite);
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
            tm.AddNewlyCreatedDBObject(attRef, true);
        }
    }

    public void Place(int CrossNumber)
    {
        using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
        {
            using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable)
            {
                using (BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    int n = CrossNumber + 1;

                    using (BlockReference brf = new BlockReference(new Point3d(50 * tempX, -50 * (CrossNumber % 4), 0), bt[DataCollection[CrossNumber].NamaPatok]))
                    {
                        btr.AppendEntity(brf);
                        ResetAttributes(brf, Attributes, tr);
                        Attributes.Clear();
                    }

                    if (n % 4 == 0)
                    {
                        tempX++;
                    }
                }
            }

            tr.Commit();
        }
    }
}