/*

 Copyright (c) 2004-2006 Ladislav Prosek. Inspired by PipeChannel and MS Shared Source CLI.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PHP.Core
{
	/// <summary>
	/// Shared memory connector. Abstract class, base of <see cref="ShmClientConnector"/> and
	/// <see cref="ShmServerConnector"/>.
	/// </summary>
	/// <remarks>
	/// Clients and servers use connectors to build private connections.
	/// </remarks>
	internal abstract class ShmConnectorBase : IDisposable
	{
		#region Fields

		/// <summary>Handle of the connector section object.</summary>
		protected IntPtr connectSectionHandle;

		/// <summary>Handle of the request event object.</summary>
		protected IntPtr connectRequestEventHandle;

		/// <summary>Handle of the confirm event object.</summary>
		protected IntPtr connectConfirmEventHandle;

		/// <summary>Handle of the request serializing mutex object.</summary>
		protected IntPtr connectMutexHandle;

		/// <summary>
		/// Virtual address of the mapped view into <see cref="connectSectionHandle"/>.
		/// </summary>
		protected IntPtr viewAddr;

		/// <summary>
		/// Equal to MAX_PATH times 2 (Section name in Unicode).
		/// </summary>
		protected const uint CONNECTOR_SECTION_SIZE = 260 * 2;

		/// <summary>Timeout when waiting for various synchronization objects.</summary>
		protected const uint waitTimeout = 5000;

		/// <summary>Common prefix of <see cref="connectSectionHandle"/> names.</summary>
		protected const string fileMappingPrefix = "Global\\ShmChannel_connsection_";

		/// <summary>Common prefix of <see cref="connectRequestEventHandle"/> names.</summary>
		protected const string connRequestPrefix = "Global\\ShmChannel_connreqevent_";

		/// <summary>Common prefix of <see cref="connectConfirmEventHandle"/> names.</summary>
		protected const string connConfirmPrefix = "Global\\ShmChannel_connackevent_";

		/// <summary>Common prefix of <see cref="connectMutexHandle"/> names.</summary>
		protected const string connMutexPrefix = "Global\\ShmChannel_connmutex_";

		#endregion

		#region IDisposable implementation and related members

		/// <summary>
		/// Standard <see cref="IDisposable.Dispose"/> implementation.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes of unmanaged and optionally also managed resources.
		/// </summary>
		/// <param name="disposing">If <B>true</B>, both managed and unmanaged resources should be released.
		/// If <B>false</B> only unmanaged resources should be released.</param>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(ShmChannel.verbose, "Disposing connector.");

			if (viewAddr != IntPtr.Zero)
			{
				ShmNative.UnmapViewOfFile(viewAddr);
				viewAddr = IntPtr.Zero;
			}

			ShmNative.CloseHandleOnce(ref connectMutexHandle);
			ShmNative.CloseHandleOnce(ref connectConfirmEventHandle);
			ShmNative.CloseHandleOnce(ref connectRequestEventHandle);
			ShmNative.CloseHandleOnce(ref connectSectionHandle);
		}

		/// <summary>
		/// Destructor that will run only if the <see cref="Dispose()"/> method does not get called.
		/// </summary>
		~ShmConnectorBase()
		{
			Dispose(false);
		}

		#endregion
	}

	/// <summary>
	/// Shared memory connector, the server end.
	/// </summary>
	internal sealed class ShmServerConnector : ShmConnectorBase
	{
		/// <summary>
		/// If <B>true</B>, an incoming request was recognized but not yet confirmed.
		/// </summary>
		private bool requestPending = false;

		/// <summary>
		/// Constructs a new <see cref="ShmServerConnector"/>.
		/// </summary>
		/// <param name="sectionName">The section name.</param>
		/// <exception cref="ShmIOException">Creation of unmanaged resources failed.</exception>
		public ShmServerConnector(string sectionName)
		{
			Debug.WriteLineIf(ShmChannel.verbose, "ShmServerConnector.ctor(" + sectionName + ")");

			string file_mapping_name = fileMappingPrefix + sectionName;

			// create file mapping (section) object
			connectSectionHandle = ShmNative.CreateFileMapping(
				ShmNative.INVALID_HANDLE_VALUE,
				ShmNative.securityAttributes,
				ShmNative.PAGE_READWRITE | ShmNative.SEC_COMMIT,
				0,
				CONNECTOR_SECTION_SIZE,
				file_mapping_name);

			if (connectSectionHandle == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("creating_file_mapping_failed", file_mapping_name),
					Marshal.GetLastWin32Error());
			}

			// map it into virtual address space
			viewAddr = ShmNative.MapViewOfFile(connectSectionHandle, ShmNative.FILE_MAP_WRITE, 0, 0,
				CONNECTOR_SECTION_SIZE);

			if (viewAddr == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("mapping_view_failed"), Marshal.GetLastWin32Error());
			}

			// create events
			connectRequestEventHandle = ShmNative.CreateNamedEvent(connRequestPrefix + sectionName, false);
			connectConfirmEventHandle = ShmNative.CreateNamedEvent(connConfirmPrefix + sectionName, false);

			// create mutex
			connectMutexHandle = ShmNative.CreateNamedMutex(connMutexPrefix + sectionName, false);
		}

		/// <summary>
		/// Listens for an incoming connection.
		/// </summary>
		/// <param name="stopWaitingEvent">The event that signalizes that waiting should be aborted.</param>
		/// <returns><B>True</B> if a request was received, <B>false</B> if waiting was aborted because
		/// <paramref name="stopWaitingEvent"/> having been set.</returns>
		/// <exception cref="InvalidOperationException">A request is already pending and must be confirmed before
		/// waiting for another connection.</exception>
		/// <exception cref="ShmIOException">Waiting for <see cref="ShmConnectorBase.connectRequestEventHandle"/>
		/// failed.</exception>
		public bool WaitForConnect(IntPtr stopWaitingEvent)
		{
			if (requestPending)
			{
				throw new InvalidOperationException(ShmResources.GetString("request_already_pending"));
			}

			// wait for request
			//switch (ShmNative.WaitForSingleObject(connectRequestEventHandle, ShmNative.INFINITE))
			switch (ShmNative.WaitForMultipleObjects(
				2,
				new IntPtr[] { connectRequestEventHandle, stopWaitingEvent },
				false,
				ShmNative.INFINITE))
			{
				case ShmNative.WAIT_OBJECT_0 + 1:
				{
					// the stop waiting event has been set
					return false;
				}

				case ShmNative.WAIT_TIMEOUT:
				{
					throw new ShmIOException(ShmResources.GetString("timeout_waiting_for_request_event"));
				}

				case ShmNative.WAIT_FAILED:
				{
					throw new ShmIOException(ShmResources.GetString("waiting_for_request_event_failed"),
						Marshal.GetLastWin32Error());
				}
			}

			requestPending = true;
			return true;
		}

		/// <summary>
		/// Confirms the incoming connection.
		/// </summary>
		/// <param name="name">Private section name to be passed to the connecting side.</param>
		/// <remarks>
		/// If no request is pending, <see cref="WaitForConnect"/> is called first.
		/// </remarks>
		/// <exception cref="ArgumentException"><paramref name="name"/> is longer than
		/// <see cref="ShmConnectorBase.CONNECTOR_SECTION_SIZE"/>.
		/// </exception>
		public void ConfirmConnect(string name)
		{
			if (!requestPending)
			{
				throw new InvalidOperationException();
			}

			char[] chars = name.ToCharArray();
			int len = chars.Length;

			if (len >= ((CONNECTOR_SECTION_SIZE / 2) - 1))
			{
				throw new ArgumentException(ShmResources.GetString("section_name_too_long"), "name");
			}

			// copy characters
			Marshal.Copy(chars, 0, viewAddr, len);

			// trailing zero
			Marshal.WriteInt16(viewAddr, len * 2, 0);

			// let the client know that the name is prepared in the section
			ShmNative.SetEvent(connectConfirmEventHandle);

			requestPending = false;
		}

		/// <summary>
		/// Performs disposal tasks specific for <see cref="ShmServerConnector"/>.
		/// </summary>
		/// <param name="disposing">If <B>true</B>, both managed and unmanaged resources should be released.
		/// </param>
		protected override void Dispose(bool disposing)
		{
			if (requestPending) ConfirmConnect(String.Empty);
			base.Dispose(disposing);
		}

	}

	/// <summary>
	/// Shared memory connector, the client end.
	/// </summary>
	internal sealed class ShmClientConnector : ShmConnectorBase
	{
		/// <summary>
		/// Constructs a new <see cref="ShmClientConnector"/>.
		/// </summary>
		/// <param name="sectionName">The section name.</param>
		/// <exception cref="ShmIOException">Creation of unmanaged resources failed.</exception>
		public ShmClientConnector(string sectionName)
		{
			Debug.WriteLineIf(ShmChannel.verbose, "ShmClientConnector.ctor(" + sectionName + ")");

			string file_mapping_name = fileMappingPrefix + sectionName;

			// open file mapping (section) object
			connectSectionHandle = ShmNative.OpenFileMapping(
				ShmNative.FILE_MAP_READ | ShmNative.FILE_MAP_WRITE,
				false,
				file_mapping_name);

			if (connectSectionHandle == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("opening_file_mapping_failed", file_mapping_name),
					Marshal.GetLastWin32Error());
			}

			// map it into virtual address space
			viewAddr = ShmNative.MapViewOfFile(connectSectionHandle, ShmNative.FILE_MAP_WRITE, 0, 0, CONNECTOR_SECTION_SIZE);

			if (viewAddr == IntPtr.Zero)
			{
				throw new ShmIOException(ShmResources.GetString("mapping_view_failed", file_mapping_name),
					Marshal.GetLastWin32Error());
			}

			// open events
			connectRequestEventHandle = ShmNative.OpenNamedEvent(connRequestPrefix + sectionName);
			connectConfirmEventHandle = ShmNative.OpenNamedEvent(connConfirmPrefix + sectionName);

			// open mutex
			connectMutexHandle = ShmNative.OpenNamedMutex(connMutexPrefix + sectionName);
		}

		/// <summary>
		/// Tries to connect to the server end and obtain a private file mapping name.
		/// </summary>
		/// <returns>Private file mapping name to be used for the new connection.</returns>
		/// <exception cref="ShmIOException">Waiting for <see cref="ShmConnectorBase.connectMutexHandle"/> or
		/// <see cref="ShmConnectorBase.connectConfirmEventHandle"/> failed.</exception>
		public string Connect()
		{
			string private_section_name = null;

			do
			{
				// acquire the connection mutex, so that other clients trying to connect will have to wait
				switch (ShmNative.WaitForSingleObject(connectMutexHandle, 3 * waitTimeout))
				{
					case ShmNative.WAIT_TIMEOUT:
					{
						throw new ShmIOException(ShmResources.GetString("timeout_waiting_for_mutex"));
					}

					case ShmNative.WAIT_FAILED:
					{
						throw new ShmIOException(ShmResources.GetString("waiting_for_mutex_failed"),
							Marshal.GetLastWin32Error());
					}
				}

				try
				{
					// let the server know that we are interested in getting a private section
					ShmNative.SetEvent(connectRequestEventHandle);

					// wait for acknowledgement
					switch (ShmNative.WaitForSingleObject(connectConfirmEventHandle, waitTimeout))
					{
						case ShmNative.WAIT_TIMEOUT:
						{
							throw new ShmIOException(ShmResources.GetString("timeout_waiting_for_confirm_event"));
						}

						case ShmNative.WAIT_FAILED:
						{
							throw new ShmIOException(ShmResources.GetString("waiting_for_confirm_event_failed"),
								Marshal.GetLastWin32Error());
						}
					}

					// read the private section name from the shared section
					private_section_name = Marshal.PtrToStringUni(viewAddr);
				}
				finally
				{
					// release the connection mutex, so that the other clients trying to connect
					// will be let in (note that both events are in nonsignaled state now)
					ShmNative.ReleaseMutex(connectMutexHandle);
				}
			}
			while (private_section_name == String.Empty);

			return private_section_name;
		}
	}
}
