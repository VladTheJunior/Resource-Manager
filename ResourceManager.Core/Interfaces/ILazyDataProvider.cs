using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceManager.Core.Interfaces
{
    public interface ILazyDataProvider
    {
        byte[] GetData();
    }
}
