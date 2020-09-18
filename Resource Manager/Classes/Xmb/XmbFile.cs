using Resource_Manager.Classes.Alz4;
using Resource_Manager.Classes.L33TZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Resource_Manager.Classes.Xmb
{
    public class XMBFile
    {
        public class CustomEncodingStringWriter : StringWriter
        {
            public CustomEncodingStringWriter(Encoding encoding)
            {
                Encoding = encoding;
            }

            public override Encoding Encoding { get; }
        }

        public XmlDocument file { get; set; }
        private char[] decompressedHeader { get; set; }
        private uint dataLength { get; set; }
        private char[] unknown1 { get; set; }
        private uint unknown2 { get; set; }
        private uint version { get; set; }

        private uint numElements { get; set; }

        private uint numAttributes { get; set; }


        #region Convert To XML

        public static async Task<XMBFile> LoadXMBFile(Stream input)
        {
            XMBFile xmb = new XMBFile();

            xmb.file = new XmlDocument();

            var reader = new BinaryReader(input, Encoding.Default, true);

            reader.Read(xmb.decompressedHeader = new char[2], 0, 2);
            if (new string(xmb.decompressedHeader) != "X1")
            {
                throw new Exception("'X1' not detected - Not a valid XML file!");
            }

            xmb.dataLength = reader.ReadUInt32();

            reader.Read(xmb.unknown1 = new char[2], 0, 2);
            if (new string(xmb.unknown1) != "XR")
            {
                throw new Exception("'XR' not detected - Not a valid XML file!");
            }

            xmb.unknown2 = reader.ReadUInt32();
            xmb.version = reader.ReadUInt32();


            if (xmb.unknown2 != 4)
            {
                throw new Exception("'4' not detected - Not a valid XML file!");
            }

            if (xmb.version != 8)
            {
                throw new Exception("Not a valid Age of Empires 3 XML file!");
            }

            xmb.numElements = reader.ReadUInt32();

            // Now that we know how many elements there are we can read through
            // them and create them in our XMBFile object.
            List<string> elements = new List<string>();
            for (int i = 0; i < xmb.numElements; i++)
            {
                int elementLength = reader.ReadInt32();
                elements.Add(Encoding.Unicode.GetString(reader.ReadBytes(elementLength * 2)));
            }
            // Now do the same for attributes
            xmb.numAttributes = reader.ReadUInt32();
            List<string> attributes = new List<string>();
            for (int i = 0; i < xmb.numAttributes; i++)
            {
                int attributeLength = reader.ReadInt32();
                attributes.Add(Encoding.Unicode.GetString(reader.ReadBytes(attributeLength * 2)));
            }
            // Now parse the root element...

            await Task.Run(() =>
            {
                XmlElement root = xmb.parseNode(ref reader, elements, attributes);
                if (root != null)
                {
                    xmb.file.AppendChild(root);
                }
            });

            return xmb;
        }


        private XmlElement parseNode(ref BinaryReader reader, List<string> elements, List<string> attributes)
        {
            // Firstly check this is actually a valid node

            char[] nodeHeader;
            reader.Read(nodeHeader = new char[2], 0, 2);
            if (new string(nodeHeader) != "XN")
                throw new Exception("'XN' not found - Not a valid XMB file!");
            // Get the length (?)
            int length = reader.ReadInt32();
            // Get the inner text for this node
            int innerTextLength = reader.ReadInt32();
            string innerText = Encoding.Unicode.GetString(reader.ReadBytes(innerTextLength * 2));
            // Now get the int that refers to the name of this node.
            int nameID = reader.ReadInt32();
            // Create a new XmlElement for this node


            XmlElement node = file.CreateElement(elements[nameID]);
            node.InnerText = innerText;
            // Line number...
            int lineNumber = reader.ReadInt32();
            // Now read in the attributes
            int numAttributes = reader.ReadInt32();
            for (int i = 0; i < numAttributes; i++)
            {
                int attrID = reader.ReadInt32();
                XmlAttribute attribute = file.CreateAttribute(attributes[attrID]);

                int attributeLength = reader.ReadInt32();
                attribute.InnerText = Encoding.Unicode.GetString(reader.ReadBytes(attributeLength * 2));
                node.Attributes.Append(attribute);
            }
            // Now handle child nodes (recursively)
            int numChildren = reader.ReadInt32();

            for (int i = 0; i < numChildren; i++)
            {
                // Get the child node using this same method.
                XmlElement child = parseNode(ref reader, elements, attributes);
                // Append the newly created
                // child to this node.
                node.AppendChild(child);
            }
            // Once done return this node so it can be
            // added to its own parent.
            return node;
        }


        public static async Task<string> XmbToXmlAsync(byte[] data)
        {
            using (var fileStream = new MemoryStream(data, false))
            {

                XMBFile xmb = await XMBFile.LoadXMBFile(fileStream);
                using StringWriter sw = new CustomEncodingStringWriter(Encoding.UTF8);
                using XmlTextWriter textWriter = new XmlTextWriter(sw);

                textWriter.Formatting = Formatting.Indented;

                xmb.file.Save(textWriter);
                return sw.ToString();
            }
        }

        #endregion


        #region Convert To XMB
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

        public static async Task CreateXMBFileL33T(string filename)
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

        public static async Task CreateXMBFileALZ4(string filename)
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
            // need upgrade
            await Alz4Utils.CompressBytesAsAlz4Async(output.ToArray(), filename);
        }
        #endregion
    }
}
