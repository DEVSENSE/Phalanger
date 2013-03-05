using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Runtime.InteropServices;
using System.ComponentModel;
using PHP.Core.Reflection;
using System.Runtime.Serialization;

namespace PHP.Library.Soap
{
    /// <summary>
    /// This class is used to send SOAP fault responses from the PHP handler. faultcode, faultstring, faultactor and detail are standard elements of a SOAP Fault. 
    /// </summary>
    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif
    public class SoapFault : PHP.Library.SPL.Exception
    {
        internal static void Throw(ScriptContext/*!*/context,
            string faultcode, string faultstring, bool throwSoapFault=true)
        {
            if (throwSoapFault)
            {
                var e = new SoapFault(context, true);
                e.__construct(context, faultcode, faultstring);

                throw new PhpUserException(e);
            }
            else
            {
                PhpException.Throw(PhpError.Error, faultstring);
            }
        }

        #region __construct

        /// <summary>
        /// Initializes a new instance of SoapFault
        /// </summary>
        /// <param name="context"></param>
        /// <param name="faultcode">The error code of the SoapFault.</param>
        /// <param name="faultstring">The error message of the SoapFault.</param>
        /// <param name="faultactor">A string identifying the actor that caused the error. </param>
        /// <param name="detail">More details about the cause of the error. </param>
        /// <param name="faultname">Can be used to select the proper fault encoding from WSDL. </param>
        /// <param name="headerfault">Can be used during SOAP header handling to report an error in the response header.</param>
        /// <returns>A New instance of SoapFault.</returns>
        [ImplementsMethod]
        public object __construct(ScriptContext/*!*/context,
            object faultcode, object faultstring, [Optional]object faultactor,
            [Optional]object detail, [Optional]object faultname,
            [Optional]object headerfault)
        {
            base.__construct(context, faultstring, faultcode);

            return null;
        }

        #endregion

        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SoapFault(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SoapFault(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object __construct(object instance, PhpStack stack)
        {
            object faultcode = stack.PeekValue(1);
            object faultstring = stack.PeekValue(2);
            object faultactor = stack.PeekValueOptional(3);
            object detail = stack.PeekValueOptional(4);
            object faultname = stack.PeekValueOptional(5);
            object headerfault = stack.PeekValueOptional(6);
            stack.RemoveFrame();
            return ((SoapFault)instance).__construct(stack.Context, faultcode, faultstring, faultactor, detail, faultname, headerfault);
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected SoapFault(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

}
