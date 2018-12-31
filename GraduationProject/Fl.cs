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
                List<LinkInfo> beamInfo = GetBeamInfo(Doc, lev, flLineList);
                //获取扣除的梁模板面积
                double deduct = 0;
                foreach (LinkInfo li in beamInfo)
                {
                    deduct += li.BeamWidth * li.Length;
                }

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
            if (flList.Count() == 1)
            {
                using (Transaction trans = new Transaction(doc))
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
            List<FamilyInstance> beamInstanceList = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory
                (BuiltInCategory.OST_StructuralFraming).Where(m => m.LevelId.ToString() == lev.Id.ToString()).ToList().ConvertAll(m => m as FamilyInstance);
            List<FamilyInstance> vaildBeamInsList = new List<FamilyInstance>();
            foreach (FamilyInstance bInst in beamInstanceList)
            {
                if ()
                {

                }
            }
            return beamList;

        }
        /// <summary>
        /// 判断梁的中心线是否落在楼板轮廓内
        /// </summary>
        /// <param name="flLineList">楼板轮廓线</param>
        /// <param name="beamInst">梁实例</param>
        /// <param name="realValue">实际梁在楼板内的长度</param>
        /// <returns>是否梁中心线在楼板内</returns>
        public bool IsIncluded(List<Line> flLineList, FamilyInstance beamInst, out double realValue)
        {
            //射线法基点的获取
            double z = (Doc.GetElement(beamInst.LevelId) as Level).Elevation;
            XYZ basicXYZ =GetBasicXYZ(flLineList,z);
            //梁中心线是否在楼板内
            //总体思路为：获取梁中心线的端点---通过端点与射线法基点形成两端线段---判断这两条线段与轮廓线的交点情况
            //如果交点只有一个，那么该端点在楼板封闭区间内
            //如果交点超过一个，那么该端点在楼板封闭区间外
            //---判断两个端点是否都落在楼板封闭区间内
            //都落在楼板内，实际长度取梁长度，都不落在楼板内，实际长度取0，一个在一个不在，实际长度取于楼板轮廓交点到楼板内端点的长度
            Line beamLocationLine = (beamInst.Location as LocationCurve).Curve as Line;
            XYZ pOne = beamLocationLine.GetEndPoint(0);
            XYZ pTwo = beamLocationLine.GetEndPoint(1);
            Line ln1 = Line.CreateBound(pOne, basicXYZ);
            Line ln2 = Line.CreateBound(pTwo, basicXYZ);
            IntersectionResultArray instRstAray1 = new IntersectionResultArray();
            IntersectionResultArray instRstAray2 = new IntersectionResultArray();
            int? pint1 = null;
            int? pint2 = null;
            XYZ realXYZOne = new XYZ();
            XYZ realXYZTwo = new XYZ();
            foreach (Line lin in flLineList)
            {
                pint1 = 0;
                List<Line> lineList = new List<Line>();
                if (pOne.IsAlmostEqualTo(lin.GetEndPoint(0)) || pOne.IsAlmostEqualTo(lin.GetEndPoint(1))) { pint1 = 1; break; }
                if (Line.CreateBound(pOne, lin.GetEndPoint(0)).Length + Line.CreateBound(pOne, lin.GetEndPoint(1)).Length == lin.Length) { pint1 = 1; break; }
                SetComparisonResult stCRst = lin.Intersect(ln1, out instRstAray1);
                if (stCRst == SetComparisonResult.Overlap && instRstAray1.Size == 1)
                {
                    pint1++;
                }
            }
            foreach (Line lin in flLineList)
            {
                pint2 = 0;
                List<Line> lineList = new List<Line>();
                if (pTwo.IsAlmostEqualTo(lin.GetEndPoint(0)) || pTwo.IsAlmostEqualTo(lin.GetEndPoint(1))) { pint2 = 1; break; }
                if (Line.CreateBound(pTwo, lin.GetEndPoint(0)).Length + Line.CreateBound(pTwo, lin.GetEndPoint(1)).Length == lin.Length) { pint2 = 1; break; }
                SetComparisonResult stCRst = lin.Intersect(ln2, out instRstAray2);
                if (stCRst == SetComparisonResult.Overlap && instRstAray2.Size == 1)
                {
                    pint2++;
                }
            }
            if (pint1 == 1 && pint2 == 1)
            {
                realValue = beamLocationLine.Length;
                return true;
            }
            else if (pint1 > 1 && pint2 > 1)
            {
                realValue = 0;
                return false;
            }
            else if (pint2 > 1)
            {
                IntersectionResultArray instRstAray3 = GetInstRstAray(flLineList, beamLocationLine);
                if (instRstAray3 != null) pTwo = instRstAray3.get_Item(0).XYZPoint;
                Line newLocationCurve = Line.CreateBound(pOne, pTwo);
                realValue = newLocationCurve.Length;
                return true;
            }
            else
            {
                IntersectionResultArray instRstAry4 = GetInstRstAray(flLineList, beamLocationLine);
                if (instRstAry4 != null) pOne = instRstAry4.get_Item(0).XYZPoint;
                Line newLocationCurve = Line.CreateBound(pOne, pTwo);
                realValue = newLocationCurve.Length;
                return true;
            }
        }

        /// <summary>
        /// 获取一个在封闭区间外的基点作为射线法的基点
        /// </summary>
        /// <param name="flLineList">封闭轮廓线</param>
        /// <param name="z">点的高程点，保证该点与轮廓线在相同的平面</param>
        /// <returns>基点坐标</returns>
        private XYZ GetBasicXYZ(List<Line> flLineList, double z)
        {
            //获取在某个封闭区间的基点
            double x = 0;
            double x0 = flLineList.OrderByDescending(m => m.GetEndPoint(0).X).ToList().ConvertAll(m => m.GetEndPoint(0).X).First();
            double x1 = flLineList.OrderByDescending(m => m.GetEndPoint(1).X).ToList().ConvertAll(m => m.GetEndPoint(1).X).First();
            double y = 0;
            double y0 = flLineList.OrderByDescending(m => m.GetEndPoint(0).Y).ToList().ConvertAll(m => m.GetEndPoint(0).Y).First();
            double y1 = flLineList.OrderByDescending(m => m.GetEndPoint(1).Y).ToList().ConvertAll(m => m.GetEndPoint(1).Y).First();
            if (x0 > x1) { x = x0; } else x = x1;
            if (y0 > y1) { x = y0; } else y = y1;
            return new XYZ(x+100, y+100, z);
        }

        /// <summary>
        ///当有一个点在封闭区间外，一个在封闭区间内时，获取该线段与封闭区间轮廓线的交点信息
        /// </summary>
        /// <param name="flLineList">楼板的边界线</param>
        /// <param name="beamLocationLine">梁的中线</param>
        /// <returns>交点的集合</returns>
        private IntersectionResultArray GetInstRstAray(List<Line> flLineList, Line beamLocationLine)
        {
            IntersectionResultArray instRstAray = new IntersectionResultArray();
            foreach (Line fLin in flLineList)
            {
                SetComparisonResult stRst = fLin.Intersect(beamLocationLine, out instRstAray);
                if (stRst == SetComparisonResult.Overlap && instRstAray.Size == 1)
                {
                    return instRstAray;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取项目的标高，并按照标高的高程由小到大进行排列
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        private List<Level> GetLevList(Document doc)
        {
            List<Level> levList = new FilteredElementCollector(doc).OfClass(typeof(Level)).ToList().ConvertAll(m => m as Level).OrderBy(m => m.Elevation).ToList();
            return levList;
        }

        //尚未完成
        /// <summary>
        /// 获取模板工程量
        /// </summary>
        /// <param name="levList"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
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
