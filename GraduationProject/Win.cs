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
    public partial class Win : Window
    {
        public UIDocument uiDoc;
        public List<string> chbStrList;
        public string modeStr;
        public List<int> levList;
        public string oneFlName;
        public int p=1;
        public Win(UIDocument uidoc)
        {
            InitializeComponent();
            uiDoc = uidoc;
            this.Loaded += Win_Loaded;
        }

        private void Win_Loaded(object sender, RoutedEventArgs e)
        {
            chbB.IsChecked = true;
            chbC.IsChecked = true;
            chbF.IsChecked = true;
            chbBc.IsChecked = true;
            chbFc.IsChecked = true;
            chbCc.IsChecked = true;
            List<Level> levList = bc.GetLevList(uiDoc.Document);
            fstCmb.ItemsSource = levList.ConvertAll(m => m.Name);
            fstCmb.SelectedIndex = 0;
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void dignWin_Closed(object sender, EventArgs e)
        {
            List<CheckBox> chbList = new List<CheckBox>();
            chbList.Add(chbB);
            chbList.Add(chbC);
            chbList.Add(chbF);
            chbList.Add(chbBc);
            chbList.Add(chbCc);
            chbList.Add(chbFc);
            chbStrList = new List<string>();
            foreach (CheckBox chb in chbList)
            {
                if (chb.IsChecked == true)
                {
                    chbStrList.Add(chb.Content.ToString());
                }
            }
            string s = sTxtb.Text;
            try
            {
                //DialogResult = true;
                if (s == "all")
                {
                    levList.Add(100000);
                }
                else
                {
                    string[] ssp = s.Split(new char[] { ',' });
                    List<int> cflNum = new List<int>();
                    List<int> bflNum = new List<int>();
                    List<int> flNum = new List<int>();
                    foreach (string str in ssp)
                    {
                        char[] scha =str.ToCharArray();
                        if (scha.Contains('-'))//如果是相连着的
                        {
                            string cstr = null;
                            foreach (Char c in scha)
                            {
                                cstr += c;
                            }
                            string[] cssp = cstr.Split(new char[] { '-' });//将其重新组装
                            int i1 = Convert.ToInt32(cssp[0]);//初始楼层
                            int i2 = Convert.ToInt32(cssp[1]);//末尾楼层
                            for (int i = i1; i <= i2; i++)
                            {
                                cflNum.Add(i);
                            }
                        }
                        else
                        {
                            bflNum.Add(Convert.ToInt32(str));//单层的楼层编号
                        }

                    }
                    //对这两种标高进行比较，得出最终的标高
                    foreach (int i in cflNum)
                    {
                        if (flNum.Count == 0 || flNum.Where(m => m == i).Count() == 0)
                        {
                            flNum.Add(i);
                        }
                    }
                    foreach (int i in bflNum)
                    {
                        if (flNum.Count == 0 || flNum.Where(m => m == i).Count() == 0)
                        {
                            flNum.Add(i);
                        }
                    }
                    levList = flNum;
                }
                //string st = null;
                //foreach (int lev in levList)
                //{
                //    st += lev.ToString() + "\r\n";
                //}
                //MessageBox.Show(st, "楼层数", MessageBoxButton.OK);
                p =1;
            }
            catch
            {
                p = 0;
                MessageBox.Show("楼层输入框输入出错", "警告", MessageBoxButton.OK);
            }
            
        }

    }
}

