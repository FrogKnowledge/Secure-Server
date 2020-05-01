using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace X509.Crypto {
    internal static class Win32ErrorHelper {
        internal static void ThrowExceptionIfGetLastErrorIsNotZero() {
            int lastWin32Error = Marshal.GetLastWin32Error();
            if (lastWin32Error != 0) {
                Marshal.ThrowExceptionForHR(Win32ErrorHelper.HResultFromWin32(lastWin32Error));
            }
        }

        private static int HResultFromWin32(int win32ErrorCode) {
            if (win32ErrorCode > 0) {
                return (win32ErrorCode & 65535) | -2147024896;
            }
            return win32ErrorCode;
        }
    }
}
