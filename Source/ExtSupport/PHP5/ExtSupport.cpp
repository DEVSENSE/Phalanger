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

//#include "stdafx.h"
//#include "API/zend/zend.h"
//#include "API/zend/zend_constants.h"
//#include "API/zend/zend_API.h"
//#include "API/main/php.h"
#include "ExtSupport.h"
#include "RemoteDispatcher.h"
//#include "TsrmLs.h"
//#include "Streams.h"

using namespace System;
using namespace System::IO;

using namespace PHP::ExtManager;


#pragma unmanaged

// pointers to CRT functions in msvcrt.dll (C Runtime 6)
CRT6_FUNCTION_DECL(close);
CRT6_FUNCTION_DECL(creat);
CRT6_FUNCTION_DECL(errno);
CRT6_FUNCTION_DECL(fdopen);
CRT6_FUNCTION_DECL(fileno);
CRT6_FUNCTION_DECL(fstat);
CRT6_FUNCTION_DECL(get_osfhandle);
CRT6_FUNCTION_DECL(lseek);
CRT6_FUNCTION_DECL(open);
CRT6_FUNCTION_DECL(open_osfhandle);
CRT6_FUNCTION_DECL(read);
CRT6_FUNCTION_DECL(write);
CRT6_FUNCTION_DECL(fclose);
CRT6_FUNCTION_DECL(fflush);
CRT6_FUNCTION_DECL(fopen);
CRT6_FUNCTION_DECL(fread);
CRT6_FUNCTION_DECL(fseek);
CRT6_FUNCTION_DECL(ftell);
CRT6_FUNCTION_DECL(fwrite);
CRT6_FUNCTION_DECL(rewind);
CRT6_FUNCTION_DECL(setvbuf);

// If <B>true</B>, ExtSupport is already collocated to current process (possibly to another
// app domain within this process). It is an error to collocate ExtSupport more than once
// to one process.
static bool IsCollocatedInProcess = false;

#pragma managed

// Loads and initializes msvcrt.dll (MS C Runtime 6) that is consumed by extensions.
void StartupHelper::InitializeCRT6()
{
	HMODULE mod;

	msvcrt = ::LoadLibrary(L"msvcrt.dll");
	if (msvcrt == NULL)
	{
#ifdef DEBUG
		Debug::WriteLine("EXT SUP", "msvcrt.dll is not available, mapping msvcr80.dll instead");
#endif

		mod = ::GetModuleHandle(L"msvcr80.dll");
		Debug::Assert(mod != NULL, "Could not map msvcr80.dll");
	}
	else mod = msvcrt;

	CRT6_FUNCTION_INIT_(mod, close);
	CRT6_FUNCTION_INIT_(mod, creat);
	CRT6_FUNCTION_INIT_(mod, errno);
	CRT6_FUNCTION_INIT_(mod, fdopen);
	CRT6_FUNCTION_INIT_(mod, fileno);
	CRT6_FUNCTION_INIT_(mod, fstat);
	CRT6_FUNCTION_INIT_(mod, get_osfhandle);
	CRT6_FUNCTION_INIT_(mod, lseek);
	CRT6_FUNCTION_INIT_(mod, open);
	CRT6_FUNCTION_INIT_(mod, open_osfhandle);
	CRT6_FUNCTION_INIT_(mod, read);
	CRT6_FUNCTION_INIT_(mod, write);
	CRT6_FUNCTION_INIT(mod, fclose);
	CRT6_FUNCTION_INIT(mod, fflush);
	CRT6_FUNCTION_INIT(mod, fopen);
	CRT6_FUNCTION_INIT(mod, fread);
	CRT6_FUNCTION_INIT(mod, fseek);
	CRT6_FUNCTION_INIT(mod, ftell);
	CRT6_FUNCTION_INIT(mod, fwrite);
	CRT6_FUNCTION_INIT(mod, rewind);
	CRT6_FUNCTION_INIT(mod, setvbuf);

	Debug::Assert(crt6_close != NULL && crt6_creat != NULL && crt6_errno != NULL && crt6_fdopen != NULL &&
		crt6_fileno != NULL && crt6_fstat != NULL && crt6_get_osfhandle != NULL && crt6_lseek != NULL &&
		crt6_open != NULL && crt6_open_osfhandle != NULL && crt6_read != NULL && crt6_write != NULL &&
		crt6_fclose != NULL && crt6_fflush != NULL && crt6_fopen != NULL && crt6_fread != NULL &&
		crt6_fseek != NULL && crt6_ftell != NULL && crt6_fwrite != NULL && crt6_rewind && crt6_setvbuf != NULL);
}

// Shuts down msvcrt.dll (MS C Runtime 6) that is consumed by extensions.
void StartupHelper::ShutdownCRT6()
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

	IsCollocated = true;

	Request::RequestThreadSlotName = "LocalExtManRequest5";

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

			InitializeCRT6();
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
