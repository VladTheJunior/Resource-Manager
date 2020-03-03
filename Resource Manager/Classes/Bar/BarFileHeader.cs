
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Resource_Manager.Classes.Bar
{
    public class BarFileHeader
    {
        public BarFileHeader(IReadOnlyCollection<FileInfo> fileInfos, uint version)
        {
            Espn = "ESPN";
            Unk0 = version;
            Unk1 = 0x44332211;
            Unk2 = new byte[66 * 4];
            Checksum = 0;
            NumberOfFiles = (uint)fileInfos.Count;
            if (version == 4)
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

        public BarFileHeader(BinaryReader binaryReader)
        {
            var espn = new string(binaryReader.ReadChars(4));
            if (espn != "ESPN")
                throw new Exception("File is not a valid BAR Archive");

            Espn = espn;

            Unk0 = binaryReader.ReadUInt32();

            Unk1 = binaryReader.ReadUInt32();

            Unk2 = binaryReader.ReadBytes(66 * 4);

            Checksum = binaryReader.ReadUInt32();

            NumberOfFiles = binaryReader.ReadUInt32();

            if (Unk0 == 4)
            {
                Unk3 = binaryReader.ReadUInt32();
                FilesTableOffset = binaryReader.ReadInt64();
            }
            else
                FilesTableOffset = binaryReader.ReadInt32();

            FileNameHash = binaryReader.ReadUInt32();
        }

        public string Espn { get; }

        public uint Unk0 { get; }

        public uint Unk1 { get; }

        public byte[] Unk2 { get; }

        public uint Checksum { get; }

        public uint NumberOfFiles { get; }

        public uint Unk3 { get; set; }


        public long FilesTableOffset { get; }

        public uint FileNameHash { get; }

        
        public byte[] ToByteArray()
        {
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(Espn.ToCharArray());
                    bw.Write(Unk0);
                    bw.Write(Unk1);
                    bw.Write(Unk2);
                    bw.Write(Checksum);
                    bw.Write(NumberOfFiles);
                    if (Unk0 == 4)
                    {
                        bw.Write(Unk3);
                        bw.Write(FilesTableOffset);
                    }
                    else
                        bw.Write(Convert.ToInt32(FilesTableOffset));
                    bw.Write(FileNameHash);
                    return ms.ToArray();
                }
            }
        }
    }
}
