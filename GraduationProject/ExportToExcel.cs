using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TemplateCount
{
   public class ExportToExcel
    {
        public BasisCode bc = new BasisCode();
        public ExportToExcel(List<List<TpAmount>> list_List)
        {
            //保存文件
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Excel 工作簿|*.xlsx"; //删选、设定文件显示类型
            sfd.DefaultExt = ".xlsx";
            sfd.FileName = "BIMING模板算量";
            sfd.AddExtension = true;
            sfd.ShowDialog();
            string path = sfd.FileName;
            FileInfo newFile = new FileInfo(path);
            //如果新文件存在的话则删除它
            if (newFile.Exists)
            {
                try
                {
                    newFile.Delete();
                    newFile = new FileInfo(path);
                }
                catch
                {
                    MessageBox.Show("该文档正在被其他应用使用,无法删除", "警告", MessageBoxButtons.OK);
                }
            }
            //新建工作簿
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                //
                int i = 0;
                ExcelWorksheet[] sheetList = new ExcelWorksheet[list_List.Count];
                foreach (List<TpAmount> list in list_List)
                {
                    //第一个文档
                    TpAmount tpa = list.First();
                    //取得算量模板的属性
                    List<string> fieldsList = TpAFiieldTxt(tpa);
                    //转换后的属性名称
                    List<int> columnSizeList = new List<int>();
                    List<string> proNameList = bc.ProTransform(fieldsList,out columnSizeList);
                    //将工作表的名字命名为该种构件的名称
                    sheetList[i] = package.Workbook.Worksheets.Add(tpa.ComponentType);
                    //当前所操作的工作表
                    ExcelWorksheet worksheet = sheetList[i];
                    //从第二行开始
                    int row = 2;
                    //每个构件的模板合计数量
                    double partAmount = 0;
                    //总计的模板数量
                    double allAmount = 0;
                    //存储相同的构件出现的次数
                    int time = 0;
                    //排列某个工作表
                    foreach (TpAmount l in list)
                    {
                        int n = list.IndexOf(l);
                        //如果是相同的构件则把模板量加起来
                      if (n == 0 || l.ElemId == list[n - 1].ElemId)
                        {
                            partAmount += l.TemplateAmount * l.TemplateNum;
                            time++;
                        }else if(time==1)//如果只有一个相同构件时
                        {
                            partAmount += l.TemplateAmount * l.TemplateNum;
                            time = 0;
                        }
                        else//到某一行不是同一个构件时
                        {
                            worksheet.Cells[row, fieldsList.Count - 1].Value = partAmount;
                            worksheet.Cells[row, 1].Value = "合计";
                            worksheet.Cells[row, fieldsList.Count].Value = "平方米";
                            allAmount += partAmount;
                            partAmount = l.TemplateAmount * l.TemplateNum;
                            row++;
                            time = 0;
                        }
                        //按照从属性的顺序一行一行的填数据
                        foreach (string str in fieldsList)
                        {
                            if (row==2)
                            {
                                //第一行标题字段的对应文字
                                worksheet.Cells[1, fieldsList.IndexOf(str) + 1].Value = proNameList.ElementAt(fieldsList.IndexOf(str));
                                worksheet.Column(fieldsList.IndexOf(str) + 1).Width = columnSizeList.ElementAt(fieldsList.IndexOf(str));
                            }
                            //获得属性集合
                            PropertyInfo[] pinfo = l.GetType().GetProperties();
                            //获得对应属性的值
                            PropertyInfo pi = pinfo.First(m => m.Name == str);
                            //如果该值不为空，则该空填入该值
                            if (pi != null)
                            {
                                worksheet.Cells[row, fieldsList.IndexOf(str) + 1].Value = pi.GetValue(l);
                            }
                            //自动调整行高
                            worksheet.Row(row).CustomHeight = true;
                        }
                        
                        row++;
                    }
                    i++;
                    worksheet.Cells[row, fieldsList.Count - 1].Value = allAmount;
                    worksheet.Cells[row, 1].Value = "总计";
                    worksheet.Cells[row, fieldsList.Count].Value = "平方米";
                    //设置字体，也可以是中文，比如：宋体
                    worksheet.Cells.Style.Font.Name = "宋体";
                    //字体加粗
                    worksheet.Cells.Style.Font.Bold = false;
                    //字体大小
                    worksheet.Cells.Style.Font.Size = 12;
                    //字体颜色
                    worksheet.Cells.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                    //单元格背景样式，要设置背景颜色必须先设置背景样式
                    worksheet.Cells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    //单元格背景颜色
                    worksheet.Cells.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.White);
                    //垂直居中
                    worksheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    //水平居中
                    worksheet.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    //单元格是否自动换行
                    worksheet.Cells.Style.WrapText = false;
                    //设置单元格格式为文本
                    worksheet.Cells.Style.Numberformat.Format = "@";
                    //单元格自动适应大小
                    worksheet.Cells.Style.ShrinkToFit = true;
                    worksheet.Column(1).Width = 20;
                    worksheet.Column(2).Width = 20;
                    worksheet.Column(6).Width = 25;
                    worksheet.Column(7).Width = 25;
                }
                package.Save();
            }

        }

        private List<string> TpAFiieldTxt(TpAmount l)
        {
            Type tpaTp = l.GetType();
            List<string> fieldList = new List<string>();
            PropertyInfo[] ps = tpaTp.GetProperties();
            foreach (PropertyInfo pi in ps)
            {
                if (pi.GetValue(l) == null) continue;
                fieldList.Add(pi.Name);
            }
            return fieldList;
        }

    }
}


