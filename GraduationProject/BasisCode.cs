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
using bc = TemplateCount.BasisCode;
using Autodesk.Revit.DB.Architecture;

namespace TemplateCount
{
    public static class BasisCode
    {
        //定义不同构件属性的枚举
        public enum TypeName
        {
            板模板 = 0,
            梁模板 = 1,
            柱模板 = 2,
            墙模板 = 3,
            楼梯模板 = 4,
            基础模板 = 5,
            梁砼工程量 = 6,
            板砼工程量 = 7,
            柱砼工程量 = 8,
            墙砼工程量 = 9,
            楼梯砼工程量 = 10,
            基础砼工程量 = 11,

        }

        /// <summary>
        /// 转换面积（中转英）
        /// </summary>
        /// <param name="Earea"></param>
        public static double EAToCA(double Earea)
        {
            return Earea * Math.Pow(304.8, 2) / Math.Pow(10, 6);
        }
        ///
        public static double EVToCV(double EVolume)
        {
            return EVolume * Math.Pow(304.8, 3) / Math.Pow(10, 9);
        }
        /// <summary>
        /// 主体模板生成
        /// </summary>
        /// <param name="hostElemList">构件List</param>
        /// <param name="normalList">排除面的法向</param>
        /// <returns>生成有问题的构件的id</returns>
        public static string HostElemTpGenerate(List<Element> hostElemList,DirectShapeType dst,List<XYZ> normalList)
        {
            Document doc = hostElemList[0].Document;
            List<Solid> solidList = new List<Solid>();
            List<ElementId> failureIdList = new List<ElementId>();
            string failureTxt = null;
            foreach (Element hostElem in hostElemList)
            {
                if (hostElem is Stairs)
                    solidList.Add(bc.AllUnionSolid(hostElem));
                else if (hostElem.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFoundation))
                    solidList.Add(bc.AllSolid_Of_Element(hostElem).OrderBy(m => m.Volume).Last());
                else if (hostElem.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming))
                {
                    try
                    {
                        LocationCurve Curve = (hostElem as FamilyInstance).Location as LocationCurve;
                        solidList = bc.AllSolid_Of_Element(hostElem);
                    }
                    catch 
                    {
                        failureTxt += hostElem.Id.IntegerValue.ToString() + "\r\n";
                        continue;
                    }
                }
                else
                    solidList = bc.AllSolid_Of_Element(hostElem);
                foreach (Solid sd in solidList)
                {
                    foreach (Face face in sd.Faces)
                    {
                        XYZ faceNormal = face.ComputeNormal(new UV(0, 0));
                        if (normalList.Where(m => m.IsAlmostEqualTo(faceNormal)).Count() > 0) continue;
                        try
                        {
                            bc.SurfaceLayerGernerate(doc, face, dst, hostElem.Id);
                        }
                        catch
                        {
                            if (failureIdList.Count == 0 || failureIdList.Where(m => m.IntegerValue == hostElem.Id.IntegerValue).Count() == 0)
                                failureIdList.Add(hostElem.Id);
                        }
                    }
                }
            }
            if (failureIdList.Count!=0)
            {
                failureIdList.ConvertAll(m => failureTxt += m.IntegerValue + "\r\n");
            }
            return failureTxt;


        }
        /// <summary>
        /// 返回被剪切的梁集合
        /// </summary>
        /// <param name="beamList">梁集合</param>
        /// /// <param name="doc">项目文档</param>
        public static List<FamilyInstance> JoinBeamToBeam(List<FamilyInstance> beamList, Document doc)
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
        public static double TRF(double lastDouble, int i)
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
        public static double TRF(double lastDouble)
        {
            double tf = Convert.ToDouble(lastDouble.ToString("0.######"));
            return tf;
        }
        /// <summary>
        /// 返回Excel表对应的字段
        /// </summary>
        /// <param name="strlist"></param>
        /// <returns></returns>
        public static List<string> ProTransform(List<string> strlist, out List<int> columnSizeList)
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
                    case "ComponentHighth":
                        str = "构件高度";
                        i = 15;
                        break;
                    case "ComponentWidth":
                        str = "构件宽度(mm)";
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
                    case "TpId":
                        str = "模板ID";
                        i = 15;
                        break;
                    case "TpType":
                        str = "模板部位";
                        i = 20;
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
        /// 返回项目中按照高程从小到大排列后的标高集合
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <returns></returns>
        public static List<Level> GetLevList(Document doc)
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
        public static List<Element> FilterElementList<T>(Document doc, BuiltInCategory bIC, Level lev = null) where T : class
        {
            List<Element> elemList = new FilteredElementCollector(doc).OfClass(typeof(T)).OfCategory(bIC).ToList();
            if (lev != null)
            {
                try
                {
                    if (elemList[0].Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming)
                        elemList = elemList.Where(m => (m as FamilyInstance).Host != null && (m as FamilyInstance).Host.Id.IntegerValue == lev.Id.IntegerValue).ToList();
                    else if (elemList[0] is Stairs)
                        elemList = elemList.Where(m => m.get_Parameter(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM).AsElementId() == lev.Id).ToList();
                    else
                        elemList = elemList.Where(m => m.LevelId.IntegerValue == lev.Id.IntegerValue).ToList();

                }
                catch
                {
                    return new List<Element>();
                }
            }
            try
            {
                if (elemList[0].Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFoundation)
                    elemList = elemList.Where(m => !(m as FamilyInstance).Symbol.Name.Contains("预制") && !(m as FamilyInstance).Symbol.Name.Contains("预制")).ToList();
                else if (elemList[0] is FamilyInstance)
                    elemList = elemList.Where(m => !(m as FamilyInstance).Symbol.FamilyName.Contains("钢") && !(m as FamilyInstance).Symbol.FamilyName.Contains("PC")).ToList();
                else if (elemList[0] is Floor)
                    elemList = elemList.Where(m => !(m as Floor).FloorType.Name.Contains("PC")).ToList();
            }
            catch
            {
                return new List<Element>();
            }
            return elemList;

        }

        /// <summary>
        /// 获取对应的模板工程量对象
        /// </summary>
        /// <param name="TpListList">模板对象</param>
        /// <param name="tpName">那种类型的模板</param>
        /// <returns></returns>
        public static List<List<ProjectAmount>> ProjectAmoutList(List<List<Element>> TpListList, TypeName tpName)
        {
            List<List<ProjectAmount>> projectAmountList_List = new List<List<ProjectAmount>>();
            string ty = Enum.GetName(typeof(bc.TypeName), tpName);
            if (ty.Contains("模板"))
            {
                foreach (List<Element> dsList in TpListList)//此处dsList的第一个为模板宿主对象
                {
                    List<ProjectAmount> pAList = new List<ProjectAmount>();
                    foreach (Element ds in dsList)
                    {
                        if (dsList.IndexOf(ds) == 0) continue;
                        int index = dsList.IndexOf(ds);
                        ProjectAmount TpA = new ProjectAmount(ds, tpName, dsList.IndexOf(ds) < 2 ? true : false);
                        pAList.Add(TpA);
                    }
                    if (pAList.Count != 0)
                        projectAmountList_List.Add(pAList);
                }
                return projectAmountList_List;
            }
            foreach (List<Element> elemList in TpListList)//此处dsList的第一个为模板宿主对象
            {
                List<ProjectAmount> pAList = new List<ProjectAmount>();
                foreach (Element elem in elemList)
                    pAList.Add(new ProjectAmount(elem, tpName, true));
                if (pAList.Count != 0)
                    projectAmountList_List.Add(pAList);
            }
            return projectAmountList_List;
        }

        /// <summary>
        /// 返回过滤器元素集合
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="ty">查找的元素类型</param>
        /// <returns></returns>
        public static List<Element> FilterElementList<T>(Document doc) where T : class
        {

            List<Element> elemList = new FilteredElementCollector(doc).OfClass(typeof(T)).ToList();
            return elemList;
        }

        /// <summary>
        /// 返回过滤器元素集合
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="bIC">查找的元素对应的BuiltInCategory</param>
        /// <returns></returns>
        public static List<Element> FilterElementList(Document doc, BuiltInCategory bIC)
        {
            List<Element> elemList = new FilteredElementCollector(doc).OfCategory(bIC).ToList();
            return elemList;
        }
        /// <summary>
        /// 获取实体的全部Solid
        /// </summary>
        /// <param name="e"></param>
        /// <param name="vdl"></param>
        /// <returns></returns>
        public static List<Solid> AllSolid_Of_Element(Element e)
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
        /// <summary>
        /// 获取楼梯合并solid
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static Solid AllUnionSolid(Element e)
        {
            Solid sd = null;
            try
            {
                Options options = new Options();
                options.IncludeNonVisibleObjects = false;
                options.DetailLevel = ViewDetailLevel.Fine;
                options.ComputeReferences = true;

                GeometryElement geoElement1 = e.get_Geometry(options);//点进去
                IEnumerator enumerator = geoElement1.GetEnumerator();
                {
                    while (enumerator.MoveNext())
                    {
                        GeometryObject gobj = enumerator.Current as GeometryObject;
                        if (gobj is GeometryInstance)
                        {
                            GeometryInstance geoInstance = gobj as GeometryInstance;
                            GeometryElement gElem = geoInstance.SymbolGeometry;
                            IEnumerator enumerator1 = gElem.GetEnumerator();
                            while (enumerator1.MoveNext())
                            {
                                GeometryObject gobj1 = enumerator1.Current as GeometryObject;
                                if (gobj1 is Solid)
                                {
                                    Solid solid = gobj1 as Solid;
                                    if (sd == null) sd = solid;
                                    else sd = BooleanOperationsUtils.ExecuteBooleanOperation(sd, solid, BooleanOperationsType.Union);
                                }
                            }
                        }
                    }
                }
            }
            catch
            { }
            return sd;
        }
        /// <summary>
        /// 模板生成
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="face"></param>
        /// <param name="drt"></param>
        /// <param name="HostELemID"></param>
        /// <returns></returns>
        public static ElementId SurfaceLayerGernerate(Document doc, Face face, DirectShapeType drt, ElementId HostELemID)
        {
            DirectShape dsElem = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Parts), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            XYZ faceNormal = face.ComputeNormal(new UV(0, 0));
            if (face is CylindricalFace) return null;//如果是圆柱面则排除
            IList<CurveLoop> faceLoopList = face.GetEdgesAsCurveLoops();
            Solid sd = GeometryCreationUtilities.CreateExtrusionGeometry(faceLoopList, faceNormal, 5 / 304.8);
            //模板尺寸判断，矩形标尺寸，非矩形不标
            string tpSize = null;
            double last = sd.Volume;
            //catch { SWF.MessageBox.Show(HostELemID.IntegerValue + ""); }
            //对于生成的solid进行预处理
            sd = SolidHandle(doc, HostELemID, sd);
            double now = sd.Volume;
            if (now == 0) return null;
            if (last == now)
            {
                if (faceLoopList.Count == 1)
                {
                    List<Curve> curveList = faceLoopList[0].ToList();
                    tpSize = bc.RectTangleSize(curveList);
                }
                else tpSize = "非矩形板";
            }
            else tpSize = "非矩形板";
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
                    else if (doc.GetElement(HostELemID) is Wall)
                        dsParamter.Set("墙洞顶模板");
                    else if (doc.GetElement(HostELemID) is Stairs)
                        dsParamter.Set("平台底模板");
                }
                else if (faceNormal.IsAlmostEqualTo(XYZ.BasisZ))
                {
                    //墙
                    if (doc.GetElement(HostELemID) is Wall) dsParamter.Set("墙洞底模板");
                }
                else
                {
                    if (doc.GetElement(HostELemID) is Floor)
                        dsParamter.Set("楼板侧模板");
                    else if (doc.GetElement(HostELemID).Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming))
                        dsParamter.Set("梁侧模板");
                    else if (doc.GetElement(HostELemID).Category.Id == new ElementId(BuiltInCategory.OST_StructuralColumns))
                        dsParamter.Set("柱侧模板");
                    else if (doc.GetElement(HostELemID) is Wall)
                        dsParamter.Set("墙侧模板");
                    else if (doc.GetElement(HostELemID) is Stairs)
                        dsParamter.Set("楼梯侧模板");
                    else if (doc.GetElement(HostELemID).Category.Id == new ElementId(BuiltInCategory.OST_StructuralFoundation))
                        dsParamter.Set("基础侧模板");
                }
            }
            catch (Exception e)
            {
                SWF.MessageBox.Show(e.ToString());
            }
            dsElem.LookupParameter("模板尺寸").Set(tpSize);
            dsElem.LookupParameter("X").Set(faceNormal.X);
            dsElem.LookupParameter("Y").Set(faceNormal.Y);
            dsElem.LookupParameter("Z").Set(faceNormal.Z);
            doc.ActiveView.PartsVisibility = PartsVisibility.ShowPartsOnly;

            return dsElem.Id;
        }
        /// <summary>
        /// 判断是否是矩形
        /// </summary>
        /// <param name="curveList"></param>
        /// <returns></returns>

        ///是否是矩形
        public static string RectTangleSize(List<Curve> curveList)
        {
            if (curveList.Count != 4) return "非矩形板";
            List<XYZ> xyzList = curveList.ConvertAll(m => m.GetEndPoint(0));
            XYZ center = XYZ.Zero;
            xyzList.ConvertAll(m => center += m);
            center = center / 4;
            double d = double.MaxValue;
            foreach (XYZ xyz in xyzList)
            {
                if (d == double.MaxValue)
                    d = bc.TRF(center.DistanceTo(xyz));
                else if (d - bc.TRF(xyz.DistanceTo(center)) > 10 / 304.8)
                    return "非矩形板";
            }
            string size = bc.TRF(xyzList[0].DistanceTo(xyzList[1]) * 304.8, 0).ToString() + "mm x" + bc.TRF(xyzList[1].DistanceTo(xyzList[2]) * 304.8, 0).ToString() + "mm";
            return size;
        }

        /// <summary>
        /// 处理碰撞
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="hostELemID"></param>
        /// <param name="sd"></param>
        /// <returns></returns>
        public static Solid SolidHandle(Document doc, ElementId hostELemID, Solid sd)
        {
            //TODO:这里是取得该物体是否剪切别的物体
            Element hostElem = doc.GetElement(hostELemID);
            //碰撞集合
            List<Element> elembeCutList = JoinGeometryUtils.GetJoinedElements(doc, hostElem).Where(m =>
            {
                if (JoinGeometryUtils.AreElementsJoined(doc, doc.GetElement(m), hostElem))
                {
                    //if (JoinGeometryUtils.IsCuttingElementInJoin(doc, hostElem, doc.GetElement(m)))
                    //{
                    return true;
                    //}
                }
                return false;
            }).ToList().ConvertAll(m => doc.GetElement(m));
            //对于与梁进行碰撞的Solid的处理
            if (hostElem.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming))
            {
                try
                {
                    elembeCutList = elembeCutList.Where(m => m.Category
                        .Id == new ElementId(BuiltInCategory.OST_StructuralFraming) || m.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns).ToList();
                }
                catch { SWF.MessageBox.Show(hostELemID + ""); }

            }
            else if (hostElem is Floor)
            {
                elembeCutList = elembeCutList.Where(m => m.Category.Id == new ElementId(BuiltInCategory.OST_StructuralFraming)
                 || m.Category.Id == new ElementId(BuiltInCategory.OST_StructuralColumns) || m.Category.Id == new ElementId(BuiltInCategory.OST_Walls))
                        .ToList();
            }
            else if (hostElem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns)
            {
                elembeCutList = elembeCutList.Where(m => m is Wall && m.Name.Contains("DW")).ToList();
            }
            else if (hostElem is Wall)
            {
                Curve walCurve = (hostElem.Location as LocationCurve).Curve;
                XYZ startPoint = walCurve.GetEndPoint(0);
                XYZ endPoint = walCurve.GetEndPoint(1);
                startPoint = new XYZ(startPoint.X, startPoint.Y, 0);
                endPoint = new XYZ(endPoint.X, endPoint.Y, 0);
                List<Element> walElemList = bc.FilterElementList<Wall>(doc).Where(m =>
                  {
                      if (hostElem.Id == m.Id) return false;
                      if (!hostElem.Name.Contains("DW")) return false;
                      if (hostElem.LevelId != m.LevelId) return false;
                      Curve mc = (m.Location as LocationCurve).Curve;
                      XYZ sp = mc.GetEndPoint(0);
                      XYZ ep = mc.GetEndPoint(1);
                      sp = new XYZ(sp.X, sp.Y, 0);
                      ep = new XYZ(ep.X, ep.Y, 0);
                      if (sp.IsAlmostEqualTo(startPoint) || sp.IsAlmostEqualTo(endPoint) || ep.IsAlmostEqualTo(endPoint) || ep.IsAlmostEqualTo(startPoint))
                          return true;
                      return false;
                  }).ToList();
                elembeCutList = elembeCutList.Where(m => m.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns || (m is Wall && m.Name.Contains("DW"))).ToList();
                elembeCutList.AddRange(walElemList);
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
        public static bool ShareParameterGenerate(Document doc, Autodesk.Revit.ApplicationServices.Application app)
        {
            //设置共享参数
            string TxtFileName = app.RecordingJournalFilename;
            Definition IdDf;
            Definition areaDf;
            Definition sizeDf;
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
                sizeDf = dsGroup.Definitions.get_Item("模板尺寸");
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
                ExternalDefinitionCreationOptions TemplateSize = new ExternalDefinitionCreationOptions("模板尺寸", ParameterType.Text);
                TemplateSize.UserModifiable = false;
                ExternalDefinitionCreationOptions X = new ExternalDefinitionCreationOptions("X", ParameterType.Number);
                X.UserModifiable = false;
                ExternalDefinitionCreationOptions Y = new ExternalDefinitionCreationOptions("Y", ParameterType.Number);
                Y.UserModifiable = false;
                ExternalDefinitionCreationOptions Z = new ExternalDefinitionCreationOptions("Z", ParameterType.Number);
                Z.UserModifiable = false;

                // 创建参数
                IdDf = dg.Definitions.Create(elemID);
                areaDf = dg.Definitions.Create(TemplateArea);
                sizeDf = dg.Definitions.Create(TemplateSize);
                XDf = dg.Definitions.Create(X);
                YDf = dg.Definitions.Create(Y);
                ZDf = dg.Definitions.Create(Z);

            }
            if (IdDf == null || areaDf == null || YDf == null || XDf == null || ZDf == null || sizeDf == null)
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
            bmap.Insert(sizeDf, TemBd);
            bmap.Insert(XDf, TemBd);
            bmap.Insert(YDf, TemBd);
            bmap.Insert(ZDf, TemBd);
            //设置视图，打开组成部分
            doc.ActiveView.PartsVisibility = PartsVisibility.ShowPartsOnly;
            Material partMat = null;
            try
            {
                partMat = FilterElementList<Material>(doc).Where(m => m.Name == "模板材质").First() as Material;
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



