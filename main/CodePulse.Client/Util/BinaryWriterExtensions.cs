using System;
using System.IO;
using System.Text;

namespace CodePulse.Client.Util
{
    public static class BinaryWriterExtensions
    {
        public static void FlushAndLog(this BinaryWriter writer, string description)
        {
            writer.Flush();
        }

        public static void WriteBigEndian(this BinaryWriter writer, int value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer), "Expected non-null BinaryWriter");
            }

            if (!BitConverter.IsLittleEndian)
            {
                writer.Write(value);
                return;
            }
            WriteBytesBigEndian(writer, BitConverter.GetBytes(value));
        }

        public static void WriteBigEndian(this BinaryWriter writer, long value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer), "Expected non-null BinaryWriter");
            }

            if (!BitConverter.IsLittleEndian)
            {
                writer.Write(value);
                return;
            }
            WriteBytesBigEndian(writer, BitConverter.GetBytes(value));
        }

        public static void WriteUtfBigEndian(this BinaryWriter writer, string value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer), "Expected non-null BinaryWriter");
            }

            var bytes = Encoding.UTF8.GetBytes(value);

            writer.WriteBigEndian(Convert.ToInt16(bytes.Length));
            writer.Write(bytes);
        }

        public static void WriteBigEndian(this BinaryWriter writer, short value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer), "Expected non-null BinaryWriter");
            }

            if (!BitConverter.IsLittleEndian)
            {
                writer.Write(value);
                return;
            }
            WriteBytesBigEndian(writer, BitConverter.GetBytes(value));
        }

        public static void WriteBigEndian(this BinaryWriter writer, ushort value)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer), "Expected non-null BinaryWriter");
            }

            if (!BitConverter.IsLittleEndian)
            {
                writer.Write(value);
                return;
            }
            WriteBytesBigEndian(writer, BitConverter.GetBytes(value));
        }

        private static void WriteBytesBigEndian(BinaryWriter writer, byte[] bytes)
        {
            Array.Reverse(bytes);
            writer.Write(bytes);
        }
    }
}
