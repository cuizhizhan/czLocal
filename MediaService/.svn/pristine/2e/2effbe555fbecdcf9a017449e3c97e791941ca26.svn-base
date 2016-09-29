using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 描述
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DescriptAttribute : System.Attribute
    {
        /// <summary>
        /// 描述
        /// </summary>
        public string Descript { get; private set; }
        /// <summary>
        /// 枚举值
        /// </summary>
        /// <param name="descript"></param>
        public DescriptAttribute(string descript)
        {
            Descript = descript;
        }
    }
}
