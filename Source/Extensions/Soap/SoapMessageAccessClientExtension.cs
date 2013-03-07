using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Services.Protocols;
using System.IO;

namespace PHP.Library.Soap
{

        /// <summary>
        /// SOAP extensions that enables to read request and response SOAP messages
        /// </summary>
        public class SoapMessageAccessClientExtension : SoapExtension, IDisposable
        {
            private Stream oldStream;
            private Stream newStream;
            private bool mustStoreSoapMessage;

            /// <summary>
            /// Gets the initializer.
            /// </summary>
            /// <param name="methodInfo">Method info.</param>
            /// <param name="attribute">Attribute.</param>
            /// <returns></returns>
            public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute)
            {
                return null;
            }

            /// <summary>
            /// Gets the initializer.
            /// </summary>
            /// <param name="t">T.</param>
            /// <returns></returns>
            public override object GetInitializer(Type t)
            {
                //return typeof(SoapMessageAccessClientExtension);
                if (t.BaseType == typeof(SoapHttpClientProtocolExtended))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            /// <summary>
            /// Initializes the specified initializer.
            /// </summary>
            /// <param name="initializer">Initializer.</param>
            public override void Initialize(object initializer)
            {
                mustStoreSoapMessage = (bool)initializer;
            }

            /// <summary>
            /// Processs the message.
            /// </summary>
            /// <param name="message">Message.</param>
            public override void ProcessMessage(SoapMessage message)
            {
                switch (message.Stage)
                {
                    case SoapMessageStage.BeforeSerialize:
                        break;

                    case SoapMessageStage.AfterSerialize:
                        StoreRequestMessage(message);
                        // Pass it off as the actual stream
                        //Copy(newStream, oldStream);
                        // Indicate for the return that we don't wish to chain anything in
                        break;

                    case SoapMessageStage.BeforeDeserialize:
                        StoreResponseMessage(message);
                        // Pass it off as the actual stream
                        break;

                    case SoapMessageStage.AfterDeserialize:
                        break;

                    default:
                        throw new ArgumentException("Invalid message stage [" + message.Stage + "]", "message");
                }
            }

            /// <summary>
            /// Chains the stream.
            /// </summary>
            /// <param name="stream">Stream.</param>
            /// <returns></returns>
            public override Stream ChainStream(Stream stream)
            {
                // Store old
                oldStream = stream;
                newStream = new MemoryStream();

                // Return new stream
                return newStream;
            }

            /// <summary>
            /// Stores the request message.
            /// </summary>
            /// <param name="message">Message.</param>
            private void StoreRequestMessage(SoapMessage message)
            {
                // Rewind the source stream
                newStream.Position = 0;

                if (mustStoreSoapMessage)
                {
                    try
                    {
                        // Store message in our slot in the SoapHttpClientProtocol-derived class
                        byte[] bufEncSoap = new Byte[newStream.Length];
                        newStream.Read(bufEncSoap, 0, bufEncSoap.Length);
                        ((SoapHttpClientProtocolExtended)(((SoapClientMessage)message).Client)).SoapRequestInternal = bufEncSoap;
                    }
                    catch (Exception ex)
                    {
                        throw new MessageStorageException("An error occured while trying to access the SOAP stream.", ex);
                    }
                }

                Copy(newStream, oldStream);
            }

            /// <summary>
            /// Stores the response message.
            /// </summary>
            /// <param name="message">Message.</param>
            private void StoreResponseMessage(SoapMessage message)
            {
                Stream tempStream = new MemoryStream();
                Copy(oldStream, tempStream);

                if (mustStoreSoapMessage)
                {
                    try
                    {
                        // Store message in our slot in the SoapHttpClientProtocol-derived class
                        byte[] bufEncSoap = new Byte[tempStream.Length];
                        tempStream.Read(bufEncSoap, 0, bufEncSoap.Length);
                        ((SoapHttpClientProtocolExtended)(((SoapClientMessage)message).Client)).SoapResponseInternal = bufEncSoap;
                    }
                    catch (Exception ex)
                    {
                        throw new MessageStorageException("An error occured while trying to access the SOAP stream.", ex);
                    }
                }

                Copy(tempStream, newStream);
            }

            /// <summary>
            /// Copys the specified from.
            /// </summary>
            /// <param name="from">From.</param>
            /// <param name="to">To.</param>
            private static void Copy(Stream from, Stream to)
            {
                if (from.CanSeek == true)
                    from.Position = 0;
                TextReader reader = new StreamReader(from);
                TextWriter writer = new StreamWriter(to);
                writer.WriteLine(reader.ReadToEnd());
                writer.Flush();
                if (to.CanSeek == true)
                    to.Position = 0;
            }

            #region IDisposable Members

            /// <summary>
            /// Disposes this instance.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Disposes the specified disposing.
            /// </summary>
            /// <param name="disposing">Disposing.</param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // Free other state (managed objects)
                }

                // Free your own state (unmanaged objects)
                // Set large fields to null
                if (oldStream != null)
                {
                    oldStream.Close();
                    oldStream = null;
                }

                if (newStream != null)
                {
                    newStream.Close();
                    newStream = null;
                }
            }

            /// <summary>
            /// 'Destruct' the SOAP message access client extension.
            /// </summary>
            ~SoapMessageAccessClientExtension()
            {
                // Simply call Dispose(false)
                Dispose(false);
            }

            #endregion
        }
    }

