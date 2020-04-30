using System;

namespace X509.Crypto {
    public enum KeyType {
        Exchange = 1,
        Signature
    }
    public enum ContextFlags : uint {
        CRYPT_NEWKEYSET=0x08,
        CRYPT_DELETEKEYSET=0x10,
        CRYPT_MACHINE_KEYSET=0x20,
        CRYPT_SILENT=0x40,
        CRYPT_DEFAULT_CONTAINER_OPTIONAL=0x80,
        CRYPT_VERIFYCONTEX=0xF0000000
    }

    public enum ProviderTypes : uint {
        PROV_RSA_FULL=0x01,
        PROV_RSA_AES,
        PROV_RSA_SIG,
        PROV_RSA_SCHANNEL,
        PROV_DSS,
        PROV_DSS_DH,
        PROV_DH_SCHANNEL,
        PROV_FORTEZZA,
        PROV_MS_EXCHANGE,
        PROV_SSL
    }
}