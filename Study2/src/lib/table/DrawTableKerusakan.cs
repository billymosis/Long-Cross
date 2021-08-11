using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using static PLC.Utilities;

namespace PLC
{
    public class TableKerusakan
    {
        [Index(0)]
        public string Kolom0 { get; set; }

        [Index(1)]
        public string Kolom1 { get; set; }

        [Index(2)]
        public string Kolom2 { get; set; }

        [Index(3)]
        public string Kolom3 { get; set; }

        [Index(4)]
        public string Kolom4 { get; set; }

        [Index(5)]
        public string Kolom5 { get; set; }

        [Index(6)]
        public string Kolom6 { get; set; }

        [Index(7)]
        public string Kolom7 { get; set; }

        [Index(8)]
        public string Kolom8 { get; set; }

        [CommandMethod("DE")]
        public static void DE()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ");
            POFO.Filter = "Data Tabel (*.csv)| *csv";
            string s = ed.GetFileNameForOpen(POFO).StringResult;
            using (StreamReader reader = new StreamReader(File.Open(s, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
                {
                    using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable)
                    {
                        using (BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                        {
                            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                            {
                                csv.Configuration.HasHeaderRecord = false;
                                for (int i = 0; i < 4; i++)
                                {
                                    csv.Read();
                                }

                                IEnumerable<TableKerusakan> records = csv.GetRecords<TableKerusakan>();
                                double y = 0;
                                double baris = -0.1;

                                const double TEXT_HEIGHT = 0.35;
                                const double ROW_HEIGHT = 0.7;
                                const double ROW_INCREMENT = 40;

                                const int MERAH = 1; //KETEBALAN
                                const int SILVER = 8;

                                double x0 = 0.5;
                                double x1 = 5.7;
                                double x2 = 7.7;
                                double x3 = 10.0;
                                double x4 = 11.1;
                                double x5 = 13.5;
                                double x6 = 16.0;
                                double x7 = 19.0;
                                double x8 = 26.0;

                                string alphabet = "";

                                int pageCounter = 1;

                                int myCounter = 0;

                                int jumlahData = 0;
                                int kolom = 0;
                                foreach (TableKerusakan r in records)
                                {
                                    jumlahData++;
                                    //using (Line L = new Line())
                                    //{
                                    //    L.StartPoint = new Point3d(x0 - 0.5, baris, 0);
                                    //    L.EndPoint = new Point3d(x0 + 29.5, baris, 0);
                                    //    btr.AppendEntity(L);
                                    //}

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x0, y, 0);
                                        tx.TextString = r.Kolom0;
                                        tx.Height = TEXT_HEIGHT;
                                        tx.ColorIndex = MERAH;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x1, y, 0);
                                        tx.TextString = r.Kolom1;
                                        tx.Height = TEXT_HEIGHT;
                                        tx.ColorIndex = MERAH;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x2, y, 0);
                                        tx.TextString = r.Kolom3;
                                        tx.Height = TEXT_HEIGHT;
                                        tx.ColorIndex = MERAH;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {

                                        if (!string.IsNullOrEmpty(r.Kolom4))
                                        {
                                            alphabet = (Convert.ToChar(myCounter + 65).ToString());
                                            myCounter++;
                                        }
                                        else
                                        {
                                            alphabet = "";
                                            myCounter = 0;
                                        }
                                        tx.Position = new Point3d(x3, y, 0);
                                        tx.TextString = alphabet;
                                        tx.Height = TEXT_HEIGHT;
                                        tx.ColorIndex = MERAH;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x4, y, 0);
                                        tx.TextString = r.Kolom4;
                                        tx.Height = TEXT_HEIGHT;
                                        tx.ColorIndex = MERAH;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x5, y, 0);
                                        tx.TextString = r.Kolom5;
                                        tx.Height = TEXT_HEIGHT;
                                        tx.ColorIndex = MERAH;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x6, y, 0);
                                        if (r.Kolom6 == "0")
                                        {
                                            tx.TextString = "";
                                        }
                                        else
                                        {
                                            tx.TextString = r.Kolom6;
                                        }
                                        tx.Height = TEXT_HEIGHT;
                                        tx.ColorIndex = MERAH;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x7, y, 0);
                                        tx.TextString = r.Kolom7;
                                        tx.Height = TEXT_HEIGHT;
                                        tx.ColorIndex = MERAH;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x8, y, 0);
                                        tx.TextString = r.Kolom8;
                                        tx.Height = TEXT_HEIGHT;
                                        tx.ColorIndex = MERAH;
                                        btr.AppendEntity(tx);
                                    }


