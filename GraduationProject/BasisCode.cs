using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SWF = System.Windows.Forms;
using Autodesk.Revit.DB;
namespace TemplateCount
{
    public class BasisCode
    {
        /// <summary>
        /// 返回项目中按照高程从小到大排列后的标高集合
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <returns></returns>
        public List<Level> GetLevList(Document doc)
        {
            List<Level> levList = new FilteredElementCollector(doc).OfClass(typeof(Level)).OfCategory(BuiltInCategory.OST_Levels).ToList()
                .ConvertAll(m => (m as Level)).OrderBy(m => m.Elevation).ToList();
            return levList;
        }
        /// <summary>
        /// 返回过滤器元素集合
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="ty">查找的元素类型</param>
        /// <param name="bIC">查找的元素对应的BuiltInCategory</param>
        /// <returns></returns>
        public List<Element> FilterElementList(Document doc, Type ty, BuiltInCategory bIC)
        {
            List<Element> elemList = new FilteredElementCollector(doc).OfClass(ty).OfCategory(bIC).ToList();
            return elemList;
        }
        /// <summary>
        /// 返回过滤器元素集合
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="ty">查找的元素类型</param>
        /// <returns></returns>
        public List<Element> FilterElementList(Document doc, Type ty)
        {

            List<Element> elemList = new FilteredElementCollector(doc).OfClass(ty).ToList();
            return elemList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="lev_List"></param>
        /// <param name="beamTpa_List"></param>
        /// <param name="flTpa_List"></param>
        /// <param name="colTpa_List"></param>
        /// <param name="beamConcret_List"></param>
        public void AllELmentList(Document doc, List<Level> lev_List, out List<TpAmount> beamTpa_List, out List<TpAmount> flTpa_List, out List<TpAmount> colTpa_List, out List<TpAmount> beamConcret_List, out List<TpAmount> colConcret_List, out List<TpAmount> flConcret_List)
        {
            //梁的集合
            List<FamilyInstance> beamList = FilterElementList(doc, typeof(FamilyInstance), BuiltInCategory.OST_StructuralFraming).ConvertAll(m => m as FamilyInstance);
            //板的集合
            List<Floor> flList = FilterElementList(doc, typeof(Floor)).ConvertAll(m => m as Floor);
            //柱的集合
            List<FamilyInstance> columnList = FilterElementList(doc, typeof(FamilyInstance), BuiltInCategory.OST_StructuralColumns).ConvertAll(m => m as FamilyInstance);
            //梁的模板集合
            beamTpa_List = new List<TpAmount>();
            //板的模板集合
            flTpa_List = new List<TpAmount>();
            //梁混凝土的集合
            beamConcret_List = new List<TpAmount>();
            //柱混凝土集合
            colConcret_List = new List<TpAmount>();
            //板混凝土集合
            flConcret_List = new List<TpAmount>();

            //柱的模板集合
            colTpa_List = new List<TpAmount>();
            foreach (Level lev in lev_List)
            {
                //防止梁的集合为零
                try
                {
                    //该标高下梁的模板集合
                    List<FamilyInstance> levBeam_List = beamList.Where(m => m.Host.Id.IntegerValue == lev.Id.IntegerValue).ToList();
                    if (levBeam_List.Count != 0)
                    {
                        List<FamilyInstance> beCutBeam_List = JoinBeamToBeam(levBeam_List, doc);
                        TpCount levBeamTC = new TpCount(doc, beCutBeam_List, levBeam_List, TpCount.TypeName.梁模板, out List<TpAmount> levBeamTpa);
                        beamTpa_List.AddRange(levBeamTpa);
                        TpCount levBeamCTC = new TpCount(doc, levBeam_List, TpCount.TypeName.梁砼工程量, out List<TpAmount> levBeamCTpa);
                        beamConcret_List.AddRange(levBeamCTpa);
                    }

                }
                catch { }
                //防止板为空
                try
                {
                    //该标高下板的模板集合
                    List<Floor> levFl_List = flList.Where(m => m.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM).AsElementId().IntegerValue == lev.Id.IntegerValue).ToList();
                    if (levFl_List.Count != 0)
                    {
                        TpCount levFlTC = new TpCount(levFl_List, TpCount.TypeName.板模板, lev, out List<TpAmount> levFlTpa);
                        flTpa_List.AddRange(levFlTpa);
                        TpCount levFlCTC = new TpCount(doc, levFl_List, TpCount.TypeName.板砼工程量, out List<TpAmount> levFlCTpa);
                        flConcret_List.AddRange(levFlCTpa);
                    }

                }
                catch { }
                //防止柱子为空
                try
                {
                    List<FamilyInstance> levCol_List = columnList.Where(m => m.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_PARAM).AsElementId().IntegerValue == lev.Id.IntegerValue).ToList();
                    if (levCol_List.Count != 0)
                    {
                        TpCount levColTC = new TpCount(doc, levCol_List, lev, TpCount.TypeName.柱模板, out List<TpAmount> levColTpa);
                        colTpa_List.AddRange(levColTpa);
                        TpCount levColCTC = new TpCount(doc, levCol_List, TpCount.TypeName.柱砼工程量, out List<TpAmount> levColCTpa);
                        colConcret_List.AddRange(levColCTpa);
                    }
                }
                catch
                { }
            }
        }

