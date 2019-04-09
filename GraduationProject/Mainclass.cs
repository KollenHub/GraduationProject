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
using TpCount = TemplateCount.ProjectCountCommand;
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
            Win win = new Win(uiDoc);
            if (win.ShowDialog() == false) return Result.Succeeded;
            judge = win.p;
            if (judge == 1)
            {
                List<List<List<ProjectAmount>>> tpAListList_List = new List<List<List<ProjectAmount>>>();
                //项目标高集合
                List<int> levIndexList = win.levList.Where((m => m > 0 && m <= bc.GetLevList(doc).Count || m == int.MaxValue)).ToList();
                List<Level> lev_List = levIndexList.Contains(int.MaxValue) ? bc.GetLevList(doc) : levIndexList.ConvertAll(m => bc.GetLevList(doc).ElementAt(m - 1));
                //try
                //{
                List<string> strList = win.chbStrList;
                foreach (string str in strList)
                {
                    switch (str)
                    {
                        //TODO:没有生成模板的构件标记
                        case "梁模板":
                            List<List<Element>> beamTpListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.梁模板, lev_List);
                            List<List<ProjectAmount>> beamTpa_List = bc.ProjectAmoutList(beamTpListList, bc.TypeName.梁模板);
                            tpAListList_List.Add(beamTpa_List);
                            break;
                        case "柱模板":
                            List<List<Element>> colTpListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.柱模板, lev_List);
                            List<List<ProjectAmount>> colTpa_List = bc.ProjectAmoutList(colTpListList, bc.TypeName.柱模板);
                            tpAListList_List.Add(colTpa_List);
                            break;
                        case "板模板":
                            List<List<Element>> flTpListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.板模板, lev_List);
                            List<List<ProjectAmount>> flTpa_List = bc.ProjectAmoutList(flTpListList, bc.TypeName.板模板);
                            tpAListList_List.Add(flTpa_List);
                            break;
                        case "墙模板":
                            List<List<Element>> walTpListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.墙模板, lev_List);
                            List<List<ProjectAmount>> walTpa_List = bc.ProjectAmoutList(walTpListList,bc.TypeName.墙模板);
                            tpAListList_List.Add(walTpa_List);
                            break;
                        case "楼梯模板":
                            List<List<Element>> stairTpListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.楼梯模板, lev_List);
                            List<List<ProjectAmount>> stairTpa_List = bc.ProjectAmoutList(stairTpListList, bc.TypeName.楼梯模板);
                            tpAListList_List.Add(stairTpa_List);
                            break;
                        case "基础模板":
                            List<List<Element>> basisTpListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.基础模板, lev_List);
                            List<List<ProjectAmount>> basisTpa_List = bc.ProjectAmoutList(basisTpListList, bc.TypeName.基础模板);
                            tpAListList_List.Add(basisTpa_List);
                            break;
                        case "梁混凝土量":
                            List<List<Element>> beamListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.梁砼工程量, lev_List);
                            List<List<ProjectAmount>> beamConcret_List = bc.ProjectAmoutList(beamListList, bc.TypeName.梁砼工程量);
                            tpAListList_List.Add(beamConcret_List);
                            break;
                        case "柱混凝土量":
                            List<List<Element>> colListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.柱砼工程量, lev_List);
                            List<List<ProjectAmount>> colConcret_List = bc.ProjectAmoutList(colListList, bc.TypeName.柱砼工程量);
                            tpAListList_List.Add(colConcret_List);
                            break;
                        case "板混凝土量":
                            List<List<Element>> fllist = TpCount.BuiltingQuantityCount(doc, bc.TypeName.板砼工程量, lev_List);
                            List<List<ProjectAmount>> flConcret_List = bc.ProjectAmoutList(fllist, bc.TypeName.板砼工程量);
                            tpAListList_List.Add(flConcret_List);
                            break;
                        case "墙混凝土量":
                            List<List<Element>> walListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.墙砼工程量, lev_List);
                            List<List<ProjectAmount>> walConcret_List = bc.ProjectAmoutList(walListList, bc.TypeName.墙砼工程量);
                            tpAListList_List.Add(walConcret_List);
                            break;
                        case "楼梯混凝土量":
                            List<List<Element>> stairListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.楼梯砼工程量, lev_List);
                            List<List<ProjectAmount>> stairConcret_List = bc.ProjectAmoutList(stairListList, bc.TypeName.楼梯砼工程量);
                            tpAListList_List.Add(stairConcret_List);
                            break;
                        case "基础混凝土量":
                            List<List<Element>> basisListList = TpCount.BuiltingQuantityCount(doc, bc.TypeName.基础砼工程量, lev_List);
                            List<List<ProjectAmount>> basisConcret_List = bc.ProjectAmoutList(basisListList, bc.TypeName.基础砼工程量);
                            tpAListList_List.Add(basisConcret_List);
                            break;

                        default:
                            break;
                    }
                }
                ExportToExcel worsheet = new ExportToExcel(tpAListList_List);
            }
            TaskDialog.Show("提示", "表格导出完成", TaskDialogCommonButtons.Yes);
            return Result.Succeeded;

        }
    }
}
