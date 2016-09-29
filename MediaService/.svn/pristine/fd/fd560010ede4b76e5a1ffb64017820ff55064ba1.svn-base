using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MediaService
{
    /// <summary>
    /// 点赞记录
    /// </summary>
    public class Parise
    {
        public ObjectId _id { get; set; }
        /// <summary>
        /// 点赞id 精确到秒
        /// </summary>
        public long LastModiTime { get; set; }
        /// <summary>
        /// 聊天消息id
        /// </summary>
        public long MsgTime { get; set; }
        public int Uid { get; set; }
        public int IsParise { get; set; }
        /// <summary>
        /// 被赞的ouid
        /// </summary>
        public int Ouid { get; set; }
        public int Tid { get; set; }
    }

    /// <summary>
    /// 聊天消息
    /// </summary>
    public class MessageInfo
    {
        public ObjectId _id { get; set; }
        /// <summary>
        /// 时间做主键 HHmmssfff
        /// </summary>
        public int Time { get; set; }
        public int Tid { get; set; }
        public int Senduid { get; set; }
        public string Message { get; set; }
        public byte PackId { get; set; }
    }

    public class VogMessage
    {
        public ObjectId _id { get; set; }
        /// <summary>
        /// 主键 UtcNow.Ticks
        /// </summary>
        public long CreateTime { get; set; }
        public ulong Datetime { get; set; }
        public int Tid { get; set; }
        public int Senduid { get; set; }
        public int Ouid { get; set; }
    }
}
