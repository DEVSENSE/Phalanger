/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

#if SILVERLIGHT
using PHP.CoreCLR;
using DirectoryEx = PHP.CoreCLR.DirectoryEx;
#else
using System.Collections.Specialized; // case-insensitive hashtable
using System.Runtime.Serialization;
using DirectoryEx = System.IO.Directory;
#endif

namespace PHP.Core
{
    #region TestUtils

    public static class TestUtils
    {
#if DEBUG

        /// <summary>
        /// Runs unit tests (methods marked with <see cref="TestAttribute"/>) included in the specified assembly.
        /// </summary>
        public static void UnitTest(Assembly/*!*/ assembly, TextWriter/*!*/ output)
        {
            ScriptContext.CurrentContext.DisableErrorReporting();

            foreach (MethodInfo method in GetTestMethods(assembly))
            {
                output.Write("Testing {0}.{1} ... ", method.DeclaringType.Name, method.Name);

                Debug.Assert(method.GetParameters().Length == 0 && method.ReturnType == Emit.Types.Void && method.IsStatic);

                try
                {
                    method.Invoke(null, ArrayUtils.EmptyStrings);
                    output.WriteLine("OK.");
                }
                catch (TargetInvocationException)
                {
                    output.WriteLine("Failed.");
                }
            }
            output.WriteLine("Done.");
        }

        private static IEnumerable<MethodInfo> GetTestMethods(Assembly/*!*/ assembly)
        {
            // scans assembly for test methods:
            foreach (Type type in assembly.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
                {
                    object[] attrs = method.GetCustomAttributes(typeof(TestAttribute), false);
                    if (attrs.Length == 1)
                    {
                        if (((TestAttribute)attrs[0]).One)
                        {
                            //result = new ArrayList();
                            //result.Add(method);
                            //return result;
                        }
                        else
                        {
                            yield return method;
                        }
                    }
                }
            }
        }

#endif
    }

    #endregion

    #region DebugHelper

    /// <summary>
    /// Debug helpers.
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// Asserts exactly specified number of the references to be non-null.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertNonNull(int count, params object[] references)
        {
            Debug.Assert(references != null);

            foreach (object reference in references)
                if (reference != null) count--;

            Debug.Assert(count == 0);
        }