        /// <summary>
        /// 返回过滤器元素集合
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="bIC">查找的元素对应的BuiltInCategory</param>
        /// <returns></returns>
        public List<Element> FilterElementList(Document doc, BuiltInCategory bIC)
        {
            List<Element> elemList = new FilteredElementCollector(doc).OfCategory(bIC).ToList();
            return elemList;
        }

        /// <summary>
        /// 求一条梁的底面以及其对应的侧面的集合的集合
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="beam">梁实例</param>
        /// <returns>底面侧面集合</returns>
        public List<List<Face>> AllFaceOfBeam(Document doc, FamilyInstance beam)
        {
            //配套的侧面和底面的集合
            List<List<Face>> bsFaceList_List = new List<List<Face>>();
            //构件截面尺寸
            double b = beam.Symbol.LookupParameter("b").AsDouble();
            double h = beam.Symbol.LookupParameter("h").AsDouble();
            Options opt = new Options();
            opt.ComputeReferences = true;
            opt.IncludeNonVisibleObjects = false;
            opt.DetailLevel = doc.ActiveView.DetailLevel;
            GeometryElement geoElem = beam.get_Geometry(opt);
            IEnumerator geoelemIEnum = geoElem.GetEnumerator();
            while (geoelemIEnum.MoveNext() == true)
            {
                GeometryObject geoObjct = geoelemIEnum.Current as GeometryObject;
                if (geoObjct is Solid)
                {
                    Solid sd = geoObjct as Solid;
                    if (sd.Faces.Size <= 1) continue;
                    //底面集合
                    List<Face> bFace_List = new List<Face>();
                    //侧面集合
                    List<Face> sFace_List = new List<Face>();
                    foreach (Face face in sd.Faces)
                    {
                        //如果面积小于等于截面面积，默认该面是端部截面，排除
                        if (TRF(face.Area) <= TRF(b * h)) continue;
                        //如果该面的法向向量与（0，0，-1）一致则说明该面是底面，获取它
                        if (face.ComputeNormal(new UV(0, 0)).IsAlmostEqualTo(new XYZ(0, 0, -1))) bFace_List.Add(face);
                        //除去顶面，剩下的都是侧面
                        else if (!face.ComputeNormal(new UV(0, 0)).IsAlmostEqualTo(new XYZ(0, 0, 1))) sFace_List.Add(face);
                    }
                    foreach (Face bf in bFace_List)
                    {
                        //每段梁的一个侧面和一个底面，第一个为底面，第二个为侧面
                        List<Face> bsFace_List = new List<Face>();
                        bsFace_List.Add(bf);
                        foreach (Face sf in sFace_List)
                        {
                            //如果底面除以宽度得到的长度与侧面除以高度得到的长度是一致的，则说明该侧面与底面是配套的
                            if (Math.Abs(TRF(sf.Area / h) - TRF(bf.Area / b)) * 304.8 < 0.01)
                            {
                                bsFace_List.Add(sf);
                                break;
                            }
                        }
                        bsFaceList_List.Add(bsFace_List);
                    }
                }

            }
            return bsFaceList_List;
        }

        /// <summary>
        /// 获得柱的侧面
        /// </summary>
        /// <param name="cfi"></param>
        /// <returns></returns>
        public List<Face> ComponentSideFace(FamilyInstance fi)
        {
            List<Face> colsideFaces = new List<Face>();
            Options opt = new Options();
            opt.ComputeReferences = true;
            opt.IncludeNonVisibleObjects = false;
            opt.DetailLevel = ViewDetailLevel.Fine;
            GeometryElement gelem = fi.get_Geometry(opt);
            IEnumerator gEnum = gelem.GetEnumerator();
            while (gEnum.MoveNext())
            {
                GeometryObject gobj = gEnum.Current as GeometryObject;
                if (gobj is Solid)
                {
                    Solid sd = gobj as Solid;
                    foreach (Face face in sd.Faces)
                    {
                        if (face.ComputeNormal(new UV(0, 0)).IsAlmostEqualTo(new XYZ(0, 0, 1)) || face.ComputeNormal(new UV(0, 0)).IsAlmostEqualTo(new XYZ(0, 0, -1)))
                            continue;
                        colsideFaces.Add(face);
                    }
                }
            }
            if (colsideFaces.Count != 4)
            {
                MessageBox.Show("出错了");
                return null;

            }
            return colsideFaces;
        }

