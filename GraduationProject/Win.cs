using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GraduationProject
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class Win : Window
    {
        public List<string> chbStrList { get; set; }
        public string modeStr { get; set; }
        public Win()
        {
            InitializeComponent();
            this.Loaded += Win_Loaded;
        }

        private void Win_Loaded(object sender, RoutedEventArgs e)
        {
            chbB.IsChecked = true;
            rbtnE.IsChecked = true;
        }

        private void dignWin_Closed(object sender, EventArgs e)
        {
            List<CheckBox> chbList = new List<CheckBox>();
            chbList.Add(chbB);
            chbList.Add(chbC);
            chbList.Add(chbCF);
            chbList.Add(chbF);
            chbList.Add(chbS);
            chbList.Add(chbW);
            chbStrList = new List<string>();
            foreach (CheckBox chb in chbList)
            {
                if (chb.IsChecked==true)
                {
                    chbStrList.Add(chb.Content.ToString());
                }
            }
            if (rbtnE.IsChecked == true) modeStr = "Excel表格";
            if (rbtnW.IsChecked == true) modeStr = "Word文档";
            if (rbtnT.IsChecked == true) modeStr = "Txt文本";
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
            
        }
    }
}
