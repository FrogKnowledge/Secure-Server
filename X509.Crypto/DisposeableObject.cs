using System;
using System.Runtime.InteropServices;

namespace X509.Crypto
{
    [StructLayout(LayoutKind.Sequential)]
    public abstract class DisposeableObject : IDisposable
    {
        private bool disposed;

        ~DisposeableObject()
        {
            this.CleanUp(false);
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.CleanUp(true);
                this.disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        protected abstract void CleanUp(bool viaDispose);

        protected void ThrowIfDisposed()
        {
            this.ThrowIfDisposed(base.GetType().FullName);
        }

        protected void ThrowIfDisposed(string objectName)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(objectName);
            }
        }
    }
}
