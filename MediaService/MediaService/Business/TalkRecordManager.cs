using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    public class TalkRecordDic : ConcurrentDictionary<int, DateTime>
    {

    }

    public class TalkRecordManager
    {
        #region 单例
        private static TalkRecordManager _Instance = null;
        private static object _thisLock = new object();
        /// <summary>
        /// 单例
        /// </summary>
        internal static TalkRecordManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_thisLock)
                    {
                        if (_Instance == null)
                            _Instance = new TalkRecordManager();
                    }
                }
                return _Instance;
            }
        }

        private TalkRecordManager()
        {
        }
        #endregion

        private const int _Maximum = 50;//TalkRecordDic 最大容量，多余则清理50
        private const int _MaxMinutes = 10;//清理最小缓存时间2
        private static ConcurrentDictionary<int, TalkRecordDic> _TalkRecordDic = new ConcurrentDictionary<int, TalkRecordDic>();//约聊缓存

        /// <summary>
        /// 添加最新约聊对象
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tuid"></param>
        public void TryAddRecord(int uid, int tuid)
        {
            if (!_TalkRecordDic.ContainsKey(uid))
            {
                TalkRecordDic records = new TalkRecordDic();
                records.TryAdd(tuid, DateTime.Now);
                _TalkRecordDic.TryAdd(uid, records);
                return;
            }
            if (IsLegalTuid(uid, tuid))
            {
                TalkRecordDic records = new TalkRecordDic();
                if (_TalkRecordDic.TryGetValue(uid, out records))
                {
                    if (records.ContainsKey(tuid))
                    {
                        DateTime dt;
                        if (records.TryGetValue(tuid, out dt))
                        {
                            records.TryUpdate(tuid, DateTime.Now, dt);
                        }
                    }
                    else
                        records.TryAdd(tuid, DateTime.Now);
                }
                if (records.Count > _Maximum)
                {
                    List<int> hasClear = new List<int>();
                    foreach (KeyValuePair<int, DateTime> record in records)
                    {
                        TimeSpan span = (TimeSpan)(DateTime.Now - record.Value);
                        if (span.TotalMinutes > _MaxMinutes)
                        {
                            hasClear.Add(record.Key);
                        }
                    }
                    //过滤当前约聊
                    if (hasClear.Contains(tuid))
                        hasClear.Remove(tuid);
                    DateTime dt;
                    hasClear.ForEach(x => records.TryRemove(x, out dt));
                }
            }
        }

        /// <summary>
        /// 检查tuid是否合法
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="tuid"></param>
        /// <returns>true:合法,false:不合法</returns>
        public bool IsLegalTuid(int uid, int tuid)
        {
            if (!_TalkRecordDic.ContainsKey(uid))
                return true;
            TalkRecordDic records = new TalkRecordDic();
            if (_TalkRecordDic.TryGetValue(uid, out records))
            {
                if (records.ContainsKey(tuid))
                {
                    DateTime dt;
                    if (records.TryGetValue(tuid, out dt))
                    {
                        TimeSpan span = (TimeSpan)(DateTime.Now - dt);
                        MediaService.WriteLog("约聊匹配时间间隔 legal=" + (span.Seconds < MediaService.TalkRecordTime) + ",secords=" + span.Seconds + ",interval:" + MediaService.TalkRecordTime, MediaService.wirtelog);
                        if (span.TotalSeconds < MediaService.TalkRecordTime)
                            return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 查询记录
        /// </summary>
        /// <returns></returns>
        public string GetRecord()
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder subsb = new StringBuilder();
            foreach (KeyValuePair<int, TalkRecordDic> records in _TalkRecordDic)
            {
                subsb.Clear();
                if (!string.IsNullOrWhiteSpace(sb.ToString()))
                {
                    sb.Append(",");
                }
                sb.Append("{\"uid\":" + records.Key + ",\"records\":[");
                foreach (KeyValuePair<int, DateTime> record in records.Value)
                {
                    if (!string.IsNullOrWhiteSpace(subsb.ToString()))
                    {
                        subsb.Append(",");
                    }
                    subsb.Append("{\"tuid\":" + record.Key + ",\"datetime\":\"" + record.Value + "\"}");
                }
                sb.Append(subsb.ToString());
                sb.Append("]}");
            }
            return sb.ToString();
        }
    }
}
