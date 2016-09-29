using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 呼叫应答
    /// </summary>
    public class ResponseCallTalk
    {
        /// <summary>
        /// mongodb内置id
        /// </summary>
        public ObjectId _id { get; set; }
        /// <summary>
        /// 应答者
        /// </summary>
        public int Uid { get; set; }
        /// <summary>
        /// 应答者SN
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// 应答GUID
        /// </summary>
        public string TGuid { get; set; }
        /// <summary>
        /// 频道号
        /// </summary>
        public int Tid { get; set; }
        /// <summary>
        /// 用户当时的状态
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// 呼叫时间
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ResponseTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="guid"></param>
        /// <param name="tid"></param>
        /// <param name="type"></param>
        public ResponseCallTalk(int uid,string sn,string guid,int tid,int type)
        {
            Uid = uid;
            SN = sn;
            TGuid = guid;
            Tid = tid;
            Type = type;
            ResponseTime = DateTime.Now;
        }

    }
}
