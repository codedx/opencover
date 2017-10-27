using System.Security.Principal;

namespace OpenCover.Test
{
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
    }
}