        /// <summary>
        /// Asserts that the array is non-null and doesn't contain null references.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertAllNonNull(params object[] array)
        {
            Debug.Assert(array != null);

            for (int i = 0; i < array.Length; i++)
                Debug.Assert(array[i] != null);
        }
    }

    #endregion

    #region Reflection Utils

    /// <summary>
    /// Utilities manipulating metadata via reflection.
    /// </summary>
    public static class ReflectionUtils
    {
        /// <summary>
        /// Sets user entry point if this feature is supported.
        /// </summary>
        internal static void SetUserEntryPoint(ModuleBuilder/*!*/ builder, MethodInfo/*!*/ method)
        {
            try
            {
                if (setUserEntryPointSupported ?? true) //TODO: UserEntryPoint shouldn't be set if there isn't any user method that is called first or it should be generated trivial method with emptystatement and one sequencepoint
                    builder.SetUserEntryPoint(method);
                setUserEntryPointSupported = true;
            }
            catch (NotImplementedException)
            {
                setUserEntryPointSupported = false;
            }
            catch (NotSupportedException)
            {
                setUserEntryPointSupported = false;
            }
        }
        private static bool? setUserEntryPointSupported;

        #region Global Fields

        private const string GlobalFieldsType = "<Global Fields>";

        internal static List<FieldInfo>/*!!*/ GetGlobalFields(Assembly/*!*/ assembly, BindingFlags bindingFlags)
        {
            List<FieldInfo> result = new List<FieldInfo>();

#if SILVERLIGHT
			Module[] modules = assembly.GetModules();
#else
            Module[] modules = assembly.GetModules(false); // false - include resource modules (?)
#endif
            foreach (Module module in modules)
            {
                result.AddRange(module.GetFields(bindingFlags));

                Type global_type = module.GetType(GlobalFieldsType);
                if (global_type != null)
                    result.AddRange(global_type.GetFields(bindingFlags));
            }

            return result;
        }

        internal static FieldBuilder/*!*/ DefineGlobalField(ModuleBuilder/*!*/ moduleBuilder, string/*!*/ name, Type/*!*/ type, FieldAttributes attributes)
        {
            //FieldBuilder result = TryDefineRealGlobalField(moduleBuilder, name, type, attributes);
            //if (result != null)
            //    return result;

            TypeBuilder global_type = (TypeBuilder)moduleBuilder.GetType(GlobalFieldsType);
            if (global_type == null)
            {
                global_type = moduleBuilder.DefineType(GlobalFieldsType, TypeAttributes.Class | TypeAttributes.Public |
                    TypeAttributes.Sealed | TypeAttributes.SpecialName);

                global_type.DefineDefaultConstructor(MethodAttributes.PrivateScope);
            }

            return global_type.DefineField(name, type, attributes);
        }

        //private static FieldBuilder TryDefineRealGlobalField(ModuleBuilder/*!*/ moduleBuilder, string/*!*/ name, Type/*!*/ type, FieldAttributes attributes)
        //{
        //    try
        //    {
        //        if (EnvironmentUtils.IsDotNetFramework)
        //        {
        //            // .NET Framework:

        //            FieldInfo fm_ModuleData = typeof(Module).GetField("m_moduleData", BindingFlags.Instance | BindingFlags.NonPublic);
        //            FieldInfo fm_globalTypeBuilder = fm_ModuleData.FieldType.GetField("m_globalTypeBuilder", BindingFlags.Instance | BindingFlags.NonPublic);

        //            object m_ModuleData = fm_ModuleData.GetValue(moduleBuilder);
        //            TypeBuilder m_globalTypeBuilder = (TypeBuilder)fm_globalTypeBuilder.GetValue(m_ModuleData);

        //            return m_globalTypeBuilder.DefineField(name, type, attributes);
        //        }
        //        else
        //        {
        //            // Mono:

        //            FieldInfo f_global_fields = typeof(ModuleBuilder).GetField("global_fields", BindingFlags.Instance | BindingFlags.NonPublic);
        //            FieldBuilder[] global_fields = (FieldBuilder[])f_global_fields.GetValue(moduleBuilder);

        //            FieldInfo f_global_type = typeof(ModuleBuilder).GetField("global_type", BindingFlags.Instance | BindingFlags.NonPublic);
        //            TypeBuilder global_type = (TypeBuilder)f_global_type.GetValue(moduleBuilder);

        //            FieldBuilder result = global_type.DefineField(name, type, attributes);

        //            if (global_fields != null)
        //            {
        //                FieldBuilder[] new_global_fields = new FieldBuilder[global_fields.Length + 1];
        //                System.Array.Copy(global_fields, new_global_fields, global_fields.Length);
        //                new_global_fields[global_fields.Length] = result;

        //                f_global_fields.SetValue(moduleBuilder, new_global_fields);
        //            }
        //            else
        //            {
        //                global_fields = new FieldBuilder[1];
        //                global_fields[0] = result;

        //                f_global_fields.SetValue(moduleBuilder, global_fields);
        //            }

        //            return result;
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}

        internal static void CreateGlobalType(ModuleBuilder/*!*/ moduleBuilder)
        {
            moduleBuilder.CreateGlobalFunctions();

            TypeBuilder global_type = (TypeBuilder)moduleBuilder.GetType(GlobalFieldsType);
            if (global_type != null)
                global_type.CreateType();
        }

        #endregion

        #region Utils

        internal static ParameterBuilder/*!*/ DefineParameter(MethodInfo/*!*/ method, int index, ParameterAttributes attributes,
            string/*!*/ name)
        {
            Debug.Assert(method is MethodBuilder || method is DynamicMethod);

            MethodBuilder builder = method as MethodBuilder;
            if (builder != null)
                return builder.DefineParameter(index, attributes, name);
            else
                return ((DynamicMethod)method).DefineParameter(index, attributes, name);
        }

        #endregion

        internal static void SetCustomAttribute(MethodInfo/*!*/ method, CustomAttributeBuilder/*!*/ customAttributeBuilder)
        {
            MethodBuilder builder = method as MethodBuilder;
            if (builder != null)
                builder.SetCustomAttribute(customAttributeBuilder);
        }

        internal static Type[]/*!!*/ GetParameterTypes(ParameterInfo[]/*!!*/ parameters)
        {
            Type[] types = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                types[i] = parameters[i].ParameterType;
            return types;
        }

        public static object GetDefault(Type type)
        {
            return (type.IsValueType) ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// Parses the <paramref name="realType"/> into <paramref name="transientId"/>, <paramref name="sourceFile"/> and <paramref name="typeName"/>.
        /// </summary>
        /// <param name="realType">Type from within <see cref="PHP.Core.Reflection.TransientModule"/>, <see cref="PHP.Core.Reflection.ScriptModule"/> or <see cref="PHP.Core.Reflection.PureModule"/>.</param>
        /// <param name="transientId"><c>-1</c> or Id of transiend module.</param>
        /// <param name="sourceFile"><c>null</c> or relative file name of the contained type.</param>
        /// <param name="typeName">Cannot be null. PHP type name without the prefixed <c>&lt;</c>~<c>&gt;</c> information. CLR notation of namespaces.</param>
        /// <remarks>Handles special cases of types from ClassLibrary and Core.</remarks>
        public static void ParseTypeId(Type/*!*/realType, out int transientId, out string sourceFile, out string typeName)
        {
            Debug.Assert(realType != null);

            // parse the type name (ScriptModule, PureModule, TransientModule):
            ParseTypeId(realType.FullName, out transientId, out sourceFile, out typeName);

            //
            // handle special cases:
            //

            // [ImplementsTypeAttribute] with PHPTypeName specified => take the PHPTypeName only
            var attr = ImplementsTypeAttribute.Reflect(realType);
            if (attr != null && attr.PHPTypeName != null)
            {
                typeName = attr.PHPTypeName;
                return;
            }
            
            // PHP.Library. => cut of the namespace, keep realType.Name only
            // J: HACK because of PHP types in ClassLibrary and Core
            if (realType.Namespace.StartsWith(Namespaces.Library, StringComparison.Ordinal))
            {
                typeName = realType.Name;
                return;
            }            
        }

        /// <summary>
        /// Parses the <paramref name="realTypeFullName"/> into <paramref name="transientId"/>, <paramref name="sourceFile"/> and <paramref name="typeName"/>.
        /// </summary>
        /// <param name="realTypeFullName">Expecting <see cref="Type.FullName"/> (type CLR full name, including <c>.</c>, <c>+</c>) of a type from within <see cref="PHP.Core.Reflection.TransientModule"/>, <see cref="PHP.Core.Reflection.ScriptModule"/> or <see cref="PHP.Core.Reflection.PureModule"/>.</param>
        /// <param name="transientId"><c>-1</c> or Id of transiend module.</param>
        /// <param name="sourceFile"><c>null</c> or relative file name of the contained type.</param>
        /// <param name="typeName">PHP type name without the prefixed <c>&lt;</c>~<c>&gt;</c> information. CLR notation of namespaces. Can be <c>null</c> reference if there is no type name (global function in transient module).</param>
        internal static void ParseTypeId(string/*!*/realTypeFullName, out int transientId, out string sourceFile, out string typeName)
        {
            Debug.Assert(!string.IsNullOrEmpty(realTypeFullName));

            //
            transientId = PHP.Core.Reflection.TransientAssembly.InvalidEvalId;

            // <srcFile{[^?]id}>.typeName
            if (realTypeFullName[0] == '<')
            {
                // naming policy of TransientModule, ScriptModule
                int closing;
                if ((closing = realTypeFullName.IndexOf('>', 1)) >= 0)
                {
                    // find ^ or ?
                    int idDelim;
                    if ((idDelim = realTypeFullName.IndexOfAny(PHP.Core.Reflection.TransientModule.IdDelimiters, 1, closing)) >= 0)
                        transientId = int.Parse(realTypeFullName.Substring(idDelim + 1, closing - idDelim - 1));
                    else
                        idDelim = closing;
                    
                    // parse sourceFile out:
                    sourceFile = realTypeFullName.Substring(1, idDelim - 1);
                }
                else
                {
                    Debug.Fail("Unexpected Type.FullName! Missing closing '>' in '" + realTypeFullName + "'.");
                    sourceFile = null;
                    closing = 1;
                }

                if (realTypeFullName.Length > closing + 1)
                {
                    // parse typeName out:
                    Debug.Assert(
                        realTypeFullName.Length > closing + 2 && realTypeFullName[closing + 1] == '.',
                        "Unexpected Type.FullName! Missing '.' after '>' in '" + realTypeFullName + "'.");

                    // get the type name (without version id and generic params):
                    typeName = ClrNotationUtils.SubstringWithoutBackquoteAndHash(
                        realTypeFullName,
                        closing + 2,
                        realTypeFullName.Length - closing - 2);
                }
                else
                {
                    typeName = null;
                }
            }
            else
            {
                // naming policy of PureModule:
                sourceFile = null;  // we are not able to determine the file name here
                typeName = realTypeFullName;
            }
        }
    }

    #endregion

    #region Date and Time

    ///// <summary>
    ///// Fixes the strange behavior of <see cref="System.TimeZone"/> which translates between local and
    ///// UTC using current time zone, completely disregarding the current zone's UTC offset.
    ///// </summary>
    //public abstract class CustomTimeZoneBase : TimeZone
    //{
    //    public override DateTime ToLocalTime(DateTime time)
    //    {
    //        TimeSpan offset = GetUtcOffset(time);
    //        DateTime local = new DateTime((time + offset).Ticks, DateTimeKind.Local);

    //        // was the offset correct?
    //        offset = GetUtcOffset(local);
    //        if (local - offset != time)
    //        {
    //            return new DateTime((time + offset).Ticks, DateTimeKind.Local);
    //        }
    //        else return local;
    //    }

    //    public override DateTime ToUniversalTime(DateTime time)
    //    {
    //        return new DateTime((time - GetUtcOffset(time)).Ticks, DateTimeKind.Utc);
    //    }
    //}

    /// <summary>
    /// Unix TimeStamp to DateTime conversion and vice versa
    /// </summary>
    public static class DateTimeUtils
    {
        #region Nested Class: UtcTimeZone, GmtTimeZone

        //private sealed class _UtcTimeZone : CustomTimeZoneBase
        //{
        //    public override string DaylightName { get { return "UTC"; } }
        //    public override string StandardName { get { return "UTC"; } }

        //    public override TimeSpan GetUtcOffset(DateTime time)
        //    {
        //        return new TimeSpan(0);
        //    }

        //    public override DaylightTime GetDaylightChanges(int year)
        //    {
        //        return new DaylightTime(new DateTime(0), new DateTime(0), new TimeSpan(0));
        //    }


        //}

        //private sealed class _GmtTimeZone : CustomTimeZoneBase
        //{
        //    public override string DaylightName { get { return "GMT Daylight Time"; } }
        //    public override string StandardName { get { return "GMT Standard Time"; } }

        //    public override TimeSpan GetUtcOffset(DateTime time)
        //    {
        //        return IsDaylightSavingTime(time) ? new TimeSpan(0, +1, 0, 0, 0) : new TimeSpan(0);
        //    }
        //    public override DaylightTime GetDaylightChanges(int year)
        //    {
        //        return new DaylightTime
        //        (
        //          new DateTime(year, 3, 27, 1, 0, 0),
        //          new DateTime(year, 10, 30, 2, 0, 0),
        //          new TimeSpan(0, +1, 0, 0, 0)
        //        );
        //    }
        //}

        #endregion

        /// <summary>
        /// Time 0 in terms of Unix TimeStamp.
        /// </summary>
        public static readonly DateTime/*!*/UtcStartOfUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// UTC time zone.
        /// </summary>
        public static TimeZoneInfo/*!*/UtcTimeZone { get { return TimeZoneInfo.Utc; } }

        /// <summary>
        /// Converts <see cref="DateTime"/> representing UTC time to UNIX timestamp.
        /// </summary>
        /// <param name="dt">Time.</param>
        /// <returns>Unix timestamp.</returns>
        public static int UtcToUnixTimeStamp(DateTime dt)
        {
            double seconds = (dt - UtcStartOfUnixEpoch).TotalSeconds;

            if (seconds < Int32.MinValue)
                return Int32.MinValue;
            if (seconds > Int32.MaxValue)
                return Int32.MaxValue;

            return (int)seconds;
        }

        /// <summary>
        /// Converts UNIX timestamp (number of seconds from 1.1.1970) to <see cref="DateTime"/>.
        /// </summary>
        /// <param name="timestamp">UNIX timestamp</param>
        /// <returns><see cref="DateTime"/> structure representing UTC time.</returns>
        public static DateTime UnixTimeStampToUtc(int timestamp)
        {
            return UtcStartOfUnixEpoch + TimeSpan.FromSeconds(timestamp);
        }

#if !SILVERLIGHT
        /// <summary>
        /// Gets the daylight saving time difference between two dates.
        /// </summary>
        /// <param name="src">Source date.</param>
        /// <param name="dst">Destination date.</param>
        /// <returns>
        /// The time span that has to be added to the source date's Daylight Saving Time Delta to get 
        /// destination date's one.
        /// </returns>
        public static TimeSpan GetDaylightTimeDifference(DateTime src, DateTime dst)
        {
            TimeZone zone = TimeZone.CurrentTimeZone;

            if (src.Kind != DateTimeKind.Local) src = zone.ToLocalTime(src);
            if (dst.Kind != DateTimeKind.Local) dst = zone.ToLocalTime(dst);

            DaylightTime src_dt = zone.GetDaylightChanges(src.Year);
            DaylightTime dst_dt = zone.GetDaylightChanges(dst.Year);

            // difference between DST of src and dst:
            return
              (TimeZone.IsDaylightSavingTime(dst, dst_dt) ? dst_dt.Delta : TimeSpan.Zero) -
              (TimeZone.IsDaylightSavingTime(src, src_dt) ? src_dt.Delta : TimeSpan.Zero);
        }
#endif

        /// <summary>
        /// Determine maximum of three given <see cref="DateTime"/> values.
        /// </summary>
        public static DateTime Max(DateTime d1, DateTime d2, DateTime d3)
        {
            return (d1 < d2) ? ((d2 < d3) ? d3 : d2) : ((d1 < d3) ? d3 : d1);
        }
#if DEBUG

        static FieldInfo TimeZone_CurrentTimeZone = null;

        /// <summary>
        /// Sets system current time zone (for debugging purposes only).
        /// </summary>
        public static void SetCurrentTimeZone(TimeZone/*!*/ zone)
        {
            Debug.Assert(zone != null);

            if (TimeZone_CurrentTimeZone == null)
                TimeZone_CurrentTimeZone = typeof(TimeZone).GetField("currentTimeZone", BindingFlags.NonPublic | BindingFlags.Static);
            
            Debug.Assert(TimeZone_CurrentTimeZone != null, "Missing private field of TimeZone class.");
            TimeZone_CurrentTimeZone.SetValue(null, zone);
        }

#endif


        //		private static TimeZone GetTimeZoneFromRegistry(TimeZone/*!*/ zone)
        //		{
        //		  try
        //		  {
        //		    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
        //		      @"Software\Microsoft\Windows NT\CurrentVersion\Time Zones\" + zone.StandardName,false))
        //		    {
        //  		    if (key == null) return null;
        //		      
        //		      byte[] tzi = key.GetValue("TZI") as byte[];
        //		      if (tzi == null) continue;
        //    		    
        //    		  int bias = BitConverter.ToInt32(tzi,0);
        //    		  
        //  		  }  
        //		  }
        //		  catch (Exception)
        //		  {
        //		  }
        //
        //		  return null;
        //		}		
    }

    #endregion

    #region ClrNotationUtils

    internal static class ClrNotationUtils
    {
        internal const char VersionIndexDelimiter = '#';
        internal const char GenericParamsDelimiter = '`';

        /// <summary>
		/// Makes full CLR name from this instance. 
		/// </summary>
        /// <param name="qualifiedName">Qualified name to be converted to CLR notation.</param>
		/// <param name="genericParamCount">Number of generic parameters.</param>
		/// <param name="versionIndex">Index of the conditional version or 0 for unconditional.</param>
		/// <returns>Full CLR name.</returns>
        public static string ToClrNotation(this QualifiedName qualifiedName, int genericParamCount, int versionIndex)
		{
			Debug.Assert(versionIndex >= 0, "Version index should be known.");

			StringBuilder result = new StringBuilder();

            for (int i = 0; i < qualifiedName.Namespaces.Length; i++)
			{
                result.Append(qualifiedName.Namespaces[i]);
				result.Append('.');
			}

            if (qualifiedName.Name.Value != "")
                result.Append(qualifiedName.Name);

			if (versionIndex > 0)
			{
				result.Append(VersionIndexDelimiter);
				result.Append(versionIndex);
			}

			if (genericParamCount > 0)
			{
				result.Append(GenericParamsDelimiter);
				result.Append(genericParamCount);
			}

			return result.ToString();
		}

        /// <summary>
        /// Handles PHP type and parses its name.
        /// </summary>
        public static QualifiedName FromClrNotation(Type/*!*/type)
        {
            Debug.Assert(type != null);

            if (type.Assembly == typeof(ApplicationContext).Assembly)
                return new QualifiedName(new Name(type.Name));  // ignore namespace in Core
            else
            {
                // handle PHP type with type name specified in the attribute:
                var attr = ImplementsTypeAttribute.Reflect(type);
                if (attr != null && attr.PHPTypeName != null)
                    return new QualifiedName(new Name(attr.PHPTypeName));

                // default behaviour:
                return FromClrNotation(type.FullName, true);
            }
        }

		/// <summary>
		/// Parses CLR full name. 
		/// </summary>
		public static QualifiedName FromClrNotation(string/*!*/ fullName, bool hasBaseName)
		{
            if (fullName[0] == '<')
            {
                // "<*>.PhpTypeName"
                int lastGt = fullName.IndexOf('>');
                if (lastGt > 0)
                {
                    Debug.Assert(fullName[lastGt + 1] == '.');
                    fullName = fullName.Substring(lastGt + 2);
                }
            }
            
			int component_count = 1;
			for (int i = 0; i < fullName.Length; i++)
			{
				if (fullName[i] == '.' || fullName[i] == '+')
					component_count++;
			}

			Name[] namespaces = new Name[hasBaseName ? component_count - 1 : component_count];

			int j = 0;
			int last_separator = -1;
			for (int i = 0; i < fullName.Length; i++)
			{
				if (fullName[i] == '.' || fullName[i] == '+')
				{
					namespaces[j++] = new Name(SubstringWithoutBackquoteAndHash(fullName, last_separator + 1, i - last_separator - 1));
					last_separator = i;
				}
			}

			Name last_component = new Name(SubstringWithoutBackquoteAndHash(fullName, last_separator + 1, fullName.Length - last_separator - 1));

			if (hasBaseName)
			{
				return new QualifiedName(last_component, namespaces);
			}
			else
			{
				namespaces[j] = last_component;
				return new QualifiedName(Name.EmptyBaseName, namespaces);
			}
		}

		private static char[] BackquoteAndHash = new char[] { GenericParamsDelimiter, VersionIndexDelimiter };

		internal static string/*!*/ SubstringWithoutBackquoteAndHash(string/*!*/ fullName, int start, int length)
		{
			int backquote = fullName.IndexOfAny(BackquoteAndHash, start, length);
			if (backquote != -1)
				length = backquote - start;

			return fullName.Substring(start, length);
		}
}
        #endregion

    #region Threading
    //TODO: do something with this #if
