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
    public class Avoidance : IExternalCommand
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
                SplitfloorByBeam(levList, flList, doc);
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
                List<XYZ> colPos = colList.ConvertAll(m => (m.Location as LocationPoint).Point);
                List<Line> beamCurve = beamList.ConvertAll(m => (m.Location as LocationCurve).Curve as Line);
                Level lev = doc.GetElement(beamList[0].Host.Id) as Level;

                List<Line> newBeamLineList = new List<Line>();
                //存储要创建的梁实例
                List<FamilySymbol> bSymList = new List<FamilySymbol>();
                foreach (Line l in beamCurve)
                {
                    XYZ p1 = l.GetEndPoint(0);
                    XYZ p2 = l.GetEndPoint(1);
                    XYZ dirt = (p1 - p2) / p1.DistanceTo(p2);
                    //当柱端点与梁端点相距不超过某个值是默认让其相交
                    foreach (XYZ p in colPos)
                    {
                        XYZ pi = new XYZ(p.X, p.Y, lev.Elevation);
                        FamilyInstance col = colList[colPos.IndexOf(p)];
                        double b = col.Symbol.LookupParameter("b").AsDouble();
                        double h = col.Symbol.LookupParameter("h").AsDouble();
                        double length = b > h ? b : h;
                        //如果相邻不超过一百毫米默认连接
                        if (p1.DistanceTo(pi) < length / 2 + 100 / 304.8)
                        {
                            if (dirt.IsAlmostEqualTo(new XYZ(0, 1, 0)) || dirt.IsAlmostEqualTo(new XYZ(0, -1, 0)))
                            {
                                p1 = new XYZ(p1.X, pi.Y, lev.Elevation);
                            }
                            else
                            {
                                p1 = new XYZ(pi.X, p1.Y, lev.Elevation);
                            }
                        }
                        else if (p2.DistanceTo(pi) < length / 2 + 100 / 304.8)
                        {
                            if (dirt.IsAlmostEqualTo(new XYZ(0, 1, 0)) || dirt.IsAlmostEqualTo(new XYZ(0, -1, 0)))
                            {
                                p2 = new XYZ(p2.X, pi.Y, lev.Elevation);
                            }
                            else
                            {
                                p2 = new XYZ(pi.X, p2.Y, lev.Elevation);
                            }
                        }

                    }
                    Line li = Line.CreateBound(p1, p2);
                    newBeamLineList.Add(li);
                    bSymList.Add(beamList[beamCurve.IndexOf(l)].Symbol);
                }
                //创建新的梁实例
                using (Transaction trans = new Transaction(doc))
                {
                    trans.Start("join");
                    List<ElementId> delID = beamList.ConvertAll(m => m.Id);
                    doc.Delete(delID);
                    beamList = new List<FamilyInstance>();
                    for (int i = 0; i < newBeamLineList.Count; i++)
                    {
                        if (bSymList[i].IsActive == false) bSymList[i].Activate();
                        FamilyInstance fi = doc.Create.NewFamilyInstance(newBeamLineList[i], bSymList[i], lev, StructuralType.Beam);
                        beamList.Add(fi);
                    }


                    //梁柱剪切关系
                    foreach (FamilyInstance col in colList)
                    {
                        foreach (FamilyInstance beam in beamList)
                        {
                            if (JoinGeometryUtils.AreElementsJoined(doc, beam, col) == true)
                            {
                                JoinGeometryUtils.SwitchJoinOrder(doc, beam, col);
                            }
                        }
                    }
                    trans.Commit();
                }
            }
        }
        /// <summary>
        /// 梁剪切板
        /// </summary>
        /// <param name="levList">标高集合</param>
        /// <param name="floorList">楼板集合</param>
        /// <param name="doc"> 项目文档</param>
        private void SplitfloorByBeam(List<Level> levList, List<Floor> floorList, Document doc)
        {

            foreach (Floor fl in floorList)
            {

                List<FamilyInstance> colElemList = JoinGeometryUtils.GetJoinedElements(doc, fl).Where(m => doc.GetElement(m).
                GetType() == typeof(FamilyInstance) && doc.GetElement(m).Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns).ToList().ConvertAll(m => doc.GetElement(m) as FamilyInstance);
                List<FamilyInstance> beamElemList = JoinGeometryUtils.GetJoinedElements(doc, fl).Where(m => doc.GetElement(m).
GetType() == typeof(FamilyInstance) && doc.GetElement(m).Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming).ToList().ConvertAll(m => doc.GetElement(m) as FamilyInstance);

                using (Transaction tran = new Transaction(doc))
                {
                    tran.Start("Join");
                    foreach (FamilyInstance beam in beamElemList)
                    {
                        if (JoinGeometryUtils.AreElementsJoined(doc, beam, fl))
                        {
                            if (JoinGeometryUtils.IsCuttingElementInJoin(doc, fl, beam))
                            {
                                JoinGeometryUtils.SwitchJoinOrder(doc, beam, fl);
                            }
                        }

                    }
                    foreach (FamilyInstance col in colElemList)
                    {
                        if (JoinGeometryUtils.AreElementsJoined(doc, fl, col))
                        {
                            if (JoinGeometryUtils.IsCuttingElementInJoin(doc, fl, col))
                            {
                                JoinGeometryUtils.SwitchJoinOrder(doc, fl, col);
                            }
                        }

                    }

                    tran.Commit();
                }
                //取得最小的封闭环数列
                List<Reference> faceReferences = HostObjectUtils.GetTopFaces(fl).ToList();
                //获取楼板顶面
                GeometryObject topFaceGeo = fl.GetGeometryObjectFromReference(faceReferences[0]);
                Face topFace1 = topFaceGeo as Face;
                //获取封闭环
                var loopList = topFace1.GetEdgesAsCurveLoops();//最小封闭环
                //获取标高
                Level level = doc.GetElement(fl.LevelId) as Level;
                FloorType floorType = fl.FloorType;
                //根据封闭的曲线数组进行新的楼板创建
                using (Transaction tran1 = new Transaction(doc))
                {
                    tran1.Start("split");
                    doc.Delete(fl.Id);//删除旧楼板
                    foreach (CurveLoop curveloop in loopList)
                    {
                        Curve[] curveList = curveloop.ToArray();//曲线链分离成曲线数组
                        CurveArray curveArray = new CurveArray();
                        foreach (Curve cu in curveList)//数组变组合
                        {
                            curveArray.Append(cu);
                        }
                        Floor newfloor = doc.Create.NewFloor(curveArray, floorType, level, true);
                    }
                    tran1.Commit();
                }

            }
        }
    }
}
