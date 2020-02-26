using Resource_Unpacker.Classes.CompressedFiles;
using Resource_Unpacker.Classes.DDT;
using Resource_Unpacker.Classes.XMB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Archive_Unpacker.Classes.Bar
{
    public class BarFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private CollectionViewSource entriesCollection;
        private string filterText;

        public ICollectionView SourceCollection
        {
            get
            {
                return this.entriesCollection.View;
            }
        }



        public string FilterText
        {
            get
            {
                return filterText;
            }
            set
            {
                filterText = value;
                NotifyPropertyChanged();
                entriesCollection.View.Refresh();
            }
        }

        void Filter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterText))
            {
                e.Accepted = true;
                return;
            }

            var entry = e.Item as Entry;
            e.Accepted = entry.fileName.ToLower().Contains(FilterText.ToLower());
        }

        private string barFilePath;
        public char[] header { get; set; }
        public uint version { get; set; }
        public uint unknown1 { get; set; }
        public byte[] unknown2 { get; set; }
        public uint unknown3 { get; set; }
        public uint numFiles { get; set; }
        public uint unknown4 { get; set; }
        public long directoryOffset { get; set; }
        public int dirNameLength { get; set; }
        public string dirName { get; set; }
        public uint numFilesInDirectory { get; set; }




        private long currentProgress;
        public long CurrentProgress
        {
            get
            {
                return currentProgress;
            }
            set
            {
                currentProgress = value;
                NotifyPropertyChanged();
            }
        }


        private long maximumProgress;
        public long MaximumProgress
        {
            get
            {
                return maximumProgress;
            }
            set
            {
                maximumProgress = value;
                NotifyPropertyChanged();
            }
        }


        private void ResetProgress(List<Entry> files)
        {
            CurrentProgress = 0;
            MaximumProgress = files.Sum(x=>x.fileLength2);
        }

        private BitmapSource imageFile;
        public BitmapSource ImageFile
        {
            get
            {
                return imageFile;
            }
            set
            {
                imageFile = value;
                NotifyPropertyChanged();
            }
        }

        

        //     using FileStream file = new FileStream("file.bin", FileMode.Create, System.IO.FileAccess.Write);
        //  decompressedFile.CopyTo(file);
        //  decompressedFile.Seek(0, SeekOrigin.Begin);
        // decompressedFile = newStream;

        public async Task saveFiles(List<Entry> files, string savePath)
        {
            ResetProgress(files);
            if (files.Count == 0) return;

            using var input = File.OpenRead(barFilePath);


            foreach (var file in files)
            {
                // Locate the file within the BAR file.
                input.Seek(file.fileOffset, SeekOrigin.Begin);

                Directory.CreateDirectory(Path.GetDirectoryName(file.fileName));

                using (FileStream output = new FileStream(file.fileName, FileMode.Create))
                {
                    using (BinaryWriter writer = new BinaryWriter(output))
                    {
                        // How many bytes are left? Well currently all of them!
                        uint bytesRemaining = file.fileLength2;
                        // Define a 128K buffer (this is what AoE3Ed uses and
                        // it seems to work perfectly).
                        uint bufferSize = 128 * 1024;
                        byte[] buffer = new byte[bufferSize];
                        // Now loop through the file as long as there are bytes
                        // remaining.
                        while (bytesRemaining > 0)
                        {
                            // If the buffer is still smaller than the file
                            // then use it as is, then remove the buffer size
                            // from bytes remaining. Otherwise resize the
                            // buffer and read only what is needed before
                            // setting the buffer to zero to end the loop.
                            if (bytesRemaining >= bufferSize)
                            {
                                await input.ReadAsync(buffer, 0, (int)bufferSize);
                                bytesRemaining -= bufferSize;
                                CurrentProgress += bufferSize;
                            }
                            else
                            {
                                buffer = new byte[bytesRemaining];
                                await input.ReadAsync(buffer, 0, (int)bytesRemaining);
                                bytesRemaining = 0;
                                CurrentProgress += bytesRemaining;
                            }
                            // Write the contents of the buffer to the file!
                            writer.Write(buffer);

                            
                        }
                        // Once we're finished we need to close the writer.
                        writer.Close();
                    }
                }
            }
            ResetProgress(files);



        }

        public async Task<string> readFile(Entry file)
        {

            // Firstly, is the file parameter null?
            if (file == null)
                return "";

            using var input = File.OpenRead(barFilePath);
            // Locate the file within the BAR file.
            input.Seek(file.fileOffset, SeekOrigin.Begin);

            if (Path.GetExtension(file.fileName).ToUpper() == ".DDT")
            {
                var data = new byte[file.fileLength2];
                await input.ReadAsync(data, 0, data.Length);

                CompressedFile compressedFile = new CompressedFile();
                await compressedFile.LoadCompressedFile(new MemoryStream(data));


                DDTImage ddt = new DDTImage();
                ddt.LoadDDT(compressedFile.decompressedFile);
                if (ddt.TextureAlphaUsage == 0 && ddt.TextureType==1 && ddt.ImageCount == 1)
                    ImageFile = BitmapSource.Create(ddt.Width, ddt.Height, 96, 96, PixelFormats.Bgr24, null, ddt.pixels, 4 * ddt.Width);
                
                else
                    ImageFile = BitmapSource.Create(ddt.Width, ddt.Height, 96, 96, PixelFormats.Bgra32, null, ddt.pixels, 4 * ddt.Width);
                return ddt.Width.ToString() + "x" + ddt.Height.ToString() + Environment.NewLine + "Texture Type: " + ddt.TextureType.ToString() + Environment.NewLine + "Mipmaps count: " + ddt.ImageCount.ToString(); ;

            }

            if (Path.GetExtension(file.fileName).ToUpper() == ".XMB")
            {
                var data = new byte[file.fileLength2];
                await input.ReadAsync(data, 0, data.Length);

                CompressedFile compressedFile = new CompressedFile();
                await compressedFile.LoadCompressedFile(new MemoryStream(data));

                XMBFile xmb = new XMBFile();
                await xmb.LoadXMBFile(compressedFile.decompressedFile);
                using StringWriter sw = new StringWriter();
                using XmlTextWriter textWriter = new XmlTextWriter(sw);

                textWriter.Formatting = Formatting.Indented;

                xmb.file.Save(textWriter);
                return sw.ToString();

            }
            return "";
        }



        public ObservableCollection<Entry> entries { get; set; }

        public BarFile(string filename)
        {


            if (!File.Exists(filename))
                throw new Exception("BAR file does not exist!");
            barFilePath = filename;
            using var file = File.OpenRead(filename);
            var reader = new BinaryReader(file);
            reader.Read(this.header = new char[4], 0, 4);
            if (new string(header) != "ESPN")
                throw new Exception("'ESPN' not detected - Not a valid BAR file!");
            this.version = reader.ReadUInt32();
            this.unknown1 = reader.ReadUInt32();
            if (unknown1 != 0x44332211)
                throw new Exception("'0x44332211' not detected - Not a valid BAR file!");
            reader.Read(this.unknown2 = new byte[264]);
            bool allZeroes = true;
            foreach (byte b in unknown2)
            {
                if (b != 0)
                    allZeroes = false;
            }
            if (!allZeroes)
                throw new Exception("Successive zeroes not detected - Not a valid BAR file!");


            this.unknown3 = reader.ReadUInt32();
            numFiles = reader.ReadUInt32();

            if (numFiles == 0)
                throw new Exception("No files stored - This is an empty / invalid BAR file!");

            if (version == 4)
            {
                this.unknown4 = reader.ReadUInt32();
                this.directoryOffset = reader.ReadInt64();
            }
            else
            {
                this.directoryOffset = reader.ReadInt32();
            }

            if (directoryOffset == 0)
                throw new Exception("File table offset blank - Not a valid BAR file!");
            file.Seek(this.directoryOffset, SeekOrigin.Begin);
            this.dirNameLength = reader.ReadInt32();
            this.dirName = Encoding.Unicode.GetString(reader.ReadBytes(this.dirNameLength * 2));
            this.numFilesInDirectory = reader.ReadUInt32();

            if (numFilesInDirectory != numFiles)
                throw new Exception("File total mismatch - Not a valid BAR file!");

            entries = new ObservableCollection<Entry>();
            for (int i = 0; i < this.numFiles; i++)
            {
                Entry entry = new Entry();
                if (version == 4)
                    entry.fileOffset = reader.ReadInt64();
                else
                    entry.fileOffset = reader.ReadInt32();
                entry.fileLength1 = reader.ReadUInt32();
                entry.fileLength2 = reader.ReadUInt32();
                entry.year = reader.ReadUInt16();
                entry.month = reader.ReadUInt16();
                entry.dayOfWeek = reader.ReadUInt16();
                entry.day = reader.ReadUInt16();
                entry.hour = reader.ReadUInt16();
                entry.minute = reader.ReadUInt16();
                entry.second = reader.ReadUInt16();
                entry.msecond = reader.ReadUInt16();
                entry.fileNameLength = reader.ReadInt32();
                entry.fileName = this.dirName + Encoding.Unicode.GetString(reader.ReadBytes(entry.fileNameLength * 2));
                if (version == 4)
                    entry.isCompressed = Convert.ToBoolean(reader.ReadUInt32());
                entries.Add(entry);
            }
            ResetProgress(entries.ToList());
            entriesCollection = new CollectionViewSource();

            entriesCollection.Source = entries;
            entriesCollection.Filter += Filter;
        }



    }
}
