using Force.Crc32;
using Resource_Manager.Classes.Alz4;
using Resource_Manager.Classes.Bar;
using Resource_Manager.Classes.Ddt;
using Resource_Manager.Classes.L33TZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace Resource_Manager
{
    /// <summary>
    /// Логика взаимодействия для EntryDetailsDialog.xaml
    /// </summary>
    public partial class EntryDetailsDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public BarEntry entry { get; set; }

        private string headerText;
        public string HeaderText
        {
            get
            {
                return headerText;
            }
            set
            {
                headerText = value;
                NotifyPropertyChanged();
            }
        }

        private int header;
        public int Header
        {
            get
            {
                return header;
            }
            set
            {
                header = value;
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

        private string ddtUsage;
        public string DdtUsage
        {
            get
            {
                return ddtUsage;
            }
            set
            {
                ddtUsage = value;
                NotifyPropertyChanged();
            }
        }

        private string ddtAlpha;
        public string DdtAlpha
        {
            get
            {
                return ddtAlpha;
            }
            set
            {
                ddtAlpha = value;
                NotifyPropertyChanged();
            }
        }

        private string ddtFormat;
        public string DdtFormat
        {
            get
            {
                return ddtFormat;
            }
            set
            {
                ddtFormat = value;
                NotifyPropertyChanged();
            }
        }

        static IEnumerable<Enum> GetFlags(Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }

        public async void GetCustomValues(string filename)
        {
            if (!File.Exists(filename))
                throw new Exception("BAR file does not exist!");
            using var file = File.OpenRead(filename);
            var reader = new BinaryReader(file);

            reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
            byte[] data = reader.ReadBytes(entry.FileSize2);
            await Task.Run(() =>
            {
                entry.CRC32 = Crc32Algorithm.Compute(data);
            });
            reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
            HeaderText = new string(reader.ReadChars(4));
            reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
            Header = reader.ReadInt32();
            if (entry.Extension == ".DDT")
            { 
            if (Alz4Utils.IsAlz4File(data))
            {
                data = await Alz4Utils.ExtractAlz4BytesAsync(data);
            }
            else
            {
                if (L33TZipUtils.IsL33TZipFile(data))
                    data = await L33TZipUtils.ExtractL33TZippedBytesAsync(data);
            }



            PreviewDdt = new DdtFile(data, true);
                var flagList = new List<string>();
                if (PreviewDdt.Usage.HasFlag(DdtFileTypeUsage.AlphaTest))
                {
                    flagList.Add(DdtFileTypeUsage.AlphaTest.ToString());
                }
                if (PreviewDdt.Usage.HasFlag(DdtFileTypeUsage.LowDetail))
                {
                    flagList.Add(DdtFileTypeUsage.LowDetail.ToString());
                }
                if (PreviewDdt.Usage.HasFlag(DdtFileTypeUsage.Bump))
                {
                    flagList.Add(DdtFileTypeUsage.Bump.ToString());
                }
                if (PreviewDdt.Usage.HasFlag(DdtFileTypeUsage.Cube))
                {
                    flagList.Add(DdtFileTypeUsage.Cube.ToString());
                }
                if (flagList.Count > 0)
                    DdtUsage = ((byte)PreviewDdt.Usage).ToString() + " (" + string.Join('+', flagList) + ")";
                else
                    DdtUsage = ((byte)PreviewDdt.Usage).ToString();
                DdtAlpha = ((byte)PreviewDdt.Alpha).ToString() + " (" + PreviewDdt.Alpha.ToString() + ")";
                DdtFormat = ((byte)PreviewDdt.Format).ToString() + " (" + PreviewDdt.Format.ToString() + ")";
                gpDDT.Visibility = Visibility.Visible;
            }

        }
        public EntryDetailsDialog(BarEntry e, string BarPath)
        {
            entry = e;
            
            InitializeComponent();
            GetCustomValues(BarPath);
            DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
