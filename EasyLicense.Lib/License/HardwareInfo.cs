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

        private string CalculateMd5Hash(string input)
        {
            MD5 md5 = MD5.Create();

            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }
        public string GetHardwareString()
        {
            string key = MotherboardName + CpuId;
            string str = CalculateMd5Hash(key) + "d0b71512-3c2a-4772-acec-8c5b78c6ad07";
            return str.Substring(0, 6);
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