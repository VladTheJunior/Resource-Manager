using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resource_Manager.Classes.Bar
{
    public class BarFile
    {
        public static BarFile Load(string filename)
        {
            BarFile barFile = new BarFile();
            if (!File.Exists(filename))
                throw new Exception("BAR file does not exist!");
            using var file = File.OpenRead(filename);
            var reader = new BinaryReader(file);
            barFile.barFileHeader = new BarFileHeader(reader);

            file.Seek(barFile.barFileHeader.FilesTableOffset, SeekOrigin.Begin);
            var rootNameLength = reader.ReadUInt32();
            barFile.RootPath = Encoding.Unicode.GetString(reader.ReadBytes((int)rootNameLength * 2));
            barFile.NumberOfRootFiles = reader.ReadUInt32();

            var barFileEntrys = new List<BarEntry>();
            for (uint i = 0; i < barFile.NumberOfRootFiles; i++)
            {
                barFileEntrys.Add(BarEntry.Load(reader, barFile.barFileHeader.Version, barFile.RootPath));
            }
            barFile.BarFileEntrys = new ReadOnlyCollection<BarEntry>(barFileEntrys);
            return barFile;
        }

        public static async Task<BarFile> Create(string root, uint version)
        {
            BarFile barFile = new BarFile();

            if (!Directory.Exists(root))
                throw new Exception("Directory does not exist!");

            var fileInfos = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                        .Select(fileName => new FileInfo(fileName)).ToArray();



            if (root.EndsWith(Path.DirectorySeparatorChar.ToString()))
                root = root[0..^1];


            using (var fileStream = File.Open(root + ".bar", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = new BinaryWriter(fileStream))
                {
                    //Write Bar Header
                    var header = new BarFileHeader(fileInfos, version);
                    writer.Write(header.ToByteArray());

                    if (version == 4)
                        writer.Write(0);
                    //    writer.Write(0x7FF7);

                    //Write Files
                    var barEntrys = new List<BarEntry>();
                    foreach (var file in fileInfos)
                    {
                        var filePath = file.FullName;

                        barEntrys.Add(await BarEntry.Create(root, file, (int)writer.BaseStream.Position,
                            version));
                        using (var fileStream2 = File.Open(filePath, FileMode.Open, FileAccess.Read,
                            FileShare.Read))
                        {
                            using (var binReader = new BinaryReader(fileStream2))
                            {
                                var buffer = new byte[4096];
                                int read;
                                while ((read = binReader.Read(buffer, 0, buffer.Length)) > 0)
                                    writer.Write(buffer, 0, read);
                            }
                        }
                    }

                    barFile.barFileHeader = header;
                    barFile.RootPath = Path.GetFileName(root) + Path.DirectorySeparatorChar;
                    barFile.NumberOfRootFiles = (uint)barEntrys.Count;
                    barFile.BarFileEntrys = new ReadOnlyCollection<BarEntry>(barEntrys);

                    writer.Write(barFile.ToByteArray(version));
                }
            }

            return barFile;
        }

        public BarFileHeader barFileHeader { get; set; }

        public string RootPath { get; set; }

        public uint NumberOfRootFiles { get; set; }

        public IReadOnlyCollection<BarEntry> BarFileEntrys { get; set; }

        public byte[] ToByteArray(uint version)
        {
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(RootPath.Length);
                    bw.Write(Encoding.Unicode.GetBytes(RootPath));
                    bw.Write(NumberOfRootFiles);
                    foreach (var barFileEntry in BarFileEntrys)
                        bw.Write(barFileEntry.ToByteArray(version));
                    return ms.ToArray();
                }
            }
        }
    }
}
