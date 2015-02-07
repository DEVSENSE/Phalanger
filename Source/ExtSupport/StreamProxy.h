//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// StreamProxy.h
// - contains declaration of StreamWrapperProxy class
// - contains declaration of StreamProxy class
//

#pragma once

#include "stdafx.h"

#include "Streams.h"
#include "Request.h"
#include "Module.h"

#undef RemoveDirectory // grrr...

using namespace System;
using namespace System::IO;

using namespace PHP::Core;

/*

  Designed and implemented by Ladislav Prosek.
  
*/

namespace PHP
{
	namespace ExtManager
	{
		/// <summary>
		/// Class of remotable, marshaled by reference objects that serve as proxies of native PHP stream wrappers.
		/// </summary>
		private ref class StreamWrapperProxy : public MarshalByRefObject, public ISponsor, public ILifetimeBoundMBR,
												public IExternalStreamWrapper
		{
		private:
			/// <summary>
			/// Creates a new <see cref="StreamWrapperProxy"/>.
			/// </summary>
			/// <param name="wrapper">The underlying native PHP stream wrapper.</param>
			/// <remarks>
			/// This constructor is private. Use <see cref="CreateWrapperProxy"/> factory method to
			/// obtain an instance.
			/// </remarks>
			StreamWrapperProxy(php_stream_wrapper *wrapper, Module ^containingModule)
			{
				this->wrapper = wrapper;
				this->containingModule = containingModule;
				this->isExpired = false;
				Request::GetCurrentRequest()->RegisterLifetimeBoundMBR(this);
			}
			
			/// <summary>Pointer to the underlying native PHP stream wrapper.</summary>
			php_stream_wrapper *wrapper;

			/// <summary>The <see cref="Module"/> that registered this stream wrapper.</summary>
			Module ^containingModule;

			/// <summary>
			/// <B>true</B> if this instance has expired and should no longer be retained, <B>false</B>
			/// otherwise.
			/// </summary>
			bool isExpired;

		public:
			/// <summary>
			/// Obtains a lifetime service object to control the lifetime policy for this instance.
			/// </summary>
			/// <returns>An object of type <see cref="ILease"/> used to control the lifetime policy for this
			/// instance.</returns>
			/// <remarks>
			/// <see cref="MarshalByRefObject.InitializeLifetimeService"/> is overriden in order to be able to
			/// register a sponsor. This very object becomes its own sponsor. <seealso cref="ISponsor"/>
			/// </remarks>
			virtual Object ^InitializeLifetimeService() override
			{
				ILease ^lease = static_cast<ILease ^>(MarshalByRefObject::InitializeLifetimeService());
				lease->Register(this);
				return lease;
			}

			// ISponsor implementation

			/// <summary>
			/// Requests a sponsoring client to renew the lease for this object.
			/// </summary>
			/// <param name="lease">The lifetime lease of the object that requires lease renewal.</param>
			/// <returns>The additional lease time for the specified object.</returns>
			virtual TimeSpan Renewal(ILease ^lease)
			{
#ifdef DEBUG
				Debug::WriteLine("EXT SUP", "StreamWrapperProxy::Renewal");
#endif

				if (isExpired) return TimeSpan::Zero;
				else return lease->RenewOnCallTime;
			}

			// ILifetimeBoundMBR implementation

			// Invoked by the <see cref="Request"/> this instance is bound to, to signalize that
			// the lifetime expires.
			virtual void Expire()
			{
				isExpired = true;
			}

			// IExternalStreamWrapper implementation

			/// <summary>
			/// Returns an <see cref="IExternalStream"/> proxy of a native PHP stream.
			/// </summary>
			/// <param name="path">The file path passed to <c>fopen</c> PHP function.</param>
			/// <param name="mode">The mode passed to <c>fopen</c> PHP function.</param>
			/// <param name="options">Combination of <c>StreamWrapper.StreamOpenFlags</c>.</param>
			/// <param name="opened_path">The real full path of the file actually opened.</param>
			/// <param name="context">The context provided for the stream wrapper at the call to <c>fopen</c>
			/// PHP function.</param>
			/// <returns>
			/// A new <see cref="MarshalByRefObject"/> implementing the <see cref="IExternalStream"/> interface
			/// or <B>null</B> if there was an error.
			/// </returns>
			virtual IExternalStream ^Open(String ^path, String ^mode, int options, String ^%opened_path,
				Object ^context);

			/// <include file='../Core/Doc/Wrappers.xml' path='docs/method[@name="Stat"]/*'/>
			virtual StatStruct Stat(String ^path, int options, Object ^context, bool streamStat);

			/// <include file='../Core/Doc/Wrappers.xml' path='docs/method[@name="Unlink"]/*'/>
			virtual bool Unlink(String ^path, int options, Object ^context);

			/// <include file='../Core/Doc/Wrappers.xml' path='docs/method[@name="Listing"]/*'/>
			virtual array<String ^> ^Listing(String ^path, int options, Object ^context);
		
			/// <include file='../Core/Doc/Wrappers.xml' path='docs/method[@name="Rename"]/*'/>
			virtual bool Rename(String ^fromPath, String ^toPath, int options, Object ^context);
		
			/// <include file='../Core/Doc/Wrappers.xml' path='docs/method[@name="MakeDirectory"]/*'/>
			virtual bool MakeDirectory(String ^path, int accessMode, int options, Object ^context);

			/// <include file='../Core/Doc/Wrappers.xml' path='docs/method[@name="RemoveDirectory"]/*'/>
			virtual bool RemoveDirectory(String ^path, int options, Object ^context);

			/// <include file='../Core/Doc/Wrappers.xml' path='docs/property[@name="Label"]/*'/>
			virtual property String ^Label
			{
				String ^get();
			}

			/// <include file='../Core/Doc/Wrappers.xml' path='docs/property[@name="IsUrl"]/*'/>
			virtual property bool IsUrl
			{
				bool get();
			}

		public:
			/// <summary>
			/// Creates a new <see cref="StreamWrapperProxy"/> for the given scheme portion of a URL.
			/// </summary>
			/// <param name="scheme">The scheme for which a wrapper is requested.</param>
			/// <returns>The <see cref="StreamWrapperProxy"/> or <B>null</B> if no wrapper was found for the
			/// given scheme.</returns>
			static StreamWrapperProxy ^CreateWrapperProxy(String ^scheme);

			/// <summary>
			/// Returns an <see cref="ICollection"/> of schemes of all registered stream wrappers.
			/// </summary>
			/// <returns>The <see cref="ICollection"/>.</returns>
			static ICollection ^GetWrapperSchemes();

			/// <summary>
			/// Updates <see cref="streamWrapperModules"/>.
			/// </summary>
			/// <param name="scheme">The scheme of the wrapper being registered.</param>
			static void RegisterWrapper(String ^scheme);

			/// <summary>
			/// Updates <see cref="streamWrapperModules"/>.
			/// </summary>
			/// <param name="scheme">The scheme of the wrapper being unregistered.</param>
			static void UnregisterWrapper(String ^scheme);

		private:
			/// <summary>
			/// Keys are wrapper schemes, values are <see cref="Module"/>s that registered the wrapper.
			/// </summary>
			static Hashtable ^streamWrapperModules = gcnew Hashtable();
		};

