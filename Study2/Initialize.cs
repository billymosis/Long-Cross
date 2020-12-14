﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using static PLC.Utilities;

namespace PLC
{
    public class Initialization : IExtensionApplication
    {
        public void Initialize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("Billy Plugin");
            ImportBlock();
            LoadLinetype();
        }


        [CommandMethod("DAA")]
        public static void DAA()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross4.csv";
            Data oye = new Data(s);


        }

        [CommandMethod("AW")]
        public static void AW()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross4.csv";
            Data d = new Data(s);
            Plan x = new Plan(d);
            x.DrawPolygon();
            for (int i = 0; i < d.TotalCrossNumber; i++)
            {
                x.DrawPlanCross(i);
            }

            HecRAS hec = new HecRAS(d.DataPlan);

            List<XYZ> records = new List<XYZ>();
            int k = 0;
            foreach (Point3dCollection items in d.DataPlan.RAWSurfaceData)
            {
                int j = 0;
                foreach (Point3d item in items)
                {
                    if (k < d.TotalCrossNumber)
                    {
                        records.Add(new XYZ { Id = j, Name = d.DataPlan.namaPatok[k], Description = d.DataPlan.descriptionList[k][j], X = item.X, Y = item.Y, Z = item.Z });
                        j++;
                    }
                }
                k++;
            }
            using (StreamWriter writer = new StreamWriter(GetDllPath() + @"\" + "XYZ.csv", false))
            using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
            WriteFile(hec.GEORAS, "oke.geo");
        }

        public class XYZ
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }

        [CommandMethod("CrossDraw")]
        public static void CrossDraw()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ");
            string path = ed.GetFileNameForOpen(POFO).StringResult;
            try
            {
                string s = path;
                Stopwatch stopwatch = new Stopwatch();
                Data d = new Data(s);
                Cross x = new Cross(d);
                ProgressMeter pm = new ProgressMeter();
                pm.Start("Processing Cross");
                pm.SetLimit(x.Count);
                int limit = x.Count;
                limit = 8;
                stopwatch.Start();
                for (int i = 0; i < d.TotalCrossNumber; i++)
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
            catch (System.Exception e)
            {
                ed.WriteMessage(e.Message);
                throw;
            }
        }

        [CommandMethod("WR")]
        public static void WR()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;

            string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross4.csv";
            Stopwatch stopwatch = new Stopwatch();
            Data d = new Data(s);
            Cross x = new Cross(d);
            ProgressMeter pm = new ProgressMeter();
            pm.Start("Processing Cross");
            List<int> numbers = new List<int>() { 52, 53, 54, 55 };
            int limit = numbers.Count;
            pm.SetLimit(limit);
            stopwatch.Start();
            for (int i = 0; i < limit; i++)
            {
                x.Draw(d.CrossDataCollection[i]);
                x.Place(d.CrossDataCollection[i], i);
                pm.MeterProgress();
            }

            if (x.crossError != null && x.crossError.Split(',').Length != 0)
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


        [CommandMethod("QE")]
        public static void QE()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;

            string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross4.csv";
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

        private void ImportBlock()
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

        public void Terminate()

        {
            Console.WriteLine("Cleaning up...");
        }
    }
}