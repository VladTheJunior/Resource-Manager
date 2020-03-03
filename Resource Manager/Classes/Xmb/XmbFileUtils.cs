using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Resource_Manager.Classes.Xmb
{
    public static class XmbFileUtils
    {

        public class CustomEncodingStringWriter : StringWriter
        {
            public CustomEncodingStringWriter(Encoding encoding)
            {
                Encoding = encoding;
            }

            public override Encoding Encoding { get; }
        }

        public static async Task<string> XmbToXmlAsync(byte[] data)
        {
            using (var fileStream = new MemoryStream(data, false))
            {

                XMBFile xmb = new XMBFile();
                await xmb.LoadXMBFile(fileStream);
                using StringWriter sw = new CustomEncodingStringWriter(Encoding.UTF8);
                using XmlTextWriter textWriter = new XmlTextWriter(sw);

                textWriter.Formatting = Formatting.Indented;

                xmb.file.Save(textWriter);
                return sw.ToString(); 
            }
        }

    }
}
