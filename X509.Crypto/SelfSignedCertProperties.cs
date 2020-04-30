using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;

namespace X509.Crypto {
    public class SelfSignedCertProperties {
        public DateTime ValidFrom {
            get;
            set;
        }

        public DateTime ValidTo {
            get;
            set;
        }

        public X500DistinguishedName Name {
            get;
            set;
        }

        public int KeyBitLength {
            get;
            set;
        }

        public bool IsPrivateKeyExportable {
            get;
            set;
        }

        public SelfSignedCertProperties() {
            DateTime today = DateTime.Today;
            this.ValidFrom = today.AddDays(-1.0);
            this.ValidTo = today.AddYears(10);
            this.Name = new X500DistinguishedName("cn=self");
            this.KeyBitLength = 4096;
        }
    }
}
