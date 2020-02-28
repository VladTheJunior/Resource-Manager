using Resource_Unpacker.Classes.Bar;
using Resource_Unpacker.Classes.Ddt;
using Resource_Unpacker.Classes.L33TZip;
using Resource_Unpacker.Classes.Xmb;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Archive_Unpacker.Classes.BarViewModel
{
    public class BarViewModel : INotifyPropertyChanged
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

            var entry = e.Item as BarEntry;
            e.Accepted = entry.FileName.ToLower().Contains(FilterText.ToLower());
        }


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

        public int extractingState { get; set; }

        private void ResetProgress(List<BarEntry> files)
        {
            CurrentProgress = 0;
            MaximumProgress = files.Sum(x => x.FileSize2);
        }

        public async Task saveFiles(List<BarEntry> files, string savePath, CancellationToken token)
        {
            ResetProgress(files);
            if (files.Count == 0) return;

            using var input = File.OpenRead(barFilePath);


            foreach (var file in files)
            {
                if (token.IsCancellationRequested)
                {
                    while (extractingState == 1)
                        await Task.Delay(1000);
                }
                if (token.IsCancellationRequested && extractingState == 2)
                {
                    ResetProgress(files);
                    return;
                }
                // Locate the file within the BAR file.
                input.Seek(file.Offset, SeekOrigin.Begin);

                Directory.CreateDirectory(Path.GetDirectoryName(file.FileName));

                using (FileStream output = new FileStream(file.FileName, FileMode.Create))
                {
                    using (BinaryWriter writer = new BinaryWriter(output))
                    {
                        // How many bytes are left? Well currently all of them!
                        int bytesRemaining = file.FileSize2;
                        // Define a 128K buffer (this is what AoE3Ed uses and
                        // it seems to work perfectly).
                        int bufferSize = 128 * 1024;
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


        public async Task readFile(BarEntry file)
        {

            // Firstly, is the file parameter null?
            if (file == null)
                return;
            PreviewDdt = null;
            Preview = null;
            PreviewImage = null;
            string ext = Path.GetExtension(file.FileName).ToUpper();


            if (ext == ".DDT")
            {
                using FileStream input = File.OpenRead(barFilePath);
                // Locate the file within the BAR file.
                input.Seek(file.Offset, SeekOrigin.Begin);
                var data = new byte[file.FileSize2];
                await input.ReadAsync(data, 0, data.Length);
                if (L33TZipUtils.IsL33TZipFile(data))
                    data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
                PreviewDdt = new DdtFile(data);
                return;
            }
            if (ext == ".BMP" || ext == ".PNG" || ext == ".CUR")
            {
                using FileStream input = File.OpenRead(barFilePath);
                // Locate the file within the BAR file.
                input.Seek(file.Offset, SeekOrigin.Begin);
                var data = new byte[file.FileSize2];
                await input.ReadAsync(data, 0, data.Length);
                if (L33TZipUtils.IsL33TZipFile(data))
                    data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(data))
                {

                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                PreviewImage = bitmap;
                return;
            }

            if (ext == ".XMB")
            {
                using FileStream input = File.OpenRead(barFilePath);
                // Locate the file within the BAR file.
                input.Seek(file.Offset, SeekOrigin.Begin);
                var data = new byte[file.FileSize2];
                await input.ReadAsync(data, 0, data.Length);
                if (L33TZipUtils.IsL33TZipFile(data))
                    data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
                Preview = new Document();
                Preview.Text = await XmbFileUtils.XmbToXmlAsync(data);
                Preview.SyntaxHighlighting = "XML";
                NotifyPropertyChanged("Preview");
                return;
            }
            if (ext == ".XML" || ext == ".SHP" || ext == ".LGT" || ext == ".XS" || ext == ".TXT" || ext == ".CFG")
            {
                using FileStream input = File.OpenRead(barFilePath);
                // Locate the file within the BAR file.
                input.Seek(file.Offset, SeekOrigin.Begin);
                var data = new byte[file.FileSize2];
                await input.ReadAsync(data, 0, data.Length);
                if (L33TZipUtils.IsL33TZipFile(data))
                    data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
                Preview = new Document();
                Preview.Text = System.Text.Encoding.UTF8.GetString(data);
                if (ext == ".XS")
                    Preview.SyntaxHighlighting = "C++";
                else
                    Preview.SyntaxHighlighting = "XML";
                NotifyPropertyChanged("Preview");
                return;
            }
            //Preview = new Document();
            //Preview.Text = ToHex(data);

            //Preview.SyntaxHighlighting = "C++";


            return;
        }

        static string ToHex(byte[] bytes)
        {

            string splitString = BitConverter.ToString(bytes).Replace("-", " ");
            return string.Join(string.Empty, splitString.Select((x, i) => i > 0 && i % 48 == 0 ? string.Format("\n{0}", x) : x.ToString())); ;
        }

        public string barFilePath { get; }
        public BarFile barFile { get; }

        private BitmapImage previewImage;
        public BitmapImage PreviewImage
        {
            get { return previewImage; }
            set
            {
                previewImage = value;
                NotifyPropertyChanged();
            }
        }

        private DdtFile previewDdt;
        public DdtFile PreviewDdt
        {
            get { return previewDdt; }
            set
            {
                previewDdt = value;
                NotifyPropertyChanged();
            }
        }

        public class Document
        {
            public string Text { get; set; }
            public string SyntaxHighlighting { get; set; }
        }

        private Document preview;
        public Document Preview
        {
            get { return preview; }
            set
            {
                preview = value;
                NotifyPropertyChanged();
            }
        }


        public string barFileName
        {
            get
            {
                return Path.GetFileName(barFilePath);
            }
        }

        public BarViewModel(string filename)
        {
            extractingState = 0;
            barFilePath = filename;
            barFile = new BarFile(filename);
            entriesCollection = new CollectionViewSource();
            ResetProgress(barFile.BarFileEntrys.ToList());
            entriesCollection.Source = barFile.BarFileEntrys;
            entriesCollection.Filter += Filter;
        }



    }
}
