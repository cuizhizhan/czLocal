using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 点赞定时器
    /// </summary>
    public class DianZanTimer
    {
        #region 单例
        private static DianZanTimer _Instance = null;
        private static object _thisLock = new object();
        /// <summary>
        /// 单例
        /// </summary>
        internal static DianZanTimer Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_thisLock)
                    {
                        if (_Instance == null)
                            _Instance = new DianZanTimer();
                    }
                }
                return _Instance;
            }
        }

        private DianZanTimer()
        {
            _Timer = new System.Timers.Timer();
            _Timer.Elapsed += _Timer_Elapsed;
            _Timer.Interval = Interval;
            _Timer.AutoReset = true;
            _Timer.Enabled = true;
        }

        #endregion

        private readonly int Interval = 1000 * 60 * 30;
        private System.Timers.Timer _Timer = null;
        private const int _Hour = 04;

        public void Start()
        {
            _Timer.Start();
            MediaService.WriteLog("-----------点赞统计定时器启动成功-----------------", MediaService.wirtelog);
        }

        public void Stop()
        {
            _Timer.Stop();
            MediaService.WriteLog("--------------点赞统计定时器停止-----------------", MediaService.wirtelog);
        }

        void _Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            DateTime dt = DateTime.Now;
            if (dt.Hour == _Hour)
            {
                MediaService.WriteLog("--------------开始处理点赞数据整理--------------", MediaService.wirtelog);
                try
                {
                    //执行DianZan数据处理
                    DZAmountManger.Instance.Amount(dt);
                }
                catch (Exception ex)
                {
                    MediaService.WriteLog("点赞数据整理异常：" + ex.StackTrace, MediaService.wirtelog);
                }
            }
        }

    }
}
