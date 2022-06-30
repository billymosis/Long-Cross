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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using static PLC.Utilities;

namespace PLC
{

    public class Command
    {



        /// <summary>
        /// Switch Trial and Full Licensed Mode
        /// </summary>
        [CommandMethod("XX")]
        public static void XX()
        {
            if (Global.Licensed)
            {
                Global.Licensed = false;
            }
            else
            {
                Global.Licensed = true;
            }
        }

        [CommandMethod("BUBBLE")]
        public static void Bubble()
        {
            Utilities.Bubble("Test", "This is Bubble");
        }

        private static void Bw_Closed(object sender, TrayItemBubbleWindowClosedEventArgs e)
        {
            TrayItemBubbleWindow x = sender as TrayItemBubbleWindow;
            x.Dispose();
        }

        [CommandMethod("CABOUT")]
        public static void CABOUT()
        {
            GUI.About ab = new GUI.About(Global.CheckLicense);
            Application.ShowModalWindow(Application.MainWindow.Handle, ab, false);
            Initialization init = new Initialization();
        }

        [CommandMethod("CX")]
        public static void CX()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            ed.WriteMessage(string.Join(Environment.NewLine, PLC.Licensing.TrialData.ReadJSON()));
        }

        [CommandMethod("CZ")]
        public static void CZ()
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;
            Global.counter.value = 50;
            ed.WriteMessage($"Global : {Global.counter.ToString()}");
        }

        //[CommandMethod("AW")]
        //public static void AW()
        //{
        //    Document Doc = Application.DocumentManager.MdiActiveDocument;
        //    Editor ed = Doc.Editor;
        //    string s = @"E:/AutoCAD Project/Study2/Study2/data/Cross4.csv";
        //    Data d = new Data(s);
        //    Plan x = new Plan(d);

        //    x.DrawPolygon();
        //    for (int i = 0; i < d.TotalDataLine; i++)
        //    {
        //        x.DrawPlanCross(i);
        //    }
        //    HecRAS hec = new HecRAS(d.DataPlan);
        //    List<XYZ> records = new List<XYZ>();
        //    int k = 0;
        //    foreach (Point3dCollection items in d.DataPlan.RAWSurfaceData)
        //    {
        //        int j = 0;
        //        foreach (Point3d item in items)
        //        {
        //            if (k < d.TotalDataLine)
        //            {
        //                records.Add(new XYZ { Id = j, Name = d.DataPlan.namaPatok[k], Description = d.DataPlan.descriptionList[k][j], X = item.X, Y = item.Y, Z = item.Z });
        //                j++;
        //            }
        //        }
        //        k++;
        //    }
        //    using (StreamWriter writer = new StreamWriter(GetDllPath() + @"\" + "XYZ.csv", false))
        //    using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        //    {
        //        csv.WriteRecords(records);
        //    }
        //    WriteFile(hec.GEORAS, "oke.geo");
        //}

        public class XYZ
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
        }


        //[CommandMethod("DF")]
        //public static void Df()
        //{
        //    Document Doc = Application.DocumentManager.MdiActiveDocument;
        //    Editor ed = Doc.Editor;


        //    OpenFileDialog OFD = new OpenFileDialog("Select Cross File", null, "csv; txt", "SelectCross", OpenFileDialog.OpenFileDialogFlags.AllowMultiple);

        //    OFD.ShowDialog();
        //    string[] fileList = OFD.GetFilenames();
        //    foreach (string path in fileList)
        //    {
        //        string s = path;
        //        string directoryFolder = Path.GetDirectoryName(path);
        //        string fileName = Path.GetFileNameWithoutExtension(path);
        //        Data d = new Data(s);
        //        Plan x = new Plan(d);
        //        x.DrawPolygon();
        //        for (int i = 0; i < d.TotalCrossNumber; i++)
        //        {
        //            x.DrawPlanCross(i);
        //        }
        //    }

        //}

        //[CommandMethod("PlanDraw")]
        //public static void PlanDraw()
        //{
        //    Document Doc = Application.DocumentManager.MdiActiveDocument;
        //    Editor ed = Doc.Editor;
        //    PromptOpenFileOptions POFO = new PromptOpenFileOptions("Select File: ")
        //    {
        //        Filter = "Cross File (*.csv)|*.csv|" + "Cross File TXT Tab Delimeted(*.txt)|*.txt"
        //    };
        //    string path = ed.GetFileNameForOpen(POFO).StringResult;
        //    string s = path;
        //    string directoryFolder = Path.GetDirectoryName(path);
        //    string fileName = Path.GetFileNameWithoutExtension(path);
        //    Data d = new Data(s);
        //    Plan x = new Plan(d);
        //    x.DrawPolygon();
        //    for (int i = 0; i < d.TotalCrossNumber; i++)
        //    {
        //        //if (Global.Licensed)
        //        //{
        //        //    x.DrawPlanCross(i);
        //        //}
        //        //else
        //        //{
        //        //    if (Global.counter.value > 0)
        //        //    {
        //        //        x.DrawPlanCross(i);
        //        //        Global.AddCounter(-1);
        //        //    }
        //        //    else
        //        //    {
        //        //        Utilities.Bubble("Limit",$"You have reached your limit, trial will be reseted tomorrow at {DateTime.Now.AddDays(1).ToShortDateString()}.");
        //        //    }
        //        //}
        //        x.DrawPlanCross(i);

        //    }

        //    HecRAS hec = new HecRAS(d.DataPlan);

        //    List<XYZ> records = new List<XYZ>();
        //    int k = 0;
        //    foreach (Point3dCollection items in d.DataPlan.RAWSurfaceData)
        //    {
        //        int j = 0;
        //        foreach (Point3d item in items)
        //        {
        //            if (k < d.TotalCrossNumber)
        //            {
        //                records.Add(new XYZ { Id = j, Name = d.DataPlan.namaPatok[k], Description = d.DataPlan.descriptionList[k][j], X = item.X, Y = item.Y, Z = item.Z });
        //                j++;
        //            }
        //        }
        //        k++;
        //    }
        //    using (StreamWriter writer = new StreamWriter(directoryFolder + @"\" + fileName + "_XYZ.csv", false))
        //    using (CsvWriter csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        //    {
        //        csv.WriteRecords(records);
        //    }
        //    WriteFile(hec.GEORAS, directoryFolder + @"\" + fileName + ".geo");
        //}




    }
}
