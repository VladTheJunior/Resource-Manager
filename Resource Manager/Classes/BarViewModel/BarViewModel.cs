using Resource_Manager.Classes.Bar;
using Resource_Manager.Classes.Ddt;
using Resource_Manager.Classes.L33TZip;
using Resource_Manager.Classes.Xmb;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
            e.Accepted = entry.FileNameWithRoot.ToLower().Contains(FilterText.ToLower());
        }


        private double currentProgress;
        public double CurrentProgress
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


        public int extractingState { get; set; }

        private void ResetProgress()
        {
            CurrentProgress = 0;

        }

        public async Task saveFiles(List<BarEntry> files, string savePath, bool Decompress, CancellationToken token)
        {
            ResetProgress();
            if (files.Count == 0) return;

            using var input = File.OpenRead(barFilePath);

            var filesSize = files.Sum(x => x.FileSize2);

            foreach (var file in files)
            {
                if (token.IsCancellationRequested)
                {
                    while (extractingState == 1)
                        await Task.Delay(1000);
                }
                if (token.IsCancellationRequested && extractingState == 2)
                {
                    ResetProgress();
                    return;
                }
                // Locate the file within the BAR file.
                input.Seek(file.Offset, SeekOrigin.Begin);

                Directory.CreateDirectory(Path.Combine(savePath, Path.GetDirectoryName(file.FileNameWithRoot)));



                var data = new byte[file.FileSize2];
                await input.ReadAsync(data, 0, data.Length);

                // 
                if (file.Extension != ".XMB" && file.isCompressed && Decompress)
                {
                    data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
                }

                await File.WriteAllBytesAsync(Path.Combine(savePath, file.FileNameWithRoot), data);
                CurrentProgress += (double)file.FileSize2 / filesSize;
            }
            ResetProgress();



        }




        public async Task readFile(BarEntry file)
        {

            // Firstly, is the file parameter null?
            if (file == null)
                return;
            PreviewDdt = null;
            Preview = null;
            PreviewImage = null;


            if (file.Extension == ".WAV"/* || file.Extension == ".MP3"*/)
            {
                using FileStream input = File.OpenRead(barFilePath);
                // Locate the file within the BAR file.
                input.Seek(file.Offset, SeekOrigin.Begin);
                var data = new byte[file.FileSize2];
                await input.ReadAsync(data, 0, data.Length);
                //File.WriteAllBytes(file.fileNameWithoutPath, data);
                audio = new MemoryStream(data);
                if (file.isCompressed)
                {
                    //   AudioFileReader audioFileReader = new AudioFileReader();
                    //  DirectSoundOut directSoundOut = new DirectSoundOut();

                    // audioFileReader.
                    audio = new MemoryStream(data);

                }
                else
                    audio = new MemoryStream(data);

                return;
            }
            if (file.Extension == ".DDT")
            {
                using FileStream input = File.OpenRead(barFilePath);
                // Locate the file within the BAR file.
                input.Seek(file.Offset, SeekOrigin.Begin);
                var data = new byte[file.FileSize2];
                await input.ReadAsync(data, 0, data.Length);
                if (L33TZipUtils.IsL33TZipFile(data))
                    data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
                PreviewDdt = new DdtFile(data, true);
                return;
            }
            if (file.Extension == ".BMP" || file.Extension == ".PNG" || file.Extension == ".CUR" || file.Extension == ".JPG")
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

            if (file.Extension == ".XMB")
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
            if (file.Extension == ".XAML" || file.Extension == ".XML" || file.Extension == ".SHP" || file.Extension == ".LGT" || file.Extension == ".XS" || file.Extension == ".TXT" || file.Extension == ".CFG")
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
                if (file.Extension == ".XS")
                    Preview.SyntaxHighlighting = "C++";
                else
                    Preview.SyntaxHighlighting = "XML";
                NotifyPropertyChanged("Preview");
                return;
            }

            return;
        }

        public MemoryStream audio { get; set; }

        public string barFilePath { get; set; }
        public BarFile barFile { get; set; }

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
                return Path.GetFileName(barFilePath) + (barFile.barFileHeader.Unk0 == 4 ? " (Definitive Edition)" : " (Complete Collection)");
            }
        }

        public static BarViewModel Load(string filename)
        {
            BarViewModel barViewModel = new BarViewModel();
            barViewModel.extractingState = 0;
            barViewModel.barFilePath = filename;

            barViewModel.barFile = BarFile.Load(filename);

            barViewModel.ResetProgress();

            barViewModel.entriesCollection = new CollectionViewSource();
            barViewModel.entriesCollection.Source = barViewModel.barFile.BarFileEntrys;
            barViewModel.entriesCollection.Filter += barViewModel.Filter;
            return barViewModel;
        }

        public static async Task<BarViewModel> Create(string rootFolder, uint version)
        {
            BarViewModel barViewModel = new BarViewModel();
            barViewModel.extractingState = 0;

            var filename = rootFolder;
            if (rootFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                filename = rootFolder.Substring(0, rootFolder.Length - 1);

            barViewModel.barFilePath = filename + ".bar";
            barViewModel.barFile = await BarFile.Create(rootFolder, version);
            barViewModel.entriesCollection = new CollectionViewSource();
            barViewModel.ResetProgress();

            barViewModel.entriesCollection.Source = barViewModel.barFile.BarFileEntrys;
            barViewModel.entriesCollection.Filter += barViewModel.Filter;
            return barViewModel;
        }



    }
}