#if !SILVERLIGHT

    /// <summary>
    /// Implements cache mechanism to be used in multi-threaded environment.
    /// </summary>
    /// <typeparam name="K">The cache key type.</typeparam>
    /// <typeparam name="T">The cache value type.</typeparam>
    public class SynchronizedCache<K, T>
    {
        /// <summary>
        /// The lock used to access the cache synchronously. Cannot be null.
        /// </summary>
        private readonly ReaderWriterLockSlim/*!*/cacheLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Cached values. Cannot be null.
        /// </summary>
        private readonly Dictionary<K, T>/*!*/innerCache = new Dictionary<K, T>();

        /// <summary>
        /// Amount of items in the cache dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                return innerCache.Count;
            }
        }

        /// <summary>
        /// The update function used when cache miss. Cannot be null.
        /// </summary>
        private readonly Func<K, T>/*!*/updateFunction;

        /// <summary>
        /// Initialize the new instance of SynchronizedCache object.
        /// </summary>
        /// <param name="updateFunction">The update function used when cache miss.
        /// Note the function is called within the write lock exclusively.</param>
        public SynchronizedCache(Func<K, T>/*!*/updateFunction)
        {
            if (updateFunction == null)
                throw new ArgumentNullException("updateFunction");

            //
            this.updateFunction = updateFunction;
        }

        /// <summary>
        /// Try to get an item from the cache. If the given <paramref name="key"/> is not found,
        /// the <see cref="updateFunction"/> is used to create new item.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// the cache does not contain given <paramref name="key"/> yet.
        /// <returns>The item according to the given <paramref name="key"/>.</returns>
        [DebuggerNonUserCode]
        public T Get(K key)
        {
            T result;

            // TODO (J): 2-gen cache, persistent readonly cache without locks

            // try to find the value in the cache first
            cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (innerCache.TryGetValue(key, out result))
                    return result;

                // upgrade to write lock and add new value into the cache
                cacheLock.EnterWriteLock();
                try
                {
                    // double check the lock, the item could be added while the thread was waiting for the writer lock
                    if (innerCache.TryGetValue(key, out result))
                        return result;

                    // only here, the Get method can be called recursively

                    // add the value into the cache
                    // create new value synchronously here
                    innerCache.Add(key, (result = updateFunction(key)));

                    //
                    return result;
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Update the value with the given <paramref name="key"/> using specified <paramref name="updateFunction"/>.
        /// </summary>
        /// <param name="key">Key of the value to be updated or added.</param>
        /// <param name="updateFunction">The update function used to get the value of the item. The parameter cannot be null.</param>
        /// <returns>The value of the item with given <paramref name="key"/>.</returns>
        [DebuggerNonUserCode]
        public T Update(K key, Func<K, T>/*!*/updateFunction)
        {
            Debug.Assert(updateFunction != null);

            cacheLock.EnterWriteLock();
            try
            {
                // update the value in the cache
                // create new value synchronously here
                return innerCache[key] = updateFunction(key);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Update the value with the given <paramref name="key"/> using default <see cref="updateFunction"/>.
        /// </summary>
        /// <param name="key">Key of the value to be updated or added.</param>
        /// <returns>The value of the item with given <paramref name="key"/>.</returns>
        [DebuggerNonUserCode]
        public T Update(K key)
        {
            return Update(key, updateFunction);
        }
    }

#else

    /// <summary>
    /// Implements cache mechanism to be used in multi-threaded environment.
    /// </summary>
    /// <typeparam name="K">The cache key type.</typeparam>
    /// <typeparam name="T">The cache value type.</typeparam>
    public class SynchronizedCache<K, T>
    {
        /// <summary>
        /// The lock used to access the cache synchronously. Cannot be null.
        /// </summary>
        private readonly object/*!*/cacheLock = new object();

        /// <summary>
        /// Cached values. Cannot be null.
        /// </summary>
        private readonly Dictionary<K, T>/*!*/innerCache = new Dictionary<K, T>();

        /// <summary>
        /// The update function used when cache miss. Cannot be null.
        /// </summary>
        private readonly Func<K, T>/*!*/updateFunction;

        /// <summary>
        /// Initialize the new instance of SynchronizedCache object.
        /// </summary>
        /// <param name="updateFunction">The update function used when cache miss.
        /// Note the function is called within the lock.</param>
        public SynchronizedCache(Func<K, T>/*!*/updateFunction)
        {
            if (updateFunction == null)
                throw new ArgumentNullException("updateFunction");

            //
            this.updateFunction = updateFunction;
        }

        /// <summary>
        /// Try to get an item from the cache. If the given <paramref name="key"/> is not found,
        /// the <see cref="updateFunction"/> is used to create new item.
        /// </summary>
        /// <param name="key">The key of the item.</param>
        /// the cache does not contain given <paramref name="key"/> yet.
        /// <returns>The item according to the given <paramref name="key"/>.</returns>
        public T Get(K key)
        {
            T result;

            // try to find the value in the cache first
            lock(cacheLock)
            {
                if (innerCache.TryGetValue(key, out result))
                    return result;

                // add the value into the cache
                // create new value synchronously here
                innerCache.Add(key, (result = updateFunction(key)));
                return result;
            }
        }

        /// <summary>
        /// Update the value with the given <paramref name="key"/> using specified <paramref name="updateFunction"/>.
        /// </summary>
        /// <param name="key">Key of the value to be updated or added.</param>
        /// <param name="updateFunction">The update function used to get the value of the item. The parameter cannot be null.</param>
        /// <returns>The value of the item with given <paramref name="key"/>.</returns>
        public T Update(K key, Func<K, T>/*!*/updateFunction)
        {
            Debug.Assert(updateFunction != null);

            lock(cacheLock)
            {
                // update the value in the cache
                // create new value synchronously here
                return innerCache[key] = updateFunction(key);
            }
        }

        /// <summary>
        /// Update the value with the given <paramref name="key"/> using default <see cref="updateFunction"/>.
        /// </summary>
        /// <param name="key">Key of the value to be updated or added.</param>
        /// <returns>The value of the item with given <paramref name="key"/>.</returns>
        public T Update(K key)
        {
            return Update(key, updateFunction);
        }
    }


#endif

    #endregion

    #region Delegates

    public static class DelegateExtensions
    {
        /// <summary>
        /// Combine with another predicate function. Both functions must return true.
        /// </summary>
        /// <typeparam name="T">The type of predicate functions argument.</typeparam>
        /// <param name="predicate1">This predicate. Can be null.</param>
        /// <param name="predicate2">Another predicate. Can be null.</param>
        /// <returns>Combination of two given predicates or null if both arguments are null.</returns>
        public static Predicate<T> AndAlso<T>(this Predicate<T> predicate1, Predicate<T> predicate2)
        {
            if (predicate1 == null && predicate2 == null)
                return null;

            if (predicate1 == null)
                return predicate2;

            if (predicate2 == null)
                return predicate1;

            // else combine
            return (arg) => predicate1(arg) && predicate2(arg);
        }

        /// <summary>
        /// Combine with another predicate function. Predicates will be processed sequentially until one pass.
        /// </summary>
        /// <typeparam name="T">The type of predicate functions argument.</typeparam>
        /// <param name="predicate1">This predicate. Can be null.</param>
        /// <param name="predicate2">Another predicate. Can be null.</param>
        /// <returns>Combination of two given predicates or null if both arguments are null.</returns>
        public static Predicate<T> OrElse<T>(this Predicate<T> predicate1, Predicate<T> predicate2)
        {
            if (predicate1 == null && predicate2 == null)
                return null;

            if (predicate1 == null)
                return predicate2;

            if (predicate2 == null)
                return predicate1;

            // else combine
            return (arg) => predicate1(arg) || predicate2(arg);
        }
    }

    #endregion

    #region Numbers

    internal static class NumberUtils
    {
        /// <summary>
        /// Determines whether given <see cref="long"/> can be safely converted to <see cref="int"/>.
        /// </summary>
        public static bool IsInt32(long l)
        {
            int i = unchecked((int)l);
            return (i == l);
        }
    }

    #endregion

    #region Collections

    /// <summary>
    /// Type of an item in dictionary collection.
    /// </summary>
    public enum DictionaryItemType { Keys, Values, Entries };

    /// <summary>
    /// Auxiliary class which represents invalid key or value.
    /// </summary>
    [Serializable]
    public class InvalidItem : ISerializable
    {
        /// <summary>Prevents instantiation.</summary>
        private InvalidItem() { }

        /// <summary>Invalid item singleton.</summary>
        internal static readonly InvalidItem Default = new InvalidItem();

        #region Serialization
#if !SILVERLIGHT

        /// <include file='Doc/Common.xml' path='/docs/method[@name="GetObjectData"]/*'/>
        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(InvalidItemDeserializer));
        }

        [Serializable]
        private class InvalidItemDeserializer : IObjectReference
        {
            public Object GetRealObject(StreamingContext context)
            {
                return InvalidItem.Default;
            }
        }

#endif
        #endregion
    }

    #region WeakCache

    /// <summary>
    /// Maps real objects to their associates (of type <typeparamref name="T"/>).
    /// </summary>
    /// <typeparam name="T">The type of objects associated with real objects.</typeparam>
    /// <remarks>
    /// The cache should store only the real objects that are alive, i.e. reachable from GC roots.
    /// It is assumed that there exists a (strong) reference from instances of <typeparamref name="T"/>
    /// to their associated real objects. Therefore holding the associates in this cache using strong
    /// references only would not work and a more sophisticated pattern is employed.
    /// </remarks>
    internal class WeakCache<T> : Dictionary<object, object>
    {
        #region WeakCacheKey
#if !SILVERLIGHT
        /// <summary>
        /// Weak reference with overriden <see cref="GetHashCode"/> and <see cref="Equals"/>.
        /// </summary>
        /// <remarks>
        /// Delegating <see cref="GetHashCode"/> to the target and confirming equality with the target
        /// makes it possible to use the target (real object) as key when performing dictionary lookups.
        /// There's not need to create a new <see cref="WeakCacheKey"/> just to call something like
        /// <see cref="TryGetValue"/>.
        /// </remarks>
        private class WeakCacheKey : WeakReference
        {
            private int hashCode;

            internal WeakCacheKey(object obj)
                : base(obj, false)
            {
                this.hashCode = obj.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (obj != this && (!IsAlive || !Object.ReferenceEquals(obj, Target))) return false;

                return true;
            }

            public override int GetHashCode()
            {
                return hashCode;
            }
        }

#else

        private class WeakCacheKey
        {
            private WeakReference _ref;

            private int hashCode;

            internal WeakCacheKey(object obj)
            {
                _ref = new WeakReference(obj,false);

                this.hashCode = obj.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (obj != this && (!_ref.IsAlive || !Object.ReferenceEquals(obj, _ref.Target))) return false;

                return true;
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public bool IsAlive
            {
                get
                { return _ref.IsAlive; }
            }


            public object Target
            {
                get
                { return _ref.Target; }
            }

        }

#endif

        #endregion

        private int allocCheckCounter;
        private int lastSweepCount;
        private long lastSweepMemory;

        /// <summary>
        /// Adds a new real object - associate mapping.
        /// </summary>
        public void Add(object key, T value)
        {
            CheckAllocation();

            base.Add(new WeakCacheKey(key), new WeakReference(value, true));
        }

        /// <summary>
        /// Retrieves the associate for a given real object.
        /// </summary>
        public bool TryGetValue(object key, out T value)
        {
            object obj;

            bool success = base.TryGetValue(key, out obj);
            if (!success)
            {
                value = default(T);
                return false;
            }

            WeakReference wr = obj as WeakReference;
            if (wr != null)
            {
                value = (T)wr.Target;
            }
            else
            {
                // turn the strong ref to weak ref now
                this[key] = new WeakReference(obj, true);

                value = (T)obj;
            }

            return true;
        }

        /// <summary>
        /// Check whether it is reasonable to perform a weak reference sweep and delegates to <see cref="WeakReferenceSweep"/>.
        /// </summary>
        /// <remarks>
        /// Inspired by <c>System.ComponentModel.WeakHashTable.ScavengeKeys</c> BCL internal class.
        /// </remarks>
        private void CheckAllocation()
        {
            int count = Count;

            if (count != 0)
            {
                if (lastSweepCount == 0) lastSweepCount = count;
                else
                {
                    long mem = GC.GetTotalMemory(false);
                    if (lastSweepMemory == 0) lastSweepMemory = mem;
                    else
                    {
                        float mem_delta = ((float)(mem - lastSweepMemory)) / ((float)lastSweepMemory);
                        float count_delta = ((float)(count - lastSweepCount)) / ((float)lastSweepCount);

                        if ((mem_delta < 0 && count_delta >= 0) || ++allocCheckCounter > 4096)
                        {
                            WeakReferenceSweep();

                            lastSweepMemory = mem;
                            lastSweepCount = count;
                            allocCheckCounter = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes items representing real objects that are already dead.
        /// </summary>
        private void WeakReferenceSweep()
        {
            List<WeakCache<T>.WeakCacheKey> dead_refs = new List<WeakCache<T>.WeakCacheKey>();
            List<KeyValuePair<object, T>> strong_keys = new List<KeyValuePair<object, T>>();

            foreach (KeyValuePair<object, object> pair in this)
            {
                WeakCache<T>.WeakCacheKey key = (WeakCache<T>.WeakCacheKey)pair.Key;

                if (!key.IsAlive) dead_refs.Add(key);
                else
                {
                    if (!(pair.Value is WeakReference))
                    {
                        strong_keys.Add(new KeyValuePair<object, T>(key.Target, (T)pair.Value));
                    }
                }
            }

            // remove dead keys
            foreach (WeakCache<T>.WeakCacheKey key in dead_refs)
            {
                Remove(key);
            }

            // weaken strong references to living associates
            foreach (KeyValuePair<object, T> pair in strong_keys)
            {
                if (pair.Key != null) this[pair.Key] = new WeakReference(pair.Value, true);
            }
        }

        /// <summary>
        /// Ensures that the associate of the given real object is held strongly.
        /// </summary>
        /// <remarks>
        /// Should be called from within the associate's finalizer when the real object is
        /// figured out to be still alive.
        /// </remarks>
        public void Resurrect(object key, T value)
        {
            Debug.Assert(ContainsKey(key));

            // turn the weak ref into strong ref
            this[key] = value;
        }
    }

    #endregion

    #region GenericEnumeratorAdapter, GenericDictionaryAdapter

    /// <summary>
    /// Makes it possible to use C# iterators to implement the <see cref="IDictionaryEnumerator"/>
    /// interface.
    /// </summary>
    /// <remarks>
    /// Optionally performs CLR to PHP wrapping on returned values.
    /// </remarks>
    [Serializable]
    public class GenericEnumerableAdapter<TValue> : IDictionaryEnumerator
    {
        #region Fields

        private IEnumerator<TValue>/*!*/ baseEnumerator;
        private bool wrapValues;

        #endregion

        #region Construction

        public GenericEnumerableAdapter(IEnumerator<TValue>/*!*/ baseEnumerator, bool wrapValues)
        {
            Debug.Assert(baseEnumerator != null);

            this.baseEnumerator = baseEnumerator;
            this.wrapValues = wrapValues;
        }

        #endregion

        #region IDictionaryEnumerator Members

        public DictionaryEntry Entry
        {
            get { return new DictionaryEntry(Key, Value); }
        }

        public object Key
        {
            get
            { return null; }
        }

        public object Value
        {
            get
            {
                TValue value = baseEnumerator.Current;
                if (wrapValues) return PHP.Core.Reflection.ClrObject.WrapDynamic(value);
                return value;
            }
        }

        #endregion

        #region IEnumerator Members

        public object Current
        {
            get { return new DictionaryEntry(Key, Value); }
        }

        public bool MoveNext()
        {
            return baseEnumerator.MoveNext();
        }

        public void Reset()
        {
            baseEnumerator.Reset();
        }

        #endregion
    }

    /// <summary>
    /// Makes it possible to use C# 2.0 iterators to implement the <see cref="IDictionaryEnumerator"/>
    /// interface.
    /// </summary>
    /// <remarks>
    /// Optionally performs CLR to PHP wrapping on returned keys and values.
    /// </remarks>
    [Serializable]
    public class GenericDictionaryAdapter<TKey, TValue> : IDictionaryEnumerator
    {
        #region Fields

        private IEnumerator<KeyValuePair<TKey, TValue>>/*!*/ baseEnumerator;
        private bool wrapKeysAndValues;

        #endregion

        #region Construction

        public GenericDictionaryAdapter(IEnumerator<KeyValuePair<TKey, TValue>>/*!*/ baseEnumerator, bool wrapKeysAndValues)
        {
            Debug.Assert(baseEnumerator != null);

            this.baseEnumerator = baseEnumerator;
            this.wrapKeysAndValues = wrapKeysAndValues;
        }

        #endregion

        #region IDictionaryEnumerator Members

        public DictionaryEntry Entry
        {
            get { return new DictionaryEntry(Key, Value); }
        }

        public object Key
        {
            get
            {
                TKey key = baseEnumerator.Current.Key;
                if (wrapKeysAndValues) return PHP.Core.Reflection.ClrObject.WrapDynamic(key);
                return key;
            }
        }

        public object Value
        {
            get
            {
                TValue value = baseEnumerator.Current.Value;
                if (wrapKeysAndValues) return PHP.Core.Reflection.ClrObject.WrapDynamic(value);
                return value;
            }
        }

        #endregion

        #region IEnumerator Members

        public object Current
        {
            get { return this.Entry; }
        }

        public bool MoveNext()
        {
            return baseEnumerator.MoveNext();
        }

        public void Reset()
        {
            baseEnumerator.Reset();
        }

        #endregion
    }

    #endregion

    #region HashQueue

    internal sealed class HashQueue       // GENERICS: <K,V>
    {
        public delegate object KeyProvider(object value); // GENERICS: K(V)

        private readonly Queue queue;     // GENERICS: <V>
        private readonly Hashtable index; // GENERICS: <K,int>
        private readonly KeyProvider keyProvider;

        public HashQueue(ICollection collection, KeyProvider keyProvider)
        {
            queue = new Queue(collection);
            index = new Hashtable(StringComparer.CurrentCultureIgnoreCase);

            this.keyProvider = keyProvider;

            foreach (object item in collection)
                index[keyProvider(item)] = 1;
        }

        public int Count { get { return queue.Count; } }

        public object Dequeue()
        {
            object result = queue.Dequeue();
            object key = keyProvider(result);
            int count = (int)index[key];

            if (count == 0)
                index.Remove(key);
            else
                index[key] = count - 1;

            return result;
        }

        public void Enqueue(object item)
        {
            queue.Enqueue(item);
            object key = keyProvider(item);
            object count = index[key];

            index[key] = (count != null) ? (int)count + 1 : 1;
        }

        public bool Contains(object item)
        {
            return index.ContainsKey(keyProvider(item));
        }
    }

    #endregion

    #endregion

    #region FileSystemUtils

    /// <summary>
    /// File system utilities.
    /// </summary>
    public static partial class FileSystemUtils
    {
        /// <summary>
        /// Returns the given URL without the username/password information.
        /// </summary>
        /// <remarks>
        /// Removes the text between the last <c>"://"</c> and the following <c>'@'</c>.
        /// Does not check the URL for validity. Works for php://filter paths too.
        /// </remarks>
        /// <param name="url">The URL to modify.</param>
        /// <returns>The given URL with the username:password section replaced by <c>"..."</c>.</returns>
        public static string StripPassword(string url)
        {
            if (url == null) return null;

            int url_start = url.LastIndexOf("://");
            if (url_start > 0)
            {
                url_start += "://".Length;
                int pass_end = url.IndexOf('@', url_start);
                if (pass_end > url_start)
                {
                    StringBuilder sb = new StringBuilder(url.Length);
                    sb.Append(url.Substring(0, url_start));
                    sb.Append("...");
                    sb.Append(url.Substring(pass_end));  // results in: scheme://...@host
                    return sb.ToString();
                }
            }
            return url;
        }

        public static int FileSize(FileInfo fi)//TODO: Move this to PlatformAdaptationLayer
        {
            if (EnvironmentUtils.IsDotNetFramework)
            {
                // we are not calling full stat(), it is slow
                return (int)fi.Length;
            }
            else
            {
                //bypass Mono bug in FileInfo.Length
                using (FileStream stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return unchecked((int)stream.Length);
                }
            }
        }
    }

    #endregion
}
