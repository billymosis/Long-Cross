using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static PLC.Utilities;




namespace PLC
{


    public class Cross
    {
        private readonly Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        private int tempX;
        private List<AttributeDefinition> Attributes = new List<AttributeDefinition>();
        private List<DataCross> DataCollection;
        private readonly double panjangPotongan = 0.5;

        public string crossError;


        public int Count => DataCollection.Count;
        public Cross(Data d)
        {
            DataCollection = d.CrossDataCollection;
        }


        private void DrawCrossPolyline(BlockTableRecord btr, List<double> Dist, List<double> Elev)
        {
            Polyline crossLine = new Polyline();

            for (int i = 0; i < Elev.Count; i++)
            {
                crossLine.AddVertexAt(i, new Point2d(Dist[i], Elev[i]), 0, 0, 0);
            }

            btr.AppendEntity(crossLine);
            crossLine.Dispose();
        }

        private void DrawSegment(BlockTableRecord btr, int SegmentNumber, List<double> Dist, List<double> Elev, List<string> Desc)
        {
            int i = SegmentNumber;

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
        }

        private void DrawPembantu1(BlockTableRecord btr, int NodeNumber, List<double> Dist, List<double> Elev, double Datum)
        {
            int i = NodeNumber;

            using (Line Pembantu1 = new Line())
            {
                Pembantu1.StartPoint = new Point3d(Dist[i], Elev[i], 0);
                Pembantu1.EndPoint = new Point3d(Dist[i], Datum, 0);
                Pembantu1.Linetype = "DASHED";
                btr.AppendEntity(Pembantu1);
            }
        }

        private void DrawPembantu2(BlockTableRecord btr, int NodeNumber, List<double> Dist, double Datum)
        {
            int i = NodeNumber;
            using (Line Pembantu2 = new Line())
            {
                Pembantu2.StartPoint = new Point3d(Dist[i], Datum, 0);
                Pembantu2.EndPoint = new Point3d(Dist[i], Datum - 2, 0);
                btr.AppendEntity(Pembantu2);
            }
        }

        private void DrawTextElevation(BlockTableRecord btr, int NodeNumber, List<double> Dist, List<double> Elev, double Datum)
        {
            using (DBText tx = new DBText())
            {
                int i = NodeNumber;
                tx.Rotation = Math.PI / 2;
                tx.TextString = (Math.Round(Elev[i], 2)).ToString("N2");
                tx.Justify = AttachmentPoint.BaseCenter;
                tx.AlignmentPoint = new Point3d(Dist[i], Datum - 0.5, 0);
                btr.AppendEntity(tx);
                double Bounds = tx.Bounds.Value.MaxPoint.X - tx.Bounds.Value.MinPoint.X;

                if (i != 0 && (Bounds >= Math.Abs(Dist[i] - Dist[i - 1])))
                {
                    tx.Justify = AttachmentPoint.TopCenter;
                    tx.ColorIndex = 1;
                }
            }
        }