                                    baris = baris - ROW_HEIGHT;
                                    y = y - ROW_HEIGHT;
                                    if (jumlahData % 50 == 0)
                                    {
                                        x0 = x0 + ROW_INCREMENT;
                                        x1 = x1 + ROW_INCREMENT;
                                        x2 = x2 + ROW_INCREMENT;
                                        x3 = x3 + ROW_INCREMENT;
                                        x4 = x4 + ROW_INCREMENT;
                                        x5 = x5 + ROW_INCREMENT;
                                        x6 = x6 + ROW_INCREMENT;
                                        x7 = x7 + ROW_INCREMENT;
                                        x8 = x8 + ROW_INCREMENT;
                                        y = 0;
                                        baris = -0.1;
                                          

                                        // INPUT LINE VERTIKAL
                                        DrawLine(new Point3d(0 + ROW_INCREMENT * kolom, 0.7, 0), new Point3d(0 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT / pageCounter, 0), btr, SILVER);
                                        DrawLine(new Point3d(5.5 + ROW_INCREMENT * kolom, 0.7, 0), new Point3d(5.5 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT / pageCounter, 0), btr, SILVER);
                                        DrawLine(new Point3d(7.5 + ROW_INCREMENT * kolom, 0.7, 0), new Point3d(7.5 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT / pageCounter, 0), btr, SILVER);
                                        DrawLine(new Point3d(9.8 + ROW_INCREMENT * kolom, 0.7, 0), new Point3d(9.8 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT / pageCounter, 0), btr, SILVER);
                                        DrawLine(new Point3d(10.8 + ROW_INCREMENT * kolom, 0.7, 0), new Point3d(10.8 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT / pageCounter, 0), btr, SILVER);
                                        DrawLine(new Point3d(13.2 + ROW_INCREMENT * kolom, 0.7, 0), new Point3d(13.2 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT / pageCounter, 0), btr, SILVER);
                                        DrawLine(new Point3d(15.7 + ROW_INCREMENT * kolom, 0.7, 0), new Point3d(15.7 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT / pageCounter, 0), btr, SILVER);
                                        DrawLine(new Point3d(17.4 + ROW_INCREMENT * kolom, 0.7, 0), new Point3d(17.4 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT / pageCounter, 0), btr, SILVER);
                                        DrawLine(new Point3d(24.3 + ROW_INCREMENT * kolom, 0.7, 0), new Point3d(24.3 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT / pageCounter, 0), btr, SILVER);
                                        DrawLine(new Point3d(30.0 + ROW_INCREMENT * kolom, 0.7, 0), new Point3d(30.0 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT / pageCounter, 0), btr, SILVER);
                                        // input line horizontal
                                        DrawLine(new Point3d(0 +((pageCounter -1) * 40), 0.7, 0), new Point3d(30 + ((pageCounter - 1) * 40), 0.7, 0), btr, SILVER);
                                        DrawLine(new Point3d(0 + ((pageCounter - 1) * 40), (-jumlahData * ROW_HEIGHT / pageCounter), 0), new Point3d(30 + ((pageCounter - 1) * 40), (-jumlahData * ROW_HEIGHT / pageCounter), 0), btr, SILVER);

                                        kolom = kolom + 1;
                                        pageCounter++;

                                    }
                                }


                            }
                        }
                    }
                    tr.Commit();
                }
            }
        }

    }




}
