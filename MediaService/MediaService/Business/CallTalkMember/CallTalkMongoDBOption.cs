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
    /// 呼叫记录数据操作类
    /// </summary>
    public static class CallTalkMongoDBOption
    {
        private const string CallTalkTableName = "ct_";
        private const string ResponseCallTalkTableName = "rct_";
        /// <summary>
        /// 保存呼叫记录
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="call"></param>
        public static void SaveCallTalk(CallTalkInfo call)
        {
            MongoCollection amountCollection = MediaService.mongoDataBase.GetCollection(CallTalkTableName + DateTime.Now.ToString("yyyyMMdd"));
            amountCollection.Insert(typeof(CallTalkInfo), call);
        }

        /// <summary>
        /// 保存呼叫记录
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="call"></param>
        public static void SaveCallTalk(List<CallTalkInfo> call)
        {
            MongoCollection amountCollection = MediaService.mongoDataBase.GetCollection(CallTalkTableName + DateTime.Now.ToString("yyyyMMdd"));
            amountCollection.InsertBatch(typeof(CallTalkInfo), call);
        }

        /// <summary>
        /// 查询呼叫记录
        /// </summary>
        /// <returns></returns>
        public static List<CallTalkInfo> QueryCallTalk(DateTime start, DateTime end, string talkName)
        {
            List<CallTalkInfo> calltalks = new List<CallTalkInfo>();
            //按时间区间查询数据
            int i = 0;
            do
            {
                MongoCollection mongoCollection = MediaService.mongoDataBase.GetCollection(CallTalkTableName + start.ToString("yyyyMMdd"));//选择集合
                IMongoQuery limit = Query.And(Query.GT("CallTime", start), Query.LT("CallTime", end));
                IMongoQuery query = Query.And(Query.EQ("TalkName", talkName), limit);
                MongoCursor curson = mongoCollection.FindAs(typeof(CallTalkInfo), query);
                foreach (CallTalkInfo call in curson)
                {
                    calltalks.Add(call);
                }
            }
            while (end.Subtract(start.AddDays(++i)).TotalDays > 0);
            return calltalks;
        }

        /// <summary>
        /// 保存响应呼叫记录
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="call"></param>
        public static void SaveResponseCallTalk(ResponseCallTalk response)
        {
            MongoCollection amountCollection = MediaService.mongoDataBase.GetCollection(ResponseCallTalkTableName + DateTime.Now.ToString("yyyyMMdd"));
            amountCollection.Insert(typeof(ResponseCallTalk), response);
        }

        /// <summary>
        /// 保存响应呼叫记录
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="call"></param>
        public static void SaveResponseCallTalk(List<ResponseCallTalk> response)
        {
            MongoCollection amountCollection = MediaService.mongoDataBase.GetCollection(ResponseCallTalkTableName + DateTime.Now.ToString("yyyyMMdd"));
            amountCollection.InsertBatch(typeof(ResponseCallTalk), response);
        }

        /// <summary>
        /// 查询响应呼叫记录
        /// </summary>
        /// <returns></returns>
        public static List<ResponseCallTalk> QueryResponseCallTalk(DateTime start, DateTime end, string guid)
        {
            List<ResponseCallTalk> responses = new List<ResponseCallTalk>();
            //按时间区间查询数据
            int i = 0;
            do
            {
                MongoCollection mongoCollection = MediaService.mongoDataBase.GetCollection(ResponseCallTalkTableName + DateTime.Now.ToString("yyyyMMdd"));//选择集合
                IMongoQuery query = Query.EQ("TGuid", guid);
                MongoCursor curson = mongoCollection.FindAs(typeof(ResponseCallTalk), query);
                foreach (ResponseCallTalk call in curson)
                {
                    responses.Add(call);
                }
            }
            while (end.Subtract(start.AddDays(++i)).TotalDays > 0);
            return responses;
        }
    }
}