        /// <summary>
        /// 返回楼板底边的长和宽
        /// </summary>
        /// <param name="bface">楼板底面的面</param>
        /// <param name="delLengthSize">扣减长边的值(单位为英寸)</param>
        /// <param name="Area">扣减后的面积</param>
        /// <returns></returns>
        public string SlabSize(Face bface, double delLengthSize, out double Area)
        {
            EdgeArrayArray eArrAray = bface.EdgeLoops;
            if (eArrAray.Size > 1)
            {
                Area = EAToCA(bface.Area);
                return "-";
            }
            else
            {
                EdgeArray eAray = eArrAray.get_Item(0);
                if (eAray.Size != 4)
                {
                    Area = EAToCA(bface.Area);
                    return "-";
                }
                List<Line> linList = new List<Line>();
                foreach (Edge ed in eAray)
                {
                    Line l = (ed.AsCurve()) as Line;
                    linList.Add(l);
                }
                foreach (Line l1 in linList)
                {
                    XYZ dirt1 = l1.Direction;
                    foreach (Line l2 in linList)
                    {
                        XYZ dirt2 = l2.Direction;
                        if (dirt1.IsAlmostEqualTo(dirt2)) continue;
                        double angle = dirt1.X * dirt2.X + dirt1.Y * dirt2.Y + dirt1.Z * dirt2.Z;
                        if (Math.Abs(angle) <= 0.001)
                        {
                            double length1 = TRF(l1.Length);
                            double length2 = TRF(l2.Length);
                            string s = length1 > length2 ? BAndH(l1.Length - delLengthSize, l2.Length) : BAndH(l2.Length - delLengthSize, l1.Length);
                            Area = length1 > length2 ? EAToCA((l1.Length - delLengthSize) * l2.Length) : EAToCA((l2.Length - delLengthSize) * l1.Length);
                            return s;
                        }

                    }
                }
                Area = EAToCA(bface.Area);
                return "-";
            }
        }
        /// <summary>
        /// 返回楼板底边的长和宽
        /// </summary>
        /// <param name="bface">楼板底面的面</param>
        /// <param name="delLengthSize">扣减长边的值(单位为英寸)</param>
        /// <returns></returns>
        public string SlabSize(Face bface, double delLengthSize)
        {
            EdgeArrayArray eArrAray = bface.EdgeLoops;
            if (eArrAray.Size > 1)
            {
                return "-";
            }
            else
            {
                EdgeArray eAray = eArrAray.get_Item(0);
                if (eAray.Size != 4) return "-";
                List<Line> linList = new List<Line>();
                foreach (Edge ed in eAray)
                {
                    Line l = (ed.AsCurve()) as Line;
                    linList.Add(l);
                }
                foreach (Line l1 in linList)
                {
                    XYZ dirt1 = l1.Direction;
                    foreach (Line l2 in linList)
                    {
                        XYZ dirt2 = l2.Direction;
                        if (dirt1.IsAlmostEqualTo(dirt2)) continue;
                        double angle = dirt1.X * dirt2.X + dirt1.Y * dirt2.Y + dirt1.Z * dirt2.Z;
                        if (Math.Abs(angle) <= 0.001)
                        {
                            double length1 = TRF(l1.Length);
                            double length2 = TRF(l2.Length);
                            string s = length1 > length2 ? BAndH(l1.Length - delLengthSize, l2.Length) : BAndH(l2.Length - delLengthSize, l1.Length);
                            return s;
                        }

                    }
                }
                return "-";
            }
        }

