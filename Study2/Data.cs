using Autodesk.AutoCAD.Geometry;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using static PLC.Utilities;


namespace PLC
{
    public class Data
    {

        public string FilePath { get; set; }
        public int TotalDataLine { get; set; }
        public int TotalCrossNumber { get; set; }
        public int TotalCrossFix { get; set; }
        public List<DataCross> CrossDataCollection = new List<DataCross>();
        public DataPlan DataPlan;
        public List<List<string>> LineData = new List<List<string>>();
        public bool JackVersion = false;

        public Data(string FilePath)
        {
            this.FilePath = FilePath;
            ReadData(this.FilePath);
            TotalDataLine = LineData.Count / 3;
            GetCrossData();
            GetPlanData();
        }

        public void GetCrossData()
        {
            for (int i = 0; i < TotalDataLine; i++)
            {
                int j = 0 + i * 3;
                int k = 3;
                List<List<string>> CrossGroup = new List<List<string>>();
                CrossGroup = LineData.GetRange(j, k);
                DataCross cdx = new DataCross(CrossGroup);
                if (!string.IsNullOrEmpty(cdx.NamaPatok))
                {
                    CrossDataCollection.Add(cdx);
                    TotalCrossNumber++;
                }
                if (cdx.PatokSaja == false)
                {
                    TotalCrossFix++;
                }
            }
        }

        public void GetPlanData()
        {
            DataPlan = new DataPlan(this);
        }


        public void ReadData(string p)
        {
            List<List<string>> Lines = new List<List<string>>();
            using (StreamReader reader = new StreamReader(p))
            using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false;
                if (p.Contains(".txt"))
                {
                    csv.Configuration.Delimiter = "\t";
                    for (int i = 0; i < 8; i++)
                    {
                        csv.Read();
                    }
                }
                IEnumerable<dynamic> records = csv.GetRecords<dynamic>();
                foreach (IDictionary<string, object> items in records)
                {
                    List<string> Line = new List<string>();
                    foreach (KeyValuePair<string, object> item in items)
                    {
                        Line.Add(item.Value.ToString());
                    }
                    if (Line[0].Contains("SALURAN"))
                    {
                        JackVersion = true;
                    }
                    Lines.Add(Line);
                }
            }
            LineData = Lines;
            if (JackVersion)
            {
                LineData.RemoveRange(0, 8);
            }

        }
    }

}