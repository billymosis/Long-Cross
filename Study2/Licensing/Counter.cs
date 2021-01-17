using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace PLC.Licensing
{

    public class Counter
    {
        public static byte[] MyIV = { 140, 212, 3, 14, 55, 76, 71, 88, 9, 10, 65, 172, 138, 149, 151, 178 };

        public static byte[] myKey = Encoding.UTF8.GetBytes("8Tf78#tVVIMPg!0j1t30X&qW");

        public static void DateChecker()
        {
            Dictionary<string, string> j = ReadJSON();
            Config c = new Config(j);
            DateTime last = Convert.ToDateTime(c.LastUseDate);
            DateTime now = DateTime.UtcNow;
            DateTime exp = Convert.ToDateTime(c.ExpiredDate);
            DateTime InstallDate = exp.AddYears(-1);
            TimeSpan remaining = exp - now;
            TimeSpan usableday = exp - InstallDate;

            if (remaining.Days < 0)
            {
                Console.WriteLine("Expired");
            }
            else if (c.TimeTamper)
            {
                DateTime y = GetNistTime().ToUniversalTime();
                if (now.Date == y.Date)
                {
                    c.TimeTamper = false;
                    c.LastUseDate = y.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);
                    byte[] data = EncryptToJSON(c);
                    SaveSettingsInIsoStorage("Settings.dat", data);
                    Console.WriteLine("Thank you for your cooperation. Licensing service is back to normal.");
                    DateChecker();
                }
                else
                {
                    Console.WriteLine("Time violation detected, please change your date to correct date.");
                    Console.WriteLine("Please set your system clock to the Internet Time: " + y.ToUniversalTime().Date.ToShortDateString());
                }
            }
            else if (last.Date > now.Date || now < InstallDate)
            {
                c.TimeTamper = true;
                Console.WriteLine("Time violation detected, please change your date to correct date.");
                DateTime y = GetNistTime().ToUniversalTime();
                Console.WriteLine("Please set your system clock to the Internet Time: " + y.ToUniversalTime().Date.ToShortDateString());
                byte[] data = EncryptToJSON(c);
                SaveSettingsInIsoStorage("Settings.dat", data);
            }
            else if ((now >= InstallDate && now < exp) && now.Date >= last.Date && c.TimeTamper == false)
            {
                c.UsedDateCount = usableday.Days - remaining.Days;
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
                SaveSettingsInIsoStorage("Settings.dat", data);
                Console.WriteLine("In Range");
            }
        }

        public static byte[] EncryptToJSON(object o)
        {
            string json = JsonConvert.SerializeObject(o);

            using (Aes myAes = Aes.Create())
            {
                byte[] encrypted = EncryptStringToBytes_Aes(json, myKey, MyIV);
                return encrypted;
            }
        }

        public static DateTime GetNistTime()
        {
            DateTime result;
            try
            {
                TcpClient client = new TcpClient("time.nist.gov", 13);
                using (StreamReader streamReader = new StreamReader(client.GetStream()))
                {
                    string response = streamReader.ReadToEnd();
                    string utcDateTimeString = response.Substring(7, 17);
                    result = DateTime.ParseExact(utcDateTimeString, "yy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                    return result;
                }
            }
            catch (SocketException SE)
            {
                Console.WriteLine("Request Timeout, Please connect to the internet.");
                Console.WriteLine(SE.Message);
                throw;
            }
        }

        public static void DecreaseAndUpdateCounter()
        {
            Dictionary<string, string> j = ReadJSON();
            Config c = new Config(j);
            Console.WriteLine("awal :" + c.LicenseLimit);
            c.LicenseLimit = c.LicenseLimit - 10;
            Console.WriteLine("menjadi :" + c.LicenseLimit);
            byte[] data = EncryptToJSON(c);
            SaveSettingsInIsoStorage("Settings.dat", data);
            DateChecker();
        }

        public static void SetAndUpdateCounter(int value)
        {
            Dictionary<string, string> j = ReadJSON();
            Config c = new Config(j);
            Console.WriteLine("awal :" + c.LicenseLimit);
            c.LicenseLimit = value;
            Console.WriteLine("menjadi :" + c.LicenseLimit);
            byte[] data = EncryptToJSON(c);
            SaveSettingsInIsoStorage("Settings.dat", data);
            DateChecker();
        }

        public static void InitialWrite()
        {
            const int LICENSE_YEAR_LENGTH = -1;
            string filename = "Settings.dat";
            string LicensePath = @"E:\AutoCAD Project\EasyLicense-master\EasyLicense\DemoProject\bin\Debug\license.lic";

            IsolatedStorageFile applicationStorageFileForUser = IsolatedStorageFile.GetUserStoreForAssembly();
            bool FileExist = applicationStorageFileForUser.FileExists(filename);

            XmlDocument myLicense = new XmlDocument();
            myLicense.Load(LicensePath);
            XmlNodeList x = myLicense.GetElementsByTagName("license");
            string ExpirationDate = x[0].Attributes["expiration"].Value;
            TimeSpan DayCounter = DateTime.UtcNow - Convert.ToDateTime(ExpirationDate).AddYears(LICENSE_YEAR_LENGTH);
            if (FileExist)
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
                };
                TimeSpan remaining = Convert.ToDateTime(coy.ExpiredDate) - Convert.ToDateTime(coy.LastUseDate);
                coy.RemainingDay = remaining.Days;
                byte[] data = EncryptToJSON(coy);
                SaveSettingsInIsoStorage(filename, data);
                DateChecker();
            }

        }

        public static void Main()
        {
            int i = int.Parse(Console.ReadLine());
            switch (i)
            {
                case 1:
                    {
                        InitialWrite();
                        break;
                    }
                case 2:
                    {
                        Console.WriteLine(string.Join(Environment.NewLine, ReadJSON()));
                        break;
                    }
                case 3:
                    {
                        DateChecker();
                        break;
                    }
                case 4:
                    {
                        DecreaseAndUpdateCounter();
                        break;
                    }
                default:
                    break;
            }
            Dictionary<string, string> j = ReadJSON();
            Main();
            Console.ReadLine();
        }

        public static byte[] ReadAllBytes(BinaryReader reader)
        {
            const int bufferSize = 4096;
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[bufferSize];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                {
                    ms.Write(buffer, 0, count);
                }

                return ms.ToArray();
            }
        }

        public static string ReadData(byte[] data)
        {
            return DecryptStringFromBytes_Aes(data, myKey, MyIV);
        }

        public static Dictionary<string, string> ReadJSON()
        {
            byte[] data2 = ReadSettingsFromIsoStorage("Settings.dat");
            string json = ReadData(data2);
            Dictionary<string, string> Dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            return Dict;
        }

        private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException("cipherText");
            }

            if (Key == null || Key.Length <= 0)
            {
                throw new ArgumentNullException("Key");
            }

            if (IV == null || IV.Length <= 0)
            {
                throw new ArgumentNullException("IV");
            }

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
            {
                throw new ArgumentNullException("plainText");
            }

            if (Key == null || Key.Length <= 0)
            {
                throw new ArgumentNullException("Key");
            }

            if (IV == null || IV.Length <= 0)
            {
                throw new ArgumentNullException("IV");
            }

            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
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
