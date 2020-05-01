using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace X509.Crypto {
    public class CryptContextHelper {

        public static byte[] CreateX509Certificate(string name, string password, DateTime validTo) {
            X509Certificate2 tempCert = null;
            SelfSignedCertProperties props = new SelfSignedCertProperties {
                IsPrivateKeyExportable = true,
                KeyBitLength = 1024,
                Name = new X500DistinguishedName("CN="+name),
                ValidFrom=DateTime.Now,
                ValidTo=validTo
            };
            using (CryptContext ctx = new CryptContext()) {
                ctx.Open();
                tempCert = ctx.CreateSelfSignedCertificate(props);
            }
            return tempCert.Export(X509ContentType.Pfx, password);
        }
    }
}