        /// <summary>
        /// 返回梁和梁相交的面的个数
        /// </summary>
        /// <param name="fi">剪切梁</param>
        /// <param name="ficut">被剪切梁</param>
        /// <returns>相交的面的个数</returns>
        public int BtoBFaceNum(FamilyInstance fi, FamilyInstance ficut)
        {
            int i = 0;
            Line filine = (fi.Location as LocationCurve).Curve as Line;
            Line ficutline = (ficut.Location as LocationCurve).Curve as Line;
            XYZ p1 = filine.GetEndPoint(0);
            XYZ p2 = filine.GetEndPoint(1);
            XYZ pos1 = ficutline.GetEndPoint(0);
            XYZ pos2 = ficutline.GetEndPoint(1);
            IntersectionResultArray instRstAray = new IntersectionResultArray();
            SetComparisonResult stRst = filine.Intersect(ficutline, out instRstAray);
            if (instRstAray.Size != 0 && stRst == SetComparisonResult.Overlap)
            {
                XYZ instPos = instRstAray.get_Item(0).XYZPoint;
                double b = fi.Symbol.LookupParameter("b").AsDouble();
                //被剪切梁端点1离相交位置的距离
                double length1 = pos1.DistanceTo(instPos);
                //梁侧离端点2的位置
                double length2 = pos2.DistanceTo(instPos);
                if (length1 > b / 2) i++;
                if (length2 > b / 2) i++;
            }
            if (i == 0) i++;
            return i;
        }
        /// <summary>
        /// 两条平行线之间的距离
        /// </summary>
        /// <param name="l1">平行线1</param>
        /// <param name="l2">平行线2</param>
        /// <returns>两线间距离</returns>
        public double LineToLineDistance(Line l1, Line l2)
        {
            //利用海伦公式求取三角形的高度
            XYZ top = l1.GetEndPoint(0);
            XYZ bottom1 = l2.GetEndPoint(0);
            XYZ bottom2 = l2.GetEndPoint(0);
            //斜边
            double a = top.DistanceTo(bottom1);
            double b = top.DistanceTo(bottom2);
            //底边
            double c = bottom1.DistanceTo(bottom2);
            //判断三条边是否为0
            if (a == 0 || b == 0 || c == 0) return 0;
            //半周长
            double p = (a + b + c) / 2;
            //三角形面积
            double area = Math.Pow(p * (p - a) * (p - b) * (p - c), 0.5);
            //高度即两条线之间的距离
            double h = area / c;
            return h;
        }
        /// <summary>
        /// 得到剪切梁与综合梁的板扣减面积
        /// </summary>
        /// <param name="bfi">梁实例</param>
        /// <param name="doc"> 项目文旦</param>
        /// <returns></returns>
        public List<TpAmount> CutByFloorAmount(FamilyInstance bfi, Document doc)
        {
            List<TpAmount> tpamount_List = new List<TpAmount>();
            List<Floor> bfiFl_List = JoinGeometryUtils.GetJoinedElements(doc, bfi).Where(m => doc.GetElement(m) is Floor).ToList()
                .ConvertAll(m => doc.GetElement(m) as Floor);
            Level lev = bfi.Host as Level;
            string bfiMt = bfi.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsValueString();
            foreach (Floor bfl in bfiFl_List)
            {
                FloorType fltp = bfl.FloorType;
                string flMt = fltp.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsValueString();
                if (flMt == bfiMt) continue;
                double h = fltp.LookupParameter("默认的厚度").AsDouble();
                double flCutBeamArea = GetFlCutBeamArea(bfi, bfl);
                double sfArea = EAToCA(flCutBeamArea);
                double length = flCutBeamArea / h;
                string bh = BAndH(length, h);
                TpAmount tpa = new TpAmount(bfi, lev, TpCount.TypeName.梁模板, "-", bh, -sfArea, 1);
                tpamount_List.Add(tpa);
            }
            return tpamount_List;
        }

