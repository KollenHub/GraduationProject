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
            if(win.ShowDialog()==false)return Result.Succeeded;
            judge = win.p;
            if (judge == 1)
            {
                List<List<List<ProjectAmount>>> tpAListList_List = new List<List<List<ProjectAmount>>>();
                //项目标高集合
                List<int> levIndexList = win.levList.Where((m=>m>0&&m<=bc.GetLevList(doc).Count || m==int.MaxValue)).ToList();
                List<Level> lev_List=levIndexList.Contains(int.MaxValue)?bc.GetLevList(doc):levIndexList.ConvertAll(m => bc.GetLevList(doc).ElementAt(m - 1));
                //梁混凝土
                List<List<ProjectAmount>> beamConcret_List = new List<List<ProjectAmount>>();
                //柱混凝土
                List<List<ProjectAmount>> colConcret_List = new List<List<ProjectAmount>>();
                //板混凝土
                List<List<ProjectAmount>> flConcret_List = new List<List<ProjectAmount>>();
                //作为存储projectAmount的中间数组
                List<ProjectAmount> pAList = null;
                //获取所有构件分别对应的模板集合
                //try
                //{
                List<string> strList = win.chbStrList;
                foreach (string str in strList)
                {
                    switch (str)
                    {
                        //TODO:没有生成模板的构件标记
                        case "梁模板":
                            List<List<ProjectAmount>> beamTpa_List = new List<List<ProjectAmount>>();
                            List<List<Element>> beamTpListList = TpCount.TemplateQuantityCount(doc, bc.TypeName.梁模板,lev_List);
                            foreach (List<Element> beamList in beamTpListList)
                            {
                                pAList = new List<ProjectAmount>();
                                FamilyInstance fi = beamList[0] as FamilyInstance;
                                Level lev = doc.GetElement(fi.Host.Id) as Level;
                                foreach (Element ds in beamList)
                                {
                                    if (beamList.IndexOf(ds) == 0) continue;
                                    double tepArea = Convert.ToDouble(ds.LookupParameter("模板面积").AsValueString());
                                    int index = beamList.IndexOf(ds);
                                    ProjectAmount beamTpA = new ProjectAmount(ds,bc.TypeName.梁模板,index<2?true:false);
                                    pAList.Add(beamTpA);
                                }
                                beamTpa_List.Add(pAList);
                            }
                            tpAListList_List.Add(beamTpa_List);
                            break;
                        case "柱模板":
                            List<List<ProjectAmount>> colTpa_List = new List<List<ProjectAmount>>();
                            List<List<Element>> colTpListList = TpCount.TemplateQuantityCount(doc, bc.TypeName.柱模板,lev_List);
                            foreach (List<Element> colList in colTpListList)
                            {
                                pAList = new List<ProjectAmount>();
                                FamilyInstance fi = colList[0] as FamilyInstance;
                                Level lev = doc.GetElement(fi.LevelId) as Level;
                                foreach (Element ds in colList)
                                {
                                    if (colList.IndexOf(ds) == 0) continue;
                                    double tepArea = Convert.ToDouble(ds.LookupParameter("模板面积").AsValueString());
                                    int index =colList.IndexOf(ds);
                                    ProjectAmount beamTpA = new ProjectAmount(ds, bc.TypeName.柱模板, index < 2 ? true : false);
                                    pAList.Add(beamTpA);
                                }
                               colTpa_List.Add(pAList);
                            }
                            tpAListList_List.Add(colTpa_List);
                            break;
                        case "板模板":
                            List<List<ProjectAmount>> flTpa_List = new List<List<ProjectAmount>>();
                            List<List<Element>> flTpListList = TpCount.TemplateQuantityCount(doc, bc.TypeName.板模板,lev_List);
                            foreach (List<Element> flList in flTpListList)
                            {
                                pAList = new List<ProjectAmount>();
                                Floor fl = flList[0] as Floor;
                                Level lev = doc.GetElement(fl.LevelId) as Level;
                                foreach (Element ds in flList)
                                {
                                    if (flList.IndexOf(ds) == 0) continue;
                                    double tepArea = Convert.ToDouble(ds.LookupParameter("模板面积").AsValueString());
                                    int index = flList.IndexOf(ds);
                                    ProjectAmount beamTpA = new ProjectAmount(ds, bc.TypeName.板模板, index < 2 ? true : false);
                                    pAList.Add(beamTpA);
                                }
                                flTpa_List.Add(pAList);
                            }
                            tpAListList_List.Add(flTpa_List);
                            break;
                        case "梁混凝土量":
                            tpAListList_List.Add(beamConcret_List);
                            break;
                        case "柱混凝土量":
                            tpAListList_List.Add(colConcret_List);
                            break;
                        case "板混凝土量":
                            tpAListList_List.Add(flConcret_List);
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
