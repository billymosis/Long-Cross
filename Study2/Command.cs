using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using static PLC.Utilities;

namespace PLC
{

    public class Command
    {

        [CommandMethod("CABOUT")]
        public static void CABOUT()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            Licensing.CheckLicense x = new PLC.Licensing.CheckLicense();
            GUI.About ab = new GUI.About(x);
            Application.ShowModalWindow(Application.MainWindow.Handle, ab, false);
        }

        [CommandMethod("CX")]
        public static void CX()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            Global.counter--;
            ed.WriteMessage($"Global : {Global.counter.ToString()}");
            Initialization.cmdList.Add("LINE");
        }

        [CommandMethod("CZ")]
        public static void CZ()
        {

            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            Global.counter = 50;
            ed.WriteMessage($"Global : {Global.counter.ToString()}");
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
            for (int i = 0; i < d.TotalDataLine; i++)
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
                    if (k < d.TotalDataLine)
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


        [CommandMethod("DF")]
        public static void Df()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;


            OpenFileDialog OFD = new OpenFileDialog("Select Cross File", null, "csv; txt", "SelectCross", OpenFileDialog.OpenFileDialogFlags.AllowMultiple);

            OFD.ShowDialog();
            string[] fileList = OFD.GetFilenames();
            foreach (string path in fileList)
            {
                string s = path;
                string directoryFolder = Path.GetDirectoryName(path);
                string fileName = Path.GetFileNameWithoutExtension(path);
                Data d = new Data(s);
                Plan x = new Plan(d);
                x.DrawPolygon();
                for (int i = 0; i < d.TotalCrossNumber; i++)
                {
                    x.DrawPlanCross(i);
                }
            }

        }

        [CommandMethod("PlanDraw")]
        public static void PlanDraw()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ")
            {
                Filter = "Cross File (*.csv)|*.csv|" + "Cross File TXT Tab Delimeted(*.txt)|*.txt"
            };
            string path = ed.GetFileNameForOpen(POFO).StringResult;
            string s = path;
            string directoryFolder = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
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
            using (StreamWriter writer = new StreamWriter(directoryFolder + @"\" + fileName + "_XYZ.csv", false))
            using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }
            WriteFile(hec.GEORAS, directoryFolder + @"\" + fileName + ".geo");
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
    }
}
