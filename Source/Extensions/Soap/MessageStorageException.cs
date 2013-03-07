using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace PHP.Library.Soap
{
    /// <summary>
    /// Exception that can occur when storing SOAP message
    /// </summary>
    [Serializable]
    public class MessageStorageException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="MessageStorageException"/> instance.
        /// </summary>
        public MessageStorageException()
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessageStorageException"/> instance.
        /// </summary>
        /// <param name="message">Message.</param>
        public MessageStorageException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessageStorageException"/> instance.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner.</param>
        public MessageStorageException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Creates a new <see cref="MessageStorageException"/> instance.
        /// </summary>
        /// <param name="serializationInfo">Serialization info.</param>
        /// <param name="serializationContext">Serialization context.</param>
        protected MessageStorageException(SerializationInfo serializationInfo, StreamingContext serializationContext)
            : base(serializationInfo, serializationContext)
        { }
    }
}
