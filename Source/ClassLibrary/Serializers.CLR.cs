
/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Runtime.Serialization;
using PHP.Core;
using PHP.Core.Reflection;
using System.Collections.Generic;

namespace PHP.Library
{
	#region Serializer

	/// <summary>
	/// A base class for serializers, i.e. a named formatters.
	/// </summary>
	public abstract class Serializer : MarshalByRefObject
	{
        #region ClassContextHolder

        /// <summary>
        /// Common base class of <c>ObjectWriter</c> and <c>ObjectReader</c> containing the cached class context functionality.
        /// </summary>
        /// <remarks>
        /// Class context is needed when invoking <c>__sleep</c> and <c>__wakeup</c> magic methods.
        /// </remarks>
        internal abstract class ClassContextHolder
        {
            /// <summary>
            /// Initialize the ClassCOntextHolder with a known DTypeDesc.
            /// Use UnknownTypeDesc.Singleton to specify an unknown caller. In this case the caller will be determined when needed.
            /// </summary>
            /// <param name="caller"></param>
            public ClassContextHolder(DTypeDesc caller)
            {
                if (caller == null || !caller.IsUnknown)
                {
                    ClassContext = caller;
                }
            }

            /// <summary>
            /// Copies info from already used ClassContextHolder. It reuses the holder iff class context was already initialized.
            /// </summary>
            /// <param name="holder">Exiting class context holder with potentionaly already obtained class context.</param>
            internal ClassContextHolder(ClassContextHolder/*!*/holder)
            {
                Debug.Assert(holder != null);

                this._classContext = holder._classContext;
                this.classContextIsValid = holder.classContextIsValid;
            }

            /// <summary>
            /// Gets or sets the current class context. See <see cref="_classContext"/>.
            /// </summary>
            protected DTypeDesc ClassContext
            {
                get
                {
                    return (classContextIsValid ?
                        _classContext :
                        (ClassContext = PhpStackTrace.GetClassContext()));
                }
                set
                {
                    _classContext = value;
                    classContextIsValid = true;
                }
            }

            /// <summary>
            /// Holds the current class context (a type derived from <see cref="DObject"/> in whose
            /// scope the calling code is executing). Initialized lazily.
            /// </summary>
            private DTypeDesc _classContext;

            /// <summary>
            /// Invalid class context singleton. The initial value for <see cref="_classContext"/>.
            /// </summary>
            private bool classContextIsValid;        
        }

        #endregion

		/// <summary>
		/// Gets a name of the serializer. Shouldn't return a <B>null</B> reference.
		/// </summary>
		protected abstract string GetName();

		/// <summary>
		/// Creates a formatter. Shouldn't return a <B>null</B> reference.
		/// </summary>
        /// <param name="caller">DTypeDesc of the class context or UnknownTypeDesc if class context is not known yet and will be determined lazily.</param>
		protected abstract IFormatter CreateFormatter(DTypeDesc caller);

		/// <summary>
		/// Gets tring representation of the serializer.
		/// </summary>
		/// <returns>The name of the serializer.</returns>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Gets the serializer name (always non-null).
		/// </summary>
		public string Name
		{
			get
			{
				string result = GetName();
				if (result == null)
					throw new InvalidMethodImplementationException(GetType().FullName + ".GetName");
				return result;
			}
		}

		/// <summary>
		/// Creates a formatter (always non-null).
		/// </summary>
        /// <param name="caller">DTypeDesc of the class context or UnknownTypeDesc if class context is not known yet and will be determined lazily.</param>
        /// <returns>New IFormatter class instance.</returns>
        private IFormatter GetFormatter(DTypeDesc caller)
		{
			IFormatter result = CreateFormatter(caller);
			if (result == null)
				throw new InvalidMethodImplementationException(GetType().FullName + "CreateFormatter");
			return result;
		}

		/// <summary>
		/// Serializes a graph of connected objects to a byte array using a given formatter.
		/// </summary>
		/// <param name="variable">The variable to serialize.</param>
        /// <param name="caller">DTypeDesc of the caller's class context if it is known or UnknownTypeDesc if it should be determined lazily.</param>
        /// <returns>
		/// The serialized representation of the <paramref name="variable"/> or a <B>null</B> reference on error.
		/// </returns>
		/// <exception cref="PhpException">Serialization failed (Notice).</exception>
		public PhpBytes Serialize(object variable, DTypeDesc caller)
		{
			MemoryStream stream = new MemoryStream();

			try
			{
				try
				{
					// serialize the variable into the memory stream
                    GetFormatter(caller).Serialize(stream, variable);
				}
				catch (System.Reflection.TargetInvocationException e)
				{
					throw e.InnerException;
				}
			}
			catch (SerializationException e)
			{
				PhpException.Throw(PhpError.Notice, LibResources.GetString("serialization_failed", e.Message));
				return null;
			}

			// extract the serialized data
			return new PhpBytes(stream.ToArray());
		}

