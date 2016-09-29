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
    /// 呼叫信息
    /// </summary>
    public class CallTalkInfo
    {
        /// <summary>
        /// mongodb内置id
        /// </summary>
        public ObjectId _id { get; set; }
        /// <summary>
        /// 呼叫者
        /// </summary>
        public int Uid { get; set; }
        /// <summary>
        /// 呼叫者SN
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// 呼叫GUID
        /// </summary>
        public string TGuid { get; set; }
        /// <summary>
        /// 频道号
        /// </summary>
        public int Tid { get; set; }
        /// <summary>
        /// 频道名称
        /// </summary>
        public string TalkName { get; set; }
        /// <summary>
        /// 被呼叫用户
        /// </summary>
        public string CalledUid
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in CalledUsers)
                {
                    sb.Append(item);
                    if (item != CalledUsers.LastOrDefault())
                        sb.Append(",");
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// 呼叫时间
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CallTime { get; set; }
        /// <summary>
        /// 被呼叫用户
        /// </summary>
        public List<int> CalledUsers { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="sn"></param>
        /// <param name="guid"></param>
        /// <param name="tid"></param>
        /// <param name="talkname"></param>
        /// <param name="user"></param>
        public CallTalkInfo(int uid, string sn, string guid, int tid, string talkname, List<int> user)
        {
            CallTime = DateTime.Now;
            SN = sn;
            CalledUsers = user;
            Uid = uid;
            TGuid = guid;
            Tid = tid;
            TalkName = talkname;
        }
    }
}
