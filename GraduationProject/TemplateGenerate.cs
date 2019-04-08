using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.ApplicationServices;
using System.IO;
using bc = TemplateCount.BasisCode;
using Autodesk.Revit.DB.Architecture;
using SWF= System.Windows.Forms;

namespace TemplateCount
{
    [Transaction(TransactionMode.Manual)]
    public class TemplateGenerate : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Application app = commandData.Application.Application;
            Document doc = uidoc.Document;
            DefinitionFile ds = app.OpenSharedParameterFile();
            DirectShapeType dst = null;
            TpWin Tpwin = new TpWin();
            if (Tpwin.ShowDialog() == false) return Result.Succeeded;
            List<string> checkOpt = Tpwin.chbStrList;
            using (TransactionGroup transGroup = new TransactionGroup(doc, "模板创建"))
            {
                if (transGroup.Start() == TransactionStatus.Started)
                {
                    Transaction transShare = new Transaction(doc, "创建共享参数");
                    transShare.Start();
                    bool b = bc.ShareParameterGenerate(doc, app);
                    transShare.Commit();
                    if (b == false) transGroup.RollBack();
                    if (bc.FilterElementList<DirectShapeType>(doc).Where(m => m.Name == "模板").Count() == 0)
                    {
                        Transaction transCreat = new Transaction(doc, "创建类型");
                        transCreat.Start();
                        dst = DirectShapeType.Create(doc, "模板", new ElementId(BuiltInCategory.OST_Parts));
                        transCreat.Commit();
                    }
                    else dst = bc.FilterElementList<DirectShapeType>(doc).Where(m => m.Name == "模板").First() as DirectShapeType;
                    List<Element> floorList = bc.FilterElementList<Floor>(doc);
                    List<Element> beamList = bc.FilterElementList<FamilyInstance>(doc,BuiltInCategory.OST_StructuralFraming);
                    List<Element> colList = bc.FilterElementList<FamilyInstance>(doc,BuiltInCategory.OST_StructuralColumns);
                    List<Element> walList = bc.FilterElementList<Wall>(doc, BuiltInCategory.OST_Walls).Where(m => m.Name.Contains("DW")).Count()>0? 
                        bc.FilterElementList<Wall>(doc, BuiltInCategory.OST_Walls).Where(m => m.Name.Contains("DW")).ToList():new List<Element>();
                    List<Element> stairsList = bc.FilterElementList<Stairs>(doc, BuiltInCategory.OST_Stairs).ToList();
                    List<Element> basisList = bc.FilterElementList<FamilyInstance>(doc, BuiltInCategory.OST_StructuralFoundation);
                    //SWF.MessageBox.Show(stairsList.Count.ToString());  
                    string failureElemIds = null;
                    Transaction trans = new Transaction(doc, "创建模板");
                    trans.Start();
                    //楼板
                    if (checkOpt.Contains("板模板"))
                    {
                     string flTxt=bc.HostElemTpGenerate(floorList, dst, new List<XYZ>() { XYZ.BasisZ });
                        if (flTxt != null) failureElemIds += "楼板" + "\r\n" + flTxt;
                    }
                    //梁模板
                    if (checkOpt.Contains("梁模板"))
                    {
                     string beamTxt=bc.HostElemTpGenerate(beamList, dst, new List<XYZ>() { XYZ.BasisZ });
                        if (beamTxt != null) failureElemIds += "梁" + "\r\n" + beamTxt;
                    }
                    //柱模板
                    if (checkOpt.Contains("柱模板"))
                    {
                     string colTxt=bc.HostElemTpGenerate(colList, dst, new List<XYZ>() { XYZ.BasisZ, -XYZ.BasisZ });
                        if (colTxt != null) failureElemIds += "柱" + "\r\n" + colTxt;
                    }
                    //墙模板
                        foreach (Wall wal in walList)
                    {
                        List<ElementId> failureWallIds = new List<ElementId>();
                        if (!checkOpt.Contains("墙模板")) break;
                        IList<Solid> walSolidList = bc.AllSolid_Of_Element(wal);
                        foreach (Solid sd in walSolidList)
                        {
                            List<Face> faceList = sd.Faces.Cast<Face>().ToList();
                            //获取高度最小值
                            double walBottom =double.MaxValue;
                            double walHeight = double.MinValue;
                            faceList.ConvertAll(m =>
                            {
                                BoundingBoxUV bdxyz = m.GetBoundingBox();
                                XYZ min = m.Evaluate(bdxyz.Min);
                                XYZ max = m.Evaluate(bdxyz.Max);
                                double minV = min.Z < max.Z ? min.Z : max.Z;
                                double maxV = min.Z > max.Z ? min.Z : max.Z;
                                walBottom = walBottom < minV ? walBottom : minV;
                                walHeight = walHeight > maxV ? walHeight : maxV;
                                return true;
                            });
                            
                            foreach (Face wface in sd.Faces)
                            {
                                XYZ wfaceNormal = wface.ComputeNormal(new UV(0, 0));
                                double faceElvation = wface.Evaluate(new UV(0, 0)).Z;
                                if (wfaceNormal.IsAlmostEqualTo(XYZ.BasisZ) && (walHeight - faceElvation) * 304.8 < 10) continue;
                                if (wfaceNormal.IsAlmostEqualTo(-XYZ.BasisZ) && (faceElvation - walBottom) * 304.8 < 10) continue;
                                try
                                {
                                    bc.SurfaceLayerGernerate(doc, wface, dst, wal.Id);
                                }
                                catch
                                {
                                    if (failureWallIds.Count == 0 || failureWallIds.Where(m => m.IntegerValue == wal.Id.IntegerValue).Count() == 0)
                                        failureWallIds.Add(wal.Id);
                                }
                            }
                            if (failureWallIds.Count!=0)
                            {
                                failureElemIds += "墙" + "\r\n";
                                failureWallIds.ConvertAll(m => m.IntegerValue.ToString() + "\r\n");
                            }
                        }
                    }
                    //楼梯模板
                    if (checkOpt.Contains("楼梯模板"))
                    {
                     string stairsTxt=bc.HostElemTpGenerate(stairsList, dst, new List<XYZ>() { XYZ.BasisZ });
                        if (stairsTxt != null) failureElemIds += "楼梯" + "\r\n"+stairsTxt;
                    }
                    //基础模板
                    if (checkOpt.Contains("基础模板"))
                    {
                     string basisTxt=bc.HostElemTpGenerate(basisList, dst, new List<XYZ>() { XYZ.BasisZ, -XYZ.BasisZ });
                        if (basisTxt != null) failureElemIds += "基础" + "\r\n"+basisTxt;
                    }
                    trans.Commit();
                    if (failureElemIds != null)
                    {
                        SWF.SaveFileDialog sfd = new SWF.SaveFileDialog();
                        sfd.Filter = "文本文档|*.txt"; //删选、设定文件显示类型
                        sfd.DefaultExt = ".txt";
                        sfd.FileName = "模板生成错误构件ID";
                        sfd.AddExtension = true;
                        sfd.ShowDialog();
                        string path = sfd.FileName;
                        File.WriteAllText(@"C: \Users\BXS\Desktop\FailureText.txt", failureElemIds);
                    }
                        
                }
                transGroup.Assimilate();
            }
            return Result.Succeeded;

        }

    }
}


