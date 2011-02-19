//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// SharedMem.h
// - contains declarations of shared memory related exported functions
// - basically an emulation of Unix shared memory API
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

#define IPC_PRIVATE	0
#define IPC_CREAT	00001000
#define IPC_EXCL	00002000
#define IPC_NOWAIT	00004000

#define IPC_RMID	0
#define IPC_SET		1
#define IPC_STAT	2
#define IPC_INFO	3

#define SHM_R		PAGE_READONLY
#define SHM_W		PAGE_READWRITE

#define	SHM_RDONLY	FILE_MAP_READ
#define	SHM_RND		FILE_MAP_WRITE
#define	SHM_REMAP	FILE_MAP_COPY

struct ipc_perm
{
	int key;
	unsigned short uid;
	unsigned short gid;
	unsigned short cuid;
	unsigned short cgid;
	unsigned short mode;
	unsigned short seq;
};

struct shmid_ds 
{
	struct ipc_perm shm_perm;
	int				shm_segsz;
	time_t			shm_atime;
	time_t			shm_dtime;
	time_t			shm_ctime;
	unsigned short	shm_cpid;
	unsigned short	shm_lpid;
	short			shm_nattch;
};

struct shm_pair
{
	void	*addr;
	HANDLE	info;
	HANDLE	segment;
	struct	shmid_ds *descriptor;
	bool	destroying;
};

#ifdef __cplusplus
extern "C"
{
#endif

ZEND_API int shmget(int key, int size, int flags);
ZEND_API void *shmat(int key, const void *shmaddr, int flags);
ZEND_API int shmdt(const void *shmaddr);
ZEND_API int shmctl(int key, int cmd, struct shmid_ds *buf);

#ifdef __cplusplus
}
#endif
