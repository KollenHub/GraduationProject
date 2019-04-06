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
                    List<Floor> floorList = bc.FilterElementList<Floor>(doc).ConvertAll(m => m as Floor);
                    List<Element> beamList = bc.FilterElementList<FamilyInstance>(doc,BuiltInCategory.OST_StructuralFraming);
                    List<Element> colList = bc.FilterElementList<FamilyInstance>(doc,BuiltInCategory.OST_StructuralColumns);
                    List<Element> walList = bc.FilterElementList<Wall>(doc, BuiltInCategory.OST_Walls).Where(m => m.Name.Contains("DW")).Count()>0? 
                        bc.FilterElementList<Wall>(doc, BuiltInCategory.OST_Walls).Where(m => m.Name.Contains("DW")).ToList():new List<Element>();
                    string failureElemIds = null;
                    Transaction trans = new Transaction(doc, "创建模板");
                    trans.Start();
                    //楼板
                    foreach (Floor fl in floorList)
                    {
                        if(!checkOpt.Contains("板模板"))break;
                        List<Solid> ElemSolidList = bc.AllSolid_Of_Element(fl);
                        foreach (Solid sd in ElemSolidList)
                        {
                            foreach (Face face in sd.Faces)
                            {

                                if (face.ComputeNormal(new UV(0, 0)).IsAlmostEqualTo(-XYZ.BasisZ))
                                {
                                    try
                                    {
                                        bc.SurfaceLayerGernerate(doc, face, dst, fl.Id);
                                    }
                                    catch { failureElemIds += fl.Id.IntegerValue + "\r\n"; }
                                }
                            }
                        }
                    }
                    //梁模板
                    foreach (FamilyInstance bInstance in beamList)
                    {
                        if (!checkOpt.Contains("梁模板")) break;
                       
                    IList<Solid> beamSolidList = bc.AllSolid_Of_Element(bInstance);
                        Curve locationCurve = null;
                        try
                        {
                            locationCurve = (bInstance.Location as LocationCurve).Curve;
                        }
                        catch
                        {
                            failureElemIds +="轴线出错" +bInstance.Id.IntegerValue + "\r\n";
                        }
                        foreach (Solid sd in beamSolidList)
                        {
                            foreach (Face bface in sd.Faces)
                            {
                                XYZ faceNormal = bface.ComputeNormal(new UV(0, 0)).Normalize();
                                bool isGenerate = false;
                                if (locationCurve is Line)
                                {
                                    Line l = locationCurve as Line;
                                    XYZ dirt = l.Direction.Normalize();
                                    if (faceNormal.IsAlmostEqualTo(dirt) || faceNormal.IsAlmostEqualTo(-dirt) || faceNormal.IsAlmostEqualTo(XYZ.BasisZ)) continue;
                                    XYZ sideNormal = dirt.CrossProduct(XYZ.BasisZ).Normalize();
                                    if (faceNormal.IsAlmostEqualTo(-XYZ.BasisZ))
                                        isGenerate = true;
                                    else if (faceNormal.IsAlmostEqualTo(sideNormal) || faceNormal.IsAlmostEqualTo(-sideNormal))
                                        isGenerate = true;
                                    try
                                    {
                                        bc.SurfaceLayerGernerate(doc, bface, dst, bInstance.Id);
                                    }
                                    catch { failureElemIds += bInstance.Id.IntegerValue + "\r\n"; }
                                }
                            }
                        }
                    }
                    //柱模板
                    foreach (FamilyInstance colInstance in colList)
                    {
                        if (!checkOpt.Contains("柱模板")) break;
                        IList<Solid> colSolidList = bc.AllSolid_Of_Element(colInstance);
                        foreach (Solid sd in colSolidList)
                        {
                            foreach (Face cface in sd.Faces)
                            {
                                XYZ faceNormal = cface.ComputeNormal(new UV(0, 0));
                                if (faceNormal.IsAlmostEqualTo(XYZ.BasisZ) || faceNormal.IsAlmostEqualTo(-XYZ.BasisZ))
                                    continue;
                                try
                                {
                                    bc.SurfaceLayerGernerate(doc, cface, dst, colInstance.Id);
                                }
                                catch { failureElemIds += colInstance.Id.IntegerValue + "\r\n"; }
                            }
                        }
                    }
                    //墙模板
                    foreach (Wall wal in walList)
                    {
                       
                        if (!checkOpt.Contains("墙模板")) break;
                        IList<Solid> walSolidList = bc.AllSolid_Of_Element(wal);
                        //3维视图
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
                                catch { failureElemIds += wal.Id.IntegerValue + "\r\n"; }
                            }
                        }
                    }
                    trans.Commit();
                    if (failureElemIds != null)
                        File.WriteAllText(@"C: \Users\BXS\Desktop\FailureText.txt", failureElemIds);
                }
                transGroup.Assimilate();
            }
            return Result.Succeeded;

        }

    }
}


