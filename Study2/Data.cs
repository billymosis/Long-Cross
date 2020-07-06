using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;


public class Data
{
    public List<string> Elevation { get; set; }
    public List<string> Distance { get; set; }
    public List<string> Description { get; set; }
    public double TanggulKiri { get; set; }
    public double TanggulKanan { get; set; }
    public double Dasar { get; set; }
    public string NamaPatok { get; set; }
    public double KX { get; set; }
    public double KY { get; set; }
    public double KZ { get; set; }
    public string Bangunan { get; set; }
    public string TipeBangunan { get; set; }
    public List<Data> DataCollection = new List<Data>();
    public void GetData(string Path)
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
                Data d = new Data();

                d.Elevation = ls[i].Split(new char[] { ',' }).ToList();
                d.Distance = ls[i + 1].Split(new char[] { ',' }).ToList();
                d.Description = ls[i + 2].Split(new char[] { ',' }).ToList();
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
        Revamp();

    }
    public void Revamp()
    {
        foreach (Data item in DataCollection)
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
                if (double.Parse(item.Elevation[i]) < item.Dasar || item.Dasar == 0)
                {
                    item.Dasar = double.Parse(item.Elevation[i]);
                }
            }


        }

    }
}


