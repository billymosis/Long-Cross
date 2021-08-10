using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace EasyLicense.Lib.License.Validator
{
    public class HardwareInfo
    {
        public HardwareInfo()
        {
        }

        private string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("X2"));
                }
                return builder.ToString();
            }
        }

        public string GetHardwareString()
        {
            string key = MotherboardName + CpuId;
            string str = ComputeSha256Hash(key + "d0b71512-3c2a-4772-acec-8c5b78c6ad07");
            return str.Substring(0, 25);
        }

        public string MotherboardName
        {
            get
            {
                try
                {
                    ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                    ManagementObjectCollection moc = mos.Get();
                    foreach (ManagementObject mo in moc)
                    {
                        return mo["SerialNumber"] as string;
                    }
                }
                catch
                {


                }
                return "Motherboard fail";
            }
        }


        public string CpuId
        {
            get
            {
                try
                {
                    using (ManagementClass mc = new ManagementClass("Win32_Processor"))
                    using (ManagementObjectCollection moc = mc.GetInstances())
                    {
                        foreach (ManagementObject mo in moc)
                        {
                            return mo.Properties["ProcessorId"].Value.ToString().Trim();
                        }
                    }
                }
                catch
                {
                }
                return "Get CpuId fail";
            }
        }
    }
}