using ResourceManager.Core.Impl.AOE3.Data;
using ResourceManager.Core.Interfaces;
using ResourceManager.Core.Interfaces.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ResourceManager.Core.Impl.AOE3
{
    internal class AOE3BarCommonFile : ICommonFile
    {
        public string FileName { get; set; } = "";

        public int FileSize { get; private set; } = 0;

        public DateTime LastWriteTime { get; set; } = DateTime.Now;

        public bool IsCompressed { get; private set; } = false;

        public uint CRC32 { get; private set; } = 0;

        public long Offset { get; set; } = 0;

        public int FileSize2 { get; set; } = 0;

        public int FileSize3 { get; set; } = 0;

        private string FileNameWithRoot { get; set; } = "";

        public ILazyDataProvider DataProvider { get; set; } = null;

        // TODO: IBinaryDataProvider?
        internal void Load(BinaryReader binaryReader, int version, string rootPath)
        {
            if (version > 3)
            {
                Offset = binaryReader.ReadInt64();
            }
            else
                Offset = binaryReader.ReadInt32();

            FileSize = binaryReader.ReadInt32();
            FileSize2 = binaryReader.ReadInt32();

            if (version == 5)
            {
                FileSize3 = binaryReader.ReadInt32();
            }

            LastWriteTime = (new AOE3BarLastWriteTime(binaryReader)).AsDateTime();
            var length = binaryReader.ReadUInt32();

            FileName = Encoding.Unicode.GetString(binaryReader.ReadBytes((int)length * 2));
            FileNameWithRoot = Path.Combine(rootPath, FileName);
            if (version > 3)
                IsCompressed = Convert.ToBoolean(binaryReader.ReadUInt32());
            else
                IsCompressed = FileName.EndsWith(".xmb", StringComparison.OrdinalIgnoreCase);
        }

        internal IBinaryData LoadData()
        {
            if (DataProvider == null)
                throw new InvalidOperationException($"No data provider is set for {FileName} [Full: {FileNameWithRoot}]");

            byte[] fullData = DataProvider.GetData();

            // TODO: Add other formats
            // -- Compressed
            // -- DDT
            // -- XML

            return new AOE3BarSimpleBinaryData(fullData);
        }
    }
}
