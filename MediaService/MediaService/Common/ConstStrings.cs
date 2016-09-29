using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace MediaService
{
    public class ConstStrings
    {
        /// <summary>
        /// 协议头
        /// </summary>
        public const string Headstr = "-----------------------------7dd27182c0258\r\n"
                                      + "Content-Disposition: form-data; name=\"file\"; filename=\"E:\\1.mp3\"\r\n"
                                      + "Content-Type: audio/mpeg\r\n"
                                      + "\r\n";

        /// <summary>
        /// 协议尾
        /// </summary>
        public const string Endstr = "\r\n\r\n-----------------------------7dd27182c0258--\r\n";

        /// <summary>
        /// 请求URL头 (统一写成字符串常量)
        /// </summary>
        public const string REQUEST_URL_HEAD = TEST;

        public const string PE = "http://base.api.dbscar.com/";
        public const string TEST = "http://golo.test.x431.com:8008/dev/";

        /// <summary>
        /// 需要检测经销商的SN开头
        /// </summary>
        public const string StrNeedCheckReSeller = "971695";

        /// <summary>
        /// 测试环境appkey
        /// </summary>
        public const string StrNeedCheckReSeller_Debug_Appkey = "64a4c44a797ae1e9147ee7cde50b11e7";

        /// <summary>
        /// 正式环境appkey
        /// </summary>
        public const string StrNeedCheckReSeller_Release_Appkey = "06d3c9f62b6d332bb125d556fd3baf7b";

        /// <summary>
        /// 测试环境appid
        /// </summary>
        public const string StrNeedCheckReSeller_Debug_AppId = "InnerApp";

        /// <summary>
        /// 正式环境appid
        /// </summary>
        public const string StrNeedCheckReSeller_Release_AppId = "innerdev";

        public const string StrNeedCheckReSeller_Debug_Url = "http://mycar.test.x431.com:8000/rest/sysProduct/getSerialNoInfo.json";
        public const string StrNeedCheckReSeller_Release_Url = "http://mycar.x431.com/rest/sysProduct/getSerialNoInfo.json";


    }
}
