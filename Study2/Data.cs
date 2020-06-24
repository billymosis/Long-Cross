using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;
using System.Linq;


public class Data
{
    public List<string> Elevation { get; set; }
    public List<string> Distance { get; set; }
    public List<string> Description { get; set; }
    public List<Data> DataCollection = new List<Data>();
    public void GetData(string Path)
    {
        using (TextFieldParser tfp = new TextFieldParser(Path))
        {
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
                    Elevation = ls[i].Split(new char[] { ',' }).ToList(),
                    Distance = ls[i + 1].Split(new char[] { ',' }).ToList(),
                    Description = ls[i + 2].Split(new char[] { ',' }).ToList()
                };
                DataCollection.Add(d);
            }

        }

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
            }
        }
    }
}


