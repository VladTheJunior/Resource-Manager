using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace Resource_Manager
{
    /// <summary>
    /// Логика взаимодействия для CreateBarFileDialog.xaml
    /// </summary>
    public partial class CreateBarFileDialog : Window
    {
        public CreateBarFileDialog()
        {
            InitializeComponent();
        }

        public string RootPath { get; set; } = "";

        public uint Version
        {
            get
            {
                return (tbGameVersion.SelectedIndex == 0) ? (uint)2 : (uint)5;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    RootPath = folderBrowserDialog.SelectedPath;
                    tbRootPath.Text = RootPath;
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(RootPath))
                MessageBox.Show("Select Bar Root Folder!");
            else
            {
                DialogResult = true;
            }
        }
    }
}
