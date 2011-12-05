/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// StreamProxy.cpp
// - contains definition of StreamWrapperProxy class
// - contains definition of StreamProxy class
//

#include "stdafx.h"

#include "StreamProxy.h"
#include "StreamFopenWrappers.h"
#include "Streams.h"
#include "TsrmLs.h"
#include "Errors.h"
#include "Hash.h"
#include "VirtualWorkingDir.h"

using namespace System;
using namespace System::Runtime::InteropServices;

using namespace PHP::Core;
using namespace PHP::Library;

namespace PHP
{
	namespace ExtManager
	{
		/// <summary>
		/// Just a holder for a static field (global <c>__gc</c> variables are not allowed).
		/// </summary>
		private ref class StringTerminationHolder
		{
		public:
			/// <summary>A <see cref="String"/> of length 1 containing the null character.</summary>
			static String ^StringTermination = gcnew String("", 0, 1);
		};
	}
}

// Provides a transient address of a readonly native ANSI representation of the given managed string.
#define PEEP_NATIVE_STRING(mng_string, ntv_string, code)															\
			array<unsigned char> ^_bytes = Request::AppConf->Globalization->PageEncoding->GetBytes					\
				(String::Concat(mng_string, StringTerminationHolder::StringTermination));							\
			/* append \0 - means extra GC allocation, but it is surely cheaper than the unmanaged allocation */		\
			/* would be if \0 was appended to the char* after conversion */											\
																													\
			GCHandle _handle = GCHandle::Alloc(_bytes, GCHandleType::Pinned);										\
			try																										\
			{																										\
				char *ntv_string = (char *)Marshal::UnsafeAddrOfPinnedArrayElement(_bytes, 0).ToPointer();			\
				code;																								\
			}																										\
			finally																								\
			{																										\
				_handle.Free();																						\
			}


namespace PHP
{
	namespace ExtManager
	{
		// StreamWrapperProxy implementation:

		// Creates a new <see cref="StreamWrapperProxy"/> for the given scheme portion of a URL.
		StreamWrapperProxy ^StreamWrapperProxy::CreateWrapperProxy(String ^scheme)
		{
			TSRMLS_FETCH();
			HashTable *wrapper_table = php_stream_get_url_stream_wrappers_hash();

			if (wrapper_table == NULL) return nullptr;

			// lookup the native wrapper
			php_stream_wrapper *wrapper;
			PEEP_NATIVE_STRING(scheme, scheme_unmng,
			{
				if (zend_hash_find(wrapper_table, scheme_unmng, scheme->Length, (void **)&wrapper) == FAILURE) return nullptr;
			});

			// a wrapper was found
            return gcnew StreamWrapperProxy(wrapper, static_cast<Module ^>(streamWrapperModules[scheme]));
		}

		// Returns an <see cref="ICollection"/> of schemes of all registered stream wrappers.
		ICollection ^StreamWrapperProxy::GetWrapperSchemes()
		{
			array<String ^> ^arr = gcnew array<String ^>(streamWrapperModules->Count);
			streamWrapperModules->Keys->CopyTo(arr, 0);

			return static_cast<ICollection ^>(arr);
		}

		// Updates <see cref="streamWrapperModules"/>.
		void StreamWrapperProxy::RegisterWrapper(String ^scheme)
		{
			streamWrapperModules->Add(scheme, Module::GetCurrentModule());
		}

		// Updates <see cref="streamWrapperModules"/>.
		void StreamWrapperProxy::UnregisterWrapper(String ^scheme)
		{
			streamWrapperModules->Remove(scheme);
		}