        /// <summary>
        /// 转换面积（中转英）
        /// </summary>
        /// <param name="Earea"></param>
        public double EAToCA(double Earea)
        {
            return Earea * Math.Pow(304.8, 2) / Math.Pow(10, 6);
        }
        ///
        public double EVToCV(double EVolume)
        {
            return EVolume * Math.Pow(304.8, 3) / Math.Pow(10, 9);
        }
        /// <summary>
        /// 得到宽X长的毫米规范命名
        /// </summary>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public string BAndH(double length, double width)
        {
            return TRF(width * 304.8, 0).ToString() + "mm x " + TRF((length * 304.8), 0).ToString() + "mm";
        }
        /// <summary>
        /// 获取与楼板与梁相交的侧面的面积
        /// </summary>
        /// <param name="bfi">梁实例</param>
        /// <param name="bfl">楼板实例</param>
        /// <returns></returns>
        public double GetFlCutBeamArea(FamilyInstance beam, Floor fl)
        {
            //获取梁的中心线
            Line beamLine = (beam.Location as LocationCurve).Curve as Line;
            //中心线方向向量
            XYZ dirt = beamLine.Direction;
            //取一个端点
            XYZ p = beamLine.GetEndPoint(0);
            //取梁的宽度
            double b = beam.Symbol.LookupParameter("b").AsDouble();
            Options opt = new Options();
            opt.ComputeReferences = true;
            opt.DetailLevel = fl.Document.ActiveView.DetailLevel;
            opt.IncludeNonVisibleObjects = false;
            //取板的几何元素
            GeometryElement geoElem = fl.get_Geometry(opt);
            //遍历迭代器
            IEnumerator flEnum = geoElem.GetEnumerator();
            Face sface = null;
            while (flEnum.MoveNext() == true)
            {
                GeometryObject geoObj = flEnum.Current as GeometryObject;
                //取楼板的solid
                if (geoObj is Solid)
                {
                    //取楼板的solid
                    Solid sd = geoObj as Solid;
                    //取楼板的面
                    foreach (Face face in sd.Faces)
                    {
                        //取楼板面的法向向量
                        XYZ normal = face.ComputeNormal(new UV(0, 0));
                        //竖直向上的面，顶面和底面
                        XYZ topNormal = new XYZ(0, 0, 1);
                        //排除掉顶面和底面
                        if (normal.IsAlmostEqualTo(-topNormal) || normal.IsAlmostEqualTo(topNormal)) continue;
                        //排除掉与梁方向垂直的面
                        if (normal.IsAlmostEqualTo(dirt) || normal.IsAlmostEqualTo(-dirt)) continue;
                        //取该面上的一个点
                        List<CurveLoop> lin_List = face.GetEdgesAsCurveLoops().ToList();
                        Line l = lin_List[0].ToArray()[0] as Line;
                        XYZ pos = l.GetEndPoint(0);
                        //楼板侧面上的角点与梁上的端点形成的向量用以求距离
                        XYZ AtoP = pos - p;
                        double d = Math.Abs((AtoP.X * normal.X + AtoP.Y * normal.Y + AtoP.Z * AtoP.Z) / (normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z));
                        if (d <= b / 2 + 0.01)
                        {
                            sface = face;
                            break;
                        }
                    }
                }
            }
            return sface.Area;
        }
        /// <summary>
        /// 返回被剪切的梁集合
        /// </summary>
        /// <param name="beamList">梁集合</param>
        /// /// <param name="doc">项目文档</param>
        public List<FamilyInstance> JoinBeamToBeam(List<FamilyInstance> beamList, Document doc)
        {
            using (Transaction trans = new Transaction(doc, "梁连接"))
            {
                //存储被剪切(包含即剪切其它梁又被其他梁剪切)的梁实例
                List<FamilyInstance> joinBeamList = new List<FamilyInstance>();
                //存储已经与其它构件处理过连接关系的构件
                List<FamilyInstance> hasJoinBeamList = new List<FamilyInstance>();
                //遍历梁的集合
                foreach (FamilyInstance fi1 in beamList)
                {
                    trans.Start();
                    foreach (FamilyInstance fi2 in beamList)
                    {
                        //排除已经处理过连接关系的构件
                        if (hasJoinBeamList.Count != 0 && hasJoinBeamList.Where(m => m.Id.IntegerValue == fi2.Id.IntegerValue).Count() != 0) continue;
                        //排除构件本身
                        if (fi2.Id.IntegerValue == fi1.Id.IntegerValue) continue;
                        //比较构件高低
                        double h1 = fi1.Symbol.LookupParameter("h").AsDouble() * 304.8;
                        double h2 = fi2.Symbol.LookupParameter("h").AsDouble() * 304.8;
                        FamilyInstance f1 = h1 >= h2 ? fi1 : fi2;
                        FamilyInstance f2 = h1 >= h2 ? fi2 : fi1;
                        if (JoinGeometryUtils.AreElementsJoined(doc, f1, f2))
                        {
                            bool b = JoinGeometryUtils.IsCuttingElementInJoin(doc, f1, f2);
                            //梁高大剪切梁高小的
                            if (b == false)
                            {
                                SubTransaction sbTrans = new SubTransaction(doc);
                                sbTrans.Start();
                                JoinGeometryUtils.SwitchJoinOrder(doc, f1, f2);
                                sbTrans.Commit();
                            }
                            if (joinBeamList.Count == 0 || joinBeamList.Where(m => m.Id.IntegerValue == f2.Id.IntegerValue).Count() == 0)
                            {
                                joinBeamList.Add(f2);
                            }
                        }
                    }
                    hasJoinBeamList.Add(fi1);
                    trans.Commit();
                }
                return joinBeamList;
            }
        }
        /// <summary>
        /// 精确小数位数
        /// </summary>
        /// <param name="lastDouble">转换的数</param>
        /// <param name="i">保留的小数位数</param>
        /// <returns></returns>
        public double TRF(double lastDouble, int i)
        {
            double tf = lastDouble * Math.Pow(10, i);
            int integer = Convert.ToInt32(tf);
            tf = integer / Math.Pow(10, i) * 1.0;
            return tf;
        }
        /// <summary>
        /// 无参数默认小数后六位
        /// </summary>
        /// <param name="lastDouble">转换的数</param>
        /// <returns></returns>
        public double TRF(double lastDouble)
        {
            double tf = Convert.ToDouble(lastDouble.ToString("0.######"));
            return tf;
        }
        /// <summary>
        /// 返回Excel表对应的字段
        /// </summary>
        /// <param name="strlist"></param>
        /// <returns></returns>
        public List<string> ProTransform(List<string> strlist, out List<int> columnSizeList)
        {
            List<string> proNameList = new List<string>();
            columnSizeList = new List<int>();
            foreach (string pi in strlist)
            {
                //表格列字段
                string str = null;
                //表格列宽度
                int i = 0;
                switch (pi)
                {
                    case "ComponentName":
                        str = "构件名称";
                        i = 20;
                        break;
                    case "ComponentType":
                        i = 20;
                        str = "构件类型";
                        break;
                    case "LevelName":
                        str = "构件标高";
                        i = 15;
                        break;
                    case "ComponentLength":
                        str = "构件长度(mm)";
                        i = 15;
                        break;
                    case "ElemId":
                        str = "构件ID";
                        i = 15;
                        break;
                    case "TemplateSize":
                        str = "模板尺寸";
                        i = 25;
                        break;
                    case "TemplateDelSize":
                        str = "扣减尺寸";
                        i = 25;
                        break;
                    case "ConcretVolumes":
                        str = "混凝土工程量(m3)";
                        i = 25;
                        break;
                    case "TemplateAmount":
                        str = "模板面积(m2)";
                        i = 25;
                        break;
                    case "TemplateNum":
                        str = "模板个数";
                        i = 15;
                        break;
                    case "MaterialName":
                        str = "混凝土等级";
                        i = 25;
                        break;
                    default:
                        break;
                }
                proNameList.Add(str);
                columnSizeList.Add(i);
            }
            return proNameList;

        }
        /// <summary>
        /// 获取楼板的底面
        /// </summary>
        /// <param name="fl">楼板</param>
        /// <returns></returns>
        public Face SlabBottomFace(Floor fl)
        {
            Face bface = null;
            Options opt = new Options();
            opt.ComputeReferences = true;
            opt.DetailLevel = ViewDetailLevel.Fine;
            opt.IncludeNonVisibleObjects = false;
            GeometryElement gelem = fl.get_Geometry(opt);
            IEnumerator elemEnum = gelem.GetEnumerator();
            while (elemEnum.MoveNext())
            {
                GeometryObject gobj = elemEnum.Current as GeometryObject;
                if (gobj is Solid)
                {
                    Solid sd = gobj as Solid;
                    foreach (Face face in sd.Faces)
                    {
                        if (face.ComputeNormal(new UV(0, 0)).IsAlmostEqualTo(new XYZ(0, 0, -1)))
                        {
                            bface = face;
                            break;
                        }
                    }
                }
            }

            return bface;
        }
        /// <summary>
        /// 获取实体的全部Solid
        /// </summary>
        /// <param name="e"></param>
        /// <param name="vdl"></param>
        /// <returns></returns>
        public List<Solid> AllSolid_Of_Element(Element e)
        {
            List<Solid> solid_list = new List<Solid>();
            try
            {
                Options options = new Options();
                options.IncludeNonVisibleObjects = false;
                options.DetailLevel = ViewDetailLevel.Fine;
                options.ComputeReferences = true;

                GeometryElement geoElement = e.get_Geometry(options);
                IEnumerator enumerator = geoElement.GetEnumerator();
                {
                    while (enumerator.MoveNext())
                    {
                        GeometryObject geoObj = enumerator.Current as GeometryObject;
                        if (geoObj is GeometryInstance)
                        {
                            GeometryInstance geoinstance = geoObj as GeometryInstance;
                            GeometryElement geoObjtmp = geoinstance.GetInstanceGeometry();
                            //GeometryElement geoObjtmp = geoinstance.GetSymbolGeometry();
                            IEnumerator enumeratorobj = geoObjtmp.GetEnumerator();
                            {
                                while (enumeratorobj.MoveNext())
                                {
                                    GeometryObject obj2 = enumeratorobj.Current as GeometryObject;
                                    if (obj2 is Solid)
                                    {
                                        Solid sd = obj2 as Solid;
                                        if (sd.Volume > 0)
                                            solid_list.Add(obj2 as Solid);
                                    }
                                }
                            }
                        }
                        else if (geoObj is Solid)
                        {
                            Solid sd = geoObj as Solid;
                            if (sd.Volume > 0)
                                solid_list.Add(geoObj as Solid);
                        }
                    }
                }
            }
            catch
            { }
            return solid_list;
        }

