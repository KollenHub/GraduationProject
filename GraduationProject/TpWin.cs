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
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using bc = TemplateCount.BasisCode;
namespace TemplateCount
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TpWin : Window
    {
        public List<string> chbStrList;
        public TpWin()
        {
            InitializeComponent();
        }
        private void BtnEnter_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void DignWin_Closed(object sender, EventArgs e)
        {
            List<CheckBox> chbList = new List<CheckBox>();
            chbList.Add(chbB);
            chbList.Add(chbC);
            chbList.Add(chbF);
            chbList.Add(chbS);
            chbList.Add(chbW);
            chbList.Add(chbF);
            chbStrList = new List<string>();
            foreach (CheckBox chb in chbList)
            {
                if (chb.IsChecked == true)
                {
                    chbStrList.Add(chb.Content.ToString());
                }
            }
        }
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

