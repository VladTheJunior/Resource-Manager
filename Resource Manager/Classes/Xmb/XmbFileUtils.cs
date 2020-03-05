using Resource_Manager.Classes.L33TZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        public class XmlString
        {
            public string Content { get; set; }
            public int Size { get; set; }
        }

        static void ExtractStrings(XmlNode node, ref List<XmlString> elements, ref List<XmlString> attributes)
        {
            if (!elements.Any(x => x.Content == node.Name))
                elements.Add(new XmlString() { Content = node.Name, Size = elements.Count });

            foreach (XmlAttribute attr in node.Attributes)
                if (!attributes.Any(x => x.Content == attr.Name))
                    attributes.Add(new XmlString() { Content = attr.Name, Size = attributes.Count });

            int count = node.ChildNodes.Count;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                    ExtractStrings(child, ref elements, ref attributes);
            }

        }

        static void WriteNode(ref BinaryWriter writer, XmlNode node, List<XmlString> elements, List<XmlString> attributes)
        {
            try
            {
                writer.Write((byte)88);
                writer.Write((byte)78);


                long Length_off = writer.BaseStream.Position;
                // length in bytes
                writer.Write(0);
                if (node.HasChildNodes)
                {
                    if (node.FirstChild.NodeType == XmlNodeType.Text)
                    {

                        // innerTextLength
                        writer.Write(node.FirstChild.Value.Length);
                        // innerText
                        if (node.FirstChild.Value.Length != 0)
                            writer.Write(Encoding.Unicode.GetBytes(node.FirstChild.Value));
                    }
                    else
                    {
                        // innerTextLength
                        writer.Write(0);
                    }
                }
                else
                {

                    // innerTextLength
                    writer.Write(0);

                }
                // nameID
                int NameID = elements.FirstOrDefault(x => x.Content == node.Name).Size;
                writer.Write(NameID);

                /*      int lineNum = 0;
                      for (int i = 0; i < elements.Count; i++)
                          if (elements[i].Content == node.Name)
                          {
                              lineNum = i;
                              break;
                          }*/
                // Line number ... need recount
                writer.Write(0);


                int NumAttributes = node.Attributes.Count;
                // length attributes
                writer.Write(NumAttributes);
                for (int i = 0; i < NumAttributes; ++i)
                {

                    int n = attributes.FirstOrDefault(x => x.Content == node.Attributes[i].Name).Size;
                    // attrID
                    writer.Write(n);
                    // attributeLength
                    writer.Write(node.Attributes[i].InnerText.Length);
                    // attribute.InnerText
                    writer.Write(Encoding.Unicode.GetBytes(node.Attributes[i].InnerText));
                }

                int NumChildren = 0;
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {

                    if (node.ChildNodes[i].NodeType == XmlNodeType.Element)
                    {
                        NumChildren++;

                    }
                }
                // NumChildren nodes (recursively)
                writer.Write(NumChildren);
                for (int i = 0; i < node.ChildNodes.Count; ++i)
                    if (node.ChildNodes[i].NodeType == XmlNodeType.Element)
                    {

                        WriteNode(ref writer, node.ChildNodes[i], elements, attributes);

                    }
                long NodeEnd = writer.BaseStream.Position;
                writer.BaseStream.Seek(Length_off, SeekOrigin.Begin);

                writer.Write((int)(NodeEnd - (Length_off + 4)));
                writer.BaseStream.Seek(NodeEnd, SeekOrigin.Begin);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + Environment.NewLine + node.OuterXml, "Write error - Node " + node.Name, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static async Task CreateXMBFile(string filename)
        {
            using var output = new MemoryStream();

            var writer = new BinaryWriter(output, Encoding.Default, true);

            writer.Write((byte)88);
            writer.Write((byte)49);

            writer.Write(0);

            writer.Write((byte)88);
            writer.Write((byte)82);
            writer.Write(4);
            writer.Write(8);


            XmlDocument file = new XmlDocument();
            file.Load(filename);
            XmlNode rootElement = file.FirstChild;


            // Get the list of element/attribute names, sorted by first appearance
            List<XmlString> ElementNames = new List<XmlString>();
            List<XmlString> AttributeNames = new List<XmlString>();
            await Task.Run(() =>
            {
                ExtractStrings(file.DocumentElement, ref ElementNames, ref AttributeNames);

            });

            // Output element names
            int NumElements = ElementNames.Count;
            writer.Write(NumElements);
            for (int i = 0; i < NumElements; ++i)
            {
                writer.Write(ElementNames[i].Content.Length);
                writer.Write(Encoding.Unicode.GetBytes(ElementNames[i].Content));
            }

            int NumAttributes = AttributeNames.Count;
            writer.Write(NumAttributes);
            for (int i = 0; i < NumAttributes; ++i)
            {
                writer.Write(AttributeNames[i].Content.Length);
                writer.Write(Encoding.Unicode.GetBytes(AttributeNames[i].Content));
            }

            // Output root node, plus all descendants
            await Task.Run(() =>
            {
                WriteNode(ref writer, rootElement, ElementNames, AttributeNames);
            });


            // Fill in data-length field near the beginning
            long DataEnd = writer.BaseStream.Position;
            writer.BaseStream.Seek(2, SeekOrigin.Begin);
            int Length = (int)(DataEnd - (2 + 4));
            writer.Write(Length);
            writer.BaseStream.Seek(DataEnd, SeekOrigin.Begin);
            var name = Path.ChangeExtension(filename, "");
            if (!Path.HasExtension(name))
                filename = Path.ChangeExtension(filename, ".xml.xmb");
            else
                filename = Path.ChangeExtension(filename, ".xmb");
            await L33TZipUtils.CompressBytesAsL33TZipAsync(output.ToArray(), filename);
        }

    }
}
/*        static void WriteNode(ref BinaryWriter writer, XmlReader node, List<XmlString> elements, List<XmlString> attributes)
        {
            
            writer.Write((byte)88);
            writer.Write((byte)78);


            long Length_off = writer.BaseStream.Position;
            // length in bytes
            writer.Write(0);
            XmlReader r = node.ReadSubtree();
            if (r.NodeType == XmlNodeType.Text)
            {

                var text = node.ReadElementContentAsString();
                // innerTextLength
                writer.Write(text.Length);
                // innerText
                if (text.Length!= 0)
                    writer.Write(Encoding.Unicode.GetBytes(text));
            }
            else
            {
                // innerTextLength
                writer.Write(0);              
            }
            // nameID
            int NameID = elements.FirstOrDefault(x => x.Content == node.Name).Size;
            writer.Write(NameID);

            int lineNum = 0;
            for (int i = 0; i < elements.Count; i++)
                if (elements[i].Content == node.Name)
                {
                    lineNum = i;
                    break;
                }
            // Line number ... need recount
            writer.Write(((IXmlLineInfo)node).LineNumber + 2);


            int NumAttributes = node.AttributeCount;
            // length attributes
            writer.Write(NumAttributes);
            for (int i = 0; i < NumAttributes; ++i)
            {
                
                int n = attributes.FirstOrDefault(x => x.Content == node.Name).Size;

                var attr = node.GetAttribute(i);
                // attrID
                writer.Write(n);
                // attributeLength
                writer.Write(attr.Length);
                // attribute.InnerText
                writer.Write(Encoding.Unicode.GetBytes(attr));
            }
            int NumChildren = CountChildNodes(node, XmlNodeType.Element);
            writer.Write(NumChildren);
           
            node.MoveToElement();
            
            while (node.Read())
            {
                MessageBox.Show(node.Name);
                using (var innerReader = node.ReadSubtree())
                {
                    while (innerReader.Read())
                    {
                        if (innerReader.NodeType == XmlNodeType.Element)
                        {
                            WriteNode(ref writer, innerReader, elements, attributes);
                        }
                    }
                }


            }
             //       int NumChildren = node.ChildNodes.Count;
            // NumChildren nodes (recursively)
            
       //    for (int i = 0; i < NumChildren; ++i)
       //         if (node.ChildNodes[i].NodeType == XmlNodeType.Element)
       //             WriteNode(ref writer, node.ChildNodes[i], elements, attributes);
//
            long NodeEnd = writer.BaseStream.Position;
            writer.BaseStream.Seek(Length_off, SeekOrigin.Begin);
            
            writer.Write((int)(NodeEnd - (Length_off + 4)));
            writer.BaseStream.Seek(NodeEnd, SeekOrigin.Begin);
        }*/
