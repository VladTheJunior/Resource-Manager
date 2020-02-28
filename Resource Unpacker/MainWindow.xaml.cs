using Archive_Unpacker.Classes.BarViewModel;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Resource_Unpacker.Classes.Bar;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;


public class RecentFile
{
    public string Title { get; set; }
    public string FileName { get; set; }
    public ICommand OnClickCommand { get; set; }
}

namespace Resource_Unpacker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<RecentFile> recentFiles { get; set; } = new ObservableCollection<RecentFile>();

        private string fileContent;
        public string FileContent
        {
            get { return fileContent; }
            set { fileContent = value; NotifyPropertyChanged(); }
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

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            files.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(Thumb_DragDelta), true);

            for (int i = 0; i < Math.Min(5, Settings.Default.RecentFiles.Count); i++)

                recentFiles.Add(new RecentFile() { FileName = Settings.Default.RecentFiles[i], Title = Path.GetFileName(Settings.Default.RecentFiles[i]), OnClickCommand = new RelayCommand<string>(openFile) });
        }

        private async void files_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            try
            {
                var entry = files.SelectedItem as BarEntry;
                if (entry == null)
                {
                    ImageViewer.Visibility = Visibility.Collapsed;
                    XMLViewer.Visibility = Visibility.Collapsed;
                    return;
                }
                var entries = files.SelectedItems.Cast<BarEntry>().ToList();
                SelectedSize = entries.Sum(x => x.FileSize2);
                await file.readFile(entry);

                if (file.Preview != null)
                    XMLViewer.Text = file.Preview.Text;
                string ext = Path.GetExtension(entry.FileName).ToUpper();
                if (ext == ".DDT")
                {
                    ImagePreview.Source = file.PreviewDdt.Bitmap;
                    XMLViewer.Visibility = Visibility.Collapsed;
                    ImageViewer.Visibility = Visibility.Visible;
                }
                else
                if (ext == ".BMP" || ext == ".PNG" || ext == ".CUR")
                {
                    ImagePreview.Source = file.PreviewImage;
                    XMLViewer.Visibility = Visibility.Collapsed;
                    ImageViewer.Visibility = Visibility.Visible;
                }
                else
                if (ext == ".XMB" || ext == ".XML" || ext == ".SHP" || ext == ".LGT" || ext == ".XS" || ext == ".TXT" || ext == ".CFG")
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
        }

        private void openFile(string path = null)
        {
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
                    return;
            }
            try
            {
                file = new BarViewModel(filePath);
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
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            openFile();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        public class RelayCommand<T> : ICommand
        {
            private Action<string> openFile;


            public RelayCommand(Action<string> openFile)
            {
                this.openFile = openFile;
            }

            public void Execute(object parameter)
            {
                openFile(parameter.ToString());
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;
        }


        CancellationTokenSource CancelTokenSource;
        CancellationToken Token;

        private async void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            if (files.SelectedItems.Count != 0)
            {
                mainMenu.IsEnabled = false;

                bPause.IsEnabled = true;
                bStop.IsEnabled = true;
                bRun.IsEnabled = false;


                file.extractingState = 0;
                CancelTokenSource = new CancellationTokenSource();
                Token = CancelTokenSource.Token;

                var entries = files.SelectedItems.Cast<BarEntry>().ToList();
                try
                {


                    await Task.Run(async () =>
                    {
                        await file.saveFiles(entries, "", Token);
                        //
                    }
                           );
                    /*       CurrentProgress = 0;
                           progressBar.Maximum = entries.Sum(x => x.fileLength2);
                           try
                           {
                               await Task.Run(async () =>
                               {
                                   foreach (Entry f in entries)
                                   {
                                       await file.saveFile(f, "");
                                       CurrentProgress += f.fileLength2;
                                   }
                               }
                               );*/
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                bPause.IsEnabled = false;
                bStop.IsEnabled = false;
                bRun.IsEnabled = false;
                mainMenu.IsEnabled = true;
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Age of Empires III - Resource Unpacker (CC, DE)\nVerson: 0.1.0\nXaKOps, 2020", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {

            string targetURL = "https://github.com/XaKOps/Resource-Unpacker";
            var psi = new ProcessStartInfo
            {
                FileName = targetURL,
                UseShellExecute = true
            };
            Process.Start(psi);

        }



        private GridViewColumnHeader listViewSortCol = null;
        private SortAdorner listViewSortAdorner = null;

        public class SortAdorner : Adorner
        {
            private static Geometry ascGeometry =
                Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

            private static Geometry descGeometry =
                Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

            public ListSortDirection Direction { get; private set; }

            public SortAdorner(UIElement element, ListSortDirection dir)
                : base(element)
            {
                this.Direction = dir;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);

                if (AdornedElement.RenderSize.Width < 20)
                    return;

                TranslateTransform transform = new TranslateTransform
                    (
                        AdornedElement.RenderSize.Width - 12,
                        (AdornedElement.RenderSize.Height - 5) / 2
                    );
                drawingContext.PushTransform(transform);

                Geometry geometry = ascGeometry;
                if (this.Direction == ListSortDirection.Descending)
                    geometry = descGeometry;
                drawingContext.DrawGeometry(Brushes.Black, null, geometry);

                drawingContext.Pop();
            }
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
            if (header.Tag.ToString() == "FileName")
            {
                minWidth = 250;
            }
            if (header.Tag.ToString() == "FileSize2")
            {
                minWidth = 160;
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

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {

        }

        private async void MenuItem_Click_6(object sender, RoutedEventArgs e)
        {
            if (file == null) return;
            // if (file..Count == 0) return;
            mainMenu.IsEnabled = false;

            bPause.IsEnabled = true;
            bStop.IsEnabled = true;
            bRun.IsEnabled = false;


            file.extractingState = 0;
            CancelTokenSource = new CancellationTokenSource();
            Token = CancelTokenSource.Token;
            //   CurrentProgress = 0;
            //     progressBar.Maximum = file.entries.Sum(x => x.fileLength2);
            try
            {

                await Task.Run(async () =>
                 {
                   //             foreach (Entry f in file.entries)
                   //             {
                   await file.saveFiles(file.barFile.BarFileEntrys.ToList(), "", Token);
                   //                    CurrentProgress += f.fileLength2;
                   ////                }
               }
             );
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //      CurrentProgress = 0;
            bPause.IsEnabled = false;
            bStop.IsEnabled = false;
            bRun.IsEnabled = false;
            mainMenu.IsEnabled = true;
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
        private double _zoomValue = 1.0;


        public int ZoomValue
        {
            get
            {
                return (int)(_zoomValue * 100);
            }
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
                if (ZoomValue > 20)
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
}

