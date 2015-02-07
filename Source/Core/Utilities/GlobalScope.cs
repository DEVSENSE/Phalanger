using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using PHP.Core.Reflection;
using PHP.Core.Binders;
using PHP.Core.Emit;

namespace PHP.Core.Utilities
{

    /// <summary>
    /// Convenience class for accessing global functions and global variables
    /// </summary>
    public sealed class GlobalScope : DynamicObject
    {
        private ScriptContext context;
        private ClassesScope classes;

        private ClassesScope Classes
        {
            get
            {
                if (classes == null)
                    classes = new ClassesScope(this);

                return classes;
            }

        }

        /// <summary>
        /// Class for calling constructors of php classes
        /// </summary>
        internal class ClassesScope : DynamicObject
        {
            private GlobalScope globals;

            internal ClassesScope(GlobalScope globals)
            {
                this.globals = globals;
            }

            /// <summary>
            /// Specifies dynamic behavior for invoke operation for global function
            /// </summary>
            public override bool TryInvokeMember(
                InvokeMemberBinder binder,
                Object[] args,
                out Object result
            )
            {
                result = globals.context.NewObject(binder.Name, wrapArgs(args));

                return true;
            }
        }


        /// <summary>
        /// Initialize GlobalScope object
        /// </summary>
        /// <param name="currentContext"></param>
        public GlobalScope(ScriptContext currentContext)
        {
            context = currentContext;
        }

        #region DynamicObject

        /// <summary>
        /// Specifies dynamic behavior for invoke operation for global function
        /// </summary>
        public override bool TryInvokeMember(
            InvokeMemberBinder binder,
            Object[] args,
            out Object result
        )
        {
            result = PhpVariable.Dereference(context.Call(binder.Name, null, null, wrapArgs(args)));
            return true;
        }

        private static object[] wrapArgs(Object[] args)
        {
            object[] wrappedArgs = new object[args.Length];

            for (int i = 0; i < args.Length; ++i)
            {
                Debug.Assert(!(args[i] is PhpReference));
                wrappedArgs[i] = ClrObject.WrapDynamic(args[i]);
            }
            return wrappedArgs;
        }

        /// <summary>
        /// Specifies dynamic behavior for get operation for global variable
        /// </summary>
        public override bool TryGetMember(
            GetMemberBinder binder,
            out Object result
        )
        {
            if (binder.Name == "new")
            {
                result = Classes;
                return true;
            }


            context.GlobalVariables.TryGetValue(binder.Name, out result);
            result = PhpVariable.Dereference(result);

            return true;
        }

        /// <summary>
        /// Specifies dynamic behavior for set operation for global function
        /// </summary>
        public override bool TrySetMember(
            SetMemberBinder binder,
            Object value
        )
        {
            Debug.Assert(!(value is PhpReference));

            context.GlobalVariables[binder.Name] = ClrObject.WrapDynamic(value);
            return true;
        }

        #endregion

        #region Echo

        /// <summary>
        /// Writes data to the current output.
        /// </summary>
        /// <param name="value">Data to be written.</param>
        public void Echo(object value)
        {
            ScriptContext.Echo(value, context);
        }

        /// <summary>
        /// Writes <see cref="PhpBytes" /> data to the current output.
        /// </summary>
        /// <param name="value">Data to be written.</param>
        public void Echo(PhpBytes value)
        {
            ScriptContext.Echo(value, context);
        }

        /// <summary>
        /// Writes <see cref="string" /> to the current output.
        /// </summary>
        /// <param name="value">The string to be written.</param>
        public void Echo(string value)
        {
            ScriptContext.Echo(value, context);
        }

        /// <summary>
        /// Writes <see cref="bool" /> to the current output.
        /// </summary>
        /// <param name="value">The boolean to be written.</param>
        public void Echo(bool value)
        {
            ScriptContext.Echo(value, context);
        }

        /// <summary>
        /// Writes <see cref="int" /> to the current output.
        /// </summary>
        /// <param name="value">The integer to be written.</param>
        public void Echo(int value)
        {
            ScriptContext.Echo(value, context);
        }

        /// <summary>
        /// Writes <see cref="long"/> to the current output.
        /// </summary>
        /// <param name="value">The long integer to be written.</param>
        public void Echo(long value)
        {
            ScriptContext.Echo(value, context);
        }

        /// <summary>
        /// Writes <see cref="double"/> to the current output.
        /// </summary>
        /// <param name="value">The double to be written.</param>
        public void Echo(double value)
        {
            ScriptContext.Echo(value, context);
        }

        #endregion

    }


}
