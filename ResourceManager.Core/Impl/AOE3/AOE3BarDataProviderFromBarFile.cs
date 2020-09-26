using ResourceManager.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ResourceManager.Core.Impl.AOE3
{
    internal class AOE3BarDataProviderFromBarFile : ILazyDataProvider
    {
        private string filename;
        private long offset;
        private int fileSize;

        public AOE3BarDataProviderFromBarFile(string filename, long offset, int fileSize)
        {
            this.filename = filename;
            this.offset = offset;
            this.fileSize = fileSize;
        }

        public byte[] GetData()
        {
            using FileStream input = File.OpenRead(filename);
            
            // Locate the file within the BAR file.
            input.Seek(offset, SeekOrigin.Begin);
            
            // Copy into buffer
            var data = new byte[fileSize];
            input.Read(data, 0, data.Length);
            return data;
        }
    }
}
