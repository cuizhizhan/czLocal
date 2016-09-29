using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// app呼叫设备
    /// </summary>
    public class AppCallDeviceModel
    {
        public int tid { get; set; }
        public List<UidIsOnLineModel> status { get; set; } 
    }

    public class UidIsOnLineModel
    {
        public string uid { get; set; }
        public int isonline { get; set; }
    }
}
