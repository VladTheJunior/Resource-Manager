using ResourceManager.Core.Enum;
using ResourceManager.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ResourceManager.Core.Impl.AOE3
{
    internal class AOE3BarDataContainer : IDataContainer
    {
        // Static Definitions
        private static string[] Versions = {
            "", // 0
            "", // 1
            "AOE3 Complete Collection", // 2
            "AOE3 DE Dev", // 3
            "AOE3 DE 1st Beta", // 4
            "AOE3 DE Stress Test Beta" // 5
        };

        private static int[] ValidVersions =
        {
            2, 3, 4, 5
        };

        // Props
        private IList<ICommonFile> LoadedFiles = new List<ICommonFile>();
        private AOE3BarHeader Header = new AOE3BarHeader(ValidVersions[ValidVersions.Length - 1]);
        
        public IEnumerable<ICommonFile> Files => LoadedFiles;

        // TODO: Enforce Version Check
        public int Version { get => Header.Version; set => Header.Version = value; }

        public int[] GetValidVersionNumbers()
        {
            return ValidVersions;
        }

        public string GetVersionName(int version)
        {
            return Versions[version];
        }

        public void LoadArchiveFile(string path, ArchiveReadFlags flags)
        {
            using var file = File.OpenRead(path);
            var reader = new BinaryReader(file);
            Header = new AOE3BarHeader(reader);
            file.Seek(Header.FilesTableOffset, SeekOrigin.Begin);
            var rootNameLength = reader.ReadUInt32();
            string rootPath = Encoding.Unicode.GetString(reader.ReadBytes((int)rootNameLength * 2));
            uint numberOfRootFiles = reader.ReadUInt32();

            for (uint i = 0; i < numberOfRootFiles; i++)
            {
                AOE3BarCommonFile commonFile = new AOE3BarCommonFile();
                commonFile.Load(reader, Version, rootPath);
                commonFile.DataProvider = new AOE3BarDataProviderFromBarFile(path, commonFile.Offset, commonFile.FileSize2);
                LoadedFiles.Add(commonFile);
            }
        }

        public ICommonFile LoadFile(string path)
        {
            throw new NotImplementedException();
        }

        public void SaveArchiveFile(string path, ArchiveWriteFlags flags)
        {
            throw new NotImplementedException();
        }

        public void SaveFile(ICommonFile file, string path)
        {
            throw new NotImplementedException();
        }
    }
}
