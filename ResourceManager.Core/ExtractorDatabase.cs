using ResourceManager.Core.Enum;
using ResourceManager.Core.Impl.AOE3;
using ResourceManager.Core.Interfaces;
using System;

namespace ResourceManager.Core
{
    public class ExtractorDatabase
    {
        public IDataContainer CreateContainer(ArchiveFileFormat format)
        {
            switch (format)
            {
                case ArchiveFileFormat.AOE3:
                    return new AOE3BarDataContainer();
                default:
                    throw new NotImplementedException($"Archive Format {format} is not supported!");
            }
        }
    }
}
