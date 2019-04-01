using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using bc = TemplateCount.BasisCode;
namespace TemplateCount
{
    public static class ProjectCountCommand
    {
        //定义不同构件属性的枚举
        public enum TypeName
        {
            板模板 = 0,
            梁模板 = 1,
            柱模板 = 2,
            墙模板=3,
            楼梯模板=4,
            基础模板=5,
            梁砼工程量 = 6,
            板砼工程量 = 7,
            柱砼工程量 = 8,
            墙砼工程量 = 9,
            楼梯砼工程量 =10,
            基础砼工程量 = 11,

        }

        public static List<ProjectAmount> TemplateQuantityCount (Document doc,TypeName tyName)
        {
            List<ProjectAmount> wordAmountList = new List<ProjectAmount>();
            switch (tyName)
            {
                case TypeName.板模板:
                    List<Element> flList = bc.FilterElementList<Floor>(doc,BuiltInCategory.OST_Floors);
                    break;
                case TypeName.梁模板:
                    List<Element> beamList = bc.FilterElementList<FamilyInstance>(doc, BuiltInCategory.OST_StructuralFraming);
                    break;
                case TypeName.柱模板:
                    List<Element> columnList = bc.FilterElementList<FamilyInstance>(doc, BuiltInCategory.OST_StructuralColumns);
                    break;
                case TypeName.墙模板:
                    List<Element> wallList = bc.FilterElementList<Wall>(doc);
                    break;
                case TypeName.楼梯模板:
                    List<Element> stairList = bc.FilterElementList(doc, BuiltInCategory.OST_Stairs);
                    break;
                case TypeName.基础模板:
                    List<Element> foundationList = bc.FilterElementList(doc, BuiltInCategory.OST_StructuralFoundation);
                    break;
                default:
                    break;
            }
            return null;
        }

        

        /// <summary>
        /// 梁柱混凝土工程量计算
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="InstanceList">全部梁的集合</param>
        /// <param name="joinBeamList">被剪切梁的集合</param>
        /// <param name="tpAmountList">导出的工程量集合</param>
        public static void ConcretQuantityCount(Document doc, List<FamilyInstance> InstanceList, TypeName tpname, out List<ProjectAmount> tpAmountList)
        {
            tpAmountList = new List<ProjectAmount>();
            Level lev = null;
            if (tpname == TypeName.梁砼工程量)
            {
                lev = doc.GetElement(InstanceList[0].Host.Id) as Level;
            }
            else if (tpname==TypeName.柱砼工程量)
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
        public static void ConcretQuantityCount(Document doc,List<Floor> flList,TypeName tpname,out List<ProjectAmount> tpAmountList)
        {
            tpAmountList = new List<ProjectAmount>();
            Level lev = doc.GetElement(flList[0].LevelId) as Level;
            foreach (Floor fl in flList)
            {
                double instanceVolume = 0;
                List<Solid> flSolidList = bc.AllSolid_Of_Element(fl);
                flSolidList.ConvertAll(m => instanceVolume += bc.EVToCV(m.Volume));
                ProjectAmount flConcret = new ProjectAmount(fl, lev, TypeName.板砼工程量, instanceVolume);
                tpAmountList.Add(flConcret);
            }
        }


    }
}
