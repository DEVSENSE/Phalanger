//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// ExtSupport.h 
// - contains declaration of StartupHelper class
// - contains declaration of AssemblyResolver class
//

#pragma once

#include "stdafx.h"
#include "PhpMarshaler.h"

// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the ZEND_EXPORTS
// symbol defined on the command line. this symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// ZEND_API functions as being imported from a DLL, wheras this DLL sees symbols defined 
// with this macro as being exported.

#ifdef ZEND_EXPORTS
#define ZEND_API __declspec(dllexport)
#else
#define ZEND_API __declspec(dllimport)
#endif

using namespace System;
using namespace System::IO;
using namespace System::Collections;

using namespace PHP::Core;

// pointers to CRT functions in msvcrt.dll (C Runtime 6)
#define CRTX_FUNCTION_EXTERN(function, ret_type, arguments) \
	typedef ret_type (*function##_proto)##arguments; \
	extern function##_proto crtx_##function

#define CRTX_FUNCTION_DECL(function) \
	function##_proto crtx_##function

#if defined(PHP4TS)
#define CRTX_FUNCTION_INIT(module, function) \
	crtx_##function = (function##_proto)::GetProcAddress(module, #function)

#define CRTX_FUNCTION_INIT_(module, function) \
	crtx_##function = (function##_proto)::GetProcAddress(module, "_" #function)
#elif defined(PHP5TS) //&& _MSC_VER == 1500
#define CRTX_FUNCTION_INIT(module, function) \
	crtx_##function = function

#define CRTX_FUNCTION_INIT_(module, function) \
	crtx_##function = (function##_proto)_##function	// casting just because of _fstat function, to be compatible with msvcrt and msvcr90 at once
#else // defined(PHP4TS/PHP5TS)
#error Extension Support have to load MSVCR90.dll.
#endif

CRTX_FUNCTION_EXTERN(close, int, (int));
CRTX_FUNCTION_EXTERN(creat, int, (const char *, int));
CRTX_FUNCTION_EXTERN(errno, int *, (void));
CRTX_FUNCTION_EXTERN(fdopen, FILE *, (int, const char *));
CRTX_FUNCTION_EXTERN(fileno, int, (FILE *));
CRTX_FUNCTION_EXTERN(fstat, int, (int, struct stat *));
CRTX_FUNCTION_EXTERN(get_osfhandle, intptr_t, (int));
CRTX_FUNCTION_EXTERN(lseek, long, (int, long, int));
CRTX_FUNCTION_EXTERN(open, int, (const char *, int, int));
CRTX_FUNCTION_EXTERN(open_osfhandle, int, (intptr_t, int));
CRTX_FUNCTION_EXTERN(read, int, (int, void *, unsigned int));
CRTX_FUNCTION_EXTERN(write, int, (int, const void *, unsigned int));
CRTX_FUNCTION_EXTERN(fclose, int, (FILE *));
CRTX_FUNCTION_EXTERN(fflush, int, (FILE *));
CRTX_FUNCTION_EXTERN(fopen, FILE *, (const char *, const char *));
CRTX_FUNCTION_EXTERN(fread, size_t, (void *, size_t, size_t, FILE *));
CRTX_FUNCTION_EXTERN(fseek, int, (FILE *, long, int));
CRTX_FUNCTION_EXTERN(ftell, long, (FILE *));
CRTX_FUNCTION_EXTERN(fwrite, size_t, (const void *, size_t, size_t, FILE *));
CRTX_FUNCTION_EXTERN(rewind, void, (FILE *));
CRTX_FUNCTION_EXTERN(setvbuf, int, (FILE *, char *, int, size_t));

namespace PHP
{
	namespace ExtManager
	{
		ref class RemoteDispatcher;

		/// <summary>
		/// Contains a method that hooks the <see cref="AppDomain.AssemblyResolve"/> event.
		/// </summary>
		private ref class AssemblyResolver
		{
		public:
			/// <summary>
			/// Occurs when the resolution of an assembly fails.
			/// </summary>
			/// <param name="sender">The source of the event.</param>
			/// <param name="args">A <see cref="ResolveEventArgs"/> that contains the event data.</param>
			/// <returns>The <see cref="Assembly"/> that resolves the assembly.</returns>
			static System::Reflection::Assembly ^AssemblyResolveEventHandler(Object ^sender, ResolveEventArgs ^args)
			{
#ifdef DEBUG
				Debug::WriteLine("EXT SUP", String::Concat("Resolving assembly: ", args->Name));
#endif

				array<System::Reflection::Assembly ^> ^asses = AppDomain::CurrentDomain->GetAssemblies();
				for (int i = 0; i < asses->Length; i++)
				{
					if (asses[i]->FullName->Equals(args->Name)) return asses[i];
				}
				
				return nullptr;
			}
		};

		/// <summary>
		/// Contains methods that support <c>ExtManager</c> startup.
		/// </summary>
		public ref class StartupHelper
		{
		// Static members
		public:
			static StartupHelper()
			{
				// hook AssemblyResolve event (since Assembly::Load does not search in loaded assemblies)
				AppDomain::CurrentDomain->AssemblyResolve += gcnew ResolveEventHandler(&AssemblyResolver::AssemblyResolveEventHandler);
			}

			/// <summary>
			/// Loads and initializes msvcrt.dll (MS C Runtime 6) that is consumed by extensions.
			/// </summary>
			static void InitializeCRTX();

			/// <summary>
			/// Shuts down msvcrt.dll (MS C Runtime 6) that is consumed by extensions.
			/// </summary>
			static void ShutdownCRTX();

			/// <summary>
			/// Unloads all extensions.
			/// </summary>
			static void UnloadExtensions();

			/// <summary>
			/// Initialization method of this <see cref="Assembly"/> when extensions are configured as
			/// collocated.
			/// </summary>
			/// <returns>An instance of <see cref="RemoteDispatcher"/> implementing the <see cref="IExternals"/>
			/// interface.</returns>
			static RemoteDispatcher ^Collocate();

			/*///<summary>
			/// <B>true</B> if this <c>ExtManager</c> is collocated with <c>Core</c>, <B>false</B> otherwise.
			///</summary>
			static bool IsCollocated = false;*/

			/// <summary>
			/// List of <see cref="String"/>s - errors that occurred during <see cref="LoadExtensions"/>.
			/// </summary>
			/// <remarks>
			/// These errors are thrown whenever a new <see cref="Request"/> is created.
			/// </remarks>
			static ArrayList ^StartupErrors = gcnew ArrayList();

		private:
			/// <summary>Private singleton whose finalization detaches from C runtime libraries.</summary>
			static StartupHelper ^singleton = nullptr;

			/// <summary>Handle to msvcrt.dll (C Runtime 6) module.</summary>
			static HMODULE msvcrt;
		};
	}
}
