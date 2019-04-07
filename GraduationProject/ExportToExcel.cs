using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using bc = TemplateCount.BasisCode;
namespace TemplateCount
{
    public class ExportToExcel
    {
        public ExportToExcel(List<List<List<ProjectAmount>>> pAListList_List)
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
            string fileName = path.Split(new char[] { '\\' }).Reverse().ToList().First();
            string directory = Path.GetDirectoryName(path);

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
                    string filename = fileName.Split(new char[] { '.' })[0] + new Random().Next(1, 10000) + "." + fileName.Split(new char[] { '.' })[1];
                    path = directory + "\\" + filename;
                    MessageBox.Show("该文档正在被其他应用使用,无法删除," + "\r\n已将新文件名更换为" + filename, "警告", MessageBoxButtons.OK);
                    newFile = new FileInfo(path);
                }
            }
            //新建工作簿
            using (ExcelPackage package = new ExcelPackage(newFile))
            {
                int i = 0;
                ExcelWorksheet[] sheetList = new ExcelWorksheet[pAListList_List.Count];
                foreach (List<List<ProjectAmount>> tpaListList in pAListList_List)
                {
                    //如果为空则返回
                    if (tpaListList.Count == 0) continue;
                    //第一个文档
                    ProjectAmount tpa = tpaListList[0][0];
                    //取得算量模板的属性
                    List<string> fieldsList = FiieldTxt(tpa);
                    //转换后的属性表格宽度
                    List<int> columnSizeList = new List<int>();
                    //转换后的属性名称
                    List<string> proNameList = bc.ProTransform(fieldsList, out columnSizeList);
                    //将工作表的名字命名为该种构件的名称
                    sheetList[i] = package.Workbook.Worksheets.Add(tpa.TypeName);
                    //当前所操作的工作表
                    ExcelWorksheet worksheet = sheetList[i];
                    //从第二行开始
                    int row = 2;
                    //总计的工程量数量
                    double allAmount = 0;
                    //表格标题行初始化设置
                    foreach (string str in fieldsList)
                    {
                        //第一行标题字段的对应文字
                        worksheet.Cells[1, fieldsList.IndexOf(str) + 1].Value = proNameList.ElementAt(fieldsList.IndexOf(str));
                        worksheet.Column(fieldsList.IndexOf(str) + 1).Width = columnSizeList.ElementAt(fieldsList.IndexOf(str));
                    }
                    //加一列合计
                    worksheet.Cells[1, fieldsList.Count + 1].Value = "合计(m2)";
                    worksheet.Column(fieldsList.Count + 1).Width = 15;
                    //排列某个工作表
                    foreach (List<ProjectAmount> tpList in tpaListList)
                    {
                        //合计的数据
                        double componetTotal = 0;
                        int rowlast = row;
                        //按行填数据
                        foreach (ProjectAmount tp in tpList)
                        {
                            //按列填数据
                            for (int col = 1; col < fieldsList.Count + 1; col++)
                            {
                                //获得属性集合
                                PropertyInfo[] pinfo = tp.GetType().GetProperties();
                                //获得对应属性的值
                                try
                                {
                                    PropertyInfo pi = pinfo.First(m => m.Name == fieldsList[col - 1]);
                                    //如果该值不为空，则该空填入该值
                                    if (pi != null)
                                        worksheet.Cells[row, col].Value = pi.GetValue(tp);
                                }
                                catch { }
                                //自动调整行高
                                worksheet.Row(row).CustomHeight = true;
                            }
                            if (tpa.TypeName.Contains("模板"))
                                componetTotal += tp.TemplateAmount;
                            else
                                componetTotal += tp.ConcretVolumes;
                            row++;
                        }
                        worksheet.Cells[rowlast, fieldsList.Count + 1].Value = componetTotal;
                        allAmount += componetTotal;
                        //TODO：如果只有一块，不知合并是否会报错
                        if (row - rowlast - 1 > 0)
                        {
                            if (tpa.TypeName.Contains("模板"))
                            {
                                //第几行开始为模板数据
                                int tpColNum = fieldsList.IndexOf("TpId");
                                //将其它列进行合并

                                for (int num = 1; num < tpColNum + 1; num++)
                                {
                                    worksheet.Cells[rowlast, num, row - 1, num].Merge = true;
                                }
                            }

                            else//混凝土不知是否有需要
                            {

                            }
                            worksheet.Cells[rowlast, fieldsList.Count + 1, row-1, fieldsList.Count + 1].Merge = true;
                        }

                    }
                    i++;
                    worksheet.Cells[row, 1].Value = "总计";
                    if (worksheet.Name.Contains("模板"))
                    {
                        worksheet.Cells[row, fieldsList.Count +1].Value = allAmount;
                        worksheet.Cells[row, fieldsList.Count+2].Value = "平方米";
                    }
                    else
                    {
                        worksheet.Cells[row, fieldsList.Count+1].Value = allAmount;
                        worksheet.Cells[row, fieldsList.Count + 2].Value = "立方米";
                    }
                    //row = row + 2;
                    //if (worksheet.Name.Contains("工程量"))
                    //{
                    //    worksheet.Cells[row, 1].Value = "混凝土等级";
                    //    worksheet.Cells[row, 2].Value = "总量";
                    //    worksheet.Cells[row, 3].Value = "单位";
                    //    row++;
                    //    List<string> materiaTypes = new List<string>();
                    //    for (int j = 0; j < tpaListList.Count; j++)
                    //    {
                    //        ProjectAmount tp = tpaListList[j];
                    //        if (materiaTypes.Count == 0 || materiaTypes.Where(m => m == tp.MaterialName).Count() == 0)
                    //            materiaTypes.Add(tp.MaterialName);
                    //    }
                    //    foreach (string materia in materiaTypes)
                    //    {
                    //        int index = materiaTypes.IndexOf(materia);
                    //        double num = 0;
                    //        tpaListList.Where(m => m.MaterialName == materia).ToList().ConvertAll(m => num += m.ConcretVolumes);
                    //        worksheet.Cells[row, 1].Value = materia;
                    //        worksheet.Cells[row, 2].Value = bc.TRF(num, 3);
                    //        worksheet.Cells[row, 3].Value = "立方米";
                    //        row++;
                    //    }

                    //}

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
                    for (int k = 0; k < columnSizeList.Count; k++)
                    {
                        worksheet.Column(k + 1).Width = columnSizeList[k];
                    }

                    for (int p = 1; p < row+1; p++)
                    {
                        worksheet.Row(p).Height = 20;
                        for (int l = 1; l < columnSizeList.Count + 2; l++)
                        {
                            worksheet.Cells[p, l].Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.FromArgb(191, 191, 191));//设置单元格所有边框
                        }
                    }
                }
                package.Save();
            }

        }

        private List<string> FiieldTxt(ProjectAmount l)
        {

            Type tpaTp = l.GetType();
            List<string> fieldList = new List<string>();
            PropertyInfo[] ps = tpaTp.GetProperties();
            foreach (PropertyInfo pi in ps)
            {
                if (pi.GetValue(l) == null) continue;
                try
                {
                    object obj = pi.GetValue(l);
                    if (obj is int i)
                    {
                        if (i == 0) continue;
                    }
                    else if (obj is double d)
                    {
                        if (d == 0) continue;
                    }
                    //if (pi.GetType().IsPrimitive == true && (int)pi.GetValue(l) == 0)
                    //    continue;
                }
                catch { }
                fieldList.Add(pi.Name);
            }
            return fieldList;
        }


    }
}


