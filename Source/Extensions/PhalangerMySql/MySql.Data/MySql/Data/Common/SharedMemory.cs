namespace MySql.Data.Common
{
    using MySql.Data.MySqlClient;
    using System;

    internal class SharedMemory : IDisposable
    {
        private const uint FILE_MAP_WRITE = 2;
        private IntPtr fileMapping;
        private IntPtr view;

        public SharedMemory(string name, IntPtr size)
        {
            this.fileMapping = NativeMethods.OpenFileMapping(2, false, name);
            if (this.fileMapping == IntPtr.Zero)
            {
                throw new MySqlException("Cannot open file mapping " + name);
            }
            this.view = NativeMethods.MapViewOfFile(this.fileMapping, 2, 0, 0, size);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.view != IntPtr.Zero)
                {
                    NativeMethods.UnmapViewOfFile(this.view);
                    this.view = IntPtr.Zero;
                }
                if (this.fileMapping != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(this.fileMapping);
                    this.fileMapping = IntPtr.Zero;
                }
            }
        }

        public IntPtr View
        {
            get
            {
                return this.view;
            }
        }
    }
}

