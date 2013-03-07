using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace PHP.Library.Soap
{
    ///// <summary>
    ///// 
    ///// </summary>
    //[Serializable]
    //public class InvocationException : Exception
    //{
    //    /// <summary>
    //    /// Creates a new <see cref="MessageStorageException"/> instance.
    //    /// </summary>
    //    public InvocationException()
    //    {
    //    }

    //    /// <summary>
    //    /// Creates a new <see cref="MessageStorageException"/> instance.
    //    /// </summary>
    //    /// <param name="message">Message.</param>
    //    public InvocationException(string message)
    //        : base(message)
    //    {
    //    }

    //    /// <summary>
    //    /// Creates a new <see cref="MessageStorageException"/> instance.
    //    /// </summary>
    //    /// <param name="message">Message.</param>
    //    /// <param name="inner">Inner.</param>
    //    public InvocationException(string message, Exception inner)
    //        : base(message, inner)
    //    {
    //    }

    //    /// <summary>
    //    /// Creates a new <see cref="MessageStorageException"/> instance.
    //    /// </summary>
    //    /// <param name="serializationInfo">Serialization info.</param>
    //    /// <param name="serializationContext">Serialization context.</param>
    //    protected InvocationException(SerializationInfo serializationInfo, StreamingContext serializationContext)
    //        : base(serializationInfo, serializationContext)
    //    { }
    //}

    /// <summary>
    /// Exception that can occur when dynamicaly creating proxy type from WSDL
    /// </summary>
    [Serializable]
    public class DynamicCompilationException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="DynamicCompilationException"/> instance.
        /// </summary>
        public DynamicCompilationException()
        {
        }

        /// <summary>
        /// Creates a new <see cref="DynamicCompilationException"/> instance.
        /// </summary>
        /// <param name="message">Message.</param>
        public DynamicCompilationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="DynamicCompilationException"/> instance.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner.</param>
        public DynamicCompilationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Creates a new <see cref="DynamicCompilationException"/> instance.
        /// </summary>
        /// <param name="serializationInfo">Serialization info.</param>
        /// <param name="serializationContext">Serialization context.</param>
        protected DynamicCompilationException(SerializationInfo serializationInfo, StreamingContext serializationContext)
            : base(serializationInfo, serializationContext)
        { }
    }

    /// <summary>
    /// Exception that can occur when injecting SoapExtension into pipeline
    /// </summary>
    [Serializable]
    public class PipelineConfigurationException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="PipelineConfigurationException"/> instance.
        /// </summary>
        public PipelineConfigurationException()
        {
        }

        /// <summary>
        /// Creates a new <see cref="PipelineConfigurationException"/> instance.
        /// </summary>
        /// <param name="message">Message.</param>
        public PipelineConfigurationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="PipelineConfigurationException"/> instance.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner.</param>
        public PipelineConfigurationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Creates a new <see cref="PipelineConfigurationException"/> instance.
        /// </summary>
        /// <param name="serializationInfo">Serialization info.</param>
        /// <param name="serializationContext">Serialization context.</param>
        protected PipelineConfigurationException(SerializationInfo serializationInfo, StreamingContext serializationContext)
            : base(serializationInfo, serializationContext)
        { }
    }

    /// <summary>
    /// Exception when trying to instantiate proxy object for SOAP service
    /// </summary>
    [Serializable]
    public class ProxyTypeInstantiationException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="ProxyTypeInstantiationException"/> instance.
        /// </summary>
        public ProxyTypeInstantiationException()
        {
        }

        /// <summary>
        /// Creates a new <see cref="ProxyTypeInstantiationException"/> instance.
        /// </summary>
        /// <param name="message">Message.</param>
        public ProxyTypeInstantiationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ProxyTypeInstantiationException"/> instance.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner.</param>
        public ProxyTypeInstantiationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ProxyTypeInstantiationException"/> instance.
        /// </summary>
        /// <param name="serializationInfo">Serialization info.</param>
        /// <param name="serializationContext">Serialization context.</param>
        protected ProxyTypeInstantiationException(SerializationInfo serializationInfo, StreamingContext serializationContext)
            : base(serializationInfo, serializationContext)
        { }
    }
}
