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
using System.Drawing;
using System.Globalization;
using System.IO;
using static PLC.Utilities;
using static Dreambuild.AutoCAD.CodePackTest;
using Newtonsoft.Json;

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
            CreateLayer("CUT-REGION", 1);
            CreateLayer("FILL-REGION", 2);
            CreateLayer("BOTH-REGION", 3);
            //CanalCADTray();

            //Global.CheckLicense = new PLC.Licensing.CheckLicense();
            //PLC.Licensing.TrialData.InitialWrite();
            //ed.WriteMessage(string.Join(Environment.NewLine, PLC.Licensing.TrialData.ReadJSON()));


            ////Application.DocumentManager.DocumentLockModeChanged += new DocumentLockModeChangedEventHandler(VetoCommandIfInList);

            ////Set Global variable
            ////Change Licensed to false to set Trial Mode
            //Global.Licensed = Global.CheckLicense.License2;
            ////Global.Licensed = false;
            //Global.counter = new Counter(int.Parse(PLC.Licensing.TrialData.ReadJSON()["LicenseLimit"]));

            AppConfig appConfig = new AppConfig();

            ImportBlock(appConfig.BlockPath);
            LoadLinetype();
            LoadStyle();
        }

        //private void VetoCommandIfInList(object sender, DocumentLockModeChangedEventArgs e)
        //{
        //    if (cmdList.Contains(e.GlobalCommandName.ToUpper()))
        //    {
        //        e.Veto();
        //    }
        //}

        //private void CanalCADTray()
        //{
        //    Document Doc = Application.DocumentManager.MdiActiveDocument;
        //    Editor ed = Doc.Editor;
        //    Global.MyTray = new TrayItem
        //    {
        //        ToolTipText = "Canal CAD",
        //        Icon = new Icon(SystemIcons.Information, 16, 16),
        //    };
        //    Application.StatusBar.TrayItems.Add(Global.MyTray);
        //}


        public void Terminate()

        {
            //string myTempFile = Path.Combine(Path.GetTempPath(), "TempCAA0D");
            //FileInfo file = new FileInfo(myTempFile);
            //file.Delete();

            //Application.DocumentManager.DocumentLockModeChanged -= new DocumentLockModeChangedEventHandler(VetoCommandIfInList);

            //Licensing.TrialData.SetAndUpdateCounter(Global.counter.value);
            Console.WriteLine("Cleaning up...");
        }
    }

    public class AppConfig
    {
        const string APP_CONFIG_DIRECTORY = "CanalCAD";
        public ConfigSchema Schema { get; set; }
        public string BlockPath { get; set; }
        private string Filename { get; set; } = "config.json";
        private string InstallationPath { get; set; }

        public AppConfig()
        {
            this.InstallationPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            ReadConfigFile();
            FinalPath();
        }

        private void ReadConfigFile()
        {
            string fullDirectory = InstallationPath + @"\" + APP_CONFIG_DIRECTORY;
            string configPath = fullDirectory + @"\" + this.Filename;
            if (FileOrDirectoryExist(configPath))
            {
                var content = ReadFile(configPath);
                this.Schema = JsonConvert.DeserializeObject<ConfigSchema>(content);
            }
            else
            {
                Application.ShowAlertDialog($"Config file not found \n{configPath}");
            }
        }

        public void FinalPath()
        {
            string path = this.Schema.BlockPath;
            string directory = InstallationPath + @"\" + APP_CONFIG_DIRECTORY;
            if (path[0] == '.')
            {
                path = directory + path.Substring(1);
            }
            this.BlockPath = path;
        }
    }

    public class ConfigSchema
    {
        public string BlockPath { get; set; }
    }
}