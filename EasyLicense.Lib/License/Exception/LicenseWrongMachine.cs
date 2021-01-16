using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace EasyLicense.Lib.License.Exception
{
    /// <summary>
    ///     Thrown when suitable license machine is wrong.
    /// </summary>
    [Serializable]
    public class LicenseWrongMachine : RhinoLicensingException
    {
        /// <summary>
        ///     Creates a new instance of <seealso cref="LicenseWrongMachine" /> .
        /// </summary>
        public LicenseWrongMachine()
        {
        }

        /// <summary>
        ///     Creates a new instance of <seealso cref="LicenseWrongMachine" /> .
        /// </summary>
        /// <param name="message">error message</param>
        public LicenseWrongMachine(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Creates a new instance of <seealso cref="LicenseWrongMachine" /> .
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="inner">inner exception</param>
        public LicenseWrongMachine(string message, System.Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        ///     Creates a new instance of <seealso cref="LicenseWrongMachine" /> .
        /// </summary>
        /// <param name="info">serialization information</param>
        /// <param name="context">steaming context</param>
        protected LicenseWrongMachine(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
