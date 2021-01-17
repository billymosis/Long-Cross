using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using static PLC.Utilities;

namespace PLC
{
    public class Initialization : IExtensionApplication
    {
        public static StringCollection cmdList = new StringCollection();

        public void Initialize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("Billy Plugin");
            PLC.Licensing.Counter.InitialWrite();
            Licensing.CheckLicense CheckLicense = new PLC.Licensing.CheckLicense();

            Application.DocumentManager.DocumentLockModeChanged += new DocumentLockModeChangedEventHandler(VetoCommandIfInList);

            //Set Global variable
            Global.Licensed = CheckLicense.License2;
            Global.counter = int.Parse(PLC.Licensing.Counter.ReadJSON()["LicenseLimit"]);

            ImportBlock();
            LoadLinetype();
        }

        private void VetoCommandIfInList(object sender, DocumentLockModeChangedEventArgs e)
        {
            if (cmdList.Contains(e.GlobalCommandName.ToUpper()))
            {
                e.Veto();
            }
        }


        public void Terminate()

        {
            //string myTempFile = Path.Combine(Path.GetTempPath(), "TempCAA0D");
            //FileInfo file = new FileInfo(myTempFile);
            //file.Delete();

            Application.DocumentManager.DocumentLockModeChanged -= new DocumentLockModeChangedEventHandler(VetoCommandIfInList);

            Licensing.Counter.SetAndUpdateCounter(Global.counter);
            Console.WriteLine("Cleaning up...");
        }
    }
}