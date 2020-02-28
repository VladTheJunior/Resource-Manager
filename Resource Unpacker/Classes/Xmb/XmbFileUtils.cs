using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Resource_Unpacker.Classes.Xmb
{
    public static class XmbFileUtils
    {

        public static async Task<string> XmbToXmlAsync(byte[] data)
        {
            using (var fileStream = new MemoryStream(data, false))
            {

                XMBFile xmb = new XMBFile();
                await xmb.LoadXMBFile(fileStream);
                using StringWriter sw = new StringWriter();
                using XmlTextWriter textWriter = new XmlTextWriter(sw);

                textWriter.Formatting = Formatting.Indented;

                xmb.file.Save(textWriter);
                return sw.ToString(); 
               // return Encoding.UTF8.GetBytes(result);
            }
        }

    }
}
