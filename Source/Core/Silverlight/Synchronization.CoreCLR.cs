using System;
using System.Collections.Generic;
using System.Text;
using PHP.Core;
using System.Threading;

namespace PHP.CoreCLR
{
	class ReaderWriterLockSlim
	{
		private readonly object objLock = new object();
		
		public void EnterReadLock()
		{
			Monitor.Enter(objLock);	
		}

		public void ExitReadLock()
		{
			Monitor.Exit(objLock);
		}

		public void EnterWriteLock()
		{
			Monitor.Enter(objLock);
		}

		public void ExitWriteLock()
		{
			Monitor.Exit(objLock);
		}
	}
}
