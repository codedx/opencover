namespace CodePulse.Client.Message
{
    public abstract class MessageTypes
    {
        public const byte Hello = 0;
        public const byte Configuration = 1;
        public const byte Start = 2;
        public const byte Stop = 3;
        public const byte Pause = 4;
        public const byte Unpause = 5;
        public const byte Suspend = 6;
        public const byte Unsuspend = 7;
        public const byte Heartbeat = 8;
        public const byte DataBreak = 9;
        public const byte MapThreadName = 10;
        public const byte MapMethodSignature = 11;
        public const byte MapException = 12;

        public const byte MethodEntry = 20;
        public const byte MethodExit = 21;
        public const byte Exception = 22;
        public const byte ExceptionBubble = 23;

        public const byte DataHello = 30;
        public const byte DataHelloReply = 31;

        public const byte ClassTransformed = 40;
        public const byte ClassIgnored = 41;
        public const byte ClassTransformFailed = 42;

        public const byte Marker = 50;

        public const byte Error = 99;
    }
}
