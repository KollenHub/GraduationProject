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
            BasisCode bc = new BasisCode();
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
                        if (scha.Contains('-'))
                        {
                            string cstr = null;
                            foreach (Char c in scha)
                            {
                                cstr += c;
                            }
                            string[] cssp = cstr.Split(new char[] { '-' });
                            int i1 = Convert.ToInt32(cssp[0]);
                            int i2 = Convert.ToInt32(cssp[1]);
                            for (int i = i1; i <= i2; i++)
                            {
                                cflNum.Add(i);
                            }
                        }
                        else
                        {
                            bflNum.Add(Convert.ToInt32(str));
                        }

                    }
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
                string st = null;
                foreach (int lev in levList)
                {
                    st += lev.ToString() + "\r\n";
                }
                MessageBox.Show(st, "楼层数", MessageBoxButton.OK);
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