        public ElementId SurfaceLayerGernerate(Document doc, Face face, DirectShapeType drt, ElementId HostELemID)
        {
            DirectShape dsElem = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Parts), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            XYZ faceNormal = face.ComputeNormal(new UV(0, 0));
            if (face is CylindricalFace) return null;//如果是圆柱面则排除
            IList<CurveLoop> faceLoopList = face.GetEdgesAsCurveLoops();
            Solid sd = GeometryCreationUtilities.CreateExtrusionGeometry(faceLoopList, faceNormal, 5 / 304.8);

            //catch { SWF.MessageBox.Show(HostELemID.IntegerValue + ""); }
            //对于生成的solid进行预处理
            sd = SolidHandle(doc, HostELemID, sd);
            dsElem.SetShape(new List<GeometryObject>() { sd });
            dsElem.SetTypeId(drt.Id);
            dsElem.LookupParameter("HostElemID").Set(HostELemID.IntegerValue);
            dsElem.LookupParameter("模板面积").Set(sd.Volume / (5 / 304.8));
            Parameter dsParamter = dsElem.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            try
            {
                if (faceNormal.IsAlmostEqualTo(-XYZ.BasisZ))
                {
                    if (doc.GetElement(HostELemID) is Floor)
                        dsParamter.Set("楼板底模板");
                    else if (doc.GetElement(HostELemID).Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming))
                        dsParamter.Set("梁底模板");

                }
                else
                {
                    if (doc.GetElement(HostELemID) is Floor)
                        dsParamter.Set("楼板侧模板");
                    else if (doc.GetElement(HostELemID).Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming))
                        dsParamter.Set("梁侧模板");
                    else if (doc.GetElement(HostELemID).Category.Id == new ElementId(BuiltInCategory.OST_StructuralColumns))
                        dsParamter.Set("柱侧模板");
                }
            }
            catch (Exception e)
            {
                SWF.MessageBox.Show(e.ToString());
            }
            dsElem.LookupParameter("X").Set(faceNormal.X);
            dsElem.LookupParameter("Y").Set(faceNormal.Y);
            dsElem.LookupParameter("Z").Set(faceNormal.Z);
            doc.ActiveView.PartsVisibility = PartsVisibility.ShowPartsOnly;

            return dsElem.Id;
        }

        public Solid SolidHandle(Document doc, ElementId hostELemID, Solid sd)
        {
            Element hostElem = doc.GetElement(hostELemID);
            List<Element> elembeCutList = JoinGeometryUtils.GetJoinedElements(doc, hostElem).Where(m =>
            {
                if (JoinGeometryUtils.AreElementsJoined(doc, doc.GetElement(m), hostElem))
                {
                    if (JoinGeometryUtils.IsCuttingElementInJoin(doc, hostElem, doc.GetElement(m)))
                    {
                        return true;
                    }
                }
                return false;
            }).ToList().ConvertAll(m => doc.GetElement(m));
            //对于与梁进行碰撞的Solid的处理
            if (hostElem.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming))
            {
                try
                {
                    elembeCutList = elembeCutList.Where(m => m.Category
                       .Id == new ElementId(BuiltInCategory.OST_StructuralFraming))
                       .ToList();
                }
                catch { SWF.MessageBox.Show(hostELemID + ""); }

            }
            else if (hostElem is Floor)
            {
                elembeCutList = elembeCutList.Where(m => m.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming) 
                || m.Category.Id == new ElementId(BuiltInCategory.OST_StructuralColumns)||m.Category.Id==new ElementId(BuiltInCategory.OST_Walls))
                        .ToList();
            }
            Solid lastSolid = sd;
            foreach (Element e in elembeCutList)
            {
                Solid sdcut = AllSolid_Of_Element(e)[0];
                try
                {
                    lastSolid = BooleanOperationsUtils.ExecuteBooleanOperation(lastSolid, sdcut, BooleanOperationsType.Difference);
                }//可能由于几何体太过复杂导致Bool失败
                catch { }

            }
            return lastSolid;
        }

        /// <summary>
        /// 创建共享参数
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        public bool ShareParameterGenerate(Document doc, Autodesk.Revit.ApplicationServices.Application app)
        {
            //设置共享参数
            string TxtFileName = app.RecordingJournalFilename;
            Definition IdDf;
            Definition areaDf;
            Definition XDf;
            Definition YDf;
            Definition ZDf;
            string sNametmp = TxtFileName.Substring(0, TxtFileName.LastIndexOf("\\")) + "\\Teplate共享参数.txt";
            if (!File.Exists(sNametmp))
            {
                File.WriteAllText(sNametmp, "", Encoding.Default);
                app.SharedParametersFilename = sNametmp;
            }
            try
            {
                DefinitionFile dsFile = app.OpenSharedParameterFile();
                DefinitionGroup dsGroup = dsFile.Groups.ToList().Where(m => m.Name == "模板信息").First();
                IdDf = dsGroup.Definitions.get_Item("HostElemID");
                areaDf = dsGroup.Definitions.get_Item("模板面积");
                XDf = dsGroup.Definitions.get_Item("X");
                YDf = dsGroup.Definitions.get_Item("Y");
                ZDf = dsGroup.Definitions.get_Item("Z");
            }
            catch
            {
                // 判断 路径是否有效，如果为空，读者可以创建一txt文件
                //将路径赋值给app.SharedParametersFilename
                DefinitionFile dfile = app.OpenSharedParameterFile();
                // 创建一个共享参数分组
                DefinitionGroup dg = dfile.Groups.Create("模板信息");

                // 参数创建的选项，包括参数名字，参数类型，用户是不是可以修改。。
                ExternalDefinitionCreationOptions elemID = new ExternalDefinitionCreationOptions("HostElemID", ParameterType.Integer);
                elemID.UserModifiable = false;
                ExternalDefinitionCreationOptions TemplateArea = new ExternalDefinitionCreationOptions("模板面积", ParameterType.Area);
                TemplateArea.UserModifiable = false;
                ExternalDefinitionCreationOptions X = new ExternalDefinitionCreationOptions("X", ParameterType.Number);
                X.UserModifiable = false;
                ExternalDefinitionCreationOptions Y = new ExternalDefinitionCreationOptions("Y", ParameterType.Number);
                Y.UserModifiable = false;
                ExternalDefinitionCreationOptions Z = new ExternalDefinitionCreationOptions("Z", ParameterType.Number);
                Z.UserModifiable = false;

                // 创建参数
                IdDf = dg.Definitions.Create(elemID);
                areaDf = dg.Definitions.Create(TemplateArea);
                XDf = dg.Definitions.Create(X);
                YDf = dg.Definitions.Create(Y);
                ZDf = dg.Definitions.Create(Z);

            }
            if (IdDf == null || areaDf == null || YDf == null || XDf == null || ZDf == null)
            {
                return false;
            }
            // 创建一个Category集合

            CategorySet cateSet = app.Create.NewCategorySet();

            // 获取墙的category
            Category TemplateCate = Category.GetCategory(doc, BuiltInCategory.OST_Parts);

            // 在Category集合中加入 模板的category
            bool flag = cateSet.Insert(TemplateCate);

            // 给 这个Category集合中的Category 创建一个实例绑定
            InstanceBinding TemBd = app.Create.NewInstanceBinding(cateSet);
            //ElementBinding TemBd = app.Create.NewTypeBinding(cateSet);

            // 获取当前Document的BindingMap
            BindingMap bmap = doc.ParameterBindings;

            //创建共享参数和Category之间的Binding
            bmap.Insert(IdDf, TemBd);
            bmap.Insert(areaDf, TemBd);
            bmap.Insert(XDf, TemBd);
            bmap.Insert(YDf, TemBd);
            bmap.Insert(ZDf, TemBd);
            //设置视图，打开组成部分
            doc.ActiveView.PartsVisibility = PartsVisibility.ShowPartsOnly;
            Material partMat = null;
            try
            {
                partMat = FilterElementList(doc, typeof(Material)).Where(m => m.Name == "模板材质").First() as Material;
            }
            catch
            {
                partMat = doc.GetElement(Material.Create(doc, "模板材质")) as Material;
                partMat.Color = new Color(255, 0, 0);
            }
            doc.Settings.Categories.get_Item(BuiltInCategory.OST_Parts).Material = partMat;
            return true;
        }

    }
}



