using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;

namespace GraduationProject
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MainClass : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Win win = new Win();
            if (win.ShowDialog()==true)
            {
                List<string> strList = win.chbStrList;
                foreach (string  str in strList)
                {
                    switch (str)
                    {
                        case "梁模板":
                            break;
                        case "柱模板":
                            break;
                        case "板模板": Fl flamount = new Fl(commandData);
                          List<TpAmount> list=  flamount.FLTpAmount;
                            break;
                        case "剪力墙模板":
                            break;
                        case "楼梯模板":
                            break;
                        default:
                            break;
                    }
                }
            }
            return Result.Succeeded;

        }
    }
}
