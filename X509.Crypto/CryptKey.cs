using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X509.Crypto {
    public abstract class CryptKey : DisposeableObject {
        private CryptContext ctx;

        private IntPtr handle;

        internal IntPtr Handle {
            get {
                return this.handle;
            }
        }

        public abstract KeyType Type {
            get;
        }

        internal CryptKey(CryptContext ctx, IntPtr handle) {
            this.ctx = ctx;
            this.handle = handle;
        }

        protected override void CleanUp(bool viaDispose) {
            if (viaDispose) {
                this.ctx.DestroyKey(this);
            }
        }
    }
}
