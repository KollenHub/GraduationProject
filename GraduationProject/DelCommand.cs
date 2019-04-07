using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
namespace TemplateCount
{
    [Transaction(TransactionMode.Manual)]
    class DelCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            List<ElementId> hostElemIdList = uidoc.Selection.GetElementIds().ToList();
            List<Element> dsList = BasisCode.FilterElementList<DirectShape>(doc);
            List<ElementId> delIds = new List<ElementId>();
            foreach (Element ds in dsList)
            {
                int id = ds.LookupParameter("HostElemID").AsInteger();
                if (hostElemIdList.Contains(new ElementId(id))) delIds.Add(ds.Id);
            }
            using (Transaction trans = new Transaction(doc, "删除模板"))
            {
                trans.Start();
                doc.Delete(delIds);
                trans.Commit();
            }
            return Result.Succeeded;
        }
    }
}
