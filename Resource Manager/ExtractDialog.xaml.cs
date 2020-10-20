using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;

namespace Resource_Manager
{
    /// <summary>
    /// Interaktionslogik für ExportDDT.xaml
    /// </summary>
    public partial class ExtractDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public string Path { get; set; } = "";

        private bool autoDecompress = true;
        public bool AutoDecompress
        {
            get
            {
                return autoDecompress;
            }
            set
            {
                autoDecompress = value;
                NotifyPropertyChanged();
            }
        }

        public ExtractDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void NavigateButton_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ExportPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ExportPath.Text))
            {
                Path = ExportPath.Text;
                DialogResult = true;
            }
            else
                System.Windows.Forms.MessageBox.Show("The path " + ExportPath.Text + "doesn't exist. Select valid path.");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}