using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MediaService
{
    /// <summary>
    /// 自驾游业务逻辑
    /// </summary>
    public abstract class HttpSelfTravelBusiness
    {
        /// <summary>
        /// 通过App创建的频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string CreateTalkWithoutUid(NameValueCollection qs)
        {
            /* NameValueCollection值列表
             * appid,token,ouid,talkname,verification,[auth],[imageurl],remark,type,talkmode (0 是普通对讲,1是实时对讲)
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null ||
                qs["talkname"] == null && qs["verification"] == null && qs["info"] == null && qs["talkmode"] == null)
            {
                MediaService.WriteLog("CreateTalkWithoutUid ：" + recv, MediaService.wirtelog);
                return recv;
            }
            int type = 0;
            if (!int.TryParse(qs["type"], out type))
            {
                return CommFunc.StandardFormat(MessageCode.FormatError);
            }
            var remark = qs["remark"];
            var verification = qs["verification"];
            var talkname = qs["talkname"];
            var talkmode = qs["talkmode"];
            //验证token
            int ouid = 0;
            bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["appid"].ToString(),
                qs["token"].ToString(), ref ouid, ref recv);
            if (!isVerToken)
                return recv;

            recv = CreateTalk(ouid, verification, talkname, qs["auth"], remark, type, qs["imageurl"], talkmode);
            MediaService.WriteLog("CreateTalk 返回值 ：" + recv, MediaService.wirtelog);
            return recv;
        }

        /// <summary>
        /// 退出/解散app创建的频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string ExitTalkWithoutUid(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * appid,ouid,token,tid
            */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["tid"] == null)
            {
                MediaService.WriteLog("ExitTalkWithoutUid ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog(
                    "ExitTalkWithoutUid ：ouid =" + qs["ouid"] + "token =" + qs["token"] + "appid =" + qs["appid"] +
                    "tid =" + qs["tid"], MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"].ToString(), qs["token"], ref ouid,
                    ref recv);
                if (!isVerToken)
                    return recv;

                //1.查询该频道是否存在
                //2.不存在：返回告知该频道不存在
                //3.存在,判定该用户是否在该频道里面
                //4.如果用户不在在wy_talkuser 里面，删除wy_talk中的数据
                //5.如果用户在wy_talkuser里面,判定是否是自己创建的频道
                //6.是自己创建的频道：该频道信息从wy_talk表中删除；同时删除wy_talkuser tid =wy_talk.tid
                //7.不是自己创建的频道：该频道信息从wy_talkuser tid =wy_talk.tid  and uid = uid 中删除 , wy_talk.usernum-1
                int tid;
                if (!Int32.TryParse(qs["tid"].ToString(), out tid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                if (PublicClass.FindTalkType(tid) == 2)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkNotExist);
                }

                return UserQuitTalk(tid, ouid, Convert.ToInt32(qs["appid"]));
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        /// <summary>
        /// 加入app创建的频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string JoinTalkWithoutUid(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * appid,token, ouid , tid ,[auth]
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["tid"] == null || qs["appid"] == null || qs["token"] == null)
            {
                MediaService.WriteLog("接收到UserJoinTalkWithToken：" + recv, MediaService.wirtelog);
                return recv;
            }
            string auth = qs["auth"] == null ? "" : qs["auth"].Replace("'", "");
            int _tid;
            int.TryParse(qs["tid"], out _tid);
            if (PublicClass.FindTalkType(_tid) == 2)
            {
                return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, "目标为企业频道");
            }
            //验证token  app调用时要验证token
            int ouid = 0;
            bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
            if (!isVerToken)
                return recv;
            //end

            DataTable dt = SqlHelper.ExecuteTable("select tid,auth from [wy_talk] where tid='" + _tid + "'");
            if (dt.Rows.Count > 0)
            {
                if (dt.Rows[0]["auth"].ToString() == "" || dt.Rows[0]["auth"].ToString() == auth)
                {
                    StringBuilder sb = new StringBuilder();
                    Object obj =
                        SqlHelper.ExecuteScalar("select 1 from [wy_talkuser] where tid=" + _tid + " and uid=" + ouid);
                    if (obj == null)
                    {
                        sb.Append("insert [wy_talkuser] (tid,uid,uidtype) values (" + _tid + "," + ouid + "," + 1 +
                                  ");update [wy_talk] set usernum=usernum+1 where tid=" + _tid);
                    }
                    if (sb.ToString() != string.Empty)
                        SqlHelper.ExecuteNonQuery(sb.ToString());
                }
                else
                {
                    if (auth == "")
                        return CommBusiness.WriteErrorJson(43, "需要输入群组验证码");
                    else
                        return CommBusiness.WriteErrorJson(7, "输入的群组验证码错误");
                }
            }
            else
            {
                return CommFunc.StandardFormat(MessageCode.TalkNotExist);
            }
            return CommFunc.StandardFormat(MessageCode.Success);
        }

        #region private func

        private static string CreateTalk(int ouid, string verification, string talkname, string auth, string remark,int type, string imageurl, string talkmode)
        {
            string recv = "";
            if (verification == CommFunc.StringToMD5Hash(talkname + MediaService.Verification))
            {
                object obj =
                    SqlHelper.ExecuteScalar("select count(tid) from [wy_talk] where createuid=" + ouid + " and type=4");
                    //type=4
                if (obj != null && Convert.ToInt32(obj) < 2) //频道数只可创建2个
                {
                    if (CommBusiness.IsValiNum(talkname) && CommBusiness.IsTalkNameOKWithNoToken(talkname) == true)
                    {
                        auth = auth == null ? "" : auth.ToString().Replace("'", "");
                        var _imageurl = imageurl ?? "";
                        obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname + "'");
                        if (obj == null)
                        {
                            obj =
                                SqlHelper.ExecuteScalar(
                                    string.Format(
                                        "insert [wy_talk] (talkname,auth,createuid,type,talknotice,imageurl,talkmode) values ('{0}','{1}',{2},{3},'{4}','{5}',{6});select scope_identity()",
                                        talkname, auth, ouid, type, remark, _imageurl, talkmode));

                            if (obj != null)
                            {
                                obj =
                                    SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname +
                                                            "'");
                                string tid = obj.ToString();
                                SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid,xuhao) values (" + tid + "," +
                                                          ouid + ",'1')");
                                string data = "{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" +
                                              auth + "\"}";
                                recv = CommFunc.StandardObjectFormat(MessageCode.Success, data);
                            }
                            else
                            {
                                recv = CommFunc.StandardFormat(MessageCode.TalkCreateFaild);
                            }
                        }
                        else
                        {
                            recv = CommFunc.StandardFormat(MessageCode.TalkExist);
                        }
                    }
                    else
                    {
                        recv = CommFunc.StandardFormat(MessageCode.TalkInvalid);
                    }
                }
                else
                {
                    recv = CommFunc.StandardFormat(MessageCode.TalkFull);
                }
            }
            else
            {
                recv = CommFunc.StandardFormat(MessageCode.TalkVerificationFaild);
            }
            return recv;
        }

        private static string UserQuitTalk(int tid, int ouid,int appid)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT createuid FROM wy_talk WHERE tid = " + tid);
            object result = SqlHelper.ExecuteScalar(sb.ToString());
            if (result == null)
                return CommFunc.StandardFormat(MessageCode.TalkNotExist);
            int cuid = (int) result;

            sb.Clear();
            sb.Append("SELECT count(*) FROM wy_talkuser,wy_talk WHERE wy_talk.tid=wy_talkuser.tid AND wy_talk.tid = " +
                      tid + " AND wy_talkuser.uid=" + ouid);
            result = SqlHelper.ExecuteScalar(sb.ToString());
            if (result != null && ((int) result) > 0)
            {
                if (cuid == ouid)
                {
                    sb.Clear();
                    //List<int> uids = new List<int>();
                    //sb.Append("SELECT uid FROM wy_talkuser where tid=" + tid);
                    //DataTable uidlist = SqlHelper.ExecuteTable(sb.ToString());
                    //if (uidlist != null && uidlist.Rows.Count > 0)
                    //{
                    //    foreach (DataRow dr in uidlist.Rows)
                    //    {
                    //        uids.Add(Convert.ToInt32(dr["uid"].ToString()));
                    //    }
                    //}
                    sb.Clear();
                    sb.Append("delete [wy_talk] where tid='" + tid + "';");
                    sb.Append("delete [wy_talkuser] where tid='" + tid + "';");
                    SqlHelper.ExecuteNonQuery(sb.ToString());
                }
                else
                {
                    sb.Clear();
                    sb.Append("UPDATE wy_talk SET usernum=usernum-1 WHERE tid =' " + tid + "';");
                    sb.Append("delete [wy_talkuser] where tid='" + tid + "' and uid=" + ouid + ";");
                    SqlHelper.ExecuteNonQuery(sb.ToString());
                }
                SqlHelper.ExecuteNonQuery("UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + ouid);
                string recv = "{\"status\":true,\"tid\":" + tid + ",\"createuid\":" + cuid + "}";
                string uidsSQL = string.Format("select uid from wy_uidmap where ouid={0}", ouid);
                var uidList = new List<int>();
                foreach (DataRow uidRow in SqlHelper.ExecuteTable(uidsSQL).Rows)
                {
                    uidList.Add(Convert.ToInt32(uidRow["uid"]));
                }
                PublicClass.SendToOnlineUserList(null, recv, "", uidList, 99, 0, CommType.userQuitTalk,appid);
            }
            return CommFunc.StandardFormat(MessageCode.Success);
        }

        #endregion

        internal static string GetMyTalkListWithoutUid(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * appid,ouid,token,minitid
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["minitid"] == null)
            {
                MediaService.WriteLog("GetMyTalkListWithoutUid ：" + recv, MediaService.wirtelog);
                return recv;
            }
            StringBuilder log = new StringBuilder("GetMyTalkListWithoutUid ：ouid=" + qs["ouid"] + " token=" + qs["token"] + " appid=" + qs["appid"] + " minitid=" + qs["minitid"]);
            try
            {
                int minitid;
                if (!Int32.TryParse(qs["minitid"], out minitid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                //验证token
                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                StringBuilder sb = new StringBuilder();
                string sql;
                if (minitid == 0)
                {
                    sql = string.Format(
                        "select T1.id,T1.tid,T1.xuhao,T1.duijiang,t2.talknotice,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.imageurl,T2.talkmode from (select id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = {0}) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid and t2.type <> 3 order by T1.id desc;",
                        ouid);
                }
                else
                {
                    sql =
                        string.Format(
                            "select T1.id,T1.tid,T1.xuhao,T1.duijiang,t2.talknotice,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.talkmode,T2.imageurl from (select  id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = {0} and id<{1}) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid and t2.type <> 3 order by T1.id desc;",
                            ouid, minitid);
                }
                List<int> uidsList = new List<int>();
                //var table = SqlHelper.ExecuteTable(string.Format("select uid from wy_uidmap where ouid ={0}", ouid)); //根据ouid查到绑定的uid
                //if (table != null)
                //{
                //    for (int i = 0; i < table.Rows.Count; i++)
                //    {
                //        uidsList.Add(table.Rows[i]["uid"].ToString().ToInt());
                //    }
                //}
                List<int> tids = new List<int>();
                DataTable dt = SqlHelper.ExecuteTable(sql);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int tid = dt.Rows[i]["tid"].ToString().ToInt();
                    if (!tids.Contains(tid))
                        tids.Add(tid);
                    else
                        continue;
                    //string sn = dt.Rows[i]["glsn"] == null ? "" : dt.Rows[i]["glsn"].ToString().Remove(0, 4);
                    string talkname = dt.Rows[i]["talkname"].ToString();
                    string auth = dt.Rows[i]["auth"].ToString();
                    string remark = dt.Rows[i]["remark"].ToString();
                    string talkmode = dt.Rows[i]["talkmode"].ToString();
                    if (dt.Rows[i]["type"] != null)
                    {
                        if (dt.Rows[i]["talknotice"] != null && dt.Rows[i]["talknotice"].ToString() != "")
                        //如果频道表已经有备注, 取频道表的备注
                        {
                            remark = dt.Rows[i]["talknotice"].ToString();
                        }
                    }
                    string create = "false";
                    if (ouid==Convert.ToInt32(dt.Rows[i]["createuid"]))
                    {
                        create = "true";
                    }
                    string imageurl = dt.Rows[i]["imageurl"] == null ? "" : dt.Rows[i]["imageurl"].ToString();
                    string type = dt.Rows[i]["type"].ToString();
                    int totalnum = 0;
                    int usernum = 0;
                    HttpZGoloBusiness.GetTalkNum(tid, ref totalnum, ref usernum);
                    sb.Append(",{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"talkmode\":\"" + talkmode + "\",\"auth\":\"" + auth + "\",\"remark\":\"" + remark + "\",\"create\":" + create + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + ",\"imageurl\":\"" + imageurl + "\",\"type\":\"" + type + "\"}");
                }
                if (dt.Rows.Count > 0)
                {
                    minitid = Int32.Parse(dt.Rows[dt.Rows.Count - 1]["id"].ToString());
                    sb.Remove(0, 1);
                }
                sb.Insert(0, "{\"minitid\":" + minitid + ",\"list\":[");
                sb.Append("]}");
                MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
                return CommFunc.StandardObjectFormat(MessageCode.Success, sb.ToString());
            }
            catch (Exception e)
            {
                log.Append(" 执行异常：").Append(e);
                MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        /// <summary>
        /// 修改频道备注
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string UpdateTalkNoticeWithoutUid(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * appid,ouid,token,tid,[auth],[remark]
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["tid"] == null)
            {
                MediaService.WriteLog("接收到修改频道的信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                string tid = qs["tid"].ToString().Replace("'", "");
                object obj = SqlHelper.ExecuteScalar("select auth from [wy_talk] where tid='" + tid + "' and createuid=" + ouid);
                StringBuilder sb = new StringBuilder();
                sb.Append("{\"status\":true,\"tid\":" + tid);
                if (obj != null)
                {
                    if (qs["auth"] != null)
                    {
                        string auth = qs["auth"].ToString().Replace(",", "");
                        if ((auth.Length == 3 && CommBusiness.IsValiNum(auth) && obj.ToString() != auth) || (auth == ""))
                        {
                            SqlHelper.ExecuteNonQuery("update [wy_talk] set auth='" + qs["auth"].Replace("'", "") + "',usernum=1 where tid='" + tid + "';delete [wy_talkuser] where tid='" + tid + "' and uid!='" + ouid + "'");
                            sb.Append(",\"auth\":\"" + auth + "\"");
                        }
                    }
                }
                if (qs["remark"] != null)
                {
                    sb.Append(",\"remark\":\"" + qs["remark"].ToString().Replace("'", "") + "\"");
                    SqlHelper.ExecuteNonQuery("update [wy_talkuser] set remark='" + qs["remark"].Replace("'", "").Trim() + "' where tid=" + tid + " and uid=" + ouid);

                    object _obj = SqlHelper.ExecuteScalar(string.Format("select type,createuid from [wy_talk] where tid='{0}' and createuid={1} and type=4", tid, ouid));
                    if (_obj != null)
                    {
                        SqlHelper.ExecuteNonQuery(string.Format("update [wy_talk] set talknotice='{0}' where tid={1}", qs["remark"].Replace("'", "").Trim(), tid)); //用户更新频道信息时如果此频道是创建者,同时更新talk的notice. 为了兼容老版本.
                    }
                }

                sb.Append("}");
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        internal static string SearchTalkListForTravel(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * [uid],ouid,appid,token,keyword,pageindex,pagesize
            */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["keyword"] == null || qs["pageindex"] == null || qs["pagesize"] == null) //|| qs["uid"]==null
            {
                MediaService.WriteLog("接收到获取频道列表 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {

                var keyword = qs["keyword"];
                //验证token
                int ouid = 0;
                int uid = 0;
                int.TryParse(qs["uid"], out uid);
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;
                //end

                int pageindex, pagesize;
                if (!int.TryParse(qs["pageindex"], out pageindex) || !int.TryParse(qs["pagesize"], out pagesize))
                {
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                }
                //分页查询type为0或者4并按关键字查询的频道列表 (0:以前的普通对讲频道,4:实时对讲频道)
                string sql;
                if (qs["uid"] == null)
                {
                    sql = string.Format("SELECT TOP {0} * FROM (SELECT ROW_NUMBER() OVER (ORDER BY tid) AS RowNumber,t.* FROM [weiyun].[dbo].wy_talk t where  t.type =4 and (t.talknotice like '%{1}%' or t.talkname like '%{1}%')) A WHERE RowNumber > {2}*({3}-1)", pagesize, keyword, pagesize, pageindex);
                }
                else
                {
                    sql = string.Format("SELECT TOP {0} * FROM (SELECT ROW_NUMBER() OVER (ORDER BY tid) AS RowNumber,t.* FROM [weiyun].[dbo].wy_talk t where t.type in(0,4) and (t.talknotice like '%{1}%' or t.talkname like '%{1}%')) A WHERE RowNumber > {2}*({3}-1)", pagesize, keyword, pagesize, pageindex);
                }

                var datatable = SqlHelper.ExecuteTable(sql);
                List<int> tids = new List<int>();
                var searchTalkListModels = new List<SearchTalkListModel>();
                foreach (DataRow row in datatable.Rows)
                {
                    int tid;
                    int.TryParse(row["tid"].ToString(), out tid);
                    if (!tids.Contains(tid))
                        tids.Add(tid);
                    else
                        continue;
                    bool create = false;
                    int totalnum = 0;
                    int usernum = 0;
                    HttpZGoloBusiness.GetTalkNum(tid, ref totalnum, ref usernum);

                    if (uid != 0)
                    {
                        if (uid == int.Parse(row["createuid"].ToString())) //判断uid是否是createuid
                        {
                            create = true;
                        }
                    }
                    SearchTalkListModel searchTalkList = new SearchTalkListModel
                    {
                        tid = tid,
                        auth = row["auth"].ToString(),
                        talkname = row["talkname"].ToString(),
                        remark = row["talknotice"].ToString(),
                        create = create,
                        usernum = usernum,
                        totalnum = totalnum,
                        imageurl =
                            row["imageurl"] != null
                                ? row["imageurl"].ToString()
                                : "",
                        type = row["type"].ToString(),
                        sn = string.Empty
                    };
                    searchTalkListModels.Add(searchTalkList);
                }
                //if (datatable.Rows.Count > 0)
                //{
                //    minitid = Int32.Parse(datatable.Rows[datatable.Rows.Count - 1]["id"].ToString());
                //}
                var jsonCommModel = new AppJsonResultModel<List<SearchTalkListModel>>((int)MessageCode.Success, MessageCodeDiscription.GetMessageCodeDiscription(MessageCode.Success), searchTalkListModels);
                var jsonstr = JsonConvert.SerializeObject(jsonCommModel);
                return jsonstr;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("获取频道列表" + e, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
    }
}
