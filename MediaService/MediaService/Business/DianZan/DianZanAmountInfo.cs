using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MediaService
{
    public class DianZanAmountInfo
    {
        public ObjectId _id;
        public int channel = 0;//频道号
        public int uid = 0;//被点赞用户uid
        public string url = "";//文件地址
        public long time = 0;//第一次点赞时间
        public int count = 0;//点赞次数
        public int tlen = 0;//音频长度

        public DianZanAmountInfo(int _channel, int _uid, string _url, long _time, int _count, int _tlen)
        {
            channel = _channel;
            uid = _uid;
            url = _url;
            time = _time;
            count = _count;
            tlen = _tlen;
        }

        public override string ToString()
        {
            return "{\"channel\":" + channel + ",\"url\":\"" + url + "\",\"time\":" + time + ",\"count\":" + count + ",\"tlen\":" + tlen + "}";
        }
    }

    public class QueryDianZanAmountInfo : DianZanAmountInfo
    {
        public string channelname = "";//频道名称

        public QueryDianZanAmountInfo(int _channel, int _uid, string _url, long _time, int _count, int _tlen, string _channelname) :
            base(_channel, _uid, _url, _time, _count, _tlen)
        {
            channelname = _channelname;
        }

        public override string ToString()
        {
            return "{\"channel\":" + channel + ",\"channelname\":\"" + channelname + "\",\"url\":\"" + url + "\",\"time\":" + time + ",\"count\":" + count + ",\"tlen\":" + tlen + "}";
        }
    }
}
