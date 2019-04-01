using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;

namespace TemplateCount
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ConflictResolution : IExternalCommand
    {
        public BasisCode bc = new BasisCode();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            BasisCode bc = new BasisCode();
            List<Level> levList = bc.GetLevList(doc);
            //所有需要配模板的墙的集合
            List<List<Wall>> wallListList = new List<List<Wall>>();
            //所有需要配模板的梁的集合
            List<List<FamilyInstance>> beamListList = new List<List<FamilyInstance>>();
            //所有需要配模板的柱的集合
            List<List<FamilyInstance>> colListList = new List<List<FamilyInstance>>();
            //所有需要配模板的板的集合          
            List<List<Floor>> flListList = new List<List<Floor>>();

            for (int i = 0; i < levList.Count; i++)
            {
                Level lev = levList[i];
                //需要配模的墙实例
                List<Wall> wallList = bc.FilterElementList(doc, typeof(Wall)).ConvertAll(m => m as Wall).Where(m => m.LevelId.IntegerValue == lev.Id.IntegerValue).ToList();
                //需要配模的梁实例
                List<FamilyInstance> beamList = bc.FilterElementList(doc, typeof(FamilyInstance), BuiltInCategory.OST_StructuralFraming)
                    .ConvertAll(m => m as FamilyInstance).Where(m => m.Host.Id.IntegerValue == lev.Id.IntegerValue).ToList();
                //需要配模的柱实例
                List<FamilyInstance> colList = bc.FilterElementList(doc, typeof(FamilyInstance), BuiltInCategory.OST_StructuralColumns)
                    .ConvertAll(m => m as FamilyInstance).Where(m => m.LevelId.IntegerValue == lev.Id.IntegerValue).ToList();
                //标高以下的柱子
                List<FamilyInstance> nextColList = null;
                if (i > 0)
                {
                    nextColList = bc.FilterElementList(doc, typeof(FamilyInstance), BuiltInCategory.OST_StructuralColumns)
                        .ConvertAll(m => m as FamilyInstance).Where(m => m.LevelId.IntegerValue == levList[i - 1].Id.IntegerValue).ToList();
                }
                //参与比较的柱子
                List<FamilyInstance> compareColList = null;
                if (nextColList == null)
                {
                    compareColList = colList;
                }
                else { compareColList = nextColList; }

                //需要配模的楼板实例
                List<Floor> flList = bc.FilterElementList(doc, typeof(Floor), BuiltInCategory.OST_Floors).ConvertAll(m => m as Floor)
                    .Where(m => m.LevelId.IntegerValue == lev.Id.IntegerValue).ToList();
                JoinBeamAndColumns(ref beamList, compareColList, doc);
                List<FamilyInstance> beCutBeam_List = bc.JoinBeamToBeam(beamList, doc);
                FloorJoinBeamAndColumn(levList, flList, doc);

            }

            return Result.Succeeded;
        }
        /// <summary>
        /// 梁柱连接
        /// </summary>
        /// <param name="beamList">梁的实例</param>
        /// <param name="colList">柱子的实例</param>
        /// <param name="doc">项目文档</param>
        private void JoinBeamAndColumns(ref List<FamilyInstance> beamList, List<FamilyInstance> colList, Document doc)
        {

            if (colList.Count != 0 && beamList.Count != 0)
            {
                List<XYZ> colPosList = colList.ConvertAll(m => (m.Location as LocationPoint).Point);
                Level lev = doc.GetElement(beamList[0].Host.Id) as Level;
                List<FamilyInstance> BeamNotJoinNowColListList = new List<FamilyInstance>();
                //需要改变定位线的梁、
                List<ElementId> beamElemChnageIds = new List<ElementId>();
                List<Line> beamLocationCurveList = new List<Line>();
                foreach (FamilyInstance col in colList)
                {

                    XYZ colPos = (col.Location as LocationPoint).Point;
                    XYZ direction = -XYZ.BasisZ;
                    double b = col.Symbol.LookupParameter("b").AsDouble();
                    double h = col.Symbol.LookupParameter("h").AsDouble();
                    double length = b > h ? b : h;
                    double curveLoopWidth = length / 2 + 200 / 304.8;
                    CurveLoop cuLoop = new CurveLoop();
                    double x = colPos.X;
                    double y = colPos.Y;
                    double z = lev.Elevation;
                    //左上
                    XYZ p1 = new XYZ(x - curveLoopWidth, y + curveLoopWidth, z);
                    //左下
                    XYZ p2 = p1 + new XYZ(0, -2 * curveLoopWidth, 0);
                    //右下
                    XYZ p3 = p2 + new XYZ(2 * curveLoopWidth, 0, 0);
                    //右上
                    XYZ p4 = p3 + new XYZ(0, 2 * curveLoopWidth, 0);
                    Curve c1 = Line.CreateBound(p1, p2);
                    Curve c2 = Line.CreateBound(p2, p3);
                    Curve c3 = Line.CreateBound(p3, p4);
                    Curve c4 = Line.CreateBound(p4, p1);
                    cuLoop.Append(c1);
                    cuLoop.Append(c2);
                    cuLoop.Append(c3);
                    cuLoop.Append(c4);
                    Solid intersectSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { cuLoop }, direction, 200 / 304.8);
                    ElementIntersectsSolidFilter ElemInsectSolidFilter = new ElementIntersectsSolidFilter(intersectSolid);
                    IList<Element> beamNotJoinColList = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
                        .OfCategory(BuiltInCategory.OST_StructuralFraming).WherePasses(ElemInsectSolidFilter).ToElements().
                        Where(m => !JoinGeometryUtils.AreElementsJoined(doc, m, col)).ToList();
                    //Transaction trans = new Transaction(doc, "创建内建模型");
                    //trans.Start();
                    //DirectShapeType drt = DirectShapeType.Create(doc, "实体", new ElementId(BuiltInCategory.OST_Parts));
                    //DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Parts), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
                    //ds.SetShape(new List<GeometryObject>() { intersectSolid });
                    //ds.SetTypeId(drt.Id);
                    //trans.Commit();
                    //TaskDialog.Show("biao", "uu");
                    foreach (Element e in beamNotJoinColList)
                    {
                        //判断是否发生了变化
                        bool pd = true;
                        Line l = (e.Location as LocationCurve).Curve is Line ? (e.Location as LocationCurve).Curve as Line : null;
                        if (l == null) continue;
                        XYZ lp1 = l.GetEndPoint(0);
                        XYZ lp2 = l.GetEndPoint(1);
                        XYZ dirt = (lp1 - lp2) / lp1.DistanceTo(lp2);
                        //当柱端点与梁端点相距不超过某个值是默认让其相交
                        XYZ pi = new XYZ(x, y, z);
                        if (lp1.DistanceTo(pi) < curveLoopWidth)
                        {
                            if (dirt.IsAlmostEqualTo(new XYZ(0, 1, 0)) || dirt.IsAlmostEqualTo(new XYZ(0, -1, 0)))
                            {
                                lp1 = new XYZ(lp1.X, pi.Y, lev.Elevation);
                            }
                            else if (dirt.IsAlmostEqualTo(new XYZ(-1, 0, 0)) || dirt.IsAlmostEqualTo(new XYZ(1, 0, 0)))
                            {
                                lp1 = new XYZ(pi.X, lp1.Y, lev.Elevation);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if (lp2.DistanceTo(pi) < curveLoopWidth)
                        {
                            if (dirt.IsAlmostEqualTo(new XYZ(0, 1, 0)) || dirt.IsAlmostEqualTo(new XYZ(0, -1, 0)))
                            {
                                lp2 = new XYZ(lp2.X, pi.Y, lev.Elevation);
                            }
                            else if (dirt.IsAlmostEqualTo(new XYZ(-1, 0, 0)) || dirt.IsAlmostEqualTo(new XYZ(1, 0, 0)))
                            {
                                lp2 = new XYZ(pi.X, lp2.Y, lev.Elevation);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else pd = false;
                        if (pd == true)
                        {
                            if (beamElemChnageIds.Count() == 0 || beamElemChnageIds.Where(m => m == e.Id).Count() == 0)
                            {
                                beamElemChnageIds.Add(e.Id);
                                beamLocationCurveList.Add(Line.CreateBound(lp1, lp2));
                            }
                            else
                            {
                                int index = beamElemChnageIds.IndexOf(e.Id);
                                Line indexLine = beamLocationCurveList.ElementAt(index);
                                XYZ pone = indexLine.GetEndPoint(0);
                                XYZ ptwo = indexLine.GetEndPoint(1);
                                //变化的线
                                Line linelast1 = Line.CreateBound(pone, lp2);
                                Line linelast2 = Line.CreateBound(lp1, ptwo);
                                beamLocationCurveList[index] = linelast1.Length > linelast2.Length ? linelast1 : linelast2;
                            }
                        }
                    }
                }
                //创建新的梁实例
                using (Transaction transChange = new Transaction(doc))
                {
                    transChange.Start("join");
                    foreach (ElementId beamId in beamElemChnageIds)
                    {
                        int index = beamElemChnageIds.IndexOf(beamId);
                        Element beam = doc.GetElement(beamId);
                        (beam.Location as LocationCurve).Curve = beamLocationCurveList[index];
                        TaskDialog.Show("kaishi", "sdduas");
                    }
                    transChange.Commit();


                }
                using (Transaction trans = new Transaction(doc, "调整顺序"))
                {
                    trans.Start();
                    //梁柱剪切关系
                    foreach (FamilyInstance col in colList)
                    {
                        foreach (FamilyInstance beam in beamList)
                        {
                            if (JoinGeometryUtils.AreElementsJoined(doc, beam, col) == true)
                            {
                                if (JoinGeometryUtils.IsCuttingElementInJoin(doc, beam, col))
                                {
                                    JoinGeometryUtils.SwitchJoinOrder(doc, col, beam);
                                }
                            }
                        }
                    }
                    trans.Commit();
                }
            }
        }
        /// <summary>
        /// 板与梁和柱的连接
        /// </summary>
        /// <param name="levList">标高集合</param>
        /// <param name="floorList">楼板集合</param>
        /// <param name="doc"> 项目文档</param>
        private void FloorJoinBeamAndColumn(List<Level> levList, List<Floor> floorList, Document doc)
        {

            foreach (Floor fl in floorList)
            {
                string flMat = fl.FloorType.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsValueString();
                List<FamilyInstance> colElemList = JoinGeometryUtils.GetJoinedElements(doc, fl).Where(m => doc.GetElement(m).
                GetType() == typeof(FamilyInstance) && doc.GetElement(m).Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns).ToList().ConvertAll(m => doc.GetElement(m) as FamilyInstance);
                List<FamilyInstance> beamElemList = JoinGeometryUtils.GetJoinedElements(doc, fl).Where(m => doc.GetElement(m).GetType() ==
                typeof(FamilyInstance) && doc.GetElement(m).Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming).ToList().ConvertAll(m => doc.GetElement(m) as FamilyInstance);
                using (Transaction tran = new Transaction(doc))
                {
                    tran.Start("Join");
                    foreach (FamilyInstance beam in beamElemList)
                    {
                        string beamMat = beam.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsValueString();
                        //如果材质相同，则板剪梁
                        if (beamMat == flMat)
                        {
                            if (JoinGeometryUtils.IsCuttingElementInJoin(doc, beam, fl))
                            {
                                JoinGeometryUtils.SwitchJoinOrder(doc, beam, fl);
                            }
                        }
                        else//如果材质不同，则梁剪板
                        {
                            if (JoinGeometryUtils.IsCuttingElementInJoin(doc, fl, beam))
                            {
                                JoinGeometryUtils.SwitchJoinOrder(doc, beam, fl);
                            }
                        }
                    }
                    foreach (FamilyInstance col in colElemList)
                    {
                        string colMat = col.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsValueString();
                        //如果材质相同则板剪柱
                        if (colMat == flMat)
                        {
                            if (JoinGeometryUtils.IsCuttingElementInJoin(doc, col, fl))
                            {
                                JoinGeometryUtils.SwitchJoinOrder(doc, fl, col);
                            }
                        }
                        //如果材质不同则柱剪板
                        else
                        {
                            if (JoinGeometryUtils.IsCuttingElementInJoin(doc, fl, col))
                            {
                                JoinGeometryUtils.SwitchJoinOrder(doc, fl, col);
                            }
                        }

                    }
                    tran.Commit();
                }
            }
        }
    }
}
