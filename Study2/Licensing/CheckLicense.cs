using EasyLicense.Lib;
using EasyLicense.Lib.License.Validator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PLC.Licensing
{
    public class CheckLicense
    {

        //Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public string LicensePath = "license.lic";
        public string LicensedTo;
        public string LicenseExpiration;
        public string MachineID;
        public bool License2 = false;
        public IDictionary<string, string> Attributes;

        public CheckLicense()
        {
            Licensed();
        }

        public bool Licensed()
        {
            const string publicKey = "<RSAKeyValue><Modulus>0OkWgMgbsdWHvYM15swa/31+pp+ySS6Wv+HpWy1UGWmTk45s8x4301D0BbTL+FwrRWECLxlfy1dozrz2JYU50vP6U+2ekygG37ltYCtbkhvyp8aBsTHIrH8Inq7wIz4q3GdCRPZLfUnwA0YIccMSnJa0UHH1+Mz161K87+j+qsU=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
            LicenseValidator validator = new LicenseValidator(publicKey, LicensePath);
            try
            {
                validator.AssertValidLicense();
                Attributes = validator.LicenseAttributes;
                LicensedTo = Attributes["name"];
                LicenseExpiration = validator.ExpirationDate.ToShortDateString();
                MachineID = Attributes["key"];
                License2 = true;
                return true;
            }
            catch (Exception ex)
            {
                
            }

            return false;
        }
    }
}
