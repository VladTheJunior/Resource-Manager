using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Data;

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


        FileStream input;

        public async void saveFile(Entry file, string savePath)
        {
            // Could be doing some risky stuff here, so try...
            try
            {
                // Firstly, is the file parameter null?
                if (file == null)
                    throw new Exception("Supplied file was null - Could not save!");
                // Instantiate the input stream, opening the BAR
                // file specified in the barFilePath variable

                if (input == null)

                    input = new FileStream(barFilePath, FileMode.Open);
                // Locate the file within the BAR file.
                input.Seek(file.fileOffset, SeekOrigin.Begin);
                // Work out the full path of the file to be saved.
                //  string newPath = savePath;
                // Work out the directory path where the file is to be saved...
                // string dirPath = newPath.Substring(0, newPath.LastIndexOf('\\') + 1);
                // ...Now check that the directories exist! If the 

                Directory.CreateDirectory(Path.GetDirectoryName(file.fileName));

                // Now using a FileStream / BinaryWriter combo, write the data as it is
                // read from the BAR file to the new file (which will be overwritten if
                // it already exists, perhaps a bool check method could be used to make
                // sure the user doesn't overwrite something they've modded?)
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
                            }
                            else
                            {
                                buffer = new byte[bytesRemaining];
                                await input.ReadAsync(buffer, 0, (int)bytesRemaining);
                                bytesRemaining = 0;
                            }
                            // Write the contents of the buffer to the file!
                            writer.Write(buffer);
                        }
                        // Once we're finished we need to close the writer.
                        writer.Close();
                    }
                }

            }
            catch (Exception e)
            {
                // Have we caught an exception? Tell the user
                // about it...
                Console.WriteLine(e.Message);
            }

        }

        public async void readFile(Entry file)
        {
            // Could be doing some risky stuff here, so try...
            try
            {
                // Firstly, is the file parameter null?
                if (file == null)
                    throw new Exception("Supplied file was null - Could not save!");
                // Instantiate the input stream, opening the BAR
                // file specified in the barFilePath variable

                if (input == null)

                    input = new FileStream(barFilePath, FileMode.Open);
                // Locate the file within the BAR file.
                input.Seek(file.fileOffset, SeekOrigin.Begin);
                if (!file.isCompressed)
                {
                    var data = new byte[file.fileLength2];
                    input.Read(data, 0, data.Length);
                   
               //     return  new MemoryStream(data);
                }

                // Now using a FileStream / BinaryWriter combo, write the data as it is
                // read from the BAR file to the new file (which will be overwritten if
                // it already exists, perhaps a bool check method could be used to make
                // sure the user doesn't overwrite something they've modded?)
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
                            }
                            else
                            {
                                buffer = new byte[bytesRemaining];
                                await input.ReadAsync(buffer, 0, (int)bytesRemaining);
                                bytesRemaining = 0;
                            }
                            // Write the contents of the buffer to the file!
                            writer.Write(buffer);
                        }
                        // Once we're finished we need to close the writer.
                        writer.Close();
                    }
                }

            }
            catch (Exception e)
            {
                // Have we caught an exception? Tell the user
                // about it...
                Console.WriteLine(e.Message);
            }

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
            entriesCollection = new CollectionViewSource();

            entriesCollection.Source = entries;
            entriesCollection.Filter += Filter;
        }



    }
}
