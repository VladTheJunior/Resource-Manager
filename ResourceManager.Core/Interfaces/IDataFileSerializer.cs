using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ResourceManager.Core.Interfaces
{
    public interface IDataFileSerializer<Header>
    {
        void Read(Stream stream, Header headerInfo);

        void Write(Stream stream, Header headerInfo);
    }
}
