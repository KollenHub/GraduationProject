using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
namespace TemplateCount
{
    public class TpCount
    {
        public BasisCode bc = new BasisCode();
        //定义不同构件属性的枚举
        public enum TypeName
        {
            板模板 = 0,
            梁模板 = 1,
            柱模板 = 2,
            梁砼工程量 = 3,
            板砼工程量 = 4,
            柱砼工程量 = 5,

        }
        /// <summary>
        /// 梁模板的计算方法
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="joinBeamList">被剪切梁</param>
        /// <param name="Beam_List">全部梁的集合</param>
        /// <param name="tpName">类型名称</param>
        /// <param name="tpAmountList">模板量</param>
        public TpCount(Document doc, List<FamilyInstance> joinBeamList, List<FamilyInstance> Beam_List, TypeName tpName, out List<TpAmount> tpAmountList)
        {
            tpAmountList = new List<TpAmount>();
            //剪切梁
            List<FamilyInstance> cutBeam_List = Beam_List.Except(joinBeamList).ToList();
            //被剪切梁
            List<FamilyInstance> becutBeam_List = new List<FamilyInstance>();
            //综合梁
            List<FamilyInstance> allBeam_List = new List<FamilyInstance>();
            foreach (FamilyInstance fi in joinBeamList)
            {
                //获得与该梁连接的全部梁
                List<Element> fiJoinElemList = JoinGeometryUtils.GetJoinedElements(doc, fi).ToList().ConvertAll(m => doc.GetElement(m));
                List<FamilyInstance> fiJoinList = fiJoinElemList.Where(m => m.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming).ToList()
                    .ConvertAll(m => m as FamilyInstance);
                //区分出纯被剪切梁和综合梁
                foreach (FamilyInstance fj in fiJoinList)
                {
                    //判断该梁是否剪切所相连的梁，如果是则是综合梁
                    if (JoinGeometryUtils.IsCuttingElementInJoin(doc, fi, fj) == true)
                    {
                        allBeam_List.Add(fi);
                        break;
                    }
                }
                //不是综合梁则是纯被剪切梁
                if (allBeam_List.Count == 0 || allBeam_List.Where(m => m.Id.IntegerValue == fi.Id.IntegerValue).Count() == 0)
                {
                    becutBeam_List.Add(fi);
                }
            }
            Beam_List.Clear();//按照剪切梁和被剪切梁、综合梁的顺序排列
            Beam_List.AddRange(cutBeam_List);
            Beam_List.AddRange(becutBeam_List);
            Beam_List.AddRange(allBeam_List);
            //计算梁的模板面积
            foreach (FamilyInstance bfi in Beam_List)
            {
                //标高
                Level lev = bfi.Host as Level;
                //获取该梁中配套的侧面和底面集合的集合
                List<List<Face>> faceList_List = bc.AllFaceOfBeam(doc, bfi);
                //把每一套侧面和底面的面积以及其尺寸拿出来
                foreach (List<Face> face_List in faceList_List)
                {
                    Face bface = face_List[0];
                    Face sface = face_List[1];
                    double h = bfi.Symbol.LookupParameter("h").AsDouble();
                    double b = bfi.Symbol.LookupParameter("b").AsDouble();
                    double barea = bc.EAToCA(bface.Area);
                    double sarea = bc.EAToCA(sface.Area);
                    string bftp = bc.BAndH((bface.Area / b), b);
                    string sftp = bc.BAndH((sface.Area / h), h);
                    //分别创建对应的模板面积字段对象
                    TpAmount btpa = new TpAmount(bfi, lev, tpName, bftp, "-", barea, 1);
                    TpAmount stpa = new TpAmount(bfi, lev, tpName, sftp, "-", sarea, 2);
                    tpAmountList.Add(btpa);
                    tpAmountList.Add(stpa);
                }
                //楼板是否同种材质判断（同种扣除面积，非同种不扣除）
                List<TpAmount> flDeltpa = bc.CutByFloorAmount(bfi, doc);
                tpAmountList.AddRange(flDeltpa);
                //排除纯被剪切梁
                if (becutBeam_List.IndexOf(bfi) != -1) continue;
                //获取与该梁相梁接的被剪切的梁
                List<Element> bfiCutElemList = JoinGeometryUtils.GetJoinedElements(doc, bfi).ToList().ConvertAll(m => doc.GetElement(m));
                List<FamilyInstance> bfiCutBeamList = bfiCutElemList.Where(m => m.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming).ToList()
                    .ConvertAll(m => m as FamilyInstance);
                //存储剪切面有多少个类型
                List<FamilySymbol> bfiCutSymList = new List<FamilySymbol>();
                foreach (FamilyInstance bficut in bfiCutBeamList)
                {
                    FamilySymbol fs = bficut.Symbol;
                    if (bfiCutSymList.Count == 0 || bfiCutSymList.Where(m => m.Id.IntegerValue == fs.Id.IntegerValue).Count() == 0)
                        bfiCutSymList.Add(fs);
                }
                //存储每个类型对应的模板面积对象
                foreach (FamilySymbol fs in bfiCutSymList)
                {
                    List<FamilyInstance> fi_List = bfiCutBeamList.Where(m => m.Symbol.Id.IntegerValue == fs.Id.IntegerValue).ToList();
                    int num = 0;
                    foreach (FamilyInstance fi in fi_List)
                    {
                        //获取该梁与相连的那个梁有多少个面
                        num += bc.BtoBFaceNum(bfi, fi);
                    }
                    double fsb = (fs.LookupParameter("b").AsDouble() * 304.8);
                    double fsh = (fs.LookupParameter("h").AsDouble() * 304.8);
                    string delTpSize = fsb.ToString() + "mm x " + fsh.ToString() + "mm";
                    double delTpa = (fsb * fsh / 1000000);
                    TpAmount sDelTpa = new TpAmount(bfi, lev, tpName, "-", delTpSize, -delTpa, num);
                }
            }

        }

