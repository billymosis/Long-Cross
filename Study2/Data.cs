using Microsoft.VisualBasic.FileIO;
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
    public List<Data> DataCollection = new List<Data>();
    public void GetData(string Path)
    {
        using (TextFieldParser tfp = new TextFieldParser(Path))
        {

            //  S = Nilai awal sampai akhir
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
                DataCollection.Add(d);
            }

        }
        Revamp();

    }
    public void Revamp()
    {
        foreach (Data item in DataCollection)
        {
            int limit = item.Elevation.IndexOf("");
            if (!(limit == -1))
            {
                item.Elevation.RemoveRange(limit, item.Elevation.Count - limit);
                item.Distance.RemoveRange(limit, item.Distance.Count - limit);
                item.Description.RemoveRange(limit, item.Description.Count - limit);
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
}