		// Creates a new <c>php_stream</c> and returns a managed proxy to it.
		// (partially copied from _php_stream_open_wrapper_ex() in streams.c)
		IExternalStream ^StreamWrapperProxy::Open(String ^path, String ^mode, int options,
			String ^%opened_path, Object ^context)
		{
			Request ^request = Request::EnsureRequestExists();

			TSRMLS_FETCH();
			if (wrapper->wops->stream_opener == NULL)
			{
				PhpException::Throw(PhpError::Warning, LibResources::GetString("open_wrapper_op_unsupported"));
				return nullptr;
			}

			Module ^old_module = request->CurrentInvocationContext.Module;
			request->CurrentInvocationContext.Module = containingModule;

			try
			{
				opened_path = nullptr;
				char *opened_path_unmng = NULL;
				php_stream *stream;
				PEEP_NATIVE_STRING(path, path_unmng,
				{
					PEEP_NATIVE_STRING(mode, mode_unmng,
					{
						wrapper->err_count = 0;
						wrapper->err_stack = NULL;

						stream = wrapper->wops->stream_opener(wrapper, path_unmng, mode_unmng, options/* ^ REPORT_ERRORS*/,
							&opened_path_unmng, NULL TSRMLS_CC);
						if (stream == NULL) return nullptr;
					});
				});
				stream->wrapper = wrapper;

				try
				{
					// if the caller asked for a persistent stream but the wrapper did not return one, force an error here
					if ((options & STREAM_OPEN_PERSISTENT) && !stream->is_persistent)
					{
						php_stream_wrapper_log_error(wrapper, options/* ^ REPORT_ERRORS*/ TSRMLS_CC,
							"wrapper does not support persistent streams");
						php_stream_close(stream);
						return nullptr;
					}

					// make the stream seekable if requested
					if (stream != NULL && (options & STREAM_MUST_SEEK))
					{
						php_stream *newstream;

						switch (php_stream_make_seekable_rel(stream, &newstream, (options & STREAM_WILL_CAST)
								? PHP_STREAM_PREFER_STDIO : PHP_STREAM_NO_PREFERENCE))
						{
							case PHP_STREAM_UNCHANGED:   break;
							case PHP_STREAM_RELEASED:    stream = newstream; break;

							default:
								php_stream_close(stream);
								if (options & REPORT_ERRORS)
								{
									PEEP_NATIVE_STRING(path, tmp,
									{
										php_strip_url_passwd(tmp);
										php_error_docref1(NULL TSRMLS_CC, tmp, E_WARNING, "could not make seekable - %s",
												tmp);
										efree(tmp);
									});
									return nullptr;
								}
						}
					}

					// if opened for append, seek to the correct initial file position
					if (stream->ops->seek && (stream->flags & PHP_STREAM_FLAG_NO_SEEK) == 0 && mode->IndexOf('a')
						&& stream->position == 0)
					{
						off_t newpos = 0;

						if (0 == stream->ops->seek(stream, 0, SEEK_CUR, &newpos TSRMLS_CC))
						{
							stream->position = newpos;
						}
					}

					if (opened_path_unmng != NULL)
					{
						opened_path = gcnew String(opened_path_unmng, 0, strlen(opened_path_unmng),
							Request::AppConf->Globalization->PageEncoding);
					}

					// return a new proxy to the native stream
					return gcnew StreamProxy(stream, containingModule);
				}
				finally
				{
					if (opened_path_unmng != NULL) efree(opened_path_unmng);
				}
			}
			finally
			{
				request->CurrentInvocationContext.Module = old_module;
			}
		}

		// Stat wrapper operation implementation.
		StatStruct StreamWrapperProxy::Stat(String ^path, int options, Object ^context, bool streamStat)
		{
			StatStruct stat_struct;
			Request ^request = Request::EnsureRequestExists();

			TSRMLS_FETCH();
			if (wrapper->wops->url_stat == NULL)	// TODO: call stream_stat() if streamStat is TRUE
			{
				PhpException::Throw(PhpError::Warning, LibResources::GetString("stat_wrapper_op_unsupported"));
				stat_struct.st_size = -1;
				return stat_struct;
			}

			// invoke the "stater"
			php_stream_statbuf stat_ssb;

			PEEP_NATIVE_STRING(path, path_unmng,
			{
				Module ^old_module = request->CurrentInvocationContext.Module;
				request->CurrentInvocationContext.Module = containingModule;

				try
				{
					if (wrapper->wops->url_stat(wrapper, path_unmng, &stat_ssb TSRMLS_CC) != 0) return stat_struct;
				}
				finally
				{
					request->CurrentInvocationContext.Module = old_module;
				}
			});

			Marshal::PtrToStructure(IntPtr(&stat_ssb.sb), stat_struct);
			return stat_struct;
		}

		// Unlink wrapper operation implementation.
		bool StreamWrapperProxy::Unlink(String ^path, int options, Object ^context)
		{
			PhpException::Throw(PhpError::Warning, LibResources::GetString("unlink_wrapper_op_unsupported"));
			return false;
		}

