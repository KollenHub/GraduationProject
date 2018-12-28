using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraduationProject
{
    public class TpAmount
    {
        //定义不同构件属性的枚举
        public enum TypeName
        {
            Floor = 0,
            Beam = 1,
            Columns = 2,
            StructralWall = 3,
            Stairs = 4,
            WaterBox = 5

        }
        //通用属性
        /// <summary>
        /// 构件名称
        /// </summary>
        public string ComponentName { get; set; }
        /// 标高名称
        /// </summary>
        public string LevelName { get; set; }
        /// <summary>
        /// 模板数量（平方米）
        /// </summary>
        public double TemplateAmount { get; set; }
        /// <summary>
        /// 模板交叉重叠扣减量（平方米）
        /// </summary>
        public double RebateValue { get; set; }

        //特殊属性
        /// <summary>
        /// 构件长度（适用于柱梁板）
        /// </summary>
        public double ComponentLength { get; set; }
        /// <summary>
        /// 构件类别，形如200x300（适用于板梁柱）
        /// </summary>
        public string ComponentType { get; set; }
        /// <summary>
        /// 构件宽度（适用于板，楼梯）
        /// </summary>
        public double ComponentWidth { get; set; }


        /// <summary>
        /// 多种多样的构件构造函数
        /// </summary>
        public TpAmount()
        {
        }
        
        /// <summary>
        /// 梁/柱的构造函数
        /// </summary>
        /// <param name="componentname">构件名称</param>
        /// <param name="componentType">构件类型，例如板厚以及梁截面等</param>
        /// <param name="levname">楼层标高</param>
        /// <param name="componentlength">构件长度/高度（梁/柱）/</param>        
        /// <param name="rebate"> 扣减模板量，详见模板量扣减规则</param>
        /// <param name="templateamount">最终的模板量</param>
        public TpAmount(string componentname, string componentType,
            string levname, double componentlength, double rebate, double templateamount)
        {
            this.ComponentName = componentname;
            this.ComponentType = ComponentType;
            this.LevelName = levname;
            this.ComponentLength = componentlength;
            this.RebateValue = rebate;
            this.TemplateAmount = templateamount;
        }
        /// <summary>
        /// 板的构造函数
        /// </summary>
        /// <param name="componentname">构件名称</param>
        /// <param name="componentType">构件类型，例如板厚以及梁截面等</param>
        /// <param name="levname">楼层标高</param>
        /// <param name="componentlength">构件长度</param>
        /// <param name="componentwidth">构件宽度</param>
        /// <param name="rebate"> 扣减模板量，详见模板量扣减规则</param>
        /// <param name="templateamount">最终的模板量</param>
        public TpAmount(string componentname, string componentType,
            string levname, double componentlength,  double rebate, double templateamount,double componentwidth) : this(componentname,componentType,levname,componentlength,rebate,templateamount)
        {
            this.ComponentWidth = componentwidth;
        }
       

    }
}
