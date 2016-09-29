using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 导航到非绑定设备
    /// </summary>
    public class NavigationModel
    {
        /// <summary>
        /// 设备序列号
        /// </summary>
        public string sn { get; set; }

        /// <summary>
        /// 在线状态,1:在线
        /// </summary>
        public int isonline { get; set; }
    }
}