		// Listing wrapper operation implementation.
		array<String ^> ^StreamWrapperProxy::Listing(String ^path, int options, Object ^context)
		{
			Request ^request = Request::EnsureRequestExists();

			TSRMLS_FETCH();
			if (wrapper->wops->dir_opener == NULL)
			{
				PhpException::Throw(PhpError::Warning, LibResources::GetString("open_dir_wrapper_op_unsupported"));
				return nullptr;
			}

			// invoke the directory opener
			Module ^old_module = request->CurrentInvocationContext.Module;
			request->CurrentInvocationContext.Module = containingModule;

			try
			{
				php_stream *stream;
				PEEP_NATIVE_STRING(path, path_unmng,
				{
					stream = wrapper->wops->dir_opener(wrapper, path_unmng, "r", options/* ^ REPORT_ERRORS*/,
							NULL, NULL TSRMLS_CC);

					if (stream == NULL) return nullptr;
				});

				stream->wrapper = wrapper;
				stream->flags |= PHP_STREAM_FLAG_NO_BUFFER;

				// read the stream
				ArrayList ^result = gcnew ArrayList();
				php_stream_dirent entry;

				while (php_stream_readdir(stream, &entry))
				{
					result->Add(gcnew String(entry.d_name, 0, strlen(entry.d_name),
						Request::AppConf->Globalization->PageEncoding));
				}

				// close the stream
				if (wrapper->wops->stream_closer != NULL) wrapper->wops->stream_closer(wrapper, stream TSRMLS_CC);
				return dynamic_cast<array<String ^> ^>(result->ToArray(String::typeid));
			}
			finally
			{
				request->CurrentInvocationContext.Module = old_module;
			}
		}

		// Rename wrapper operation implementation.
		bool StreamWrapperProxy::Rename(String ^fromPath, String ^toPath, int options, Object ^context)
		{
			PhpException::Throw(PhpError::Warning, LibResources::GetString("rename_wrapper_op_unsupported"));
			return false;
		}
	
		// Make directory wrapper operation implementation.
		bool StreamWrapperProxy::MakeDirectory(String ^path, int accessMode, int options, Object ^context)
		{
			PhpException::Throw(PhpError::Warning, LibResources::GetString("mkdir_wrapper_op_unsupported"));
			return false;
		}

		// Remove directory wrapper operation implementation.
		bool StreamWrapperProxy::RemoveDirectory(String ^path, int options, Object ^context)
		{
			PhpException::Throw(PhpError::Warning, LibResources::GetString("rmdir_wrapper_op_unsupported"));
			return false;
		}

		// Returns label of this stream wrapper.
		String ^StreamWrapperProxy::Label::get()
		{
			if (wrapper->wops->label == NULL) return nullptr;
			return gcnew String(wrapper->wops->label, 0, strlen(wrapper->wops->label),
				Request::AppConf->Globalization->PageEncoding);
		}

		// Returns the is_url flag of this stream wrapper.
		bool StreamWrapperProxy::IsUrl::get()
		{
			return (wrapper->is_url ? true : false);
		}

		// StreamProxy implementation:

		// Write data from a buffer to the stream.
		int StreamProxy::Write(array<unsigned char> ^buffer, int offset, int count)
		{
			if (stream == NULL) throw gcnew InvalidOperationException(ExtResources::GetString("native_stream_already_freed"));

			Request ^request = Request::EnsureRequestExists();

			TSRMLS_FETCH();
			GCHandle handle = GCHandle::Alloc(buffer, GCHandleType::Pinned);
			try
			{
				Module ^old_module = request->CurrentInvocationContext.Module;
				request->CurrentInvocationContext.Module = containingModule;

				try
				{
					return php_stream_write(stream,
						(char *)Marshal::UnsafeAddrOfPinnedArrayElement(buffer, offset).ToPointer(), count);
				}
				finally
				{
					request->CurrentInvocationContext.Module = old_module;
				}
			}
			finally
			{
				handle.Free();
			}
		}

