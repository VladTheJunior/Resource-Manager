using Archive_Unpacker.Classes.BarViewModel;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.Win32;
using Resource_Manager.Classes.Bar;
using Resource_Manager.Classes.BarComparer;
using Resource_Manager.Classes.Sort;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

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

        public BarComparer barComparer { get; set; }

        #endregion

        public CompareWindow()
        {

            IHighlightingDefinition customHighlighting;
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Resource_Manager.Classes.Diff.xshd"))
            {
                using (XmlReader reader = new XmlTextReader(s))
                {
                    customHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    //     XMLViewer2.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            HighlightingManager.Instance.RegisterHighlighting("Diff", new string[] { }, customHighlighting);

            InitializeComponent();
            SearchPanel.Install(XMLViewer1);
            SearchPanel.Install(XMLViewer2);
            DataContext = this;
            BarComparerView.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(Thumb_DragDelta), true);
            BarComparerView.Items.GroupDescriptions.Add(new PropertyGroupDescription("type"));
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            int minWidth = 100;
            Thumb senderAsThumb = e.OriginalSource as Thumb;
            GridViewColumnHeader header
              = senderAsThumb.TemplatedParent as GridViewColumnHeader;
            if (header == null) return;
            if (header.Tag == null) return;
            if (header.Tag.ToString() == "entryOld.isCompressed" || header.Tag.ToString() == "entryNew.isCompressed")
            {
                minWidth = 40;
            }
            if (header.Tag.ToString() == "entryOld.FileNameWithRoot" || header.Tag.ToString() == "entryNew.FileNameWithRoot")
            {
                minWidth = 195;
            }
            if (header.Tag.ToString() == "entryOld.FileSize2" || header.Tag.ToString() == "entryNew.FileSize2")
            {
                minWidth = 130;
            }
            if (header.Tag.ToString() == "entryOld.CRC32" || header.Tag.ToString() == "entryNew.CRC32")
            {
                minWidth = 80;
            }
            if (header.Tag.ToString() == "entryOld.lastModifiedDate" || header.Tag.ToString() == "entryNew.lastModifiedDate")
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
                BarComparerView.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            listViewSortCol = column;
            listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            BarComparerView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }

        private async void BarView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (e.AddedItems.Count == 0 || Bar1 == null || Bar2 == null || barComparer == null)
                return;

            
            //   e.Handled = true;
             try
            {

                ImageViewer1.Visibility = Visibility.Collapsed;
                ImageViewer2.Visibility = Visibility.Collapsed;
                XMLViewer1.Visibility = Visibility.Collapsed;
                XMLViewer1.Text = "";

                XMLViewer2.Visibility = Visibility.Collapsed;
                XMLViewer2.Text = "";

                BarComparerEntry entry = (BarComparerView.SelectedItem as BarComparerEntry);
                if (entry == null)

                    return;
                    
                BarEntry entryOld = entry.entryOld;
                BarEntry entryNew = entry.entryNew;

                await Bar1.readFile(entryOld);
                await Bar2.readFile(entryNew);


                if (Bar1.Preview != null && Bar2.Preview != null)
                {
                    try
                    {
                        IEnumerable<string> Old = new List<string>();
                        IEnumerable<string> New = new List<string>();
                        await Task.Run(() =>
                        {
                            var diff = SideBySideDiffBuilder.Diff(Bar1.Preview.Text, Bar2.Preview.Text);


                            Old = diff.OldText.Lines.Select(x =>
                            {
                                if (x.Type == ChangeType.Inserted || x.SubPieces.Any(x => x.Type == ChangeType.Inserted))
                                    return "!+ " + x.Text;
                                if (x.Type == ChangeType.Deleted || x.SubPieces.Any(x => x.Type == ChangeType.Deleted))
                                    return "!- " + x.Text;
                                return "   " + x.Text;
                            }
                            );

                            New = diff.NewText.Lines.Select(x =>
                            {
                                if (x.Type == ChangeType.Inserted || x.SubPieces.Any(x => x.Type == ChangeType.Inserted))
                                    return "!+ " + x.Text;
                                if (x.Type == ChangeType.Deleted || x.SubPieces.Any(x => x.Type == ChangeType.Deleted))
                                    return "!- " + x.Text;
                                return "   " + x.Text;
                            });
                        });

                        XMLViewer1.Text = string.Join("\n", Old);
                        XMLViewer2.Text = string.Join("\n", New);
                    }
                    catch { }
                }

                if (entryNew == null)
                {
                    if (Bar1.Preview != null)
                    {
                        XMLViewer1.Text = Bar1.Preview.Text;
                    }
                }


                if (entryOld == null)
                {

                    if (Bar2.Preview != null)
                    {
                        XMLViewer2.Text = Bar2.Preview.Text;
                    }
                }
                if (entryOld != null)
                {
                    if (entryOld.Extension == ".DDT")
                    {
                        ImagePreview1.Source = Bar1.PreviewDdt.Bitmap;
                        XMLViewer1.Visibility = Visibility.Collapsed;
                        ImageViewer1.Visibility = Visibility.Visible;
                    }
                    else
                    if (entryOld.Extension == ".BMP" || entryOld.Extension == ".PNG" || entryOld.Extension == ".CUR" || entryOld.Extension == ".JPG")
                    {
                        ImagePreview1.Source = Bar1.PreviewImage;
                        XMLViewer1.Visibility = Visibility.Collapsed;
                        ImageViewer1.Visibility = Visibility.Visible;
                    }
                    else
                    if (entryOld.Extension == ".XMB" || entryOld.Extension == ".XML" || entryOld.Extension == ".SHP" || entryOld.Extension == ".LGT" || entryOld.Extension == ".XS" || entryOld.Extension == ".TXT" || entryOld.Extension == ".CFG" || entryOld.Extension == ".XAML")
                    {
                        ImageViewer1.Visibility = Visibility.Collapsed;
                        XMLViewer1.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ImageViewer1.Visibility = Visibility.Collapsed;
                        XMLViewer1.Visibility = Visibility.Collapsed;

                    }
                }

                if (entryNew != null)
                {
                    if (entryNew.Extension == ".DDT")
                    {
                        ImagePreview2.Source = Bar2.PreviewDdt.Bitmap;
                        XMLViewer2.Visibility = Visibility.Collapsed;
                        ImageViewer2.Visibility = Visibility.Visible;
                    }
                    else
                    if (entryNew.Extension == ".BMP" || entryNew.Extension == ".PNG" || entryNew.Extension == ".CUR" || entryNew.Extension == ".JPG")
                    {
                        ImagePreview2.Source = Bar2.PreviewImage;
                        XMLViewer2.Visibility = Visibility.Collapsed;
                        ImageViewer2.Visibility = Visibility.Visible;
                    }
                    else
                    if (entryNew.Extension == ".XMB" || entryNew.Extension == ".XML" || entryNew.Extension == ".SHP" || entryNew.Extension == ".LGT" || entryNew.Extension == ".XS" || entryNew.Extension == ".TXT" || entryNew.Extension == ".CFG" || entryNew.Extension == ".XAML")
                    {
                        ImageViewer2.Visibility = Visibility.Collapsed;
                        XMLViewer2.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ImageViewer2.Visibility = Visibility.Collapsed;
                        XMLViewer2.Visibility = Visibility.Collapsed;

                    }
                }
            }
            catch (Exception ex)
            {
               // MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void bSelectBar1_Click(object sender, RoutedEventArgs e)
        {
            mainMenu.IsEnabled = false;
            SpinnerFile1.Visibility = Visibility.Visible;
            tbFile.Text = "Opening";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Age of Empires 3 .BAR files (*.bar)|*.bar";
            string filePath;
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                tbBar1Name.ToolTip = filePath;
            }
            else
            {
                SpinnerFile1.Visibility = Visibility.Collapsed;
                tbFile.Text = "Open";
                mainMenu.IsEnabled = true;
                return;
            }
            try
            {
                barComparer = null;
                NotifyPropertyChanged("barComparer");
                Bar1 = null;
                OldOpen.IsChecked = false;
                NotifyPropertyChanged("Bar1");
                Bar1 = await BarViewModel.Load(filePath, true);
                NotifyPropertyChanged("Bar1");
                OldOpen.IsChecked = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SpinnerFile1.Visibility = Visibility.Collapsed;
            tbFile.Text = "Open";
            mainMenu.IsEnabled = true;
        }

        private async void bSelectBar2_Click(object sender, RoutedEventArgs e)
        {
            mainMenu.IsEnabled = false;
            SpinnerFile1.Visibility = Visibility.Visible;
            tbFile.Text = "Opening";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Age of Empires 3 .BAR files (*.bar)|*.bar";
            string filePath;
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
                tbBar2Name.ToolTip = filePath;
            }
            else
            {
                SpinnerFile1.Visibility = Visibility.Collapsed;
                tbFile.Text = "Open";
                mainMenu.IsEnabled = true;
                return;
            }


            try
            {
                barComparer = null;
                NotifyPropertyChanged("barComparer");
                Bar2 = null;
                NewOpen.IsChecked = false;
                NotifyPropertyChanged("Bar2");
                Bar2 = await BarViewModel.Load(filePath, true);
                NotifyPropertyChanged("Bar2");
                NewOpen.IsChecked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SpinnerFile1.Visibility = Visibility.Collapsed;
            tbFile.Text = "Open";
            mainMenu.IsEnabled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            mainMenu.IsEnabled = false;
            SpinnerFile2.Visibility = Visibility.Visible;
            tbCompare.Text = "Comparing";
            if (Bar1 != null && Bar2 != null)
            {
                barComparer = await BarComparer.Compare(Bar1, Bar2);
                NotifyPropertyChanged("barComparer");

            }
            else
                MessageBox.Show("You should open 2 files for comparison !");
            SpinnerFile2.Visibility = Visibility.Collapsed;
            tbCompare.Text = "Compare";
            mainMenu.IsEnabled = true;
        }

        private void XMLViewer1_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            var offset = XMLViewer1.VerticalOffset;
            if (Math.Abs(XMLViewer2.VerticalOffset - offset) > 1)
                XMLViewer2.ScrollToVerticalOffset(offset);



        }

        private void XMLViewer2_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var offset = XMLViewer2.VerticalOffset;
            if (Math.Abs(XMLViewer1.VerticalOffset - offset) > 1)
                XMLViewer1.ScrollToVerticalOffset(offset);
        }
    }

}
