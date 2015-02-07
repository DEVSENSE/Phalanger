using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace PHP.Core.Emit
{
    internal class DelegateBuilder
    {
        #region Fields

        public static readonly Type[] DelegateCtorSignature = new Type[] { typeof(object), typeof(IntPtr) };

        private ModuleBuilder/*!*/moduleBuilder;

        #endregion

        #region Constructor

        public DelegateBuilder(ModuleBuilder/*!*/moduleBuilder)
        {
            Debug.Assert(moduleBuilder != null);

            this.moduleBuilder = moduleBuilder;
        }

        #endregion

        #region Cache

        /// <summary>
        /// Cache of created delegate types.
        /// </summary>
        private List<Tuple<Type[], Type>> delegateTypesCache = null;

        /// <summary>
        /// Try to find created delegate from <see cref="delegateTypesCache"/>.
        /// </summary>
        /// <param name="types">Delegate type args.</param>
        /// <returns><see cref="Type"/> matching <paramref name="types"/> or <c>null</c>.</returns>
        private Type GetDelegateTypeFromCache(Type[]/*!*/types)
        {
            Tuple<Type[], Type> find;
            if (delegateTypesCache != null && (find = delegateTypesCache.Find(x => ArrayUtils.Equals(x.Item1, types))) != null)
                return find.Item2;

            return null;
        }

        /// <summary>
        /// Add given delegate type into the <see cref="delegateTypesCache"/>.
        /// </summary>
        /// <param name="types"></param>
        /// <param name="delegateType"></param>
        /// <returns><paramref name="delegateType"/>.</returns>
        private Type/*!*/AddDelegateTypeToCache(Type[]/*!*/types, Type/*!*/delegateType)
        {
            if (delegateTypesCache == null) delegateTypesCache = new List<Tuple<Type[], Type>>();
            delegateTypesCache.Add(new Tuple<Type[], Type>(types, delegateType));

            return delegateType;
        }

        #endregion

        #region GetDelegateType

        /// <summary>
        /// Gets a System.Linq.Expressions.Expression.Type object that represents a generic
        /// System.Func or System.Action delegate type that has specific type arguments.
        /// 
        /// For <paramref name="types"/> longer than 17 items, current module's <see cref="TypeBuilder"/> is used instead of Transient one.
        /// This avoids of "Unable to make a reference to a transient module from a non-transient module." exception.
        /// 
        /// For less or equal than 17 items, <see cref="System.Linq.Expressions.Expression.GetDelegateType"/> is used.
        /// </summary>
        /// <param name="types">The type arguments of the delegate.</param>
        /// <param name="uniqueId">A number used to name new delegate.</param>
        /// <returns>The delegate type.</returns>
        public Type/*!*/GetDelegateType(Type[]/*!*/types, long uniqueId)
        {
            Debug.Assert(types != null);

            if (moduleBuilder.IsTransient() ||  // we are in transient module (so dynamically created types can be referenced)
                (types.Length <= 17 && !types.Any((Type t) => t.IsByRef)))    // OR less or equal 17 items and none of them is by reference
                return System.Linq.Expressions.Expression.GetDelegateType(types);

            // else, Action or Func cannot be used, make the delegate:
            return
                GetDelegateTypeFromCache(types) ??      // try to find in cache first
                AddDelegateTypeToCache(                 // create the delegate type
                    types,
                    CreateDelegateType(moduleBuilder, types, string.Format("Delegate{0}'{1}", types.Length, uniqueId)));
        }

        /// <summary>
        /// Create the delegate type.
        /// </summary>
        /// <param name="moduleBuilder"></param>
        /// <param name="types">Delegate type args.</param>
        /// <param name="delegateTypeName">Unique name of the type.</param>
        /// <returns>Delegate type.</returns>
        private static Type/*!*/CreateDelegateType(ModuleBuilder/*!*/moduleBuilder, Type[]/*!*/types, string delegateTypeName)
        {
            // make the delegate:
            TypeBuilder typeBuilder = moduleBuilder.DefineType(delegateTypeName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoClass, typeof(MulticastDelegate));

            Type returnType = types[types.Length - 1];
            Type[] parameterTypes = ArrayUtils.RemoveLast<Type>(types);

            typeBuilder.DefineConstructor(MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, CallingConventions.Standard, DelegateCtorSignature).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            typeBuilder.DefineMethod("Invoke", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask, returnType, parameterTypes).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            return typeBuilder.CreateType();
        }

        #endregion

        #region GetDelegateCtor

        public static ConstructorInfo/*!*/GetDelegateCtor(Type/*!*/delegateType)
        {
            return delegateType.GetConstructor(DelegateCtorSignature);
        }

        #endregion
    }
}
