using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CstPatcher
{
    public static class BinaryDataExtensions
    {
        public static bool IsEndOfFileReached(this Stream stream) =>
            stream.Position == stream.Length;

        public static bool IsEndOfFileReached(this BinaryReader reader) =>
            reader.BaseStream.IsEndOfFileReached();

        public static string ReadZeroTerminatedString(this BinaryReader reader, Encoding encoding = null)
        {
            encoding ??= Encoding.ASCII;
            var bytes = new List<byte>();

            while (!reader.IsEndOfFileReached())
            {
                byte ch = reader.ReadByte();
                if (ch == 0) break;
                bytes.Add(ch);
            }

            return encoding.GetString(bytes.ToArray());
        }

        public static long JumpTo(this Stream stream, long position)
        {
            long oldPos = stream.Position;
            stream.Position = position;
            return oldPos;
        }

        public static long JumpTo(this BinaryReader reader, long position) =>
            reader.BaseStream.JumpTo(position);

        public static long JumpTo(this BinaryWriter writer, long position) =>
            writer.BaseStream.JumpTo(position);

        public static void WriteZeroTerminatedString(this BinaryWriter writer, string text, Encoding encoding = null)
        {
            encoding ??= Encoding.ASCII;
            var bytes = encoding.GetBytes(text);

            writer.Write(bytes);
            writer.Write((byte)0);
        }
    }
}