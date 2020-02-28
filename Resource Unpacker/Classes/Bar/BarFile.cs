using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Resource_Unpacker.Classes.Bar
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

        public BarEntry(BinaryReader binaryReader, uint version, string rootPath)
        {
            if (version == 4)
                Offset = binaryReader.ReadInt64();
            else
                Offset = binaryReader.ReadInt32();
            FileSize = binaryReader.ReadInt32();
            FileSize2 = binaryReader.ReadInt32();
            LastWriteTime = new BarEntryLastWriteTime(binaryReader);
            var length = binaryReader.ReadUInt32();
            FileName = Path.Combine(rootPath, Encoding.Unicode.GetString(binaryReader.ReadBytes((int)length * 2)));
            if (version == 4)
                isCompressed = Convert.ToBoolean(binaryReader.ReadUInt32());
        }

   /*     public BarEntry(string filename, long offset, int filesize, uint version, BarEntryLastWriteTime modifiedDates, )
        {
            FileName = filename;
            Offset = offset;
            FileSize = filesize;
            FileSize2 = filesize;
            LastWriteTime = modifiedDates;
            if (version == 4)
                isCompressed = Convert.ToBoolean(binaryReader.ReadUInt32());
        }

        public BarEntry(string rootPath, FileInfo fileInfo, long offset, bool ignoreLastWriteTime = true)
        {
            rootPath = rootPath.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? rootPath
                : rootPath + Path.DirectorySeparatorChar;

            FileName = fileInfo.FullName.Replace(rootPath, string.Empty);
            Offset = offset;
            FileSize = (int)fileInfo.Length;
            FileSize2 = FileSize;
            LastWriteTime = ignoreLastWriteTime
                ? new BarEntryLastWriteTime(new DateTime(2011, 1, 1))
                : new BarEntryLastWriteTime(fileInfo.LastWriteTimeUtc);
        }
*/

        public string FileName { get; }

        public bool isCompressed { get;}

        public long Offset { get; }

        public int FileSize { get; }

        public int FileSize2 { get; }

        public BarEntryLastWriteTime LastWriteTime { get; }

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
                        bw.Write(isCompressed);
                    return ms.ToArray();
                }
            }
        }
    }

    public class BarFile
    {
        public BarFile(string filename)
        {
            if (!File.Exists(filename))
                throw new Exception("BAR file does not exist!");
            using var file = File.OpenRead(filename);
            var reader = new BinaryReader(file);
            BarFileHeader barFileHeader = new BarFileHeader(reader);

            file.Seek(barFileHeader.FilesTableOffset, SeekOrigin.Begin);
            var rootNameLength = reader.ReadUInt32();
            RootPath = Encoding.Unicode.GetString(reader.ReadBytes((int)rootNameLength * 2));
            NumberOfRootFiles = reader.ReadUInt32();
            var barFileEntrys = new List<BarEntry>();
            for (uint i = 0; i < NumberOfRootFiles; i++)
                barFileEntrys.Add(new BarEntry(reader, barFileHeader.Unk0, RootPath));
            BarFileEntrys = new ReadOnlyCollection<BarEntry>(barFileEntrys);
        }
        /*       public BarFile(string rootPath, IEnumerable<BarEntry> barEntrys)
               {
                   var enumerable = barEntrys as BarEntry[] ?? barEntrys.ToArray();
                   RootPath = rootPath;
                   NumberOfRootFiles = (uint)enumerable.Length;
                   BarFileEntrys = enumerable.ToList();
               }
       */
        public BarFile(BinaryReader binaryReader, uint version)
        {
            var rootNameLength = binaryReader.ReadUInt32();
            RootPath = Encoding.Unicode.GetString(binaryReader.ReadBytes((int)rootNameLength * 2));
            NumberOfRootFiles = binaryReader.ReadUInt32();
            var barFileEntrys = new List<BarEntry>();
            for (uint i = 0; i < NumberOfRootFiles; i++)
                barFileEntrys.Add(new BarEntry(binaryReader, version, RootPath));
            BarFileEntrys = new ReadOnlyCollection<BarEntry>(barFileEntrys);
        }

        public string RootPath { get; }

        public uint NumberOfRootFiles { get; }

        public IReadOnlyCollection<BarEntry> BarFileEntrys { get; }

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
