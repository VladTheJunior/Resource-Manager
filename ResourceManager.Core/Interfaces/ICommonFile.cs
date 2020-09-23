using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceManager.Core.Interfaces
{
    public interface ICommonFile
    {
        string FileName { get; set; }

        int FileSize { get; }

        DateTime LastWriteTime { get; set; }

        bool IsCompressed { get; }

        uint CRC32 { get; }
    }
}
