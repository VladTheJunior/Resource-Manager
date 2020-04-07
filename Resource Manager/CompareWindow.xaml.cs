using Archive_Unpacker.Classes.BarViewModel;
using Microsoft.Win32;
using Resource_Manager.Classes.Bar;
using Resource_Manager.Classes.Sort;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Resource_Manager
{
    /// <summary>
    /// Логика взаимодействия для CompareWindow.xaml
    /// </summary>
    public partial class CompareWindow : Window, INotifyPropertyChanged
    {
        #region Variables
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private GridViewColumnHeader listViewSortCol = null;
        private SortAdorner listViewSortAdorner = null;


        private double _zoomValue1 = 1.0;
        private double _zoomValue2 = 1.0;

        public int ZoomValue1
        {
            get
            {
                return (int)(_zoomValue1 * 100);
            }
        }

        public int ZoomValue2
        {
            get
            {
                return (int)(_zoomValue2 * 100);
            }
        }


        public BarViewModel Bar1 { get; set; }
        public BarViewModel Bar2 { get; set; }

        #endregion

        public CompareWindow()
        {
            InitializeComponent();
            //SearchPanel.Install(XMLViewer);
            DataContext = this;
            BarView1.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(Thumb_DragDelta), true);
            BarView2.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(Thumb_DragDelta), true);
            BarView1.Items.GroupDescriptions.Add(new PropertyGroupDescription("type"));
            BarView2.Items.GroupDescriptions.Add(new PropertyGroupDescription("type"));
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
                minWidth = 40;
            }
            if (header.Tag.ToString() == "FileNameWithRoot")
            {
                minWidth = 195;
            }
            if (header.Tag.ToString() == "FileSize2")
            {
                minWidth = 130;
            }
            if (header.Tag.ToString() == "CRC32")
            {
                minWidth = 80;
            }
            if (header.Tag.ToString() == "lastModifiedDate")
            {
                minWidth = 160;
            }
            if (header.Column.ActualWidth < minWidth)
            {
                e.Handled = true;
                header.Column.Width = minWidth;

            }
        }

        private void ImageViewer1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (ZoomValue1 < 400)
                    _zoomValue1 += 0.1;
                else
                    return;
            }
            else
            {
                if (ZoomValue1 > 10)
                    _zoomValue1 -= 0.1;
                else
                    return;
            }
            NotifyPropertyChanged("ZoomValue1");
            ScaleTransform scale = new ScaleTransform(_zoomValue1, _zoomValue1);
            ImagePreview1.LayoutTransform = scale;
            e.Handled = true;
        }

        private void ImageViewer2_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (ZoomValue2 < 400)
                    _zoomValue2 += 0.1;
                else
                    return;
            }
            else
            {
                if (ZoomValue2 > 10)
                    _zoomValue2 -= 0.1;
                else
                    return;
            }
            NotifyPropertyChanged("ZoomValue2");
            ScaleTransform scale = new ScaleTransform(_zoomValue2, _zoomValue2);
            ImagePreview2.LayoutTransform = scale;
            e.Handled = true;
        }

        private void TextBlock1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _zoomValue1 = 1;
            ScaleTransform scale = new ScaleTransform(_zoomValue1, _zoomValue1);
            ImagePreview1.LayoutTransform = scale;
            NotifyPropertyChanged("ZoomValue1");
        }

        private void TextBlock2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _zoomValue2 = 1;
            ScaleTransform scale = new ScaleTransform(_zoomValue2, _zoomValue2);
            ImagePreview2.LayoutTransform = scale;
            NotifyPropertyChanged("ZoomValue2");
        }

        private void files_Click(object sender, RoutedEventArgs e)
        {

            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            string sortBy = column.Tag.ToString();
            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                BarView1.Items.SortDescriptions.Clear();
                BarView2.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            listViewSortCol = column;
            listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            BarView1.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
            BarView2.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }

        private async void BarView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || Bar1 == null || Bar2 == null || Bar1.CompareEntries.Count == 0 || Bar2.CompareEntries.Count == 0 || BarView1.SelectedIndex != BarView2.SelectedIndex)
            {
                return;
            }

            //   e.Handled = true;
            try
            {

                ImageViewer1.Visibility = Visibility.Collapsed;
                ImageViewer2.Visibility = Visibility.Collapsed;
                XMLViewer.Visibility = Visibility.Collapsed;
                XMLViewer.NewText = "";
                XMLViewer.OldText = "";
                BarEntry entry = BarView1.SelectedItem as BarEntry;
                BarEntry entry2 = BarView2.SelectedItem as BarEntry;
                if (entry != null)
                {
                    if (entry.type != "Added")
                    {

                        await Bar1.readFile(entry);
                    }
                }

                if (entry2 != null)
                {
                    if (entry2.type != "Removed")
                    {

                        await Bar2.readFile(entry2);
                    }
                }

                if (entry != null)
                {
                    if (entry.type != "Added")
                    {



                        if (Bar1.Preview != null)
                        {

                            XMLViewer.OldText = Bar1.Preview.Text;

                        }

                        if (entry.Extension == ".DDT")
                        {
                            ImagePreview1.Source = Bar1.PreviewDdt.Bitmap;
                            XMLViewer.Visibility = Visibility.Collapsed;
                            ImageViewer1.Visibility = Visibility.Visible;
                        }
                        else
                        if (entry.Extension == ".BMP" || entry.Extension == ".PNG" || entry.Extension == ".CUR" || entry.Extension == ".JPG")
                        {
                            ImagePreview1.Source = Bar1.PreviewImage;
                            XMLViewer.Visibility = Visibility.Collapsed;
                            ImageViewer1.Visibility = Visibility.Visible;
                        }
                        else
                        if (entry.Extension == ".XMB" || entry.Extension == ".XML" || entry.Extension == ".SHP" || entry.Extension == ".LGT" || entry.Extension == ".XS" || entry.Extension == ".TXT" || entry.Extension == ".CFG" || entry.Extension == ".XAML")
                        {
                            ImageViewer1.Visibility = Visibility.Collapsed;
                            XMLViewer.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ImageViewer1.Visibility = Visibility.Collapsed;
                            XMLViewer.Visibility = Visibility.Collapsed;

                        }

                    }
                }

                if (entry2 != null)
                {
                    if (entry2.type != "Removed")
                    {


                        if (Bar2.Preview != null)
                        {

                            XMLViewer.NewText = Bar2.Preview.Text;

                        }
                        if (entry.Extension == ".DDT")
                        {
                            ImagePreview2.Source = Bar2.PreviewDdt.Bitmap;
                            XMLViewer.Visibility = Visibility.Collapsed;
                            ImageViewer2.Visibility = Visibility.Visible;
                        }
                        else
                        if (entry.Extension == ".BMP" || entry.Extension == ".PNG" || entry.Extension == ".CUR" || entry.Extension == ".JPG")
                        {
                            ImagePreview2.Source = Bar2.PreviewImage;
                            XMLViewer.Visibility = Visibility.Collapsed;
                            ImageViewer2.Visibility = Visibility.Visible;
                        }
                        else
                        if (entry.Extension == ".XMB" || entry.Extension == ".XML" || entry.Extension == ".SHP" || entry.Extension == ".LGT" || entry.Extension == ".XS" || entry.Extension == ".TXT" || entry.Extension == ".CFG" || entry.Extension == ".XAML")
                        {
                            ImageViewer2.Visibility = Visibility.Collapsed;
                            XMLViewer.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            ImageViewer2.Visibility = Visibility.Collapsed;
                            XMLViewer.Visibility = Visibility.Collapsed;

                        }

                    }






                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


        public static DependencyObject GetScrollViewer(DependencyObject o)
        {
            if (o is ScrollViewer)
            { return o; }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);
                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;
        }

        private void SyncScrollViewers()
        {
            var scrollViewer1 = GetScrollViewer(BarView1) as ScrollViewer;
            var scrollViewer2 = GetScrollViewer(BarView2) as ScrollViewer;
            scrollViewer1.ScrollChanged += (s, e) =>
             {
                 var offset = scrollViewer1.VerticalOffset;
                 if (Math.Abs(scrollViewer2.VerticalOffset - offset) > 1)
                     scrollViewer2.ScrollToVerticalOffset(offset);
             };
            scrollViewer2.ScrollChanged += (s, e) =>
            {
                var offset = scrollViewer2.VerticalOffset;
                if (Math.Abs(scrollViewer1.VerticalOffset - offset) > 1)
                    scrollViewer1.ScrollToVerticalOffset(offset);
            };
        }


        private async void bSelectBar1_Click(object sender, RoutedEventArgs e)
        {
            bSelectBar1.IsEnabled = false;
            SpinnerFile1.Visibility = Visibility.Visible;
            tbFile1.Text = "Opening";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Age of Empires 3 .BAR files (*.bar)|*.bar";
            string filePath;
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                tbBar1Name.Text = filePath;
            }
            else
            {
                SpinnerFile1.Visibility = Visibility.Collapsed;
                tbFile1.Text = "Open";
                bSelectBar1.IsEnabled = true;
                return;
            }
            try
            {
                Bar1 = null;
                NotifyPropertyChanged("Bar1");
                Bar1 = await BarViewModel.Load(filePath, true);
                NotifyPropertyChanged("Bar1");

                if (Bar2 != null)
                {
                    Bar2.ResetComparer();
                    //NotifyPropertyChanged("Bar2");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SpinnerFile1.Visibility = Visibility.Collapsed;
            tbFile1.Text = "Open";
            bSelectBar1.IsEnabled = true;
        }

        private async void bSelectBar2_Click(object sender, RoutedEventArgs e)
        {
            bSelectBar2.IsEnabled = false;
            SpinnerFile2.Visibility = Visibility.Visible;
            tbFile2.Text = "Opening";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Age of Empires 3 .BAR files (*.bar)|*.bar";
            string filePath;
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                tbBar2Name.Text = filePath;
            }
            else
            {
                SpinnerFile2.Visibility = Visibility.Collapsed;
                tbFile2.Text = "Open";
                bSelectBar2.IsEnabled = true;
                return;
            }


            try
            {
                Bar2 = null;
                NotifyPropertyChanged("Bar2");
                Bar2 = await BarViewModel.Load(filePath, true);
                NotifyPropertyChanged("Bar2");

                if (Bar1 != null)
                {
                    Bar1.ResetComparer();
                    //NotifyPropertyChanged("Bar1");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SpinnerFile2.Visibility = Visibility.Collapsed;
            tbFile2.Text = "Open";
            bSelectBar2.IsEnabled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SyncScrollViewers();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            if (Bar1 != null && Bar2 != null)
            {
                Bar1.Compare(Bar2, false);
                Bar2.Compare(Bar1, true);
            }
            else
                MessageBox.Show("You should open 2 files for comparison !");
            (sender as Button).IsEnabled = true;
        }


    }

}