        private void DrawTextDistance(BlockTableRecord btr, int SegmentNumber, List<double> Dist, double Datum)
        {
            int i = SegmentNumber;
            using (DBText tx = new DBText())
            {
                tx.Rotation = Math.PI * 2;
                tx.TextString = Math.Round(Dist[i + 1] - Dist[i], 2).ToString("N2");
                tx.Justify = AttachmentPoint.MiddleCenter;
                Vector3d v = new Point3d(Dist[i], Datum - 1.5, 0).GetVectorTo(new Point3d(Dist[i + 1], Datum - 1.5, 0));
                tx.AlignmentPoint = new Point3d(Dist[i], Datum - 1.5, 0) + v * 0.5;
                btr.AppendEntity(tx);
                double Bounds = tx.Bounds.Value.MaxPoint.X - tx.Bounds.Value.MinPoint.X;
                if (Bounds >= Dist[i + 1] - Dist[i])
                {
                    tx.Rotation = Math.PI / 2;
                    tx.ColorIndex = 1;
                }
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

        public void Place(DataCross CD, int CrossNumber)
        {
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    using (BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                    {
                        int n = CrossNumber + 1;

                        using (BlockReference brf = new BlockReference(new Point3d(50 * tempX, -50 * (CrossNumber % 4), 0), bt[CD.NamaPatok]))
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

        /// <summary>
        /// Text Label for Debugging
        /// </summary>
        /// <param name="btr"></param>
        /// <param name="NodeNumber"></param>
        /// <param name="Dist"></param>
        /// <param name="Elev"></param>
        /// <param name="Desc"></param>
        private void DrawTextLabel(BlockTableRecord btr, int NodeNumber, List<double> Dist, List<double> Elev, List<string> Desc)
        {
            int i = NodeNumber;

            using (DBText tx = new DBText())
            {
                tx.Position = new Point3d(Dist[i], Elev[i], 0);
                tx.TextString = Desc[i];
                btr.AppendEntity(tx);
            }
        }

        /// <summary>
        /// Red Modified Boundary Depends on Length
        /// </summary>
        /// <param name="btr"></param>
        /// <param name="CD"></param>
        /// <param name="panjangBoundary"></param>
        private void DrawModifiedBoundary(BlockTableRecord btr, DataCross CD, double panjangBoundary)
        {

            using (Polyline PL = new Polyline())
            {
                double boundaryXLeft = CD.MidPoint.X - (panjangBoundary / 2);
                double boundaryXRight = CD.MidPoint.X + (panjangBoundary / 2);
                Point2d UL = new Point2d(boundaryXLeft, CD.MaxElv);
                Point2d UR = new Point2d(boundaryXRight, CD.MaxElv);
                Point2d DR = new Point2d(boundaryXRight, CD.MinElv);
                Point2d DL = new Point2d(boundaryXLeft, CD.MinElv);
                PL.AddVertexAt(0, UL, 0, 0, 0);
                PL.AddVertexAt(1, UR, 0, 0, 0);
                PL.AddVertexAt(2, DR, 0, 0, 0);
                PL.AddVertexAt(3, DL, 0, 0, 0);
                PL.ColorIndex = 1;
                PL.Closed = true;
                btr.AppendEntity(PL);
            }
        }

        /// <summary>
        /// White Original Boundary
        /// </summary>
        /// <param name="btr"></param>
        /// <param name="CD"></param>
        private void DrawOriginalBoundary(BlockTableRecord btr, DataCross CD)
        {
            using (Polyline PL = new Polyline())
            {
                Point2d UL = new Point2d(CD.MinDist, CD.MaxElv);
                Point2d UR = new Point2d(CD.MaxDist, CD.MaxElv);
                Point2d DR = new Point2d(CD.MaxDist, CD.MinElv);
                Point2d DL = new Point2d(CD.MinDist, CD.MinElv);
                PL.AddVertexAt(0, UL, 0, 0, 0);
                PL.AddVertexAt(1, UR, 0, 0, 0);
                PL.AddVertexAt(2, DR, 0, 0, 0);
                PL.AddVertexAt(3, DL, 0, 0, 0);
                PL.Closed = true;
                btr.AppendEntity(PL);
            }
        }

        private void InsertPatok(Transaction tr, BlockTableRecord btr, List<double> Dist, List<double> Elev)
        {
            int ip = Dist.IndexOf(0);
            if (ip != -1)
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    using (BlockReference brf = new BlockReference(new Point3d(Dist[ip], Elev[ip], 0), bt["Patok"]))
                    {
                        btr.AppendEntity(brf);
                    }
                }
            }
        }
        


        public void Draw(DataCross CD)
        {
            List<double> Elev = CD.Elevation.ConvertAll(x => double.Parse(x));
            List<double> Dist = CD.Distance.ConvertAll(x => double.Parse(x));
            List<string> Desc = CD.Description;
            double BoundaryLength = 20;
            double boundaryXLeft = CD.MidPoint.X - (BoundaryLength / 2);
            double boundaryXRight = CD.MidPoint.X + (BoundaryLength / 2);

            //FIRST INDEX PERTAMA SETELAH BOUNDARY KIRI
            int first = Dist.FindIndex(s => s > boundaryXLeft);
            //LAST INDEX PERTAMA SEBELUM BOUNDARY KANAN
            int last = Dist.FindLastIndex(s => s < boundaryXRight);

            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForWrite) as BlockTable)
                {
                    using (BlockTableRecord btr = new BlockTableRecord())
                    {
                        if (!bt.Has(CD.NamaPatok))
                        {
                            bt.Add(btr);
                            btr.Name = CD.NamaPatok;

                            int limit = Elev.Count;
                            int ip = Dist.IndexOf(0);

                            //Error Detector
                            if (ip == -1 || (Desc.FindIndex(s => s.Contains("D")) == -1))
                            {
                                crossError += CD.NamaPatok + ", ";
                            }

                            DrawModifiedBoundary(btr, CD, 20);
                            //DrawOriginalBoundary(btr, CD);

                            // Gambar block pelengkap tiap cross
                            InsertPatok(tr, btr, Dist, Elev);


                            DrawCrossPolyline(btr, Dist, Elev);
                            //Gambar Semua SEGMENT

                            for (int i = 0; i < Elev.Count - 1; i++)
                            {
                                DrawSegment(btr, i, Dist, Elev, Desc);
                                DrawTextDistance(btr, i, Dist, CD.Datum);
                            }

                            // Gambar Pelengkap sesuai NODE

                            for (int i = 0; i < Elev.Count; i++)
                            {
                                DrawPembantu1(btr, i, Dist, Elev, CD.Datum);
                                DrawPembantu2(btr, i, Dist, CD.Datum);
                                DrawTextElevation(btr, i, Dist, Elev, CD.Datum);
                                DrawTextLabel(btr, i, Dist, Elev, Desc);
                            }

                            //MID POINT
                            if (CD.PunyaBoundaryDasar == true)
                            {
                                using (DBPoint c = new DBPoint())
                                {
                                    c.Position = new Point3d(CD.Intersect.X, CD.Intersect.Y, 0);
                                    btr.AppendEntity(c);
                                }
                            }




                            // Set the insertion point for the block
                            //btr.Origin = new Point3d(boundaryXLeft - 6, CRD.Datum - 2, 0);
                            btr.Origin = new Point3d(0, 0, 0);

                        }
                    }


                    //BLOCK TABLE
                }
                //TRANSACTION

                tr.Commit();

                //ent.IntersectWith()
            }
        }





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
            Data d = new Data(s);
            Cross x = new Cross(d);
            ProgressMeter pm = new ProgressMeter();
            pm.Start("Processing Cross");
            pm.SetLimit(x.Count);
            int limit = x.Count;
            //limit = 8;
            stopwatch.Start();
            for (int i = 0; i < limit; i++)
            {
                x.Draw(d.CrossDataCollection[i]);
                x.Place(d.CrossDataCollection[i], i);
                pm.MeterProgress();
            }

            if (x.crossError.Split(',').Length != 0)
            {
                ed.WriteMessage($"\nTerdapat {x.crossError.Split(',').Length - 1} kesalahan data: {x.crossError.Remove(x.crossError.Length - 2)}" +
    $"\nProgress selesai dalam waktu {stopwatch.ElapsedMilliseconds} ms\n");
            }
            else
            {
                ed.WriteMessage($"\nProgress selesai dalam waktu {stopwatch.ElapsedMilliseconds} ms\n");
            }

            Application.SetSystemVariable("XCLIPFRAME", 0);
            Application.SetSystemVariable("PDMODE", 3);
            pm.Stop();
            stopwatch.Stop();
            pm.Dispose();

        }
    }
}