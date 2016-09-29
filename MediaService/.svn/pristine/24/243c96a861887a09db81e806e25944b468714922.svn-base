using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 回调
    /// </summary>
    public class CallBackService
    {
        #region 单例
        private static CallBackService _Instance = null;
        private static object _thisLock = new object();
        /// <summary>
        /// 单例
        /// </summary>
        internal static CallBackService Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_thisLock)
                    {
                        if (_Instance == null)
                            _Instance = new CallBackService();
                    }
                }
                return _Instance;
            }
        }

        private CallBackService()
        {
        }

        #endregion

        /// <summary>
        /// 启动回调线程服务
        /// </summary>
        public void StartService()
        {
            Thread callback = new Thread(new ThreadStart(CheckCallBack));
            callback.IsBackground = true;
            callback.Start();

            MediaService.WriteLog("------启动回调线程服务------", true);
        }

        private void CheckCallBack()
        {
            while (true)
            {
                try
                {
                    List<Guid> guid = new List<Guid>();
                    foreach (var item in CallBackTask.CallBack)
                    {
                        if (!item.Value.IsCompleted())
                            item.Value.SendBuffer();
                        else
                            guid.Add(item.Key);
                    }
                    guid.ForEach(x =>
                    {
                        CallBackInfo info = null;
                        CallBackTask.CallBack.TryRemove(x, out info);
                    });
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("回调线程服务错误：" + err.Message, true);
                }
                Thread.Sleep(20000);
            }
        }

    }
}
