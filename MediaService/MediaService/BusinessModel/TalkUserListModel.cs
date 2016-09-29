using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 频道成员Model
    /// </summary>
    public class TalkUserListModel
    {
        public string tid { get; set; }
        public string uid { get; set; }
        public string remark { get; set; }
        public string glsn { get; set; }
        public string gender { get; set; }
    }
}
