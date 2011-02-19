//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// SharedMem.cpp 
// - contains definitions of shared memory related functions
// - basically an emulation of Unix shared memory API
//

#include "stdafx.h"
#include "SharedMem.h"

#include "stdio.h"
#include "time.h"
#include "process.h"

using namespace System;
using namespace System::Threading;
using namespace System::Collections;


//
// SharedMemoryManager
//
// Manages a collection of shared memory pairs (see struct shm_pair in SharedMem.h).
private ref class SharedMemoryManager
{
public:	
	// Initializes hashtable of created shared memory pairs.
	static SharedMemoryManager()
	{
#ifdef DEBUG
		Debug::WriteLine("PHP4TS", "static SharedMemoryManager::SharedMemoryManager invoked.");
#endif
		segmentTable = gcnew Hashtable();
	}

	// Gets shared memory pair having the given key.
	static shm_pair *getByKey(int key)
	{
#ifdef DEBUG
		Debug::WriteLine("PHP4TS", "SharedMemoryManager::getByKey invoked.");
#endif

		Object ^obj = segmentTable[key];
		if (obj != nullptr) 
		{
			IntPtr ^ptr = static_cast<IntPtr ^>(obj);
			return static_cast<shm_pair *>(ptr->ToPointer());
		}

		shm_pair *pair = new shm_pair;
		if (pair == NULL) return NULL;

		segmentTable->Add(key, IntPtr(pair));
		return pair;
	}

	// Gets shared memory pair given the address where it is mapped.
	static shm_pair *getByAddr(const void *addr)
	{
#ifdef DEBUG
		Debug::WriteLine("PHP4TS", "SharedMemoryManager::getByAddr invoked.");
#endif

		IDictionaryEnumerator ^enumerator = segmentTable->GetEnumerator();
		while (enumerator->MoveNext() == true)
		{
			IntPtr ^ptr = static_cast<IntPtr ^>(enumerator->Value);
			shm_pair *pair = static_cast<shm_pair *>(ptr->ToPointer());

			if (pair->addr == addr) return pair;
		}
		return NULL;
	}

	// Removes memory pair having the given key from the hashtable.
	static void removeByKey(int key)
	{
#ifdef DEBUG
		Debug::WriteLine("PHP4TS", "SharedMemoryManager::removeByKey invoked.");
#endif

		Object ^obj = segmentTable[key];
		if (obj != nullptr)
		{
			IntPtr ^ptr = static_cast<IntPtr ^>(obj);
			segmentTable->Remove(key);
			delete static_cast<shm_pair *>(ptr->ToPointer());
		}
	}

private:
	static Hashtable ^segmentTable;
};

// copied from tsrm_win32.c, slightly modified and beautified
ZEND_API int shmget(int key, int size, int flags)
{
	shm_pair *shm;
	wchar_t shm_segment[26], shm_info[29];
	HANDLE shm_handle, info_handle;
	BOOL created = FALSE;

	if (size < 0) return -1;

#pragma warning (disable: 4996)
	swprintf(shm_segment, L"EXTSUPPORT_SHM_SEGMENT:%d", key);
	swprintf(shm_info, L"EXTSUPPORT_SHM_DESCRIPTOR:%d", key);
#pragma warning (default: 4996)

	shm_handle  = OpenFileMapping(FILE_MAP_ALL_ACCESS, FALSE, shm_segment);
	info_handle = OpenFileMapping(FILE_MAP_ALL_ACCESS, FALSE, shm_info);

	if ((!shm_handle && !info_handle)) 
	{
		if (flags & IPC_EXCL) return -1;
		if (flags & IPC_CREAT)
		{
			shm_handle	= CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, size, shm_segment);
			info_handle	= CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, sizeof(shm->descriptor), shm_info);
			created		= TRUE;
		}
		if (!shm_handle || !info_handle) return -1;
	}

	Monitor::Enter(SharedMemoryManager::typeid);
	try
	{
		shm = SharedMemoryManager::getByKey(key);

		shm->destroying = false;
		shm->segment	= shm_handle;
		shm->info		= info_handle;
		shm->descriptor = (shmid_ds *)MapViewOfFileEx(shm->info, FILE_MAP_ALL_ACCESS, 0, 0, 0, NULL);

		if (created)
		{
			shm->descriptor->shm_perm.key	= key;
			shm->descriptor->shm_segsz		= size;
			shm->descriptor->shm_ctime		= time(NULL);
			shm->descriptor->shm_cpid		= _getpid();
			shm->descriptor->shm_perm.mode	= flags;

			shm->descriptor->shm_perm.cuid	= shm->descriptor->shm_perm.cgid = 0;
			shm->descriptor->shm_perm.gid	= shm->descriptor->shm_perm.uid  = 0;
			shm->descriptor->shm_atime		= shm->descriptor->shm_dtime	 = 0;
			shm->descriptor->shm_lpid		= shm->descriptor->shm_nattch	 = 0;
			shm->descriptor->shm_perm.mode	= shm->descriptor->shm_perm.seq	 = 0;

			shm->addr = NULL;
		}

		if (shm->descriptor->shm_perm.key != key || size > shm->descriptor->shm_segsz)
		{
			CloseHandle(shm->segment);
			UnmapViewOfFile(shm->descriptor);
			CloseHandle(shm->info);
			return -1;
		}
	}
	finally
	{
		Monitor::Exit(SharedMemoryManager::typeid);
	}

	return key;
}

