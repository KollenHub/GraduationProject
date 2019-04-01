using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;
using bc = TemplateCount.BasisCode;
namespace TemplateCount
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MainClass : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            int judge = 100;
            do
            {
                Win win = new Win(uiDoc);
                win.ShowDialog();
                judge = win.p;
                if (judge == 1)
                {
                    List<List<ProjectAmount>> tpAList_List = new List<List<ProjectAmount>>();
                    //项目标高集合
                    List<int> levIndexList =new List<int>();
                    foreach (int i in win.levList)
                    {
                        if (i >= 0 && i <= bc.GetLevList(doc).Count) levIndexList.Add(i);
                    }
                    List<Level> lev_List = levIndexList.ConvertAll(m=>bc.GetLevList(doc).ElementAt(m-1));
                    
                    //梁模板集合
                    List<ProjectAmount> beamTpa_List = new List<ProjectAmount>();
                    //板模板集合
                    List<ProjectAmount> flTpa_List = new List<ProjectAmount>();
                    //柱模板集合
                    List<ProjectAmount> colTpa_List = new List<ProjectAmount>();
                    //梁混凝土
                    List<ProjectAmount> beamConcret_List = new List<ProjectAmount>();
                    //柱混凝土
                    List<ProjectAmount> colConcret_List = new List<ProjectAmount>();
                    //板混凝土
                    List<ProjectAmount> flConcret_List = new List<ProjectAmount>();
                    //获取所有构件分别对应的模板集合
                    try
                    {
                        List<string> strList = win.chbStrList;
                    foreach (string str in strList)
                    {
                        switch (str)
                        {
                            case "梁模板":
                                tpAList_List.Add(beamTpa_List);
                                break;
                            case "柱模板":
                                tpAList_List.Add(colTpa_List);
                                break;
                            case "板模板":
                                tpAList_List.Add(flTpa_List);
                                break;
                            case "梁混凝土量":
                                tpAList_List.Add(beamConcret_List);
                                break;
                            case "柱混凝土量":
                                tpAList_List.Add(colConcret_List);
                                break;
                            case "板混凝土量":
                                tpAList_List.Add(flConcret_List);
                                break;

                            default:
                                break;
                        }
                    }
                    ExportToExcel worsheet = new ExportToExcel(tpAList_List);
                }
                    catch
                {
                    judge = 0;
                }
            }
            } while (judge == 0);
            TaskDialog.Show("提示", "表格导出完成", TaskDialogCommonButtons.Yes);
            return Result.Succeeded;

        }
    }
}
