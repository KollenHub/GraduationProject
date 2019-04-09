using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using bc = TemplateCount.BasisCode;
namespace TemplateCount
{
    public static class ProjectCountCommand
    {
        /// <summary>
        /// 模板计算工程量方法
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="tyName"></param>
        /// <param name="levList"></param>
        /// <returns></returns>
        public static List<List<Element>> BuiltingQuantityCount(Document doc, bc.TypeName tyName, List<Level> levList)
        {
            List<Element> hostElemList = new List<Element>();
            string ty = Enum.GetName(typeof(bc.TypeName), tyName);
            //作为判断的中介
            string p = null;
            if (ty.Contains("模板"))
               p = ty.Split(new char[] { '模' })[0];
            else
               p = ty.Split(new char[] { '砼' })[0];
            foreach (Level lev in levList)
            {
                switch (p)
                {
                    case "板":
                        hostElemList .AddRange(bc.FilterElementList<Floor>(doc, BuiltInCategory.OST_Floors,lev));
                        break;
                    case "梁":
                        hostElemList.AddRange(bc.FilterElementList<FamilyInstance>(doc, BuiltInCategory.OST_StructuralFraming,lev));
                        break;
                    case "柱":
                        hostElemList .AddRange( bc.FilterElementList<FamilyInstance>(doc, BuiltInCategory.OST_StructuralColumns,lev));
                        break;
                    case "墙":
                        hostElemList.AddRange(bc.FilterElementList<Wall>(doc, BuiltInCategory.OST_Walls, lev));
                        break;
                    case "楼梯":
                        hostElemList.AddRange(bc.FilterElementList<Stairs>(doc, BuiltInCategory.OST_Stairs, lev));
                        break;
                    case "基础":
                        hostElemList.AddRange(bc.FilterElementList<FamilyInstance>(doc, BuiltInCategory.OST_StructuralFoundation,lev));
                        break;
                    default:
                        break;
                }
            }
            
            //模板集合
            if (ty.Contains("模板"))
            {
                List<List<Element>> TpListList = hostElemList.ConvertAll(m => new List<Element>() { m });
                List<Element> dsList = bc.FilterElementList<DirectShape>(doc);
                foreach (DirectShape ds in dsList)
                {
                    int dsId = ds.LookupParameter("HostElemID").AsInteger();
                    //MessageBox.Show("sdjhashdh");
                    foreach (List<Element> tpList in TpListList)
                    {
                        if (tpList[0].Id.IntegerValue == dsId)
                        {
                            tpList.Add(ds);
                            break;
                        }
                    }
                }
                return TpListList;
            }
            return new List<List<Element>>() { hostElemList };
        }
    }
}