		/// <summary>
		/// Class of remotable, marshaled by reference objects that serve as proxies of native PHP stream.
		/// </summary>
		private ref class StreamProxy : public MarshalByRefObject, public ISponsor, public ILifetimeBoundMBR,
										public IExternalStream
		{
		public:
			/// <summary>
			/// Creates a new <see cref="StreamProxy"/>.
			/// </summary>
			/// <param name="stream">The underlying native PHP stream.</param>
			/// <remarks>
			/// Use <see cref="StreamWrapperProxy.Open"/> factory method to obtain an instance.
			/// </remarks>
			StreamProxy(php_stream *stream, Module ^containingModule)
			{
				this->stream = stream;
				this->containingModule = containingModule;
				this->isExpired = false;
				Request::GetCurrentRequest()->RegisterLifetimeBoundMBR(this);
			}

			/// <summary>
			/// Obtains a lifetime service object to control the lifetime policy for this instance.
			/// </summary>
			/// <returns>An object of type <see cref="ILease"/> used to control the lifetime policy for this
			/// instance.</returns>
			/// <remarks>
			/// <see cref="MarshalByRefObject.InitializeLifetimeService"/> is overriden in order to be able to
			/// register a sponsor. This very object becomes its own sponsor. <seealso cref="ISponsor"/>
			/// </remarks>
			virtual Object ^InitializeLifetimeService() override
			{
				ILease ^lease = static_cast<ILease ^>(MarshalByRefObject::InitializeLifetimeService());
				lease->Register(this);
				return lease;
			}

			// ISponsor implementation

			/// <summary>
			/// Requests a sponsoring client to renew the lease for this object.
			/// </summary>
			/// <param name="lease">The lifetime lease of the object that requires lease renewal.</param>
			/// <returns>The additional lease time for the specified object.</returns>
			virtual TimeSpan Renewal(ILease ^lease)
			{
#ifdef DEBUG
				Debug::WriteLine("EXT SUP", "StreamProxy::Renewal");
#endif

				if (isExpired) return TimeSpan::Zero;
				else return lease->RenewOnCallTime;
			}

			// ILifetimeBoundMBR implementation

			// Invoked by the <see cref="Request"/> this instance is bound to, to signalize that
			// the lifetime expires.
			virtual void Expire()
			{
				Close();
				isExpired = true;
			}

			// IExternalStream implementation

			/// <include file='../ClassLibrary/Doc/Streams.xml' path='/docs/method[@name="RawWrite"]/*'/>
			virtual int Write(array<unsigned char> ^buffer, int offset, int count);

			/// <include file='../ClassLibrary/Doc/Streams.xml' path='/docs/method[@name="RawRead"]/*'/>
			virtual int Read(array<unsigned char> ^%buffer, int offset, int count);

			/// <summary>
			/// Closes the stream.
			/// </summary>
			/// <returns><B>true</B> on success, <B>false</B> on error.</returns>
			virtual bool Close();

		    /// <include file='../ClassLibrary/Doc/Streams.xml' path='/docs/method[@name="RawFlush"]/*'/>
			virtual bool Flush();

		    /// <include file='../ClassLibrary/Doc/Streams.xml' path=/docs/method[@name="RawSeek"]/*'/>
			virtual bool Seek(int offset, SeekOrigin whence);

			/// <summary>
			/// Gets the current position in the stream.
			/// </summary>
			/// <returns>The position or <c>-1</c> on error.</returns>
			virtual int Tell();

		    /// <include file='../ClassLibrary/Doc/Streams.xml' path='/docs/property[@name="Eof"]/*'/>
			virtual bool Eof();

			/// <summary>	
			/// Returns the stat structure for the stram.
			/// </summary>
			/// <returns>The stat structure describing the stream.</returns>
			virtual StatStruct Stat();

		private:
			/// <summary>Pointer to the underlying native PHP stream.</summary>
			php_stream *stream;

			/// <summary>The <see cref="Module"/> that registered the corresponding stream wrapper.</summary>
			Module ^containingModule;

			/// <summary>
			/// <B>true</B> if this instance has expired and should no longer be retained, <B>false</B>
			/// otherwise.
			/// </summary>
			bool isExpired;
		};
	}
}
