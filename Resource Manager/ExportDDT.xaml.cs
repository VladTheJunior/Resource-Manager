using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Resource_Manager
{
    /// <summary>
    /// Interaktionslogik für ExportDDT.xaml
    /// </summary>
    public partial class ExportDDT : Window
    {
        private bool canceled = true;
        private string path;

        public bool Canceled { get => canceled; set => canceled = value; }
        public string Path { get => path; set => path = value; }

        public ExportDDT()
        {
            InitializeComponent();
        }

        private void NavigateButton_Click(object sender, RoutedEventArgs e)
        {
            using(FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if(result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.OK)
                {
                    ExportPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            canceled = false;
            if (Directory.Exists(ExportPath.Text))
            {
                path = ExportPath.Text;

                this.Close();
            }
            else
                System.Windows.Forms.MessageBox.Show("The path " + ExportPath.Text + "doesn't exist. Canceling export.");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            canceled = true;
            this.Close();
        }
    }
}
