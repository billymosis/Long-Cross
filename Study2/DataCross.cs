using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static PLC.Utilities;

namespace PLC
{
    public class DataCross
    {
        public List<string> Elevation { get; set; }
        public List<string> Distance { get; set; }
        public List<string> Description { get; set; }

        public bool PunyaBoundaryDasar { get; set; }
        public bool PatokSaja { get; set; }

        public Point3d MidPoint { get; set; }
        public Point2d Intersect { get; set; }

        public double TanggulKiri { get; set; }
        public double TanggulKanan { get; set; }
        public double DasarKiri { get; set; }
        public double DasarKanan { get; set; }

        public int TanggulKiriIndex { get; set; }
        public int TanggulKananIndex { get; set; }
        public int DasarKiriIndex { get; set; }
        public int DasarKananIndex { get; set; }

        public double ElvAsDasar { get; set; }
        public double DistAsDasar { get; set; }
        public string NamaPatok { get; set; }
        // Koordinat Patok
        public double KX { get; set; }
        public double KY { get; set; }
        public double KZ { get; set; }

        public double Datum { get; set; }
        public double MaxDist { get; set; }
        public double MinDist { get; set; }
        public double TotalLength { get; set; }
        public double MaxElv { get; set; }
        public double MinElv { get; set; }
        public string Bangunan { get; set; }
        public string TipeBangunan { get; set; }

        public DataCross(List<List<string>> ls)
        {
            GetData(ls);
        }



        public DataCross GetData(List<List<string>> ls)
        {
            const int BATAS_KOLOM = 10;
            int limit = ls.Count;
            Elevation = ls[0];
            Distance = ls[1];
            Description = ls[2];

            NamaPatok = Elevation[0];
            KX = double.Parse(Elevation[1]);
            KY = double.Parse(Elevation[2]);
            KZ = double.Parse(Elevation[3]);
            if (!string.IsNullOrEmpty(Distance[1]))
            {
                Bangunan = Distance[1];
            }
            if (!string.IsNullOrEmpty(Distance[2]))
            {
                TipeBangunan = Distance[2];
            }


            // Remove leading string
            Elevation.RemoveRange(0, BATAS_KOLOM);
            Distance.RemoveRange(0, BATAS_KOLOM);
            Description.RemoveRange(0, BATAS_KOLOM);


            // Remove trailing empty string
            int m = Elevation.FindIndex(x => x == "");
            int n = Elevation.Count;
            if (!(m == -1))
            {
                Elevation.RemoveRange(m, n - m);
                Description.RemoveRange(m, n - m);
                Distance.RemoveRange(m, n - m);
            }
            return Revamp(this);
        }

        public DataCross Revamp(DataCross y)
        {
            for (int i = 0; i < y.Elevation.Count; i++)
            {
                if (y.Description[i].Contains("T"))
                {
                    if (y.TanggulKiri == 0)
                    {
                        y.TanggulKiri = double.Parse(y.Elevation[i]);
                        y.TanggulKiriIndex = i;
                    }
                    else
                    {
                        y.TanggulKanan = double.Parse(y.Elevation[i]);
                        y.TanggulKananIndex = i;
                    }
                }

                //Dasar = Elevasi terendah
                if (double.Parse(y.Elevation[i]) < y.MinElv || y.MinElv == 0)
                {
                    y.MinElv = double.Parse(y.Elevation[i]);
                    y.Datum = Math.Round(y.MinElv) - 2;
                }

                //MaxElv
                if (double.Parse(y.Elevation[i]) > y.MaxElv || y.MaxElv == 0)
                {
                    y.MaxElv = double.Parse(y.Elevation[i]);
                }

                //MaxDist
                if (double.Parse(y.Distance[i]) > y.MaxDist || y.MaxDist == 0)
                {
                    y.MaxDist = double.Parse(y.Distance[i]);
                }

                //MinDist
                if (double.Parse(y.Distance[i]) < y.MinDist || y.MinDist == 0)
                {
                    y.MinDist = double.Parse(y.Distance[i]);
                }
            }
            y.TotalLength = Math.Abs(y.MaxDist - y.MinDist);

            //Mid Point
            if (y.Description.FindIndex(s => s.Contains("D")) != -1)
            {
                y.PunyaBoundaryDasar = true;
                int ds = y.Description.FindIndex(s => s.Contains("D"));
                int de = y.Description.FindIndex(ds + 1, s => s.Contains("D"));


                Point3d StartPoint = new Point3d(double.Parse(y.Distance[ds]), double.Parse(y.Elevation[ds]), 0);
                Point3d EndPoint = new Point3d(double.Parse(y.Distance[de]), double.Parse(y.Elevation[de]), 0);

                y.DasarKiri = StartPoint.Y;
                y.DasarKanan = EndPoint.Y;
                y.DasarKiriIndex = ds;
                y.DasarKananIndex = de;

                Vector3d v = StartPoint.GetVectorTo(EndPoint);
                y.MidPoint = new Point3d(double.Parse(y.Distance[ds]), double.Parse(y.Elevation[ds]), 0) + v * 0.5;
                if (ds - de == -1)
                {
                    y.Intersect = FindIntersection2d(y.MidPoint, new Point3d(y.MidPoint.X, y.MinElv, 0), StartPoint, EndPoint);
                }
                else
                {
                    int qq = y.Distance.ConvertAll(k => double.Parse(k)).FindIndex(l => l >= y.MidPoint.X);
                    int qw = y.Distance.ConvertAll(k => double.Parse(k)).FindLastIndex(l => l <= y.MidPoint.X);
                    Point3d q = new Point3d(double.Parse(y.Distance[qw]), double.Parse(y.Elevation[qw]), 0);
                    Point3d w = new Point3d(double.Parse(y.Distance[qq]), double.Parse(y.Elevation[qq]), 0);
                    y.Intersect = FindIntersection2d(y.MidPoint, new Point3d(y.MidPoint.X, y.MinElv, 0), q, w);
                    if (double.IsNaN(y.Intersect.X))
                    {
                        y.Intersect = new Point2d(y.MidPoint.X, y.MinElv);
                    }
                }

            }
            else
            {
                y.PunyaBoundaryDasar = false;
                y.PatokSaja = true;
            }

            return y;

        }
    }


}