        /// <summary>
        /// 楼板模板的计算方法
        /// </summary>
        /// <param name="flList">楼板集合</param>
        /// <param name="tpName">类型名称</param>
        /// <param name="tpAmountList">模板量</param>
        public TpCount(List<Floor> flList, TypeName tpName, Level lev, out List<TpAmount> tpAmountList)
        {
            tpAmountList = new List<TpAmount>();
            //板的底面积
            foreach (Floor fl in flList)
            {
                Face bface = bc.SlabBottomFace(fl);
                string bfaceSize = bc.SlabSize(bface, 0);
                double tpamount = bc.EAToCA(bface.Area);
                TpAmount flTpa = new TpAmount(fl, lev, tpName, bfaceSize, tpamount, 1);
                tpAmountList.Add(flTpa);
            }
        }
        /// <summary>
        /// 柱子的模板计算
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="colList">柱子的实例集合</param>
        /// <param name="lev">柱子所在标高</param>
        /// <param name="tpAmountList">模板量</param>
        public TpCount(Document doc, List<FamilyInstance> colList, Level lev, out List<TpAmount> tpAmountList)
        {
            tpAmountList = new List<TpAmount>();

            foreach (FamilyInstance cfi in colList)
            {
                double d = 0;
                List<Floor> joinFloor = JoinGeometryUtils.GetJoinedElements(doc, cfi).Where(m => doc.GetElement(m) is Floor).ToList().ConvertAll(m => doc.GetElement(m) as Floor);
                if (joinFloor.Count != 0)
                {
                    string cmstr = cfi.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsValueString();
                    foreach (Floor fl in joinFloor)
                    {
                        string fmstr = fl.FloorType.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsValueString();
                        if (cmstr == fmstr)
                        {
                            if (d == 0) d = fl.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble();
                            else d = Math.Min(d, fl.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble());
                        }
                    }
                }
                List<Face> csFaces = bc.ComponentSideFace(cfi);
                foreach (Face sf in csFaces)
                {

                    double length = cfi.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM).AsDouble();
                    double width = sf.Area / length;
                    string sfacesize = bc.BAndH(length - d, width);
                    double tpamount = bc.EAToCA((length - d) * width);
                    TpAmount cTpa = new TpAmount(cfi, lev, TypeName.柱模板, sfacesize, null, tpamount, 1);
                    tpAmountList.Add(cTpa);
                }
            }
        }

        /// <summary>
        /// 梁柱混凝土工程量计算
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="InstanceList">全部梁的集合</param>
        /// <param name="joinBeamList">被剪切梁的集合</param>
        /// <param name="tpAmountList">导出的工程量集合</param>
        public TpCount(Document doc, List<FamilyInstance> InstanceList, TypeName tpname, out List<TpAmount> tpAmountList)
        {
            tpAmountList = new List<TpAmount>();
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
                List<Solid> beamSolidList = bc.AllSolid_Of_Element(inst, ViewDetailLevel.Fine);
                beamSolidList.ConvertAll(m => instanceVolume += bc.EVToCV(m.Volume));
                TpAmount beamConcret = new TpAmount(inst, lev, tpname, instanceVolume);
                tpAmountList.Add(beamConcret);
            }
        }


    }
}
