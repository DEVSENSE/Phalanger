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
// This library provides various services to loaded PHP extensions.
//
// ExtSupport.cpp 
// - contains definition of StartupHelper class
//

#include "stdafx.h"
#include "ExtSupport.h"
#include "RemoteDispatcher.h"
#include "TsrmLs.h"
#include "Streams.h"

#if defined(PHP5TS)
#include "io.h"
#endif // defined(PHP5TS)

using namespace System;
using namespace System::IO;

using namespace PHP::ExtManager;


#pragma unmanaged

// pointers to CRT functions in msvcrt.dll (C Runtime 6)
CRTX_FUNCTION_DECL(close);
CRTX_FUNCTION_DECL(creat);
CRTX_FUNCTION_DECL(errno);
CRTX_FUNCTION_DECL(fdopen);
CRTX_FUNCTION_DECL(fileno);
CRTX_FUNCTION_DECL(fstat);
CRTX_FUNCTION_DECL(get_osfhandle);
CRTX_FUNCTION_DECL(lseek);
CRTX_FUNCTION_DECL(open);
CRTX_FUNCTION_DECL(open_osfhandle);
CRTX_FUNCTION_DECL(read);
CRTX_FUNCTION_DECL(write);
CRTX_FUNCTION_DECL(fclose);
CRTX_FUNCTION_DECL(fflush);
CRTX_FUNCTION_DECL(fopen);
CRTX_FUNCTION_DECL(fread);
CRTX_FUNCTION_DECL(fseek);
CRTX_FUNCTION_DECL(ftell);
CRTX_FUNCTION_DECL(fwrite);
CRTX_FUNCTION_DECL(rewind);
CRTX_FUNCTION_DECL(setvbuf);

// If <B>true</B>, ExtSupport is already collocated to current process (possibly to another
// app domain within this process). It is an error to collocate ExtSupport more than once
// to one process.
static bool IsCollocatedInProcess = false;

#pragma managed

// Loads and initializes msvcrt.dll (MS C Runtime 6) that is consumed by extensions.
void StartupHelper::InitializeCRTX()
{

#if defined(PHP4TS)
	if ((msvcrt = ::LoadLibrary(L"msvcrt.dll")) == NULL) throw gcnew System::Exception("Phalanger PHP4 Extension Support: msvcrt.dll could not be loaded.");
#elif defined(PHP5TS)
	//if ((msvcrt = LoadLibraryEx(L"msvcr90.dll", 0, LOAD_WITH_ALTERED_SEARCH_PATH)) == NULL) throw gcnew System::Exception("Phalanger PHP5.3 Extension Support: msvcr90.dll could not be loaded.");
#endif // defined(PHP4TS/PHP5TS)
	
	CRTX_FUNCTION_INIT_(msvcrt, close);
	CRTX_FUNCTION_INIT_(msvcrt, creat);
	CRTX_FUNCTION_INIT_(msvcrt, errno);
	CRTX_FUNCTION_INIT_(msvcrt, fdopen);
	CRTX_FUNCTION_INIT_(msvcrt, fileno);
	CRTX_FUNCTION_INIT_(msvcrt, get_osfhandle);
	CRTX_FUNCTION_INIT_(msvcrt, lseek);
	CRTX_FUNCTION_INIT_(msvcrt, open);
	CRTX_FUNCTION_INIT_(msvcrt, open_osfhandle);
	CRTX_FUNCTION_INIT_(msvcrt, read);
	CRTX_FUNCTION_INIT_(msvcrt, write);
	CRTX_FUNCTION_INIT_(msvcrt, fstat);
	CRTX_FUNCTION_INIT(msvcrt, fclose);
	CRTX_FUNCTION_INIT(msvcrt, fflush);
	CRTX_FUNCTION_INIT(msvcrt, fopen);
	CRTX_FUNCTION_INIT(msvcrt, fread);
	CRTX_FUNCTION_INIT(msvcrt, fseek);
	CRTX_FUNCTION_INIT(msvcrt, ftell);
	CRTX_FUNCTION_INIT(msvcrt, fwrite);
	CRTX_FUNCTION_INIT(msvcrt, rewind);
	CRTX_FUNCTION_INIT(msvcrt, setvbuf);

	Debug::Assert(crtx_close != NULL && crtx_creat != NULL && crtx_errno != NULL && crtx_fdopen != NULL &&
		crtx_fileno != NULL && crtx_fstat != NULL && crtx_get_osfhandle != NULL && crtx_lseek != NULL &&
		crtx_open != NULL && crtx_open_osfhandle != NULL && crtx_read != NULL && crtx_write != NULL &&
		crtx_fclose != NULL && crtx_fflush != NULL && crtx_fopen != NULL && crtx_fread != NULL &&
		crtx_fseek != NULL && crtx_ftell != NULL && crtx_fwrite != NULL && crtx_rewind && crtx_setvbuf != NULL);
}

// Shuts down msvcrt.dll (MS C Runtime 6) that is consumed by extensions.
void StartupHelper::ShutdownCRTX()
{
	if (msvcrt != NULL) FreeLibrary(msvcrt);
}

// Unloads all loaded extensions.
void StartupHelper::UnloadExtensions()
{
	int count = Module::GetModuleCount();
	for (int i = 0; i < count; i++) Module::GetModule(i)->ModuleShutdown();
}

// Initialization method of this assembly to be called from Core when loading local ExtManager.
RemoteDispatcher ^StartupHelper::Collocate()
{
	if (singleton == nullptr) singleton = gcnew StartupHelper();

	//IsCollocated = true;

#if defined(PHP4TS)
	Request::RequestThreadSlotName = "LocalExtManRequest4";
#elif defined(PHP5TS)
	Request::RequestThreadSlotName = "LocalExtManRequest5";
#endif

	// no 'error' message boxes
	SetErrorMode(SEM_NOOPENFILEERRORBOX | SEM_FAILCRITICALERRORS);

	// perform initialization under process-wide lock
	static MUTEX_T mutex = tsrm_mutex_alloc();
	tsrm_mutex_lock(mutex);
	try
	{
		// If ExtSupport has already been collocated to this process, don't perform ExtSupportInit
		if (!IsCollocatedInProcess)
		{
			// initialize winsock
			WORD wVersionRequested = MAKEWORD(2, 0);
			WSADATA wsaData;

			int errCode = WSAStartup(wVersionRequested, &wsaData);
			if (errCode != 0) throw gcnew System::Net::Sockets::SocketException(errCode);

			InitializeCRTX();
			RemoteDispatcher::ExtSupportInit();

			IsCollocatedInProcess = true;
		}
	}
	finally
	{
		tsrm_mutex_unlock(mutex);
	}

	return gcnew RemoteDispatcher();
}
