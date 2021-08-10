using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace PLC
{
    public static class Utilities
    {

        public static void Bubble(string myTitle, string myMessage)
        {
            TrayItemBubbleWindow bw = new TrayItemBubbleWindow
            {
                Title = myTitle,
                //HyperLink = "https://billymosis.com",
                //HyperText = "Mantab Bang",

                Text = myMessage,
                //Text2 = "",
                IconType = IconType.Information,
            };
            bw.Closed += Bw_Closed;
            Global.MyTray.ShowBubbleWindow(bw);
        }

        private static void Bw_Closed(object sender, TrayItemBubbleWindowClosedEventArgs e)
        {
            TrayItemBubbleWindow x = sender as TrayItemBubbleWindow;
            x.Dispose();
        }

        public static void ImportBlock()
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
        public static void LoadLinetype()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Linetype table for read
                LinetypeTable acLineTypTbl;
                acLineTypTbl = acTrans.GetObject(acCurDb.LinetypeTableId,
                                                    OpenMode.ForRead) as LinetypeTable;

                string sLineTypName = "DASHED";

                if (acLineTypTbl.Has(sLineTypName) == false)
                {
                    // Load the Center Linetype
                    acCurDb.LoadLineTypeFile(sLineTypName, "acad.lin");
                }

                // Save the changes and dispose of the transaction
                acTrans.Commit();
            }
        }

        public static Point2d ConvertPoint2d(Point3d p)
        {
            return new Point2d(p.X, p.Y);
        }

        public static Point3d ConvertPoint3d(Point2d p)
        {
            return new Point3d(p.X, p.Y, 0);
        }

        public static bool IsLeft(Point3d p1, Point3d p2)
        {
            return p1.X < p2.X;
        }

        public static bool IsLeft(Point2d p1, Point2d p2)
        {
            return p1.X < p2.X;
        }

        public static bool IsRight(Point3d p1, Point3d p2)
        {
            return p1.X > p2.X;
        }

        public static bool IsRight(Point2d p1, Point2d p2)
        {
            return p1.X > p2.X;
        }

        public static Point2d GetMidPoint(Point2d first, Point2d second)
        {
            return new Point2d(Math.Abs(first.X + second.X) / 2, Math.Abs(first.Y + second.Y) / 2);
        }

        public static Point2d FindIntersection2d(Point3d s1, Point3d e1, Point3d s2, Point3d e2)
        {
            double a1 = e1.Y - s1.Y;
            double b1 = s1.X - e1.X;
            double c1 = a1 * s1.X + b1 * s1.Y;

            double a2 = e2.Y - s2.Y;
            double b2 = s2.X - e2.X;
            double c2 = a2 * s2.X + b2 * s2.Y;

            double delta = a1 * b2 - a2 * b1;
            //If lines are parallel, the result will be (NaN, NaN).
            return delta == 0 ? new Point2d(double.NaN, double.NaN)
                : new Point2d((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
        }

        public static Point2d FindIntersection2d(Point2d s1, Point2d e1, Point2d s2, Point2d e2)
        {
            double a1 = e1.Y - s1.Y;
            double b1 = s1.X - e1.X;
            double c1 = a1 * s1.X + b1 * s1.Y;

            double a2 = e2.Y - s2.Y;
            double b2 = s2.X - e2.X;
            double c2 = a2 * s2.X + b2 * s2.Y;

            double delta = a1 * b2 - a2 * b1;
            //If lines are parallel, the result will be (NaN, NaN).
            return delta == 0 ? new Point2d(double.NaN, double.NaN)
                : new Point2d((b2 * c1 - b1 * c2) / delta, (a1 * c2 - a2 * c1) / delta);
        }

        public static int ClosestTo(this IEnumerable<int> collection, int target)
        {
            // NB Method will return int.MaxValue for a sequence containing no elements.
            // Apply any defensive coding here as necessary.
            int closest = int.MaxValue;
            int minDifference = int.MaxValue;
            foreach (int element in collection)
            {
                long difference = Math.Abs((long)element - target);
                if (minDifference > difference)
                {
                    minDifference = (int)difference;
                    closest = element;
                }
            }

            return closest;
        }

        public static double CR2D(double radians)
        {
            return (180 / Math.PI) * radians;
        }

        public static Point2d PolarPoints(Point2d pPt, double dAng, double dDist)
        {
            return new Point2d(pPt.X + dDist * Math.Cos(dAng),
                                 pPt.Y + dDist * Math.Sin(dAng));
        }
        public static Point3d PolarPoints(Point3d pPt, double dAng, double dDist)
        {
            return new Point3d(pPt.X + dDist * Math.Cos(dAng),
                                 pPt.Y + dDist * Math.Sin(dAng),
                                 pPt.Z);
        }

        public static double GetSlope(Point2d first, Point2d second)
        {
            return (second.Y - first.Y) / (second.X - first.X);
        }

        public static double GetReciprocal(double value)
        {
            return (1 / value) * -1;
        }

        public static double GetConstant(double X, double Y, double Reciprocal)
        {
            return Y - (Reciprocal * X);
        }

        public static int GetQuadrant(Point3d first, Point3d second)
        {
            int Quadrant = 0;
            if (second.X > first.X && second.Y > first.Y)
            {
                Quadrant = 1;
            }
            else if (second.X < first.X && second.Y > first.Y)
            {
                Quadrant = 2;
            }
            else if (second.X < first.X && second.Y < first.Y)
            {
                Quadrant = 3;
            }
            else if (second.X > first.X && second.Y < first.Y)
            {
                Quadrant = 4;
            }

            return Quadrant;
        }

        public static int[] BothQuadrant(Point3d Before, Point3d Now, Point3d Next)
        {
            int[] array = new int[] { GetQuadrant(Before, Now), GetQuadrant(Next, Now) };
            return array;
        }

        public static List<Point3d> ConvertP3DC2List(Point3dCollection P3DC)
        {
            List<Point3d> P3D = new List<Point3d>();
            foreach (Point3d item in P3DC)
            {
                P3D.Add(item);
            }
            return P3D;
        }

        public static DateTime GetNistTime()
        {
            DateTime result;
            try
            {
                TcpClient client = new TcpClient("time.nist.gov", 13);
                using (StreamReader streamReader = new StreamReader(client.GetStream()))
                {
                    string response = streamReader.ReadToEnd();
                    string utcDateTimeString = response.Substring(7, 17);
                    result = DateTime.ParseExact(utcDateTimeString, "yy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                    return result;
                }
            }
            catch (SocketException SE)
            {
                Utilities.Bubble("Warning", "Request Timeout, Please connect to the internet.\n Error Message: " + SE.Message);
                throw;
            }
        }

        public static string GetDllPath()
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filename = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
            string filePathRelativeToAssembly = Path.Combine(assemblyPath, @"..\SomeFolder\SomeRelativeFile.txt");
            string normalizedPath = Path.GetFullPath(filePathRelativeToAssembly);

            return assemblyPath;
        }

        public static void WriteFile(string[] content, string filename)
        {
            using (StreamWriter file = new StreamWriter(GetDllPath() + @"\" + filename, false))
            {
                foreach (string item in content)
                {
                    file.WriteLine(item);
                }

            }
        }

        public static void WriteFile(string content, string filename)
        {
            using (StreamWriter file = new StreamWriter(filename, false))
            {

                file.WriteLine(content);


            }
        }

        public static void DrawLine(Point3d Awal, Point3d Akhir, BlockTableRecord btr)
        {
            using (Line L = new Line())
            {
                L.StartPoint = Awal;
                L.EndPoint = Akhir;
                btr.AppendEntity(L);

            }
        }

        public static void DrawLine(Point3d Awal, Point3d Akhir, BlockTableRecord btr, int WARNA)
        {
            using (Line L = new Line())
            {
                L.StartPoint = Awal;
                L.EndPoint = Akhir;
                L.ColorIndex = WARNA;
                btr.AppendEntity(L);

            }
        }

    }
}