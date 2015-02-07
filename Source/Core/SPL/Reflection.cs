/*

 Copyright (c) 2005-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

using PHP.Core;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library.SPL
{
    #region Reflector

    [ImplementsType]
    public interface Reflector
    {
        // HACK HACK HACK!!!
        // The "export" method is public static in PHP, and returns always null.
        // This can be declared in pure IL only, not in C#, however we are achieving this by
        // adding its <see cref="DRoutineDesc"/> during initilization (<see cref="ApplicationContext.AddExportMethod"/>).
        // Note we cannot declare the method here, since it would be needed to override it in every derived class.
        //[ImplementsMethod]
        //static object export(ScriptContext/*!*/context) { return null; }

        [ImplementsMethod]
        object __toString(ScriptContext/*!*/context);
    }

    #endregion

    #region Reflection

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// class Reflection 
    /// { 
    ///   public static string getModifierNames(int modifiers);
    ///   public static mixed export(Reflector r [, bool return]);  
    /// }
    /// </code>
    /// </para>
    /// </remarks>
#if !SILVERLIGHT
    [Serializable]
#endif
    [ImplementsType]
    public class Reflection : PhpObject
    {
        [Flags]
        public enum Modifiers
        {
            Static = 0x01,
            Abstract = 0x02,
            Final = 0x04,
            AbstractClass = 0x20,
            FinalClass = 0x40,
            Public = 0x100,
            Protected = 0x200,
            Private = 0x400,
            VisibilityMask = Public | Protected | Private
        }

        #region PHP Methods

        /// <summary>
        /// Gets an array of modifier names contained in modifiers flags.
        /// </summary>
        [ImplementsMethod]
        public static object getModifierNames(ScriptContext/*!*/context, object/*int*/modifiers)
        {
            PhpArray result = new PhpArray();
            Modifiers flags = (Modifiers)Core.Convert.ObjectToInteger(modifiers);

            if ((flags & (Modifiers.Abstract | Modifiers.AbstractClass)) != 0)
                result.Add("abstract");

            if ((flags & (Modifiers.Abstract | Modifiers.AbstractClass)) != 0)
                result.Add("final");

            switch (flags & Modifiers.VisibilityMask)
            {
                case Modifiers.Public: result.Add("public"); break;
                case Modifiers.Protected: result.Add("protected"); break;
                case Modifiers.Private: result.Add("private"); break;
            }

            if ((flags & Modifiers.Static) != 0)
                result.Add("static");

            return result;
        }

        /// <summary>
        /// Exports a reflection.
        /// </summary>
        [ImplementsMethod]
        public static object export(ScriptContext/*!*/context, object/*Reflector*/reflector, object/*bool*/doReturn)
        {
            if (reflector == null)
                PhpException.ArgumentNull("reflector");

            throw new NotImplementedException();
        }

        #endregion

        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            typeDesc.AddMethod("getModifierNames", PhpMemberAttributes.Public | PhpMemberAttributes.Static, getModifierNames);
            typeDesc.AddMethod("export", PhpMemberAttributes.Public | PhpMemberAttributes.Static, export);
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Reflection(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Reflection(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getModifierNames(object instance, PhpStack stack)
        {
            stack.CalleeName = "getModifierNames";
            object arg1 = stack.PeekValue(1);
            stack.RemoveFrame();

            int typed1 = Core.Convert.ObjectToInteger(arg1);
            return getModifierNames(stack.Context, typed1);
        }

        /// <summary>
        /// 
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object export(object instance, PhpStack stack)
        {
            stack.CalleeName = "export";
            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValueOptional(2);
            stack.RemoveFrame();

            Reflector typed1 = arg1 as Reflector;
            if (typed1 == null) { PhpException.InvalidArgumentType("reflector", "Reflector"); return null; }
            bool typed2 = (ReferenceEquals(arg2, Arg.Default)) ? false : Core.Convert.ObjectToBoolean(arg2);

            return export(stack.Context, typed1, typed2);
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected Reflection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    #endregion

    #region ReflectionException

    [ImplementsType]
#if !SILVERLIGHT
    [Serializable]
#endif

    public class ReflectionException : Exception
    {
        #region Implementation Details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        { throw new NotImplementedException(); }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionException(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionException(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected ReflectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    #endregion

    #region ReflectionProperty

    /// <summary>
    /// The ReflectionProperty class.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [ImplementsType]
    public class ReflectionProperty : PhpObject, Reflector
    {
        internal DTypeDesc dtype;

        internal DPropertyDesc property;
        
        #region Constants

        public const int IS_STATIC = 1;
        public const int IS_PUBLIC = 256;
        public const int IS_PROTECTED = 512;
        public const int IS_PRIVATE = 1024;

        #endregion

        #region Properties

        [PhpVisible]
        public string name
        {
            get
            {
                return (property != null) ? property.MakeFullName() : null;
            }
        }

        [PhpVisible]
        public string @class
        {
            get
            {
                return (dtype != null) ? dtype.MakeFullName() : null;
            }
        }

        #endregion

        #region Nested class: RuntimePhpProperty

        private sealed class RuntimePhpProperty : DPropertyDesc
        {
            #region Construction

		    /// <summary>
		    /// Used by type population.
		    /// </summary>
		    internal RuntimePhpProperty(DTypeDesc/*!*/ declaringType, GetterDelegate getterStub, SetterDelegate setterStub)
			    : base(declaringType, PhpMemberAttributes.Public)
		    {
			    this._getterStub = getterStub;
			    this._setterStub = setterStub;
		    }

		    #endregion

    		#region Emission (runtime getter/setter stubs)

		protected override GetterDelegate GenerateGetterStub()
		{
            throw new NotImplementedException();
		}

		protected override SetterDelegate GenerateSetterStub()
		{
            throw new NotImplementedException();
		}

		#endregion
        }

        #endregion

        #region Nested class: KnownRuntimeProperty

        private sealed class KnownRuntimeProperty : KnownProperty
        {
            public override bool IsIdentityDefinite
            {
                get { return true; }
            }

            public override MemberInfo RealMember
            {
                get { throw null; }
            }

            internal override PhpTypeCode EmitGet(CodeGenerator codeGenerator, Core.Emit.IPlace instance, bool wantRef, ConstructedType constructedType, bool runtimeVisibilityCheck)
            {
                throw null;
            }

            internal override AssignmentCallback EmitSet(CodeGenerator codeGenerator, Core.Emit.IPlace instance, bool isRef, ConstructedType constructedType, bool runtimeVisibilityCheck)
            {
                throw null;
            }

            internal override void EmitUnset(CodeGenerator codeGenerator, Core.Emit.IPlace instance, ConstructedType constructedType, bool runtimeVisibilityCheck)
            {
                throw null;
            }

            public KnownRuntimeProperty(DPropertyDesc desc, string name)
                :base(desc, new VariableName(name))
            {

            }
        }

        #endregion

        // ReflectionProperty::__clone — Clone
        
        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionProperty(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionProperty(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            object @class = stack.PeekValue(1);
            object propertyname = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionProperty)instance).__construct(stack.Context, @class, propertyname);
        }

        /// <summary>
        /// Constructs a ReflectionFunction object.
        /// </summary>
        [ImplementsMethod]
        public virtual object __construct(ScriptContext context, object @class, object propertyname)
        {
            string propertynameStr = PhpVariable.AsString(propertyname);

            if (@class == null || string.IsNullOrEmpty(propertynameStr))
                return false;

            this.dtype = null;
            this.property = null;

            DObject dobj;
            string str;

            if ((dobj = (@class as DObject)) != null)
            {
                this.dtype = dobj.TypeDesc;
            }
            else if ((str = PhpVariable.AsString(@class)) != null)
            {
                this.dtype = context.ResolveType(str, null, null, null, ResolveTypeFlags.UseAutoload);
            }

            if (this.dtype == null)
                return false;

            if (this.dtype.GetProperty(new VariableName(propertynameStr), dtype, out this.property) == GetMemberResult.NotFound)
            {
                object runtimeValue;
                if (dobj != null && dobj.RuntimeFields != null && dobj.RuntimeFields.TryGetValue(propertynameStr, out runtimeValue))
                {
                    // create desc of runtime field:
                    this.property = new RuntimePhpProperty(dtype,
                        (instance) => ((DObject)instance).GetRuntimeField(this.name, null),
                        (instance, value) => ((DObject)instance).SetRuntimeField(this.name, value, null, null, null));
                    this.property.Member = new KnownRuntimeProperty(this.property, propertynameStr);
                }
                else
                {
                    return false;
                }
            }

            return null;
        }

        #endregion

        // ReflectionProperty::export — Export

        [ImplementsMethod]
        public virtual object/*ReflectionClass*/getDeclaringClass(ScriptContext context)
        {
            if (dtype == null)
                return false;

            return new ReflectionClass(context, true)
            {
                typedesc = dtype
            };
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getDeclaringClass(object instance, PhpStack stack)
        {
            stack.RemoveFrame();

            return ((ReflectionProperty)instance).getDeclaringClass(stack.Context);
        }

        // ReflectionProperty::getDocComment — Gets doc comment

        [ImplementsMethod]
        public virtual object/*int*/getModifiers(ScriptContext context)
        {
            if (property == null)
                return false;

            int result = 0;

            if (property.IsStatic) result |= IS_STATIC;
            if (property.IsPublic) result |= IS_PUBLIC;
            if (property.IsProtected) result |= IS_PROTECTED;
            if (property.IsPrivate) result |= IS_PRIVATE;

            return result;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getModifiers(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).getModifiers(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*string*/getName(ScriptContext context)
        {
            return this.name;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionProperty)instance).getName(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*mixed*/getValue(ScriptContext context, object @object)
        {
            var dobj = @object as DObject;

            if (property != null)
            {
                if (!property.IsStatic && dobj == null)
                {
                    PhpException.ArgumentNull("object");
                    return false;
                }

                return PhpVariable.Dereference(property.Get(dobj));
            }

            return false;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getValue(object instance, PhpStack stack)
        {
            var @object = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((ReflectionProperty)instance).getValue(stack.Context, @object);
        }

        [ImplementsMethod]
        public virtual object/*void*/setValue(ScriptContext context, object arg1, object arg2)
        {
            DObject dobj = null;
            object value;

            if (property == null)
                return false;

            if (property.IsStatic)
            {
                if (arg2 != Arg.Default)
                {
                    PhpException.InvalidArgumentCount("ReflectionProperty", "setValue");
                    return false;
                }

                value = arg1;
            }
            else
            {
                if (arg2 == Arg.Default)
                {
                    PhpException.MissingArgument(2, "setValue");
                    return false;
                }

                dobj = arg1 as DObject;
                value = arg2;

                if (dobj == null)
                {
                    PhpException.ArgumentNull("object");
                    return false;
                }
            }

            property.Set(dobj, value);

            return null;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setValue(object instance, PhpStack stack)
        {
            var arg1 = stack.PeekValue(1);
            var arg2 = stack.PeekValueOptional(1);
            stack.RemoveFrame();

            return ((ReflectionProperty)instance).setValue(stack.Context, arg1, arg2);
        }

        /// <summary>
        /// Checks whether the property is the default.
        /// </summary>
        /// <param name="context"><see cref="ScriptContext"/>.</param>
        /// <returns>TRUE if the property was declared at compile-time, or FALSE if it was created at run-time.</returns>
        [ImplementsMethod]
        public virtual object/*bool*/isDefault(ScriptContext context)
        {
            return !(this.property is RuntimePhpProperty);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isDefault(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionProperty)instance).isDefault(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isPrivate(ScriptContext context)
        {
            return property != null && property.IsPrivate;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isPrivate(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionProperty)instance).isPrivate(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isProtected(ScriptContext context)
        {
            return property != null && property.IsProtected;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isProtected(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionProperty)instance).isProtected(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isPublic(ScriptContext context)
        {
            return property != null && property.IsPublic;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isPublic(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionProperty)instance).isPublic(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isStatic(ScriptContext context)
        {
            return property != null && property.IsStatic;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isStatic(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionProperty)instance).isStatic(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/setAccessible(ScriptContext context, object accessible)
        {
            if (this.property == null)
                return false;

            bool baccessible = Core.Convert.ObjectToBoolean(accessible);
            // TODO: remember private property accessibility
            return null;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setAccessible(object instance, PhpStack stack)
        {
            var accessible = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionProperty)instance).setAccessible(stack.Context, accessible);
        }

        [ImplementsMethod]
        public virtual object/*string*/__toString(ScriptContext context)
        {
            if (property == null)
                return false;

            return string.Format("Property [ {0} ${1} ]",
                (property.IsStatic ? "static " : string.Empty) + (property.IsPublic ? "public" : (property.IsProtected ? "protected" : "private")),
                this.name);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __toString(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionProperty)instance).__toString(stack.Context);
        }
    }

    #endregion
}
