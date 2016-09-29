using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 
    /// </summary>
    public static class CallBackTask
    {
        /// <summary>
        /// 
        /// </summary>
        public static ConcurrentDictionary<Guid, CallBackInfo> CallBack = new ConcurrentDictionary<Guid, CallBackInfo>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="g"></param>
        /// <param name="info"></param>
        public static void AddCallBackInfo(string g, CallBackInfo info)
        {
            Guid guid;
            bool isOK = Guid.TryParse(g, out guid);
            if (!isOK)
                return;
            if (!CallBack.ContainsKey(guid))
                CallBack.TryAdd(guid, info);
        }

        /// <summary>
        /// 添加接收uid
        /// </summary>
        /// <param name="g"></param>
        /// <param name="uid"></param>
        public static void UpdateCallBackInfo(string g, int uid)
        {

            Guid guid;
            bool isOK = Guid.TryParse(g, out guid);
            if (!isOK)
                return;
            CallBackInfo info = null;
            CallBack.TryGetValue(guid, out info);
            if (info == null)
                return;
            info.SetReceive(uid);
        }
    }
}
