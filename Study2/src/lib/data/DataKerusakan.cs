using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PLC
{
    public class DataKerusakan
    {

        [CommandMethod("DAA")]
        public static void DAA()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;

            OpenFileDialog OFD = new OpenFileDialog("Select Cross File", null, "csv; txt", "SelectCross", OpenFileDialog.OpenFileDialogFlags.AllowMultiple);
            System.Windows.Forms.DialogResult DR = OFD.ShowDialog();
            if (DR != System.Windows.Forms.DialogResult.OK) return;

            string[] fileList = OFD.GetFilenames();
            foreach (string path in fileList)
            {
                string s = path;

                Data2 oye = new Data2(s);
            }

        }

        public List<string> Tipe { get; private set; }
        public List<string> Sisi { get; private set; }
        public string NamaPatok { get; private set; }
        public double KX { get; private set; }
        public double KY { get; private set; }
        public double KZ { get; private set; }
        public double Station { get; private set; }
        public double StationKum { get; private set; }
        public List<double> Awal = new List<double>();
        public List<double> Akhir = new List<double>();

        public DataKerusakan(List<List<string>> ls)
        {
            GetData(ls);
        }

        public DataKerusakan GetData(List<List<string>> ls)
        {
            const int BATAS_KOLOM = 6;
            int limit = ls.Count;
            List<string> _Awal = ls[0];
            List<string> _Akhir = ls[1];
            Tipe = ls[2];
            Sisi = ls[3];

            NamaPatok = _Awal[0];
            if (string.IsNullOrEmpty(NamaPatok))
            {
                return null;
            }
            KX = double.TryParse(_Awal[1], out double result) ? result : 0;
            KY = double.TryParse(_Awal[2], out result) ? result : 0;
            KZ = double.TryParse(_Awal[3], out result) ? result : 0;
            Station = double.TryParse(_Awal[4], out result) ? result : 0;
            StationKum = double.TryParse(_Awal[5], out result) ? result : 0;

            // Remove leading string
            _Awal.RemoveRange(0, BATAS_KOLOM);
            _Akhir.RemoveRange(0, BATAS_KOLOM);
            Tipe.RemoveRange(0, BATAS_KOLOM);
            Sisi.RemoveRange(0, BATAS_KOLOM);

            // Remove trailing empty string
            int m = _Awal.FindIndex(x => x == string.Empty);
            int n = _Awal.Count;
            if (!(m == -1))
            {
                _Awal.RemoveRange(m, n - m);
                _Akhir.RemoveRange(m, n - m);
                Tipe.RemoveRange(m, n - m);
                Sisi.RemoveRange(m, n - m);
            }

            foreach (string item in _Awal)
            {
                Awal.Add(double.Parse(item));

            }
            foreach (string item in _Akhir)
            {
                Akhir.Add(double.Parse(item));
            }

            return this;
        }
    }

    public class Data2
    {
        public string FilePath { get; set; }
        public int TotalDataLine { get; set; }
        public int TotalCrossNumber { get; set; }
        public int TotalCrossFix { get; set; }
        public List<DataKerusakan> KerusakanDataCollection = new List<DataKerusakan>();
        public List<List<string>> LineData = new List<List<string>>();
        private Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

        public Data2(string FilePath)
        {
            this.FilePath = FilePath;
            ReadData(this.FilePath);
            TotalDataLine = LineData.Count / 4;
            GetKerusakanData();
            Draw();
        }
        public void Draw()
        {
            using (Transaction tr = Application.DocumentManager.MdiActiveDocument.TransactionManager.StartTransaction())
            {
                using (BlockTable bt = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.BlockTableId, OpenMode.ForRead) as BlockTable)
                {
                    using (BlockTableRecord btr = tr.GetObject(Application.DocumentManager.MdiActiveDocument.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord)
                    {
                        PromptPointResult PRO = ed.GetPoint("\nSelect Point:");
                        if (PRO.Status == PromptStatus.Cancel) return;
                        for (int i = 0; i < KerusakanDataCollection.Count; i++)
                        {
                            DataKerusakan now = KerusakanDataCollection[i];
                            DataKerusakan next = i + 1 == KerusakanDataCollection.Count ? now : KerusakanDataCollection[i + 1];
                            DataKerusakan prev = i == 0 ? now : KerusakanDataCollection[i - 1];

                            const double TebalKotak = 7;
                            const double JarakKotak = 1.2;
                            const double TinggiText = 4;
                            const bool PakaiHatch = false;
                            double StartX;
                            double FinishX;
                            double StartY;

                            StartX = PRO.Value.X + now.StationKum;
                            FinishX = PRO.Value.X + next.StationKum;
                            StartY = PRO.Value.Y;
                            // Garis Station
                            using (Line L = new Line())
                            {
                                L.StartPoint = new Point3d(StartX, StartY, 0);
                                L.EndPoint = new Point3d(FinishX, StartY, 0);
                                L.ColorIndex = 55;
                                btr.AppendEntity(L);
                            }


                            // Garis pembatas patok
                            using (Line L = new Line())
                            {
                                L.StartPoint = new Point3d(StartX, StartY - 1, 0);
                                L.EndPoint = new Point3d(StartX, StartY + 1, 0);
                                L.ColorIndex = 55;
                                btr.AppendEntity(L);
                            }

                            // Text lokasi Patok
                            using (DBText tx = new DBText())
                            {
                                tx.Position = new Point3d(StartX, StartY + 2, 0);
                                tx.TextString = now.NamaPatok;
                                tx.Height = 0.1;
                                tx.ColorIndex = 1;
                                btr.AppendEntity(tx);
                            }


                            // Kotak
                            for (int j = 0; j < now.Awal.Count; j++)
                            {
                                int posisi = 0;
                                if (now.Sisi[j] == "Kanan")
                                {
                                    posisi = -1;
                                }
                                else
                                {
                                    posisi = 1;
                                }
                                using (Polyline PL = new Polyline())
                                {

                                    Point2d UL = new Point2d(now.Awal[j] + StartX, StartY + JarakKotak * posisi);
                                    Point2d UR = new Point2d(now.Akhir[j] + StartX, StartY + JarakKotak * posisi);
                                    Point2d DR = new Point2d(now.Akhir[j] + StartX, StartY + (JarakKotak + TebalKotak) * posisi);
                                    Point2d DL = new Point2d(now.Awal[j] + StartX, StartY + (JarakKotak + TebalKotak) * posisi);
                                    PL.AddVertexAt(0, UL, 0, 0, 0);
                                    PL.AddVertexAt(1, UR, 0, 0, 0);
                                    PL.AddVertexAt(2, DR, 0, 0, 0);
                                    PL.AddVertexAt(3, DL, 0, 0, 0);
                                    PL.ColorIndex = 1;
                                    PL.Closed = true;
                                    ObjectId oid = new ObjectId();
                                    oid = btr.AppendEntity(PL);
                                    tr.AddNewlyCreatedDBObject(PL, true);

                                    ObjectIdCollection oidCol = new ObjectIdCollection
                                    {
                                        oid
                                    };

                                    // Text dalam kotak
                                    using (DBText tx = new DBText())
                                    {
                                        tx.Justify = AttachmentPoint.MiddleCenter;
                                        tx.AlignmentPoint = new Point3d((now.Awal[j] + now.Akhir[j]) / 2 + StartX, StartY + (JarakKotak + TebalKotak) / 2 * posisi, 0);
                                        tx.TextString = (Convert.ToChar(j+65).ToString());
                                        tx.Height = TinggiText;
                                        tx.ColorIndex = 1;
                                        btr.AppendEntity(tx);
                                    }


                                    // Hatch
                                    if (PakaiHatch)
                                    {
                                        using (Hatch Ha = new Hatch())
                                        {
                                            btr.AppendEntity(Ha);
                                            tr.AddNewlyCreatedDBObject(Ha, true);
                                            Ha.PatternScale = 8;
                                            Ha.ColorIndex = 1;
                                            Ha.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
                                            Ha.Associative = true;
                                            Ha.AppendLoop(HatchLoopTypes.Default, oidCol);
                                            Ha.EvaluateHatch(true);
                                        }
                                    }


                                }
                            }


                        }

                    }
                }
                tr.Commit();
            }
        }

        public void GetKerusakanData()
        {
            for (int i = 0; i < TotalDataLine; i++)
            {
                int j = 0 + i * 4;
                int k = 4;
                List<List<string>> KerusakanGroup = new List<List<string>>();
                KerusakanGroup = LineData.GetRange(j, k);
                DataKerusakan cdx = new DataKerusakan(KerusakanGroup);
                if (!string.IsNullOrEmpty(cdx.NamaPatok))
                {
                    KerusakanDataCollection.Add(cdx);
                    TotalCrossNumber++;
                }
            }
        }


        public void ReadData(string p)
        {
            List<List<string>> Lines = new List<List<string>>();
            using (StreamReader reader = new StreamReader(File.Open(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false;
                IEnumerable<dynamic> records = csv.GetRecords<dynamic>();
                foreach (IDictionary<string, object> items in records)
                {
                    List<string> Line = new List<string>();
                    foreach (KeyValuePair<string, object> item in items)
                    {
                        Line.Add(item.Value.ToString());
                        Console.WriteLine("Mantab");
                    }
                    Lines.Add(Line);
                }
            }
            LineData = Lines;
        }
    }
}
