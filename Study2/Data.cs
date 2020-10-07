using Autodesk.AutoCAD.Geometry;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using static Utilities;

public class Data
{
    public List<string> Elevation { get; set; }
    public List<string> Distance { get; set; }
    public List<string> Description { get; set; }

    public bool PunyaPatok { get; set; }
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
    private List<Data> DataCollection = new List<Data>();

    public Data()
    {
    }

    public List<Data> GetData(string Path)
    {
        using (TextFieldParser tfp = new TextFieldParser(Path))
        {
            // S = Nilai awal sampai akhir
            // l = Nilai per baris sudah dibagi dalam array
            // ls = convert setiap baris array menjadi list
            string s = tfp.ReadToEnd();
            string[] l = s.Split(new char[] { '\r', '\n' });
            List<string> ls = new List<string>();
            foreach (string item in l)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    ls.Add(item);
                }
            }
            int limit = ls.Count;
            for (int i = 0; i < limit; i = i + 3)
            {
                Data d = new Data
                {
                    Elevation = new List<string>(ls[i].Split(new char[] { ',' })),
                    Distance = new List<string>(ls[i + 1].Split(new char[] { ',' })),
                    Description = new List<string>(ls[i + 2].Split(new char[] { ',' }))
                };
                d.NamaPatok = d.Elevation[0];
                d.KX = double.Parse(d.Elevation[1]);
                d.KY = double.Parse(d.Elevation[2]);
                d.KZ = double.Parse(d.Elevation[3]);
                if (!(string.IsNullOrEmpty(d.Distance[1])))
                {
                    d.Bangunan = d.Distance[1];
                }
                if (!(string.IsNullOrEmpty(d.Distance[2])))
                {
                    d.TipeBangunan = d.Distance[2];
                }

                d.Elevation.RemoveRange(0, 10);
                d.Distance.RemoveRange(0, 10);
                d.Description.RemoveRange(0, 10);
                int m = d.Elevation.FindIndex(x => x == "");
                int n = d.Elevation.Count;
                if (!(m == -1))
                {
                    d.Elevation.RemoveRange(m, n - m);
                    d.Description.RemoveRange(m, n - m);
                    d.Distance.RemoveRange(m, n - m);
                }
                DataCollection.Add(d);
            }
        }
        return Revamp(DataCollection);
    }

    public List<Data> Revamp(List<Data> y)
    {
        List<Data> x = y;
        foreach (Data item in x)
        {
            for (int i = 0; i < item.Elevation.Count; i++)
            {
                if (item.Description[i].Contains("T"))
                {
                    if (item.TanggulKiri == 0)
                    {
                        item.TanggulKiri = double.Parse(item.Elevation[i]);
                        item.TanggulKiriIndex = i;
                    }
                    else
                    {
                        item.TanggulKanan = double.Parse(item.Elevation[i]);
                        item.TanggulKananIndex = i;
                    }
                }

                //Dasar = Elevasi terendah
                if (double.Parse(item.Elevation[i]) < item.MinElv || item.MinElv == 0)
                {
                    item.MinElv = double.Parse(item.Elevation[i]);
                    item.Datum = Math.Round(item.MinElv) - 2;
                }

                //MaxElv
                if (double.Parse(item.Elevation[i]) > item.MaxElv || item.MaxElv == 0)
                {
                    item.MaxElv = double.Parse(item.Elevation[i]);
                }

                //MaxDist
                if (double.Parse(item.Distance[i]) > item.MaxDist || item.MaxDist == 0)
                {
                    item.MaxDist = double.Parse(item.Distance[i]);
                }

                //MinDist
                if (double.Parse(item.Distance[i]) < item.MinDist || item.MinDist == 0)
                {
                    item.MinDist = double.Parse(item.Distance[i]);
                }

                item.TotalLength = Math.Abs(item.MaxDist - item.MinDist);


                //Mid Point
                if (item.Description.FindIndex(s => s.Contains("D")) != -1)
                {
                    item.PunyaPatok = true;
                    int ds = item.Description.FindIndex(s => s.Contains("D"));
                    int de = item.Description.FindIndex(ds + 1, s => s.Contains("D"));


                    Point3d StartPoint = new Point3d(double.Parse(item.Distance[ds]), double.Parse(item.Elevation[ds]), 0);
                    Point3d EndPoint = new Point3d(double.Parse(item.Distance[de]), double.Parse(item.Elevation[de]), 0);

                    item.DasarKiri = StartPoint.Y;
                    item.DasarKanan = EndPoint.Y;
                    item.DasarKiriIndex = ds;
                    item.DasarKananIndex = de;

                    Vector3d v = StartPoint.GetVectorTo(EndPoint);
                    item.MidPoint = new Point3d(double.Parse(item.Distance[ds]), double.Parse(item.Elevation[ds]), 0) + v * 0.5;
                    if (ds - de == -1)
                    {
                        item.Intersect = FindIntersection2d(item.MidPoint, new Point3d(item.MidPoint.X, item.MinElv, 0), StartPoint, EndPoint);
                    }
                    else
                    {
                        int qq = item.Distance.ConvertAll(k => double.Parse(k)).FindIndex(l => l >= item.MidPoint.X);
                        int qw = item.Distance.ConvertAll(k => double.Parse(k)).FindLastIndex(l => l <= item.MidPoint.X);
                        Point3d q = new Point3d(double.Parse(item.Distance[qw]), double.Parse(item.Elevation[qw]), 0);
                        Point3d w = new Point3d(double.Parse(item.Distance[qq]), double.Parse(item.Elevation[qq]), 0);
                        item.Intersect = FindIntersection2d(item.MidPoint, new Point3d(item.MidPoint.X, item.MinElv, 0), q, w);
                    }

                }
                else
                {
                    item.PunyaPatok = false;
                }
            }
        }
        return x;
    }
}