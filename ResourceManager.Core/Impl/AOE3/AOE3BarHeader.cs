using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ResourceManager.Core.Impl.AOE3
{
    internal class AOE3BarHeader
    {
        public int Version { get; set; }

        public string Espn { get; }

        public uint Unk1 { get; }

        public byte[] Unk2 { get; }

        public uint Checksum { get; }

        public uint NumberOfFiles { get; }

        public uint Unk3 { get; set; }

        public long FilesTableOffset { get; }

        public uint FileNameHash { get; }

        public AOE3BarHeader(IReadOnlyCollection<FileInfo> fileInfos, int version)
        {
            Espn = "ESPN";
            Version = version;
            Unk1 = 0x44332211;
            Unk2 = new byte[66 * 4];
            Checksum = 0;
            NumberOfFiles = (uint)fileInfos.Count;
            if (Version > 3)
            {
                Unk3 = 0;
                FilesTableOffset = 304 + fileInfos.Sum(key => key.Length);
            }
            else
            {
                FilesTableOffset = 292 + (int)fileInfos.Sum(key => key.Length);
            }
            FileNameHash = 0;
        }

        public AOE3BarHeader(int version)
        {
            Version = version;
            Unk1 = 0x44332211;
            Unk2 = new byte[66 * 4];
            Checksum = 0;
            NumberOfFiles = 0;
        }
        public AOE3BarHeader(BinaryReader binaryReader)
        {
            var espn = new string(binaryReader.ReadChars(4));
            if (espn != "ESPN")
                throw new InvalidOperationException("File is not a valid BAR Archive");

            Espn = espn;

            Version = binaryReader.ReadInt32();

            if (Version != 2 && Version != 4 && Version != 5)
                throw new InvalidOperationException("Version " + Version.ToString() + " of the BAR file is not supported. Please contact the developer");
            Unk1 = binaryReader.ReadUInt32();

            Unk2 = binaryReader.ReadBytes(66 * 4);

            Checksum = binaryReader.ReadUInt32();

            NumberOfFiles = binaryReader.ReadUInt32();

            if (Version > 3)
            {
                Unk3 = binaryReader.ReadUInt32();
                FilesTableOffset = binaryReader.ReadInt64();
            }
            else
                FilesTableOffset = binaryReader.ReadInt32();

            FileNameHash = binaryReader.ReadUInt32();
        }
    }
}
