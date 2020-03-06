using Resource_Manager.Classes.L33TZip;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Resource_Manager.Classes.Bar
{
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
                return Path.GetExtension(FileName).ToUpper() != "" ? Path.GetExtension(FileName).ToUpper() : "UNKNOWN";
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
}
