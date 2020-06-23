using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Study2
{
    class Class6
    {
        public void Readerx(string s)
        {
            Cross Cross1 = new Cross
            {
                Elevation = new List<double>(),
                Distance = new List<double>(),
                Description = new List<string>()
            };
            Dictionary<int, Cross> Crosses = new Dictionary<int, Cross>();
            Crosses.Add(0, Cross1);
            char[] LineSeparator = new char[] { '\r', '\n' };
            char[] charSeparators = new char[] { ',' };

            using (StreamReader reader = new StreamReader(s))
            {

                while (!reader.EndOfStream)
                {

                    string line1 = reader.ReadLine();
                    string[] val1 = line1.Split(charSeparators);
                    foreach (string item in val1)
                    {
                        if (double.TryParse(item, out double temp))
                        {
                            Cross1.Elevation.Add(temp);
                        }
                        else
                        {
                            break;
                        }
                    }
                    string line2 = reader.ReadLine();
                    string[] val2 = line2.Split(charSeparators);
                    foreach (string item in val2)
                    {
                        if (double.TryParse(item, out double temp))
                        {
                            Cross1.Distance.Add(temp);
                        }
                        else
                        {
                            break;
                        }
                    }
                    string line3 = reader.ReadLine();
                    string[] val3 = line3.Split(charSeparators);
                    foreach (string item in val3)
                    {
                        Cross1.Description.Add(item);
                    }
                    break;

                }
            }
            int limit = Cross1.Elevation.Count;
            Cross1.Description.RemoveRange(limit, Cross1.Description.Count - limit);
        }
    }
    
}