		// Reads data from the stream to a buffer.
		int StreamProxy::Read(array<unsigned char> ^%buffer, int offset, int count)
		{
			if (stream == NULL) throw gcnew InvalidOperationException(ExtResources::GetString("native_stream_already_freed"));

			Request ^request = Request::EnsureRequestExists();

			TSRMLS_FETCH();

			GCHandle handle = GCHandle::Alloc(buffer, GCHandleType::Pinned);
			try
			{
				Module ^old_module = request->CurrentInvocationContext.Module;
				request->CurrentInvocationContext.Module = containingModule;

				try
				{
					return php_stream_read(stream, (char *)Marshal::UnsafeAddrOfPinnedArrayElement(buffer, offset).ToPointer(),
						count);
				}
				finally
				{
					request->CurrentInvocationContext.Module = old_module;
				}
			}
			finally
			{
				handle.Free();
			}
		}

		// Closes the stream.
		bool StreamProxy::Close()
		{
			if (stream == NULL) throw gcnew InvalidOperationException(ExtResources::GetString("native_stream_already_freed"));

			Request ^request = Request::EnsureRequestExists();

			Module ^old_module = request->CurrentInvocationContext.Module;
			request->CurrentInvocationContext.Module = containingModule;

			try
			{
				TSRMLS_FETCH();
				if (php_stream_close(stream) == 0)
				{
					stream = NULL;
					request->DeregisterLifetimeBoundMBR(this);
					isExpired = true;
					return true;
				}
				else return false;
			}
			finally
			{
				request->CurrentInvocationContext.Module = old_module;
			}
		}

		// Flushes the stream.
		bool StreamProxy::Flush()
		{
			if (stream == NULL) throw gcnew InvalidOperationException(ExtResources::GetString("native_stream_already_freed"));

			Request ^request = Request::EnsureRequestExists();

			Module ^old_module = request->CurrentInvocationContext.Module;
			request->CurrentInvocationContext.Module = containingModule;

			try
			{
				TSRMLS_FETCH();
				return (php_stream_flush(stream) == 0);
			}
			finally
			{
				request->CurrentInvocationContext.Module = old_module;
			}
		}

		// Sets new position in the stream.
		bool StreamProxy::Seek(int offset, SeekOrigin whence)
		{
			if (stream == NULL) throw gcnew InvalidOperationException(ExtResources::GetString("native_stream_already_freed"));

			Request ^request = Request::EnsureRequestExists();

			Module ^old_module = request->CurrentInvocationContext.Module;
			request->CurrentInvocationContext.Module = containingModule;

			try
			{
				TSRMLS_FETCH();
				return (php_stream_seek(stream, offset, (int)whence) == 0);
			}
			finally
			{
				request->CurrentInvocationContext.Module = old_module;
			}
		}

		// Gets the current position in the stream.
		int StreamProxy::Tell()
		{
			if (stream == NULL) throw gcnew InvalidOperationException(ExtResources::GetString("native_stream_already_freed"));
			return stream->position;
		}

		// Determines whether this stream has reached its end.
		bool StreamProxy::Eof()
		{
			if (stream == NULL) throw gcnew InvalidOperationException(ExtResources::GetString("native_stream_already_freed"));

			Request ^request = Request::EnsureRequestExists();

			Module ^old_module = request->CurrentInvocationContext.Module;
			request->CurrentInvocationContext.Module = containingModule;

			try
			{
				TSRMLS_FETCH();
				return (php_stream_eof(stream) != 0);
			}
			finally
			{
				request->CurrentInvocationContext.Module = old_module;
			}
		}

		// Returns the stat structure for the stram.
		StatStruct StreamProxy::Stat()
		{
			StatStruct stat_struct;

			if (stream == NULL) throw gcnew InvalidOperationException(ExtResources::GetString("native_stream_already_freed"));

			Request ^request = Request::EnsureRequestExists();

			TSRMLS_FETCH();
			if (stream->ops->stat == NULL)
			{
				stat_struct.st_size = -1;
				return stat_struct;
			}

			// invoke the "stater"
			php_stream_statbuf stat_ssb;

			Module ^old_module = request->CurrentInvocationContext.Module;
			request->CurrentInvocationContext.Module = containingModule;

			try
			{
				if (stream->ops->stat(stream, &stat_ssb TSRMLS_CC) != 0)
				{
					stat_struct.st_size = -1;
					return stat_struct;
				}
			}
			finally
			{
				request->CurrentInvocationContext.Module = old_module;
			}
			
			Marshal::PtrToStructure(IntPtr(&stat_ssb.sb), stat_struct);
			return stat_struct;
		}
	}
}
