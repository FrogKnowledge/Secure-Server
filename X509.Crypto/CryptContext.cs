using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;


namespace X509.Crypto
{
    public class CryptContext : DisposeableObject
    {
        private IntPtr handle = IntPtr.Zero;

        public IntPtr Handle
        {
            get
            {
                return this.handle;
            }
        }

        public string ContainerName { get; set; }

        public string ProviderName { get; set; }

        public int ProviderType { get; set; }

        public int Flags { get; set; }

        public CryptContext()
        {
            this.ContainerName = Guid.NewGuid().ToString();
            this.ProviderType = (int)ProviderTypes.PROV_RSA_FULL;
            this.Flags = (int)ContextFlags.CRYPT_NEWKEYSET;
        }

        public void Open()
        {
            base.ThrowIfDisposed();
            if (!Win32Native.CryptAcquireContext(out this.handle, this.ContainerName, this.ProviderName, this.ProviderType, this.Flags))
            {
                Win32ErrorHelper.ThrowExceptionIfGetLastErrorIsNotZero();
            }
        }

        public X509Certificate2 CreateSelfSignedCertificate(SelfSignedCertProperties properties)
        {
            this.ThrowIfDisposedOrNotOpen();
            this.GenerateKeyExchangeKey(properties.IsPrivateKeyExportable, properties.KeyBitLength);
            byte[] rawData = properties.Name.RawData;
            GCHandle gCHandle = GCHandle.Alloc(rawData, GCHandleType.Pinned);

            Win32Native.CryptKeyProviderInformation keyProviderInfo = new Win32Native.CryptKeyProviderInformation
            {
                ContainerName = this.ContainerName,
                KeySpec = 1,
                ProviderType = (int)ProviderTypes.PROV_RSA_FULL
            };

            IntPtr intPtr = Win32Native.CertCreateSelfSignCertificate(this.handle, new Win32Native.CryptoApiBlob(rawData.Length, gCHandle.AddrOfPinnedObject()), 0, keyProviderInfo, IntPtr.Zero, this.ToSystemTime(properties.ValidFrom), this.ToSystemTime(properties.ValidTo), IntPtr.Zero);
            gCHandle.Free();

            if (IntPtr.Zero == intPtr)
            {
                Win32ErrorHelper.ThrowExceptionIfGetLastErrorIsNotZero();
            }

            X509Certificate2 result = new X509Certificate2(intPtr);
            if (!Win32Native.CertFreeCertificateContext(intPtr))
            {
                Win32ErrorHelper.ThrowExceptionIfGetLastErrorIsNotZero();
            }
            return result;
        }

        private Win32Native.SystemTime ToSystemTime(DateTime dateTime)
        {
            long num = dateTime.ToFileTime();
            Win32Native.SystemTime systemTime = new Win32Native.SystemTime();

            if (!Win32Native.FileTimeToSystemTime(ref num, systemTime))
            {
                Win32ErrorHelper.ThrowExceptionIfGetLastErrorIsNotZero();
            }
            return systemTime;
        }

        public KeyExchangeKey GenerateKeyExchangeKey(bool exportable, int keyBitLength)
        {
            this.ThrowIfDisposedOrNotOpen();
            uint flags = (uint)((exportable ? 1 : 0) | keyBitLength << 16);
            IntPtr intPtr;

            if (!Win32Native.CryptGenKey(this.handle, 1, flags, out intPtr))
            {
                Win32ErrorHelper.ThrowExceptionIfGetLastErrorIsNotZero();
            }
            return new KeyExchangeKey(this, intPtr);
        }

        internal void DestroyKey(CryptKey key)
        {
            this.ThrowIfDisposedOrNotOpen();
            if (!Win32Native.CryptDestroyKey(key.Handle))
            {
                Win32ErrorHelper.ThrowExceptionIfGetLastErrorIsNotZero();
            }
        }

        protected override void CleanUp(bool viaDispose)
        {
            if (this.handle != IntPtr.Zero && !Win32Native.CryptReleaseContext(this.handle, 0) && viaDispose)
            {
                Win32ErrorHelper.ThrowExceptionIfGetLastErrorIsNotZero();
            }
        }

        private void ThrowIfDisposedOrNotOpen()
        {
            base.ThrowIfDisposed();
            this.ThrowIfNotOpen();
        }

        private void ThrowIfNotOpen()
        {
            if (IntPtr.Zero == this.handle)
            {
                throw new InvalidOperationException("You must call CryptContext.Open first.");
            }
        }
    }
}
