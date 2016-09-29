using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 点赞数据整理
    /// </summary>
    public class DZAmountManger
    {
        #region 单例
        private static DZAmountManger _Instance = null;
        private static object _thisLock = new object();
        /// <summary>
        /// 单例
        /// </summary>
        internal static DZAmountManger Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_thisLock)
                    {
                        if (_Instance == null)
                            _Instance = new DZAmountManger();
                    }
                }
                return _Instance;
            }
        }

        private DZAmountManger()
        {
        }

        #endregion

        #region 统计

        /// <summary>
        ///  整理点赞信息，暂时按照天数整理
        /// </summary>
        /// <param name="dt">基准时间点（整理dt时间之前的数据）</param>
        public void Amount(DateTime dt)
        {
            dt = Convert.ToDateTime(dt.ToString("yyyy/MM/dd"));
            //获取上一个时段存储记录
            DateTime lastTime = GetLastPoint(dt);
            TimeSpan ts = (TimeSpan)(dt - lastTime);
            int days = (int)System.Math.Ceiling((double)ts.Days);
            DateTime temp = lastTime;
            for (int i = 1; i < days; i++)
            {
                temp = lastTime.AddDays(i);
                List<QueryDianZaninfo> dz = QueryLastPoint(temp);
                if (dz.Count == 0)
                    continue;
                List<DianZanAmountInfo> amounts = AmountDianZan(dz);
                if (amounts.Count == 0)
                    continue;
                SaveAmountInfo(amounts, temp);
            }

        }

        //存储整理数据
        private void SaveAmountInfo(List<DianZanAmountInfo> amounts, DateTime dt)
        {
            MediaService.WriteLog("存储统计点赞的时间段：" + dt.ToString("yyyy-MM-dd"), MediaService.wirtelog);
            MongoCollection mongoCollection = MediaService.mongoDataBase.GetCollection("dz_" + dt.ToString("yyyyMM"));//选择集合
            //批量插入,不考虑重复,在查询数据的时候过滤
            mongoCollection.InsertBatch(typeof(DianZanAmountInfo), amounts);

            //修改整理时间点
            MongoCollection amountCollection = MediaService.mongoDataBase.GetCollection("dz_amount");
            IMongoQuery query = Query.Type("time", BsonType.Int64);
            DZAmount one = (DZAmount)amountCollection.FindOneAs(typeof(DZAmount), query);
            if (one == null)
            {
                DZAmount at = new DZAmount(ConvertDateTime(dt));
                amountCollection.Insert(typeof(DZAmount), at);
            }
            else
            {
                one.time = ConvertDateTime(dt);
                BsonDocument bd = BsonExtensionMethods.ToBsonDocument(one);
                query = Query.EQ("_id", one._id);
                amountCollection.Update(query, new UpdateDocument(bd));
            }
        }

        //将所有的点赞信息进行统计
        public List<DianZanAmountInfo> AmountDianZan(List<QueryDianZaninfo> infos)
        {
            List<string> files = new List<string>();
            infos.ForEach(x =>
            {
                if (!files.Contains(x.file))
                    files.Add(x.file);
            });
            List<DianZanAmountInfo> amounts = new List<DianZanAmountInfo>();
            foreach (var info in files)
            {
                IEnumerable<QueryDianZaninfo> result = infos.Where(x => x.file == info);
                DianZanAmountInfo amount = AmountDianZan(result);
                amounts.Add(amount);
            }
            return amounts;
        }

        //记录一个文件的点赞
        private DianZanAmountInfo AmountDianZan(IEnumerable<QueryDianZaninfo> infos)
        {
            if (!infos.Any())
                return null;
            QueryDianZaninfo dzinfo = infos.FirstOrDefault();
            long mintime = infos.Min(x => x.time);
            string url = PackageUrl(dzinfo.file, mintime);
            int count = infos.Count();
            int tlen = dzinfo.tlen;
            DianZanAmountInfo amount = new DianZanAmountInfo(dzinfo.channel, dzinfo.ouid, url, mintime, count,tlen);
            return amount;
        }

        //组装URL
        private string PackageUrl(string file, long _time)
        {
            DateTime dt = StampToDateTime(_time);
            String time = dt.ToString("yyyy/MM/dd");//"2015/08/27";
            String url = "http://res.talk.golo5.com/media_mp3/" + time + "/" + file + ".mp3";
            return url;
        }

        //取出当天的点赞信息
        private List<QueryDianZaninfo> QueryLastPoint(DateTime dt)
        {
            //查询
            MongoCollection mongoCollection = MediaService.mongoDataBase.GetCollection("dz_" + dt.ToString("yyyyMMdd"));//选择集合
            MongoCursor curson = mongoCollection.FindAllAs(typeof(QueryDianZaninfo));
            List<QueryDianZaninfo> infos = new List<QueryDianZaninfo>();
            foreach (QueryDianZaninfo tmp in curson)
            {
                infos.Add(tmp);
            }
            return infos;
        }

        //上一个存储时段，没有则设置为上一天时间
        private DateTime GetLastPoint(DateTime dt)
        {
            MongoCollection mongoCollection = MediaService.mongoDataBase.GetCollection("dz_amount");//选择集合
            IMongoQuery query = Query.Type("time", BsonType.Int64);
            DZAmount one = (DZAmount)mongoCollection.FindOneAs(typeof(DZAmount), query);
            if (one == null || one.time == 0)
                return dt.AddDays(-10);
            else
                return StampToDateTime(one.time);
        }

        #endregion

        #region 查询

        //查询当月记录下uid 中的时间段数据
        private List<DianZanAmountInfo> QueryRecord(int uid, DateTime dt, bool up)
        {
            long time = ConvertDateTime(dt);
            List<DianZanAmountInfo> amounts = new List<DianZanAmountInfo>();
            //选择向上（向下）数据
            MongoCollection mongoCollection = MediaService.mongoDataBase.GetCollection("dz_" + dt.ToString("yyyyMM"));//选择集合
            IMongoQuery limit = up ? Query.LT("time", time) : Query.GT("time", time);
            IMongoQuery query = Query.And(Query.EQ("uid", uid), limit);
            MongoCursor curson = mongoCollection.FindAs(typeof(DianZanAmountInfo), query);
            foreach (DianZanAmountInfo tmp in curson)
            {
                if (amounts.FirstOrDefault(x => x.url.Equals(tmp.url)) == null)
                    amounts.Add(tmp);
            }
            return amounts;
        }

        //统计数据
        private List<DianZanAmountInfo> SumRecord(int uid, int count, DateTime dt, bool up)
        {
            List<DianZanAmountInfo> amounts = new List<DianZanAmountInfo>();
            int i = 0;
            while (amounts.Count < count && i < 6)
            {
                int u = up ? -i : i;
                if (dt.AddMonths(u) > DateTime.Now)
                    break;
                foreach (var item in QueryRecord(uid, dt.AddMonths(u), up))
                {
                    if (amounts.FirstOrDefault(x => x.url.Equals(item.url)) == null)
                        amounts.Add(item);
                }
                i++;
            }
            return amounts;
        }

        /// <summary>
        /// 查询点赞
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="count"></param>
        /// <param name="time"></param>
        /// <param name="up"></param>
        /// <returns></returns>
        public List<DianZanAmountInfo> QueryAmountInfo(int uid, int count, long time, bool up)
        {
            if (up)
                return QueryAmountInfoUp(uid, count, time);
            else
                return QueryAmountInfoDown(uid, count, time);
        }

        //日期向下翻页(找日期小的时间)
        private List<DianZanAmountInfo> QueryAmountInfoUp(int uid, int count, long time)
        {
            bool up = true;
            //1.查询时间
            DateTime dt = StampToDateTime(time);
            DateTime savetime = GetLastPoint(dt);
            TimeSpan ts = (TimeSpan)(dt - savetime);
            List<DianZanAmountInfo> amounts = new List<DianZanAmountInfo>();
            if (ts.TotalDays < 0)
            {
                //直接查历史库
                amounts = SumRecord(uid, count, dt, up);
            }
            else
            {
                ts = (TimeSpan)(DateTime.Now - dt);
                if (ts.TotalDays <= 2)//查询当日库
                {
                    //当前时间day < 2 查询日库
                    for (int i = 0; i < 2; i++)
                    {
                        List<DianZanAmountInfo> _amounts = ScreenOut(ConvertDateTime(dt.AddDays(-i)), uid, up, time);
                        if (_amounts.Count == 0)
                            continue;
                        foreach (var item in _amounts)
                        {
                            if (amounts.FirstOrDefault(x => x.url.Equals(item.url)) == null)
                                amounts.Add(item);
                        }
                        if (amounts.Count >= count)
                            break;
                    }
                }
                if (amounts.Count < count)//查询当月库
                {
                    List<DianZanAmountInfo> _amounts = SumRecord(uid, count, dt, up);
                    foreach (var item in _amounts)
                    {
                        if (amounts.FirstOrDefault(x => x.url.Equals(item.url)) == null)
                            amounts.Add(item);
                    }
                }
            }
            if (amounts.Count < count)
                return amounts.OrderByDescending(x => x.time).ToList<DianZanAmountInfo>();
            else
                return amounts.OrderByDescending(x => x.time).Take(count).ToList<DianZanAmountInfo>();
        }

        //日期向上翻页（找日期大的时间）
        private List<DianZanAmountInfo> QueryAmountInfoDown(int uid, int count, long time)
        {
            bool up = false;
            //1.查询时间
            DateTime dt = StampToDateTime(time);
            DateTime savetime = GetLastPoint(dt);
            TimeSpan ts = (TimeSpan)(dt - savetime);
            List<DianZanAmountInfo> amounts = new List<DianZanAmountInfo>();
            if (ts.TotalDays > 0)//直接查日库
            {
                //当前时间day < 2 查询日库
                for (int i = 0; i < 2; i++)
                {
                    List<DianZanAmountInfo> _amounts = ScreenOut(ConvertDateTime(dt.AddDays(i)), uid, up, time);
                    if (_amounts.Count == 0)
                        continue;
                    foreach (var item in _amounts)
                    {
                        if (amounts.FirstOrDefault(x => x.url.Equals(item.url)) == null)
                            amounts.Add(item);
                    }
                    if (amounts.Count >= count)
                        break;
                }

            }
            else
            {
                //直接查历史库
                amounts = SumRecord(uid, count, dt, up);
                if (amounts.Count < count)//查询当日库
                {
                    ts = (TimeSpan)(DateTime.Now - savetime);
                    for (int i = 0; i < System.Math.Ceiling(ts.TotalDays); i++)
                    {
                        List<DianZanAmountInfo> _amounts = ScreenOut(ConvertDateTime(dt.AddDays(i)), uid, up, time);
                        if (_amounts.Count == 0)
                            continue;
                        foreach (var item in _amounts)
                        {
                            if (amounts.FirstOrDefault(x => x.url.Equals(item.url)) == null)
                                amounts.Add(item);
                        }
                        if (amounts.Count >= count)
                            break;
                    }
                }
            }
            if (amounts.Count < count)
                return amounts.OrderBy(x => x.time).ToList<DianZanAmountInfo>();
            else
                return amounts.OrderBy(x => x.time).Take(count).ToList<DianZanAmountInfo>();
        }

        //从日库中筛选出uid中时间点上下翻页数据
        private List<DianZanAmountInfo> ScreenOut(long querytime, int uid, bool up, long comparetime)
        {
            List<QueryDianZaninfo> infos = QueryLastPoint(querytime, uid);
            List<DianZanAmountInfo> amounts = AmountDianZan(infos);//一天的点赞统计信息

            if (up)
                return amounts.FindAll(x => x.time < comparetime);
            else
                return amounts.FindAll(x => x.time > comparetime);
        }

        //取出uid当天的点赞信息
        private List<QueryDianZaninfo> QueryLastPoint(long time, int uid)
        {
            DateTime dt = StampToDateTime(time);
            MongoCollection mongoCollection = MediaService.mongoDataBase.GetCollection("dz_" + dt.ToString("yyyyMMdd"));//选择集合
            IMongoQuery query = Query.EQ("ouid", uid);
            MongoCursor curson = mongoCollection.FindAs(typeof(QueryDianZaninfo), query);
            List<QueryDianZaninfo> infos = new List<QueryDianZaninfo>();
            foreach (QueryDianZaninfo tmp in curson)
            {
                infos.Add(tmp);
            }
            return infos;
        }

        #endregion

        #region 获取标准时间戳

        public long GetTimeStamp()
        {
            return DateTime.UtcNow.Ticks / 10000000 - 62135596800;
        }

        // 时间戳转为C#格式时间
        public DateTime StampToDateTime(long timeStamp)
        {
            DateTime dateTimeStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dateTimeStart.Add(toNow);
        }
        /// <summary>
        /// DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name=”time”></param>
        /// <returns></returns>
        public long ConvertDateTime(System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        }
        #endregion
    }
    /// <summary>
    /// 将整理的时间点存入MongoDB
    /// </summary>
    public class DZAmount
    {
        public ObjectId _id;
        public long time = 0;

        public DZAmount(long _time)
        {
            time = _time;
        }
    }
}
