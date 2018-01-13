using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace OpenCover.Framework.Utility
{
    /// <summary>
    /// Helper method(s) for identity-related tasks.
    /// </summary>
    public class IdentityHelper
    {
        /// <summary>
        /// Returns true if current Windows user is running as an administrator.
        /// </summary>
        /// <returns>True if current user running as administrator.</returns>
        public static bool IsRunningAsWindowsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Returns the account SID for the specified account name.
        /// </summary>
        /// <param name="accountName">The name of account whose SID is required.</param>
        /// <returns>SID for requested account, or null if SID unavailable.</returns>
        public static string LookupAccountSid(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new ArgumentException("Account name value cannot be null or whitespace.", nameof(accountName));
            }

            accountName = AdjustAccountName(accountName);

            var sidLen = 0;
            var sid = new byte[sidLen];
            var domainNameLen = 0;
            var domainName = new StringBuilder();
            LookupAccountName(Environment.MachineName, accountName, sid, ref sidLen, domainName, ref domainNameLen, out _);

            sid = new byte[sidLen];
            domainName = new StringBuilder(domainNameLen);
            if (!LookupAccountName(Environment.MachineName, accountName, sid, ref sidLen, domainName, ref domainNameLen, out _) ||
                !ConvertSidToStringSidW(sid, out var stringSidPtr))
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(stringSidPtr);
            }
            finally
            {
                LocalFree(stringSidPtr);
            }
        }

        private static string AdjustAccountName(string accountName)
        {
            if (accountName.StartsWith(@".\"))
            {
                return Environment.MachineName + accountName.Substring(1);
            }

            if (accountName.ToLowerInvariant().Contains("localsystem"))
            {
                return "NT Authority\\SYSTEM";
            }

            return accountName;
        }

        [DllImport("Kernel32.dll")]
        private static extern bool LocalFree(IntPtr ptr);

        [DllImport("Advapi32.dll")]
        private static extern bool ConvertSidToStringSidW(byte[] sid, out IntPtr stringSid);

        [DllImport("Advapi32.dll")]
        private static extern bool LookupAccountName(string machineName, string accountName, byte[] sid,
            ref int sidLen, StringBuilder domainName, ref int domainNameLen, out int peUse);
    }
}
