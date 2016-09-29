using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 服务地址缓存
    /// </summary>
    public class ServerCache
    {
        /// <summary>
        /// 从数据库中查询服务地址
        /// </summary>
        /// <returns></returns>
        public static List<string> SelectServers()
        {
            List<string> addresses = new List<string>();
            try
            {
                DataTable dt = SqlHelper.ExecuteTable("select address from [wy_servers] where type=1");
                if (dt == null || dt.Rows.Count == 0)
                    return addresses;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    addresses.Add(dt.Rows[i]["address"].ToString());
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
            }
            StringBuilder sb = new StringBuilder();
            addresses.ForEach(x => sb.Append(x + ","));
            MediaService.WriteLog("从数据库中查询服务地址列表：" + sb.ToString(), MediaService.wirtelog);
            return addresses;
        }

        public static void SetServers(List<string> servers)
        {
            CacheHelper.Set("MediaService_Servers", servers);
        }

        public static List<string> GetServer()
        {
            return CacheHelper.Get<List<string>>("MediaService_Servers");
        }
    }
}
