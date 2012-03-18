/*

 Copyright (c) 2004-2005 Jan Benda and Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Runtime.Serialization;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	/// <summary>
	/// Base class for PHP Resources - both built-in and extension-resources.
	/// Resources rely on GC Finalization - override FreeManaged for cleanup.
	/// When printing a resource variable in PHP, "Resource id #x" prints out.
	/// </summary>
	[Serializable]
	public class PhpResource : IDisposable, IPhpVariable, ISerializable, IPhpObjectGraphNode
	{
		/// <summary>The name of this variable type.</summary>
		public const string PhpTypeName = "resource";

		/// <summary>
		/// Handles deserialization of <see cref="PhpResource"/> instances in cases when the instance was serialized
		/// with <see cref="StreamingContextStates.Persistence"/>
		/// </summary>
		[Serializable]
		internal class Deserializer : IObjectReference
		{
			#region IObjectReference Members

			/// <include file='Doc/Common.xml' path='/docs/method[@name="GetRealObject"]/*'/>
			/// <remarks>
			/// All PHP resources are deserialized as integer 0.
			/// </remarks>
			public object GetRealObject(StreamingContext context)
			{
				return 0;
			}

			#endregion
		}

		/// <summary>
		/// Allocate a unique identifier for a resource.
		/// </summary>
		/// <remarks>
		/// Internal resources are given even numbers while resources
		/// allocated by extensions get odd numbers to minimize the communication
		/// between internal and external resource managers.
		/// </remarks>
		/// <returns>The unique identifier of an internal resource (even number starting from 2).</returns>
		private static int RegisterInternalInstance()
		{
			Interlocked.Increment(ref PhpResource.ResourceIdCounter);

			// Even numbers are reserved for internal use (odd for externals)
			return PhpResource.ResourceIdCounter * 2;
		}

		/// <summary>
		/// Create a new instance with the given Id. Used by <see cref="PhpExternalResource"/>s.
		/// </summary>
		/// <param name="resourceId">Unique resource identifier (odd for external resources).</param>
		/// <param name="resourceTypeName">The type to be reported to use when dumping a resource.</param>
        /// <param name="registerInReqContext">Whether to register this instance in current <see cref="RequestContext"/>. Should be <c>false</c> for static resources.</param>
		protected PhpResource(int resourceId, String resourceTypeName, bool registerInReqContext)
		{
			this.mResourceId = resourceId;
			this.mTypeName = resourceTypeName;

            if (registerInReqContext)
            {
                // register this resource into RequestContext,
                // so the resource will be automatically disposed at the request end.
                RequestContext req_context = RequestContext.CurrentContext;
                if (req_context != null)
                    reqContextRegistrationNode = req_context.RegisterResource(this);
            }
		}

        /// <summary>
        /// Create a new instance with the given Id. Used by <see cref="PhpExternalResource"/>s.
        /// </summary>
        /// <param name="resourceId">Unique resource identifier (odd for external resources).</param>
        /// <param name="resourceTypeName">The type to be reported to use when dumping a resource.</param>
        protected PhpResource(int resourceId, String resourceTypeName)
            : this(resourceId, resourceTypeName, true) { }

		/// <summary>
		/// Create a new instance of a given Type and Name.
		/// The instance Id is auto-incrementing starting from 1.
		/// </summary>
		/// <param name="resourceTypeName">The type to be reported to use when dumping a resource.</param>
        public PhpResource(String resourceTypeName)
            : this(resourceTypeName, true) { }

        /// <summary>
        /// Create a new instance of a given Type and Name.
        /// The instance Id is auto-incrementing starting from 1.
        /// </summary>
        /// <param name="resourceTypeName">The type to be reported to use when dumping a resource.</param>
        /// <param name="registerInReqContext">Whether to register this instance in current <see cref="RequestContext"/>. Should be <c>false</c> for static resources.</param>
        public PhpResource(String resourceTypeName, bool registerInReqContext)
            : this(PhpResource.RegisterInternalInstance(), resourceTypeName, registerInReqContext)
        { }

#if !SILVERLIGHT
		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected PhpResource(SerializationInfo info, StreamingContext context)
		{
			mDisposed = info.GetBoolean("mDisposed");
			mResourceId = info.GetInt32("mResourceId");
			mTypeName = info.GetString("mTypeName");
		}
#endif

		/// <summary>
		/// Creates a new invalid resource.
		/// </summary>
		internal PhpResource()
		{
			mDisposed = true;
			mTypeName = DisposedTypeName;
		}

		/// <summary>
		/// Returns a string that represents the current PhpResource.
		/// </summary>
		/// <returns>'Resource id #ID'</returns>
		public override string ToString()
		{
			return String.Concat(PhpTypeName + " id #", this.mResourceId.ToString());
		}

		#region Finalization & Dispose Pattern

		/// <summary>
		/// The finalizer.
		/// </summary>
		~PhpResource()
		{
			Dispose(false);
		}

		/// <summary>
		/// An alias of <see cref="Dispose"/>.
		/// </summary>
		public virtual void Close()
		{
			Dispose();
		}

		/// <summary>
		/// Dosposes the resource.
		/// </summary>
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		/// <summary>
		/// Cleans-up the resource.
		/// </summary>
		/// <remarks>
		/// When disposing non-deterministically, only unmanaged resources should be freed. 
		/// <seealso cref="FreeUnmanaged"/>
		/// </remarks>
		/// <param name="disposing">Whether the resource is disposed deterministically.</param>
		private void Dispose(bool disposing)
		{
			if (!this.mDisposed)
			{
				this.mDisposed = true;

				// dispose managed resources:
				if (disposing)
				{
					this.FreeManaged();
				}

				// dispose unmanaged resources ("unfinalized"):
				this.FreeUnmanaged();

                // unregister from the RequestContext
                this.UnregisterResource();
			}

			// shows the user this Resource is no longer valid:
			this.mTypeName = PhpResource.DisposedTypeName;
		}

        /// <summary>
        /// Unregister this instance of <see cref="PhpResource"/> from current <see cref="RequestContext"/>.
        /// </summary>
        private void UnregisterResource()
        {
            if (this.reqContextRegistrationNode != null)
            {
                Debug.Assert(RequestContext.CurrentContext != null);
                RequestContext.CurrentContext.UnregisterResource(this.reqContextRegistrationNode);
                this.reqContextRegistrationNode = null;
            }
        }

		/// <summary>
		/// Override this virtual method in your descendants to perform 
		/// cleanup of Managed resources - those having a Finalizer of their own.
		/// </summary>
		/// <remarks>
		/// Note that when Disposing explicitly, both FreeManaged and FreeUnmanaged are called.
		/// </remarks>
		protected virtual void FreeManaged()
		{
		}

		/// <summary>
		/// Override this virtual method to cleanup the contained unmanaged objects.
		/// </summary>
		/// <remarks>
		/// Note that when Dispose(false) is called from the Finalizer,
		/// the order of finalization is random. In other words, contained
		/// managed objects may have been already finalized - don't reference them.
		/// </remarks>
		protected virtual void FreeUnmanaged()
		{
		}
		#endregion

		/// <summary>Identifier of a PhpResource instance. Unique index starting at 1</summary>
		public int Id { get { return mResourceId; } }

		// <summary>Type of PhpResource - used by extensions and get_resource_type()</summary>
		//REMoved public int Type { get { return mType; }}

		/// <summary>Type resource name - string to be reported to user when dumping a resource.</summary>
		public String TypeName { get { return mTypeName; } }

		/// <summary>false if the resource has been already disposed</summary>
		public bool IsValid { get { return !this.mDisposed; } }

		/// <summary>Unique resource identifier (even for internal resources, odd for external ones).</summary>
		/// <remarks>
		/// Internal resources are given even numbers while resources
		/// allocated by extensions get odd numbers to minimize the communication
		/// between internal and external resource managers.
		/// </remarks>
		protected int mResourceId;

		/// <summary>
		/// Type resource name - string to be reported to user when dumping a resource.
		/// </summary>
		protected String mTypeName;

		/// <summary>
		/// Set in Dispose to avoid multiple cleanup attempts.
		/// </summary>
		private bool mDisposed = false;

        /// <summary>
        /// If this resource is registered into <see cref="RequestContext"/>, this points into linked list containing registered resources.
        /// </summary>
        private System.Collections.Generic.LinkedListNode<PhpResource> reqContextRegistrationNode;

		/// <summary>Static counter for unique PhpResource instance Id's</summary>
		private static int ResourceIdCounter = 0;

		/// <summary>The resources' TypeName to be displayed after call to Dispose</summary>
		private static String DisposedTypeName = "Unknown";

		#region IPhpVariable Members

		/// <summary>
		/// Defines emptiness of the <see cref="PhpResource"/>.
		/// </summary>
		/// <returns><B>false</B>. A valid resource is never empty.</returns>
		public bool IsEmpty()
		{
			return !this.IsValid;
		}

		/// <summary>
		/// Defines whether <see cref="PhpResource"/> is a scalar.
		/// </summary>
		/// <returns><B>false</B></returns>
		public bool IsScalar()
		{
			return false;
		}

		/// <summary>
		/// Returns a name of declaring type.
		/// </summary>
		/// <returns>The name.</returns>
		public string GetTypeName()
		{
			return PhpTypeName;
		}

		#endregion

		#region IPhpCloneable Members

		/// <summary>Creates a copy of this instance.</summary>
		/// <remarks>
		/// Instances of the PhpResource class are never cloned.
		/// When assigning a resource to another variable in a script,
		/// only a shallow copy is performed.
		/// </remarks>
		/// <returns>The copy of this instance.</returns>
		public object Copy(CopyReason reason)
		{
			return this;
		}

		/// <summary>Creates a copy of this instance.</summary>
		/// <remarks>
		/// Instances of the PhpResource class are never cloned.
		/// When assigning a resource to another variable in a script,
		/// only a shallow copy is performed.
		/// </remarks>
		/// <returns>The copy of this instance.</returns>
		public object DeepCopy()
		{
			return this;
		}

		#endregion

		#region IPhpComparable Members

		/// <summary>
		/// Compares this instance with an object of arbitrary PHP.NET type.
		/// </summary>
		/// <remarks>
		/// When compared with other PHP variables, PhpResource behaves like 
		/// its integer representation, i.e. the resource ID (except of the === operator).
		/// </remarks>
		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareTo(obj)"]/*'/>
		public int CompareTo(object obj)
		{
			return CompareTo(obj, PhpComparer.Default);
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareTo(obj,comparer)"]/*' />
		public int CompareTo(object obj, IComparer/*!*/ comparer)
		{
			Debug.Assert(comparer != null);
            return comparer.Compare(this.mResourceId, obj);
		}

		#endregion

		#region IPhpPrintable interface

		/// <summary>
		/// Prints values only.
		/// 'Resource id #ID'
		/// </summary>
		/// <param name="output">The output stream.</param>
		public void Print(TextWriter output)
		{
			output.WriteLine(ToString());
		}

		/// <summary>
		/// Prints types and values.
		/// 'resource(ID) of type(TYPE)'
		/// </summary>
		/// <param name="output">The output stream.</param>
		public void Dump(TextWriter output)
		{
			output.WriteLine("resource({0}) of type ({1})", this.mResourceId, this.mTypeName);
		}

		/// <summary>
		/// Prints object's definition in PHP language.
		/// 'NULL' - unexportable
		/// </summary>
		/// <param name="output">The output stream.</param>
		public void Export(TextWriter output)
		{
			output.Write("NULL");
		}
		#endregion

		#region IPhpConvertible Members

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="GetTypeCode"]/*' />
		public PhpTypeCode GetTypeCode()
		{
			return PhpTypeCode.PhpResource;
		}

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToInteger"]/*' />
		public int ToInteger()
		{
			return this.IsValid ? this.mResourceId : 0;
		}

		/// <summary>
		/// Returns <c>0</c>.
		/// </summary>
		/// <returns><c>0</c></returns>
		public long ToLongInteger()
		{
			return this.IsValid ? this.mResourceId : 0;
		}

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToDouble"]/*' />
		public double ToDouble()
		{
			return this.IsValid ? this.mResourceId : 0;
		}

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToBoolean"]/*' />
		public bool ToBoolean()
		{
			return this.IsValid;
		}

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToPhpBytes"]/*' />
		public PhpBytes ToPhpBytes()
		{
			return new PhpBytes(ToString());
		}

        /// <summary>
		/// Converts instance to its string representation according to PHP conversion algorithm.
		/// </summary>
		/// <param name="success">Indicates whether conversion was successful.</param>
		/// <param name="throwOnError">Throw out 'Notice' when conversion wasn't successful?</param>
		/// <returns>The converted value.</returns>
		string IPhpConvertible.ToString(bool throwOnError, out bool success)
		{
			success = false;
			return ToString();
		}

		/// <summary>
		/// Converts instance to a number of type <see cref="int"/>.
		/// </summary>
		/// <param name="doubleValue">The resource id.</param>
		/// <param name="intValue">The resource id.</param>
		/// <param name="longValue">The resource id.</param>
		/// <returns><see cref="Convert.NumberInfo.Integer"/>.</returns>
		public Convert.NumberInfo ToNumber(out int intValue, out long longValue, out double doubleValue)
		{
			doubleValue = this.mResourceId;
			intValue = this.mResourceId;
			longValue = this.mResourceId;
			return Convert.NumberInfo.Integer;
		}

		#endregion

		#region ISerializable Members
#if !SILVERLIGHT

		/// <include file='Doc/Common.xml' path='/docs/method[@name="GetObjectData"]/*'/>
        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if ((context.State & StreamingContextStates.Persistence) == StreamingContextStates.Persistence
				|| !typeof(PhpExternalResource).IsInstanceOfType(this))
			{
				// serialization is requested by user via the serialize() PHP function
				info.SetType(typeof(Deserializer));
			}
			else
			{
				// serialization is requested by Remoting and this this is a PhpExternalResource
				info.AddValue("mDisposed", mDisposed);
				info.AddValue("mResourceId", mResourceId);
				info.AddValue("mTypeName", mTypeName);
			}
		}

#endif
		#endregion

		#region IPhpObjectGraphNode Members

		/// <summary>
		/// Walks the object graph rooted in this node.
		/// </summary>
		/// <param name="callback">The callback method.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		public void Walk(PhpWalkCallback callback, ScriptContext context)
		{
			// PhpResources have no child objects, however as they constitute an interesting PHP type,
			// IPhpObjectGraphNode is implemented
		}

		#endregion
	}

	/// <summary>
	/// Represents a resource that was created by an extension and lives in <c>ExtManager</c>.
	/// </summary>
	[Serializable]
	public class PhpExternalResource : PhpResource
	{
		/// <summary>
		/// Creates a new <see cref="PhpExternalResource"/>.
		/// </summary>
		/// <param name="resourceId">The resource ID assigned by the external resource manager.</param>
		/// <param name="typeName">The resource type name.</param>
		public PhpExternalResource(int resourceId, string typeName)
			: base(resourceId * 2 + 1, typeName)
		{ }

		/// <summary>
		/// Returns the resource ID given when creating this instance.
		/// </summary>
		/// <remarks><seealso cref="PhpExternalResource(int,string)"/></remarks>
		/// <returns>The resource ID.</returns>
		public int GetId()
		{
			return mResourceId / 2;
		}

		#region Serialization (CLR only)
#if !SILVERLIGHT

		/// <include file='Doc/Common.xml' path='/docs/method[@name="serialization.ctor"]/*'/>
		protected PhpExternalResource(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }

#endif
		#endregion
	}
}
