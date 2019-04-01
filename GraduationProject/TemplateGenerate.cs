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

namespace TemplateCount
{
    [Transaction(TransactionMode.Manual)]
    public class TemplateGenerate : IExternalCommand
    {
        BasisCode bc = new BasisCode();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Application app = commandData.Application.Application;
            Document doc = uidoc.Document;
            DefinitionFile ds = app.OpenSharedParameterFile();
            DirectShapeType dst = null;
            using (TransactionGroup transGroup = new TransactionGroup(doc, "模板创建"))
            {
                if (transGroup.Start() == TransactionStatus.Started)
                {
                    Transaction transShare = new Transaction(doc, "创建共享参数");
                    transShare.Start();
                    bool b = bc.ShareParameterGenerate(doc, app);
                    transShare.Commit();
                    if (b == false) transGroup.RollBack();
                    if (bc.FilterElementList(doc, typeof(DirectShapeType)).Where(m => m.Name == "模板").Count() == 0)
                    {
                        Transaction transCreat = new Transaction(doc, "创建类型");
                        transCreat.Start();
                        dst = DirectShapeType.Create(doc, "模板", new ElementId(BuiltInCategory.OST_Parts));
                        transCreat.Commit();
                    }
                    else dst = bc.FilterElementList(doc, typeof(DirectShapeType)).Where(m => m.Name == "模板").First() as DirectShapeType;
                    List<Floor> floorList = bc.FilterElementList(doc, typeof(Floor)).ConvertAll(m => m as Floor);
                    List<Element> beamList = bc.FilterElementList(doc, typeof(FamilyInstance), BuiltInCategory.OST_StructuralFraming);
                    List<Element> colList = bc.FilterElementList(doc, typeof(FamilyInstance), BuiltInCategory.OST_StructuralColumns);

                    string failureElemIds = null;
                    Transaction trans = new Transaction(doc, "创建模板");
                    trans.Start();
                    foreach (Floor fl in floorList)
                    {
                        List<Solid> ElemSolidList = bc.AllSolid_Of_Element(fl);
                        foreach (Solid sd in ElemSolidList)
                        {
                            foreach (Face face in sd.Faces)
                            {

                                if (face.ComputeNormal(new UV(0, 0)).IsAlmostEqualTo(-XYZ.BasisZ))
                                {
                                    //try
                                    //{
                                        bc.SurfaceLayerGernerate(doc, face, dst, fl.Id);
                                    //}
                                    //catch { failureElemIds += fl.Id.IntegerValue + "\r\n"; }
                                }
                            }
                        }
                    }
                    foreach (FamilyInstance bInstance in beamList)
                    {
                        IList<Solid> beamSolidList = bc.AllSolid_Of_Element(bInstance);
                        Curve locationCurve = (bInstance.Location as LocationCurve).Curve;
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
                                    //try
                                    //{
                                    bc.SurfaceLayerGernerate(doc, bface, dst, bInstance.Id);
                                    //}
                                    //catch { failureElemIds += bInstance.Id.IntegerValue + "\r\n"; }
                                }
                            }
                        }
                    }
                    foreach (FamilyInstance colInstance in colList)
                    {
                        IList<Solid> colSolidList = bc.AllSolid_Of_Element(colInstance);
                        foreach (Solid sd in colSolidList)
                        {
                            foreach (Face cface in sd.Faces)
                            {
                                XYZ faceNormal = cface.ComputeNormal(new UV(0, 0));
                                if (faceNormal.IsAlmostEqualTo(XYZ.BasisZ) || faceNormal.IsAlmostEqualTo(-XYZ.BasisZ))
                                    continue;
                                //try
                                //{
                                    bc.SurfaceLayerGernerate(doc, cface, dst, colInstance.Id);
                                //}
                                //catch { failureElemIds += colInstance.Id.IntegerValue + "\r\n"; }
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


