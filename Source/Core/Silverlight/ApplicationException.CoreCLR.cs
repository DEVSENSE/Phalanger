using System;
using System.Runtime.Serialization;

namespace PHP.CoreCLR
{

    public class ApplicationException : Exception
    {
        
        // Creates a new ApplicationException with its message string set to
        // the empty string, its HRESULT set to COR_E_APPLICATION, 
        // and its ExceptionInfo reference set to null. 
        public ApplicationException()
            : base("ApplicationException")
        {
            //SetErrorCode(__HResults.COR_E_APPLICATION);
        }

        // Creates a new ApplicationException with its message string set to
        // message, its HRESULT set to COR_E_APPLICATION, 
        // and its ExceptionInfo reference set to null. 
        // 
        public ApplicationException(String message)
            : base(message)
        {
            //SetErrorCode(__HResults.COR_E_APPLICATION);
        }

        public ApplicationException(String message, Exception innerException)
            : base(message, innerException)
        {
            //SetErrorCode(__HResults.COR_E_APPLICATION);
        }

    }
}