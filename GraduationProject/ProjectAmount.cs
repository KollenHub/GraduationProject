using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using bc = TemplateCount.BasisCode;

namespace TemplateCount
{

    /// <summary>
    /// 存储execel表存储的字段
    /// </summary>
    public class ProjectAmount
    {
        //通用属性
        /// <summary>
        /// 构件名称（实例名称）
        /// </summary>
        public string ComponentName { get; set; }
        /// <summary>
        /// 构件类别，形如200x300（适用于板梁柱）
        /// </summary>
        public string ComponentType { get; set; }
        /// <summary>
        /// 构件ID（提供查询构件）
        /// </summary>
        public int ElemId { get; set; }
        /// 标高名称（楼层）
        /// </summary>
        public string LevelName { get; set; }
        //特殊属性
        /// <summary>
        /// 构件长度
        /// </summary>
        public string ComponentLength { get; set; }
        /// <summary>
        /// 构件宽度
        /// </summary>
        public string ComponetWidth { get; set; }
        /// <summary>
        /// 构件材质
        /// </summary>
        public string MaterialName { get; set; }
        /// <summary>
        /// 构件高度
        /// </summary>
        public string ComponentHighth { get; set; }
        /// <summary>
        /// 模板ID
        /// </summary>
        public int TpId { get; set; }
        /// <summary>
        /// 模板部位
        /// </summary>
        public string TpType { get; set; }
        /// <summary>
        /// 模板未扣减的尺寸（长x宽）
        /// </summary>
        public string TemplateSize { get; set; }
        /// <summary>
        /// <summary>
        /// 混凝土工程量（立方米）
        /// </summary>
        public double ConcretVolumes { get; set; }
        /// <summary>
        /// 总的模板数量（平方米）
        /// </summary>
        public double TemplateAmount { get; set; }
       
        public string TypeName = null;
        /// <summary>
        /// 多种多样的构件构造函数
        /// </summary>
        public ProjectAmount()
        {
        }
        /// <summary>
        /// 模板工程量样板
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="tpName"></param>
        public ProjectAmount(Element ds,bc.TypeName tpName,bool isFirst=false)
        {
            Document doc = ds.Document;
            List<Element> hostElemList = new List<Element>();
            Element hostElem = null;
            string ty = Enum.GetName(typeof(bc.TypeName), tpName);
            //作为判断的中介
            string p = null;
            if (ty.Contains("模板"))
                p = ty.Split(new char[] { '模' })[0];
            else
                p = ty.Split(new char[] { '砼' })[0];
            if (ty.Contains("模板"))
            { hostElem = doc.GetElement(new ElementId(ds.LookupParameter("HostElemID").AsInteger())); }
            else hostElem = ds;
            if (isFirst) {
                this.ComponentName = hostElem.Name;
                this.ElemId = hostElem.Id.IntegerValue;
                switch (p)
                {
                    case "板":
                        Floor fl = hostElem as Floor;
                        this.ComponentType = fl.FloorType.Name;
                        this.LevelName = doc.GetElement(fl.LevelId).Name;
                        break;
                    case "梁":
                        FamilyInstance bfi = hostElem as FamilyInstance;
                        this.ComponentType = bfi.Symbol.FamilyName;
                        this.LevelName = doc.GetElement(bfi.Host.Id).Name;
                        this.ComponentLength = bfi.LookupParameter("长度").AsValueString();
                        break;
                    case "柱":
                        FamilyInstance cfi = hostElem as FamilyInstance;
                        this.ComponentType = cfi.Symbol.FamilyName;
                        this.LevelName = doc.GetElement(cfi.LevelId).Name;
                        this.ComponentHighth = cfi.LookupParameter("长度").AsValueString();
                        break;
                    case "墙":
                        Wall wal = hostElem as Wall;
                        this.ComponentType = wal.WallType.Name;
                        this.LevelName = doc.GetElement(wal.LevelId).Name;
                        this.ComponentLength = wal.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString();
                        this.ComponentHighth = wal.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsValueString();
                        break;
                    case "楼梯":
                        Stairs stair = hostElem as Stairs;
                        this.ComponentName = doc.GetElement(stair.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsElementId()).Name;
                        this.ComponentType = stair.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString();
                        this.LevelName = doc.GetElement(stair.get_Parameter(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM).AsElementId()).Name;
                        break;
                    case "基础":
                        this.ComponetWidth = hostElem.get_Parameter(BuiltInParameter.CONTINUOUS_FOOTING_WIDTH).AsValueString();
                        this.ComponentLength = hostElem.get_Parameter(BuiltInParameter.CONTINUOUS_FOOTING_LENGTH).AsValueString();
                        break;
                    default:
                        break;
                }
            }
            this.TypeName = Enum.GetName(typeof(bc.TypeName), tpName);
            if (ty.Contains("模板"))
            {
                this.TpId = ds.Id.IntegerValue;
                this.TpType = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
                this.TemplateSize = ds.LookupParameter("模板尺寸").AsString();
                double tpAmount = ds.LookupParameter("模板面积").AsDouble() * 0.3048 * 0.3048;
                //MessageBox.Show(tpAmount.ToString());
                this.TemplateAmount = bc.TRF(tpAmount, 3);
            }
            else
            {
                bc.AllSolid_Of_Element(hostElem).ConvertAll(m => this.ConcretVolumes += bc.EVToCV(m.Volume));
                this.ConcretVolumes = bc.TRF(ConcretVolumes);
            }
            
        }
    }
}
