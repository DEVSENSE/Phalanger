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
        object __toString ( ScriptContext/*!*/context );
    }

	/*
	  class ReflectionException : SPL.Exception 
	  {
   
	  }
	*/
	/*
	class ReflectionException extends Exception { }
	class ReflectionFunction implements Reflector { }
	class ReflectionParameter implements Reflector { }
	class ReflectionMethod extends ReflectionFunction { }
	class ReflectionClass implements Reflector { }
	class ReflectionObject extends ReflectionClass { }
	class ReflectionProperty implements Reflector { }
	class ReflectionExtension implements Reflector { }
	*/

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
	class Reflection : PhpObject
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
        //[ImplementsMethod]
		public static PhpArray getModifierNames(int modifiers)
		{
			PhpArray result = new PhpArray();
			Modifiers flags = (Modifiers)modifiers;

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
        //[ImplementsMethod]
		public static object export(Reflector/*!*/ reflector, bool doReturn)
		{
			if (reflector == null)
				PhpException.ArgumentNull("reflector");

			// TODO:

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
			return getModifierNames(typed1);
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

			return export(typed1, typed2);
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
}
