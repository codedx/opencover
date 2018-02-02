using System.Security.AccessControl;

namespace CodePulse.Console.EffectiveAccess
{
    internal class FileSecurityObject
    {
        public RawSecurityDescriptor SecurityDescriptor;
        public AccessChkResult Result;

        public FileSecurityObject(RawSecurityDescriptor sd)
        {
            SecurityDescriptor = sd;
            Result.GrantedAccess = NativeMethods.FileAccess.None;
        }

        public struct AccessChkResult
        {
            public NativeMethods.FileAccess GrantedAccess;
        };
    }
}
