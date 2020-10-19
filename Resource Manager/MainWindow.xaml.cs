using Archive_Unpacker.Classes.BarViewModel;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Sample;
using ICSharpCode.AvalonEdit.Search;
using Resource_Manager.Classes.Alz4;
using Resource_Manager.Classes.Bar;
using Resource_Manager.Classes.Commands;
using Resource_Manager.Classes.Ddt;
using Resource_Manager.Classes.L33TZip;
using Resource_Manager.Classes.Sort;
using Resource_Manager.Classes.Xmb;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

public class RecentFile
{
    public string Title { get; set; }
    public string FileName { get; set; }
    public ICommand OnClickCommand { get; set; }
}

namespace Resource_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Variables
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool DoCRC32 = false;

        public ObservableCollection<RecentFile> recentFiles { get; set; } = new ObservableCollection<RecentFile>();

        private string fileContent;
        public string FileContent
        {
            get
            {
                return fileContent;
            }
            set
            {
                fileContent = value;
                NotifyPropertyChanged();
            }
        }

        public BarViewModel file { get; set; }


        private long selectedSize;
        public long SelectedSize
        {
            get
            {
                return selectedSize;
            }
            set
            {
                selectedSize = value;
                NotifyPropertyChanged();
            }
        }

        private double _zoomValue = 1.0;


        public int ZoomValue
        {
            get
            {
                return (int)(_zoomValue * 100);
            }
        }

        CancellationTokenSource CancelTokenSource;
        CancellationToken Token;

        private GridViewColumnHeader listViewSortCol = null;
        private SortAdorner listViewSortAdorner = null;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            SearchPanel.Install(XMLViewer);
            DataContext = this;

            tempColumn = (files.View as GridView).Columns[3];
            if (DoCRC32 == false)
            {
                (files.View as GridView).Columns.RemoveAt(3);
            }
            cdGrid.Width = new GridLength(675, GridUnitType.Pixel);
            files.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(Thumb_DragDelta), true);

            for (int i = 0; i < Math.Min(10, Settings.Default.RecentFiles.Count); i++)

                recentFiles.Add(new RecentFile() { FileName = Settings.Default.RecentFiles[i], Title = Path.GetFileName(Settings.Default.RecentFiles[i]), OnClickCommand = new RelayCommand<string>(openFile) });
        }


        FoldingManager foldingManager;

        private async void files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //files.IsEnabled = false;
            e.Handled = true;
            try
            {
                var entry = files.SelectedItem as BarEntry;
                if (entry == null)
                {
                    ImageViewer.Visibility = Visibility.Collapsed;
                    XMLViewer.Visibility = Visibility.Collapsed;
                    //files.IsEnabled = true;
                    return;
                }
                var entries = files.SelectedItems.Cast<BarEntry>().ToList();
                SelectedSize = entries.Sum(x => (long)x.FileSize2);
                await file.readFile(entry);

                if (file.Preview != null)
                {
                    XMLViewer.Text = file.Preview.Text;
                    if (foldingManager != null)
                    {
                        FoldingManager.Uninstall(foldingManager);
                        foldingManager = null;
                    }
                    foldingManager = FoldingManager.Install(XMLViewer.TextArea);
                    if (entry.Extension == ".XMB" || entry.Extension == ".XML" || entry.Extension == ".SHP" || entry.Extension == ".LGT" || entry.Extension == ".TXT" || entry.Extension == ".CFG" || entry.Extension == ".XAML" || entry.Extension == ".PY")
                    {
                        var foldingStrategy = new XmlFoldingStrategy();
                        foldingStrategy.UpdateFoldings(foldingManager, XMLViewer.Document);
                    }
                    else
                    if (entry.Extension == ".XS")
                    {
                        var foldingStrategy = new BraceFoldingStrategy();
                        foldingStrategy.UpdateFoldings(foldingManager, XMLViewer.Document);
                    }
                }

                if (entry.Extension == ".WAV")
                {
                    using (SoundPlayer player = new SoundPlayer())
                    {
                        player.Stream = file.audio;
                        player.Play();
                    }
                    ImageViewer.Visibility = Visibility.Collapsed;
                    XMLViewer.Visibility = Visibility.Collapsed;
                }
                else
                if (entry.Extension == ".DDT")
                {
                    ImagePreview.Source = file.PreviewDdt.Bitmap;
                    XMLViewer.Visibility = Visibility.Collapsed;
                    ImageViewer.Visibility = Visibility.Visible;
                }
                else
                if (entry.Extension == ".BMP" || entry.Extension == ".PNG" || entry.Extension == ".CUR" || entry.Extension == ".JPG")
                {
                    ImagePreview.Source = file.PreviewImage;
                    XMLViewer.Visibility = Visibility.Collapsed;
                    ImageViewer.Visibility = Visibility.Visible;
                }
                else
                if (entry.Extension == ".XMB" || entry.Extension == ".XML" || entry.Extension == ".SHP" || entry.Extension == ".LGT" || entry.Extension == ".XS" || entry.Extension == ".TXT" || entry.Extension == ".CFG" || entry.Extension == ".XAML" || entry.Extension == ".PY")
                {
                    ImageViewer.Visibility = Visibility.Collapsed;
                    XMLViewer.Visibility = Visibility.Visible;
                }
                else
                {
                    ImageViewer.Visibility = Visibility.Collapsed;
                    XMLViewer.Visibility = Visibility.Collapsed;

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //files.IsEnabled = true;
        }

        private async void openFile(string path = null)
        {
            mainMenu.IsEnabled = false;
            SpinnerFile.Visibility = Visibility.Visible;
            tbFile.Text = "Opening";
            var filePath = path;
            if (string.IsNullOrEmpty(path))
            {

                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Age of Empires 3 .BAR files (*.bar)|*.bar";
                if (openFileDialog.ShowDialog() == true)
                {
                    filePath = openFileDialog.FileName;
                }
                else
                {
                    SpinnerFile.Visibility = Visibility.Collapsed;
                    tbFile.Text = "File";
                    mainMenu.IsEnabled = true;
                    return;
                }
            }
            try
            {
                file = null;
                NotifyPropertyChanged("recentFiles");
                NotifyPropertyChanged("file");
                file = await BarViewModel.Load(filePath, DoCRC32);
                if (Settings.Default.RecentFiles.Contains(filePath))
                {
                    Settings.Default.RecentFiles.Remove(filePath);
                    recentFiles.Remove(recentFiles.SingleOrDefault(x => x.FileName == filePath));
                }
                recentFiles.Insert(0, new RecentFile() { FileName = filePath, Title = Path.GetFileName(filePath), OnClickCommand = new RelayCommand<string>(openFile) });
                Settings.Default.RecentFiles.Insert(0, filePath);
                Settings.Default.Save();
                NotifyPropertyChanged("recentFiles");
                NotifyPropertyChanged("file");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SpinnerFile.Visibility = Visibility.Collapsed;
            tbFile.Text = "File";
            mainMenu.IsEnabled = true;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            openFile();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {

            string targetURL = "https://github.com/VladTheJunior/Resource-Manager";
            var psi = new ProcessStartInfo
            {
                FileName = targetURL,
                UseShellExecute = true
            };
            Process.Start(psi);

        }

        private void files_Click(object sender, RoutedEventArgs e)
        {

            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            string sortBy = column.Tag.ToString();
            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                files.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            listViewSortCol = column;
            listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            files.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            int minWidth = 100;
            Thumb senderAsThumb = e.OriginalSource as Thumb;
            GridViewColumnHeader header
              = senderAsThumb.TemplatedParent as GridViewColumnHeader;
            if (header == null) return;
            if (header.Tag.ToString() == "isCompressed")
            {
                minWidth = 50;
            }
            if (header.Tag.ToString() == "FileNameWithRoot")
            {
                minWidth = 250;
            }
            if (header.Tag.ToString() == "FileSize2")
            {
                minWidth = 160;
            }
            if (header.Tag.ToString() == "CRC32")
            {
                minWidth = 100;
            }
            if (header.Tag.ToString() == "lastModifiedDate")
            {
                minWidth = 190;
            }
            if (header.Column.ActualWidth < minWidth)
            {
                e.Handled = true;
                header.Column.Width = minWidth;

            }
        }

        private async void extractMenuItem(object sender, RoutedEventArgs e)
        {
            if (file == null) return;
            List<BarEntry> entries;
            if ((sender as MenuItem).Tag.ToString() == "Selected")
                entries = files.SelectedItems.Cast<BarEntry>().ToList();
            else
                entries = file.barFile.BarFileEntrys.ToList();


            if (entries.Count != 0)
            {
                string RootPath;
                /*
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        RootPath = folderBrowserDialog.SelectedPath;
                    }
                    else
                        return;
                }
                */

                ExportDDT exportDDT = new ExportDDT();
                exportDDT.ShowDialog();

                if (exportDDT.Canceled)
                    return;

                RootPath = exportDDT.Path;

                mainMenu.IsEnabled = false;
                tbExtract.Text = "Extracting";
                SpinnerExtract.Visibility = Visibility.Visible;
                bPause.IsEnabled = true;
                bStop.IsEnabled = true;
                bRun.IsEnabled = false;
                bool decompress = false;
                if (file.barFile.barFileHeader.Version > 3)
                    decompress = MessageBox.Show("Do you want to decompress compressed files? (If you do not decompress them, you will not be able to open and edit these files.)", "Decompress files", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

                file.extractingState = 0;
                CancelTokenSource = new CancellationTokenSource();
                Token = CancelTokenSource.Token;
                try
                {

                    await Task.Run(async () =>
                    {
                        await file.saveFiles(entries, RootPath, decompress, Token, exportDDT);
                    });

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                bPause.IsEnabled = false;
                bStop.IsEnabled = false;
                bRun.IsEnabled = false;
                tbExtract.Text = "Extract";
                SpinnerExtract.Visibility = Visibility.Collapsed;
                mainMenu.IsEnabled = true;
            }
        }

        private void bPause_Click(object sender, RoutedEventArgs e)
        {
            bPause.IsEnabled = false;
            file.extractingState = 1;
            CancelTokenSource.Cancel();
            bRun.IsEnabled = true;
            bStop.IsEnabled = true;
        }

        private void bRun_Click(object sender, RoutedEventArgs e)
        {
            bRun.IsEnabled = false;
            CancelTokenSource = new CancellationTokenSource();
            Token = CancelTokenSource.Token;
            file.extractingState = 0;
            bStop.IsEnabled = true;
            bPause.IsEnabled = true;
        }

        private void bStop_Click(object sender, RoutedEventArgs e)
        {
            bStop.IsEnabled = false;
            bPause.IsEnabled = false;
            bRun.IsEnabled = false;
            file.extractingState = 2;
            CancelTokenSource.Cancel();
        }

        private void ImageViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (ZoomValue < 400)
                    _zoomValue += 0.1;
                else
                    return;
            }
            else
            {
                if (ZoomValue > 10)
                    _zoomValue -= 0.1;
                else
                    return;
            }
            NotifyPropertyChanged("ZoomValue");
            ScaleTransform scale = new ScaleTransform(_zoomValue, _zoomValue);
            ImagePreview.LayoutTransform = scale;
            e.Handled = true;
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _zoomValue = 1;
            ScaleTransform scale = new ScaleTransform(_zoomValue, _zoomValue);
            ImagePreview.LayoutTransform = scale;
            NotifyPropertyChanged("ZoomValue");
        }

        private async void TextBlock_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            //await DdtFileUtils.Ddt2PngAsync(@"D:\Development\Resource Manager\Resource Manager\bin\Release\netcoreapp3.1\Art\ui\alerts\alert_treatyend_bump.ddt");
        }

        private async void MenuItem_Click_7(object sender, RoutedEventArgs e)
        {
            mainMenu.IsEnabled = false;
            SpinnerFile.Visibility = Visibility.Visible;
            tbFile.Text = "Creating";
            CreateBarFileDialog createBarFileDialog = new CreateBarFileDialog();
            if (createBarFileDialog.ShowDialog() == true)
            {
                try
                {
                    file = null;
                    NotifyPropertyChanged("recentFiles");
                    NotifyPropertyChanged("file");
                    file = await BarViewModel.Create(createBarFileDialog.RootPath, createBarFileDialog.Version);
                    if (Settings.Default.RecentFiles.Contains(file.barFilePath))
                    {
                        Settings.Default.RecentFiles.Remove(file.barFilePath);
                        recentFiles.Remove(recentFiles.SingleOrDefault(x => x.FileName == file.barFilePath));
                    }
                    recentFiles.Insert(0, new RecentFile() { FileName = file.barFilePath, Title = Path.GetFileName(file.barFilePath), OnClickCommand = new RelayCommand<string>(openFile) });
                    Settings.Default.RecentFiles.Insert(0, file.barFilePath);
                    Settings.Default.Save();
                    NotifyPropertyChanged("recentFiles");
                    NotifyPropertyChanged("file");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            SpinnerFile.Visibility = Visibility.Collapsed;
            tbFile.Text = "File";
            mainMenu.IsEnabled = true;
        }

        private void MenuItem_Checked(object sender, RoutedEventArgs e)
        {
            files.Items.GroupDescriptions.Clear();
            files.Items.GroupDescriptions.Add(new PropertyGroupDescription("Extension"));
        }

        private void MenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            files.Items.GroupDescriptions.Clear();

        }

        private void MenuItem_Checked_1(object sender, RoutedEventArgs e)
        {
            gPreview.Visibility = Visibility.Visible;
            gsSplitter.Visibility = Visibility.Visible;
        }

        private void MenuItem_Unchecked_1(object sender, RoutedEventArgs e)
        {
            gPreview.Visibility = Visibility.Collapsed;
            gsSplitter.Visibility = Visibility.Collapsed;
        }

        private async void convertFiles(object sender, RoutedEventArgs e)
        {
            mainMenu.IsEnabled = false;
            SpinnerConvert.Visibility = Visibility.Visible;
            tbConvert.Text = "Converting";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;

            string operationType = (sender as MenuItem).Tag.ToString();
            //await DdtFileUtils.Ddt2PngAsync(@"D:\Development\Resource Manager\Resource Manager\bin\Release\netcoreapp3.1\Art\ui\alerts\alert_treatyend_bump.ddt");

            if (operationType == "topng")
            {
                openFileDialog.Filter = "Age of Empires 3 ddt files (*.ddt)|*.ddt";
                if (openFileDialog.ShowDialog() == true)
                {
                    foreach (var file in openFileDialog.FileNames)
                    {
                        try
                        {
                            await DdtFileUtils.Ddt2PngAsync(file);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            if (operationType == "toxml")
            {
                openFileDialog.Filter = "Age of Empires 3 xmb files (*.xmb)|*.xmb";

                if (openFileDialog.ShowDialog() == true)
                {
                    foreach (var file in openFileDialog.FileNames)
                    {
                        try
                        {
                            var data = await File.ReadAllBytesAsync(file);


                            if (Alz4Utils.IsAlz4File(data))
                            {
                                data = await Alz4Utils.ExtractAlz4BytesAsync(data);
                            }
                            else
                            {
                                if (L33TZipUtils.IsL33TZipFile(data))
                                    data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
                            }


                            using MemoryStream stream = new MemoryStream(data);
                            XMBFile xmb = await XMBFile.LoadXMBFile(stream);
                            var newName = file.Replace(".xml.xmb", ".xml", StringComparison.OrdinalIgnoreCase).Replace(".xmb", ".xml", StringComparison.OrdinalIgnoreCase);
                            xmb.file.Save(newName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            if (operationType == "toxmbde")
            {
                openFileDialog.Filter = "Age of Empires 3 xml files (*.xml)|*.xml";

                if (openFileDialog.ShowDialog() == true)
                {
                    foreach (var file in openFileDialog.FileNames)
                    {
                        try
                        {
                            await XMBFile.CreateXMBFileALZ4(file);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            if (operationType == "toxmbcc")
            {
                openFileDialog.Filter = "Age of Empires 3 xml files (*.xml)|*.xml";
                if (openFileDialog.ShowDialog() == true)
                {
                    foreach (var file in openFileDialog.FileNames)
                    {
                        try
                        {
                            await XMBFile.CreateXMBFileL33T(file);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Conversion error - " + Path.GetFileName(file), MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }



            tbConvert.Text = "Convert";
            SpinnerConvert.Visibility = Visibility.Collapsed;
            mainMenu.IsEnabled = true;
        }

        GridViewColumn tempColumn;
        private void MenuItem_Checked_2(object sender, RoutedEventArgs e)
        {
            (files.View as GridView).Columns.Insert(3, tempColumn);
            tempColumn.Width = 100;
            cdGrid.Width = new GridLength(775, GridUnitType.Pixel);
            if (file != null)
            {
                if (!file.IsCRC32Checked)
                {
                    file.barFile.ComputeCRC32(file.barFilePath);
                    file.IsCRC32Checked = true;
                }
            }
            DoCRC32 = true;
        }

        private void MenuItem_Unchecked_2(object sender, RoutedEventArgs e)
        {
            tempColumn = (files.View as GridView).Columns[3];
            (files.View as GridView).Columns.RemoveAt(3);
            cdGrid.Width = new GridLength(675, GridUnitType.Pixel);
            DoCRC32 = false;
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            CompareWindow window = new CompareWindow();
            window.Show();
        }

        private void TextBlock_MouseDown_2(object sender, MouseButtonEventArgs e)
        {
        }

        Point point = new Point();
        bool validPoint = false;

        private void ImagePreview_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point newPos = e.GetPosition(this);

                if (validPoint)
                {
                    ImagePreview.RenderTransform = new TranslateTransform(newPos.X - point.X, newPos.Y - point.Y);
                }

                if (!validPoint)
                {
                    point = newPos;
                    validPoint = true;
                    
                    if(ImagePreview.RenderTransform != null)
                    {
                        point.X -= ImagePreview.RenderTransform.Value.OffsetX;
                        point.Y -= ImagePreview.RenderTransform.Value.OffsetY;
                    }
                }
            }
            else
            {
                validPoint = false;
            }
        }
    }

    #region Value Converters

    public class MathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
              object parameter, CultureInfo culture)
        {
            return (double)value * 100;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class RunEnabledColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((bool)value)
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF388934"));
                else
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF707070"));
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PauseEnabledColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((bool)value)
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF00539c"));
                else
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF707070"));
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class StopEnabledColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if ((bool)value)
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFA1260D"));
                else
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF707070"));
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class HighlightingDefinitionConverter : IValueConverter
    {
        private static readonly HighlightingDefinitionTypeConverter Converter = new HighlightingDefinitionTypeConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return Converter.ConvertFrom(value);
            }
            else
                return Converter.ConvertFrom("XML");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Converter.ConvertToString(value);
        }
    }
    #endregion
}

