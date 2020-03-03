using Resource_Manager.Classes.L33TZip;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Resource_Manager.Classes.Bar
{
    public class BarEntryLastWriteTime
    {
        public BarEntryLastWriteTime(BinaryReader binaryReader)
        {
            Year = binaryReader.ReadInt16();
            Month = binaryReader.ReadInt16();
            DayOfWeek = binaryReader.ReadInt16();
            Day = binaryReader.ReadInt16();
            Hour = binaryReader.ReadInt16();
            Minute = binaryReader.ReadInt16();
            Second = binaryReader.ReadInt16();
            Msecond = binaryReader.ReadInt16();
        }

        public BarEntryLastWriteTime(DateTime dateTime)
        {
            Year = (short)dateTime.Year;
            Month = (short)dateTime.Month;
            DayOfWeek = (short)dateTime.DayOfWeek;
            Day = (short)dateTime.Day;
            Hour = (short)dateTime.Hour;
            Minute = (short)dateTime.Minute;
            Second = (short)dateTime.Second;
            Msecond = (short)dateTime.Millisecond;
        }

        public short Hour { get; }
        public short Minute { get; }
        public short Second { get; }
        public short Msecond { get; }
        public short Year { get; }
        public short Month { get; }
        public short Day { get; }
        public short DayOfWeek { get; }

        public byte[] ToByteArray()
        {
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(Year);
                    bw.Write(Month);
                    bw.Write(DayOfWeek);
                    bw.Write(Day);
                    bw.Write(Hour);
                    bw.Write(Minute);
                    bw.Write(Second);
                    bw.Write(Msecond);
                    return ms.ToArray();
                }
            }
        }
    }

    public class BarEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static BarEntry Load(BinaryReader binaryReader, uint version, string rootPath)
        {
            BarEntry barEntry = new BarEntry();

            if (version == 4)
                barEntry.Offset = binaryReader.ReadInt64();
            else
                barEntry.Offset = binaryReader.ReadInt32();
           
            barEntry.FileSize = binaryReader.ReadInt32();
            barEntry.FileSize2 = binaryReader.ReadInt32();

            barEntry.LastWriteTime = new BarEntryLastWriteTime(binaryReader);
            var length = binaryReader.ReadUInt32();

            barEntry.FileName = Encoding.Unicode.GetString(binaryReader.ReadBytes((int)length * 2));
            barEntry.FileNameWithRoot = Path.Combine(rootPath, barEntry.FileName);
            if (version == 4)
                barEntry.isCompressed = Convert.ToBoolean(binaryReader.ReadUInt32());
           

            return barEntry;
        }

        public static async Task<BarEntry> Create(string rootPath, FileInfo fileInfo, long offset, uint version)
        {
            BarEntry barEntry = new BarEntry();
            barEntry.FileNameWithRoot = Path.GetFileName(rootPath);
            rootPath = rootPath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? rootPath
                : rootPath + Path.DirectorySeparatorChar;


            barEntry.FileName = fileInfo.FullName.Replace(rootPath, string.Empty);
            barEntry.FileNameWithRoot = Path.Combine(barEntry.FileNameWithRoot, barEntry.FileName);
            barEntry.Offset = offset;

            if (version == 4)
            {
                barEntry.FileSize2 = (int)fileInfo.Length;
                barEntry.isCompressed = L33TZipUtils.IsL33TZipFile(fileInfo.FullName);
                if (barEntry.isCompressed)
                    barEntry.FileSize = (await L33TZipUtils.ExtractL33TZippedBytesAsync(fileInfo.FullName)).Length;
                else
                    barEntry.FileSize = barEntry.FileSize2;
            }
            else
            {
                barEntry.FileSize = (int)fileInfo.Length;
                barEntry.FileSize2 = barEntry.FileSize;
            }



            barEntry.LastWriteTime = new BarEntryLastWriteTime(fileInfo.LastWriteTimeUtc);

            return barEntry;
        }

        public string Extension
        {
            get
            {
                return Path.GetExtension(FileName).ToUpper() != ""? Path.GetExtension(FileName).ToUpper():"UNKNOWN";
            }
        }

        private string FileName { get; set; }

        public string FileNameWithRoot { get; set; }

        public bool isCompressed { get; set; }

        public long Offset { get; set; }

        public int FileSize { get; set; }

        public int FileSize2 { get; set; }

        public BarEntryLastWriteTime LastWriteTime { get; set; }

        public DateTime lastModifiedDate
        {
            get
            {
                return new DateTime(LastWriteTime.Year, LastWriteTime.Month, LastWriteTime.Day, LastWriteTime.Hour, LastWriteTime.Minute, LastWriteTime.Second, LastWriteTime.Msecond, DateTimeKind.Utc);
            }
        }

        public string fileFormat
        {
            get
            {
                return (isCompressed ? "Compressed " : "") + Path.GetExtension(FileName).ToUpper();
            }
        }

        public string fileNameWithoutPath
        {
            get
            {
                return Path.GetFileName(FileName);
            }
        }

        public byte[] ToByteArray(uint version)
        {
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    if (version == 4)
                        bw.Write(Offset);
                    else
                        bw.Write(Convert.ToInt32(Offset));
                    bw.Write(FileSize);
                    bw.Write(FileSize2);
                    bw.Write(LastWriteTime.ToByteArray());
                    bw.Write(FileName.Length);
                    bw.Write(Encoding.Unicode.GetBytes(FileName));
                    if (version == 4)
                        bw.Write(Convert.ToInt32(isCompressed));
                    return ms.ToArray();
                }
            }
        }
    }

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
                barFileEntrys.Add(BarEntry.Load(reader, barFile.barFileHeader.Unk0, barFile.RootPath));
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
                root = root.Substring(0, root.Length - 1);


            using (var fileStream = File.Open(root+".bar", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = new BinaryWriter(fileStream))
                {
                    //Write Bar Header
                    var header = new BarFileHeader(fileInfos, version);
                    writer.Write(header.ToByteArray());

                    if (version == 4)
                        writer.Write(0x7FF7);

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
                    barFile.RootPath = Path.GetFileName(root)+ Path.DirectorySeparatorChar;
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
