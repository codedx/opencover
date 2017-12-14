using System;
using System.IO;

namespace CodePulse.Client.Util
{
    public static class MemoryStreamExtensions
    {
        public static void Reset(this MemoryStream memoryStream)
        {
            if (memoryStream == null) throw new ArgumentNullException(nameof(memoryStream));
            memoryStream.SetLength(0);
        }
    }
}
