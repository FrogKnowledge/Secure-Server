using System;

namespace X509.Crypto
{
    public abstract class CryptKey : DisposeableObject
    {
        private readonly CryptContext ctx;

        private readonly IntPtr handle;

        internal IntPtr Handle
        {
            get
            {
                return this.handle;
            }
        }

        public abstract KeyType Type
        {
            get;
        }

        internal CryptKey(CryptContext ctx, IntPtr handle)
        {
            this.ctx = ctx;
            this.handle = handle;
        }

        protected override void CleanUp(bool viaDispose)
        {
            if (viaDispose)
            {
                this.ctx.DestroyKey(this);
            }
        }
    }
}
