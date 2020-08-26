using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;

public class Data
{
    public List<string> Elevation { get; set; }
    public List<string> Distance { get; set; }
    public List<string> Description { get; set; }
    public double TanggulKiri { get; set; }
    public double TanggulKanan { get; set; }
    public double Dasar { get; set; }
    public double ElvAsDasar { get; set; }
    public double ElvMax { get; set; }
    public double DistAsDasar { get; set; }
    public string NamaPatok { get; set; }
    public double KX { get; set; }
    public double KY { get; set; }
    public double KZ { get; set; }
    public double Datum { get; set; }
    public double BoundLeft { get; set; }
    public double BoundRight { get; set; }
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
                    }
                    else
                    {
                        item.TanggulKanan = double.Parse(item.Elevation[i]);
                    }
                }
                //Dasar = Elevasi terendah
                if (double.Parse(item.Elevation[i]) < item.Dasar || item.Dasar == 0)
                {
                    item.Dasar = double.Parse(item.Elevation[i]);
                    item.Datum = Math.Round(item.Dasar) - 2;
                }

                //ElvMax = ElevasiMax
                if (double.Parse(item.Elevation[i]) > item.ElvMax || item.Dasar == 0)
                {
                    item.ElvMax = double.Parse(item.Elevation[i]);
                }
            }
        }
        return x;
    }
}