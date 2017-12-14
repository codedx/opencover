using System;

namespace CodePulse.Client.Init
{
    public class HandshakeException : Exception
    {
        public byte Reply { get; set; }

        public HandshakeException()
        {
        }

        public HandshakeException(string message, byte reply) 
            : base(message)
        {
            Reply = reply;
        }

        public HandshakeException(string message, byte reply, Exception innerException) 
            : base(message, innerException)
        {
            Reply = reply;
        }
    }
}
