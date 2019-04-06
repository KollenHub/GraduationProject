using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using bc = TemplateCount.BasisCode;
namespace TemplateCount
{
    public static class ProjectCountCommand
    {


        public static List<List<Element>> TemplateQuantityCount(Document doc, bc.TypeName tyName, List<Level> levList)
        {
            List<Element> hostElemList = new List<Element>();
            foreach (Level lev in levList)
            {
                switch (tyName)
                {
                    case bc.TypeName.板模板:
                        hostElemList .AddRange(bc.FilterElementList<Floor>(doc, BuiltInCategory.OST_Floors,lev));
                        break;
                    case bc.TypeName.梁模板:
                        hostElemList.AddRange(bc.FilterElementList<FamilyInstance>(doc, BuiltInCategory.OST_StructuralFraming,lev));
                        break;
                    case bc.TypeName.柱模板:
                        hostElemList .AddRange( bc.FilterElementList<FamilyInstance>(doc, BuiltInCategory.OST_StructuralColumns,lev));
                        break;
                    case bc.TypeName.墙模板:
                        hostElemList = bc.FilterElementList<Wall>(doc);
                        break;
                    case bc.TypeName.楼梯模板:
                        hostElemList = bc.FilterElementList(doc, BuiltInCategory.OST_Stairs);
                        break;
                    case bc.TypeName.基础模板:
                        hostElemList = bc.FilterElementList(doc, BuiltInCategory.OST_StructuralFoundation);
                        break;
                    default:
                        break;
                }
            }

            List<Element> levElemList = new List<Element>();
            //标高筛选

            //if (tyName == bc.TypeName.梁模板)
            //    levElemList.AddRange(hostElemList.Where(m => (m as FamilyInstance).Host.Id.IntegerValue == lev.Id.IntegerValue).Count()>0?
            //        hostElemList.Where(m => (m as FamilyInstance).Host.Id.IntegerValue == lev.Id.IntegerValue):new List<Element>());
            //else
            //    levElemList.AddRange(hostElemList.Where(m => m.LevelId.IntegerValue == lev.Id.IntegerValue).Count()>0?
            //        hostElemList.Where(m => m.LevelId.IntegerValue == lev.Id.IntegerValue):new List<Element>());
            //每个主体对应的模板
            List<List<Element>> TpListList = hostElemList.ConvertAll(m => new List<Element>() { m });
            //模板集合
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



        /// <summary>
        /// 梁柱混凝土工程量计算
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="InstanceList">全部梁的集合</param>
        /// <param name="joinBeamList">被剪切梁的集合</param>
        /// <param name="tpAmountList">导出的工程量集合</param>
        public static void ConcretQuantityCount(Document doc, List<FamilyInstance> InstanceList, bc.TypeName tpname, out List<ProjectAmount> tpAmountList)
        {
            tpAmountList = new List<ProjectAmount>();
            Level lev = null;
            if (tpname == bc.TypeName.梁砼工程量)
            {
                lev = doc.GetElement(InstanceList[0].Host.Id) as Level;
            }
            else if (tpname == bc.TypeName.柱砼工程量)
            {
                lev = doc.GetElement(InstanceList[0].LevelId) as Level;
            }
            foreach (FamilyInstance inst in InstanceList)
            {
                double instanceVolume = 0;
                List<Solid> instanceSolidList = bc.AllSolid_Of_Element(inst);
                instanceSolidList.ConvertAll(m => instanceVolume += bc.EVToCV(m.Volume));
                ProjectAmount instanceConcret = new ProjectAmount(inst, lev, tpname, instanceVolume);
                tpAmountList.Add(instanceConcret);
            }
        }
        public static void ConcretQuantityCount(Document doc, List<Floor> flList, bc.TypeName tpname, out List<ProjectAmount> tpAmountList)
        {
            tpAmountList = new List<ProjectAmount>();
            Level lev = doc.GetElement(flList[0].LevelId) as Level;
            foreach (Floor fl in flList)
            {
                double instanceVolume = 0;
                List<Solid> flSolidList = bc.AllSolid_Of_Element(fl);
                flSolidList.ConvertAll(m => instanceVolume += bc.EVToCV(m.Volume));
                ProjectAmount flConcret = new ProjectAmount(fl, lev, bc.TypeName.板砼工程量, instanceVolume);
                tpAmountList.Add(flConcret);
            }
        }


    }
}
