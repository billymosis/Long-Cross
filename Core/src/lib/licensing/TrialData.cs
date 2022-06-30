// using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using static PLC.Licensing.Encryptor;

namespace PLC.Licensing
{
    public class TrialData
    {
        public static void DateChecker()
        {
            Dictionary<string, string> j = ReadJSON();
            Config c = new Config(j);
            DateTime last = Convert.ToDateTime(c.LastUseDate);
            DateTime now = DateTime.UtcNow;
            DateTime exp = Global.Licensed ? Convert.ToDateTime(c.ExpiredDate) : DateTime.UtcNow.AddDays(1);
            DateTime InstallDate = Global.Licensed ? exp.AddYears(-1) : exp.AddDays(-1);
            TimeSpan remaining = exp - now;
            TimeSpan usableday = exp - InstallDate;

            if (remaining.Days < 0)
            {
                Utilities.Bubble("Expired", "Your license has expired.");
            }
            else if (c.TimeTamper)
            {
                DateTime y = Utilities.GetNistTime().ToUniversalTime();
                if (now.Date == y.Date)
                {
                    c.TimeTamper = false;
                    c.LastUseDate = y.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);
                    byte[] data = EncryptToJSON(c);
                    SaveSettingsInIsoStorage(filename, data);
                    Utilities.Bubble("Clock Corrected", "Thank you for your cooperation. Licensing service is back to normal.");
                    DateChecker();
                }
                else
                {
                    Utilities.Bubble("Time violation detected, please change your date to correct date.", "Please set your system clock to the Internet Time: " + y.ToUniversalTime().Date.ToShortDateString());
                }
            }
            else if (last.Date > now.Date || now < InstallDate)
            {
                c.TimeTamper = true;
                DateTime y = Utilities.GetNistTime().ToUniversalTime();
                Utilities.Bubble("Time violation detected, please change your date to correct date.", "Please set your system clock to the Internet Time: " + y.ToUniversalTime().Date.ToShortDateString() + "\nand restart the AutoCAD Program.");
                byte[] data = EncryptToJSON(c);
                SaveSettingsInIsoStorage(filename, data);
            }
            else if ((now >= InstallDate && now < exp) && now.Date >= last.Date && c.TimeTamper == false)
            {
                c.UsedDateCount = usableday.Days - remaining.Days;
                c.ExpiredDate = exp.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);
                c.LastUseDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);
                if (c.FirstInstall)
                {
                    c.LicenseLimit = 50;
                }
                if (now.Date != last.Date && c.FirstInstall == false)
                {
                    TimeSpan TS = now - last;
                    if (TS.Days > 1)
                    {
                        c.DayCounter = c.DayCounter + TS.Days;
                        c.LicenseLimit = 50;
                    }
                    else
                    {
                        c.DayCounter++;
                        c.LicenseLimit = 50;
                    }

                }
                c.FirstInstall = false;
                byte[] data = EncryptToJSON(c);
                SaveSettingsInIsoStorage(filename, data);
            }
        }

        public static void DecreaseAndUpdateCounter()
        {
            Dictionary<string, string> j = ReadJSON();
            Config c = new Config(j);
            c.LicenseLimit = c.LicenseLimit - 10;
            byte[] data = EncryptToJSON(c);
            SaveSettingsInIsoStorage(filename, data);
            DateChecker();
        }

        public static void SetAndUpdateCounter(int value)
        {
            Dictionary<string, string> j = ReadJSON();
            Config c = new Config(j)
            {
                LicenseLimit = value
            };
            byte[] data = EncryptToJSON(c);
            SaveSettingsInIsoStorage(filename, data);
            DateChecker();
        }

        public static Dictionary<string, string> ReadJSON()
        {
            byte[] data2 = ReadSettingsFromIsoStorage("Settings.dat");
            string json = ReadData(data2);
            Dictionary<string, string> Dict = new Dictionary<string, string>(); // JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            return Dict;
        }

        private static bool SettingsFileExist()
        {
            IsolatedStorageFile applicationStorageFileForUser = IsolatedStorageFile.GetUserStoreForAssembly();
            return applicationStorageFileForUser.FileExists(filename);
        }

        private static bool LicenseExistandTrue()
        {
            return Global.Licensed && File.Exists(LicensePath);
        }

        public static void InitialWrite()
        {
            const int LICENSE_YEAR_LENGTH = 1;
            string ExpirationDate;
            TimeSpan DayCounter;
            if (LicenseExistandTrue())
            {
                XmlDocument myLicense = new XmlDocument();
                myLicense.Load(LicensePath);
                XmlNodeList x = myLicense.GetElementsByTagName("license");
                ExpirationDate = x[0].Attributes["expiration"].Value;
                DayCounter = DateTime.UtcNow - Convert.ToDateTime(ExpirationDate).AddYears(LICENSE_YEAR_LENGTH);
            }
            else
            {
                ExpirationDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);
                DayCounter = DateTime.UtcNow - Convert.ToDateTime(ExpirationDate).AddDays(-LICENSE_YEAR_LENGTH);
            }

            if (SettingsFileExist())
            {
                DateChecker();
            }
            else
            {
                Config coy = new Config
                {
                    ExpiredDate = ExpirationDate,
                    LastUseDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture),
                    UsedDateCount = 0,
                    DayCounter = DayCounter.Days,
                    FirstInstall = true,
                    Licensed = LicenseExistandTrue(),
                };
                TimeSpan remaining = Convert.ToDateTime(coy.ExpiredDate) - Convert.ToDateTime(coy.LastUseDate);
                coy.RemainingDay = remaining.Days;
                byte[] data = EncryptToJSON(coy);
                SaveSettingsInIsoStorage(filename, data);
                DateChecker();
            }

        }

        private static byte[] ReadSettingsFromIsoStorage(string filename)
        {
            IsolatedStorageFile applicationStorageFileForUser = IsolatedStorageFile.GetUserStoreForAssembly();
            IsolatedStorageFileStream applicationStorageStreamForUser = new IsolatedStorageFileStream(filename, FileMode.Open, applicationStorageFileForUser);
            using (BinaryReader br = new BinaryReader(applicationStorageStreamForUser))
            {
                return ReadAllBytes(br);
            }
        }

        private static void SaveSettingsInIsoStorage(string filename, byte[] content)
        {
            IsolatedStorageFile applicationStorageFileForUser = IsolatedStorageFile.GetUserStoreForAssembly();

            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, applicationStorageFileForUser))
            {
                stream.Write(content, 0, content.Length);
            }
        }
    }

    public class Config
    {
        public Config()
        {
        }

        public Config(Dictionary<string, string> D)
        {
            TimeTamper = bool.Parse(D["TimeTamper"]);
            FirstInstall = bool.Parse(D["FirstInstall"]);
            LastUseDate = D["LastUseDate"];
            ExpiredDate = D["ExpiredDate"];
            RemainingDay = int.Parse(D["RemainingDay"]);
            DayCounter = int.Parse(D["DayCounter"]);
            UsedDateCount = int.Parse(D["UsedDateCount"]);
            LicenseLimit = int.Parse(D["LicenseLimit"]);
        }

        public int DayCounter { get; set; }
        public string ExpiredDate { get; set; }
        public string LastUseDate { get; set; }
        public int RemainingDay { get; set; }
        public bool TimeTamper { get; set; }
        public bool Licensed { get; set; }
        public int UsedDateCount { get; set; }
        public int LicenseLimit { get; set; }
        public bool FirstInstall { get; set; }
    }

}
