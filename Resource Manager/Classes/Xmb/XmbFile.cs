using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Resource_Manager.Classes.Xmb
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

            var reader = new BinaryReader(input, Encoding.Default, true);


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
                XmlElement root = parseNode(ref reader, elements, attributes);
                if (root != null)
                {
                    file.AppendChild(root);
                }
            });


        }


        private XmlElement parseNode(ref BinaryReader reader, List<string> elements, List<string> attributes)
        {
            //        using var reader = new BinaryReader(input, Encoding.Default, true);

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






        /*

                public class XmlString
                {
                    public string Content { get; set; }
                    public int Length { get; set; }           
                }
                typedef std::map<utf16string, uint32_t> StringTable;

                static void ExtractStrings(XmlElement node, ref List<string> elements, ref List<string> attributes)
                {
                    if (elements.find(node->name) == elements.end())
                        elements.insert(std::make_pair(node->name, (uint32_t)elements.size()));

                    for (int i = 0; i < node.Attributes.Count; ++i)
                        if (attributes.find(node->attrs[i].name) == attributes.end())
                            attributes.insert(std::make_pair(node->attrs[i].name, (uint32_t)attributes.size()));

                    for (size_t i = 0; i < node->childs.size(); ++i)
                        ExtractStrings(node->childs[i], elements, attributes);
                }


                static void WriteNode(Stream stream, XMBFile* file, XMLElement* node, const StringTable& elements, const StringTable& attributes)
        {
            stream.Write("XN", 2);

            off_t Length_off = stream.Tell();
                stream.Write("????", 4);

            WriteUString(stream, node->text);

                uint32_t Name = elements.find(node->name)->second;
                stream.Write(&Name, 4);

            if (file->format == XMBFile::AOE3)
            {
                int32_t lineNum = node->linenum;
                stream.Write(&lineNum, 4);
            }

            uint32_t NumAttributes = (uint32_t)node->attrs.size();
            stream.Write(&NumAttributes, 4);
            for (uint32_t i = 0; i<NumAttributes; ++i)
            {
                uint32_t n = attributes.find(node->attrs[i].name)->second;
            stream.Write(&n, 4);
                WriteUString(stream, node->attrs[i].value);
        }

        uint32_t NumChildren = (uint32_t)node->childs.size();
        stream.Write(&NumChildren, 4);
            for (uint32_t i = 0; i<NumChildren; ++i)
                WriteNode(stream, file, node->childs[i], elements, attributes);

        off_t NodeEnd = stream.Tell();
        stream.Seek(Length_off, Stream::FROM_START);
            int Length = NodeEnd - (Length_off + 4);
        stream.Write(&Length, 4);
            stream.Seek(NodeEnd, Stream::FROM_START);
        }

        void SaveAsXMB(Stream stream)
        {
            stream.Write("X1", 2);

            off_t Length_off = stream.Tell();
            stream.Write("????", 4);

            stream.Write("XR", 2);

            int version[2] = { 4, -1 };

                version[1] = 8;

                version[1] = 8;

            stream.Write(version, 8);

            // Get the list of element/attribute names, sorted by first appearance
            StringTable ElementNames;
            StringTable AttributeNames;
            ExtractStrings(root, ElementNames, AttributeNames);


            // Convert into handy vector format for outputting
            std::vector<utf16string> ElementNamesByID;
            ElementNamesByID.resize(ElementNames.size());
            for (StringTable::iterator it = ElementNames.begin(); it != ElementNames.end(); ++it)
                ElementNamesByID[it->second] = it->first;

            // Output element names
            uint32_t NumElements = (uint32_t)ElementNamesByID.size();
            stream.Write(&NumElements, 4);
            for (uint32_t i = 0; i < NumElements; ++i)
                WriteUString(stream, ElementNamesByID[i]);


            // Convert into handy vector format for outputting
            std::vector<utf16string> AttributeNamesByID;
            AttributeNamesByID.resize(AttributeNames.size());
            for (StringTable::iterator it = AttributeNames.begin(); it != AttributeNames.end(); ++it)
                AttributeNamesByID[it->second] = it->first;

            // Output attribute names
            uint32_t NumAttributes = (uint32_t)AttributeNamesByID.size();
            stream.Write(&NumAttributes, 4);
            for (uint32_t i = 0; i < NumAttributes; ++i)
                WriteUString(stream, AttributeNamesByID[i]);

            // Output root node, plus all descendants
            WriteNode(stream, this, root, ElementNames, AttributeNames);

            // Fill in data-length field near the beginning
            off_t DataEnd = stream.Tell();
            stream.Seek(Length_off, Stream::FROM_START);
            int Length = DataEnd - (Length_off + 4);
            stream.Write(&Length, 4);
            stream.Seek(DataEnd, Stream::FROM_START);
        }
        */
    }
}
