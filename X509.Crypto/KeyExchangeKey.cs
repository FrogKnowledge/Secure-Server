using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X509.Crypto {
    public class KeyExchangeKey : CryptKey {
        public override KeyType Type {
            get {
                return KeyType.Exchange;
            }
        }

        internal KeyExchangeKey(CryptContext ctx, IntPtr handle)
            : base(ctx, handle) {
        }
    }
}