		/// <summary>
		/// Deserializes a graph of connected object from a byte array using a given formatter.
		/// </summary>
		/// <param name="bytes">The byte array to deserialize the graph from.</param>
        /// <param name="caller">DTypeDesc of the caller's class context if it is known or UnknownTypeDesc if it should be determined lazily.</param>
        /// <returns>
		/// The deserialized object graph or an instance of <see cref="PhpReference"/> containing <B>false</B> on error.
		/// </returns>
		/// <exception cref="PhpException">Deserialization failed (Notice).</exception>
        public PhpReference Deserialize(PhpBytes bytes, DTypeDesc caller)
		{
            MemoryStream stream = new MemoryStream(bytes.ReadonlyData);
			object result = null;

			try
			{
				try
				{
					// deserialize the data
                    result = GetFormatter(caller).Deserialize(stream);
				}
				catch (System.Reflection.TargetInvocationException e)
				{
					throw e.InnerException;
				}
			}
			catch (SerializationException e)
			{
				PhpException.Throw(PhpError.Notice, LibResources.GetString("deserialization_failed",
				  e.Message, stream.Position, stream.Length));
				return new PhpReference(false);
			}

			return PhpVariable.MakeReference(result);
		}
	}

	#endregion

	#region SingletonSerializer

	/// <summary>
	/// Represents a serializer with a singleton formatter.
	/// </summary>
	/// <threadsafety static="true"/>
	public sealed class SingletonSerializer : Serializer
	{
		/// <summary>
		/// A name of the serializer. Can't contain a <B>null</B> reference.
		/// </summary>
		private readonly string/*!*/ name;

		/// <summary>
		/// A formatter. Can't contain a <B>null</B> reference.
		/// </summary>
		private readonly IFormatter/*!*/ formatter;

		/// <summary>
		/// Creates a new instance of the serializer.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="formatter">The formatter.</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="formatter"/> are <B>null</B> references.</exception>
		public SingletonSerializer(string/*!*/ name, IFormatter/*!*/ formatter)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			if (formatter == null)
				throw new ArgumentNullException("formatter");

			this.name = name;
			this.formatter = formatter;
		}

		/// <summary>
		/// Returns the name.
		/// </summary>
		protected override string/*!*/ GetName()
		{
			return name;
		}

