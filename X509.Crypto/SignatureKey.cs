using System;

namespace X509.Crypto
{
    public class SignatureKey : CryptKey
    {
        public override KeyType Type
        {
            get
            {
                return KeyType.Signature;
            }
        }

        internal SignatureKey(CryptContext ctx, IntPtr handle)
            : base(ctx, handle)
        {
        }
    }
}
