/*

 Copyright (c) 2004-2006 Ladislav Prosek. Inspired by PipeChannel and MS Shared Source CLI.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Security;
using System.Runtime.InteropServices;

namespace PHP.Core
{
	/// <summary>
	/// Container for shared memory and synchronization native functions.
	/// </summary>
	[SuppressUnmanagedCodeSecurity]
	public class ShmNative
	{
		/// <summary>
		/// The security attributes unmanaged resources will be created with.
		/// </summary>
		public static readonly IntPtr securityAttributes;

		/// <summary>
		/// Managed definition of the <c>SECURITY_ATTRIBUTES</c> structure used when creating unmanaged resources.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct SECURITY_ATTRIBUTES
		{
			/// <summary>
			/// The size, in bytes, of this structure.
			/// </summary>
			public uint nLength;

			/// <summary>
			/// Pointer to a security descriptor.
			/// </summary>
			public IntPtr lpSecurityDescriptor;

			/// <summary>
			/// Specifies whether the returned handle is inherited when a new process is created.
			/// </summary>
			public bool bInheritHandle;
		}

		/// <summary>
		/// Static constructor. Initializes <see cref="securityAttributes"/>.
		/// </summary>
		/// <exception cref="ShmIOException">Initialization of unmanaged resources failed.</exception>
		static ShmNative()
		{
			// Security related values
			int SECURITY_DESCRIPTOR_MIN_LENGTH = 20;
			uint SECURITY_DESCRIPTOR_REVISION = 1;

			IntPtr secDescr = Marshal.AllocHGlobal(SECURITY_DESCRIPTOR_MIN_LENGTH);
			if (InitializeSecurityDescriptor(secDescr, SECURITY_DESCRIPTOR_REVISION) == false)
			{
				throw new ShmIOException(ShmResources.GetString("security_descriptor_init_failed"),
					Marshal.GetLastWin32Error());
			}

			// empty DACL (but DACL marked 'present') means all have access
			if (SetSecurityDescriptorDacl(secDescr, true, IntPtr.Zero, false) == false)
			{
				throw new ShmIOException(ShmResources.GetString("security_descriptor_set_dacl_failed"),
					Marshal.GetLastWin32Error());
			}

			SECURITY_ATTRIBUTES managedSecAttr = new SECURITY_ATTRIBUTES();
			managedSecAttr.nLength = (uint)Marshal.SizeOf(managedSecAttr);
			managedSecAttr.lpSecurityDescriptor = secDescr;
			managedSecAttr.bInheritHandle = false;

			// copy security attributes to unmanaged heap
			securityAttributes = Marshal.AllocHGlobal(Marshal.SizeOf(managedSecAttr));
			Marshal.StructureToPtr(managedSecAttr, securityAttributes, true);
		}

		/// <summary>Value of an invalid handle.</summary>
		public static IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);

		#region Constants

		/// <summary>The ERROR_ALREADY_EXISTS Win32 error code.</summary>
		public const int ERROR_ALREADY_EXISTS = 183;

		/// <summary><c>flProtect</c> flag for <see cref="CreateFileMapping"/>.</summary>
		public const uint PAGE_READONLY = 2;
		/// <summary><c>flProtect</c> flag for <see cref="CreateFileMapping"/>.</summary>
		public const uint PAGE_READWRITE = 4;
		/// <summary><c>flProtect</c> flag for <see cref="CreateFileMapping"/>.</summary>
		public const uint PAGE_WRITECOPY = 8;
		/// <summary><c>flProtect</c> flag for <see cref="CreateFileMapping"/>.</summary>
		public const uint SEC_COMMIT = 0x8000000;
		/// <summary><c>flProtect</c> flag for <see cref="CreateFileMapping"/>.</summary>
		public const uint SEC_IMAGE = 0x1000000;
		/// <summary><c>flProtect</c> flag for <see cref="CreateFileMapping"/>.</summary>
		public const uint SEC_NOCACHE = 0x10000000;
		/// <summary><c>flProtect</c> flag for <see cref="CreateFileMapping"/>.</summary>
		public const uint SEC_RESERVE = 0x4000000;

		/// <summary><c>dwDesiredAccess</c> flag for <see cref="OpenFileMapping"/>.</summary>
		public const uint FILE_MAP_WRITE = 2;
		/// <summary><c>dwDesiredAccess</c> flag for <see cref="OpenFileMapping"/>.</summary>
		public const uint FILE_MAP_READ = 4;
		/// <summary><c>dwDesiredAccess</c> flag for <see cref="OpenFileMapping"/>.</summary>
		public const uint FILE_MAP_ALL_ACCESS = 0xf001f;
		/// <summary><c>dwDesiredAccess</c> flag for <see cref="OpenFileMapping"/>.</summary>
		public const uint FILE_MAP_COPY = 1;

		/// <summary><c>dwDesiredAccess</c> flag for <see cref="OpenEvent"/>.</summary>
		public const uint EVENT_ALL_ACCESS = 0x1f0003;
		/// <summary><c>dwDesiredAccess</c> flag for <see cref="OpenEvent"/>.</summary>
		public const uint EVENT_MODIFY_STATE = 2;
		/// <summary><c>dwDesiredAccess</c> flag for <see cref="OpenEvent"/>.</summary>
		public const uint SYNCHRONIZE = 0x100000;

		/// <summary><c>dwDesiredAccess</c> flag for <see cref="OpenMutex"/>.</summary>
		public const uint MUTEX_ALL_ACCESS = 0x1f0001;

		/// <summary>
		/// <c>dwMilliseconds</c> special value for <see cref="WaitForSingleObject"/> and
		/// <see cref="WaitForMultipleObjects"/>.
		/// </summary>
		public const uint INFINITE = 0xffffffff;

		/// <summary><see cref="WaitForSingleObject"/> return value.</summary>
		public const uint WAIT_ABANDONED = 0x80;
		/// <summary><see cref="WaitForSingleObject"/> return value.</summary>
		public const uint WAIT_ABANDONED_0 = 0x80;
		/// <summary><see cref="WaitForSingleObject"/> return value.</summary>
		public const uint WAIT_OBJECT_0 = 0;
		/// <summary><see cref="WaitForSingleObject"/> return value.</summary>
		public const uint WAIT_TIMEOUT = 0x102;
		/// <summary><see cref="WaitForSingleObject"/> return value.</summary>
		public const uint WAIT_FAILED = 0xffffffff;

		/// <summary><c>dwFlags</c> flag for <see cref="FormatMessage"/>.</summary>
		public const uint FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100;
		/// <summary><c>dwFlags</c> flag for <see cref="FormatMessage"/>.</summary>
		public const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x200;
		/// <summary><c>dwFlags</c> flag for <see cref="FormatMessage"/>.</summary>
		public const uint FORMAT_MESSAGE_FROM_STRING = 0x400;
		/// <summary><c>dwFlags</c> flag for <see cref="FormatMessage"/>.</summary>
		public const uint FORMAT_MESSAGE_FROM_HMODULE = 0x800;
		/// <summary><c>dwFlags</c> flag for <see cref="FormatMessage"/>.</summary>
		public const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
		/// <summary><c>dwFlags</c> flag for <see cref="FormatMessage"/>.</summary>
		public const uint FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x2000;
		/// <summary><c>dwFlags</c> flag for <see cref="FormatMessage"/>.</summary>
		public const uint FORMAT_MESSAGE_MAX_WIDTH_MASK = 0xff;

		/// <summary><c>dwLanguageId</c> value for <see cref="FormatMessage"/>.</summary>
		public const uint LANG_NEUTRAL_SUBLANG_DEFAULT = 0x400;

		#endregion

		#region P/Invoke imports

		/// <summary><c>CreateFileMapping</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpAttributes, uint flProtect,
			uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string Name);

		/// <summary><c>OpenFileMapping</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string Name);

		/// <summary><c>MapViewOfFile</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess,
			uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

		/// <summary><c>UnmapViewOfFile</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

		/// <summary><c>CloseHandle</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool CloseHandle(IntPtr hObject);

		/// <summary><c>CreateEvent</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr CreateEvent(IntPtr lpAttributes, bool bManualReset,
			bool bInitialState, string Name);

		/// <summary><c>OpenEvent</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenEvent(uint dwDesiredAccess, bool bInheritHandle, string Name);

		/// <summary><c>SetEvent</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool SetEvent(IntPtr hEvent);

		/// <summary><c>ResetEvent</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ResetEvent(IntPtr hEvent);

		/// <summary><c>PulseEvent</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool PulseEvent(IntPtr hEvent);

		/// <summary><c>CreateMutex</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr CreateMutex(IntPtr lpAttributes, bool bInitialOwner, string Name);

		/// <summary><c>OpenMutext</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenMutex(uint dwDesiredAccess, bool bInheritHandle, string Name);

		/// <summary><c>ReleaseMutex</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReleaseMutex(IntPtr hMutex);

		/// <summary><c>OpenThread</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		/// <summary><c>WaitForSingleObject</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

		/// <summary><c>WaitForMultipleObjects</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern uint WaitForMultipleObjects(uint nCount, IntPtr[] lpHandles, bool bWaitAll,
			uint dwMilliseconds);

		/// <summary><c>InitializeSecurityDescriptor</c> API function imported from <c>advapi32.dll</c>.</summary>
		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool InitializeSecurityDescriptor(IntPtr pSecurityDescriptor, uint dwRevision);

		/// <summary><c>SetSecurityDescriptorDacl</c> API function imported from <c>advapi32.dll</c>.</summary>
		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool SetSecurityDescriptorDacl(IntPtr pSecurityDescriptor, bool bDaclPresent,
			IntPtr pDacl, bool bDaclDefaulted);

		/// <summary><c>FormatMessage</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll")]
		public static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId,
			uint dwLanguageId, ref string lpBuffer, uint nSize, IntPtr Arguments);

		/// <summary><c>LocalFree</c> API function imported from <c>kernel32.dll</c>.</summary>
		[DllImport("kernel32.dll")]
		public static extern IntPtr LocalFree(IntPtr hMem);

		#endregion

		#region Exception throwing wrappers

		/// <summary>
		/// Creates a named event. The reason for this is that the framework supports only unnamed 
		/// events (see <see cref="System.Threading.AutoResetEvent"/>).
		/// </summary>
		/// <param name="name">The name of the event.</param>
		/// <param name="signaled">Initial state of the event.</param>
		/// <returns>Handle of the created event.</returns>
		/// <exception cref="ShmIOException">Could not create the event.</exception>
		public static IntPtr CreateNamedEvent(string name, bool signaled)
		{
			IntPtr handle = CreateEvent(securityAttributes, false, signaled, name);
			if (handle == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("creating_event_failed", name), Marshal.GetLastWin32Error());
			}
			return handle;
		}

		/// <summary>
		/// Opens a named event. The reason for this is that the framework supports only unnamed events 
		/// (see <see cref="System.Threading.AutoResetEvent"/>).
		/// </summary>
		/// <param name="name">The name of the event.</param>
		/// <returns>Handle of the opened event.</returns>
		/// <exception cref="ShmIOException">Could not open the event.</exception>
		public static IntPtr OpenNamedEvent(string name)
		{
			IntPtr handle = OpenEvent(EVENT_ALL_ACCESS, false, name);
			if (handle == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("opening_event_failed", name), Marshal.GetLastWin32Error());
			}
			return handle;
		}

		/// <summary>
		/// Creates a named mutex.
		/// </summary>
		/// <param name="name">The name of the mutex.</param>
		/// <param name="initialOwner">Specifies whether the calling thread should become an owner of
		/// this mutex.</param>
		/// <returns>Handle of the created mutex.</returns>
		/// <exception cref="ShmIOException">Could not create the mutex.</exception>
		public static IntPtr CreateNamedMutex(string name, bool initialOwner)
		{
			IntPtr handle = CreateMutex(securityAttributes, initialOwner, name);
			if (handle == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("creating_mutex_failed", name),
					Marshal.GetLastWin32Error());
			}
			return handle;
		}

		/// <summary>
		/// Opens a named mutex.
		/// </summary>
		/// <param name="name">The name of the mutex.</param>
		/// <returns>Handle of the opened mutex.</returns>
		/// <exception cref="ShmIOException">Could not open the mutex.</exception>
		public static IntPtr OpenNamedMutex(string name)
		{
			IntPtr handle = OpenMutex(MUTEX_ALL_ACCESS, false, name);
			if (handle == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("opening_mutex_failed", name),
					Marshal.GetLastWin32Error());
			}
			return handle;
		}

		#endregion

		/// <summary>
		/// Closes a handle at most once.
		/// </summary>
		/// <param name="handle">The handle to close.</param>
		public static void CloseHandleOnce(ref IntPtr handle)
		{
			if (handle != IntPtr.Zero)
			{
				CloseHandle(handle);
				handle = IntPtr.Zero;
			}
		}

		/// <summary>
		/// Returns error message according to a Win32 error code.
		/// </summary>
		/// <param name="errorCode">The error code.</param>
		/// <returns>The error message.</returns>
		public static string GetErrorString(int errorCode)
		{
			string msg_buf = "";

			uint ret_val = FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM |
				FORMAT_MESSAGE_IGNORE_INSERTS, IntPtr.Zero, (uint)errorCode, 0, ref msg_buf, 255, IntPtr.Zero);

			if (ret_val == 0) return ShmResources.GetString("win32_error_code", errorCode);

			return msg_buf;
		}
	}
}
