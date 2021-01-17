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
            string publicKey = "<RSAKeyValue><Modulus>3Q3g9xwL0qdl0YzytU0Bg1rd0kqcQeIaqxgD74ay/RBGUbQh5oOeEikpinXw8rFUY81AZwkkD/viXRNDjLRRmcWV347RfloCYU2cZwhk/CnK5LJUSn4+bWf08HfGW6o6joXoTTDvqufnSRNLZignlNyOrer+OTJ7C2nIleAM6jk=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
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
