using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraduationProject
{
    /// <summary>
    /// 构件尺寸计算信息
    /// </summary>
  public  class LinkInfo
    {
        //构件长度
        public double Length { get; set; }
        //梁的宽度
        public double BeamWidth { get; set; }
        
        /// <summary>
        /// 记录梁构件的信息
        /// </summary>
        /// <param name="length"> 梁的有效长度</param>
        /// <param name="beamWidth"> 梁的有效宽度</param>
        public LinkInfo(double length,double beamWidth)
        {
            this.Length = length;
            this.BeamWidth = beamWidth;
        }
    }
}
