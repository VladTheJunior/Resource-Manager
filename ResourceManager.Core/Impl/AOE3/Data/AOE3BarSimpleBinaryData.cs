using ResourceManager.Core.Interfaces.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceManager.Core.Impl.AOE3.Data
{
    internal class AOE3BarSimpleBinaryData : IBinaryData
    {
        public byte[] Data { get; private set; }

        internal AOE3BarSimpleBinaryData(byte[] data)
        {
            Data = data;
        }


    }
}
