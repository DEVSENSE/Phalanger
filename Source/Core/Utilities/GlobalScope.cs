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
    #region BaseScope

    /// <summary>
    /// Base class for all the convenience dynamic objects for accessing global elements 
    /// </summary>
    public abstract class BaseScope : DynamicObject
    {
        private ScriptContext context;
        private string ns;
        private string nsSlash;

        protected const string ConstID = "const";
        protected const string NamespaceID = "namespace";
        protected const string ClassID = "class";

        public string Namespace
        {
            get { return ns; }
        }

        public bool UseNamespaces
        {
            get { return ns != null; }
        }

        public ScriptContext Context
        {
            get { return context; }
        }

        internal BaseScope(ScriptContext context)
        {
            this.context = context;
        }

        internal BaseScope(ScriptContext context, string namespaceName)
            :this(context)
        {
            this.ns = namespaceName;
            this.nsSlash = this.ns + QualifiedName.Separator;
        }

        /// <summary>
        /// Transforms given name to be full name including namespaces
        /// </summary>
        public string GetFullName(string name)
        {
            if (UseNamespaces)
                return nsSlash + name;

            return name;
        }

        /// <summary>
        /// Wrap all arguments to Phalanger objects only if the type is not primitive
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected static object[] wrapArgs(Object[] args)
        {
            object[] wrappedArgs = new object[args.Length];

            for (int i = 0; i < args.Length; ++i)
            {
                Debug.Assert(!(args[i] is PhpReference));
                wrappedArgs[i] = ClrObject.WrapDynamic(args[i]);
            }
            return wrappedArgs;
        }
    }

    #endregion

    #region GlobalScope

    /// <summary>
    /// Convenience class for accessing global functions and global variables
    /// </summary>
    public class GlobalScope : BaseScope
    {
        private ClassesScope classes;
        private NamespaceScope namespaces;
        private ConstsScope consts;

        /// <summary>
        /// Gets ClassesScope dynamic object that represents types defined in global scope
        /// </summary>
        protected ClassesScope Classes
        {
            get
            {
                if (classes == null)
                {
                    if (UseNamespaces)
                        classes = new ClassesScope(this.Context, Namespace);
                    else
                        classes = new ClassesScope(this.Context);
                }

                return classes;
            }
        }

        /// <summary>
        /// Gets ConstsScope dynamic object that represents types defined in global scope
        /// </summary>
        protected ConstsScope Consts
        {
            get
            {
                if (consts == null)
                {
                    if (UseNamespaces)
                        consts = new ConstsScope(this.Context, Namespace);
                    else
                        consts = new ConstsScope(this.Context);
                }

                return consts;
            }
        }

        /// <summary>
        /// Gets NamespaceScope dynamic object that represents namespaces defined in global scope
        /// </summary>
        private NamespaceScope Namespaces
        {
            get
            {
                if (namespaces == null)
                    namespaces = new NamespaceScope(this.Context);

                return namespaces;
            }
        }

        /// <summary>
        /// Initialize GlobalScope object
        /// </summary>
        /// <param name="currentContext"></param>
        internal GlobalScope(ScriptContext currentContext)
            : base(currentContext)
        {
        }

        /// <summary>
        /// Initialize GlobalScope object with namespace specified
        /// </summary>
        /// <param name="currentContext"></param>
        /// <param name="namespaceName"></param>
        protected GlobalScope(ScriptContext currentContext, string namespaceName)
            : base(currentContext, namespaceName)
        {
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
            return TryInvokeMember(binder.Name, args, out result);
        }

        /// <summary>
        /// Specifies dynamic behavior for get operation for global variable
        /// </summary>
        public override bool TryGetMember(
            GetMemberBinder binder,
            out Object result
        )
        {
            switch (binder.Name)
            {
                case ClassID:
                    result = Classes;
                    return true;

                case ConstID:
                    result = Consts;
                    return true;

                case NamespaceID:
                    result = Namespaces;
                    return true;
            }

            result = PhpVariable.Unwrap(Operators.GetVariable(Context, null, binder.Name));
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
            return TrySetMember(binder.Name, value);
        }

        #endregion


        /// <summary>
        /// Specifies dynamic behavior for invoke operation for global function
        /// </summary>
        public bool TryInvokeMember(
            string memberName,
            Object[] args,
            out Object result
        )
        {
            result = PhpVariable.Unwrap(PhpVariable.Dereference(Context.Call(memberName, null, null, wrapArgs(args))));
            return true;
        }

        public bool TrySetMember(
            string memberName,
            Object value)
        {
            Operators.SetVariable(Context, null, memberName, ClrObject.WrapDynamic(value));
            return true;
        }

    }

    #endregion

    #region NamespaceScope

    /// <summary>
    /// Dynamic Obeject for representing PHP namespaces
    /// </summary>
    public class NamespaceScope : GlobalScope
    {

        internal NamespaceScope(ScriptContext context)
            : base(context)
        {
        }

        private NamespaceScope(ScriptContext context, string namespaceName)
            : base(context, namespaceName)
        {
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
            return TryInvokeMember(GetFullName(binder.Name), args, out result);
        }

        /// <summary>
        /// Specifies dynamic behavior for get operation for global variable in namespace
        /// </summary>
        public override bool TryGetMember(
            GetMemberBinder binder,
            out Object result
        )
        {
            switch (binder.Name)
            {
                case ClassID:
                    result = Classes;
                    return true;

                case ConstID:
                    result = Consts;
                    return true;
            }

            result = new NamespaceScope(Context, GetFullName(binder.Name));

            return true;
        }

        /// <summary>
        /// Specifies dynamic behavior for set operation for global function in namespace
        /// </summary>
        public override bool TrySetMember(
            SetMemberBinder binder,
            Object value
        )
        {
            TrySetMember(GetFullName(binder.Name),value);
            return true;
        }

    }

    #endregion

    #region ClassScope

    /// <summary>
    /// Dynamic scope for reprensenting static members of class
    /// </summary>
    public class ClassScope : BaseScope
    {
        private DTypeDesc type;

        internal ClassScope(ScriptContext context, DTypeDesc type)
            : base(context)
        {
            this.type = type;
        }


        /// <summary>
        /// Specifies dynamic behavior for invoke operation for static method
        /// </summary>
        public override bool TryInvokeMember(
            InvokeMemberBinder binder,
            Object[] args,
            out Object result
        )
        {
            Context.Stack.AddFrame(wrapArgs(args));
            result = PhpVariable.Unwrap(PhpVariable.Dereference(Operators.InvokeStaticMethod(type, binder.Name, null, null, Context)));
            return true;
        }

        /// <summary>
        /// Specifies dynamic behavior for get operation for static variable
        /// </summary>
        public override bool TryGetMember(
            GetMemberBinder binder,
            out Object result
        )
        {
            if (binder.Name == ConstID)
            {
                result = new ConstsScope(Context,type);
                return true;
            }

            result = PhpVariable.Unwrap(PhpVariable.Dereference(Operators.GetStaticProperty(type, binder.Name, null, Context, false)));
            return true;
        }


        /// <summary>
        /// Specifies dynamic behavior for set operation for static variable
        /// </summary>
        public override bool TrySetMember(
            SetMemberBinder binder,
            Object value
        )
        {
            Operators.SetStaticProperty(type, binder.Name, ClrObject.WrapDynamic(value), null, Context);
            return true;

        }
    }

    #endregion

    #region ClassesScope

    /// <summary>
    /// Dynamic Object for representing PHP classes
    /// </summary>
    public class ClassesScope : BaseScope
    {

        internal ClassesScope(ScriptContext context)
            : base(context)
        {
        }

        internal ClassesScope(ScriptContext context, string namespaceName)
            : base(context, namespaceName)
        {
        }

        private DTypeDesc ResolveType(string name)
        {
            return Context.ResolveType(GetFullName(name), null, null, null, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
        }

        /// <summary>
        /// Creates new instance of specified PHP class 
        /// </summary>
        public override bool TryInvokeMember(
            InvokeMemberBinder binder,
            Object[] args,
            out Object result
        )
        {
            Context.Stack.AddFrame(wrapArgs(args));
            DTypeDesc type = ResolveType(binder.Name);
            result = Operators.New(type, null, Context, null);
            return true;
        }

        /// <summary>
        /// Gets dynamic object representing classes
        /// </summary>
        public override bool TryGetMember(
            GetMemberBinder binder,
            out Object result
        )
        {
            DTypeDesc resType = ResolveType(binder.Name);
            result = new ClassScope(Context, resType);
            return true;
        }

    }

    #endregion

    #region ConstsScope

    /// <summary>
    /// Dynamic Object for representing PHP constants
    /// </summary>
    public class ConstsScope : BaseScope
    {
        private DTypeDesc type;

        internal ConstsScope(ScriptContext context)
            : base(context)
        {
        }

        internal ConstsScope(ScriptContext context, string namespaceName)
            : base(context, namespaceName)
        {
        }

        internal ConstsScope(ScriptContext context, DTypeDesc type)
            : base(context)
        {
            this.type = type;
        }

        /// <summary>
        /// Specifies dynamic behavior for get operation for a constant
        /// </summary>
        public override bool TryGetMember(
            GetMemberBinder binder,
            out Object result
        )
        {
            if (type != null)
            {
                result = Operators.GetClassConstant(type, binder.Name, null, Context);
                return true;
            }

            result = Context.GetConstantValue(GetFullName(binder.Name), false, false);
            return true;
        }

        /// <summary>
        /// Specifies dynamic behavior for set operation for a constant
        /// </summary>
        public override bool TrySetMember(
            SetMemberBinder binder,
            Object value
        )
        {
            if (type != null)
            {
                PhpException.Throw(PhpError.Error, String.Format( PHP.Core.Localizations.Strings.constant_redefined, type.MakeFullName() + Name.ClassMemberSeparator + binder.Name));
                return true;
            }

            Context.DefineConstant(GetFullName(binder.Name), value);
            return true;

        }

    }

    #endregion
}
