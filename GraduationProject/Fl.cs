using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;

namespace GraduationProject
{
    public class Fl
    {
        /// <summary>
        /// 返回一个标准的算量模板族
        /// </summary>
        public List<TpAmount> FLTpAmount { get; set; }
        //楼板族实现类
        public Fl(ExternalCommandData data)
        {
            UIDoc = data.Application.ActiveUIDocument;
            this.Doc = UIDoc.Document;
            //获取项目的标高，并将其排序成一个集合
            List<Level> levList = GetLevList(Doc);

            foreach (Level lev in levList)
            {
                //楼板轮廓线
                List<Line> flLineList = null;
                //整个楼板的面积
                double floorArea = FlInfo(Doc, lev, out flLineList);
                //获取梁的集合            
                List<LinkInfo> beamInfo =GetBeamInfo(Doc,lev,flLineList);
                //获取扣除的梁模板面积
                double deduct = 0;
                foreach (LinkInfo li in beamInfo)
                {
                    deduct += li.BeamWidth * li.Length;
                }
                FLTpAmount = GetAmount(levList, Doc);
            }
            

            
        }
        /// <summary>
        /// 获取楼板的面积
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="lev">标高</param>
        /// <param name="flLineList">返回楼板轮廓</param>
        /// <returns></returns>

        private double FlInfo(Document doc, Level lev, out List<Line> flLineList)
        {
            List<Floor> flList = new FilteredElementCollector(doc).OfClass(typeof(Floor)).Where(m => m.LevelId.ToString() == lev.Id.ToString()).ToList().ConvertAll(m => m as Floor);
            double flArea = 0;
            if (flList.Count()==1)
            {
                using (Transaction trans=new Transaction(doc))
                {
                    trans.Start("获取轮廓线");
                    List<ElementId> modelLineId = doc.Delete(flList[0].Id).ToList();
                    trans.RollBack();
                    flLineList = modelLineId.ConvertAll(m => doc.GetElement(m)).Where
                   (m => m is ModelCurve).ToList().ConvertAll(m => ((m as ModelCurve).Location as LocationCurve).Curve as Line);
                    flArea = flList[0].get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble() * Math.Pow(0.3048, 2);
                }
            }
            else
            {
                TaskDialog.Show("警告", "请把所有的楼板合成一块板");
                flLineList = null;
            }
            return flArea;
        }


        /// <summary>
        /// 返回梁的净长度以及净宽度
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="lev">所在标高</param>
        /// <param name="flLineList">楼板轮廓廓线</param>
        /// <returns></returns>
        private List<LinkInfo> GetBeamInfo(Document doc, Level lev, List<Line> flLineList)
        {
            List<LinkInfo> beamList = new List<LinkInfo>();
            return beamList;
        }

        private List<Level> GetLevList(Document doc)
        {
            List<Level> levList = new FilteredElementCollector(doc).OfClass(typeof(Level)).ToList().ConvertAll(m => m as Level).OrderBy(m => m.Elevation).ToList();
            return levList;
        }

        private List<TpAmount> GetAmount(List<Level> levList, Document doc)
        {
            List<TpAmount> flAmountList = new List<TpAmount>();
            List<Floor> foorlist = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Floors).ToList().ConvertAll(m => m as Floor).Where(m => m != null).ToList();
            List<List<Line>> flLineList = new List<List<Line>>();
            foreach (Level lev in levList)
            {
                List<Floor> levflList = foorlist.Where(m => m.LevelId.ToString() == lev.Id.ToString()).ToList();
                double flTPValue = 0;
                List<Line> levLine = new List<Line>();
                foreach (Floor fl in levflList)
                {
                    Parameter flPar = fl.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                    flTPValue += flPar.AsDouble() * Math.Pow(0.3048, 2);
                    Transaction trans = new Transaction(doc);
                    trans.Start("获取楼板轮廓线");
                    List<ElementId> lineId = doc.Delete(fl.Id).ToList();
                    trans.RollBack();
                    foreach (ElementId lId in lineId)
                    {
                        Element elem = doc.GetElement(lId);
                        if (elem is ModelCurve)
                        {
                            levLine.Add(((elem as ModelCurve).Location as LocationCurve).Curve as Line);
                        }
                    }

                }
                TaskDialog.Show("面积", flTPValue.ToString());
            }
            return flAmountList;

        }

        public UIDocument UIDoc { get; set; }
        public Document Doc { get; set; }

    }
}
