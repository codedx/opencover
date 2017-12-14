using System;
using System.IO;
using System.Text;

namespace CodePulse.Client.Util
{
    public static class BinaryReaderExtensions
    {
        public static Guid ReadGuid(this BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Expected non-null BinaryReader");
            }
            return new Guid(reader.ReadBytes(16));
        }

        public static Guid ReadGuidBigEndian(this BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Expected non-null BinaryReader");
            }

            if (!BitConverter.IsLittleEndian)
            {
                return ReadGuid(reader);
            }

            var guidBytes = reader.ReadBytes(16);
            Array.Reverse(guidBytes, 0, 4); // big endian int (part a)
            Array.Reverse(guidBytes, 4, 2); // big endian short (part b)
            Array.Reverse(guidBytes, 6, 2); // big endian short (part c)
            return new Guid(guidBytes);
        }

        public static short ReadInt16BigEndian(this BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Expected non-null BinaryReader");
            }
            return !BitConverter.IsLittleEndian ? reader.ReadInt16() : BitConverter.ToInt16(ReadBytesBigEndian(reader, sizeof(short)), 0);
        }

        public static int ReadInt32BigEndian(this BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Expected non-null BinaryReader");
            }
            return !BitConverter.IsLittleEndian ? reader.ReadInt32() : BitConverter.ToInt32(ReadBytesBigEndian(reader, sizeof(int)), 0);
        }

        public static string ReadUtfBigEndian(this BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader), "Expected non-null BinaryReader");
            }

            var stringBytesLength = reader.ReadInt16BigEndian();
            var stringBytes = ReadBytesBigEndian(reader, stringBytesLength);

            Array.Reverse(stringBytes);
            return Encoding.UTF8.GetString(stringBytes);
        }

        private static byte[] ReadBytesBigEndian(BinaryReader reader, int byteCount)
        {
            var bytes = reader.ReadBytes(byteCount);
            Array.Reverse(bytes);
            return bytes;
        }
    }
}