// copied from tsrm_win32.c, slightly modified and beautified
ZEND_API void *shmat(int key, const void *shmaddr, int flags)
{
	Monitor::Enter(SharedMemoryManager::typeid);
	try
	{
		shm_pair *shm = SharedMemoryManager::getByKey(key);

		if (!shm || !shm->segment) return (void *)-1;

		shm->descriptor->shm_atime = time(NULL);
		shm->descriptor->shm_lpid  = _getpid();
		shm->descriptor->shm_nattch++;

		if (!shm->addr) shm->addr = MapViewOfFileEx(shm->segment, FILE_MAP_ALL_ACCESS, 0, 0, 0, NULL);
		return shm->addr;
	}
	finally
	{
		Monitor::Exit(SharedMemoryManager::typeid);
	}
}

// copied from tsrm_win32.c, slightly modified and beautified
ZEND_API int shmdt(const void *shmaddr)
{
	Monitor::Enter(SharedMemoryManager::typeid);
	try
	{
		shm_pair *shm = SharedMemoryManager::getByAddr(shmaddr);

		if (!shm || !shm->segment) return -1;

		shm->descriptor->shm_dtime = time(NULL);
		shm->descriptor->shm_lpid  = _getpid();
		
		if (--shm->descriptor->shm_nattch == 0)
		{
			int ret = UnmapViewOfFile(shm->addr) ? 0 : -1;
			shm->addr = NULL;

			if (shm->destroying)
			{
				int key = shm->descriptor->shm_perm.key;
				
				CloseHandle(shm->segment);
				UnmapViewOfFile(shm->descriptor);
				CloseHandle(shm->info);
				
				SharedMemoryManager::removeByKey(key);
			}
			return ret;
		}
		else return 0;
	}
	finally
	{
		Monitor::Exit(SharedMemoryManager::typeid);
	}
}

// copied from tsrm_win32.c, slightly modified and beautified
ZEND_API int shmctl(int key, int cmd, struct shmid_ds *buf)
{
	Monitor::Enter(SharedMemoryManager::typeid);
	try
	{
		shm_pair *shm = SharedMemoryManager::getByKey(key);

		if (!shm || !shm->segment) return -1;

		switch (cmd)
		{
			case IPC_STAT:
				memcpy(buf, shm->descriptor, sizeof(struct shmid_ds));
				return 0;

			case IPC_SET:
				shm->descriptor->shm_ctime		= time(NULL);
				shm->descriptor->shm_perm.uid	= buf->shm_perm.uid;
				shm->descriptor->shm_perm.gid	= buf->shm_perm.gid;
				shm->descriptor->shm_perm.mode	= buf->shm_perm.mode;
				return 0;

			case IPC_RMID:
				// ExtManager is the only process that has extensions mapped in it, so
				// unlike PHP we can actually free the segment.
				if (shm->descriptor->shm_nattch < 1) 
				{
					CloseHandle(shm->segment);
					UnmapViewOfFile(shm->descriptor);
					CloseHandle(shm->info);

					SharedMemoryManager::removeByKey(key);
				}
				else shm->destroying = true;
				return 0;

			default:
				return -1;
		}
	}
	finally
	{
		Monitor::Exit(SharedMemoryManager::typeid);
	}
}
