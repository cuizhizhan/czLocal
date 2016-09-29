using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MediaService
{
    /// <summary>
    /// 游客对讲需求业务逻辑
    /// </summary>
    public abstract class HttpVisitorBusiness
    {
        //创建游客频道
        public static string CreateVisitorTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,talkname,auth
            */
            try
            {
                if (qs["uid"] == null || qs["talkname"] == null || qs["auth"] == null)
                {
                    return CommFunc.StandardFormat(MessageCode.MissKey);
                }
                if (SqlHelper.ExecuteScalar(string.Format("select tid from wy_talk t where t.talkname={0}", qs["talkname"])) != null)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkExist);
                }
                var talkname = qs["talkname"];
                var auth = qs["auth"];
                var uid = qs["uid"];
                //插入频道表
                var sqlInsert = string.Format("insert into [wy_talk](talkname,auth,usernum,type,talkmode) values({0},'{1}',{2},{3},{4})", talkname, auth, 1, (int)EnumTalkType.Visitor, (int)EnumTalkMode.JIT);
                SqlHelper.ExecuteNonQuery(sqlInsert);
                var id = SqlHelper.ExecuteScalar(string.Format("select tid from wy_talk t where t.talkname={0}", qs["talkname"]));
                if (id != null && !string.IsNullOrEmpty(id.ToString()))
                {
                    //插入频道用户表
                    var sqlInsertUser = string.Format("insert into [wy_talkuser](tid,uid) values({0},{1})", id, uid);
                    SqlHelper.ExecuteNonQuery(sqlInsertUser);
                    var jsonCommModel = new AppJsonResultModel<TalkResultModel>((int)MessageCode.Success,
                    MessageCodeDiscription.GetMessageCodeDiscription(MessageCode.Success), new TalkResultModel() { tid = id.ToString()});
                    var jsonstr = JsonConvert.SerializeObject(jsonCommModel);
                    return jsonstr;
                }
                return CommFunc.StandardFormat(MessageCode.TalkCreateFaild);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.TalkCreateFaild);
            }
        }

        //查询游客频道信息
        public static string SearchVisitorTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * talkname,auth
            */
            try
            {
                if (qs["talkname"] == null || qs["auth"] == null)
                {
                    return CommFunc.StandardFormat(MessageCode.MissKey);
                }
                var datatable = SqlHelper.ExecuteTable(string.Format("select tid from wy_talk t where t.talkname={0} and t.auth='{1}' and t.type={2}", qs["talkname"], qs["auth"], (int)EnumTalkType.Visitor));
                if (datatable == null || datatable.Rows.Count <= 0)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkNotExist);
                }

                var id = datatable.Rows[0]["tid"];
                var jsonCommModel = new AppJsonResultModel<TalkResultModel>((int)MessageCode.Success, MessageCodeDiscription.GetMessageCodeDiscription(MessageCode.Success), new TalkResultModel() { tid = id.ToString() });
                var jsonstr = JsonConvert.SerializeObject(jsonCommModel);
                return jsonstr;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.SearchFaild);
            }

        }

        //加入游客频道信息
        public static string JoinVisitorTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,tid
            */
            try
            {
                if (qs["uid"] == null || qs["tid"] == null)
                {
                    return CommFunc.StandardFormat(MessageCode.MissKey);
                }
                var uid = qs["uid"];
                var tid = qs["tid"];
                var datatable = SqlHelper.ExecuteTable(string.Format("select tid from wy_talk t where t.tid={0} and t.type={1}", tid, (int)EnumTalkType.Visitor));
                if (datatable == null || datatable.Rows.Count <= 0)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkNotExist);
                }
                if (SqlHelper.ExecuteScalar(string.Format("select tid from wy_talkuser t where t.tid={0} and t.uid={1}", tid, uid)) != null)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, "重复加入");
                }
                //插入频道用户表
                var sqlInsertUser = string.Format("insert into [wy_talkuser](tid,uid) values({0},{1});update [wy_talk] set usernum=usernum+1 where tid={0};", tid, uid);
                SqlHelper.ExecuteNonQuery(sqlInsertUser);
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, e.Message);
            }
        }

        //退出游客频道
        public static string ExitVisitorTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,tid
            */
            try
            {
                if (qs["uid"] == null || qs["tid"] == null)
                {
                    return CommFunc.StandardFormat(MessageCode.MissKey);
                }
                var uid = qs["uid"];
                var tid = qs["tid"];
                //判断频道是否存在
                var _tid = SqlHelper.ExecuteScalar(string.Format("select tid from wy_talk t where t.tid={0} and t.type={1}", tid, (int)EnumTalkType.Visitor));
                if (_tid == null)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkNotExist);
                }
                //根据频道id查找频道用户表的用户
                var sqlselect = string.Format("select uid from wy_talkuser t where t.tid={0}", tid);
                var datatable = SqlHelper.ExecuteTable(sqlselect);
                //如果只剩一个用户,并且是当前要退出的用户,则删除这个频道,否则只退出这个用户
                if (datatable != null && datatable.Rows.Count == 1 && datatable.Rows[0]["uid"].ToString() == uid)
                {
                    var sqlInsertUser = string.Format("delete from [wy_talkuser] where tid={0};delete from [wy_talk] where tid={0};", tid);
                    SqlHelper.ExecuteNonQuery(sqlInsertUser);
                }
                else
                {
                    //删除频道用户表数据
                    var sqlInsertUser = string.Format("delete from [wy_talkuser] where tid={0} and uid={1};update [wy_talk] set usernum=usernum-1 where tid={0};", tid, uid);
                    SqlHelper.ExecuteNonQuery(sqlInsertUser);
                }

                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DeleteFaild, e.Message);
            }
        }
    }
}
