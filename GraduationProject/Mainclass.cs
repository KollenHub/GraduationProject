using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;
namespace TemplateCount
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MainClass : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            BasisCode bc = new BasisCode();
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
                    List<List<TpAmount>> tpAList_List = new List<List<TpAmount>>();
                    //项目标高集合
                    List<Level> lev_List = bc.GetLevList(doc);
                    //梁模板集合
                    List<TpAmount> beamTpa_List = new List<TpAmount>();
                    //板模板集合
                    List<TpAmount> flTpa_List = new List<TpAmount>();
                    //柱模板集合
                    List<TpAmount> colTpa_List = new List<TpAmount>();
                    //获取所有构件分别对应的模板集合
                    bc.AllELmentList(doc, lev_List, out beamTpa_List, out flTpa_List, out colTpa_List);
                    //try
                    //{
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
                                case "剪力墙模板":
                                    break;
                                case "楼梯模板":
                                    break;
                                default:
                                    break;
                            }
                        }
                        ExportToExcel worsheet = new ExportToExcel(tpAList_List);
                    //}
                    //catch
                    //{
                    //    judge = 0;
                    //}
                }
            } while (judge == 0);

            return Result.Succeeded;

        }
    }
}
