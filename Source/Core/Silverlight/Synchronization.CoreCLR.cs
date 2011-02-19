using System;
using System.Collections.Generic;
using System.Text;
using PHP.Core;
using System.Threading;

namespace PHP.CoreCLR
{
	class ReaderWriterLock
	{
		private readonly object objLock = new object();
		
		public void AcquireReaderLock(int timeout)
		{
			Debug.Assert(timeout == -1, "Emulated ReaderWriterLock doesn't support timeout");
			Monitor.Enter(objLock);	
		}

		public void ReleaseReaderLock()
		{
			Monitor.Exit(objLock);
		}

		public void AcquireWriterLock(int timeout)
		{
			Debug.Assert(timeout == -1, "Emulated ReaderWriterLock doesn't support timeout");
			Monitor.Enter(objLock);
		}

		public void ReleaseWriterLock()
		{
			Monitor.Exit(objLock);
		}
	}
}
