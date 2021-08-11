using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using static PLC.Utilities;

namespace PLC
{
    public class Table
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

        [CommandMethod("RT")]
        public static void RT()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ");
            string s = ed.GetFileNameForOpen(POFO).StringResult;
            using (StreamReader reader = new StreamReader(s))
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
                                IEnumerable<Table> records = csv.GetRecords<Table>();
                                double y = 0;
                                double baris = -0.1;

                                const double TEXT_HEIGHT = 0.35;
                                const double ROW_HEIGHT = 0.7;
                                const double ROW_INCREMENT = 40;
                                double x0 = 0.5;
                                double x1 = 1.75;
                                double x2 = 15;
                                double x3 = 18.25;
                                double x4 = 21.5;
                                double x5 = 27.15;

                                int jumlahData = 0;
                                int kolom = 0;
                                foreach (Table r in records)
                                {
                                    jumlahData++;
                                    using (Line L = new Line())
                                    {
                                        L.StartPoint = new Point3d(x0 - 0.5, baris, 0);
                                        L.EndPoint = new Point3d(x0 + 29.5, baris, 0);
                                        btr.AppendEntity(L);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x0, y, 0);
                                        tx.TextString = r.Kolom0;
                                        tx.Height = TEXT_HEIGHT;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x1, y, 0);
                                        tx.TextString = r.Kolom1;
                                        tx.Height = TEXT_HEIGHT;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x2, y, 0);
                                        tx.TextString = r.Kolom2;
                                        tx.Height = TEXT_HEIGHT;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x3, y, 0);
                                        tx.TextString = r.Kolom3;
                                        tx.Height = TEXT_HEIGHT;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x4, y, 0);
                                        tx.TextString = r.Kolom4;
                                        tx.Height = TEXT_HEIGHT;
                                        btr.AppendEntity(tx);
                                    }

                                    using (DBText tx = new DBText())
                                    {
                                        tx.Position = new Point3d(x5, y, 0);
                                        tx.TextString = r.Kolom5;
                                        tx.Height = TEXT_HEIGHT;
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
                                        y = 0;
                                        baris = -0.1;

                                        DrawLine(new Point3d(0 + ROW_INCREMENT * kolom, 0, 0), new Point3d(0 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT, 0), btr);
                                        DrawLine(new Point3d(1.5 + ROW_INCREMENT * kolom, 0, 0), new Point3d(1.5 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT, 0), btr);
                                        DrawLine(new Point3d(14 + ROW_INCREMENT * kolom, 0, 0), new Point3d(14 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT, 0), btr);
                                        DrawLine(new Point3d(17.5 + ROW_INCREMENT * kolom, 0, 0), new Point3d(17.5 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT, 0), btr);
                                        DrawLine(new Point3d(21 + ROW_INCREMENT * kolom, 0, 0), new Point3d(21 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT, 0), btr);
                                        DrawLine(new Point3d(26.5 + ROW_INCREMENT * kolom, 0, 0), new Point3d(26.5 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT, 0), btr);
                                        DrawLine(new Point3d(30 + ROW_INCREMENT * kolom, 0, 0), new Point3d(30 + ROW_INCREMENT * kolom, -jumlahData * ROW_HEIGHT, 0), btr);
                                        kolom = kolom + 1;
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
