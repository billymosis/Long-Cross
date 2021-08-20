using Autodesk.AutoCAD.Geometry;
using CsvHelper;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static PLC.Utilities;


namespace PLC
{
    public class Data
    {

        public string FilePath { get; set; }
        public int TotalDataLine { get; set; }
        public int TotalCrossNumber { get; set; }
        public int TotalCrossFix { get; set; }
        public Canal canal;
        public List<List<string>> LineData = new List<List<string>>();
        public bool JackVersion = false;

        public Data(string FilePath)
        {
            this.FilePath = FilePath;
            ReadData(this.FilePath);
            TotalDataLine = LineData.Count / 3;
            canal = GetNodeData();
        }

        public Canal GetNodeData()
        {
            const int BATAS_KOLOM = 10;
            Canal canal = new Canal();
            for (int i = 0; i < TotalDataLine; i++)
            {
                int j = 0 + i * 3;
                int k = 3;
                List<List<string>> CrossGroup = new List<List<string>>();
                CrossGroup = LineData.GetRange(j, k);
                string patok = CrossGroup[0][0];
                double x_base = double.Parse(CrossGroup[0][1]);
                double y_base = double.Parse(CrossGroup[0][2]);
                double z_base = double.Parse(CrossGroup[0][3]);
                int structureCode = int.TryParse(CrossGroup[1][2], out structureCode) ? structureCode : 0;
                Bangunan TipeBangunan = (Bangunan)structureCode;
                string namaBangunan = CrossGroup[1][1];

                List<double> Elevation = CrossGroup[0].GetRange(BATAS_KOLOM, CrossGroup[0].Count - BATAS_KOLOM).Where(x => !string.IsNullOrEmpty(x)).Select(x => double.Parse(x)).ToList();
                List<double> Distance = CrossGroup[1].GetRange(BATAS_KOLOM, CrossGroup[1].Count - BATAS_KOLOM).Where(x => !string.IsNullOrEmpty(x)).Select(x => double.Parse(x)).ToList();
                List<string> Description = CrossGroup[2].GetRange(BATAS_KOLOM, CrossGroup[2].Count - BATAS_KOLOM).GetRange(0, Elevation.Count);
                LinkedList<Node> mynode = new LinkedList<Node>();

                for (int l = 0; l < Elevation.Count; l++)
                {
                    Node a = new Node(Elevation[l], Distance[l], Description[l]);
                    mynode.AddLast(a);
                }
                Nodes nodes = new Nodes(x_base, y_base, z_base, mynode, patok, TipeBangunan, namaBangunan, Pengukuran.Tanggul);
                canal.AddNodes(nodes);

            }
            canal.AngleCalculate();
            return canal;
        }

        public void ReadData(string p)
        {
            List<List<string>> Lines = new List<List<string>>();
            using (StreamReader reader = new StreamReader(File.Open(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
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