﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
namespace TemplateCount
{

    /// <summary>
    /// 存储execel表存储的字段
    /// </summary>
    public class TpAmount
    {
        public BasisCode bc = new BasisCode();
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
        /// 构件长度（适用于柱梁板）
        /// </summary>
        public string ComponentLength { get; set; }
        /// <summary>
        /// 模板未扣减的尺寸（长x宽）
        /// </summary>
        public string TemplateSize { get; set; }
        /// <summary>
        /// 模板扣减的尺寸（长x宽）
        /// </summary>
        public string TemplateDelSize { get; set; }
        /// <summary>
        /// 总的模板数量（平方米）
        /// </summary>
        public double TemplateAmount { get; set; }
        /// <summary>
        /// 模板个数
        /// </summary>
        public  int TemplateNum { get; set; }
       
        
        /// <summary>
        /// 构件宽度（适用于板，楼梯）
        /// </summary>
        //public string ComponentWidth { get; set; }


        /// <summary>
        /// 多种多样的构件构造函数
        /// </summary>
        public TpAmount()
        {
        }

        /// <summary>
        /// 梁模板字段
        /// </summary>
        /// <param name="beam">梁实例</param>
        /// <param name="lev">梁所在标高</param>
        /// <param name="templateSize">模板尾扣减时的尺寸（长x宽）</param>
        /// <param name="tempDelSize">模板扣减数量（平方米）</param>
        /// <param name="templateamount">模板最终的数量（平方米）</param>
        /// <param name="num">个数</param>
        public TpAmount(FamilyInstance beam,Level lev,string templateSize,string tempDelSize, double templateamount,int num)
        {
            this.ComponentName = beam.Name.ToString();
            this.ComponentType = beam.Symbol.Family.Name.ToString();
            this.LevelName =lev.Name.ToString();
            this.ComponentLength =beam.LookupParameter("长度").AsValueString();
            this.ElemId = beam.Id.IntegerValue;
            this.TemplateSize = templateSize;
            this.TemplateDelSize = tempDelSize;
            this.TemplateAmount = bc.TRF(templateamount,3);
            this.TemplateNum = num;
        }
        /// <summary>
        /// 板模板的构造函数
        /// </summary>
        /// <param name="fl">楼板</param>
        /// <param name="lev">标高</param>
        /// <param name="templateSize">模板尺寸</param>
        /// <param name="templateamount">模板量</param>
        /// <param name="num">模板数量</param>
        public TpAmount(Floor fl, Level lev, string templateSize, double templateamount, int num)
        {
            this.ComponentName = fl.Name.ToString();
            this.ComponentType = fl.FloorType.FamilyName;
            this.LevelName = lev.Name.ToString();
            this.ElemId = fl.Id.IntegerValue;
            this.TemplateSize = templateSize;
            this.TemplateAmount = bc.TRF(templateamount, 3);
            this.TemplateNum = num;
        }
       

    }
}
