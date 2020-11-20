using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CstPatcher
{
    internal class CstScript
    {
        private static readonly Encoding ShiftJis = Encoding.GetEncoding("Shift-Jis");
        private static readonly byte[] Signature = Encoding.ASCII.GetBytes("CatScene");

        public List<ScriptBlock> Blocks;
        public List<ScriptLine> Lines;

        public void DeleteLine(int index)
        {
            Lines.RemoveAt(index);
            for (int i = 0; i < Blocks.Count; i++)
            {
                uint length = Blocks[i].Length;
                uint start = Blocks[i].Start;
                uint end = start + length;

                if (start > index) start--;

                else if (end >= index) length--;

                else continue;

                if (length == 0)
                {
                    Blocks.RemoveAt(i);
                    i--;
                    continue;
                }

                // [   ] [ ]
                // 1 2 3 4 5
                //     ^

                Blocks[i] = new ScriptBlock
                {
                    Length = length,
                    Start = start
                };
            }
        }

        public void ReadFrom(Stream s, string baseName)
        {
            var fileHeader = new byte[16];
            if (s.Read(fileHeader, 0, fileHeader.Length) != fileHeader.Length)
                throw new Exception("Failed to read file header");
            for (int i = 0; i < Signature.Length; i++)
            {
                if (fileHeader[i] != Signature[i])
                    throw new Exception("Signature mismatch");
            }

            uint compressedSize = BitConverter.ToUInt32(fileHeader, 8);
            uint uncompressedSize = BitConverter.ToUInt32(fileHeader, 12);
            bool isCompressed = compressedSize != 0;

            using var m = new MemoryStream();
            using var r = new BinaryReader(m);
            if (isCompressed)
            {
                // Skip zlib header
                s.ReadByte();
                s.ReadByte();
                using var d = new DeflateStream(s, CompressionMode.Decompress);
                d.CopyTo(m);
            }
            else
            {
                s.CopyTo(m);
            }

            r.JumpTo(0);
            uint headerSize = 16;
            uint dataLength = r.ReadUInt32() + headerSize;
            uint blockCount = r.ReadUInt32();
            uint lineTableOffset = r.ReadUInt32() + headerSize;
            uint lineDataOffset = r.ReadUInt32() + headerSize;
            uint lineTableLength = lineDataOffset - lineTableOffset;
            uint lineCount = lineTableLength / 4;

            Debug.Assert(uncompressedSize == 0 || dataLength == uncompressedSize, "Data length and uncompressed size field don't match.");
            Debug.Assert(lineTableLength % 4 == 0, "Line table length must be a multiple of 4.");

            Blocks = new List<ScriptBlock>();

            for (int i = 0; i < blockCount; i++)
            {
                uint length = r.ReadUInt32();
                uint start = r.ReadUInt32();
                Blocks.Add(new ScriptBlock
                {
                    Start = start,
                    Length = length
                });
            }

            Lines = new List<ScriptLine>();

            for (int i = 0; i < lineCount; i++)
            {
                uint offset = r.ReadUInt32();
                long pos = r.JumpTo(lineDataOffset + offset);
                byte one = r.ReadByte();
                Debug.Assert(one == 1, $"Expected line data to start with a 1, got {one}.");
                var type = (ScriptLineType)r.ReadByte();
                string content = r.ReadZeroTerminatedString(ShiftJis);
                r.JumpTo(pos);
                Lines.Add(new ScriptLine
                {
                    ScriptName = baseName,
                    Id = i,
                    Type = type,
                    Content = content
                });
            }
        }

        public void WriteTo(Stream s, bool compress)
        {
            using var m = new MemoryStream();
            using var w = new BinaryWriter(m);

            // update this later
            w.Write(uint.MaxValue);
            w.Write(uint.MaxValue);
            w.Write(uint.MaxValue);
            w.Write(uint.MaxValue);

            uint blockCount = (uint)Blocks.Count;
            foreach (var block in Blocks)
            {
                w.Write(block.Length);
                w.Write(block.Start);
            }

            uint lineTableOffset = (uint)m.Position;
            uint lineOffset = 0;
            foreach (var line in Lines)
            {
                w.Write(lineOffset);
                lineOffset += 2 + (uint)ShiftJis.GetByteCount(line.Content) + 1;
            }

            uint lineDataOffset = (uint)m.Position;
            foreach (var line in Lines)
            {
                w.Write((byte)1);
                w.Write((byte)line.Type);
                w.WriteZeroTerminatedString(line.Content, ShiftJis);
            }

            uint dataLength = (uint)m.Position;

            // update header
            uint headerSize = 16;
            w.JumpTo(0);
            w.Write(dataLength - headerSize);
            w.Write(blockCount);
            w.Write(lineTableOffset - headerSize);
            w.Write(lineDataOffset - headerSize);

            s.JumpTo(0);
            s.Write(Signature, 0, Signature.Length);
            s.Write(BitConverter.GetBytes(uint.MaxValue), 0, 4);
            s.Write(BitConverter.GetBytes(uint.MaxValue), 0, 4);

            if (compress)
            {
                var z = new ZlibStream(s);
                m.JumpTo(0);
                m.CopyTo(z);
                z.Dispose();

                uint compressedSize = (uint)s.Position - 16;

                s.JumpTo(Signature.Length);
                s.Write(BitConverter.GetBytes(compressedSize), 0, 4);
                s.Write(BitConverter.GetBytes(0), 0, 4);
            }
            else
            {
                m.JumpTo(0);
                m.CopyTo(s);

                uint uncompressedSize = (uint)s.Position - 16;

                s.JumpTo(Signature.Length);
                s.Write(BitConverter.GetBytes(0), 0, 4);
                s.Write(BitConverter.GetBytes(uncompressedSize), 0, 4);
            }
        }
    }
}