using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    public class Root
    {
        public sysProductInfoResult sysProductInfoResult { get; set; }
    }
    public class sysProductInfoResult
    {
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SysProductInfoDto sysProductInfoDto { get; set; }
    }

    public class SysProductInfoDto
    {
        /// <summary>
        /// 
        /// </summary>
        public string serialNo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int pdtState { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string filialeCode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string venderCode { get; set; }
    }

    
}
