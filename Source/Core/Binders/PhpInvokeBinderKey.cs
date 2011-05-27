using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core.Reflection;


namespace PHP.Core.Binders
{

    public struct PhpInvokeBinderKey : IEquatable<PhpInvokeBinderKey>
    {
        /// <summary>
        /// Number of arguments in the signature.
        /// </summary>
        private readonly int argumentCount;

        /// <summary>
        /// Number of generic type arguments in the signature.
        /// </summary>
        private readonly int genericArgumentCount;

        /// <summary>
        /// Binding flags
        /// </summary>
        private readonly Type returnType;

        /// <summary>
        /// Method name
        /// </summary>
        private readonly string methodName;

        /// <summary>
        /// Method name
        /// </summary>
        private readonly DTypeDesc callerClassContext;


        private readonly string stringKey;


        #region Construction

        public PhpInvokeBinderKey(string methodName, int genericParamsCount, int paramsCount, DTypeDesc callerClassContext, Type returnType)
        {
            this.methodName = methodName;
            this.genericArgumentCount = genericParamsCount;
            this.argumentCount = paramsCount;
            this.callerClassContext = callerClassContext;
            this.returnType = returnType;

            
            StringBuilder sb = new StringBuilder(( methodName != null ? methodName.Length : 0) + returnType.Name + 16);

            sb.Append(methodName);
            sb.Append("|");
            sb.Append(argumentCount);
            sb.Append("|");
            sb.Append(genericArgumentCount); 
            sb.Append("|");
            sb.Append(returnType.Name); 
            sb.Append("|");

            if (callerClassContext != null)
                sb.Append(callerClassContext.GetHashCode());

            stringKey = sb.ToString();
        }


        #endregion


        #region IEquatable<PhpInvokeBinderKey> Members

        public bool Equals(PhpInvokeBinderKey other)
        {
            if (methodName != other.methodName)
                return false;

            if (argumentCount != other.argumentCount)
                return false;

            if (genericArgumentCount != other.genericArgumentCount)
                return false;

            if (returnType != other.returnType)
                return false;

            if (callerClassContext != other.callerClassContext)
                return false;

            return true;
        }

        #endregion


        //public override int GetHashCode()
        //{
        //    int result = 1254645177;

        //    for (int i = 0; i < types.Length; i++)
        //        result ^= types[i].GetHashCode();

        //    return result;
        //}

        public override string ToString()
        {
            return stringKey;
        }


    }
}
