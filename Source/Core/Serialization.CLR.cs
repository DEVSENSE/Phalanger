/*

 Copyright (c) 2005-2006 Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

// Uncomment the following line to enable logging of serialization events into fields of instances
// being (de)serialized.
//#define SERIALIZATION_DEBUG_LOG

using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Serialization;

using PHP.Library;
using PHP.Core.Reflection;

namespace PHP.Core
{
	/// <summary>
	/// Caches current <see cref="ScriptContext"/> and class context.
	/// </summary>
	public sealed class SerializationContext
	{
		#region Construction

		/// <summary>
		/// Pulls out a serialization context from a provided streaming context or creates a new one.
		/// </summary>
		/// <param name="context">The streaming context.</param>
		/// <returns>A <see cref="SerializationContext"/>.</returns>
		public static SerializationContext/*!*/ CreateFromStreamingContext(StreamingContext context)
		{
			SerializationContext result = context.Context as SerializationContext;
			if (result == null) result = new SerializationContext();

			return result;
		}

		#endregion

		#region ScriptContext

		/// <summary>
		/// Returns current <see cref="ScriptContext"/>. Lazily initialized.
		/// </summary>
		public ScriptContext/*!*/ ScriptContext
		{
			get
			{
				if (scriptContext == null) scriptContext = ScriptContext.CurrentContext;
				return scriptContext;
			}
		}
		private ScriptContext scriptContext;

		#endregion

		#region ClassContext

		/// <summary>
		/// Returns current class context. Lazily initialized.
		/// </summary>
		public DTypeDesc ClassContext
		{
			get
			{
				if (!classContextValid)
				{
					classContext = PhpStackTrace.GetClassContext();
					classContextValid = true;
				}
				return classContext;
			}
		}
		private DTypeDesc classContext;
		private bool classContextValid;

		#endregion

		#region SleepResults

		/// <summary>
		/// Returns a dictionary of <c>__sleep</c> method return values.
		/// </summary>
		/// <remarks>
		/// Serialization asks for an object's values multiple times (flaw?) but <c>__sleep</c>
		/// should be called only once for each instance present in the object graph.
		/// </remarks>
		public Dictionary<DObject, PhpArray>/*!*/ SleepResults
		{
			get
			{
				if (sleepResults == null) sleepResults = new Dictionary<DObject, PhpArray>();
				return sleepResults;
			}
		}
		private Dictionary<DObject, PhpArray> sleepResults;

		/// <summary>
		/// Set as a value for the instances that do not implement <c>__sleep</c>/
		/// </summary>
		public static readonly PhpArray/*!*/ NoSleepResultSingleton = new PhpArray();

		#endregion
	}

	/// <summary>
	/// Implements .NET serialization.
	/// </summary>
	/// <remarks>
	/// This class is currently not registered as a surrogate for any type. <see cref="DObject"/> implements
	/// <see cref="ISerializable"/> and delegates to this class manually.
	/// </remarks>
	internal sealed class SerializationSurrogate : ISerializationSurrogate
	{
		#region Fields

		public static readonly SerializationSurrogate Instance = new SerializationSurrogate();

		private const string MembersSerializationInfoKey = "<members>";

		#endregion

		#region ISerializationSurrogate Members

		/// <summary>
		/// Populates the provided <see cref="SerializationInfo"/> with the data needed to serialize the object.
		/// </summary>
		/// <param name="obj">The object to serialize.</param>
		/// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
		/// <param name="context">The destination for this serialization.</param>
        [System.Security.SecurityCritical]
        public void GetObjectData(object/*!*/ obj, SerializationInfo/*!*/ info, StreamingContext context)
		{
			DObject instance = (DObject)obj;

			if ((context.State & StreamingContextStates.Persistence) != StreamingContextStates.Persistence)
			{
				Serialization.DebugInstanceSerialized(instance, false);

				// serialization is requested by Remoting -> serialize everything and do not change type
				MemberInfo[] members = FormatterServices.GetSerializableMembers(instance.GetType());
				info.AddValue(MembersSerializationInfoKey, FormatterServices.GetObjectData(instance, members));
			}
			else
			{
				Serialization.DebugInstanceSerialized(instance, true);

				// Serialization was requested by the user via the serialize() PHP function so it is possible that
				// the type of this instance will be undefined at deserialization time.

				if (instance.RealObject is Library.SPL.Serializable)
				{
					// the instance is PHP5.1 serializable -> reroute the deserialization to SPLDeserializer
					SPLDeserializer.GetObjectData(instance, info, context);
				}
				else
				{
					// otherwise reroute the deserialization to Deserializer, which handles __sleep
					Deserializer.GetObjectData(instance, info, context);
				}
			}
		}

		/// <summary>
		/// Populates the object using the information in the <see cref="SerializationInfo"/>.
		/// </summary>
		/// <param name="obj">The object to populate.</param>
		/// <param name="info">The information to populate the object.</param>
		/// <param name="context">The source from which the object is deserialized.</param>
		/// <param name="selector">The surrogate selector where the search for a compatible surrogate begins.</param>
		/// <returns>The populated deserialized object.</returns>
		public object SetObjectData(object/*!*/ obj, SerializationInfo/*!*/ info, StreamingContext context,
			ISurrogateSelector selector)
		{
			// use the instance's RuntimeFields as a temp storage for deserialized members
			DObject instance = (DObject)obj;

            instance.RuntimeFields = new PhpArray();
			instance.RuntimeFields.Add(MembersSerializationInfoKey,
				info.GetValue(MembersSerializationInfoKey, typeof(object[])));

			// the instance will be populated in OnDeserialization
			return obj;
		}

		#endregion

		#region OnDeserialization

		/// <summary>
		/// Runs when the entire object graph has been deserialized.
		/// </summary>
		/// <param name="obj">The object being deserialized.</param>
		public void OnDeserialization(object/*!*/ obj)
		{
			DObject instance = (DObject)obj;

			MemberInfo[] members = FormatterServices.GetSerializableMembers(instance.GetType());

			// get deserialized members from the temp storage
			Debug.Assert(instance.RuntimeFields != null && instance.RuntimeFields.Count == 1);

			object[] deserialized_members = (object[])instance.RuntimeFields[MembersSerializationInfoKey];
			instance.RuntimeFields.Clear();

			if (deserialized_members.Length != members.Length) throw new InvalidOperationException();

			FormatterServices.PopulateObjectMembers(instance, members, deserialized_members);

			Serialization.DebugInstanceDeserialized(instance, false);
		}

		#endregion
	}

	/// <summary>
	/// Handles deserialization for classes derived from <see cref="DObject"/> whose real objects
	/// do not implement the <see cref="Library.SPL.Serializable"/> interface.
	/// </summary>
	/// <remarks>
	/// The result of deserialization is either the original class or <see cref="__PHP_Incomplete_Class"/>
	/// if the original class is undefined.
	/// </remarks>
	[Serializable]
	internal class Deserializer : ISerializable, IDeserializationCallback, IObjectReference
	{
		#region Fields

		/// <summary>
		/// The key used for CLR real object when there's no <c>__sleep</c>.
		/// </summary>
		private const string ClrRealObjectSerializationInfoKey = "__RealObject";

		/// <summary>
		/// The real object to be returned by <see cref="GetRealObject"/>.
		/// </summary>
		protected DObject instance;

		/// <summary>
		/// The <see cref="SerializationInfo"/> passed to the deserializing constructor.
		/// </summary>
		protected SerializationInfo/*!*/ serInfo;

		/// <summary>
		/// A serialization context holding SC and class context.
		/// </summary>
		protected SerializationContext/*!*/ context;

		#endregion

		#region Construction

		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected Deserializer(SerializationInfo info, StreamingContext context)
		{
			this.serInfo = info;
			this.context = SerializationContext.CreateFromStreamingContext(context);

			string class_name_str = info.GetString(__PHP_Incomplete_Class.ClassNameFieldName);
			if (String.IsNullOrEmpty(class_name_str))
			{
				// note that we must never return null from GetRealObject (formatters do not like it)
				return;
			}

			instance = Serialization.GetUninitializedInstance(class_name_str, this.context.ScriptContext);
			if (instance == null)
			{
				throw new SerializationException(CoreResources.GetString("class_instantiation_failed", class_name_str));
			}
		}

		#endregion

		#region GetObjectData

		/// <summary>
		/// Populates the provided <see cref="SerializationInfo"/> with the data needed to serialize the object.
		/// </summary>
		/// <param name="instance">The object to serialize.</param>
		/// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
		/// <param name="strctx">Streaming context (should contain <see cref="SerializationContext"/>).</param>
        [System.Security.SecurityCritical]
        public static void GetObjectData(DObject/*!*/ instance, SerializationInfo/*!*/ info, StreamingContext strctx)
		{
			info.SetType(typeof(Deserializer));

			SerializationContext context = SerializationContext.CreateFromStreamingContext(strctx);

			bool sleep_called;
			PhpArray sleep_result;

			// try to get the caches __sleep result
			if (context.SleepResults.TryGetValue(instance, out sleep_result))
			{
				if (Object.ReferenceEquals(sleep_result, SerializationContext.NoSleepResultSingleton))
				{
					sleep_called = false;
					sleep_result = null;
				}
				else sleep_called = true;
			}
			else
			{
				sleep_result = instance.Sleep(context.ClassContext, context.ScriptContext, out sleep_called);
				context.SleepResults.Add(instance, (sleep_called ? sleep_result : SerializationContext.NoSleepResultSingleton));
			}

			if (sleep_called && sleep_result == null)
			{
				// __sleep did not return an array -> this instance will deserialize as NULL
				info.AddValue(__PHP_Incomplete_Class.ClassNameFieldName, String.Empty);
			}
			else
			{
				// if we have a sleep result, serialize fields according to it, otherwise serialize all fields

				IEnumerable<KeyValuePair<string, object>> serializable_properties;
				object real_object = null;

				if (sleep_result == null)
				{
					serializable_properties = Serialization.EnumerateSerializableProperties(
						instance,
						true); // get PHP fields only

					// serialize CLR real object in the "CLR way"
					if (!(instance is PhpObject)) real_object = instance.RealObject;
				}
				else
				{
					serializable_properties = Serialization.EnumerateSerializableProperties(
						instance,
						sleep_result,
						context.ScriptContext);
				}

				bool type_name_serialized = false;
				bool real_object_serialized = false;

				foreach (KeyValuePair<string, object> pair in serializable_properties)
				{
					if (pair.Key == __PHP_Incomplete_Class.ClassNameFieldName) type_name_serialized = true;

					if (pair.Key == ClrRealObjectSerializationInfoKey)
					{
						// unwrap the possibly wrapped CLR real object
						info.AddValue(pair.Key, PhpVariable.Unwrap(pair.Value));

						real_object_serialized = true;
					}
					else
					{
						PhpReference reference = pair.Value as PhpReference;
						info.AddValue(pair.Key, WrapPropertyValue(pair.Value));
					}
				}

				// if the type name has not been serialized, do it now
				if (!type_name_serialized) info.AddValue(__PHP_Incomplete_Class.ClassNameFieldName, instance.TypeName);

				// if the real object has not been serialized, do it now
				if (!real_object_serialized) info.AddValue(ClrRealObjectSerializationInfoKey, real_object);
			}
		}

		#endregion

		#region ISerializable Members

		/// <include file='Doc/Common.xml' path='/docs/method[@name="GetObjectData"]/*'/>
        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			// should never be called
			throw new InvalidOperationException();
		}

		#endregion

		#region IDeserializationCallback Members

		/// <include file='Doc/Common.xml' path='/docs/method[@name="OnDeserialization"]/*'/>
		public virtual void OnDeserialization(object sender)
		{
			if (instance == null) return;

			object real_object = serInfo.GetValue(ClrRealObjectSerializationInfoKey, typeof(object));
			if (real_object != null)
			{
				// if we have serialized CLR real object, populate the instance now
				if (instance is __PHP_Incomplete_Class)
				{
					Serialization.SetProperty(
						instance,
						ClrRealObjectSerializationInfoKey,
						ClrObject.WrapRealObject(real_object),
						context.ScriptContext);
				}
                else if (instance is IClrValue)
                {
                    Type type = instance.GetType(); // generic type ClrValue<T>
                    FieldInfo field = type.GetField("realValue");
                    Debug.Assert(field != null);
                    field.SetValue(instance, real_object);
                }
				else
				{
					((ClrObject)instance).SetRealObject(real_object);
				}
			}

			// deserialize fields
			SerializationInfoEnumerator enumerator = serInfo.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string name = enumerator.Name;

				if (name != __PHP_Incomplete_Class.ClassNameFieldName &&
					name != ClrRealObjectSerializationInfoKey)
				{
					Serialization.SetProperty(
						instance,
						name,
						UnwrapPropertyValue(enumerator.Value),
						context.ScriptContext);
				}
			}

			Serialization.DebugInstanceDeserialized(instance, true);

			instance.Wakeup(context.ClassContext, context.ScriptContext);
		}

		#endregion

		#region IObjectReference Members

		/// <include file='Doc/Common.xml' path='/docs/method[@name="GetRealObject"]/*'/>
		/// <remarks>
		/// The result is either an instance of the class that has originally been serialized,
		/// an instance of <see cref="__PHP_Incomplete_Class"/> if the real class is undefined
		/// or <B>null</B> if the class has a faulty <c>__sleep</c> and should be deserialized as <B>null</B>.
		/// </remarks>
		public object/*!*/ GetRealObject(StreamingContext context)
		{
			// never return null - formatter needs to have non-null ref for fix-ups
			if (instance == null) return new PhpSmartReference(null);
			return instance;
		}

		#endregion

		#region WrapPropertyValue, UnwrapPropertyValue

		/// <summary>
		/// Wraps a <see cref="DObject"/> with a non-aliased <see cref="PhpReference"/> to allow for reference fix-ups
		/// after deserialization.
		/// </summary>
		/// <param name="val">The object to wrap.</param>
		/// <returns><see cref="PhpReference"/> to <paramref name="val"/> if <paramref name="val"/> is a
		/// <see cref="DObject"/>, or <paramref name="val"/> itself otherwise.</returns>
		/// <remarks>
		/// <para>
		/// The purpose of this wrapping is to avoid <see cref="DObject"/>s referencing another <see cref="DObject"/>s
		/// directly which causes problems when such an object graph is deserialized.
		/// </para>
		/// <para>
		/// Without this wrapping, under certain circumstances serialization would fail with the following exception
		/// message: The object with ID 5 implements the <see cref="IObjectReference"/> interface for which all
		/// dependencies cannot be resolved. The likely cause is two instances of <see cref="IObjectReference"/> that
		/// have a mutual dependency on each other.
		/// </para>
		/// </remarks>
		internal static object WrapPropertyValue(object val)
		{
			if (val is DObject) return new PhpSmartReference(val);
			return val;
		}

		/// <summary>
		/// Eliminates non-aliased <see cref="PhpReference"/>s. <seealso cref="WrapPropertyValue"/>
		/// </summary>
		/// <param name="val">The object to unwrap.</param>
		/// <returns><paramref name="val"/>'s value if it is a non-aliased <see cref="PhpReference"/>, or
		/// <paramref name="val"/> itself otherwise.</returns>
		internal static object UnwrapPropertyValue(object val)
		{
			PhpReference reference = val as PhpReference;
			if (reference != null && !reference.IsAliased) return reference.Value;
			return val;
		}

		#endregion
	}

	/// <summary>
	/// Handles deserialization for classes derived from <see cref="DObject"/> whose real objects implement the
	/// <see cref="Library.SPL.Serializable"/> interface.
	/// </summary>
	/// <remarks>
	/// The result of deserialization is either the original class or <B>null</B> if
	/// <see cref="Library.SPL.Serializable.serialize"/> returned <B>null</B>.
	/// </remarks>
	[Serializable]
	internal class SPLDeserializer : Deserializer
	{
		#region Fields

		/// <summary>
		/// Name of the serialized field that holds the string returned by <see cref="Library.SPL.Serializable.serialize"/>.
		/// </summary>
		internal const string SerializedDataFieldName = "__Serialized_Data";

		#endregion

		#region Construction

		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected SPLDeserializer(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			// if binding failed, we cannot continue (__PHP_Incomplete_Class is no good either)
			if (instance != null && !(instance is Library.SPL.Serializable))
			{
				throw new SerializationException(CoreResources.GetString("class_has_no_unserializer",
					instance.TypeName));
			}
		}

		#endregion

		#region GetObjectData

		/// <summary>
		/// Populates the provided <see cref="SerializationInfo"/> with the data needed to serialize the object.
		/// </summary>
		/// <param name="instance">The object to serialize.</param>
		/// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
		/// <param name="strctx">Streaming context (should contain <see cref="SerializationContext"/>).</param>
        [System.Security.SecurityCritical]
        new public static void GetObjectData(DObject/*!*/ instance, SerializationInfo/*!*/ info, StreamingContext strctx)
		{
			info.SetType(typeof(SPLDeserializer));

			SerializationContext context = SerializationContext.CreateFromStreamingContext(strctx);

			object res = PhpVariable.Dereference(instance.InvokeMethod("serialize", null, context.ScriptContext));
			if (res == null)
			{
				// serialize returned NULL -> this instance will deserialize as NULL
				info.AddValue(__PHP_Incomplete_Class.ClassNameFieldName, String.Empty);
			}
			else
			{
				string res_str = PhpVariable.AsString(res);
				if (res_str == null)
				{
					// serialize did not return NULL nor a string -> throw an exception
                    Library.SPL.Exception.ThrowSplException(
                        _ctx => new Library.SPL.Exception(_ctx, true),
                        context.ScriptContext,
                        string.Format(CoreResources.serialize_must_return_null_or_string, instance.TypeName), 0, null);
				}

				info.AddValue(SerializedDataFieldName, res_str);
				info.AddValue(__PHP_Incomplete_Class.ClassNameFieldName, instance.TypeName);
			}
		}

		#endregion

		#region IDeserializationCallback Members

		/// <include file='Doc/Common.xml' path='/docs/method[@name="OnDeserialization"]/*'/>
		public override void OnDeserialization(object sender)
		{
			// check whether serialize() returned null
			if (instance == null) return;

			Serialization.DebugInstanceDeserialized(instance, true);

			Debug.Assert(instance.RealObject is Library.SPL.Serializable);

			// invoke unserialize
			context.ScriptContext.Stack.AddFrame(serInfo.GetString(SerializedDataFieldName));
			instance.InvokeMethod("unserialize", null, context.ScriptContext);
		}

		#endregion
	}
}