		/// <summary>
		/// Returns the formatter.
		/// </summary>
        protected override IFormatter/*!*/ CreateFormatter(DTypeDesc caller)
		{
			return formatter;
		}
	}

	#endregion

	#region ContextualSerializer

	/// <summary>
	/// Prepresents a serializer with a formatter utilizing the <see cref="SerializationContext"/>.
	/// </summary>
	/// <threadsafety static="true"/>
	public sealed class ContextualSerializer : Serializer
	{
		public delegate IFormatter/*!*/ FormatterFactory(DTypeDesc caller);

		/// <summary>
		/// A name of the serializer. Can't contain a <B>null</B> reference.
		/// </summary>
		private readonly string/*!*/ name;

		/// <summary>
		/// A formatter. Can't contain a <B>null</B> reference.
		/// </summary>
		private readonly FormatterFactory/*!*/ formatterFactory;

		/// <summary>
		/// Creates a new instance of the serializer.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="formatterFactory">The factory that supplies fresh instances of the formatter.</param>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="formatterFactory"/> are <B>null</B> references.</exception>
		public ContextualSerializer(string/*!*/ name, FormatterFactory/*!*/ formatterFactory)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			if (formatterFactory == null)
				throw new ArgumentNullException("formatterFactory");

			this.name = name;
			this.formatterFactory = formatterFactory;
		}

		/// <summary>
		/// Returns the name.
		/// </summary>
		protected override string/*!*/ GetName()
		{
			return name;
		}

		/// <summary>
		/// Returns the formatter.
		/// </summary>
        protected override IFormatter/*!*/ CreateFormatter(DTypeDesc caller)
		{
			return formatterFactory(caller);
		}
	}

	#endregion

	#region PhpSerializer

	public sealed class PhpSerializer : Serializer
	{
		private PhpSerializer() { }

		/// <summary>
		/// A singleton instance. 
		/// </summary>
		public static readonly PhpSerializer Default = new PhpSerializer();

		/// <summary>
		/// Returns the name.
		/// </summary>
		protected override string GetName()
		{
			return "php";
		}

		/// <summary>
		/// Returns the formatter using the current page encoding set in the global configuration.
		/// </summary>
        protected override IFormatter CreateFormatter(DTypeDesc caller)
		{
			return new PhpFormatter(Configuration.Application.Globalization.PageEncoding, caller);
		}
	}

	#endregion

    #region PhpJsonSerializer

    public sealed class PhpJsonSerializer : Serializer
    {
        private readonly JsonFormatter.EncodeOptions encodeOptions;
        private readonly JsonFormatter.DecodeOptions decodeOptions;
        
        /// <summary>
        /// Initialize parametrized serializer.
        /// </summary>
        internal PhpJsonSerializer(JsonFormatter.EncodeOptions encodeOptions, JsonFormatter.DecodeOptions decodeOptions)
        {
            // options
            this.encodeOptions = encodeOptions;
            this.decodeOptions = decodeOptions;
        }

        /// <summary>
        /// A singleton instance with default parameters.
        /// </summary>
        public static readonly PhpJsonSerializer Default = new PhpJsonSerializer(new JsonFormatter.EncodeOptions(), new JsonFormatter.DecodeOptions());

        /// <summary>
        /// Returns the name.
        /// </summary>
        protected override string GetName()
        {
            return "JSON";
        }

        /// <summary>
        /// Returns the formatter using the current page encoding set in the global configuration.
        /// </summary>
        protected override IFormatter CreateFormatter(DTypeDesc caller)
        {
            return new JsonFormatter(Configuration.Application.Globalization.PageEncoding, encodeOptions, decodeOptions, caller);
        }
    }

    #endregion

    //#region PhalangerSerializer

    //public sealed class PhalangerSerializer : Serializer
    //{
    //    private PhalangerSerializer() { }

    //    /// <summary>
    //    /// A singleton instance. 
    //    /// </summary>
    //    public static readonly PhalangerSerializer Default = new PhalangerSerializer();

    //    /// <summary>
    //    /// Returns the name.
    //    /// </summary>
    //    protected override string GetName()
    //    {
    //        return "phalanger";
    //    }

    //    /// <summary>
    //    /// Returns the formatter using the current page encoding set in the global configuration.
    //    /// </summary>
    //    protected override IFormatter CreateFormatter(DTypeDesc caller)
    //    {
    //        return new PhalangerFormatter(Configuration.Application.Globalization.PageEncoding, caller);
    //    }
    //}

    //#endregion

	#region Serializers

	/// <summary>
	/// Maintains serializers. Libraries can register their own serializers here.
	/// </summary>
	public static class Serializers
	{
		/// <summary>
		/// Registered handlers.
		/// </summary>
		private static Dictionary<string, Serializer> serializers = new Dictionary<string, Serializer>();
		private static readonly object serializersLock = new object();

		/// <summary>
		/// Registeres a new serializer. Serializers are usualy registered by libraries.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		/// <returns>Whether the serializer has been successfuly registered. Two serializers with the same names can't be registered.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="serializer"/> is a <B>null</B> reference.</exception>
		public static bool RegisterSerializer(Serializer serializer)
		{
            if (serializer == null) throw new ArgumentNullException("serializer");

			lock (serializersLock)
			{
				if (serializers.ContainsKey(serializer.Name))
					return false;

				serializers.Add(serializer.Name, serializer);
			}

			return true;
		}

		/// <summary>
		/// Gets a serializer by specified name.
		/// </summary>
		/// <param name="name">The name of the serializer.</param>
		/// <returns>The serializer or <B>null</B> reference if such serializer has not been registered.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is a <B>null</B> reference.</exception>
		public static Serializer GetSerializer(string name)
		{
			if (name == null) throw new ArgumentNullException("name");

			lock (serializersLock)
			{
                if (serializers.ContainsKey(name))
                {
                    return (Serializer)serializers[name];
                }
                else
                {
                    return null;
                }
			}
		}
	}

	#endregion
}
