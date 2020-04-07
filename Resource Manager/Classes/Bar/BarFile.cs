using Force.Crc32;
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
        public async void ComputeCRC32(string filename)
        {
            if (!File.Exists(filename))
                throw new Exception("BAR file does not exist!");
            using var file = File.OpenRead(filename);
            var reader = new BinaryReader(file);
            file.Seek(barFileHeader.FilesTableOffset, SeekOrigin.Begin);
            foreach (var barEntry in BarFileEntrys)
            {

                reader.BaseStream.Seek(barEntry.Offset, SeekOrigin.Begin);
                byte[] data = reader.ReadBytes(barEntry.FileSize2);
                await Task.Run(() =>
                {
                    barEntry.CRC32 = Crc32Algorithm.Compute(data);
                }
                );
            }
        }
        public async static Task<BarFile> Load(string filename, bool doCRC32)
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
            if (doCRC32)
            {
                foreach (var barEntry in barFileEntrys)
                {

                    reader.BaseStream.Seek(barEntry.Offset, SeekOrigin.Begin);
                    byte[] data = reader.ReadBytes(barEntry.FileSize2);
                    await Task.Run(() =>
                    {
                        barEntry.CRC32 = Crc32Algorithm.Compute(data);
                    }
                    );
                }
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

                    if (version > 3)
                        writer.Write(0);
                    //    writer.Write(0x7FF7);

                    //Write Files
                    var barEntrys = new List<BarEntry>();
                    foreach (var file in fileInfos)
                    {
                        var filePath = file.FullName;
                        var entry = await BarEntry.Create(root, file, (int)writer.BaseStream.Position, version);

                        var data = await File.ReadAllBytesAsync(filePath);
                        await Task.Run(() =>
                        {
                            entry.CRC32 = Crc32Algorithm.Compute(data);
                        }
                );
                        writer.Write(data);

                        barEntrys.Add(entry);
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
