using ResourceManager.Core.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceManager.Core.Interfaces
{
    public interface IDataContainer
    {
        int[] GetValidVersionNumbers();

        string GetVersionName(int version);

        void LoadArchiveFile(string path, ArchiveReadFlags flags);

        ICommonFile LoadFile(string path);

        void SaveArchiveFile(string path, ArchiveWriteFlags flags);

        void SaveFile(ICommonFile file, string path);

        IEnumerable<ICommonFile> Files { get; }

        int Version { get; set; }

    }
}
