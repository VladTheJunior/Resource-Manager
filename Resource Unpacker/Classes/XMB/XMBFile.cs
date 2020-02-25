using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Resource_Unpacker.Classes.XMB
{
    public class XMBFile
    {
        public XmlDocument file { get; set; }
        private char[] decompressedHeader { get; set; }
        private uint dataLength { get; set; }
        private char[] unknown1 { get; set; }
        private uint unknown2 { get; set; }
        private uint version { get; set; }

        private uint numElements { get; set; }

        private uint numAttributes { get; set; }

        public async Task LoadXMBFile(Stream input)
        {
            file = new XmlDocument();
            XmlDeclaration dec = file.CreateXmlDeclaration("1.0", null, null);
            file.AppendChild(dec);

            using var reader = new BinaryReader(input, Encoding.Default, true);


            reader.Read(decompressedHeader = new char[2], 0, 2);
            if (new string(decompressedHeader) != "X1")
            {
                throw new Exception("'X1' not detected - Not a valid XML file!");
            }

            dataLength = reader.ReadUInt32();

            reader.Read(unknown1 = new char[2], 0, 2);
            if (new string(unknown1) != "XR")
            {
                throw new Exception("'XR' not detected - Not a valid XML file!");
            }

            unknown2 = reader.ReadUInt32();
            version = reader.ReadUInt32();


            if (unknown2 != 4)
            {
                throw new Exception("'4' not detected - Not a valid XML file!");
            }

            if (version != 8)
            {
                throw new Exception("Not a valid Age of Empires 3 XML file!");
            }

            numElements = reader.ReadUInt32();

            // Now that we know how many elements there are we can read through
            // them and create them in our XMBFile object.
            List<string> elements = new List<string>();
            for (int i = 0; i < numElements; i++)
            {
                int elementLength = reader.ReadInt32();
                elements.Add(Encoding.Unicode.GetString(reader.ReadBytes(elementLength * 2)));
            }
            // Now do the same for attributes
            numAttributes = reader.ReadUInt32();
            List<string> attributes = new List<string>();
            for (int i = 0; i < numAttributes; i++)
            {
                int attributeLength = reader.ReadInt32();
                attributes.Add(Encoding.Unicode.GetString(reader.ReadBytes(attributeLength * 2)));
            }
            // Now parse the root element...

            await Task.Run(() =>
            {
                XmlElement root = parseNode(input, elements, attributes);
                if (root != null)
                {
                    file.AppendChild(root);
                }
            });


        }


        private XmlElement parseNode(Stream input, List<string> elements, List<string> attributes)
        {
            using var reader = new BinaryReader(input, Encoding.Default, true);

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
                XmlElement child = parseNode(input, elements, attributes);
                // Append the newly created
                // child to this node.
                node.AppendChild(child);
            }
            // Once done return this node so it can be
            // added to its own parent.
            return node;
        }

    }
}
