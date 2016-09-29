﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Data;
using System.Web;
using System.Drawing;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Runtime.Serialization.Json;
using System.Collections;
using MongoDB.Driver;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace MediaService
{
    class CommBusiness
    {
        private static object symObj = new object();
        private static List<FujinTalk> FujinTalkList = new List<FujinTalk>();

        //聊天相关

        #region 设置用户当前对讲默认组
        public static string SetUserDuiJiangTalk(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                int tid = System.BitConverter.ToInt32(token.buffer, 8);
                PublicClass.SetDefaultDuiJiang(tid, token.uid);
                return "{\"status\":true}";
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 获取MIC、权限
        public static string GetMic(AsyncUserToken token, int packnum)
        {
            lock (symObj)
            {
                try
                {
                    if (token.uid != 0)
                    {
                        int tid = System.BitConverter.ToInt32(token.buffer, 8);
                        MediaService.WriteLog("1066-------------------tid=" + tid + "--------------------0", MediaService.wirtelog);
                        if (InitTalkMessage(tid) == false)
                        {
                            return "{\"status\":false,\"type\":3,\"code\":\"此组不存在\"}";
                        }
                        TalkMessage talkmessage = null;
                        MediaService.talkDic.TryGetValue(tid, out talkmessage);
                        bool useringroup = false;
                        foreach (int uid in talkmessage.uidlist)
                        {
                            if (uid == token.uid)
                            {
                                useringroup = true;
                                break;
                            }
                        }
                        if (useringroup == true)
                        {
                            long ticks = DateTime.Now.Ticks;
                            if (talkmessage.micuid == 0)
                            {
                                talkmessage.micuid = token.uid;
                                talkmessage.micticks = ticks;
                                MediaService.WriteLog("1066-------------------uid=" + token.uid + "--------------------1", MediaService.wirtelog);
                                return "{\"status\":true}";
                            }
                            else
                            {
                                if (ticks - talkmessage.micticks > 10000000)
                                {
                                    talkmessage.micuid = token.uid;
                                    talkmessage.micticks = ticks;
                                    MediaService.WriteLog("1066-------------------uid=" + token.uid + "--------------------2" + ticks + "  -------------------" + talkmessage.micticks, MediaService.wirtelog);
                                    return "{\"status\":true}";
                                }
                                else
                                {
                                    MediaService.WriteLog("1066------------------uid=" + token.uid + "---------------------3", MediaService.wirtelog);
                                    return "{\"status\":false,\"type\":1,\"code\":\"mic被占用\"}";
                                }
                            }
                        }
                        else
                        {
                            return "{\"status\":false,\"type\":3,\"code\":\"不在此组\"}";
                        }
                    }
                }
                catch (Exception err)
                {
                    return "{\"status\":false,\"type\":0,\"code\":\"" + err.Message + "\"}";
                }
                return "{\"status\":false,\"type\":0,\"code\":\"未登陆\"}";
            }
        }
        #endregion

        #region 获取用户所在的对讲组
        public static string GetDuiJiangTalk(AsyncUserToken token, int packnum)
        {
            if (token.uid != 0)
            {
                int tid = 0;
                string talkname = "";
                object obj = SqlHelper.ExecuteScalar("select tid from [wy_talkuser] where uid='" + token.uid + "' and duijiang=1");
                if (obj != null)
                {
                    tid = Int32.Parse(obj.ToString());
                    if (InitTalkMessage(tid) == false)
                    {
                        return "{\"status\":false,\"type\":3,\"code\":\"此组不存在\"}";
                    }
                    obj = SqlHelper.ExecuteScalar("select talkname from [wy_talk] where tid=" + tid);
                    talkname = obj.ToString();
                }
                return "{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\"}";
            }
            else
            {

                return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
        }
        #endregion

        #region 客服模块

        #region 发送服务通知
        public static string SendServiceNotice(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                int type = System.BitConverter.ToInt32(token.buffer, 8);
                int serivetype = 0;
                if (packnum > 12)
                    serivetype = System.BitConverter.ToInt32(token.buffer, 12);
                SqlHelper.ExecuteNonQuery("update [app_userserive] set state=4 where uid='" + token.uid + "' and (state=0 or state=1 or state=5)");
                if (type == 0)
                    SqlHelper.ExecuteNonQuery("insert [app_userserive] (uid,serivetype) values (" + token.uid + "," + serivetype + ")");
                return "{\"status\":true,\"type\":" + type + "}";
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 客服对讲状态
        public static string SendDuiJiangState(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                int uid = System.BitConverter.ToInt32(token.buffer, 8);
                MediaService.WriteLog("1069:" + token.uid + "发至" + uid, true);
                Buffer.BlockCopy(System.BitConverter.GetBytes(token.uid), 0, token.buffer, 8, 4);
                UserObject uo = null;
                if (MediaService.userDic.TryGetValue(uid, out uo))
                {
                    if (uo.socket[token.appid] != null)
                    {
                        try
                        {
                            uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                            //return "{\"status\":true}";
                        }
                        catch { }
                    }
                }
                //recv = WriteErrorJson(41);
            }
            return null;
        }
        #endregion

        #region 客服呼叫用户
        public static string SendKFCallUserState(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                int uid = System.BitConverter.ToInt32(token.buffer, 8);
                MediaService.WriteLog("1080:" + token.uid + "发至" + uid, true);
                Buffer.BlockCopy(System.BitConverter.GetBytes(token.uid), 0, token.buffer, 8, 4);
                UserObject uo = null;
                if (MediaService.userDic.TryGetValue(uid, out uo))
                {
                    try
                    {
                        uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                        //return "{\"status\":true}";
                    }
                    catch { }
                }
                //recv = WriteErrorJson(41);
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 用户发送语音消息
        public static string SendAudioMessage(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                int uid = System.BitConverter.ToInt32(token.buffer, 8);
                //MediaService.WriteLog("1002:" + token.uid + "发至" + uid, true);
                Buffer.BlockCopy(System.BitConverter.GetBytes(token.uid), 0, token.buffer, 8, 4);
                UserObject uo = null;
                if (MediaService.userDic.TryGetValue(uid, out uo))
                {
                    if (uo.socket[token.appid] != null)
                    {
                        try
                        {
                            uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                        }
                        catch
                        {
                            //recv = WriteErrorJson("发送失败，用户已离线！", 0);
                        }
                    }
                }
            }
            else
            {
                //recv = WriteErrorJson("您还没有登陆！", 0);
            }
            return recv;
        }
        #endregion

        #endregion

        #region 获取会话或群信息修改状态
        public static string GetTalkModiTime(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                MediaService.WriteLog("获取会话或群信息修改列表", MediaService.wirtelog);
                StringBuilder sb = new StringBuilder();
                for (int i = 8; i < packnum; i = i + 8)
                {
                    int tid = System.BitConverter.ToInt32(token.buffer, i);
                    int moditime = System.BitConverter.ToInt32(token.buffer, i + 4);
                    sb.Append(" or (tid=");
                    sb.Append(tid);
                    sb.Append(" and moditime>");
                    sb.Append(moditime);
                    sb.Append(")");
                }
                if (sb.Length > 0)
                {
                    sb.Remove(0, 3);
                    sb.Insert(0, "select tid,talkname,lo,la,createuid from [wy_talk] where");
                    DataTable dt = SqlHelper.ExecuteTable(sb.ToString());
                    if (dt.Rows.Count > 0)
                    {
                        TalkListBaseMessageJson talkListJson = new TalkListBaseMessageJson(true, dt);
                        DataContractJsonSerializer json = new DataContractJsonSerializer(talkListJson.GetType());
                        using (MemoryStream stream = new MemoryStream())
                        {
                            json.WriteObject(stream, talkListJson);
                            recv = Encoding.UTF8.GetString(stream.ToArray());
                        }
                    }
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 获取用户信息修改状态
        public static string GetUserModiTime(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                MediaService.WriteLog("获取用户信息修改列表", MediaService.wirtelog);
                StringBuilder sb = new StringBuilder();
                for (int i = 8; i < packnum; i = i + 8)
                {
                    int uid = System.BitConverter.ToInt32(token.buffer, 8);
                    int moditime = System.BitConverter.ToInt32(token.buffer, 12);
                    sb.Append(" or (uid=");
                    sb.Append(uid);
                    sb.Append(" and moditime>");
                    sb.Append(moditime);
                    sb.Append(")");
                }
                if (sb.Length > 0)
                {
                    sb.Remove(0, 3);
                    sb.Insert(0, "select uid,gender,username,nickname,roles,district_id,area_id from [app_users] where");
                    DataTable dt = SqlHelper.ExecuteTable(sb.ToString());
                    if (dt.Rows.Count > 0)
                    {
                        UserListBaseMessageJson userListJson = new UserListBaseMessageJson(true, dt); ;
                        DataContractJsonSerializer json = new DataContractJsonSerializer(userListJson.GetType());
                        using (MemoryStream stream = new MemoryStream())
                        {
                            json.WriteObject(stream, userListJson);
                            recv = Encoding.UTF8.GetString(stream.ToArray());
                        }
                    }
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 获取未读信息数目
        public static string GetNoReadMessageNum(AsyncUserToken token)
        {
            StringBuilder sb = new StringBuilder("{\"status\":true,\"userlist\":[");
            if (token.uid != 0)
            {
                try
                {
                    DataTable dt = SqlHelper.ExecuteTable("select senduid, COUNT(1) AS noread from [wy_usermessage] where recvuid=" + token.uid + " and state=0  GROUP BY senduid");
                    int i = 0;
                    for (i = 0; i < dt.Rows.Count; i++)
                    {
                        string uid = dt.Rows[i]["senduid"].ToString();
                        string noread = dt.Rows[i]["noread"].ToString();
                        if (i != 0)
                            sb.Append(',');
                        sb.Append("{\"uid\":" + uid + ",\"noread\":" + noread + "}");
                    }
                    sb.Append("],\"talklist\":[");
                    dt = SqlHelper.ExecuteTable("select tid, noread from [wy_talkuser] where uid = " + token.uid + " and noread!=0");
                    for (i = 0; i < dt.Rows.Count; i++)
                    {
                        string tid = dt.Rows[i]["tid"].ToString();
                        string noread = dt.Rows[i]["noread"].ToString();
                        if (i != 0)
                        {
                            sb.Append(',');
                        }
                        sb.Append("{\"tid\":" + tid + ",\"noread\":" + noread + "}");
                    }
                    sb.Append("]}");
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    return WriteErrorJson(6);
                }
            }
            else
            {
                return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return sb.ToString();
        }
        #endregion

        #region 获取私聊消息列表
        public static string GetUserMessageList(AsyncUserToken token)
        {
            StringBuilder sb = new StringBuilder();
            if (token.uid != 0)
            {
                try
                {
                    int minid = System.BitConverter.ToInt32(token.buffer, 8);
                    int uid = System.BitConverter.ToInt32(token.buffer, 12);
                    string sql = "";
                    if (minid == 0)
                    {
                        SqlHelper.ExecuteNonQuery("update [wy_usermessage] set state=1 where recvuid=" + token.uid + " and senduid=" + uid + " and state=0");
                        sql = "select top 20 id,message,sendtime from [wy_usermessage] where (recvuid =" + token.uid + " and senduid= " + uid + ") or (recvuid =" + uid + " and senduid= " + token.uid + ") order by id desc";
                    }
                    else
                    {
                        sql = "select top 20 id,message,sendtime from [wy_usermessage] where  ((recvuid =" + token.uid + " and senduid= " + uid + ") or (recvuid =" + uid + " and senduid= " + token.uid + ")) and id< " + minid + " order by id desc";
                    }
                    MediaService.WriteLog("test： " + sql, MediaService.wirtelog);
                    DataTable dt = SqlHelper.ExecuteTable(sql);
                    foreach (DataRow dr in dt.Rows)
                    {
                        string message = dr["message"].ToString();
                        string sendtime = dr["sendtime"].ToString();
                        if (message.Length > 1)
                        {
                            if (sb.Length > 0)
                                sb.Append("," + message);
                            else
                                sb.Append(message);
                        }
                    }
                    int nowminid = 0;
                    if (dt.Rows.Count > 0)
                        nowminid = Int32.Parse(dt.Rows[dt.Rows.Count - 1]["id"].ToString());
                    sb.Insert(0, "{\"status\":true,\"minid\":" + nowminid + ",\"list\":[");
                    sb.Append("]}");
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    sb.Clear();
                    sb.Append(WriteErrorJson(6));
                }
            }
            else
            {
                sb.Append(WriteErrorJson(3, "你还没有登陆，请稍后再试！"));
            }
            return sb.ToString();
        }
        #endregion

        #region 删除用户聊天消息
        public static string DeleteUserMessage(AsyncUserToken token, int packnum, string tablename)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    string sql = "";
                    for (int i = 8; i < packnum - 8; i = i + 4)
                    {
                        int mid = System.BitConverter.ToInt32(token.buffer, i);
                        sql += " or id=" + mid;
                    }
                    if (sql != "")
                    {
                        sql = sql.Remove(0, 3);
                        SqlHelper.ExecuteNonQuery("delete [" + tablename + "] where senduid=" + token.uid + " and (" + sql + ")");
                    }
                    recv = "{\"status\":true}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 用户发送私聊信息
        public static string SendUserMessage(AsyncUserToken token, int packnum)
        {
            string recv = null;
            try
            {
                if (token.uid != 0)
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("发送私聊信息：" + query, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    if (qs["recvuid"] != null && qs["sendcontent"] != null)
                    {
                        string fid = qs["fid"] == null ? "" : qs["fid"].ToString().Replace("'", "");
                        string fext = qs["fext"] == null ? "" : qs["fext"].ToString().Replace("'", "");
                        string fatr = qs["fatr"] == null ? "" : qs["fatr"].ToString().Replace("'", "");
                        string mtype = qs["mtype"] == null ? "0" : qs["mtype"].ToString().Replace("'", "");
                        int recvuid = Int32.Parse(qs["recvuid"].ToString());
                        string sendcontent = StringToJson(qs["sendcontent"].ToString().Replace("'", ""));

                        MediaService.WriteLog("命令：发送私聊信息到 " + recvuid, false);

                        object obj = SqlHelper.ExecuteScalar("select uid from [wy_userblack] where (uid=" + token.uid + " and buid=" + recvuid + ") or (uid=" + recvuid + " and buid=" + token.uid + ")");
                        if (obj == null)
                        {
                            long timeStamp = GetTimeStamp();

                            //返回至客户端
                            byte[] cbyte = Encoding.UTF8.GetBytes("{\"status\":true,\"timeStamp\":" + timeStamp + "}");
                            Buffer.BlockCopy(cbyte, 0, token.buffer, 8, cbyte.Length);
                            Buffer.BlockCopy(System.BitConverter.GetBytes((short)(cbyte.Length + 8)), 0, token.buffer, 0, 2);
                            token.Socket.Send(token.buffer, 0, cbyte.Length + 8, SocketFlags.None);

                            string message = "{\"fid\":\"" + fid + "\",\"fext\":\"" + fext + "\",\"fatr\":\"" + fatr + "\",\"sendcontent\":\"" + sendcontent + "\",\"senduid\":" + token.uid + ",\"recvuid\":" + recvuid + ",\"mtype\":" + mtype + ",\"sendtime\":" + timeStamp + "}";
                            bool state = PublicClass.SendToUser(token.buffer, message.Insert(1, "\"status\":true,"), token.nickname, recvuid, Int32.Parse(mtype), 0, CommType.recvUserMessage, token.appid);
                            SqlHelper.ExecuteNonQuery("insert [wy_usermessage] (message,senduid,recvuid,state) values ('" + message + "'," + token.uid + "," + recvuid + "," + (state ? "1" : "0") + ")");
                        }
                        else
                        {
                            recv = WriteErrorJson(20);
                        }
                    }
                    else
                    {
                        recv = WriteErrorJson(11);
                    }
                }
                else
                {
                    recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
                }

            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        //用户相关
        #region 上传用户的媒体播放记录
        public static string UploadUserMediaPlayLog(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                if (token.uid != 0)
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("请求媒体：" + query + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    if (qs != null && qs["mid"] != null && qs["type"] != null && qs["state"] != null)
                    {
                        string mid = qs["mid"].ToString().Replace("'", "");
                        if (mid != "0" || mid != "")
                        {
                            string type = qs["type"].ToString().Replace("'", "");
                            string state = qs["state"].ToString().Replace("'", "");
                            Meitiinfo meitinfo = new Meitiinfo(mid, token.uid, type, state, (DateTime.Now.Ticks - 621356256000000000) / 10000000);
                            MediaService.meitiinfo.Add(meitinfo);
                        }
                        return "{\"status\":true}";
                    }
                    else
                    {
                        return WriteErrorJson(11);
                    }
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 上传用户的新闻播放记录
        public static string UploadUserNewsPlayLog(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                if (token.uid != 0)
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("请求新闻：" + query, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    if (qs != null && qs["nid"] != null && qs["type"] != null && qs["state"] != null)
                    {
                        string nid = qs["nid"].ToString().Replace("'", "");
                        if (nid != "0" || nid != "")
                        {
                            string type = qs["type"].ToString().Replace("'", "");
                            string state = qs["state"].ToString().Replace("'", "");


                            Newsinfo newsinfo = new Newsinfo(nid, token.uid, type, state, (DateTime.Now.Ticks - 621356256000000000) / 10000000);
                            MediaService.newsinfo.Add(newsinfo);
                        }
                        return "{\"status\":true}";
                    }
                    else
                    {
                        return WriteErrorJson(11);
                    }
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 设置FM发射频率
        public static string SetGoloFmHz(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                int fm = System.BitConverter.ToInt32(token.buffer, 8);
                int num = SqlHelper.ExecuteNonQuery("update [app_users] set fm=" + fm + " where uid=" + token.uid);
                if (num > 0)
                {

                    recv = "{\"status\":true,\"fm\":" + fm + "}";
                }
                else
                {
                    recv = WriteErrorJson(6, "请稍后再试试！");
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 修改用户头像
        public static string ModiUserFace(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                string url = GetUserFaceUrl(token.uid);
                string dir = url.Remove(url.Length - 2);
                url += "_0.jpg";
                short ks = System.BitConverter.ToInt16(token.buffer, 8);
                FileStream fs = null;
                if (ks == 0 || ks == 3)
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    fs = File.Create(url);
                    try
                    {
                        File.Delete(url.Replace("_0", "_1"));
                        File.Delete(url.Replace("_0", "_2"));
                    }
                    catch { }
                }
                else
                {
                    fs = new FileStream(url, FileMode.Append);
                }
                fs.Write(token.buffer, 10, packnum - 10);
                fs.Close();
                if (ks == 3 || ks == 2)
                {
                    MakeThumbnail(url, url, 200, 200, 2, "jpg");
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 注册用户
        public static string RegistUser(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("请求：" + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);

                string app_key = qs["__API__[app_key]"] == null ? "" : qs["__API__[app_key]"].ToString().Replace('\'', ' ');
                string app_secret = qs["__API__[app_secret]"] == null ? "" : qs["__API__[app_secret]"].ToString().Replace('\'', ' ');
                DataRow[] dr = MediaService.allapp.Select("app_key='" + app_key + "' and app_secret='" + app_secret + "'");
                if (dr.Length > 0)
                {

                    if (qs["password"] != null && qs["mobile"] != null && qs["verify_code"] != null)
                    {
                        string mobile = qs["mobile"] == null ? "" : qs["mobile"].ToString();
                        string password = qs["password"] == null ? "" : qs["password"].ToString().ToLower().Replace("'", "");
                        string verify_code = qs["verify_code"] == null ? "" : qs["verify_code"].ToString().Replace("'", "");
                        string nick_name = qs["nick_name"] == null ? "" : qs["nick_name"].ToString().Replace("'", "");
                        string str = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=passport_service.register&app_id=2014042900000006&ver=3.0.16", "nation_id=143&loginKey=" + mobile + "&verify_code=" + verify_code + "&password=" + password + "&app_id=2014042900000006&nick_name=" + nick_name, "POST", Encoding.UTF8);

                        MediaService.WriteLog("返回：" + str, MediaService.wirtelog);
                        int s = str.IndexOf("{\"code\":0");
                        if (s >= 0)
                        {
                            recv = "{\"status\":true}";
                        }
                        else if (str.IndexOf("verify") > 0)
                        {
                            recv = WriteErrorJson(7);
                        }
                        else
                        {
                            recv = WriteErrorJson(14);
                        }
                    }
                    else
                    {
                        recv = WriteErrorJson(13);
                    }
                }
                else
                {
                    recv = WriteErrorJson(15);
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 修改用户信息
        public static string ModiUserMessage(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                if (token.uid > 0)
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("请求：" + query, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    StringBuilder sql = new StringBuilder();
                    if (qs["newpassword"] != null)
                    {
                        string newpassword = CommBusiness.StringToMD5Hash(qs["newpassword"].ToString().Replace("'", ""));
                        string md5password = CommBusiness.StringToMD5Hash(qs["password"].ToString().Replace("'", ""));
                        object obj = SqlHelper.ExecuteScalar("select uid from[app_users] where password='" + md5password + "' and uid=" + token.uid);
                        if (obj != null)
                        {
                            if (newpassword.Length == 32)
                            {
                                sql.Append(",password='" + newpassword + "'");
                            }
                            else
                            {
                                return WriteErrorJson(16);
                            }
                        }
                        else
                        {
                            return WriteErrorJson(40);
                        }
                    }
                    if (qs["nickname"] != null)
                    {
                        string nickname = qs["nickname"].ToString().Replace("'", "");
                        if (IsValiNumCnEn(nickname) == false) return WriteErrorJson(38);
                        sql.Append(",nickname='" + qs["nickname"].ToString().Replace("'", "") + "'");
                        sql.Append(",moditime='" + GetTimeStamp() + "'");
                    }
                    if (qs["email"] != null)
                    {
                        sql.Append(",email='" + qs["email"].ToString().ToLower().Replace("'", "") + "'");
                    }
                    if (qs["gender"] != null)
                    {
                        sql.Append(",gender='" + Int32.Parse(qs["gender"].ToString()) + "'");
                    }
                    if (qs["mobile"] != null)
                    {
                        string mobile = qs["mobile"].ToString().Replace("'", "");
                        sql.Append(",mobile='" + qs["mobile"].ToString() + "'");
                    }
                    if (qs["username"] != null && qs["username"].ToString() != "")
                    {
                        string username = qs["username"].ToString().Replace("'", "");
                        if (username.Length < 20 && username.Length > 6)
                        {
                            if (IsValiNumCnEn(username) == false) return WriteErrorJson(18);
                            object obj = SqlHelper.ExecuteScalar("select uid from[app_users] where username='" + username + "' and uid!=" + token.uid);
                            if (obj == null)
                            {
                                sql.Append(",username='" + username + "'");
                            }
                            else
                            {
                                return WriteErrorJson(17);
                            }
                        }
                        else
                        {
                            return WriteErrorJson(18);
                        }
                    }
                    if (sql.Length > 0)
                    {
                        SqlHelper.ExecuteNonQuery("update [app_users] set " + sql.Remove(0, 1).ToString() + " where uid=" + token.uid);
                    }
                    DataTable dt = SqlHelper.ExecuteTable("select glsn,username,nickname,mobile from[app_users] where uid=" + token.uid);
                    if (dt.Rows.Count > 0)
                    {
                        recv = "{\"status\":true,\"glsn\":\"" + dt.Rows[0]["glsn"].ToString() + "\",\"username\":\"" + dt.Rows[0]["username"].ToString() + "\",\"nickname\":\"" + dt.Rows[0]["nickname"].ToString() + "\",\"mobile\":\"" + dt.Rows[0]["mobile"].ToString() + "\"}";
                    }
                    else
                    {
                        return WriteErrorJson(20);
                    }
                }
                else
                {
                    return WriteErrorJson(11);
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 修改用户车辆信息
        public static string ModiUserCarMessage(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid > 0)
            {
                try
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("请求：" + query, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    if (qs != null && qs["car_plate"] != null)
                    {
                        string car_plate = qs["car_plate"].ToString().Replace("'", "");
                        object obj = SqlHelper.ExecuteScalar("select uid from [app_userscar] where uid=" + token.uid);
                        if (obj == null)
                        {
                            SqlHelper.ExecuteNonQuery("insert [app_userscar] (uid,car_plate) values ('" + token.uid + "','" + car_plate + "')");
                        }
                        else
                        {
                            SqlHelper.ExecuteNonQuery("update [app_userscar] set car_plate='" + car_plate + "' where uid=" + token.uid);
                        }
                        recv = "{\"status\":true}";
                    }
                    else
                    {
                        return WriteErrorJson(11);
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 获取用户车辆信息
        public static string GetUserCarMessage(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid > 0)
            {
                try
                {
                    object obj = SqlHelper.ExecuteScalar("select car_plate from [app_userscar] where uid=" + token.uid);
                    if (obj != null)
                    {
                        recv = "{\"status\":true,\"car_plate\":\"" + obj.ToString() + "\"}";
                    }
                    else
                    {
                        recv = "{\"status\":true,\"car_plate\":\"\"}";
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 获取黑名单列表
        public static string GetBlackList(AsyncUserToken token)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    string sql = "";
                    DataTable dt = SqlHelper.ExecuteTable("select buid from [wy_userblack] where uid=" + token.uid);
                    foreach (DataRow dr in dt.Rows)
                    {
                        sql += " or uid=" + dr["buid"].ToString();
                    }
                    if (sql.Length > 0)
                    {
                        dt = SqlHelper.ExecuteTable("select uid,username from [app_users] where " + sql.Remove(0, 4));
                        foreach (DataRow dr in dt.Rows)
                        {
                            recv += (recv == "" ? "" : ",") + "{\"uid\":" + dr["uid"].ToString() + ",\"username\":\"" + HttpUtility.UrlEncode(dr["uid"].ToString().Trim()) + "\"}";
                        }
                    }
                    recv = "{\"status\":true,\"list\":[" + recv + "]}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 取消黑名单
        public static string DeleBlackUser(AsyncUserToken token)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    int uid = System.BitConverter.ToInt32(token.buffer, 8);
                    SqlHelper.ExecuteNonQuery("delete [wy_userblack] where uid=" + token.uid + " and buid=" + uid);
                    recv = "{\"status\":true}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 设黑名单
        public static string SetBlackUser(AsyncUserToken token)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    int uid = System.BitConverter.ToInt32(token.buffer, 8);
                    object obj = SqlHelper.ExecuteScalar("select id from [wy_userblack] where uid=" + token.uid + " and buid=" + uid);
                    if (obj == null)
                    {
                        SqlHelper.ExecuteNonQuery("insert [wy_userblack] (uid,buid) values (" + token.uid + "," + uid + ")");
                    }
                    recv = "{\"status\":true}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 删除好友
        public static string DeleMyFriend(AsyncUserToken token)
        {
            string recv;
            try
            {
                int uid = BitConverter.ToInt32(token.buffer, 8);
                int ouid;
                MediaService.mapDic.TryGetValue(token.uid, out ouid);
                SqlHelper.ExecuteScalar("delete [wy_userrelation] where (uid=" + token.uid + " or ouid=" + ouid + ") and fuid=" + uid);
                recv = "{\"status\":true}";
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 加好友
        public static string AddMyFriend(AsyncUserToken token)
        {
            string recv = "";
            try
            {
                int uid = System.BitConverter.ToInt32(token.buffer, 8);
                bool result = PublicClass.AddMyFriend(token.uid, uid, "");
                if (result)
                {
                    recv = "{\"status\":true}";
                }
                else
                {
                    recv = WriteErrorJson(9);
                }
                //object obj = SqlHelper.ExecuteScalar("select id from [wy_userrelation] where uid=" + token.uid + " and fuid=" + uid);
                //if (obj == null)
                //{
                //    SqlHelper.ExecuteNonQuery("insert [wy_userrelation] (uid,fuid) values (" + token.uid + "," + uid + ")");
                //    recv = "{\"status\":true}";
                //}
                //else
                //{
                //    recv = WriteErrorJson(9);
                //}
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 获取我的好友
        public static string GetMyFriend(AsyncUserToken token)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    int maxid = BitConverter.ToInt32(token.buffer, 8);
                    int ouid;
                    MediaService.mapDic.TryGetValue(token.uid, out ouid);
                    DataTable dt = SqlHelper.ExecuteTable("select top 20 id,fuid from [wy_userrelation] where (uid=" + token.uid + " OR ouid=" + ouid + ") and id>" + maxid + " order by id");
                    foreach (DataRow dr in dt.Rows)
                    {
                        string id = dr["id"].ToString();
                        string fuid = dr["fuid"].ToString();
                        recv += (recv == "" ? "" : ",") + "{\"id\":" + id + ",\"fuid\":" + fuid + "}";
                    }
                    recv = "{\"status\":true,\"list\":[" + recv + "]}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err, MediaService.wirtelog); recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 获取用户信息修改
        public static string GetUserModi(AsyncUserToken token)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    long time = System.BitConverter.ToInt64(token.buffer, 8);
                    if (time != 0)
                    {
                        time = time * 10000 + 621356256000000000;
                        DateTime dtime = new DateTime(time);
                        DataTable dt = SqlHelper.ExecuteTable("select uid,username,face from [app_usermodi] where moditime>'" + dtime.ToString() + "' order by id");
                        foreach (DataRow dr in dt.Rows)
                        {
                            int uid = Int32.Parse(dr["uid"].ToString());
                            string username = dr["username"].ToString().Trim();
                            string avatar = dr["face"].ToString();
                            if (username != "") username = HttpUtility.UrlEncode(username);
                            if (avatar == "1")
                                avatar = GetUserFaceUrl(uid);
                            else
                                avatar = "";
                            recv += (recv == "" ? "" : ",") + "{\"uid\":" + uid + ",\"username\":\"" + username + "\",\"avatar\":\"" + avatar + "\"}";
                        }
                    }
                    recv = "{\"status\":true,\"list\":[" + recv + "]}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 获取用户状态
        public static string GetUserState(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    UserObject uo = null;
                    for (int k = 8; k < packnum; k = k + 4)
                    {
                        int uid = System.BitConverter.ToInt32(token.buffer, k);
                        string online = "";
                        if (MediaService.userDic.TryGetValue(uid, out uo))
                        {
                            for (int i = 0; i < MediaService.maxappid; i++)
                            {
                                if (uo.socket[i] != null)
                                {
                                    online += (online == "" ? "" : ",") + "{\"appid\":" + i + "}";
                                }
                            }
                        }
                        recv += (recv == "" ? "" : ",") + "{\"uid\":" + uid + ",\"online\":[" + online + "]}";
                    }
                    recv = "{\"status\":true,\"list\":[" + recv + "]}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 搜索用户公开信息
        public static string SearchUserPublic(AsyncUserToken token, int packnum)
        {
            string recv = "";
            string content = "";
            DataTable dt = new DataTable();
            int uid = System.BitConverter.ToInt32(token.buffer, 8);
            if (uid > 0)
            {
                dt = SqlHelper.ExecuteTable("select uid,gender,username,nickname,roles,district_id,area_id from [app_users] where uid='" + uid + "'");
            }
            else
            {
                content = Encoding.UTF8.GetString(token.buffer, 12, packnum - 12).Replace("'", "");
                if (content != "")
                {
                    if (content[0] > 47 && content[0] < 58)
                    {
                        dt = SqlHelper.ExecuteTable("select uid,gender,username,nickname,roles,district_id,area_id from [app_users] where mobile='" + content + "'");
                    }
                    else
                    {
                        dt = SqlHelper.ExecuteTable("select uid,gender,username,nickname,roles,district_id,area_id from [app_users] where username='" + content + "'");
                    }
                }
            }
            if (dt.Rows.Count > 0)
            {
                UserBaseMessage userLoginJson = new UserBaseMessage(Int32.Parse(dt.Rows[0]["uid"].ToString()), dt.Rows[0]["username"].ToString().Trim(), Int32.Parse(dt.Rows[0]["gender"].ToString()), dt.Rows[0]["nickname"].ToString().Trim(), Int32.Parse(dt.Rows[0]["roles"].ToString()), Int32.Parse(dt.Rows[0]["district_id"].ToString()), Int32.Parse(dt.Rows[0]["area_id"].ToString()));
                DataContractJsonSerializer json = new DataContractJsonSerializer(userLoginJson.GetType());
                using (MemoryStream stream = new MemoryStream())
                {
                    json.WriteObject(stream, userLoginJson);
                    recv = Encoding.UTF8.GetString(stream.ToArray());
                    recv = recv.Insert(1, "\"status\":true,");
                }
            }
            else
            {
                recv = WriteErrorJson(20);
            }
            return recv;
        }
        #endregion

        #region 获取转一转用户列表
        public static string GetTurnUser(AsyncUserToken token)
        {
            StringBuilder sb = new StringBuilder();
            if (token.uid != 0)
            {
                long time = GetTimeStamp();
                SqlHelper.ExecuteNonQuery("update [app_users] set turntime=" + time + " where uid=" + token.uid);
                DataTable dt = SqlHelper.ExecuteTable("select top 50 uid from [app_users] where uid!=" + token.uid + " and turntime>" + (time - MediaService.turntime * 60) + " order by turntime desc");
                foreach (DataRow dr in dt.Rows)
                {
                    string uid = dr["uid"].ToString();
                    sb.Append(",{\"uid\":" + uid + "}");
                }
                if (sb.Length > 0)
                    sb.Remove(0, 1);
                sb.Insert(0, "{\"status\":true,\"list\":[");
                sb.Append("]}");
            }
            else
            {
                return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return sb.ToString();
        }
        #endregion

        #region 获取用户列表公开信息
        public static string GetUserListPublic(AsyncUserToken token, int packnum)
        {
            string recv = "";
            StringBuilder sb = new StringBuilder();
            for (int i = 8; i < packnum; i = i + 4)
            {
                int uid = System.BitConverter.ToInt32(token.buffer, i);
                if (i == 8) sb.Append(" uid=" + uid);
                else sb.Append(" or uid=" + uid);
            }
            if (sb.Length > 0)
            {
                DataTable dt = SqlHelper.ExecuteTable("select uid,gender,username,nickname,roles,district_id,area_id from [app_users] where " + sb.ToString());
                if (dt.Rows.Count > 0)
                {
                    UserListBaseMessageJson UserListJson = new UserListBaseMessageJson(true, dt);
                    DataContractJsonSerializer json = new DataContractJsonSerializer(UserListJson.GetType());
                    using (MemoryStream stream = new MemoryStream())
                    {
                        json.WriteObject(stream, UserListJson);
                        recv = Encoding.UTF8.GetString(stream.ToArray());
                    }
                }
                else
                {
                    recv = WriteErrorJson(20);
                }
            }
            else
            {
                recv = WriteErrorJson(11);
            }
            return recv;
        }
        #endregion

        //车队模块

        #region 附近组切换至对讲组
        public static string FujinToTalk(AsyncUserToken token, int packnum)
        {
            if (token.uid != 0)
            {
                int ctid = 0;
                string talkname = "";
                int ftid = System.BitConverter.ToInt32(token.buffer, 8);
                //获取默认车队
                object obj = SqlHelper.ExecuteScalar("select tid from [wy_talkuser] where uid='" + token.uid + "' and duijiang=1");
                if (obj != null)
                {
                    ctid = Int32.Parse(obj.ToString());
                    if (InitTalkMessage(ctid) == true)
                    {
                        TalkMessage talkmessage = null;
                        MediaService.talkDic.TryGetValue(ctid, out talkmessage);
                        if (talkmessage.uidlist.Contains(token.uid) == false)
                            talkmessage.uidlist.Add(token.uid);
                        talkname = talkmessage.talkname;
                    }
                    else
                    {
                        ctid = 0;
                    }
                }
                //清除附近对讲组用户
                if (ftid != 0)
                {
                    ExitFujinTalk(token.uid, ftid);
                }
                return "{\"status\":true,\"tid\":" + ctid + ",\"talkname\":\"" + talkname + "\"}";
            }
            else
            {
                return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
        }
        #endregion

        #region 对讲组切换至附近组
        public static string TalkToFujin(AsyncUserToken token, int packnum)
        {
            if (token.uid != 0)
            {
                int ftid = 0;
                int ctid = System.BitConverter.ToInt32(token.buffer, 8);
                double lo = System.BitConverter.ToDouble(token.buffer, 12);
                double la = System.BitConverter.ToDouble(token.buffer, 20);
                int cityid = 0;
                if (packnum > 28)
                    cityid = System.BitConverter.ToInt32(token.buffer, 28);
                ftid = JoinFujinTalk(token.uid, lo, la);

                if (ctid == 0) //获取默认车队
                {
                    object obj = SqlHelper.ExecuteScalar("select tid from [wy_talkuser] where uid='" + token.uid + "' and duijiang=1");
                    if (obj != null)
                    {
                        ctid = Int32.Parse(obj.ToString());
                    }
                }
                if (ctid != 0)
                {
                    TalkMessage talkmessage = null;
                    if (MediaService.talkDic.TryGetValue(ctid, out talkmessage))
                    {
                        talkmessage.uidlist.Remove(token.uid);
                    }
                }
                return "{\"status\":true,\"tid\":" + ftid + ",\"talkname\":\"\"}";
            }
            else
            {
                return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
        }
        #endregion

        #region 对讲组切换至新对讲组
        public static string TalkToTalk(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                string talkname = "";
                string zsn = "";
                int type = System.BitConverter.ToInt32(token.buffer, 4);
                int ftid = System.BitConverter.ToInt32(token.buffer, 8);
                int ntid = System.BitConverter.ToInt32(token.buffer, 12);
                MediaService.WriteLog("1072 对讲组切换至新对讲组 ntid=" + ntid + "&ftid=" + ftid + "&type=" + type + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                if (ntid != 0)
                {
                    object obj = SqlHelper.ExecuteScalar("select tid from [wy_talkuser] where uid=" + token.uid + " and duijiang=1");
                    if (obj != null)
                    {
                        int nowtid = Int32.Parse(obj.ToString());
                        SqlHelper.ExecuteNonQuery("update [wy_talkuser] set duijiang=0 where uid=" + token.uid + " and duijiang=1");
                        TalkMessage talkmessage = null;
                        if (MediaService.talkDic.TryGetValue(nowtid, out talkmessage))
                        {
                            talkmessage.uidlist.Remove(token.uid);
                        }
                    }
                    int i= SqlHelper.ExecuteNonQuery("update [wy_talkuser] set duijiang=1 where uid=" + token.uid + " and tid=" + ntid);
                    if (i == 0)
                    {
                        SqlHelper.ExecuteNonQuery(string.Format("update [wy_talkuser] set duijiang=1 where uid=(select ouid from wy_uidmap t where t.uid={0}) and tid={1}", token.uid, ntid));
                    }
                    if (InitTalkMessage(ntid) == true)
                    {
                        TalkMessage talkmessage = null;
                        if (MediaService.talkDic.TryGetValue(ntid, out talkmessage))
                        {
                            if (talkmessage.uidlist.Contains(token.uid) == false)
                                talkmessage.uidlist.Add(token.uid);
                            talkname = talkmessage.talkname;
                            zsn = talkmessage.zsn;
                        }
                    }
                    else
                    {
                        ntid = 0;
                    }
                }
                if (ftid == 0 && ntid == 0)
                {
                    object obj = SqlHelper.ExecuteScalar("select tid from [wy_talkuser] where uid=" + token.uid + " and duijiang=1");
                    if (obj != null)
                    {
                        ftid = Int32.Parse(obj.ToString());
                    }
                }
                if (ftid != 0)
                {
                    SqlHelper.ExecuteNonQuery("update [wy_talkuser] set duijiang=0 where uid=" + token.uid + " and tid=" + ftid);
                    TalkMessage talkmessage = null;
                    if (MediaService.talkDic.TryGetValue(ftid, out talkmessage))
                    {
                        talkmessage.uidlist.Remove(token.uid);
                    }
                }

                if (ntid > 0)
                {
                    SendToTalkUser(token, ntid);
                }
                if (ftid > 0)
                {
                    SendToTalkUser(token, ftid);
                }
                return "{\"status\":true,\"tid\":" + ntid + ",\"talkname\":\"" + talkname + "\",\"zsn\":\"" + zsn + "\"}";
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }

        /// <summary>
        /// 推送信息到组里的在线用户
        /// </summary>
        /// <param name="token"></param>
        /// <param name="tid">组id</param>
        private static void SendToTalkUser(AsyncUserToken token, int tid)
        {
            try
            {
                string query = "tid=" + tid;
                byte[] src = Encoding.UTF8.GetBytes(query);
                Buffer.BlockCopy(src, 0, token.buffer, 8, src.Length);
                string talkInfo = GetUsernumInTalk(token, src.Length + 8);
                MediaService.WriteLog("命令：1119， 服务器返回：" + talkInfo, MediaService.wirtelog);
                if (GetJsonValue(talkInfo, "status", ",", false) == "true")
                {
                    TalkMessage talkMessage;
                    if (MediaService.talkDic.TryGetValue(tid, out talkMessage))
                    {
                        if (talkMessage.uidlist != null && talkMessage.uidlist.Count > 0)
                        {
                            PublicClass.SendToUserList(null, talkInfo, "", talkMessage.uidlist, 99, 0, CommType.getUsernumInTalk,
                                token.appid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MediaService.WriteLog("SendToTalkUser执行出错：" + ex.Message, MediaService.wirtelog);
            }
        }

        /// <summary>
        /// 获取频道人数
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        internal static string GetUsernumInTalk(AsyncUserToken token, int packnum)
        {
            string recv;
            if (token.uid != 0)
            {
                try
                {
                    //tid=10024
                    StringBuilder sb = new StringBuilder();
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8).Replace("'", "");
                    MediaService.WriteLog("1119  获取频道人数信息 " + query + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    int tid;
                    if (qs["tid"] != null && int.TryParse(qs["tid"], out tid))
                    {
                        string sql = "select talkname,xuhao,duijiang,auth,remark,createuid,imageurl from wy_talk,wy_talkuser where wy_talk.tid = " + tid + " and wy_talk.tid = wy_talkuser.tid and wy_talk.createuid = wy_talkuser.uid ";
                        DataTable dt = SqlHelper.ExecuteTable(sql);
                        if (dt == null || dt.Rows.Count < 1)
                        {
                            recv = WriteErrorJson(4, "未找到该频道的信息！");
                        }
                        else
                        {
                            string talkname = dt.Rows[0]["talkname"] == null ? "" : dt.Rows[0]["talkname"].ToString();
                            string auth = dt.Rows[0]["auth"] == null ? "" : dt.Rows[0]["auth"].ToString();
                            string remark = dt.Rows[0]["remark"] == null ? "" : dt.Rows[0]["remark"].ToString();
                            string create = "false";
                            if (dt.Rows[0]["createuid"].ToString().Equals(token.uid.ToString()))
                            {
                                create = "true";
                            }
                            string imageurl = dt.Rows[0]["imageurl"] == null ? "" : dt.Rows[0]["imageurl"].ToString();
                            if (imageurl.Equals("<null>"))
                                imageurl = "";
                            int totalnum = 0;
                            int usernum = 0;
                            GetTalkNum(tid, token.appid, ref totalnum, ref usernum);
                            sb.Append("{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\",\"remark\":\"" + remark + "\",\"create\":" + create + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + ",\"imageurl\":\"" + imageurl + "\"}");
                            recv = sb.ToString();
                        }

                    }
                    else
                    {
                        recv = WriteErrorJson(4, "传入的参数有误！");
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err, MediaService.wirtelog);
                    return WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }

        #endregion

        #region 用户发送语音消息到附近组
        public static string SendAudioToFujin(AsyncUserToken token, int packnum)
        {
            StringBuilder sb = new StringBuilder("1073---------------------------------------" + token.uid + "   " + packnum);
            string recv = null;
            if (token.uid != 0)
            {
                Buffer.BlockCopy(System.BitConverter.GetBytes(token.uid), 0, token.buffer, 4, 4);
                int tid = System.BitConverter.ToInt32(token.buffer, 8) - 1;
                MediaService.talkinfo.Add(new Talkinfo(0, token.uid, GetTimeStamp()));
                if (FujinTalkList.Count > tid && FujinTalkList[tid] != null)
                {
                    Task parent = new Task(() =>
                    {
                        sb.Append("-uidcount:" + FujinTalkList[tid].uidlist.Count);
                        bool state = false;
                        foreach (int uid in FujinTalkList[tid].uidlist)
                        {
                            UserObject uo = null;
                            if (uid != token.uid)
                            {
                                if (MediaService.userDic.TryGetValue(uid, out uo))
                                {
                                    if (uo.socket != null && uo.socket[token.appid] != null)
                                    {
                                        if (uo.socket[token.appid].Connected)
                                        {
                                            new Task(() =>
                                            {
                                                try
                                                {
                                                    uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                                                }
                                                catch (Exception err)
                                                {
                                                    MediaService.WriteLog("语音发送到附近异常：uid=" + uid + "     " + err.Message, MediaService.wirtelog);
                                                }
                                            }, TaskCreationOptions.AttachedToParent).Start();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                state = true;
                            }
                        }
                        if (state == false)
                        {
                            try
                            {
                                byte[] b = new byte[13];
                                Buffer.BlockCopy(System.BitConverter.GetBytes((short)13), 0, b, 0, 2);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.sendAudioToFujin), 0, b, 2, 2);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                                token.Socket.Send(b, SocketFlags.None);
                                MediaService.WriteLog("语音发送到附近组，用户不在此组：uid=" + token.uid, MediaService.wirtelog);
                            }
                            catch { }
                        }
                    });
                    parent.Start();
                    parent.Wait();

                    MediaService.WriteLog(sb.ToString(), MediaService.wirtelog);
                    //taskFactory.ContinueWhenAll()
                }
            }
            return recv;
        }
        #endregion

        #region 用户发送语音消息到车队
        public static string SendAudioToTalk(AsyncUserToken token, int packnum)
        {
            StringBuilder sb = new StringBuilder("1067---------------------------------------" + token.uid + "   " + packnum);
            MediaService.WriteLog(sb + "---start", MediaService.wirtelog);
            string recv = null;
            if (token.uid != 0)
            {
                Buffer.BlockCopy(System.BitConverter.GetBytes(token.glsn), 0, token.buffer, 4, 4);
                int tid = System.BitConverter.ToInt32(token.buffer, 8);
                sb.Append("---tid:" + tid);
                MediaService.talkinfo.Add(new Talkinfo(tid, token.uid, GetTimeStamp()));
                if (InitTalkMessage(tid) == false)
                {
                    try
                    {
                        byte[] b = new byte[12];
                        Buffer.BlockCopy(System.BitConverter.GetBytes((short)12), 0, b, 0, 2);
                        Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                        Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                        Buffer.BlockCopy(System.BitConverter.GetBytes(tid), 0, b, 8, 4);
                        token.Socket.Send(b, SocketFlags.None);
                        MediaService.WriteLog("语音发送到对讲组，用户不在此组：uid=" + token.uid, MediaService.wirtelog);
                    }
                    catch { }
                    return null;
                }
                TalkMessage talkmessage = null;
                if (MediaService.talkDic.TryGetValue(tid, out talkmessage))
                {
                    talkmessage.micticks = DateTime.Now.Ticks;
                    int messageId = 0;
                    int ouid = 0;
                    try
                    {
                        MediaService.mapDic.TryGetValue(token.uid, out ouid);
                        string json = string.Format("{{\"sn\":{0},\"ouid\":\"{1}\"}}", token.glsn, ouid);

                        MessageInfo msg = null;
                        byte packId = token.buffer[12];
                        sb.Append(" packId=" + packId + "  生成json：" + json);
                        var msgs = QueryMessageInfo(tid, packId, token.uid, DateTime.Now);

                        if (msgs != null && msgs.Any())
                        {
                            int maxTime = msgs.Max(m => m.Time);
                            msg = msgs.LastOrDefault(m => m.Time == maxTime);
                        }

                        if (msg == null || msg.Time == 0)
                        {
                            int time = int.Parse(DateTime.Now.ToString("HHmmssfff"));
                            msg = new MessageInfo
                            {
                                Time = time,
                                Tid = tid,
                                Senduid = token.uid,
                                Message = json,
                                PackId = packId
                            };
                            MongoCollection col = MediaService.mongoDataBase.GetCollection("MessageInfo_" + DateTime.Now.ToString("yyyyMMdd"));
                            col.Insert(msg);
                            InMemoryCache.Instance.Add(msg.Time.ToString(), msg, DateTime.Now.AddMinutes(2));
                        }

                        messageId = msg.Time;
                        sb.Append(" Mongo保存成功，msg.Time=" + msg.Time);
                    }
                    catch (Exception ex)
                    {
                        MediaService.WriteLog("Mongo记录聊天消息出错：" + ex.Message, MediaService.wirtelog);
                        return null;
                    }

                    byte[] bf = new byte[packnum];
                    try
                    {
                        Buffer.BlockCopy(token.buffer, 0, bf, 0, packnum);
                        Buffer.BlockCopy(BitConverter.GetBytes(CommType.voiceIntercom), 0, bf, 2, 2);
                        //messageId覆盖tid（语音数据前4byte），数据预留区中增加ouid
                        Buffer.BlockCopy(BitConverter.GetBytes(messageId), 0, bf, 8, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(ouid), 0, bf, 13, 4);

                        //测试通话质量
                        //byte[] bf1 = new byte[packnum];
                        //Buffer.BlockCopy(token.buffer, 0, bf1, 0, packnum);
                        //new Task(() =>
                        //    {
                        //        string datetime = DateTime.Now.ToString("yyyyMMddHHmmssffff");
                        //        WriteTalk(bf1, 13, bf1.Length - 13, tid, token.glsn, datetime + "_1067_1067");
                        //        WriteTalk(bf, 17, bf.Length - 17, tid, token.glsn, datetime + "_1067_1074");
                        //    }).Start();
                    }
                    catch (Exception ex)
                    {
                        MediaService.WriteLog("生成buffer出错：" + ex.Message, MediaService.wirtelog);
                    }
                    Task parent = new Task(() =>
                    {
                        sb.Append("-uidcount:" + talkmessage.uidlist.Count);
                        bool state = false;
                        foreach (int uid in talkmessage.uidlist)
                        {
                            UserObject uo = null;
                            if (uid != token.uid)
                            {
                                if (MediaService.userDic.TryGetValue(uid, out uo))
                                {
                                    if (uo.socket != null && uo.socket[token.appid] != null)
                                    {
                                        if (uo.socket[token.appid].Connected)
                                        {
                                            int desUid = uid;
                                            new Task(() =>
                                            {
                                                try
                                                {
                                                    if (uo.ver < 100)
                                                    {
                                                        uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                                                    }
                                                    else
                                                    {
                                                        uo.socket[token.appid].Send(bf, 0, packnum, SocketFlags.None);
                                                    }
                                                    sb.Append(" -uid=" + desUid);
                                                }
                                                catch (Exception err)
                                                {
                                                    MediaService.WriteLog("语音发送异常：uid=" + desUid + "     " + err.Message, MediaService.wirtelog);
                                                    //try
                                                    //{
                                                    //    userObject.socket[token.appid].Shutdown(SocketShutdown.Both);
                                                    //}
                                                    //catch { }
                                                    //userObject.socket[token.appid].Close();
                                                    //userObject.socket[token.appid] = null;
                                                }
                                            }, TaskCreationOptions.AttachedToParent).Start();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                state = true;
                            }
                        }
                        if (state == false)
                        {
                            try
                            {
                                byte[] b = new byte[12];
                                Buffer.BlockCopy(System.BitConverter.GetBytes((short)12), 0, b, 0, 2);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(tid), 0, b, 8, 4);
                                //byte[] b = new byte[13];
                                //Buffer.BlockCopy(System.BitConverter.GetBytes((short)13), 0, b, 0, 2);
                                //Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.sendAudioToTalk), 0, b, 2, 2);
                                //Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                                token.Socket.Send(b, SocketFlags.None);
                                MediaService.WriteLog("语音发送到对讲组，用户不在此组：uid=" + token.uid, MediaService.wirtelog);
                            }
                            catch { }
                        }
                    });
                    parent.Start();
                    parent.Wait();
                    MediaService.WriteLog(sb.ToString(), MediaService.wirtelog);
                    //taskFactory.ContinueWhenAll()
                }
            }
            return recv;
        }

        private static void WriteTalk(byte[] buffer, int start, int lenght, int tid, int glsn, string datetime)
        {
            try
            {
                string file = MediaService.fileurl + "talk" + '/' + tid.ToString() + '/' + glsn.ToString() + '/' + datetime + ".mp3";
                string filedir = file.Substring(0, file.LastIndexOf('/'));
                if (!Directory.Exists(filedir))
                {
                    Directory.CreateDirectory(filedir);
                }
                FileStream fs = File.Open(file, FileMode.Append);
                fs.Write(buffer, start, lenght);
                fs.Close();
            }
            catch
            { }
        }

        /// <summary>
        /// 查询聊天消息
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="packId"></param>
        /// <param name="uid"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static IEnumerable<MessageInfo> QueryMessageInfo(int tid, byte packId, int uid, DateTime dateTime)
        {
            if (InMemoryCache.Instance.Count == 0) return null;//没有语音消息

            string colName = "MessageInfo_" + dateTime.ToString("yyyyMMdd");
            MongoCollection col = MediaService.mongoDataBase.GetCollection(colName);
            var query = new QueryDocument { { "Tid", tid }, { "Senduid", uid }, { "PackId", packId } };
            IEnumerable<MessageInfo> msgInfos = col.FindAs<MessageInfo>(query).Where(m =>
            {
                int hour = m.Time / 10000000;
                int minute = (m.Time / 100000) % 100 + 2;
                return hour == dateTime.Hour && minute >= dateTime.Minute;
            });
            return msgInfos;
        }

        #endregion

        #region 用户发送语音消息到频道
        public static string SendAudioToChannel(AsyncUserToken token, int packnum)
        {
            StringBuilder sb = new StringBuilder("1088---------------------------------------" + token.uid + "   " + packnum);
            string recv = null;
            if (token.uid != 0)
            {
                Buffer.BlockCopy(System.BitConverter.GetBytes(token.uid), 0, token.buffer, 4, 4);
                int tid = System.BitConverter.ToInt32(token.buffer, 8);
                MediaService.talkinfo.Add(new Talkinfo(tid, token.uid, GetTimeStamp()));
                if (InitTalkMessage(tid) == false)
                {
                    return null;
                }
                TalkMessage talkmessage = null;
                if (MediaService.talkDic.TryGetValue(tid, out talkmessage))
                {
                    talkmessage.micticks = DateTime.Now.Ticks;
                    Task parent = new Task(() =>
                    {
                        sb.Append("-uidcount:" + talkmessage.uidlist.Count);
                        bool state = false;
                        foreach (int uid in talkmessage.uidlist)
                        {
                            UserObject uo = null;
                            if (uid != token.uid)
                            {
                                if (MediaService.userDic.TryGetValue(uid, out uo))
                                {
                                    if (uo.socket != null && uo.socket[token.appid] != null)
                                    {
                                        if (uo.socket[token.appid].Connected)
                                        {
                                            new Task(() =>
                                            {
                                                try
                                                {
                                                    uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                                                }
                                                catch (Exception err)
                                                {
                                                    MediaService.WriteLog("语音发送异常：uid=" + uid + "     " + err.Message, MediaService.wirtelog);
                                                    //try
                                                    //{
                                                    //    userObject.socket[token.appid].Shutdown(SocketShutdown.Both);
                                                    //}
                                                    //catch { }
                                                    //userObject.socket[token.appid].Close();
                                                    //userObject.socket[token.appid] = null;
                                                }
                                            }, TaskCreationOptions.AttachedToParent).Start();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                state = true;
                            }
                        }
                        if (state == false)
                        {
                            try
                            {
                                byte[] b = new byte[13];
                                Buffer.BlockCopy(System.BitConverter.GetBytes((short)13), 0, b, 0, 2);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.sendAudioToTalk), 0, b, 2, 2);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                                token.Socket.Send(b, SocketFlags.None);
                                MediaService.WriteLog("语音发送到对讲组，用户不在此组：uid=" + token.uid, MediaService.wirtelog);
                            }
                            catch { }
                        }
                    });
                    parent.Start();
                    parent.Wait();

                    MediaService.WriteLog(sb.ToString(), MediaService.wirtelog);
                    //taskFactory.ContinueWhenAll()
                }
            }
            else
            {
                //recv = WriteErrorJson("您还没有登陆！", 0);
            }
            return recv;
        }
        #endregion

        #region 用户更新频道备注
        public static string UserUpdateTalkInfo(AsyncUserToken token, int packnum)
        {
            if (token.uid != 0)
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("用户更新频道验证码、备注：" + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                if (qs["tid"] != null && qs["info"] != null && qs["auth"] != null)
                {
                    string auth = "";
                    if (qs["auth"] != null)
                    {
                        auth = (Int32.Parse(qs["auth"].ToString())).ToString().PadLeft(3, '0');
                    }
                    int tid = Int32.Parse(qs["tid"].ToString());
                    string info = qs["info"].ToString().Replace("'", "");
                    int count = SqlHelper.ExecuteNonQuery("update [wy_talkuser] set info='" + info + "',auth='" + auth + "' where uid='" + token.uid + "' and tid=" + tid);
                    if (count > 0)
                    {
                        return "{\"status\":true}";
                    }
                    else
                    {
                        return WriteErrorJson(20, "更新频道备注失败！");
                    }
                }
                else
                {
                    return WriteErrorJson(11, "请求的格式不正确！");
                }
            }
            else
            {

                return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
        }
        #endregion

        #region 获取频道号码---此接口有问题，没有验证重复性
        public static string GetTalkName(AsyncUserToken token, int packnum)
        {
            string recv = "{\"status\":false}";
            if (token.uid != 0)
            {
                Random ran = new Random((int)DateTime.Now.Ticks);
                string talkname = null;
                for (int k = 0; k < 10; k++)
                {
                    string qhao = ran.Next(100, 100000).ToString();
                    if (IsTalkNameOK(qhao) == true)
                    {
                        talkname = qhao.PadLeft(5, '0');
                        break;
                    }
                }
                if (talkname != null)
                {
                    string md5 = StringToMD5Hash(talkname + MediaService.Verification);
                    recv = "{\"status\":true,\"talkname\":\"" + talkname + "\",\"verification\":\"" + md5 + "\"}";
                }
                else
                {
                    recv = WriteErrorJson(3, "分配频道号失败，请稍后再试！");
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 创建我的频道
        public static string CreateMyTalk(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8).Replace("'", "");
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                if (qs["talkname"] != null && qs["verification"] != null)
                {
                    string verification = qs["verification"].ToString();
                    string talkname = qs["talkname"].ToString();

                    if (verification == StringToMD5Hash(talkname + MediaService.Verification))
                    {
                        object obj = SqlHelper.ExecuteScalar("select count(tid) from [wy_talk] where createuid=" + token.uid + " and type=0");
                        if (obj != null && (obj.ToString() == "0" || obj.ToString() == "1"))
                        {
                            if (talkname.Length == 5 && IsValiNum(talkname) && IsTalkNameOK(talkname) == true)
                            {
                                string info = qs["info"] == null ? "" : qs["info"].Replace("'", "");
                                string auth = qs["auth"] == null ? "" : qs["auth"].Replace("'", "");//ran.Next(100, 1000).ToString() : qs["auth"].Replace("'", "");
                                string talknotice = qs["talknotice"] == null ? "" : qs["talknotice"].Replace("'", "");
                                try
                                {
                                    obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname + "'");
                                    if (obj == null)
                                    {
                                        obj = SqlHelper.ExecuteScalar("insert [wy_talk] (talkname,auth,createuid,info,talknotice) values ('" + talkname + "','" + auth + "','" + token.uid + "','" + info + "','" + talknotice + "');select scope_identity()");
                                        if (obj != null)
                                        {
                                            string tid = obj.ToString();
                                            SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid,xuhao) values (" + tid + "," + token.uid + ",'1')");
                                            recv = "{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\"}";
                                        }
                                        else
                                        {
                                            recv = WriteErrorJson(23, "创建频道写入失败，请稍后再试！");
                                        }
                                    }
                                    else
                                    {
                                        recv = WriteErrorJson(23, "您所选的频道已被占用，请稍后再试！");
                                    }
                                }
                                catch (Exception err)
                                {
                                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                                    recv = WriteErrorJson(23, "创建频道操作失败，请稍后再试！");
                                }
                            }
                            else
                            {
                                recv = WriteErrorJson(23, "您所选的频道号无效，请稍后再试！");
                            }
                        }
                        else
                        {
                            recv = WriteErrorJson(11, "您创建的频道已经达到用户的最大数！");
                        }
                    }
                    else
                    {
                        recv = WriteErrorJson(11, "请求的格式不正确！");
                    }
                }
                else
                {
                    recv = WriteErrorJson(11, "请求的格式不正确！");
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 获取我所加入的频道
        public static string GetMyAllTalk(AsyncUserToken token)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                int minitid = System.BitConverter.ToInt32(token.buffer, 8);
                string sql = "";
                if (minitid == 0)
                {
                    //sql = "select T1.id,T1.tid,T1.xuhao,T1.duijiang,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type from (select top 20 id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = " + token.uid + " order by id desc) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid order by T1.id desc";
                    sql = string.Format(
                       "select T1.id,T1.tid,T1.xuhao,T1.duijiang,t2.talknotice,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.imageurl,T3.glsn from (select top 20 id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = {0} order by id desc) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid inner join app_users as t3 on t3.uid=t2.createuid order by T1.id desc;", token.uid);
                }
                else
                {
                    //sql = "select T1.id,T1.tid,T1.xuhao,T1.duijiang,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type from (select top 20 id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = " + token.uid + " and id<" + minitid + " order by id desc) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid order by T1.id desc";
                    sql =
                        string.Format(
                            "select T1.id,T1.tid,T1.xuhao,T1.duijiang,t2.talknotice,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.imageurl,T3.glsn from (select top 20 id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = {0} and id<{1} order by id desc) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid inner join app_users as t3 on t3.uid=t2.createuid order by T1.id desc;",
                            token.uid, minitid);
                }
                DataTable dt = SqlHelper.ExecuteTable(sql);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string sn = dt.Rows[i]["glsn"] == null ? "" : dt.Rows[i]["glsn"].ToString().Remove(0, 4);
                    string tid = dt.Rows[i]["tid"].ToString();
                    string talkname = dt.Rows[i]["talkname"].ToString();
                    string xuhao = dt.Rows[i]["xuhao"].ToString();
                    string dj = dt.Rows[i]["duijiang"].ToString();
                    string auth = dt.Rows[i]["auth"].ToString();
                    string remark = dt.Rows[i]["remark"].ToString();
                    if (dt.Rows[i]["talknotice"] != null && dt.Rows[i]["talknotice"].ToString() != "") //如果频道表已经有备注, 取频道表的备注
                    {
                        remark = dt.Rows[i]["talknotice"].ToString();
                    }
                    string usernum = dt.Rows[i]["usernum"].ToString();
                    string create = "false";
                    if (dt.Rows[i]["createuid"].ToString() == token.uid.ToString())
                    {
                        create = "true";
                    }
                    string type = dt.Rows[i]["type"].ToString();
                    sb.Append(",{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"xuhao\":" + xuhao + ",\"auth\":\"" + auth + "\",\"remark\":\"" + remark + "\",\"dj\":" + dj + ",\"create\":" + create + ",\"usernum\":" + usernum + ",\"type\":\"" + type + "\",\"sn\":\"" + sn + "\"}");
                }
                if (dt.Rows.Count > 0)
                {
                    minitid = Int32.Parse(dt.Rows[dt.Rows.Count - 1]["id"].ToString());
                    sb.Remove(0, 1);
                }
                sb.Insert(0, "{\"status\":true,\"minitid\":" + minitid + ",\"list\":[");
                sb.Append("]}");
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                return WriteErrorJson(6);
            }
            return sb.ToString();
        }
        #endregion

        #region 用户加入频道
        public static string UserJoinTalk(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                if (token.uid != 0)
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8).Replace("'", "");
                    MediaService.WriteLog("1009 用户加入频道:" + query + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    string auth = qs["auth"] == null ? "" : qs["auth"].Replace("'", "");
                    string talkname = qs["talkname"] == null ? "" : qs["talkname"].Replace("'", "");
                    recv = PublicClass.JoinTalk(token.uid, auth, talkname);
                }
                else
                {
                    recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 修改频道的信息
        public static string ModiTalkMessage(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8).Replace("'", "");
                MediaService.WriteLog("修改会话信息 " + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                if (qs["tid"] != null)
                {
                    string tid = qs["tid"].ToString().Replace("'", "");
                    object obj = SqlHelper.ExecuteScalar("select auth from [wy_talk] where tid='" + tid + "' and createuid=" + token.uid);
                    if (obj != null)
                    {
                        if (qs["auth"] != null)
                        {
                            string auth = qs["auth"].ToString().Replace(",", "");
                            if ((auth.Length == 3 && IsValiNum(auth) && obj.ToString() != auth) || (auth == ""))
                            {
                                SqlHelper.ExecuteNonQuery("update [wy_talk] set auth='" + qs["auth"].Replace("'", "") + "',usernum=1 where tid='" + tid + "';delete [wy_talkuser] where tid='" + tid + "' and uid!='" + token.uid + "'");
                            }
                        }
                    }
                    if (qs["remark"] != null)
                    {
                        SqlHelper.ExecuteNonQuery("update [wy_talkuser] set remark='" + qs["remark"].Replace("'", "").Trim() + "' where tid=" + tid + " and uid=" + token.uid);
                    }
                    recv = "{\"status\":true}";
                }
                else
                {
                    recv = WriteErrorJson(11, "请求的格式不正确！");
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 用户退出频道
        public static string UserExitTalk(AsyncUserToken token)
        {
            string recv = "";
            if (token.uid > 0)
            {
                try
                {
                    int tid = BitConverter.ToInt32(token.buffer, 8);
                    MediaService.WriteLog("1046 用户退出频道：tid=" + tid + "&uid=" + token.uid, MediaService.wirtelog);
                    if (PublicClass.FindTalkType(tid) == 2)
                    {
                        return WriteErrorJson(6, " 企业群组不能退出");
                    }

                    DataTable currentTalk = SqlHelper.ExecuteTable("select createuid,type from [wy_talk] where tid=" + tid);

                    if (currentTalk.Rows.Count > 0)
                    {
                        int createuid = Convert.ToInt32(currentTalk.Rows[0]["createuid"].ToString());
                        int type = Convert.ToInt32(currentTalk.Rows[0]["type"].ToString());

                        if (createuid == token.uid)
                        {
                            SqlHelper.ExecuteNonQuery("delete [wy_talkuser] where tid=" + tid + ";delete [wy_talk] where tid=" + tid);
                            TalkMessage talkmessage = null;
                            if (MediaService.talkDic.TryRemove(tid, out talkmessage))
                            {
                                UserObject uo = null;
                                foreach (int uid in talkmessage.uidlist)
                                {
                                    try
                                    {
                                        if (MediaService.userDic.TryGetValue(uid, out uo))
                                        {
                                            if (uo.socket != null && uo.socket[token.appid] != null)
                                            {
                                                if (uo.socket[token.appid].Connected)
                                                {
                                                    byte[] b = new byte[12];
                                                    Buffer.BlockCopy(System.BitConverter.GetBytes((short)12), 0, b, 0, 2);
                                                    Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                                                    Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                                                    Buffer.BlockCopy(System.BitConverter.GetBytes(tid), 0, b, 8, 4);
                                                    uo.socket[token.appid].Send(b, SocketFlags.None);
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }
                                MediaService.WriteLog("通知用户此组已解散：uid=" + token.uid, MediaService.wirtelog);
                            }
                        }
                        else if (type == 1)//快聊频道
                        {
                            //TalkMessage talkmessage = null;
                            //if (MediaService.talkDic.TryGetValue(tid, out talkmessage))
                            //{
                            //    UserObject uo = null;
                            //    foreach (int uid in talkmessage.uidlist)
                            //    {
                            //        try
                            //        {
                            //            if (MediaService.userDic.TryGetValue(uid, out uo))
                            //            {
                            //                if (uo.socket != null && uo.socket[token.appid] != null)
                            //                {
                            //                    if (uo.socket[token.appid].Connected)
                            //                    {
                            //                        byte[] b = new byte[12];
                            //                        Buffer.BlockCopy(System.BitConverter.GetBytes((short)12), 0, b, 0, 2);
                            //                        Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                            //                        Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                            //                        Buffer.BlockCopy(System.BitConverter.GetBytes(tid), 0, b, 8, 4);
                            //                        uo.socket[token.appid].Send(b, SocketFlags.None);
                            //                    }
                            //                }
                            //            }
                            //        }
                            //        catch { }
                            //    }
                            //    MediaService.WriteLog("通知其他用户此用户已退出组：uid=" + token.uid, MediaService.wirtelog);
                            //}
                        }
                        else
                        {
                            recv = PublicClass.ExitTalk(tid, token.uid);
                            try
                            {
                                UserObject uo = null;
                                if (MediaService.userDic.TryGetValue(token.uid, out uo))
                                {
                                    if (uo.socket != null && uo.socket[token.appid] != null)
                                    {
                                        if (uo.socket[token.appid].Connected)
                                        {
                                            byte[] b = new byte[12];
                                            Buffer.BlockCopy(System.BitConverter.GetBytes((short)12), 0, b, 0, 2);
                                            Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                                            Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                                            Buffer.BlockCopy(System.BitConverter.GetBytes(tid), 0, b, 8, 4);
                                            uo.socket[token.appid].Send(b, SocketFlags.None);
                                            MediaService.WriteLog("通知用户此组已退出：uid=" + token.uid, MediaService.wirtelog);
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        recv = WriteErrorJson(28);
                    }
                    recv = "{\"status\":true,\"tid\":" + tid + "}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 加入附近车队函数
        public static int JoinFujinTalk(int uid, double lo, double la)
        {
            //搜索附近群
            int talkid = 0;
            if (FujinTalkList != null && FujinTalkList.Count > 0)
            {
                double minDistance = Double.MaxValue;
                for (int i = 0; i < FujinTalkList.Count; i++)
                {
                    double distance = GetDistance(la, lo, FujinTalkList[i].la, FujinTalkList[i].lo);
                    if (distance < minDistance)
                    {
                        talkid = i;
                        minDistance = distance;
                    }
                    if (FujinTalkList[i].uidlist.Contains(uid) == true)
                    {
                        FujinTalkList[i].uidlist.Remove(uid);
                    }
                }
                if (minDistance < MediaService.neardis)//距离小于10则加入该群
                {
                    if (FujinTalkList[talkid].uidlist.Contains(uid) == false)
                        FujinTalkList[talkid].uidlist.Add(uid);
                    talkid += 1;
                }
                else
                {
                    talkid = 0;
                }
            }
            if (talkid == 0)
            {
                //新建群
                if (uid != 0 && lo != 0 && la != 0)
                {
                    FujinTalk newtalk = new FujinTalk();
                    newtalk.lo = lo;
                    newtalk.la = la;
                    newtalk.uidlist = new List<int>();
                    newtalk.uidlist.Add(uid);
                    FujinTalkList.Add(newtalk);
                    talkid = FujinTalkList.Count;
                }
            }
            return talkid;
        }
        #endregion

        #region 退出附近车队函数
        public static void ExitFujinTalk(int uid, int tid)
        {
            tid = tid - 1;
            if (FujinTalkList.Count > tid && FujinTalkList[tid] != null)
            {
                FujinTalkList[tid].uidlist.Remove(uid);
            }
        }
        #endregion

        #region GoloZ用户加入频道
        /// <summary>
        /// GoloZ用户加入频道
        /// </summary>
        public static string GoloZUserJoinTalk(AsyncUserToken token, int packnum)
        {
            var log = new StringBuilder("GoloZ用户加入频道 ");
            string recv;
            if (token.uid != 0)
            {
                try
                {
                    //{"talkname":"98179017","auth":"91452472"}
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    log.Append(" query: ").Append(query);
                    string talkname = CommFunc.GetJsonValue(query, "talkname", "\"", true);
                    string auth = CommFunc.GetJsonValue(query, "auth", "\"", true);
                    int type = PublicClass.FindTalkType(talkname);
                    if (type == 0)
                    {
                        int uid = token.uid;
                        log.Append(" uid=").Append(uid).Append(" auth=").Append(auth).Append(" talkname=").Append(talkname);
                        string data = PublicClass.JoinTalk(uid, auth, talkname);
                        log.Append(" 加入talk：").Append(data);
                        string status = CommFunc.GetJsonValue(data, "status", ",", false);
                        string message = CommFunc.GetJsonValue(data, "message", "\"", true);
                        if (status == "false")
                            recv = WriteErrorJson(11, "频道加入失败:" + message);
                        else
                        {
                            string tid = CommFunc.GetJsonValue(data, "tid", ",", false);
                            //string sql = "SELECT tid, talkname, auth, createuid, muid, info, talknotice, moditime, usernum, imageurl, [type] FROM [dbo].[wy_talk] WHERE [tid]=" + tid;
                            string sql =string.Format("SELECT au.glsn, tid, talkname, auth, createuid, muid, info, talknotice, wt.moditime, usernum, imageurl, [type],talkmode FROM [weiyun].dbo.[wy_talk] wt,[weiyun].dbo.app_users au WHERE [tid]={0} and wt.createuid=au.uid",tid);
                            int totalnum = 0;
                            int usernum = 0;
                            GetTalkNum(Convert.ToInt32(tid), ref totalnum, token.appid, ref usernum);

                            DataTable dt = SqlHelper.ExecuteTable(sql);
                            if (dt != null && dt.Rows.Count > 0)
                            {
                                DataRow row = dt.Rows[0];
                                string _createuid = row["createuid"] == null ? row["createuid"].ToString() : "";
                                int createuid;
                                int.TryParse(_createuid, out createuid);
                                bool create = false;
                                if (createuid == token.uid)
                                {
                                    create = true;
                                }
                                else if (usernum <= 20)
                                {
                                    create = true;
                                }
                                //recv = "{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + row["talkname"] +
                                //       "\",\"xuhao\":\"1\",\"auth\":\"" + row["auth"] +
                                //       "\",\"remark\":\"\",\"dj\":\"\",\"create\":" + create.ToString().ToLower() + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + "}";
                                recv = "{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + row["talkname"] + "\",\"talkmode\":\"" + row["talkmode"] +
                                       "\",\"xuhao\":\"1\",\"auth\":\"" + row["auth"] +
                                       "\",\"remark\":\"" + row["talknotice"] + "\",\"glsn\":\"" + row["glsn"] + "\",\"dj\":\"\",\"create\":" + create.ToString().ToLower() + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + "}";
                                SendToTalkUser(token, tid.ToInt());
                            }
                            else
                                recv = "{\"status\":true}";
                        }
                    }
                    else
                        recv = "{\"status\":false}";
                }
                catch (Exception err)
                {
                    log.Append(" 执行异常：").Append(err);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            log.Append(" recv=").Append(recv);
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 获取单个频道信息
        public static string GetTalkInfo(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    //tid=10024
                    StringBuilder sb = new StringBuilder();
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8).Replace("'", "");
                    MediaService.WriteLog("1113  获取频道人数信息 " + query + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    int tid = 0;
                    if (qs["tid"] != null && int.TryParse(qs["tid"].ToString(), out tid))
                    {
                        string sql = "select talkname,xuhao,duijiang,auth,remark,createuid,imageurl from wy_talk,wy_talkuser where wy_talk.tid = " + tid + " and wy_talk.tid = wy_talkuser.tid and wy_talk.createuid = wy_talkuser.uid ";
                        DataTable dt = SqlHelper.ExecuteTable(sql);
                        if (dt == null || dt.Rows.Count < 1)
                        {
                            recv = WriteErrorJson(4, "未找到该频道的信息！");
                        }
                        else
                        {
                            string talkname = dt.Rows[0]["talkname"] == null ? "" : dt.Rows[0]["talkname"].ToString();
                            string auth = dt.Rows[0]["auth"] == null ? "" : dt.Rows[0]["auth"].ToString();
                            string remark = dt.Rows[0]["remark"] == null ? "" : dt.Rows[0]["remark"].ToString();
                            string create = "false";
                            if (dt.Rows[0]["createuid"].ToString().Equals(token.uid.ToString()))
                            {
                                create = "true";
                            }
                            string imageurl = dt.Rows[0]["imageurl"] == null ? "" : dt.Rows[0]["imageurl"].ToString();
                            if (imageurl.Equals("<null>"))
                                imageurl = "";
                            int totalnum = 0;
                            int usernum = 0;
                            GetTalkNum(tid, token.appid, ref totalnum, ref usernum);
                            sb.Append("{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\",\"remark\":\"" + remark + "\",\"create\":" + create + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + ",\"imageurl\":\"" + imageurl + "\"}");
                            recv = sb.ToString();
                        }

                    }
                    else
                    {
                        recv = WriteErrorJson(4, "传入的参数有误！");
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    return WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;

        }

        #region 获取频道数
        public static void GetTalkNum(int tid, int appid, ref int totalnum, ref int usernum)
        {
            totalnum = 0;
            usernum = 0;
            TalkMessage tm;
            object obj = SqlHelper.ExecuteScalar("SELECT count(*) FROM wy_talkuser WHERE tid=" + tid);
            if (obj != null)
            {
                totalnum = Int32.Parse(obj.ToString());
                if (MediaService.talkDic.TryGetValue(tid, out tm))
                {
                    UserObject uo = null;
                    foreach (int uid in tm.uidlist)
                    {
                        if (MediaService.userDic.TryGetValue(uid, out uo))
                        {
                            if (uo.socket != null && uo.socket[appid] != null)
                            {
                                if (uo.socket[appid].Connected)
                                {
                                    usernum++;
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion
        #endregion

        //会话相关

        #region 创建会话组或群
        public static string CreateTalk(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8).Replace("'", "");
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                recv = PublicClass.CreateTalk(token.uid, qs);
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 根据关键字搜索会话组或群
        public static string GetTalkToKey(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                if (token.uid != 0)
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8).Replace("'", "");
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    if (qs["talkkey"] != null)
                    {
                        string talkkey = qs["talkkey"].ToString().Replace(",", "");
                        DataTable dt = SqlHelper.ExecuteTable("select tid, talkname,info from [wy_talk] where talkname like '%" + talkkey + "%'");
                        foreach (DataRow dr in dt.Rows)
                        {
                            recv += (recv == "" ? "" : ",") + "{\"tid\":" + dr["tid"].ToString() + ",\"talkname\":\"" + dr["talkname"].ToString() + "\",\"info\":\"" + dr["info"].ToString() + "\"}";
                        }
                        recv = "{\"status\":true,\"list\":[" + recv + "]}";
                    }
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 添加会话或群用户
        public static string AddTalkUser(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                int tid = System.BitConverter.ToInt32(token.buffer, 8);
                object obj = SqlHelper.ExecuteScalar("select uid from [wy_talkuser] where tid=" + tid + " and uid=" + token.uid);
                if (obj != null)
                {
                    try
                    {
                        StringBuilder sql = new StringBuilder();
                        for (int i = 12; i < packnum; i += 4)
                        {
                            int uid = System.BitConverter.ToInt32(token.buffer, i);
                            sql.Append(" or uid=");
                            sql.Append(uid);
                            SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid) values (" + tid + "," + uid + ")");
                        }
                        if (sql.Length != 0)
                        {
                            DataTable dt = SqlHelper.ExecuteTable("select nickname from [app_users] where " + sql.Remove(0, 3));
                            StringBuilder message = new StringBuilder();
                            foreach (DataRow dr in dt.Rows)
                            {
                                message.Append("、");
                                message.Append(dr[0].ToString());
                            }
                            if (message.Length > 0)
                            {
                                long timeStamp = GetTimeStamp();
                                message.Remove(0, 1);
                                message.Insert(0, "邀请");
                                message.Insert(0, token.nickname);
                                message.Insert(0, "{\"fid\":\"\",\"fext\":\"\",\"fatr\":\"\",\"sendcontent\":\"");
                                message.Append(" 加入聊天\",\"senduid\":");
                                message.Append(token.uid);
                                message.Append(",\"mtype\":8,\"tid\":");
                                message.Append(tid);
                                message.Append(",\"sendtime\":");
                                message.Append(timeStamp);
                                message.Append("}");

                                TalkSend talksend = new TalkSend(message, token.nickname, 8, tid.ToString(), token.uid, token.appid);
                                MediaService.talkSendMessage.Add(talksend);
                            }
                        }

                        recv = "{\"status\":true,\"tid\":" + tid + "}";
                    }
                    catch (Exception err)
                    {
                        MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
                    }
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 获取群通知列表
        public static string GetTalkNocticeList(AsyncUserToken token, int packnum)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                if (token.uid != 0)
                {
                    int minid = System.BitConverter.ToInt32(token.buffer, 8);
                    string sql = "";
                    if (minid == 0)
                    {
                        SqlHelper.ExecuteNonQuery("update [wy_usermessage] set state=1 where recvuid=" + token.uid + " and senduid=600001 and state=0");
                        sql = "select top 20 id,tid,message,ntype,senduid from[wy_talknotice] where recvuid=" + token.uid + " order by id desc";
                    }
                    else
                    {
                        sql = "select top 20 id,tid,message,ntype,senduid from[wy_talknotice] where recvuid=" + token.uid + " and id< " + minid + " order by id desc";
                    }
                    MediaService.WriteLog("获取群通知列表： " + sql, MediaService.wirtelog);
                    DataTable dt = SqlHelper.ExecuteTable(sql);
                    foreach (DataRow dr in dt.Rows)
                    {
                        string tid = dr["tid"].ToString();
                        string ntype = dr["ntype"].ToString();
                        string senduid = dr["senduid"].ToString();
                        string message = dr["message"].ToString();
                        if (sb.Length > 0)
                            sb.Append(",{\"tid\":" + tid + ",\"senduid\":" + senduid + ",\"ntype\":" + ntype + ",\"message\":\"" + message + "\"}");
                        else
                            sb.Append("{\"tid\":" + tid + ",\"senduid\":" + senduid + ",\"ntype\":" + ntype + ",\"message\":\"" + message + "\"}");
                    }
                    int nowminid = 0;
                    if (dt.Rows.Count > 0)
                        nowminid = Int32.Parse(dt.Rows[dt.Rows.Count - 1]["id"].ToString());
                    sb.Insert(0, "{\"status\":true,\"minid\":" + nowminid + ",\"noticelist\":[");
                    sb.Append("]}");
                }
                else
                {
                    return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                return WriteErrorJson(6);
            }
            return sb.ToString();
        }
        #endregion

        #region 设置群通知状态
        public static string SetTalkNocticeState(AsyncUserToken token, int packnum)
        {
            if (token.uid != 0)
            {
                int tid = System.BitConverter.ToInt32(token.buffer, 8);
                int senduid = System.BitConverter.ToInt32(token.buffer, 12);
                int state = System.BitConverter.ToInt32(token.buffer, 16);

                object obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where tid=" + tid + " and createuid=" + token.uid);
                if (obj != null)
                {
                    if (state == 0)
                        SqlHelper.ExecuteNonQuery("update [wy_talknotice] set ntype=2 where senduid=" + senduid + " and tid=" + tid);
                    else
                    {
                        SqlHelper.ExecuteNonQuery("update [wy_talknotice] set ntype=1 where senduid=" + senduid + " and tid=" + tid);
                        SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid) values (" + tid + "," + senduid + ")");
                        obj = SqlHelper.ExecuteScalar("select nickname from [app_users] where uid=" + senduid);
                        long timeStamp = GetTimeStamp();

                        StringBuilder message = new StringBuilder("{\"fid\":\"\",\"fext\":\"\",\"fatr\":\"\",\"sendcontent\":\"");
                        message.Append(obj.ToString());
                        message.Append(" 加入群\",\"senduid\":");
                        message.Append(token.uid);
                        message.Append(",\"mtype\":8,\"tid\":");
                        message.Append(tid);
                        message.Append(",\"sendtime\":");
                        message.Append(timeStamp);
                        message.Append("}");

                        TalkSend talksend = new TalkSend(message, token.nickname, 8, tid.ToString(), token.uid, token.appid);
                        MediaService.talkSendMessage.Add(talksend);
                    }
                }
                return "{\"status\":true}";
            }
            else
            {
                return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
        }
        #endregion

        #region 创建者解散会话或群
        public static string UserDeleteTalk(AsyncUserToken token)
        {
            string recv = "";
            try
            {
                int tid = System.BitConverter.ToInt32(token.buffer, 8);
                int count = SqlHelper.ExecuteNonQuery("delete [wy_talk] where tid=" + tid + " and createuid=" + token.uid);
                if (count > 0)
                {
                    SqlHelper.ExecuteNonQuery("delete [wy_talkuser] where tid=" + tid);
                    TalkMessage talkmessage = null;
                    MediaService.talkDic.TryRemove(tid, out talkmessage);
                }
                else
                {
                    recv = WriteErrorJson(28);
                }
                recv = "{\"status\":true}";
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 客户端发送会话或群消息
        public static string SendTalkMessage(AsyncUserToken token, int packnum)
        {
            string recv = null;
            if (token.uid != 0)
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("发送会话信息：" + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                if (qs["tid"] != null && qs["sendcontent"] != null)
                {
                    string fid = qs["fid"] == null ? "" : qs["fid"].ToString().Replace("'", "");
                    string fext = qs["fext"] == null ? "" : qs["fext"].ToString().Replace("'", "");
                    string fatr = qs["fatr"] == null ? "" : qs["fatr"].ToString().Replace("'", "");
                    string mtype = qs["mtype"] == null ? "0" : qs["mtype"].ToString().Replace("'", "");
                    string sendcontent = StringToJson(qs["sendcontent"].ToString().Replace("'", ""));
                    string tid = qs["tid"].ToString().Replace("'", "");

                    MediaService.WriteLog("命令：发送会话信息 " + tid, false);
                    long timeStamp = GetTimeStamp();
                    StringBuilder message = new StringBuilder("{\"fid\":\"");
                    message.Append(fid);
                    message.Append("\",\"fext\":\"");
                    message.Append(fext);
                    message.Append("\",\"fatr\":\"");
                    message.Append(fatr);
                    message.Append("\",\"sendcontent\":\"");
                    message.Append(sendcontent);
                    message.Append("\",\"senduid\":");
                    message.Append(token.uid);
                    message.Append(",\"mtype\":");
                    message.Append(mtype);
                    message.Append(",\"tid\":");
                    message.Append(tid);
                    message.Append(",\"sendtime\":");
                    message.Append(timeStamp);
                    message.Append("}");

                    TalkSend talksend = new TalkSend(message, token.nickname, Int32.Parse(mtype), tid, token.uid, token.appid);
                    MediaService.talkSendMessage.Add(talksend);

                    recv = "{\"status\":true,\"timeStamp\":" + timeStamp + "}";
                }
                else
                {
                    recv = WriteErrorJson(11);
                }
            }
            return recv;
        }
        #endregion

        #region 获取会话或群消息列表
        public static string GetTalkMessageList(AsyncUserToken token)
        {
            StringBuilder sb = new StringBuilder();
            if (token.uid != 0)
            {
                try
                {
                    int index = System.BitConverter.ToInt32(token.buffer, 8);
                    int tid = System.BitConverter.ToInt32(token.buffer, 12);
                    string sql = "";
                    if (index == 0)
                    {
                        SqlHelper.ExecuteNonQuery("update [wy_talkuser] set noread=0 where tid=" + tid + " and uid=" + token.uid);
                        sql = "select top 20 id,message from [wy_talkmessage] where tid =" + tid + " order by id desc";
                    }
                    else
                    {
                        sql = "select top 20 id,message from [wy_talkmessage] where tid =" + tid + " and id<" + index + " order by id desc";
                    }
                    MediaService.WriteLog("test： " + sql, MediaService.wirtelog);
                    DataTable dt = SqlHelper.ExecuteTable(sql);
                    foreach (DataRow dr in dt.Rows)
                    {
                        string message = dr["message"].ToString();
                        if (message.Length > 1)
                        {
                            if (sb.Length > 1)
                                sb.Append("," + message);
                            else
                                sb.Append(message);
                        }
                    }
                    int minid = 0;
                    if (dt.Rows.Count > 0)
                        minid = Int32.Parse(dt.Rows[dt.Rows.Count - 1]["id"].ToString());
                    sb.Insert(0, "{\"status\":true,\"minid\":" + minid + ",\"list\":[");
                    sb.Append("]}");
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    sb.Clear();
                    sb.Append(WriteErrorJson(6));
                }
            }
            else
            {
                sb.Append(WriteErrorJson(3, "你还没有登陆，请稍后再试！"));
            }
            return sb.ToString();
        }
        #endregion

        #region 获取会话组或群内的用户
        public static string GetTalkUser(AsyncUserToken token)
        {
            string recv = "";
            try
            {
                int tid = System.BitConverter.ToInt32(token.buffer, 8);
                bool myon = false;
                DataTable dt = SqlHelper.ExecuteTable("select uid,duijiang from [wy_talkuser] where tid=" + tid);
                UserObject uo = null;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int uid = Int32.Parse(dt.Rows[i]["uid"].ToString());
                    string dj = dt.Rows[i]["duijiang"].ToString();
                    if (uid == token.uid) myon = true;
                    string online = "false";
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        if (uo.socket[token.appid] != null)
                        {
                            online = "true";
                        }
                    }
                    recv += (recv == "" ? "" : ",") + "{\"uid\":" + uid + ",\"dj\":" + dj + ",\"online\":\"" + online + "\"}";
                }
                if (myon == false)
                {
                    recv = WriteErrorJson(21);
                }
                else
                {
                    recv = "{\"status\":true,\"tid\":" + tid + ",\"list\":[" + recv + "]}";
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 创建者删除会话或群里的用户
        public static string DeleTalkUser(AsyncUserToken token)
        {
            string recv = "";
            try
            {
                int tid = System.BitConverter.ToInt32(token.buffer, 8);
                int uid = System.BitConverter.ToInt32(token.buffer, 12);
                object obj = SqlHelper.ExecuteScalar("select tid from[wy_talk] where tid=" + tid + " and createuid=" + token.uid);
                if (obj != null)
                {
                    SqlHelper.ExecuteNonQuery("delete [wy_talkuser] where tid=" + tid + " and uid=" + uid);
                    recv = "{\"status\":true}";
                }
                else
                {
                    recv = WriteErrorJson(28);
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 获取会话组和群列表公开信息--X
        public static string GetTalkListPublic(AsyncUserToken token, int packnum)
        {
            string recv = "";
            StringBuilder sb = new StringBuilder();
            for (int i = 8; i < packnum; i = i + 4)
            {
                int tid = System.BitConverter.ToInt32(token.buffer, i);
                if (i == 8) sb.Append(" tid=" + tid);
                else sb.Append(" or tid=" + tid);
            }
            if (sb.Length > 0)
            {
                DataTable dt = SqlHelper.ExecuteTable("select tid,talkname,lo,la,createuid from [wy_talk] where " + sb.ToString());
                if (dt.Rows.Count > 0)
                {
                    TalkListBaseMessageJson UserListJson = new TalkListBaseMessageJson(true, dt);
                    DataContractJsonSerializer json = new DataContractJsonSerializer(UserListJson.GetType());
                    using (MemoryStream stream = new MemoryStream())
                    {
                        json.WriteObject(stream, UserListJson);
                        recv = Encoding.UTF8.GetString(stream.ToArray());
                    }
                }
                else
                {
                    recv = WriteErrorJson(34);
                }
            }
            else
            {
                recv = WriteErrorJson(11);
            }
            return recv;
        }
        #endregion

        #region 获取群信息--X
        public static string GetTalkMessage(AsyncUserToken token, int packnum)
        {
            StringBuilder sb = new StringBuilder();
            int tid = System.BitConverter.ToInt32(token.buffer, 8);
            DataTable dt = SqlHelper.ExecuteTable("select uid,utype from [wy_talkuser] where tid=" + tid);
            sb.Append("\"userlist\":[");
            foreach (DataRow dr in dt.Rows)
            {
                string uid = dr["uid"].ToString();
                string utype = dr["utype"].ToString();
                if (sb.Length > 12)
                    sb.Append(",{\"uid\":" + uid + ",\"utype\":" + utype + "}");
                else
                    sb.Append("{\"uid\":" + uid + ",\"utype\":" + utype + "}");
            }
            sb.Append("]");
            dt = SqlHelper.ExecuteTable("select talkname, createuid, lo, la, info, moditime, lable, talknotice, ifsearch, ifauth from [wy_talk] where tid=" + tid);
            if (dt.Rows.Count > 0)
            {
                sb.Insert(0, "\"talkname\":\"" + dt.Rows[0]["talkname"].ToString() + "\",");
                sb.Insert(0, "\"lo\":" + dt.Rows[0]["lo"].ToString() + ",");
                sb.Insert(0, "\"la\":" + dt.Rows[0]["la"].ToString() + ",");
                sb.Insert(0, "\"info\":\"" + dt.Rows[0]["info"].ToString() + "\",");
                sb.Insert(0, "\"moditime\":\"" + dt.Rows[0]["moditime"].ToString() + "\",");
                sb.Insert(0, "\"lable\":\"" + dt.Rows[0]["lable"].ToString() + "\",");
                sb.Insert(0, "\"talknotice\":\"" + dt.Rows[0]["talknotice"].ToString() + "\",");
                sb.Insert(0, "\"ifsearch\":" + dt.Rows[0]["ifsearch"].ToString() + ",");
                sb.Insert(0, "\"ifauth\":" + dt.Rows[0]["ifauth"].ToString() + ",");
                sb.Insert(0, "\"createuid\":" + dt.Rows[0]["createuid"].ToString() + ",");
            }
            sb.Insert(0, "{\"status\":true,");
            sb.Append("}");
            return sb.ToString();
        }
        #endregion

        //平台相关

        #region 上传经纬度
        public static string SendUserLoLa(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                if (token.uid != 0)
                {
                    string log = "";
                    double lo = 0;
                    double la = 0;
                    double al = 0;
                    float vi = 0;
                    float di = 0;
                    int cityid = 0;
                    long nowtime = GetTimeStamp();
                    if (packnum >= 40)
                    {
                        lo = BitConverter.ToDouble(token.buffer, 8);
                        la = BitConverter.ToDouble(token.buffer, 16);
                        al = BitConverter.ToDouble(token.buffer, 24);
                        vi = BitConverter.ToSingle(token.buffer, 32);
                        di = BitConverter.ToSingle(token.buffer, 36);
                    }
                    log += "上传GPS：uid=" + token.uid + ",lo=" + lo + ",la=" + la + ",vi=" + vi + ",di=" + di + ",al=" + al + ",cityid=" + cityid;
                    if (packnum >= 48)
                    {
                        long time = BitConverter.ToInt64(token.buffer, 40);
                        log += ",time=" + time;
                        if (time > 0)
                        {
                            if (token.glsn > 90000000)
                                MediaService.logininfo.Add(new Logininfo(token.uid, time, nowtime));
                            else
                                MediaService.golo6Logininfo.Add(new Golo6Logininfo(token.uid, time, nowtime));
                        }
                    }
                    if (packnum > 56)
                    {
                        cityid = (int)(BitConverter.ToSingle(token.buffer, 56));
                    }

                    GPSinfo gps = new GPSinfo(lo, la, al, vi, di, token.uid, nowtime, cityid);
                    MediaService.gpsinfo.Add(gps);

                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(token.uid, out uo))
                    {
                        uo.lo[token.appid] = lo;
                        uo.la[token.appid] = la;
                        uo.vi[token.appid] = vi;
                        uo.di[token.appid] = di;
                        uo.al[token.appid] = al;
                        uo.cid[token.appid] = cityid;
                    }
                    recv = "{\"status\":true}";
                }
                else
                {
                    recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 上传电量

        public static string UploadPower(AsyncUserToken token, int packnum)
        {
            string recv;
            try
            {
                if (token.uid != 0)
                {
                    var json= Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog( "上传电量:" + json, MediaService.wirtelog);
                    var powerinfo= JsonConvert.DeserializeObject<PowerInfo>(json);
                    MediaService.Powerinfos.Add(powerinfo);
                    recv = "{\"status\":true}";
                }
                else
                {
                    recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                recv = WriteErrorJson(6);
            }
            return recv;
        }

        public static string LaunchCall(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                var json = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                var inputParamer = JsonConvert.DeserializeObject<InputParamer>(json);
                var sqlUid =
                    SqlHelper.ExecuteTable(string.Format("select uid from app_users where glsn='{0}'",
                        CommFunc.GetUniform12(inputParamer.sn)));
                if (sqlUid.Rows.Count <= 0)
                {
                    ReturnState returnState = new ReturnState
                                              {
                                                  state = inputParamer.state,
                                                  status = true,
                                                  tid = inputParamer.tid
                                              };
                    json = JsonConvert.SerializeObject(returnState);
                    return json;
                }
                var uided = Convert.ToInt32(sqlUid.Rows[0]["uid"]); //被呼叫的uid
                if (inputParamer.state == (int) EnumStateType.连接建立成功)
                {
                    recv = SendCallbackBath(token,uided,EnumStateType.连接建立成功,inputParamer.tid);
                }
                if (inputParamer.state == (int)EnumStateType.对方正在通话中)
                {
                    recv = SendCallbackBath(token, uided, EnumStateType.对方正在通话中, inputParamer.tid);
                }
                if (inputParamer.state == (int) EnumStateType.接通成功)
                {
                    recv = SendCallbackBath(token, uided, EnumStateType.接通成功, inputParamer.tid);
                }
                if (inputParamer.state == (int)EnumStateType.对方拒接)
                {
                    recv = SendCallbackBath(token, uided, EnumStateType.对方拒接, inputParamer.tid);
                }
                if (inputParamer.state == (int) EnumStateType.对方已挂断)
                {
                    recv = SendCallbackBath(token, uided, EnumStateType.对方已挂断, inputParamer.tid);//任意一方挂断都需要服务器通知，通知对方已挂断
                }
                if(inputParamer.state==(int)EnumStateType.正在连接对方)
                {
                    recv = SendCallback(token, uided, EnumStateType.正在连接对方);//给接受者通知（需要返回邀请者的sn 和tid）
                }
                if (inputParamer.state == (int) EnumStateType.收到单呼)
                {
                    recv = SendCallbackOnlyOther(token, uided, EnumStateType.收到单呼);//心跳
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog(string.Format("异常:{0}",e.Message+e.TargetSite+e.StackTrace), true);
            }
            return recv;
        }

        private static string SendCallbackBath(AsyncUserToken token, int uided, EnumStateType enumStateType,int _tid)
        {
            var json = new {state = (int) enumStateType, status = true, tid = _tid};
            UserObject uo;
            if (MediaService.userDic != null && MediaService.userDic.TryGetValue(uided, out uo)) //被呼叫者是否在线
            {
                try
                {
                    ReturnState returnState = new ReturnState {state = (int) enumStateType, status = true, tid = _tid};
                    var sendjson = JsonConvert.SerializeObject(returnState);
                    byte[] cbyte = Encoding.UTF8.GetBytes(sendjson);
                    Buffer.BlockCopy(cbyte, 0, token.buffer, 8, cbyte.Length);
                    Buffer.BlockCopy(BitConverter.GetBytes((short)(cbyte.Length + 8)), 0, token.buffer, 0, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes(CommType.LaunchCall), 0, token.buffer, 2, 2);
                    //给被呼叫方发送
                    if (uo.socket[token.appid] == null)
                    {
                        var tmp = new ReturnState {state = 1, status = true};
                        return JsonConvert.SerializeObject(tmp);
                    }
                    uo.socket[token.appid].Send(token.buffer, 0, cbyte.Length + 8, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    MediaService.WriteLog(string.Format("enumStateType:{1},ex:{0}", ex.Message, enumStateType), true);
                    return JsonConvert.SerializeObject(new { status = false });
                }
            }
            else
            {
                ReturnState returnState = new ReturnState { state = 1, status = true, tid = _tid };
                return JsonConvert.SerializeObject(returnState);
            }
            return JsonConvert.SerializeObject(json);
        }

        private static string SendCallback(AsyncUserToken token, int uided, EnumStateType stateType)
        {
            var json = "";
            UserObject uo;
            if (MediaService.userDic != null && MediaService.userDic.TryGetValue(uided, out uo)) //被呼叫者是否在线
            {
                try
                {
                    if (uo == null)
                    {
                        var returnState = new ReturnState { state = 1, status = true };
                        return JsonConvert.SerializeObject(returnState);
                    }
                    if (uo.ver < 133)
                    {
                        var tmp = new {state = 9, status = true};
                        return JsonConvert.SerializeObject(tmp);
                    }
                    var resultModel = new ReturnParamer
                                      {
                                          sn = token.glsn,
                                          state = (int) stateType,
                                          status = true,
                                          tid = CommFunc.GenerateTid()
                                      };
                    json = JsonConvert.SerializeObject(resultModel);
                    byte[] cbyte = Encoding.UTF8.GetBytes(json);
                    Buffer.BlockCopy(cbyte, 0, token.buffer, 8, cbyte.Length);
                    Buffer.BlockCopy(BitConverter.GetBytes((short) (cbyte.Length + 8)), 0, token.buffer, 0, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes(CommType.LaunchCall), 0, token.buffer, 2, 2);
                    //给被呼叫方发送
                    if (uo.socket[token.appid] == null)
                    {
                        var returnState = new ReturnState { state = 1, status = true };
                        return JsonConvert.SerializeObject(returnState);
                    }
                    uo.socket[token.appid].Send(token.buffer, 0, cbyte.Length + 8, SocketFlags.None);

                    return json;
                }
                catch(Exception ex)
                {
                    MediaService.WriteLog(string.Format("stateType:{1},ex:{0}", ex.Message, stateType), true);
                    return JsonConvert.SerializeObject(new {status = false});
                }
            }
            else
            {
                ReturnState returnState = new ReturnState {state = 1, status = true};
                json = JsonConvert.SerializeObject(returnState);
            }
            return json;
        }

        /// <summary>
        /// 只给对方发送.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="uided"></param>
        /// <param name="enumStateType"></param>
        private static string SendCallbackOnlyOther(AsyncUserToken token, int uided, EnumStateType enumStateType)
        {
            var json = new { status = true };
            UserObject uo;
            if (MediaService.userDic.TryGetValue(uided, out uo)) //被呼叫者是否在线
            {
                try
                {
                    ReturnState returnState = new ReturnState { state = (int)enumStateType, status = true };
                    var sendjson = JsonConvert.SerializeObject(returnState);
                    byte[] cbyte = Encoding.UTF8.GetBytes(sendjson);
                    Buffer.BlockCopy(cbyte, 0, token.buffer, 8, cbyte.Length);
                    Buffer.BlockCopy(BitConverter.GetBytes((short)(cbyte.Length + 8)), 0, token.buffer, 0, 2);
                    Buffer.BlockCopy(BitConverter.GetBytes(CommType.LaunchCall), 0, token.buffer, 2, 2);
                    //给被呼叫方发送
                    if (uo.socket[token.appid] == null)
                    {
                        MediaService.WriteLog(string.Format("uo.socket[token.appid] is null"), true);
                        return JsonConvert.SerializeObject(new ReturnState { state = 1, status = true });
                    }
                    uo.socket[token.appid].Send(token.buffer, 0, cbyte.Length + 8, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    MediaService.WriteLog(string.Format("stateType:{1},ex:{0}", ex.Message, enumStateType), true);
                    return JsonConvert.SerializeObject(new { status = false });
                }
            }
            else
            {
                ReturnState returnState = new ReturnState { state = 1, status = true };
                return JsonConvert.SerializeObject(returnState);
            }
            return JsonConvert.SerializeObject(json);
        }

        #endregion

        #region 根据用户经纬度获取附近的用户
        public static string GetNearUserFromLoLa(AsyncUserToken token)
        {
            double lo = BitConverter.ToDouble(token.buffer, 8);
            double la = BitConverter.ToDouble(token.buffer, 16);
            if (token.uid != 0)
            {
                double[] lalo = getAround(la, lo, 100000);
                Dictionary<string, double> dic = new Dictionary<string, double>();
                foreach (var item in MediaService.userDic)
                {
                    if (item.Value.socket != null && item.Value.socket[token.appid] != null)
                    {
                        double loc = Math.Abs(item.Value.lo[token.appid] - lo);
                        double lac = Math.Abs(item.Value.la[token.appid] - la);

                        if (lac < lalo[0] && loc < lalo[1])
                        {
                            int uid = (int)(item.Key);
                            if (uid != token.uid)
                            {
                                double dis = GetDistance(la, lo, item.Value.la[token.appid], item.Value.lo[token.appid]);
                                dic.Add("{\"uid\":" + item.Key + ",\"lo\":" + item.Value.lo[token.appid] + ",\"la\":" + item.Value.la[token.appid] + ",\"dis\":" + dis + "}", dis);
                            }
                        }
                    }
                }
                if (dic.Count == 0)
                {
                    return WriteErrorJson(20);
                }
                else
                {
                    StringBuilder recv = new StringBuilder();
                    for (int i = 0; i < 50 && dic.Count != 0; i++)
                    {
                        double mindis = 10000000000;
                        string minstr = "";
                        foreach (KeyValuePair<string, double> dis in dic)
                        {
                            if (dis.Value < mindis)
                            {
                                mindis = dis.Value;
                                minstr = dis.Key;
                            }
                        }
                        dic.Remove(minstr);
                        if (i != 0)
                        {
                            recv.Append("," + minstr);
                        }
                        else
                        {
                            recv.Append(minstr);
                        }
                    }
                    recv.Insert(0, "{\"status\":true,\"list\":[");
                    recv.Append("]}");
                    return recv.ToString();
                }
            }
            else
            {
                return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
        }
        #endregion

        #region 根据用户经纬度获取附近的群
        public static string GetNearGroupFromLoLa(AsyncUserToken token)
        {
            StringBuilder recv = new StringBuilder();
            double lo = BitConverter.ToDouble(token.buffer, 8);
            double la = BitConverter.ToDouble(token.buffer, 16);
            if (token.uid != 0)
            {
                double[] lalo = getAround(la, lo, 100000);
                double minLa = la - lalo[0];
                double maxLa = la + lalo[0];
                double minLo = lo - lalo[1];
                double maxLo = lo + lalo[1];

                DataTable dt = SqlHelper.ExecuteTable("select tid,talkname,lo,la,tid as dis from [wy_talk] where lo>" + minLo + " and lo<" + maxLo + " and la>" + minLa + " and la<" + maxLa);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string slo = dt.Rows[i]["lo"].ToString();
                    string sla = dt.Rows[i]["la"].ToString();
                    string gid = dt.Rows[i]["tid"].ToString();
                    string talkname = dt.Rows[i]["talkname"].ToString();
                    double glo = Double.Parse(slo);
                    double gla = Double.Parse(sla);
                    dt.Rows[i]["dis"] = GetDistance(la, lo, gla, glo);
                }
                DataView dv = new DataView(dt);
                dv.Sort = "dis asc";
                for (int j = 0; j < 50 && j < dv.Count; j++)
                {
                    string gid1 = dv[j]["tid"].ToString();
                    string lo1 = dv[j]["lo"].ToString();
                    string la1 = dv[j]["la"].ToString();
                    string dis1 = dv[j]["dis"].ToString();
                    string talkname1 = dv[j]["talkname"].ToString();
                    if (recv.Length != 0)
                        recv.Append("},{");
                    else recv.Append("{");
                    recv.Append("\"gid\":");
                    recv.Append(gid1);
                    recv.Append(",\"lo\":");
                    recv.Append(lo1);
                    recv.Append(",\"la\":");
                    recv.Append(la1);
                    recv.Append(",\"dis\":");
                    recv.Append(dis1);
                    recv.Append(",\"talkname\":");
                    recv.Append("\"" + talkname1 + "\"");
                }
                if (recv.Length == 0)
                {
                    return WriteErrorJson(34);
                }
                else
                {
                    recv.Insert(0, "{\"status\":true,\"list\":[");
                    recv.Append("}]}");
                    return recv.ToString();
                }
            }
            else
            {
                return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
        }
        #endregion

        #region 检查应用更新
        public static string CheckAppUpdate(AsyncUserToken token, int packnum)
        {
            string recv = "";
            string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
            MediaService.WriteLog("请求：" + query, MediaService.wirtelog);
            NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
            string app_key = qs["__API__[app_key]"] == null ? "" : qs["__API__[app_key]"].ToString().Replace('\'', ' ');
            string app_secret = qs["__API__[app_secret]"] == null ? "" : qs["__API__[app_secret]"].ToString().Replace('\'', ' ');
            DataRow[] dr = MediaService.allapp.Select("app_key='" + app_key + "' and app_secret='" + app_secret + "'");
            if (dr.Length > 0)
            {
                string app_id = dr[0]["id"].ToString();
                string describe = HttpUtility.UrlEncode(dr[0]["describe"].ToString(), Encoding.UTF8);
                string appimg = HttpUtility.UrlEncode(dr[0]["appimg"].ToString(), Encoding.UTF8);
                string systemtype = qs["systemtype"] == null ? "android" : qs["systemtype"].ToString().Replace("'", "");
                DataTable dt = SqlHelper.ExecuteTable("select ver,shenji,vermessage,installurl,packname from [app_ver] where app_id='" + app_id + "' and systemtype='" + systemtype + "'");
                if (dt != null && dt.Rows.Count > 0)
                {
                    string vermessage = HttpUtility.UrlEncode(dt.Rows[0]["vermessage"].ToString(), Encoding.UTF8);
                    string installurl = HttpUtility.UrlEncode(dt.Rows[0]["installurl"].ToString(), Encoding.UTF8);
                    string packname = HttpUtility.UrlEncode(dt.Rows[0]["packname"].ToString(), Encoding.UTF8);
                    string ver = dt.Rows[0]["ver"].ToString();
                    string update = dt.Rows[0]["shenji"].ToString() == "0" ? "false" : "true";
                    recv = "{\"status\":true,\"ver\":\"" + ver + "\",\"update\":" + update + ",\"vermessage\":\"" + vermessage + "\",\"packname\":\"" + packname + "\",\"installurl\":\"" + installurl + "\",\"describe\":\"" + describe + "\",\"appimg\":\"" + appimg + "\"}";
                }
            }
            else
            {
                recv = WriteErrorJson(29);
            }
            return recv;
        }
        #endregion

        #region 发送应用反馈
        public static string SendAppFeedback(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("请求：" + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                string content = HttpUtility.UrlDecode(qs["content"].ToString()).Replace("'", "");
                string brand = qs["brand"] == null ? "" : HttpUtility.UrlDecode(qs["brand"].ToString(), Encoding.UTF8).Replace("'", "");
                string series = qs["series"] == null ? "" : HttpUtility.UrlDecode(qs["series"].ToString(), Encoding.UTF8).Replace("'", "");
                string systemver = qs["systemver"] == null ? "" : HttpUtility.UrlDecode(qs["systemver"].ToString(), Encoding.UTF8).Replace("'", "");
                string softver = qs["softver"] == null ? "" : HttpUtility.UrlDecode(qs["softver"].ToString(), Encoding.UTF8).Replace("'", "");
                string mobile = qs["mobile"] == null ? "" : qs["mobile"].ToString().Replace("'", "");

                object obj = SqlHelper.ExecuteNonQuery("insert [app_feedback] (uid, brand, series, content, systemver, softver, appid,mobile) values (" + token.uid + ",'" + brand + "','" + series + "','" + content + "','" + systemver + "','" + softver + "','" + token.appid + "','" + mobile + "')");

                recv = "{\"status\":true}";
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 设置通知状态
        public static string SetNoticeState(AsyncUserToken token)
        {
            string recv = "";
            int noticeid = System.BitConverter.ToInt32(token.buffer, 8);
            int state = System.BitConverter.ToInt32(token.buffer, 12);

            UserObject uo = null;
            if (token.uid > 0 && MediaService.userDic.TryGetValue(token.uid, out uo))
            {
                string username = token.nickname;
                object obj = SqlHelper.ExecuteScalar("select uid from [app_notice] where id=" + noticeid);
                if (obj != null)
                {
                    string noticeuid = obj.ToString();
                    if (noticeuid == "" || noticeuid.IndexOf(username + ",") >= 0)
                    {
                        obj = SqlHelper.ExecuteScalar("select id from [app_notice_log] where noticeid=" + noticeid + " and uid ='" + username + "'");
                        if (obj == null)
                        {
                            SqlHelper.ExecuteNonQuery("insert [app_notice_log] (uid, noticeid) values ('" + username + "','" + noticeid + "')");
                        }
                        recv = "{\"status\":true}";
                    }
                }
            }
            return recv;
        }
        #endregion

        #region 获取最大通知ID
        public static string GetMaxNoticeID(AsyncUserToken token)
        {
            int maxnoticeid = 0;
            string nowtime = DateTime.Now.ToString();
            object obj = SqlHelper.ExecuteScalar("select id from [app_notice] where appid='" + token.appid + "' and (uid=''" + (token.nickname == "" ? "" : " or uid like '%" + token.nickname + ",%'") + ")  and kstime<='" + nowtime + "' and jstime>='" + nowtime + "' and state>0 order by id desc");
            if (obj != null)
            {
                maxnoticeid = Int32.Parse(obj.ToString());
            }
            return "{\"status\":true,\"maxnoticeid\":" + maxnoticeid + "}";
        }
        #endregion

        #region 获取所有通知
        public static string GetAllNotice(AsyncUserToken token)
        {
            StringBuilder sb = new StringBuilder("{\"status\":true,\"app_list\":[");
            string nowtime = DateTime.Now.ToString();
            if (token.uid > 0)
            {
                DataTable dt = SqlHelper.ExecuteTable("select id,content,kstime,jstime from [app_notice] where appid='" + token.appid + "' and (uid=''" + (token.nickname == "" ? "" : " or uid like '%" + token.nickname + ",%'") + ")  and kstime<='" + nowtime + "' and jstime>='" + nowtime + "' and state>0 order by id desc");
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (i != 0) sb.Append(",");
                    string id = dt.Rows[i]["id"].ToString();
                    string content = HttpUtility.UrlEncode(dt.Rows[i]["content"].ToString(), Encoding.UTF8);
                    string kstime = dt.Rows[i]["kstime"].ToString();
                    string jstime = dt.Rows[i]["jstime"].ToString();
                    sb.Append("{\"id\":" + dt.Rows[i]["id"].ToString() + ",\"content\":\"" + content + "\",\"kstime\":\"" + kstime + "\",\"jstime\":\"" + jstime + "\"}");
                }
            }
            sb.Append("]}");
            return sb.ToString();
        }
        #endregion

        #region 链路检测
        public static string lineCheck(AsyncUserToken token)
        {
            if (token.uid != 0 && token.appid != 0)
            {
                UserObject uo = null;
                if (MediaService.userDic.TryGetValue(token.uid, out uo))
                {
                    if (token.Socket != uo.socket[token.appid])
                    {
                        token.uid = 0;
                        token.appid = 0;
                        return WriteErrorJson(30);
                    }
                    return "{\"status\":true,\"uid\":" + token.uid + "}";

                }
                else
                {
                    return "{\"status\":true,\"code\":3,\"message\":\"未登录\",\"type\":\"3\",\"ip\":\"" + ((IPEndPoint)(token.Socket.RemoteEndPoint)).Address + "\"}";
                }
            }
            else
            {
                return "{\"status\":true,\"code\":3,\"message\":\"未登录\",\"type\":\"2\",\"ip\":\"" + ((IPEndPoint)(token.Socket.RemoteEndPoint)).Address + "\"}";
            }
        }
        #endregion

        #region 用户注销
        public static string UserExit(AsyncUserToken token)
        {
            string recv = "{\"status\":true}";
            UserObject uo = null;
            if (token.uid != 0 && MediaService.userDic.TryGetValue(token.uid, out uo))
            {
                if (uo.socket[token.appid] != null)
                {
                    uo.socket[token.appid] = null;
                }
                uo.nickname = "";
            }
            token.uid = 0;
            token.appid = 0;
            token.nickname = "";
            return recv;
        }
        #endregion

        #region 登陆命令
        private const string PREFIXSN = "9716";
        public static string userlogin(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("登陆请求：" + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                if (qs != null && qs["__API__[app_key]"] != null && qs["__API__[app_secret]"] != null && qs["__API__[login_key]"] != null && qs["__API__[password]"] != null)
                {
                    string app_key = qs["__API__[app_key]"].ToString().Replace("'", "");
                    string app_secret = qs["__API__[app_secret]"].ToString().Replace("'", "");
                    string login_key = qs["__API__[login_key]"].ToString().Replace("'", "");
                    string password = qs["__API__[password]"].ToString().Replace("'", "");
                    string udpaddress = qs["udpaddress"] == null ? "" : qs["udpaddress"].ToString().Replace("'", "");
                    string ver = (qs["ver"] + "").Replace("'", "");
                    MediaService.WriteLog(string.Format("ver:{0}", ver), true);
                    DataRow[] dr = MediaService.allapp.Select("app_key='" + app_key + "' and app_secret='" + app_secret + "'");
                    if (dr.Length > 0)
                    {
                        int appid = Int32.Parse(dr[0]["id"].ToString());
                        int hearttime = Int32.Parse(dr[0]["hearttime"].ToString());
                        int lolatime = Int32.Parse(dr[0]["lolatime"].ToString());
                        int uid = 0;
                        string username = "";
                        string nickname = string.Empty;
                        int gender = 0;
                        string email = "";
                        string mobile = "";
                        long radiomoditime = 0;
                        string avatar = "";
                        int fm = 8800;
                        int debug = 0;
                        int netpercent = 100;
                        string glsn = "";
                        string zsn = "";
                        long txlmoditime = 0;
                        long wifitime = 0;
                        string pradio = "";
                        long nowtime = GetTimeStamp();
                        string tokenlogin = StringToMD5Hash(DateTime.Now.Ticks.ToString());
                        if (login_key.Length > 0 && password.Length > 0)
                        {
                            string dbpassword = "";
                            if (password.Length > 0)
                            {
                                if (password.Length != 32)
                                    dbpassword = StringToMD5Hash(password).ToLower();
                                else
                                    dbpassword = password;
                            }
                            string key = "username";
                            if ((login_key.Length == 12) && login_key[0] == '9')
                            {
                                #region sn用户
                                key = "glsn";
                                string snkey = password;
                                if (snkey.Length > 32)
                                {
                                    snkey = snkey.Substring(0, 32);
                                }
                                else
                                {
                                    snkey = snkey.PadRight(32, '0');
                                }
                                dbpassword = "";
                                DataTable dtsn = SqlHelper.ExecuteTable("select uid,snkey from[app_users] where glsn='" + login_key + "'");
                                //本地存在
                                if (dtsn.Rows.Count > 0)
                                {
                                    string snkeymap = dtsn.Rows[0]["snkey"].ToString();
                                    if (!snkey.Equals(snkeymap))
                                    {
                                        //不同去远端验证
                                        if (VLogin(login_key, snkey))
                                        {
                                            //验证通过更新本地
                                            if (login_key.Trim().StartsWith(PREFIXSN))
                                            {
                                                zsn = login_key.Substring(login_key.Length - 8);
                                                SqlHelper.ExecuteNonQuery("update [app_users] set username = " + "'sn_" + login_key + "',zsn= '" + zsn + "',snkey= '" + snkey + "' where glsn ='" + login_key + "'");
                                            }
                                            else
                                                SqlHelper.ExecuteNonQuery("update [app_users] set username = " + "'sn_" + login_key + "',snkey= '" + snkey + "' where glsn ='" + login_key + "'");
                                        }
                                    }
                                    //相同验证通过
                                }
                                else//本地不存在
                                {
                                    //去远端验证
                                    if (VLogin(login_key, snkey))
                                    {
                                        if (login_key.Trim().StartsWith(PREFIXSN))
                                        {
                                            zsn = login_key.Substring(login_key.Length - 8);
                                            SqlHelper.ExecuteNonQuery("insert [app_users] (glsn,username,snkey,password,zsn) values ('" + login_key + "','sn_" + login_key + "','" + snkey + "','" + dbpassword + "','" + zsn + "')");
                                        }
                                        else
                                            //验证通过插入本地
                                            SqlHelper.ExecuteNonQuery("insert [app_users] (glsn,username,snkey,password) values ('" + login_key + "','sn_" + login_key + "','" + snkey + "','" + dbpassword + "')");
                                    }
                                }


                                #endregion
                            }
                            else
                            {
                                if (login_key.IndexOf('@') > 0)
                                {
                                    key = "email";
                                }
                                else if (login_key[0] > 47 && login_key[0] < 58)
                                {
                                    key = "mobile";
                                }
                                object obj = SqlHelper.ExecuteScalar("select password from[app_users] where " + key + "='" + login_key + "'");
                                if (obj == null || obj.ToString() != dbpassword)
                                {
                                    #region 深圳请求认证
                                    string str = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=passport_service.login", "login_key=" + login_key + "&password=" + password + "&app_id=2014042900000006&time=" + GetTimeStamp(), "POST", Encoding.UTF8);

                                    MediaService.WriteLog("登录提交至GOLO:" + str, MediaService.wirtelog);
                                    DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(dbscarreturnUser));
                                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(str.Trim())))
                                    {
                                        dbscarreturnUser u = (dbscarreturnUser)json.ReadObject(ms);
                                        if (u.data != null && u.data.user != null)
                                        {
                                            //sztokenlogin = u.data.token;
                                            uid = Int32.Parse(u.data.user.user_id);
                                            username = u.data.user.user_name.Replace("'", "");
                                            nickname = u.data.user.nick_name.Replace("'", "");
                                            email = u.data.user.email.Replace("'", "");
                                            mobile = "";
                                            if (u.data.user.face_url != null)
                                                avatar = u.data.user.face_url.Replace("'", "");

                                            string poststr = "action=userinfo.get_base_info_car_logo&app_id=2014042900000006&lan=zh&user_id=" + uid + "&ver=3.01";
                                            string sign = CommBusiness.StringToMD5Hash(poststr + tokenlogin).ToLower();
                                            string t = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=userinfo.get_base_info_car_logo&app_id=2014042900000006&user_id=" + uid + "&ver=3.01&sign=" + sign, "lan=zh", "POST", Encoding.UTF8); ;
                                            MediaService.WriteLog(" RecvThread接收： t=" + t, MediaService.wirtelog);
                                            if (t.IndexOf("\"is_bind_mobile\":\"1\"") > 0)
                                            {
                                                int k = t.IndexOf("\"mobile\":\"");
                                                int end = t.IndexOf("\"", k + 10);
                                                if (k > 0 && end > 0 && end > k)
                                                {
                                                    mobile = t.Substring(k + 10, end - k - 10);
                                                }
                                            }
                                            if (t.IndexOf("\"is_bind_email\":\"1\"") > 0)
                                            {
                                                email = GetJsonValue(t, "email", "\"", true);
                                            }

                                            #region vog
                                            if (GetJsonValue(t, "code", ",", false).Equals("0"))
                                            {
                                                int auid = Convert.ToInt32(SqlHelper.ExecuteScalar("select uid from wy_uidmap where ouid='" + uid + "';"));
                                                if (auid != 0)
                                                {
                                                    SqlHelper.ExecuteNonQuery("update [app_users] set username='" +
                                                                              username + "',password='" + dbpassword +
                                                                              "',nickname='" + nickname + "',email='" +
                                                                              email + "',mobile='" + mobile +
                                                                              "',avatar='" + avatar + "',token='" +
                                                                              tokenlogin + "' where uid='" +
                                                                              auid + "'");
                                                }
                                                else
                                                {
                                                    glsn = MakeSnNumber(uid);
                                                    MediaService.WriteLog("生成的glsn号：" + glsn, MediaService.wirtelog);

                                                    //不存在插入设备
                                                    StringBuilder sql = new StringBuilder();
                                                    int count = SqlHelper.ExecuteNonQuery("insert [app_users] (username,nickname,email,password,mobile,avatar,token,glsn) values ('" +
                                                          username + "','" + nickname + "','" + email +
                                                          "','" + dbpassword + "','" + mobile + "','" + avatar + "','" +
                                                          tokenlogin + "','" + glsn + "');");
                                                    if (count > 0)
                                                    {
                                                        auid = Convert.ToInt32(SqlHelper.ExecuteScalar("select uid from app_users where glsn='" + glsn + "';"));
                                                        //绑定
                                                        sql.Append("INSERT INTO wy_uidmap(ouid,uid,sim) VALUES(" + uid + "," + auid + ",'');");
                                                        int countInsert = SqlHelper.ExecuteNonQuery(sql.ToString());
                                                    }

                                                }
                                            }
                                            else
                                            {
                                                return WriteErrorJson(45);
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion
                                }
                            }
                            DataTable dt = SqlHelper.ExecuteTable("select uid,username,nickname,gender,email,mobile,avatar,(select max(updatetime) from wy_radio) as radiomoditime,token,fm,netpercent,debug,glsn,updatetime,wifitime,pradio,istalk,istalkreceive,issearch,iswifi from[app_users] where " + key + "='" + login_key + "' and password='" + dbpassword + "'");
                            if (dt.Rows.Count > 0)
                            {
                                uid = Int32.Parse(dt.Rows[0]["uid"].ToString());
                                username = dt.Rows[0]["username"].ToString().Trim();
                                nickname = dt.Rows[0]["nickname"].ToString().Trim() ?? nickname;
                                gender = Int32.Parse(dt.Rows[0]["gender"].ToString());
                                email = dt.Rows[0]["email"].ToString().Trim();
                                mobile = dt.Rows[0]["mobile"].ToString();
                                avatar = dt.Rows[0]["avatar"].ToString();
                                fm = Int32.Parse(dt.Rows[0]["fm"].ToString());
                                netpercent = Int32.Parse(dt.Rows[0]["netpercent"].ToString());
                                debug = Int32.Parse(dt.Rows[0]["debug"].ToString());
                                wifitime = Int32.Parse(dt.Rows[0]["wifitime"].ToString());
                                radiomoditime = DateTime.Parse(dt.Rows[0]["radiomoditime"].ToString()).ToUniversalTime().Ticks / 10000000 - 62135596800; //Int32.Parse(dt.Rows[0]["radiomoditime"].ToString());
                                glsn = dt.Rows[0]["glsn"].ToString();
                                pradio = dt.Rows[0]["pradio"].ToString();
                                txlmoditime = DateTime.Parse(dt.Rows[0]["updatetime"].ToString()).ToUniversalTime().Ticks / 10000000 - 62135596800;
                                int istalk = Convert.ToInt32("0" + dt.Rows[0]["istalk"]);
                                int istalkreceive = Convert.ToInt32("0" + dt.Rows[0]["istalkreceive"]);
                                int issearch = Convert.ToInt32("0" + dt.Rows[0]["issearch"]);
                                int iswifi = Convert.ToInt32("0" + dt.Rows[0]["iswifi"]);

                                int ouid = 0;
                                if (!string.IsNullOrWhiteSpace(glsn))
                                {
                                    try
                                    {
                                        string sql = "SELECT TOP 1 ouid FROM [dbo].[wy_uidmap] WHERE [uid]=" + uid;
                                        DataTable dtMap = SqlHelper.ExecuteTable(sql);
                                        if (dtMap != null && dtMap.Rows.Count > 0)
                                        {
                                            int.TryParse(dtMap.Rows[0]["ouid"].ToString(), out ouid);
                                            int oldOuid;
                                            if (MediaService.mapDic.TryGetValue(uid, out oldOuid))
                                            {
                                                if (ouid != oldOuid)
                                                {
                                                    MediaService.mapDic[uid] = ouid;
                                                }
                                            }
                                            else
                                            {
                                                MediaService.mapDic.TryAdd(uid, ouid);
                                            }
                                        }
                                        if (string.IsNullOrWhiteSpace(nickname) && ouid != 0)
                                        {
                                            string poststr = "action=userinfo.get_base_info_car_logo&app_id=2014042900000006&lan=zh&user_id=" + ouid + "&ver=3.01";
                                            string sign = StringToMD5Hash(poststr + tokenlogin).ToLower();
                                            poststr += "&sign=" + sign;
                                            string str = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=userinfo.get_base_info_car_logo&app_id=2014042900000006&user_id=" + ouid + "&ver=3.01&sign=" + sign, "lan=zh", "POST", Encoding.UTF8);

                                            if (GetJsonValue(str, "code", ",", false) == "0")
                                            {
                                                nickname = GetJsonValue(str, "nick_name", "\"", true);
                                            }
                                            MediaService.WriteLog("ouid=" + ouid + " 远程查询:" + poststr + " 返回:" + nickname, MediaService.wirtelog);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MediaService.WriteLog("查询nickname或ouid出错：" + ex.Message, MediaService.wirtelog);
                                    }
                                }

                                int navsetting = 0;
                                try
                                {
                                    object nav = SqlHelper.ExecuteScalar("SELECT navsetting FROM app_users WHERE [uid]=" + uid);
                                    if (nav != null)
                                    {
                                        string navSet = nav.ToString();
                                        int.TryParse(navSet, out navsetting);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MediaService.WriteLog("Get navsetting error: " + ex, MediaService.wirtelog);
                                }

                                UserLoginJson userLoginJson = new UserLoginJson(true, uid, username, nickname, gender, email, mobile, MediaService.bufferSize, hearttime, lolatime, appid, tokenlogin, fm, netpercent, debug, MediaService.cachetime, MediaService.micover, nowtime, glsn, txlmoditime, wifitime, radiomoditime, istalk, istalkreceive, issearch, iswifi, ouid, navsetting);
                                DataContractJsonSerializer json = new DataContractJsonSerializer(userLoginJson.GetType());
                                using (MemoryStream stream = new MemoryStream())
                                {
                                    json.WriteObject(stream, userLoginJson);
                                    recv = Encoding.UTF8.GetString(stream.ToArray());
                                }

                                UserObject uo = null;
                                if (token.uid != 0 && token.uid != uid) //当前连接换用户登录
                                {
                                    if (MediaService.userDic.TryGetValue(token.uid, out uo))
                                    {
                                        if (uo.socket != null && uo.socket[appid] != null)
                                        {
                                            uo.socket[appid] = null;
                                        }
                                    }
                                }
                                //用户绑定
                                token.appid = appid;
                                token.nickname = nickname;
                                token.uid = uid;
                                token.udpaddress = udpaddress;
                                token.praido = pradio;
                                token.glsn = 0;
                                token.radiomoditime = radiomoditime;
                                if (glsn != "")
                                {
                                    try
                                    {
                                        if (glsn.Substring(0, 4) == "9716" || glsn.Substring(0, 4) == "9711")
                                        {
                                            zsn = glsn.Substring(glsn.Length - 8);
                                        }
                                        else
                                        {
                                            zsn = "6" + glsn.Substring(glsn.Length - 7);
                                        }
                                        token.glsn = Convert.ToInt32(zsn.Trim());
                                        token.prefixsn = Convert.ToInt32(glsn.Substring(0, 4));
                                    }
                                    catch { }
                                }

                                if (MediaService.userDic.TryGetValue(uid, out uo))  //登录成功的当前用户是否在其它设备登录
                                {
                                    try
                                    {
                                        if (uo.socket[appid] != null)
                                        {
                                            if (uo.socket[appid] != token.Socket)
                                            {
                                                byte[] cbyte = Encoding.UTF8.GetBytes(WriteErrorJson(30));
                                                Buffer.BlockCopy(cbyte, 0, token.buffer, 8, cbyte.Length);
                                                Buffer.BlockCopy(System.BitConverter.GetBytes((short)(cbyte.Length + 8)), 0, token.buffer, 0, 2);
                                                Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.lineCheck), 0, token.buffer, 2, 2);
                                                uo.socket[appid].Send(token.buffer, 0, cbyte.Length + 8, SocketFlags.None);
                                            }
                                        }
                                    }
                                    catch { }
                                }
                                else
                                {
                                    uo = new UserObject(MediaService.maxappid);
                                }
                                uo.socket[appid] = token.Socket;
                                uo.nickname = nickname;
                                uo.udpaddress = udpaddress;
                                IPEndPoint clientipe = (IPEndPoint)token.Socket.RemoteEndPoint;
                                uo.ip = clientipe.Address.ToString();
                                uo.token[appid] = tokenlogin;
                                Int32.TryParse(ver, out uo.ver);
                                MediaService.userDic.AddOrUpdate(uid, uo, (uidkey, oldValue) => uo);
                                CommAction.NoticeUserLineState(uid, appid, 1);
                            }
                            else
                            {
                                recv = WriteErrorJson(4);
                            }
                        }
                        else
                        {

                            recv = WriteErrorJson(18);
                        }
                    }
                    else
                    {
                        recv = WriteErrorJson(29);
                    }
                }
                else
                {
                    recv = WriteErrorJson(11);
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                recv = WriteErrorJson(6);
            }
            return recv;
        }

        /// <summary>
        /// 生成sn
        /// </summary>
        /// <param name="version">版本号</param>
        /// <returns></returns>
        private static string MakeSnNumber(int uid)
        {
            string sn = "";
            if (uid.ToString().Length == 7)
            {
                sn = "97117" + uid;
            }
            else if (uid.ToString().Length < 7)
            {
                sn = "97117" + uid.ToString().PadLeft(7, '0');
            }
            else if (uid.ToString().Length > 7)
            {
                string subNum = uid.ToString().Substring(0, 7);
                sn = "97117" + subNum;
            }
            else
                sn = "97117" + MakeRandomString(7);
            return sn;
        }
        /// <summary>
        /// 生成随机数字符串
        /// </summary>
        /// <param name="length">字符串长度</param>
        /// <returns></returns>
        private static string MakeRandomString(int length)
        {
            Random r = new Random();
            string result = "";
            for (int i = 0; i < length; i++)
            {
                result += r.Next(10);
            }
            return result;
        }
        #endregion

        #region 第三方绑定
        public static string OtherUserBind(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("登陆请求：" + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                if (qs != null && qs["__API__[app_key]"] != null && qs["__API__[app_secret]"] != null && qs["qq_key"] != null)
                {
                    string app_key = qs["__API__[app_key]"].ToString().Replace("'", "");
                    string app_secret = qs["__API__[app_secret]"].ToString().Replace("'", "");
                    string qq_key = qs["qq_key"].ToString().Replace("'", "");

                    DataRow[] dr = MediaService.allapp.Select("app_key='" + app_key + "' and app_secret='" + app_secret + "'");
                    if (dr.Length > 0)
                    {
                        DataTable dt = SqlHelper.ExecuteTable("select uid,username,passwordm from[app_users] where qq_key='" + qq_key + "'");
                        if (dt.Rows.Count > 0)
                        {
                            SqlHelper.ExecuteNonQuery("update [app_users] set password='" + StringToMD5Hash(dt.Rows[0]["passwordm"].ToString()) + "' where uid='" + dt.Rows[0]["uid"].ToString() + "'");
                            return "{\"status\":true,\"username\":\"" + dt.Rows[0]["username"].ToString() + "\",\"passwordm\":\"" + dt.Rows[0]["passwordm"].ToString() + "\"}";
                        }
                        else
                        {
                            string ssss = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=passport_service.reg_user", "app_id=2014042900000006", "POST", Encoding.UTF8);
                            int k = ssss.IndexOf("\"uname\":\"");
                            if (k > 0)
                            {
                                int end = ssss.IndexOf("\"", k + 9);
                                if (end > 0)
                                {
                                    string uname = ssss.Substring(k + 9, end - k - 9);
                                    k = ssss.IndexOf("\"password\":\"");
                                    end = ssss.IndexOf("\"", k + 12);
                                    string password = ssss.Substring(k + 12, end - k - 12);
                                    k = ssss.IndexOf("\"user_id\":");
                                    end = ssss.IndexOf(",", k + 10);
                                    string user_id = ssss.Substring(k + 10, end - k - 10);
                                    if (uname != "" && password != "" && user_id != "") //通过UID获取个人信息
                                    {
                                        SqlHelper.ExecuteNonQuery("insert [app_users] (uid,username,password,passwordm,qq_key) values ('" + user_id + "','" + uname + "','" + StringToMD5Hash(password) + "','" + password + "','" + qq_key + "')");
                                        return "{\"status\":true,\"username\":\"" + uname + "\",\"passwordm\":\"" + password + "\"}";
                                    }
                                }
                            }
                        }
                    }
                }
                return WriteErrorJson(32);
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 获取用户Token
        public static string GetUserToken(AsyncUserToken token, int packnum)
        {
            string recv = WriteErrorJson(6);
            try
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("获取用户Token" + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                if (qs != null && qs["login_key"] != null && qs["password"] != null)
                {
                    string login_key = qs["login_key"].ToString().Replace("'", "");
                    string password = qs["password"].ToString().Replace("'", "");

                    string str = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=passport_service.login", "login_key=" + login_key + "&password=" + password + "&app_id=2014042900000006&time=" + GetTimeStamp(), "POST", Encoding.UTF8);
                    DataContractJsonSerializer json = new DataContractJsonSerializer(typeof(dbscarreturnUser));
                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(str.Trim())))
                    {
                        dbscarreturnUser u = (dbscarreturnUser)json.ReadObject(ms);
                        if (u.data != null && u.data.user != null)
                        {
                            recv = "{\"status\":true,\"token\":" + u.data.token + "}";
                        }
                    }
                }
                else
                {
                    recv = WriteErrorJson(11);
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 获取用户头像
        public static string GetUserFace(AsyncUserToken token, int packnum)
        {
            int uid = System.BitConverter.ToInt32(token.buffer, 8);
            int type = token.buffer[12];
            MediaService.WriteLog("获取用户头像：uid " + uid, MediaService.wirtelog);
            string faceurl = GetUserFaceUrl(uid);
            if (!File.Exists(faceurl + "_0.jpg"))
            {
                string u = uid.ToString().PadLeft(16, '0');
                try
                {
                    string dir = faceurl.Remove(faceurl.Length - 2);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    WebClient wbc = new WebClient();
                    wbc.DownloadFile("http://file.api.dbscar.com/face/" + u.Substring(0, 2) + "/" + u.Substring(2, 2) + "/" + u.Substring(4, 2) + "/" + u.Substring(6, 2) + "/" + u.Substring(8, 2) + "/" + u.Substring(10, 2) + "/" + u.Substring(12, 2) + "/" + u.Substring(14, 2) + "/" + uid, faceurl + "_0.jpg");
                    MakeThumbnail(faceurl + "_0.jpg", faceurl + "_0.jpg", 200, 200, 2, "jpg");
                }
                catch (Exception err)
                {
                    faceurl = MediaService.shareurl + "face";
                    MediaService.WriteLog("下载服务器头像失败：" + err.ToString() + "   " + "http://file.api.dbscar.com/face/" + u.Substring(0, 2) + "/" + u.Substring(2, 2) + "/" + u.Substring(4, 2) + "/" + u.Substring(6, 2) + "/" + u.Substring(8, 2) + "/" + u.Substring(10, 2) + "/" + u.Substring(12, 2) + "/" + u.Substring(14, 2) + "/" + uid, MediaService.wirtelog);
                }
            }
            if (File.Exists(faceurl + "_0.jpg"))
            {
                if (!File.Exists(faceurl + "_" + type + ".jpg"))
                {
                    MakeThumbnail(faceurl + "_0.jpg", faceurl + "_" + type + ".jpg", type == 1 ? 100 : 50, type == 1 ? 100 : 50, 2, "jpg");
                }
                try
                {
                    FileStream fs = File.OpenRead(faceurl + "_" + type + ".jpg");
                    int flen = (int)fs.Length;
                    Buffer.BlockCopy(System.BitConverter.GetBytes(flen), 0, token.buffer, 12, 4);
                    while (true)
                    {
                        int len = fs.Read(token.buffer, 16, MediaService.bufferSize - 16);
                        if (len == 0) break;
                        Buffer.BlockCopy(System.BitConverter.GetBytes((short)len + 16), 0, token.buffer, 0, 2);
                        token.Socket.Send(token.buffer, 0, len + 16, SocketFlags.None);
                    }
                    return null;
                }
                catch { }
            }

            Buffer.BlockCopy(System.BitConverter.GetBytes((int)0), 0, token.buffer, 12, 4);
            byte[] cbyte = Encoding.UTF8.GetBytes(WriteErrorJson(8));
            Buffer.BlockCopy(cbyte, 0, token.buffer, 16, cbyte.Length);
            Buffer.BlockCopy(System.BitConverter.GetBytes((short)(cbyte.Length + 16)), 0, token.buffer, 0, 2);
            token.Socket.Send(token.buffer, 0, cbyte.Length + 16, SocketFlags.None);
            return null;
        }
        #endregion

        #region 获取代码对应的信息
        public static string GetCodeToMessagList(AsyncUserToken token, int packnum)
        {
            string recv = null;
            try
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("获取代码对应的信息：" + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                if (qs != null && qs["__API__[app_key]"] != null && qs["__API__[app_secret]"] != null)
                {
                    string app_key = qs["__API__[app_key]"].ToString().Replace("'", "");
                    string app_secret = qs["__API__[app_secret]"].ToString().Replace("'", "");

                    DataRow[] dr = MediaService.allapp.Select("app_key='" + app_key + "' and app_secret='" + app_secret + "'");
                    if (dr.Length > 0)
                    {
                        string lang = dr[0]["lang"].ToString().Replace(',', ' ');
                        DataTable dt = SqlHelper.ExecuteTable("select cid," + lang + " from [app_codetomsg]");

                        ErrorCodeJson errorCode = new ErrorCodeJson(true, dt);
                        DataContractJsonSerializer json = new DataContractJsonSerializer(errorCode.GetType());
                        using (MemoryStream stream = new MemoryStream())
                        {
                            json.WriteObject(stream, errorCode);
                            recv = Encoding.UTF8.GetString(stream.ToArray());
                        }
                    }
                    else
                    {
                        MediaService.WriteLog("获取所有的错误代码处理异常：应用不存在", MediaService.wirtelog);
                    }
                }
                else
                {
                    MediaService.WriteLog("获取所有的错误代码处理异常：参数不正确", MediaService.wirtelog);
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("获取所有的错误代码处理异常：" + err.ToString(), MediaService.wirtelog);
            }
            return recv;
        }
        #endregion

        #region 获取注册验证码
        public static string GetRegistCode(AsyncUserToken token, int packnum)
        {
            string recv = "";
            try
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("请求：" + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                if (qs != null && qs["req_info"] != null)
                {
                    string req_info = qs["req_info"].ToString().Replace("'", "");
                    string str = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=verifycode.req_send_code&app_id=2014042900000006&ver=3.0.16", "lan=zh&req_info=" + req_info + "&isres=1", "POST", Encoding.UTF8);
                    int s = str.IndexOf("\"verifycode\":\"");
                    if (s > 0)
                    {
                        int e = str.IndexOf('}', s);
                        if (e > 0)
                        {
                            recv = "{\"status\":true," + str.Substring(s, e - s) + "}";
                        }
                        else
                        {
                            recv = WriteErrorJson(11);
                        }
                    }
                    else
                    {
                        recv = WriteErrorJson(5);
                    }
                }
                else
                {
                    recv = WriteErrorJson(7);
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog); recv = WriteErrorJson(6);
            }
            return recv;
        }
        #endregion

        #region 应用路由请求转发
        public static string UrlRoute(AsyncUserToken token, int packnum)
        {
            StringBuilder recv = new StringBuilder();
            string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
            MediaService.WriteLog("应用路由请求转发：" + query, MediaService.wirtelog);
            NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);

            if (qs != null && qs["serversname"] != null)
            {
                IPEndPoint clientipe1 = (IPEndPoint)token.Socket.RemoteEndPoint;
                string serversname = qs["serversname"].ToString().Replace("'", "");
                string postquery = qs["query"] == null ? "" : qs["query"].ToString().Replace("'", "");
                string requesttype = qs["type"] == null ? "GET" : qs["type"].ToString();
                object obj = SqlHelper.ExecuteScalar("select s_url from [app_servers] where s_servers='" + serversname + "'");
                if (obj != null)
                {
                    if (postquery != "")
                    {
                        postquery += '&';
                    }
                    postquery += "uid=" + token.uid + "&ip=" + clientipe1.Address;
                    string str = "";
                    if (qs["url"] == null)
                    {
                        str = HttpRequestRoute(obj.ToString(), postquery, requesttype, Encoding.UTF8);
                    }
                    else
                    {
                        string interfaceurl = qs["url"].ToString().Replace("'", "");
                        str = HttpRequestRoute(obj.ToString() + '/' + interfaceurl, postquery, requesttype, Encoding.UTF8);
                    }
                    if (str != "")
                    {
                        recv.Append("{\"status\":true,\"recv\":");
                        recv.Append(str);
                        recv.Append("}");
                        return recv.ToString();
                    }
                    else
                    {
                        return WriteErrorJson(32);
                    }
                }
                else
                {
                    return WriteErrorJson(31);
                }
            }
            else
            {
                return WriteErrorJson(11);
            }
        }
        #endregion

        #region 获取服务的地址
        public static string GetServiceUrl(AsyncUserToken token, int packnum)
        {
            StringBuilder recv = new StringBuilder("{\"status\":true,\"url\":\"");
            string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
            MediaService.WriteLog("1028 获取服务对用的url地址：" + " uid:" + token.uid + " sn:" + token.glsn + " url地址:" + query, MediaService.wirtelog);
            NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);

            if (qs != null && qs["serversname"] != null)
            {
                string serversname = qs["serversname"].ToString().Replace("'", "");
                object obj = SqlHelper.ExecuteScalar("select s_url from [app_servers] where s_servers='" + serversname + "'");
                if (obj != null)
                {
                    recv.Append(obj.ToString());
                    recv.Append("?");
                    if (qs["query"] != null)
                    {
                        string q = qs["query"].ToString().Replace("'", "");
                        if (q != "")
                        {
                            MediaService.WriteLog("获取服务query参数：" + HttpUtility.UrlDecode(q, Encoding.UTF8), MediaService.wirtelog);
                            recv.Append(q);
                            recv.Append("&");
                        }
                    }
                    recv.Append("uid=");
                    recv.Append(token.uid);
                    recv.Append("&token=");
                    UserObject uo = null;
                    if (token.uid != 0 && MediaService.userDic.TryGetValue(token.uid, out uo))
                    {
                        if (uo.token[token.appid] != null)
                        {
                            recv.Append(uo.token[token.appid]);
                        }
                    }
                    recv.Append("\"}");
                    return recv.ToString();
                }
                else
                {
                    return WriteErrorJson(31, "您所请求的服务不存在");
                }
            }
            else
            {
                return WriteErrorJson(11, "请求的格式不正确！");
            }
        }
        #endregion

        #region 客户端发送文件
        public static string SendUserUpdateFile(AsyncUserToken token, int packnum)
        {
            string recv = null;
            ulong datetime = System.BitConverter.ToUInt64(token.buffer, 8);
            int state = token.buffer[16];
            StringBuilder sb = GetFileID(token.uid, datetime);
            string fid = sb.ToString();
            string file = GetFileIdDir(sb);
            if (state == 0)
            {
                if (token.uid != 0)
                {
                    string filedir = file.Substring(0, file.LastIndexOf('/'));
                    if (!Directory.Exists(filedir))
                    {
                        Directory.CreateDirectory(filedir);
                    }
                    FileStream fs = File.Open(file, FileMode.Append);
                    fs.Write(token.buffer, 17, packnum - 17);
                    fs.Close();
                }
            }
            else
            {
                if (token.uid != 0)
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 17, packnum - 17);
                    MediaService.WriteLog("上传文件完成：" + query, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    if (qs != null && qs["rid"] != null && qs["type"] != null)
                    {
                        string ext = qs["ext"] == null ? "jpg" : qs["ext"].ToLower();
                        string last = qs["rid"].ToString().PadLeft(10, '0') + qs["type"].ToString().PadLeft(2, '0');
                        try
                        {
                            string filename = file + last + "." + ext;
                            File.Move(file, filename);
                            if (ext == "mp4")
                            {
                                VideoToThumbnailPic(filename, file + last + ".jpg");
                            }
                            recv = "{\"status\":true,\"fid\":\"" + fid + last + "\"}";
                        }
                        catch (Exception err)
                        {
                            MediaService.WriteLog("文件写入权限失败：fid =" + fid + last + "   " + err.Message, MediaService.wirtelog);
                            recv = WriteErrorJson(33);
                        }
                    }
                    else
                    {
                        recv = WriteErrorJson(11);
                    }
                }
                else
                {
                    recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
                }
            }
            return recv;
        }
        #endregion

        #region 客户端下载文件
        public static string GetUserUploadFile(AsyncUserToken token, int packnum)
        {
            string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
            MediaService.WriteLog("请求下载文件：" + query, MediaService.wirtelog);
            NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
            if (qs != null && qs["fid"] != null)
            {
                string fid = qs["fid"].ToString().Trim();
                if (fid.Length == 37)
                {
                    int width = (qs["width"] == null || qs["width"].ToString() == "") ? 0 : Int32.Parse(qs["width"].ToString());
                    int smalltype = qs["smalltype"] == null ? 0 : Int32.Parse(qs["smalltype"].ToString());   //1.指定宽，高按比例　2,指定宽裁减成正方形
                    string ext = qs["ext"] == null ? "jpg" : qs["ext"].ToString();

                    int type = Int32.Parse(fid.Substring(fid.Length - 2));
                    int suid = Int32.Parse(fid.Substring(3, 9));
                    int rid = Int32.Parse(fid.Substring(25, 9));
                    object obj = null;
                    if (type == 5 || type == 4)
                    {
                        obj = SqlHelper.ExecuteScalar("select id from [wy_talkuser] where tid=" + rid + " and uid=" + token.uid);
                    }
                    else if (type == 3)
                    {
                        if (token.uid == suid || token.uid == rid)
                        {
                            obj = 0;
                        }
                    }
                    else
                    {
                        obj = 0;
                    }
                    if (obj != null)
                    {
                        string filePath = GetFileIdDir(fid);
                        string ofileUrl = filePath + "." + ext;
                        try
                        {
                            if (File.Exists(ofileUrl))
                            {
                                if (width != 0)
                                {
                                    string smallfileurl = filePath + ("_" + width + "_" + smalltype) + "." + ext;
                                    if (!File.Exists(smallfileurl))
                                    {
                                        MakeThumbnail(ofileUrl, smallfileurl, width, width, smalltype, ext);
                                    }
                                    ofileUrl = smallfileurl;
                                }
                                MediaService.WriteLog("用户下载文件：fid=" + fid + "  ofileUrl=" + ofileUrl, MediaService.wirtelog);
                                FileStream fs = File.OpenRead(ofileUrl);
                                int flen = (int)fs.Length;
                                Buffer.BlockCopy(System.BitConverter.GetBytes(flen), 0, token.buffer, 8, 4);
                                while (true)
                                {
                                    int len = fs.Read(token.buffer, 12, MediaService.bufferSize - 12);
                                    if (len < 1) break;
                                    Buffer.BlockCopy(System.BitConverter.GetBytes((short)len + 12), 0, token.buffer, 0, 2);
                                    token.Socket.Send(token.buffer, 0, len + 12, SocketFlags.None);
                                }
                                fs.Close();
                                MediaService.WriteLog("用户下载文件完毕：fid=" + fid + "  ofileUrl=" + ofileUrl, MediaService.wirtelog);
                                return null;
                            }
                        }
                        catch (Exception err)
                        {
                            MediaService.WriteLog("用户下载文件：" + err.ToString(), MediaService.wirtelog);
                        }
                    }
                }
            }
            Buffer.BlockCopy(System.BitConverter.GetBytes((int)0), 0, token.buffer, 8, 4);
            byte[] cbyte = Encoding.UTF8.GetBytes(WriteErrorJson(8));
            Buffer.BlockCopy(cbyte, 0, token.buffer, 12, cbyte.Length);
            Buffer.BlockCopy(System.BitConverter.GetBytes((short)(cbyte.Length + 12)), 0, token.buffer, 0, 2);
            token.Socket.Send(token.buffer, 0, cbyte.Length + 12, SocketFlags.None);
            return null;
        }
        #endregion

        #region 设置用户IOS Token
        public static string SetIosUserToken(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("请求设置用户IOS Token：" + query, MediaService.wirtelog);
                NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                if (qs != null && qs["token"] != null)
                {
                    string iostoken = qs["token"].Replace("'", "");
                    DataTable dt = SqlHelper.ExecuteTable("select uid,msgnum from [app_ios_token] where token='" + iostoken + "' and appid=" + token.appid);
                    if (dt.Rows.Count > 0)
                    {
                        int uid = Int32.Parse(dt.Rows[0][0].ToString());
                        int msgnum = Int32.Parse(dt.Rows[0][1].ToString());
                        if (uid != token.uid || msgnum != 0)
                        {
                            SqlHelper.ExecuteNonQuery("update [app_ios_token] set uid=" + token.uid + ",msgnum=0 where token='" + iostoken + "' and appid=" + token.appid);
                        }
                    }
                    else
                    {
                        SqlHelper.ExecuteNonQuery("insert [app_ios_token] (uid,appid,token) values (" + token.uid + "," + token.appid + ",'" + iostoken + "')");
                    }
                    recv = "{\"status\":true}";
                }
                else
                {
                    recv = WriteErrorJson(11);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 客户端获取车型列表
        public static string GetCarList(AsyncUserToken token)
        {
            try
            {
                StringBuilder sb = new StringBuilder("{\"status\":true,\"list\":[");
                DataTable dt = SqlHelper.ExecuteTable("select series_id, auto_code, soft_name from [app_car_list]");
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    if (i != 0) sb.Append(",");
                    string series_id = dt.Rows[i]["series_id"].ToString();
                    string auto_code = dt.Rows[i]["auto_code"].ToString();
                    string soft_name = dt.Rows[i]["soft_name"].ToString();
                    string car_image = dt.Rows[i]["series_id"].ToString();
                    sb.Append("{\"series_id\":" + series_id + ",\"auto_code\":\"" + auto_code + "\",\"soft_name\":\"" + soft_name + "\",\"car_image\":" + car_image + "}");
                }
                sb.Append("]}");
                return sb.ToString();
            }
            catch
            {
                return WriteErrorJson(6);
            }
        }
        #endregion

        #region 客户端获取车型图标
        public static string UserDownCarFile(AsyncUserToken token, int packnum)
        {
            int carid = System.BitConverter.ToInt32(token.buffer, 8);
            MediaService.WriteLog("获取车型图标：carid = " + carid, MediaService.wirtelog);
            if (File.Exists(MediaService.shareurl + "car_ico/" + carid + ".png"))
            {
                FileStream fs = File.OpenRead(MediaService.shareurl + "car_ico/" + carid + ".png");
                int flen = (int)fs.Length;
                Buffer.BlockCopy(System.BitConverter.GetBytes(flen), 0, token.buffer, 12, 4);
                while (true)
                {
                    int len = fs.Read(token.buffer, 16, MediaService.bufferSize - 16);
                    if (len == 0) break;
                    Buffer.BlockCopy(System.BitConverter.GetBytes((short)len + 16), 0, token.buffer, 0, 2);
                    token.Socket.Send(token.buffer, 0, len + 16, SocketFlags.None);
                }
                return null;
            }
            Buffer.BlockCopy(System.BitConverter.GetBytes((int)0), 0, token.buffer, 12, 4);
            byte[] cbyte = Encoding.UTF8.GetBytes(WriteErrorJson(8));
            Buffer.BlockCopy(cbyte, 0, token.buffer, 16, cbyte.Length);
            Buffer.BlockCopy(System.BitConverter.GetBytes((short)(cbyte.Length + 16)), 0, token.buffer, 0, 2);
            token.Socket.Send(token.buffer, 0, cbyte.Length + 16, SocketFlags.None);
            return null;
        }
        #endregion

        //电台相关

        #region 获取我的区号频道列表
        public static string UserGetMyAllChannel(AsyncUserToken token)
        {
            #region Old
            //int i = 0;
            //StringBuilder sb = new StringBuilder("{\"status\":true,\"radiolist\":[");

            //try
            //{
            //    int maxid = System.BitConverter.ToInt32(token.buffer, 8);
            //    //获取媒体频道列表
            //    int areaid = System.BitConverter.ToInt32(token.buffer, 12);

            //    IEnumerable<KeyValuePair<int, RadioObject>> radiolist = MediaService.radioDic.Skip(maxid);

            //    foreach (var item in radiolist)
            //    {
            //        int rid = item.Key;
            //        RadioObject ro = (RadioObject)item.Value;
            //        if (ro.areaid == areaid || ro.areaid == 0)
            //        {
            //            string senduid = "0";
            //            if (ro.sendtype == 1)
            //            {
            //                senduid = (ro.senduid != null && ro.senduid.Contains(token.uid)) ? "0" : "-1";
            //            }
            //            else if (ro.sendtype == 2)
            //            {
            //                senduid = (ro.senduid != null && ro.senduid.Contains(token.uid)) ? "-1" : "0";
            //            }
            //            else
            //            {
            //                senduid = ro.sendtype.ToString();
            //            }
            //            if (ro.radiotype == 0 || ro.radiotype == 1)
            //            {
            //                sb.Append("{\"rid\":\"" + rid + "\",\"channelname\":\"" + ro.channelname + "\",\"cityname\":\"" + ro.cityname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":" + ro.radiotype + ",\"imageurl\":\"" + ro.imageurl + "\",\"thumburl\":\"" + ro.thumburl + "\"},");
            //            }
            //            else
            //            {
            //                if (token.praido != "")
            //                {
            //                    string pradip = token.praido + ",";
            //                    if (pradip.IndexOf(rid + ",") >= 0)
            //                    {
            //                        sb.Append("{\"rid\":\"" + rid + "\",\"channelname\":\"" + ro.channelname + "\",\"cityname\":\"" + ro.cityname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":1,\"imageurl\":\"" + ro.imageurl + "\",\"thumburl\":\"" + ro.thumburl + "\"},");
            //                    }
            //                }
            //            }
            //        }
            //        i++;
            //        if (i >= 100) break;
            //    }
            //    if (sb.ToString().EndsWith(","))
            //    {
            //        sb.Remove(sb.Length - 1, 1);
            //    }
            //    if (i < 100) i = -1;
            //    else i += maxid;
            //    sb.Append("],\"maxid\":" + i + ",\"radiomodilist\":" + token.radiomoditime + "}");
            //}
            //catch (Exception err)
            //{
            //    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
            //    return WriteErrorJson(6);
            //}
            //return sb.ToString();
            #endregion
            int i = 0;
            StringBuilder sb = new StringBuilder("{\"status\":true,\"radiolist\":[");
            try
            {
                int maxid = System.BitConverter.ToInt32(token.buffer, 8);
                //获取媒体频道列表
                int areaid = System.BitConverter.ToInt32(token.buffer, 12);
                MediaService.WriteLog("1200 获取我的区号频道列表:maxid=" + maxid + "&areaid=" + areaid + ",uid=" + token.uid + ",sn=" + token.glsn, MediaService.wirtelog);
                IEnumerable<KeyValuePair<int, RadioObject>> radiolist = MediaService.radioDic.Skip(maxid);

                foreach (var item in radiolist)
                {
                    int rid = item.Key;
                    RadioObject ro = (RadioObject)item.Value;
                    if (ro.areaid == areaid || ro.areaid == 0)
                    {
                        string senduid = "0";
                        if (ro.sendtype == 1)
                        {
                            senduid = (ro.senduid != null && ro.senduid.Contains(token.uid)) ? "0" : "-1";
                        }
                        else if (ro.sendtype == 2)
                        {
                            senduid = (ro.senduid != null && ro.senduid.Contains(token.uid)) ? "-1" : "0";
                        }
                        else
                        {
                            senduid = ro.sendtype.ToString();
                        }
                        if (ro.radiotype == 0 || ro.radiotype == 1 || ro.radiotype == 3)
                        {
                            if (ro.prid == 0)
                                sb.Append("{\"rid\":\"" + rid + "\",\"channelname\":\"" + ro.channelname + "\",\"cityname\":\"" + ro.cityname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":" + ro.radiotype + ",\"imageurl\":\"" + ro.imageurl + "\",\"thumburl\":\"" + ro.thumburl + "\",\"flashimageurl\":\"" + ro.flashimageurl + "\"},");
                            else
                            {
                                object p = MediaService.radioDic.FirstOrDefault(x => x.Key == ro.prid);
                                if (p == null)
                                    sb.Append("{\"rid\":\"" + rid + "\",\"channelname\":\"" + ro.channelname + "\",\"cityname\":\"" + ro.cityname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":" + ro.radiotype + ",\"imageurl\":\"" + ro.imageurl + "\",\"thumburl\":\"" + ro.thumburl + "\",\"flashimageurl\":\"" + ro.flashimageurl + "\"},");
                                else
                                {
                                    try
                                    {
                                        RadioObject prov = ((KeyValuePair<int, RadioObject>)p).Value;
                                        sb.Append("{\"rid\":\"" + ro.prid + "\",\"channelname\":\"" + prov.channelname + "\",\"cityname\":\"" + ro.cityname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + prov.audiourl + "\",\"channelde\":" + prov.channelde + ",\"radiotype\":" + prov.radiotype + ",\"imageurl\":\"" + prov.imageurl + "\",\"thumburl\":\"" + prov.thumburl + "\",\"flashimageurl\":\"" + prov.flashimageurl + "\"},");
                                    }
                                    catch
                                    {
                                        MediaService.WriteLog("执行异常：rid:" + rid + "-prid:" + ro.prid, MediaService.wirtelog);
                                        return WriteErrorJson(6);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (token.praido != "")
                            {
                                string pradip = token.praido + ",";
                                if (pradip.IndexOf(rid + ",") >= 0)
                                {
                                    sb.Append("{\"rid\":\"" + rid + "\",\"channelname\":\"" + ro.channelname + "\",\"cityname\":\"" + ro.cityname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":1,\"imageurl\":\"" + ro.imageurl + "\",\"thumburl\":\"" + ro.thumburl + "\",\"flashimageurl\":\"" + ro.flashimageurl + "\"},");
                                }
                            }
                        }
                    }
                    i++;
                    if (i >= 100) break;
                }
                if (sb.ToString().EndsWith(","))
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                if (i < 100) i = -1;
                else i += maxid;
                sb.Append("],\"maxid\":" + i + ",\"radiomodilist\":" + token.radiomoditime + "}");
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                return WriteErrorJson(6);
            }
            return sb.ToString();
        }
        #endregion

        #region 获取我的频道列表
        public static string GetMyAllChannel(AsyncUserToken token)
        {
            StringBuilder sb = new StringBuilder("{\"status\":true,\"radiolist\":[");
            try
            {
                int type = System.BitConverter.ToInt32(token.buffer, 8);
                MediaService.WriteLog("1081 获取我的频道列表：type " + type + ",uid=" + token.uid + ",sn=" + token.glsn, MediaService.wirtelog);

                #region 获取媒体频道列表

                ////获取媒体频道列表
                //if (type == 0 || type == 1)
                //{
                //    int areaid = System.BitConverter.ToInt32(token.buffer, 12);
                //    MediaService.WriteLog("1081 获取我的频道列表：areaid " + areaid, MediaService.wirtelog);
                //    foreach (var item in MediaService.radioDic.ToArray())
                //    {
                //        int rid = item.Key;
                //        RadioObject ro = (RadioObject)item.Value;
                //        if (ro.radiotype != 0)
                //        {
                //            if (ro.areaid == areaid || ro.areaid == 0)
                //            {
                //                string senduid = "0";
                //                if (ro.sendtype == 1)
                //                {
                //                    senduid = (ro.senduid != null && ro.senduid.Contains(token.uid)) ? "0" : "-1";
                //                }
                //                else if (ro.sendtype == 2)
                //                {
                //                    senduid = (ro.senduid != null && ro.senduid.Contains(token.uid)) ? "-1" : "0";
                //                }
                //                else
                //                {
                //                    senduid = ro.sendtype.ToString();
                //                }
                //                //sb.Append("{\"rid\":\"" + rid + "\",\"channelname\":\"" + ro.channelname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"imageurl\":\"" + ro.imageurl + "\",\"thumburl\":\"" + ro.thumburl + "\"},");

                //                sb.Append("{\"rid\":\"" + rid + "\",\"channelname\":\"" + ro.channelname + "\",\"cityname\":\"" + ro.cityname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":" + ro.radiotype + ",\"imageurl\":\"" + ro.imageurl + "\",\"thumburl\":\"" + ro.thumburl + "\"},");
                //                if (token.praido != "")
                //                {
                //                    string pradip = token.praido + ",";
                //                    if (pradip.IndexOf(rid + ",") >= 0)
                //                    {
                //                        sb.Append("{\"rid\":\"" + rid + "\",\"channelname\":\"" + ro.channelname + "\",\"cityname\":\"" + ro.cityname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":1,\"imageurl\":\"" + ro.imageurl + "\",\"thumburl\":\"" + ro.thumburl + "\"},");
                //                    }
                //                }
                //            }
                //        }
                //    }
                //    if (sb.ToString().EndsWith(","))
                //    {
                //        sb.Remove(sb.Length - 1, 1);
                //    }
                //}

                #endregion

                #region 获取媒体频道列表
                if (!string.IsNullOrWhiteSpace(token.praido))
                {
                    string[] ridlist = token.praido.Split(',');
                    int rid = 0;
                    List<int> rids = new List<int>();
                    foreach (var item in ridlist)
                    {
                        if (int.TryParse(item, out rid))
                            rids.Add(rid);
                    }
                    rids.ForEach(x =>
                    {
                        RadioObject ro = null;
                        MediaService.radioDic.TryGetValue(x, out ro);
                        if (ro != null)
                        {
                            string senduid = "0";
                            if (ro.sendtype == 1)
                            {
                                senduid = (ro.senduid != null && ro.senduid.Contains(token.uid)) ? "0" : "-1";
                            }
                            else if (ro.sendtype == 2)
                            {
                                senduid = (ro.senduid != null && ro.senduid.Contains(token.uid)) ? "-1" : "0";
                            }
                            else
                            {
                                senduid = ro.sendtype.ToString();
                            }
                            sb.Append("{\"rid\":\"" + rid + "\",\"channelname\":\"" + ro.channelname + "\",\"cityname\":\"" + ro.cityname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":1,\"imageurl\":\"" + ro.imageurl + "\",\"thumburl\":\"" + ro.thumburl + "\",\"flashimageurl\":\"" + ro.flashimageurl + "\"},");
                        }
                    });
                    if (sb.ToString().EndsWith(","))
                    {
                        sb.Remove(sb.Length - 1, 1);
                    }
                }
                #endregion

                sb.Append("],\"talklist\":[");

                ////获取自定义频道列表
                //if (type == 0 || type == 2)
                //{
                string tsql = "select T1.id,T1.tid,T1.xuhao,T1.duijiang,T1.remark,T2.muid,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.imageurl from (select id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = " + token.uid + ") AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid";
                DataTable tdt = SqlHelper.ExecuteTable(tsql);
                if (tdt.Rows.Count > 0)
                {
                    for (int i = 0; i < tdt.Rows.Count; i++)
                    {
                        string tid = tdt.Rows[i]["tid"].ToString();
                        string talkname = tdt.Rows[i]["talkname"].ToString();
                        string xuhao = tdt.Rows[i]["xuhao"].ToString();
                        string dj = tdt.Rows[i]["duijiang"].ToString();
                        string auth = tdt.Rows[i]["auth"].ToString();
                        string remark = tdt.Rows[i]["remark"].ToString();
                        string[] mid = tdt.Rows[i]["muid"].ToString().Split(',');
                        int usernum = Int32.Parse(tdt.Rows[i]["usernum"].ToString());
                        string create = "false";
                        for (int k = 0; k < mid.Length; k++)
                        {
                            string m = mid[k].Trim();
                            if (m != "" && m == token.uid.ToString())
                            {
                                create = "true";
                                break;
                            }
                        }
                        if (create != "true")
                        {
                            if (usernum <= 20)
                            {
                                create = "true";
                            }
                            else
                            {
                                if (tdt.Rows[i]["createuid"].ToString() == token.uid.ToString())
                                {
                                    create = "true";
                                }
                            }
                        }
                        string type1 = tdt.Rows[i]["type"].ToString();
                        string imageurl = tdt.Rows[i]["imageurl"].ToString();
                        sb.Append("{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"xuhao\":" + xuhao + ",\"auth\":\"" + auth + "\",\"remark\":\"" + remark + "\",\"dj\":" + dj + ",\"create\":" + create + ",\"usernum\":" + usernum + ",\"type\":\"" + type1 + "\",\"imageurl\":\"" + imageurl + "\"},");
                    }
                    if (sb.ToString().EndsWith(","))
                    {
                        sb.Remove(sb.Length - 1, 1);
                    }
                }
                //}
                sb.Append("]}");
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                return WriteErrorJson(6);
            }
            return sb.ToString();
        }
        #endregion

        #region 用户单聊状态
        public static string SendSingleChatState(AsyncUserToken token, int packnum)
        {
            string recv = null;
            try
            {
                if (token.uid != 0)
                {
                    int uid = System.BitConverter.ToInt32(token.buffer, 8);

                    MediaService.WriteLog("1082:" + token.uid + "发至" + uid, true);
                    Buffer.BlockCopy(System.BitConverter.GetBytes(token.uid), 0, token.buffer, 8, 4);

                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        try
                        {
                            uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
            }
            return recv;
        }
        #endregion

        #region 用户发送单聊信息
        public static string SendSingleChatMessage(AsyncUserToken token, int packnum)
        {
            string recv = null;
            try
            {
                if (token.uid != 0)
                {
                    int uid = System.BitConverter.ToInt32(token.buffer, 8);
                    MediaService.WriteLog("1083:" + token.uid + "发至" + uid, true);
                    Buffer.BlockCopy(System.BitConverter.GetBytes(token.uid), 0, token.buffer, 8, 4);
                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        if (uo.socket[token.appid] != null)
                        {
                            try
                            {
                                uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                            }
                            catch
                            {
                                //recv = WriteErrorJson("发送失败，用户已离线！", 0);
                            }
                        }
                    }
                }
                else
                {
                    //recv = WriteErrorJson("您还没有登陆！", 0);
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
            }

            return recv;
        }
        #endregion

        #region 用户单聊呼叫
        public static string UserSingleChatCall(AsyncUserToken token, int packnum)
        {
            string recv = null;
            try
            {
                if (token.uid != 0)
                {
                    int uid = 0;
                    ulong fsn = System.BitConverter.ToUInt64(token.buffer, 8);
                    ulong sn = System.BitConverter.ToUInt64(token.buffer, 16);

                    MediaService.WriteLog("1084:" + fsn + "发至" + sn, true);

                    string sql = "select uid from [app_users] where glsn = '" + sn + "'";
                    DataTable dt = SqlHelper.ExecuteTable(sql);

                    if (dt.Rows.Count > 0)
                    {
                        uid = Int32.Parse(dt.Rows[0]["uid"].ToString());
                    }

                    if (uid != 0)
                    {
                        if (uid == token.uid)
                        {
                            return recv = "{\"status\":true,\"type\":1,\"uid\":\"" + uid + "\"}";
                        }

                        UserObject uo = null;
                        if (MediaService.userDic.TryGetValue(uid, out uo))
                        {
                            try
                            {
                                byte[] buffer = new byte[512];
                                string content = "{\"status\":true,\"state\":0,\"sn\":\"" + fsn + "\",\"udpaddress\":\"" + token.udpaddress + "\"}";
                                int len = Encoding.UTF8.GetBytes(content, 0, content.Length, buffer, 12) + 12;
                                Buffer.BlockCopy(System.BitConverter.GetBytes((short)len), 0, buffer, 0, 2);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.sendSingleChatState), 0, buffer, 2, 2);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, buffer, 4, 4);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(token.uid), 0, buffer, 8, 4);

                                uo.socket[token.appid].Send(buffer, 0, len, SocketFlags.None);

                                recv = "{\"status\":true,\"type\":0,\"uid\":\"" + uid + "\",\"udpaddress\":\"" + uo.udpaddress + "\"}";

                            }
                            catch
                            {
                                recv = "{\"status\":true,\"type\":2,\"uid\":\"" + uid + "\"}";
                            }
                        }
                        else
                        {
                            recv = "{\"status\":true,\"type\":3,\"uid\":\"" + uid + "\"}";
                        }
                    }
                    else
                    {
                        recv = "{\"status\":true,\"type\":4,\"uid\":\"" + uid + "\"}";
                    }

                }
                else
                {
                    recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
            }
            return recv;
        }
        #endregion

        #region 群呼频道成员
        public static string UserCallTalkMember(AsyncUserToken token, int packnum)
        {
            string recv = null;
            try
            {
                if (token.uid != 0)
                {
                    int tid = System.BitConverter.ToInt32(token.buffer, 8);
                    int type = System.BitConverter.ToInt32(token.buffer, 12);
                    MediaService.WriteLog("1085 群呼频道成员：uid=" + token.uid + "&tid=" + tid + "&type=" + type + "&sn=" + token.glsn, true);
                    string guid = Guid.NewGuid().ToString("N");
                    if (type != 0)
                    {
                        string talkname = string.Empty;
                        string sql = "select tid,createuid,talkname,muid,usernum from [wy_talk] where tid = " + tid;
                        DataTable talkdt = SqlHelper.ExecuteTable(sql);
                        bool create = false;
                        if (talkdt.Rows.Count > 0)
                        {
                            string[] mid = talkdt.Rows[0]["muid"].ToString().Split(',');
                            int usernum = Int32.Parse(talkdt.Rows[0]["usernum"].ToString());
                            talkname = talkdt.Rows[0]["talkname"].ToString();
                            for (int k = 0; k < mid.Length; k++)
                            {
                                string m = mid[k].Trim();
                                if (m != "" && m == token.uid.ToString())
                                {
                                    create = true;
                                    break;
                                }
                            }
                            if (create != true)
                            {
                                if (usernum <= 20)
                                {
                                    create = true;
                                }
                                else
                                {
                                    if (talkdt.Rows[0]["createuid"].ToString() == token.uid.ToString())
                                    {
                                        create = true;
                                    }
                                }
                            }
                        }

                        if (create)
                        {
                            DataTable talkuserdt = SqlHelper.ExecuteTable("select uid,glsn from app_users where uid in (select uid from [wy_talkuser] where tid=" + tid + ")");
                            List<int> calluser = new List<int>();
                            StringBuilder callusersn = new StringBuilder();
                            List<int> replycalluser = new List<int>();
                            List<string> replycallusersn = new List<string>();
                            byte[] newsendbuffer = GetNewBuffer(guid, token.buffer, packnum, token.glsn);
                            StringBuilder allsn = new StringBuilder();
                            StringBuilder allalinesn = new StringBuilder();
                            for (int i = 0; i < talkuserdt.Rows.Count; i++)
                            {
                                bool calltrue = false;
                                int uid = Int32.Parse(talkuserdt.Rows[i]["uid"].ToString());
                                if (uid == token.uid)
                                    continue;
                                string glsn = talkuserdt.Rows[i]["glsn"].ToString();
                                allsn.Append(glsn + ",");
                                GoloProduct product = PublicClass.GetGoloProduct(glsn);
                                UserObject uo = null;
                                //bool isNew = product == GoloProduct.GoloZN && IsNewGolo(glsn);
                                if (MediaService.userDic.TryGetValue(uid, out uo))
                                {
                                    allalinesn.Append(glsn + ",");
                                    try
                                    {
                                        if (uo.socket[token.appid] != null)
                                        {
                                            uo.socket[token.appid].Send(newsendbuffer, 0, newsendbuffer.Length, SocketFlags.None);
                                            uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                                            calltrue = true;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        MediaService.WriteLog("1085 群呼频道成员失败：uid=" + token.uid + "&tid=" + tid + "&sn=" + token.glsn + "&sendsn=" + glsn + "--" + e.Message, true);
                                    }
                                    if (!calltrue)
                                    {
                                        try
                                        {
                                            if (uo.socket[8] != null)
                                            {
                                                uo.socket[8].Send(newsendbuffer, 0, newsendbuffer.Length, SocketFlags.None);
                                                uo.socket[8].Send(token.buffer, 0, packnum, SocketFlags.None);
                                                calltrue = true;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            MediaService.WriteLog("1085 群呼频道成员失败：uid=" + token.uid + "&tid=" + tid + "&sn=" + token.glsn + "&sendsn=" + glsn + "--" + e.Message, true);
                                        }
                                    }
                                }

                                if (calltrue)
                                {
                                    calluser.Add(uid);
                                    callusersn.Append(glsn + ",");
                                }

                                replycalluser.Add(uid);
                                replycallusersn.Add(glsn);
                            }
                            MediaService.WriteLog("1085 群呼频道成员：所有用户sn：" + allsn.ToString(), true);
                            MediaService.WriteLog("1085 群呼频道成员：所有登录用户sn：" + allalinesn.ToString(), true);
                            //记录呼叫数据
                            if (calluser.Any())
                            {
                                CallTalkInfo info = new CallTalkInfo(token.uid, token.glsn.ToString(), guid, tid, talkname, calluser);
                                MediaService.WriteLog("1085 群呼频道成员呼叫数据：uid=" + token.uid + "&tid=" + tid + "&sn=" + token.glsn + "&sendsn=" + callusersn, true);
                                MediaService.callTalkInfo.Add(info);
                            }

                            if (replycalluser.Any())
                            {
                                CallBackInfo info = new CallBackInfo(token.uid, token.appid, replycalluser, tid, newsendbuffer, replycallusersn);
                                CallBackTask.AddCallBackInfo(guid, info);
                                MediaService.WriteLog("1085 群呼频道重复发送成员数据：uid=" + token.uid + "&tid=" + tid + "&sn=" + token.glsn + "&sendsn=" + info.CalledSN, true);
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
            }

            return recv;
        }

        private static byte[] GetNewBuffer(string guid, byte[] oldbuffer, int packnum, int glsn)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(guid);
            int lenght = bytes.Length + packnum;
            byte[] newbuffer = new byte[lenght];
            System.Buffer.BlockCopy(oldbuffer, 0, newbuffer, 0, packnum);
            Buffer.BlockCopy(System.BitConverter.GetBytes((short)lenght), 0, newbuffer, 0, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.newUserCallTalkMember), 0, newbuffer, 2, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(glsn), 0, newbuffer, 4, 4);
            System.Buffer.BlockCopy(bytes, 0, newbuffer, packnum, bytes.Length);

            return newbuffer;
        }
        #endregion

        #region 群呼频道成员回复
        public static string ResponseUserCallTalkMember(AsyncUserToken token, int packnum)
        {
            string recv = "{\"status\":false}";
            if (token.uid != 0)
            {
                try
                {
                    //guid=ds2342sdwe&tid=85342&type=1
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("1300 群呼频道成员回复 " + query + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    string guid = qs["guid"] == null ? "" : qs["guid"].ToString();
                    if (!string.IsNullOrWhiteSpace(guid))
                    {
                        string tid = qs["tid"] == null ? "0" : qs["tid"].ToString();
                        string type = qs["type"] == null ? "0" : qs["type"].ToString();
                        ResponseCallTalk response = new ResponseCallTalk(token.uid, token.glsn.ToString(), guid, Convert.ToInt32(tid), Convert.ToInt32(type));
                        MediaService.responseCallTalk.Add(response);
                        CallBackTask.UpdateCallBackInfo(guid, token.uid);
                    }
                    recv = "{\"status\":true}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 用户上传电台语音流
        public static string UserUploadVogStream(AsyncUserToken token, int packnum)
        {
            string recv = null;
            StringBuilder log = new StringBuilder(128);
            try
            {
                if (token.uid != 0)
                {
                    int tid = BitConverter.ToInt32(token.buffer, 8);
                    ulong datetime = BitConverter.ToUInt64(token.buffer, 12);
                    byte state = token.buffer[20];
                    if (state == 0)
                    {
                        log.Append("1087 开始上传语音流：" + token.uid + " to " + tid + "&sn=" + token.glsn);
                    }
                    else if (state == 1)
                    {
                        log.Append("正在上传语音流：" + token.uid + " to " + tid + "&sn=" + token.glsn);
                    }

                    string file = MediaService.fileurl + tid.ToString() + '/' + token.uid.ToString() + '/' + datetime.ToString() + ".mp3";
                    string filedir = file.Substring(0, file.LastIndexOf('/'));
                    if (!Directory.Exists(filedir))
                    {
                        Directory.CreateDirectory(filedir);
                    }
                    FileStream fs = File.Open(file, FileMode.Append);
                    fs.Write(token.buffer, 21, packnum - 21);
                    fs.Close();

                    if (state == 2)
                    {
                        const string headstr = "-----------------------------7dd27182c0258\r\n"
                                               + "Content-Disposition: form-data; name=\"file\"; filename=\"E:\\1.mp3\"\r\n"
                                               + "Content-Type: audio/mpeg\r\n"
                                               + "\r\n";
                        byte[] headbyte = Encoding.UTF8.GetBytes(headstr);

                        fs = File.Open(file, FileMode.Open);
                        long len = fs.Length;
                        byte[] content = new byte[len];
                        fs.Read(content, 0, (int)len);
                        fs.Close();

                        if (len > 3096)
                        {
                            const string endstr = "\r\n\r\n-----------------------------7dd27182c0258--\r\n";
                            byte[] endbyte = Encoding.UTF8.GetBytes(endstr);

                            RadioObject ro;
                            if (MediaService.radioDic.TryGetValue(tid, out ro))
                            {
                                int ouid;
                                MediaService.mapDic.TryGetValue(token.uid, out ouid);

                                MongoCollection col = MediaService.mongoDataBase.GetCollection("VogMessage_" + DateTime.Now.ToString("yyyyMMdd"));
                                VogMessage vm = new VogMessage
                                {
                                    CreateTime = DateTime.UtcNow.Ticks,
                                    Datetime = datetime,
                                    Tid = tid,
                                    Senduid = token.uid,
                                    Ouid = ouid
                                };
                                col.Insert(vm);
                                long messageId = vm.CreateTime;
                                InMemoryCache.Instance.Add(messageId.ToString(), vm, DateTime.Now.AddMinutes(2));

                                string nickName = "";
                                string faceUrl = "";
                                const string sql = "SELECT nick_name,face_url FROM wy_user WHERE [user_id]=@ouid";
                                SqlParameter[] paras = { new SqlParameter("@ouid", ouid) };
                                var dt = SqlHelper.ExecuteTable(sql, paras);
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    var row = dt.Rows[0];
                                    nickName = row["nick_name"].ToString();
                                    faceUrl = row["face_url"] + "";
                                    if (faceUrl.Contains('?')) faceUrl = faceUrl.Split('?')[0];
                                }

                                string siteurl = ro.uploadurl + token.uid + "&m.zsn=" + token.glsn + "&m.messageid=" + messageId +
                                                 "&m.ouid=" + ouid + "&m.nickName=" + nickName + "&m.faceUrl=" + faceUrl;
                                log.Append(" 上传url：" + siteurl);
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(siteurl);
                                request.Method = "POST";
                                request.ContentType = "multipart/form-data; boundary=---------------------------7dd27182c0258";
                                request.ContentLength = headbyte.Length + content.Length + endbyte.Length;
                                request.Timeout = 1000 * MediaService.httptimeout;
                                request.ReadWriteTimeout = 1000 * MediaService.httptimeout;
                                Stream wr = request.GetRequestStream();
                                wr.Write(headbyte, 0, headbyte.Length);
                                wr.Write(content, 0, content.Length);
                                wr.Write(endbyte, 0, endbyte.Length);
                                wr.Close();

                                WebResponse response = request.GetResponse();
                                StreamReader httpreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                                string json = httpreader.ReadToEnd();
                                httpreader.Close();
                                response.Close();

                                MediaService.radioinfo.Add(new Talkinfo(tid, token.uid, (long)datetime));
                                log.Append(" 1087 上传语音流结束：uid=" + token.uid + " to " + tid + ",result:" + json);
                            }
                        }
                    }

                    recv = "{\"status\":true}";
                }
                else
                {
                    recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
                }
            }
            catch (Exception err)
            {
                log.Append(" 执行异常：").Append(err);
                recv = "{\"status\":false}";
            }

            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 用户上传电台语音流--新
        public static ConcurrentDictionary<ulong, UpRadioMessage> radioMessageDic = new ConcurrentDictionary<ulong, UpRadioMessage>();
        public static string NewUserUploadVogStream(AsyncUserToken token, int packnum)
        {
            string recv = null;
            StringBuilder log = new StringBuilder(128);
            try
            {
                if (token.uid != 0)
                {
                    int ouid = BitConverter.ToInt32(token.buffer, 4);
                    if (ouid <= 0)
                    {
                        MediaService.mapDic.TryGetValue(token.uid, out ouid);
                    }
                    int tid = BitConverter.ToInt32(token.buffer, 8);
                    ulong datetime = BitConverter.ToUInt64(token.buffer, 12);
                    byte count = token.buffer[20];
                    byte index = token.buffer[21];
                    UpRadioMessage message;
                    if (!radioMessageDic.ContainsKey(datetime))
                    {
                        radioMessageDic.TryAdd(datetime, new UpRadioMessage(datetime, count, index));
                    }
                    else
                    {
                        radioMessageDic.TryGetValue(datetime, out message);
                        if (message != null)
                            message.AddIndexs(index);
                    }
                    log.Append("1302 上传语音流：uid=" + token.uid + "--sn=" + token.glsn + "--tid=" + tid + "--datetime=" + datetime + "--index=" + index + "--count=" + count);

                    string file = MediaService.fileurl + tid.ToString() + '/' + token.uid.ToString() + '/' + datetime.ToString() + ".mp3";
                    string filedir = file.Substring(0, file.LastIndexOf('/'));
                    if (!Directory.Exists(filedir))
                    {
                        Directory.CreateDirectory(filedir);
                    }

                    radioMessageDic.TryGetValue(datetime, out message);
                    using (FileStream fileStream = new FileStream(file, FileMode.OpenOrCreate))
                    {
                        int offset = (MediaService.bufferSize - 22) * index;
                        fileStream.Seek(offset, SeekOrigin.Begin);
                        fileStream.Write(token.buffer, 22, packnum - 22);
                    }

                    if (message != null && message.IsCompleted())
                    {
                        const string headstr = "-----------------------------7dd27182c0258\r\n"
                                               + "Content-Disposition: form-data; name=\"file\"; filename=\"E:\\1.mp3\"\r\n"
                                               + "Content-Type: audio/mpeg\r\n"
                                               + "\r\n";
                        byte[] headbyte = Encoding.UTF8.GetBytes(headstr);

                        FileStream fs = File.Open(file, FileMode.Open);
                        long len = fs.Length;
                        byte[] content = new byte[len];
                        fs.Read(content, 0, (int)len);
                        fs.Close();

                        if (len > 3096)
                        {
                            const string endstr = "\r\n\r\n-----------------------------7dd27182c0258--\r\n";
                            byte[] endbyte = Encoding.UTF8.GetBytes(endstr);

                            RadioObject ro;
                            if (MediaService.radioDic.TryGetValue(tid, out ro))
                            {
                                MongoCollection col = MediaService.mongoDataBase.GetCollection("VogMessage_" + DateTime.Now.ToString("yyyyMMdd"));
                                VogMessage vm = new VogMessage
                                {
                                    CreateTime = DateTime.UtcNow.Ticks,
                                    Datetime = datetime,
                                    Tid = tid,
                                    Senduid = token.uid,
                                    Ouid = ouid
                                };
                                col.Insert(vm);
                                long messageId = vm.CreateTime;
                                InMemoryCache.Instance.Add(messageId.ToString(), vm, DateTime.Now.AddMinutes(2));

                                string nickName = "";
                                string faceUrl = "";
                                const string sql = "SELECT nick_name,face_url FROM wy_user WHERE [user_id]=@ouid";
                                SqlParameter[] paras = { new SqlParameter("@ouid", ouid) };
                                var dt = SqlHelper.ExecuteTable(sql, paras);
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    var row = dt.Rows[0];
                                    nickName = row["nick_name"].ToString();
                                    faceUrl = row["face_url"] + "";
                                    if (faceUrl.Contains('?')) faceUrl = faceUrl.Split('?')[0];
                                }

                                string siteurl = ro.uploadurl + token.uid + "&m.zsn=" + token.glsn + "&m.messageid=" + messageId +
                                                 "&m.ouid=" + ouid + "&m.nickName=" + nickName + "&m.faceUrl=" + faceUrl;
                                log.Append(" 上传url： ").Append(siteurl);
                                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(siteurl);
                                request.Method = "POST";
                                request.ContentType = "multipart/form-data; boundary=---------------------------7dd27182c0258";
                                request.ContentLength = headbyte.Length + content.Length + endbyte.Length;
                                request.Timeout = 1000 * MediaService.httptimeout;
                                request.ReadWriteTimeout = 1000 * MediaService.httptimeout;
                                Stream wr = request.GetRequestStream();
                                wr.Write(headbyte, 0, headbyte.Length);
                                wr.Write(content, 0, content.Length);
                                wr.Write(endbyte, 0, endbyte.Length);
                                wr.Close();

                                WebResponse response = request.GetResponse();
                                StreamReader httpreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                                string json = httpreader.ReadToEnd();
                                httpreader.Close();
                                response.Close();

                                MediaService.radioinfo.Add(new Talkinfo(tid, token.uid, (long)datetime));
                                log.Append("上传语音流结束：uid=" + token.uid + " tid=" + tid + ",result:" + json);
                                radioMessageDic.TryRemove(datetime, out message);
                            }
                        }
                    }

                    recv = "{\"status\":true}";
                }
                else
                {
                    recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
                }
            }
            catch (Exception err)
            {
                log.Append("  执行异常：").Append(err);
                recv = "{\"status\":false}";
            }

            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 获取附近频道名称
        public static string GetUserFuJinChannel(AsyncUserToken token)
        {
            #region old
            //try
            //{
            //    if (token.uid != 0)
            //    {
            //        int aread = System.BitConverter.ToInt32(token.buffer, 8);

            //        MediaService.WriteLog("ctc_baidu：" + aread, MediaService.wirtelog);
            //        DataRow[] dr = MediaService.districtapp.Select("ctc_baidu=" + aread);
            //        if (dr.Length > 0)
            //        {

            //            MediaService.WriteLog("ctc_baidu：" + "select rid, channelname, sendtype, senduid, audiourl, channelde, radiotype from [wy_radio] where channelname='" + dr[0]["areaCode"].ToString() + "'", MediaService.wirtelog);
            //            DataTable dt = SqlHelper.ExecuteTable("select rid, channelname, sendtype, senduid, audiourl, channelde, radiotype from [wy_radio] where channelname='" + dr[0]["areaCode"].ToString() + "'");
            //            if (dt.Rows.Count > 0)
            //            {
            //                return "{\"status\":true,\"rid\":\"" + dt.Rows[0]["rid"].ToString() + "\",\"channelname\":\"" + dt.Rows[0]["channelname"].ToString() + "\",\"senduid\":\"0\",\"audiourl\":\"" + dt.Rows[0]["audiourl"].ToString() + "\",\"channelde\":" + dt.Rows[0]["channelde"].ToString() + ",\"radiotype\":" + dt.Rows[0]["radiotype"].ToString() + "}";
            //            }
            //        }
            //    }
            //    else
            //    {
            //        return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            //    }
            //}
            //catch (Exception err)
            //{
            //    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
            //}
            //return "{\"status\":false}";
            #endregion
            try
            {
                if (token.uid != 0)
                {
                    int aread = System.BitConverter.ToInt32(token.buffer, 8);

                    MediaService.WriteLog("ctc_baidu：" + aread + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                    string result = string.Empty;
                    string areacode = string.Empty;
                    if (aread == 340)//深圳频道单独处理
                    {
                        DataRow[] dr = MediaService.districtapp.Select("ctc_baidu=" + aread);
                        if (dr.Length > 0)
                        {
                            areacode = dr[0]["areaCode"].ToString();
                        }
                    }
                    else
                    {
                        areacode = GetBaseAreaCode(aread);
                    }
                    if (!string.IsNullOrWhiteSpace(areacode))
                    {
                        if (QueryUserFuJinChannel(areacode, ref result))
                        {
                            return result;
                        }
                    }
                }
                else
                {
                    return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
            }
            return "{\"status\":false}";
        }

        //查询附加频道
        private static bool QueryUserFuJinChannel(string areacode, ref string result)
        {
            MediaService.WriteLog("ctc_baidu：areacode =" + areacode, MediaService.wirtelog);
            var roValue = MediaService.radioDic.FirstOrDefault(x => x.Value.areacode == areacode);
            if (roValue.Value == null)
                return false;
            RadioObject ro = roValue.Value;
            if (ro.prid == 0)
                result = "{\"status\":true,\"rid\":\"" + roValue.Key + "\",\"channelname\":\"" + ro.channelname + "\",\"senduid\":\"0\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":" + ro.radiotype + "}";
            else
            {
                object p = MediaService.radioDic.FirstOrDefault(x => x.Key == ro.prid);
                if (p == null)
                    result = "{\"status\":true,\"rid\":\"" + roValue.Key + "\",\"channelname\":\"" + ro.channelname + "\",\"senduid\":\"0\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":" + ro.radiotype + "}";
                else
                {
                    try
                    {
                        RadioObject prov = (RadioObject)((KeyValuePair<int, RadioObject>)p).Value;
                        result = "{\"status\":true,\"rid\":\"" + ro.prid + "\",\"channelname\":\"" + prov.channelname + "\",\"senduid\":\"0\",\"audiourl\":\"" + prov.audiourl + "\",\"channelde\":" + prov.channelde + ",\"radiotype\":" + prov.radiotype + "}";
                    }
                    catch
                    {
                        MediaService.WriteLog("执行异常：rid:" + roValue.Key + "-prid:" + ro.prid, MediaService.wirtelog);
                        return false;
                    }
                }
            }
            return true;

            //DataTable dt = SqlHelper.ExecuteTable("select rid, channelname, sendtype, senduid, audiourl, channelde, radiotype from [wy_radio] where channelname='" + areacode + "'");
            //if (dt.Rows.Count > 0)
            //{
            //    result = "{\"status\":true,\"rid\":\"" + dt.Rows[0]["rid"].ToString() + "\",\"channelname\":\"" + dt.Rows[0]["channelname"].ToString() + "\",\"senduid\":\"0\",\"audiourl\":\"" + dt.Rows[0]["audiourl"].ToString() + "\",\"channelde\":" + dt.Rows[0]["channelde"].ToString() + ",\"radiotype\":" + dt.Rows[0]["radiotype"].ToString() + "}";
            //    return true;
            //}
            //return false;
        }

        //找出省会城市
        private static string GetBaseAreaCode(int ctc_baidu)
        {
            string sql = @"WITH NODES  AS (
                         SELECT * FROM app_district child WHERE child.id=(select top(1) id where ctc_baidu = " + ctc_baidu +
                         @" )UNION ALL  SELECT d.* FROM app_district AS d INNER JOIN  NODES  AS n ON d.id = n.upid)  
                         SELECT top(1) areaCode FROM app_district WHERE id IN (SELECT id  FROM NODES N ) and upid = 0; ";
            object area = SqlHelper.ExecuteScalar(sql);
            return area == null ? string.Empty : area.ToString();
        }
        #endregion

        //廖佛珍

        #region 获取用户通讯录--userJoinPersonTalk = 1100;--error
        /// <summary>
        /// 获取用户通讯录
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string GetUserContactList(AsyncUserToken token)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    string strSql = @"SELECT users.uid as uid ,relation.fuid as fuid,users.glsn as sn ,relation.nickname as nickname,users.mobile as mobile,relation.state as state 
                                    from app_users as users, (SELECT wy_userrelation.fuid as fuid,wy_userrelation.nickname as nickname,wy_userrelation.state as state 
                                    from wy_userrelation WHERE wy_userrelation.uid = '" + token.uid +
                                            "' ) as relation WHERE users. uid = relation.fuid";
                    //根据用户 uid 查询跟该用户相关的通讯信息
                    DataTable dtContacts = SqlHelper.ExecuteTable(strSql);
                    string subrecv = "";
                    foreach (DataRow dr in dtContacts.Rows)
                    {
                        string fuid = dr["fuid"].ToString();
                        string sn = dr["sn"].ToString();
                        string nickname = dr["nickname"].ToString();
                        string state = dr["state"].ToString();
                        string mobile = dr["mobile"].ToString();
                        //string updatetime = dr["updatetime"].ToString();
                        subrecv += (subrecv == "" ? "" : ",") + "{\"uid\":" + token.uid + ",\"fuid\":" + fuid
                            + ",\"sn\": \"" + sn + "\",\"nickname\": \"" + nickname + "\",\"state\":" + state
                            + "}";
                        /// + ",\"updatetime\":\"" + updatetime + "\"}";
                    }
                    recv = "{\"status\":true,\"list\":[" + subrecv + "]}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 用户OK键提交
        /// <summary>
        /// 用户OK键提交
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string CreateUserOrder(AsyncUserToken token, int packnum)
        {
            StringBuilder sb = new StringBuilder();
            string recv = null;
            if (token.uid != 0)
            {
                try
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    sb.Append("用户OK键提交:" + query);
                    string oks = GetJsonValue(query, "oks", ",", false);
                    if (oks == "1")  //商品订单
                    {
                        string goods_id = GetJsonValue(query, "oid", ",", false);
                        sb.Append(" 创建订单   oid=" + goods_id + "  token.uid=" + token.uid);

                        object ouid = SqlHelper.ExecuteScalar("SELECT ouid FROM wy_uidmap WHERE uid=" + token.uid);
                        if (ouid == null)
                        {
                            return WriteErrorJson(3, "该设备未绑定用户信息！");
                        }
                        string user_id = ouid.ToString();
                        //请求南山服务器
                        string result = HttpRequestRoute("http://inner.api.dbscar.com/?action=order_service.create", "user_id=" + user_id + "&goods_id=" + goods_id + "&botton_ok=1", "POST", Encoding.UTF8);
                        sb.Append("  订单 result=" + result);
                        string code = GetJsonValue(result, "code", ",", false).Trim();
                        if (code == "0")
                        {
                            //string order_id = GetJsonValue(result, "order_id", "}", false);
                            recv = "{\"status\":true,\"order_id\":\"" + goods_id + "\",\"message\":\"下单成功\"}";
                        }
                        else
                        {
                            string message = GetJsonValue(result, "msg", "\"", true);
                            recv = WriteErrorJson(3, message);
                        }
                    }
                    else if (oks == "3") //点赞
                    {
                        int channel = Int32.Parse(GetJsonValue(query, "channel", ",", false));
                        int ouid = Int32.Parse(GetJsonValue(query, "uid", ",", false));
                        int tlen = Int32.Parse(GetJsonValue(query, "mp3Leng_s", ",", false)); ;
                        string file = "";
                        int s = query.LastIndexOf("/");
                        if (s > 0)
                        {
                            int e = query.IndexOf(".mp3", s);
                            file = query.Substring(s + 1, e - s - 1);
                        }
                        sb.Append("    点赞：channel=" + channel + ",ouid=" + ouid + ",tlen=" + tlen + ",uid=" + token.uid);
                        DianZaninfo dzinfo = new DianZaninfo(channel, token.uid, ouid, file, tlen, GetTimeStamp());
                        MediaService.dianzaninfo.Add(dzinfo);
                        recv = "{\"status\":true,\"message\":\"收到您的点赞！\"}";
                    }
                    else if (oks == "4") //红包
                    {
                        object ouid = SqlHelper.ExecuteScalar("SELECT ouid FROM wy_uidmap WHERE uid=" + token.uid);
                        if (ouid == null)
                        {
                            return WriteErrorJson(3, "该设备未绑定用户信息，无法参加抢红包活动，请到官网下载轱辘Z app，再来参加！");
                        }
                        string gift_id = GetJsonValue(query, "oid", ",", false);
                        string result = GetHongBao(ouid.ToString(), gift_id, token.glsn.ToString());

                        sb.Append("    红包：result=" + result);
                        if (result.IndexOf("\"code\":0,") > 0 && result.IndexOf("\"getamount\":") > 0)
                        {
                            string money = GetJsonValue(result, "getamount", "\"", true).Replace("\"", "");
                            recv = "{\"status\":true,\"message\":\"恭喜,你已经抢到" + money + "元的红包！\"}";
                        }
                        else
                        {
                            recv = "{\"status\":true,\"message\":\"红包已经抢完了，下次再接再厉！\"}";
                        }
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("用户OK键提交:执行异常：" + err.ToString(), MediaService.wirtelog);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            MediaService.WriteLog(sb.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 取Json字符值
        public static string GetJsonValue(string str, string substr, string laststr, bool isString)
        {
            int k = str.IndexOf("\"" + substr + "\":" + (isString ? "\"" : ""));
            if (k > 0)
            {
                int length = substr.Length + 3 + (isString ? 1 : 0);
                int e = str.IndexOf(laststr, k + length);
                return str.Substring(k + length, e - k - length);
            }
            return string.Empty;
        }
        #endregion

        #region 用户绑定回调

        /// <summary>
        /// 用户绑定回调
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string UserBindingBack(AsyncUserToken token, int packnum)
        {
            //{"uid":"10023452","sim":"89860114795400746201","gender":"1"}

            string recv = "";
            var log = new StringBuilder("用户绑定回调 1095 ");
            if (token.uid != 0)
            {
                try
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    log.Append(" query: ").Append(query);
                    string uid = GetJsonValue(query, "uid", "\"", true);
                    string sim = GetJsonValue(query, "sim", "\"", true);
                    string gender = GetJsonValue(query, "gender", "\"", true);

                    int ouid = Int32.Parse(uid);
                    int sex;
                    int.TryParse(gender, out sex);

                    //查询是否注册SN
                    string strUidMap = "SELECT count(*) FROM wy_uidmap  where ouid =" + ouid + " and uid = " + token.uid;
                    object sqlUidMapResult = SqlHelper.ExecuteScalar(strUidMap);
                    if (sqlUidMapResult != null)
                    {
                        int count = (int)sqlUidMapResult;
                        if (count > 0)
                            recv = WriteErrorJson(3, "SN已经被注册");
                        else
                        {
                            //不存在插入设备
                            StringBuilder sql = new StringBuilder();
                            sql.Append("INSERT INTO wy_uidmap(ouid,uid,sim) VALUES(" + ouid + "," + token.uid + ",\'" + sim + "\');");
                            sql.Append(string.Format("update app_users set gender={0},updatetime = GETDATE() from app_users where uid={1};", sex, uid));
                            int countInsert = SqlHelper.ExecuteNonQuery(sql.ToString());
                            if (countInsert < 1)
                            {
                                recv = WriteErrorJson(3, "用户绑定设备数据插入异常");
                            }
                            else
                            {
                                recv = "{\"status\":true}";
                                SqlHelper.ExecuteNonQuery(string.Format("UPDATE wy_userrelation SET ouid={0} WHERE [uid]={1}",
                                    ouid, token.uid));
                                if (MediaService.mapDic.ContainsKey(token.uid))
                                    MediaService.mapDic[token.uid] = ouid;
                                else
                                    MediaService.mapDic.TryAdd(token.uid, ouid);
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    log.Append(" 执行异常：").Append(err);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            log.Append(" recv=").Append(recv);
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 获取用户wifi列表
        /// <summary>
        /// 获取用户wifi列表
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string GetUserWifiList(AsyncUserToken token)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    string strSql = @"SELECT id,name,password,updatetime from wy_userwifi WHERE ouid = (SELECT DISTINCT ouid FROM wy_uidmap WHERE uid= " + token.uid + ")";
                    //根据用户 uid 查询跟该用户相关的wifi列表
                    DataTable dtContacts = SqlHelper.ExecuteTable(strSql);
                    string subrecv = "";
                    foreach (DataRow dr in dtContacts.Rows)
                    {
                        string id = dr["id"].ToString();
                        string name = dr["name"].ToString();
                        string password = dr["password"].ToString();
                        string updatetime = CommFunc.ConvertDateTimeInt(dr["updatetime"].ToString()).ToString();
                        subrecv += (subrecv == "" ? "" : ",") + "{\"id\":" + id + ",\"name\": \"" + name + "\",\"password\":\"" + password + "\",\"updatetime\":\"" + updatetime + "\"}";
                    }
                    recv = "{\"status\":true,\"list\":[" + subrecv + "]}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 获取用户通讯录--getMyContactList = 1086;
        /// <summary>
        /// 获取用户通讯录
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string GetMyContactList(AsyncUserToken token, int packnum)
        {
            MediaService.WriteLog("1086 获取用户通讯录，uid=" + token.uid, MediaService.wirtelog);
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    int myOuid;
                    MediaService.mapDic.TryGetValue(token.uid, out myOuid);
                    string strSql = "SELECT R.nickname, R.fuid, R.[state], R.updatetime, A.glsn AS sn FROM wy_userrelation R INNER JOIN app_users A ON R.fuid=A.[uid] WHERE ouid=" + myOuid;
                    //根据用户 uid 查询跟该用户相关的通讯信息
                    DataTable dtContacts = SqlHelper.ExecuteTable(strSql);
                    string subrecv = "";
                    List<int> fuids = new List<int>();
                    foreach (DataRow dr in dtContacts.Rows)
                    {
                        int fuid;
                        int.TryParse(dr["fuid"].ToString(), out fuid);
                        if (fuids.Contains(fuid) || fuid == 0)
                        { continue; }   //去除重复的fuid
                        else
                        { fuids.Add(fuid); }

                        string sn = dr["sn"].ToString();
                        string nickname = dr["nickname"].ToString();
                        string state = dr["state"].ToString();
                        string updatetime = CommFunc.ConvertDateTimeInt(dr["updatetime"].ToString()).ToString();
                        int ouid;
                        MediaService.mapDic.TryGetValue(fuid, out ouid);
                        subrecv += (subrecv == "" ? "" : ",") + "{\"fuid\":" + fuid
                            + ",\"sn\": \"" + sn + "\",\"nickname\": \"" + nickname + "\",\"state\":\"" + state
                            + "\",\"updatetime\":\"" + updatetime + "\",\"ouid\":\"" + ouid + "\"}";
                    }
                    recv = "{\"status\":true,\"list\":[" + subrecv + "]}";
                    MediaService.WriteLog("return：" + recv, MediaService.wirtelog);
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err, MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 约聊

        #region GoloZ用户请求约聊
        /// <summary>
        /// GoloZ用户请求约聊
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string ActiveRequestLocalTalk(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    MediaService.WriteLog("1105 请求约聊 uid=" + token.uid, MediaService.wirtelog);
                    UserObject uo = null;
                    TalkState ts = null;
                    if (MediaService.userDic.TryGetValue(token.uid, out uo))
                    {
                        #region 加入
                        if (!MediaService.stateDic.ContainsKey(token.uid))
                        {
                            ts = new TalkState();
                            ts.State = 0;
                            string sql = string.Format("SELECT gender FROM app_users WHERE uid={0}", token.uid);
                            object result = SqlHelper.ExecuteScalar(sql);
                            int gender = 0;
                            if (result != null && int.TryParse(result.ToString(), out gender) && gender != 0)
                            {
                                ts.Gender = gender;
                                MediaService.stateDic.TryAdd(token.uid, ts);
                                recv = "{\"status\":true}";
                            }
                            else
                                recv = WriteErrorJson(3, "你还未设置性别，请设置后重试！");
                        }
                        else
                            recv = "{\"status\":true}";
                        #endregion
                        #region 搜索
                        int cid = uo.cid[token.appid];
                        if (MediaService.stateDic.TryGetValue(token.uid, out ts))
                        {
                            if (ts.Gender == 0)
                            {
                                recv = WriteErrorJson(3, "你还未设置性别，请设置后重试！");
                                MediaService.stateDic.TryRemove(token.uid, out ts);
                            }
                            else
                            {
                                #region 已经在约聊频道
                                int preuid = 0;
                                if (ts.State == 1)
                                {
                                    preuid = ts.Currenttuid;
                                    ts.State = 0;
                                    ts.Currenttuid = 0;
                                    if (preuid != 0)
                                    {
                                        TalkState tts = null;
                                        if (MediaService.stateDic.TryGetValue(preuid, out tts))
                                        {
                                            tts.State = 0;
                                            tts.Currenttuid = 0;
                                        }
                                        //将退出同聊发送到匹配的同聊用户
                                        string sendstr = "{\"status\":true,\"tuid\":\"" + token.uid + "\"}";
                                        PublicClass.SendToOnlineUserList(null, sendstr, "", new List<int>() { preuid }, 99, 0, CommType.passiveQuitLocalTalk, token.appid);
                                    }
                                }
                                #endregion
                                UserObject matchuo = null;
                                var stateDic = MediaService.stateDic.Where(x => x.Value.Gender == findOppositeSex(ts.Gender) && x.Value.State == 0);
                                foreach (KeyValuePair<int, TalkState> item in stateDic)
                                {
                                    MediaService.WriteLog("约聊匹配.. tuid:" + item.Key, MediaService.wirtelog);
                                    matchuo = null;
                                    if (MediaService.userDic.TryGetValue(item.Key, out matchuo))
                                    {
                                        bool isLega = TalkRecordManager.Instance.IsLegalTuid(token.uid, item.Key);
                                        MediaService.WriteLog("约聊匹配1 tuid:" + item.Key + " legal:" + isLega + "cid:" + matchuo.cid[token.appid] + " ocid=" + cid, MediaService.wirtelog);

                                        if (matchuo != null && matchuo.cid[token.appid] == cid && isLega)
                                        {
                                            MediaService.WriteLog("约聊匹配成功.. uid=" + token.uid + "  tuid:" + item.Key, MediaService.wirtelog);
                                            //将同聊请求发送到匹配的同聊用户
                                            string sendstr = "{\"status\":true,\"tuid\":\"" + token.uid + "\"}";
                                            PublicClass.SendToOnlineUserList(null, sendstr, "", new List<int>() { item.Key }, 99, 0, CommType.passiveReceiveLocalTalk, token.appid);
                                            TalkRecordManager.Instance.TryAddRecord(token.uid, item.Key);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        private static int findOppositeSex(int gender)
        {
            if (gender == 1)
                return 2;
            if (gender == 2)
                return 1;
            return 0;
        }

        #endregion

        #region GoloZ用户请求同聊应答
        /// <summary>
        /// GoloZ用户请求同聊应答
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string ActiveJoinLocalTalk(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    //1.加入同聊    2.不想加入同聊（不接听）
                    //{"tuid:"1024506"}
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("1108 请求同聊应答： " + query, MediaService.wirtelog);
                    string uid = GetJsonValue(query, "tuid", "\"", true);

                    TalkState ts = null;
                    if (MediaService.stateDic.TryGetValue(token.uid, out ts))
                    {
                        ts.State = 1;
                        ts.Currenttuid = Convert.ToInt32(uid);
                    }
                    TalkState tts = null;
                    if (MediaService.stateDic.TryGetValue(Convert.ToInt32(uid), out tts))
                    {
                        tts.State = 1;
                        tts.Currenttuid = token.uid;
                    }

                    //将请求同聊应答发送到匹配的同聊用户
                    string sendstr = "{\"status\":true,\"tuid\":\"" + token.uid + "\"}";
                    PublicClass.SendToOnlineUserList(null, sendstr, "", new List<int>() { Convert.ToInt32(uid) }, 99, 0, CommType.passiveJoinLocalTalk, token.appid);

                    recv = "{\"status\":true}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region GoloZ用户用户同聊
        /// <summary>
        /// GoloZ用户用户同聊
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string UserLocalTalk(AsyncUserToken token, int packnum)
        {
            try
            {
                if (token.uid != 0)
                {
                    int uid = System.BitConverter.ToInt32(token.buffer, 8);

                    MediaService.WriteLog("约聊 1111:" + token.uid + "发至" + uid, true);
                    Buffer.BlockCopy(System.BitConverter.GetBytes(token.glsn), 0, token.buffer, 8, 4);
                    Buffer.BlockCopy(System.BitConverter.GetBytes(token.uid), 0, token.buffer, 4, 4);
                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        try
                        {
                            uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
            }
            return null;
        }
        #endregion

        #region GoloZ用户请求同聊应答
        /// <summary>
        /// GoloZ用户退出同聊
        /// 将两个同聊状态设置为0
        /// 给另一个发送退出同聊消息
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string QuitLocalTalk(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    //包括两种情况，1、已经匹配的聊天模式，2、还没有匹配就退出 ？？？
                    //{"match":"true","tuid:"1024620"}
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("1109 退出同聊 " + query, MediaService.wirtelog);
                    string match = GetJsonValue(query, "match", "\"", true);
                    string uid = GetJsonValue(query, "tuid", "\"", true);
                    bool hasMatch = false;
                    Boolean.TryParse(match, out hasMatch);
                    TalkState ts = null;
                    if (MediaService.stateDic.ContainsKey(token.uid))
                    {
                        MediaService.stateDic.TryRemove(token.uid, out ts);
                    }
                    if (hasMatch)
                    {
                        TalkState tts = null;
                        int ouid = Convert.ToInt32(uid);
                        if (MediaService.stateDic.ContainsKey(ouid))
                        {
                            MediaService.stateDic.TryRemove(ouid, out tts);
                        }
                        //将退出同聊发送到匹配的同聊用户
                        string sendstr = "{\"status\":true,\"tuid\":\"" + token.uid + "\"}";
                        PublicClass.SendToOnlineUserList(null, sendstr, "", new List<int>() { ouid }, 99, 0, CommType.passiveQuitLocalTalk, token.appid);
                    }
                    recv = "{\"status\":true}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #endregion

        #region 刷新Sim卡信息
        /// <summary>
        /// 刷新Sim卡信息
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string RefreashSim(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    //uid=10024230&sim=89860114795400812345
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("刷新Sim卡信息 " + query, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    if (qs["uid"] != null && qs["sim"] != null)
                    {
                        string uid = qs["uid"].ToString().Replace("'", "");
                        string sim = qs["sim"].ToString().Replace("'", "");
                        string strSql = "select uid,sim from wy_uidmap where uid=" + uid;

                        DataTable dt = SqlHelper.ExecuteTable(strSql);
                        if (dt.Rows.Count < 1)
                            return WriteErrorJson(3, "没有查询到用户的绑定信息！");
                        string orsim = dt.Rows[0]["sim"].ToString();
                        if (!sim.Equals(orsim))
                        {
                            string sql = "update wy_uidmap set sim='" + sim + "' ,bindtime =GETDATE() WHERE uid= " + uid;
                            int count = SqlHelper.ExecuteNonQuery(sql);
                        }
                        recv = "{\"status\":true}";
                    }
                    else
                    {
                        recv = WriteErrorJson(11, "请求的格式不正确！");
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        //快聊
        #region 快聊
        #region 1114 请求快聊
        /// <summary>
        /// GoloZ用户请求快聊
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string RequestQuickTalk(AsyncUserToken token, int packnum)
        {
            string recv = "{\"status\":false}";
            if (token.uid != 0)
            {
                try
                {
                    //uid=123213,12332,12532,234234&tid=85342
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("1114 请求快聊 " + query + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    string tid = "0";
                    string talkname = "";
                    string auth = "";
                    //不存在则创建频道
                    if (qs["tid"] == null)
                    {
                        //"{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\"}";
                        string data = PublicClass.CreateTalk(token.uid, qs, 1);
                        string status = GetJsonValue(data, "status", ",", false);
                        tid = GetJsonValue(data, "tid", ",", false);
                        talkname = GetJsonValue(data, "talkname", "\"", true);
                        auth = GetJsonValue(data, "auth", "\"", true);
                        if (status == "false")
                        {
                            recv = WriteErrorJson(6, "频道创建失败,请再次重试！");
                            return recv;
                        }
                        recv = data;
                    }
                    else //存在则查询频道号
                    {
                        tid = qs["tid"].ToString();
                        string strSql = "select talkname,auth from wy_talk where tid=" + tid;

                        DataTable dt = SqlHelper.ExecuteTable(strSql);
                        if (dt.Rows.Count < 1)
                            return WriteErrorJson(7, "该频道号不存在！");
                        talkname = dt.Rows[0]["talkname"].ToString();
                        auth = dt.Rows[0]["auth"].ToString();
                    }
                    //将成员加入频道
                    if (qs["uid"] != null)
                    {
                        //频道下的用户
                        DataTable dt = SqlHelper.ExecuteTable("select uid from [wy_talkuser] where tid=" + tid);
                        List<int> existuids = new List<int>();
                        foreach (DataRow dr in dt.Rows)
                        {
                            int tuid = 0;
                            int.TryParse(dr["uid"].ToString(), out tuid);
                            if (tuid != 0 && !existuids.Contains(tuid))
                                existuids.Add(tuid);
                        }

                        //过滤受邀请的用户
                        string[] uids = qs["uid"].ToString().Trim().Split(',');
                        List<int> uidlist = new List<int>();
                        for (int i = 0; i < uids.Length; i++)
                        {
                            int uid = 0;
                            if (int.TryParse(uids[i], out uid) && !existuids.Contains(uid))
                                uidlist.Add(uid);
                        }
                        string glsn = PublicClass.QueryGLSNByUid(token.uid);
                        string message = "{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\",\"uid\":\"" + token.uid + "\",\"glsn\":\"" + glsn + "\"," + GetUserRelation(token.uid, uids) + "}";
                        PublicClass.SendToOnlineUserList(null, message, "", uidlist, 99, 0, CommType.requestQuickTalk, token.appid);
                    }

                    recv = "{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\"}";
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }

        private static string GetUserRelation(int uid, string[] uids)
        {
            string list = "";
            try
            {
                string strSql = "SELECT R.fuid,U.glsn,R.nickname,R.state from app_users as U,(SELECT fuid,nickname,state from wy_userrelation WHERE uid = " + uid + ") as R WHERE U.uid =R.fuid";
                //根据用户 uid 查询跟该用户相关的通讯信息
                DataTable dtContacts = SqlHelper.ExecuteTable(strSql);
                string subrecv = "";
                foreach (DataRow dr in dtContacts.Rows)
                {
                    string fuid = dr["fuid"].ToString();
                    if (!uids.Contains(fuid))
                        continue;
                    string glsn = dr["glsn"].ToString();
                    string nickname = dr["nickname"].ToString();
                    string state = dr["state"].ToString();
                    subrecv += (subrecv == "" ? "" : ",") + "{\"fuid\":" + fuid
                        + ",\"glsn\": \"" + glsn + "\",\"nickname\": \"" + nickname + "\",\"state\":\"" + state
                        + "\"}";
                }
                list = "\"list\":[" + subrecv + "]";
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                list = "\"list\":[]";
            }
            return list;
        }
        #endregion

        #region 1115 响应快聊
        /// <summary>
        /// 1115 响应快聊
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string ResponseQuickTalk(AsyncUserToken token, int packnum)
        {
            string recv = "{\"status\":false}";
            if (token.uid != 0)
            {
                try
                {
                    //tid=12312&talkname=123&auth=123&state=true&nickname=aaa
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("1115 响应快聊 " + query + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    string tid = qs["tid"] == null ? "0" : qs["tid"].ToString();
                    //string uid = qs["uid"] == null ? "0" : qs["uid"].ToString();
                    string talkname = qs["talkname"] == null ? "" : qs["talkname"].ToString();
                    string auth = qs["auth"] == null ? "" : qs["auth"].ToString();
                    string state = qs["state"] == null ? "" : qs["state"].ToString();
                    string nickname = qs["nickname"] == null ? "" : qs["nickname"].ToString();

                    //频道下的用户
                    DataTable dt = SqlHelper.ExecuteTable("select uid from [wy_talkuser] where tid=" + tid);
                    List<int> uids = new List<int>();
                    foreach (DataRow dr in dt.Rows)
                    {
                        int tuid = 0;
                        int.TryParse(dr["uid"].ToString(), out tuid);
                        if (tuid != 0 && !uids.Contains(tuid))
                            uids.Add(tuid);
                    }

                    //同意加入
                    if (state.Equals("true"))
                    {
                        PublicClass.JoinTalk(token.uid, auth, talkname);
                    }
                    string glsn = PublicClass.QueryGLSNByUid(token.uid);
                    string message = "{\"status\":true,\"tid\":" + tid + ",\"uid\":\"" + token.uid + "\",\"state\":\"" + state + "\",\"glsn\":\"" + glsn + "\",\"nickname\":\"" + nickname + "\"}"; ;
                    PublicClass.SendToOnlineUserList(null, message, "", uids, 99, 0, CommType.noticeQuickTalk, token.appid);
                    recv = "{\"status\":true}";

                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 1116 加好友
        public static string AddUserFriend(AsyncUserToken token, int packnum)
        {
            string recv = "{\"status\":false}";
            if (token.uid != 0)
            {
                try
                {
                    //uid=12312&fuid=123&nickname=123
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("1116 加好友 " + query + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    string uid = qs["uid"] == null ? "0" : qs["uid"].ToString();
                    string fuid = qs["fuid"] == null ? "0" : qs["fuid"].ToString();
                    string nickname = qs["nickname"] == null ? "" : qs["nickname"].ToString();

                    bool result = PublicClass.AddMyFriend(Convert.ToInt32(uid), Convert.ToInt32(fuid), nickname);
                    if (result)
                    {
                        recv = "{\"status\":true}";
                    }
                    else
                    {
                        recv = WriteErrorJson(9);
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 1303 相互加好友
        public static string AddEachFriend(AsyncUserToken token, int packnum)
        {
            string recv = "{\"status\":false}";
            if (token.uid != 0)
            {
                try
                {
                    //uid=12312&fuid=123&nickname=123&glsn=971691002994&fnickname=123
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    MediaService.WriteLog("1303 相互加好友 " + query + "&uid=" + token.uid + "&sn=" + token.glsn, MediaService.wirtelog);
                    NameValueCollection qs = HttpUtility.ParseQueryString(query, Encoding.UTF8);
                    string uid = qs["uid"] == null ? "0" : qs["uid"].ToString();
                    string fuid = qs["fuid"] == null ? "0" : qs["fuid"].ToString();
                    string nickname = qs["nickname"] == null ? "" : qs["nickname"].ToString();
                    string fnickname = qs["fnickname"] == null ? "" : qs["fnickname"].ToString();
                    string glsn = qs["glsn"] == null ? "" : qs["glsn"].ToString();

                    bool result = false;
                    int _fuid = Convert.ToInt32(fuid);
                    bool result1 = PublicClass.AddMyFriend(Convert.ToInt32(uid), _fuid, fnickname);
                    bool result2 = PublicClass.AddMyFriend(_fuid, Convert.ToInt32(uid), nickname);
                    result = result1 || result2;
                    int ouid = 0;
                    MediaService.mapDic.TryGetValue(Convert.ToInt32(uid), out ouid);
                    string message = "{\"status\":\"" + result + "\",\"fouid\":\"" + ouid + "\",\"uid\":\"" + uid + "\",\"glsn\":\"" + glsn + "\",\"nickname\":\"" + nickname + "\"}"; ;
                    PublicClass.SendToOnlineUserList(null, message, "", new List<int>() { _fuid }, 99, 0, CommType.addEachFriendResponse, token.appid);
                    MediaService.WriteLog("1304 加好友推送消息：" + message, MediaService.wirtelog);

                    if (result)
                    {
                        recv = "{\"status\":true}";
                    }
                    else
                    {
                        recv = WriteErrorJson(9);
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion
        #endregion

        //公共方法

        #region 写错误JSON
        public static string WriteErrorJson(int code)
        {
            return "{\"status\":false,\"code\":" + code + "}";
        }
        #endregion

        #region 写错误JSON
        public static string WriteErrorJson(int code, string message)
        {
            return "{\"status\":false,\"code\":" + code + ",\"message\":\"" + message + "\"}";
        }
        #endregion

        #region 执行http服务器请求
        public static string HttpRequestRoute(string siteurl, string query, string requesttype, Encoding encoding)
        {
            try
            {
                HttpWebRequest request = null;
                if (requesttype.ToString().ToUpper() == "GET")
                {
                    MediaService.WriteLog("http请求GET请求：" + siteurl + "?" + query, MediaService.wirtelog);
                    request = (HttpWebRequest)WebRequest.Create(siteurl + "?" + query);
                    request.Timeout = 1000 * MediaService.httptimeout;
                    request.ReadWriteTimeout = 1000 * MediaService.httptimeout;
                    request.Method = "GET";
                }
                else
                {
                    MediaService.WriteLog("http请求POST请求：" + siteurl + "  POST：" + query, MediaService.wirtelog);
                    request = (HttpWebRequest)WebRequest.Create(siteurl);
                    request.Timeout = 1000 * MediaService.httptimeout;
                    request.ReadWriteTimeout = 1000 * MediaService.httptimeout;
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    byte[] bs = encoding.GetBytes(query);
                    request.ContentLength = bs.Length;
                    using (Stream reqStream = request.GetRequestStream())
                    {
                        reqStream.Write(bs, 0, bs.Length);
                        reqStream.Close();
                    }
                }
                System.Net.WebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader httpreader = new StreamReader(response.GetResponseStream(), encoding);
                string json = httpreader.ReadToEnd();
                httpreader.Close();
                response.Close();
                return json;
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                return "";
            }
        }
        #endregion

        #region 返回用户头像服务器路径
        private static string GetUserFaceUrl(int uid)
        {
            return MediaService.faceurl + uid.ToString().PadLeft(9, '0').Insert(7, "/").Insert(5, "/").Insert(3, "/");
        }
        #endregion

        #region 发送手机短信
        private static bool SendSmsMessage(string mobile, string content)
        {
            try
            {
                string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
                + "<soap:Body>"
                + "<AddMessageToLT xmlns=\"http://tempuri.org/\">"
                + "<xml>"
                + "&lt;SMS type=\"send\"&gt;"
                + "&lt;Message&gt;"
                + "&lt;MessageID&gt;99&lt;/MessageID&gt;" //短信编号
                + "&lt;PhoneNum&gt;" + mobile + "&lt;/PhoneNum&gt;" //手机号
                + "&lt;Content&gt;" + content + "&lt;/Content&gt;" //短信内容
                + "&lt;SendLevel&gt;0&lt;/SendLevel&gt;" //发送优先级别
                + "&lt;Type&gt;1&lt;/Type&gt;" //1 ：主动发送  2：回复发送 默认为1
                + "&lt;/Message&gt;"
                + "&lt;/SMS&gt;"
                + "</xml>"
                + "</AddMessageToLT>"
                + "</soap:Body>"
                + "</soap:Envelope>";

                byte[] data = Encoding.UTF8.GetBytes(xml);
                System.Net.HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(MediaService.smsurl);  //"http://192.168.6.219/gateway/server.php?wsdl"
                request.Method = "POST";
                request.ContentType = "text/xml; charset=utf-8";
                request.Expect = "";
                request.KeepAlive = false;
                request.ContentLength = data.Length;
                System.IO.Stream newStream = request.GetRequestStream();
                newStream.Write(data, 0, data.Length);//   发送数据
                newStream.Close();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream rece = response.GetResponseStream();
                StreamReader reader = new StreamReader(rece, Encoding.UTF8);
                string recvxml = reader.ReadToEnd();
                reader.Close();
                response.Close();
                if (recvxml.IndexOf("<ns1:AddMessageToLTResult>true</ns1:AddMessageToLTResult>") > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("发送手机短信异常：" + err.Message, MediaService.wirtelog);
                return false;
            }
        }
        #endregion

        #region MD5加密
        public static string StringToMD5Hash(string inputString)
        {
            MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
        #endregion

        #region 获取标准时间戳
        public static long GetTimeStamp()
        {
            return DateTime.UtcNow.Ticks / 10000000 - 62135596800;
        }
        #endregion

        #region 根据时间戳、用户ID、服务器ID获取文件ID
        public static StringBuilder GetFileID(int uid, ulong datetime)
        {
            StringBuilder sb = new StringBuilder(MediaService.ServiceID.PadLeft(3, '0'));
            sb.Append(uid.ToString().PadLeft(9, '0'));
            string time = datetime.ToString();
            if (time.Length > 13)
            {
                time = time.Substring(time.Length - 13, 13);
            }
            time = time.PadLeft(13, '0');
            sb.Append(time);
            return sb;
        }
        #endregion

        #region 获取文件ID获取文件路径
        public static string GetFileIdDir(StringBuilder sb)
        {
            sb.Insert(3, '/');
            sb.Insert(7, '/');
            sb.Insert(11, '/');
            sb.Insert(15, '/');
            sb.Insert(19, '/');
            sb.Insert(23, '/');
            sb.Insert(0, MediaService.fileurl);
            return sb.ToString();
        }
        public static string GetFileIdDir(string file)
        {
            StringBuilder sb = new StringBuilder(file);
            sb.Insert(3, '/');
            sb.Insert(7, '/');
            sb.Insert(11, '/');
            sb.Insert(15, '/');
            sb.Insert(19, '/');
            sb.Insert(23, '/');
            sb.Insert(0, MediaService.fileurl);
            return sb.ToString();
        }
        #endregion

        #region 生成缩略图
        public static void MakeThumbnail(string originalImagePath, string thumbnailPath, int width, int height, int mode, string imageType)
        {
            Image originalImage = Image.FromFile(originalImagePath);
            int towidth = width;
            int toheight = height;
            int x = 0;
            int y = 0;
            int ow = originalImage.Width;
            int oh = originalImage.Height;
            switch (mode)
            {
                //case 0://指定高宽缩放（可能变形）　　　　　　　　 
                //    break;
                case 1://指定宽，高按比例　　　　　　　　　　 
                    toheight = originalImage.Height * width / originalImage.Width;
                    break;
                case 2://指定高宽裁减（不变形）　　　　　　　　 
                    if ((double)originalImage.Width / (double)originalImage.Height > (double)towidth / (double)toheight)
                    {
                        oh = originalImage.Height;
                        ow = originalImage.Height * towidth / toheight;
                        y = 0;
                        x = (originalImage.Width - ow) / 2;
                    }
                    else
                    {
                        ow = originalImage.Width;
                        oh = originalImage.Width * height / towidth;
                        x = 0;
                        y = (originalImage.Height - oh) / 2;
                    }
                    break;
                //case 3://指定高，宽按比例 
                //    towidth = originalImage.Width * height / originalImage.Height;
                //    break;
                default:
                    break;
            }
            //新建一个bmp图片 
            Image bitmap = new System.Drawing.Bitmap(towidth, toheight);
            //新建一个画板 
            Graphics g = System.Drawing.Graphics.FromImage(bitmap);
            //设置高质量插值法 
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            //设置高质量,低速度呈现平滑程度 
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //清空画布并以透明背景色填充 
            g.Clear(Color.Transparent);
            //在指定位置并且按指定大小绘制原图片的指定部分 
            g.DrawImage(originalImage, new Rectangle(0, 0, towidth, toheight),
               new Rectangle(x, y, ow, oh),
               GraphicsUnit.Pixel);
            try
            {
                originalImage.Dispose();
                //以jpg格式保存缩略图 
                switch (imageType.ToLower())
                {
                    case "jpg":
                        bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;
                    case "gif":
                        bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Gif);
                        break;
                    case "bmp":
                        bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Bmp);
                        break;
                    case "png":
                        bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                    case "jpeg":
                        bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;
                    default:
                        bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;
                }
            }
            catch { }
            finally
            {
                bitmap.Dispose();
                g.Dispose();
            }
        }
        #endregion

        #region 获取获取两个经纬度之间的距离
        public static double GetDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double RAD = Math.PI / 180.0;
            double radLat1 = lat1 * RAD;
            double radLat2 = lat2 * RAD;
            double a = radLat1 - radLat2;
            double b = (lng1 - lng2) * RAD;

            double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) +
             Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2)));
            s = s * 6378.137;
            s = Math.Round(s * 10000) / 10000;
            return s;
        }
        #endregion

        #region 通过经纬度和距离计算经纬度范围（单位米）
        public static double[] getAround(double la, double lo, int raidus)
        {
            double[] lola = new double[2];

            double degree = (24901 * 1609) / 360.0;
            lola[0] = 1 / degree * raidus;

            double mpdLng = degree * Math.Cos(la * (Math.PI / 180));
            lola[1] = 1 / mpdLng * raidus;
            return lola;
        }
        #endregion

        #region 根据视频文件生成缩略图
        public static void VideoToThumbnailPic(string vFileName, string thumbnailPath)
        {
            System.Diagnostics.ProcessStartInfo ImgstartInfo = new System.Diagnostics.ProcessStartInfo(MediaService.ffmpeg);
            ImgstartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            //ImgstartInfo.UseShellExecute = false; //是否显示ffmpeg窗口
            /*参数设置
             * -y（覆盖输出文件，即如果生成的文件（flv_img）已经存在的话，不经提示就覆盖掉了）
             * -i 1.avi 输入文件
             * -f image2 指定输出格式
             * -ss 8 后跟的单位为秒，从指定时间点开始转换任务
             * -vframes
             * -s 指定分辨率
             */
            ImgstartInfo.Arguments = " -ss 0 -i " + vFileName + " -y -f image2 -vframes 1 " + thumbnailPath;
            try
            {
                System.Diagnostics.Process.Start(ImgstartInfo);
            }
            catch (Exception err)
            {
                MediaService.WriteLog("根据视频文件生成缩略图异常：" + err.Message, MediaService.wirtelog);
            }
        }
        #endregion

        #region json字符串转义
        public static string StringToJson(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '/':
                        sb.Append("\\/");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
        #endregion

        #region 检测字符串是否是字母数字中文组成
        public static bool IsValiNumCnEn(string str)
        {
            int chfrom = Convert.ToInt32("4e00", 16);    //范围（0x4e00～0x9fff）转换成int（chfrom～chend）
            int chend = Convert.ToInt32("9fff", 16);
            for (int i = 0; i < str.Length; i++)
            {
                char s = str[i];
                if (s > 47 && s < 58 || s > 64 && s < 91 || s > 96 && s < 123)
                {
                    continue;
                }
                else
                {
                    int code = char.ConvertToUtf32(str, i);
                    if (code >= chfrom && code <= chend)
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        #region 检测字符串是否是数字组成
        public static bool IsValiNum(string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                char s = str[i];
                if (s > 47 && s < 58)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region 修改用户手机号
        public static string ModiUserMobile(string login_key, string password, string mobile, string vcode)
        {
            string ssss = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=passport_service.login", "login_key=" + login_key + "&password=" + password + "&app_id=2014042900000006&time=" + (DateTime.UtcNow.Ticks / 10000000 - 62135596800), "POST", Encoding.UTF8);
            int k = ssss.IndexOf("\"token\":\"");
            if (k > 0)
            {
                int end = ssss.IndexOf("\"", k + 9);
                if (end > 0)
                {
                    string token = ssss.Substring(k + 9, end - k - 9);
                    k = ssss.IndexOf("\"user_id\":\"");
                    end = ssss.IndexOf("\"", k + 11);
                    string uid = ssss.Substring(k + 11, end - k - 11);

                    if (uid != "") //通过UID获取个人信息
                    {
                        string poststr = "action=userinfo.unbind_tel&app_id=2014042900000006&user_id=" + uid + "&ver=3.01";
                        string sign = CommBusiness.StringToMD5Hash(poststr + token).ToLower();
                        string t = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=userinfo.unbind_tel&app_id=2014042900000006&user_id=" + uid + "&ver=3.01&sign=" + sign, "user_id=" + uid, "POST", Encoding.UTF8);
                        MediaService.WriteLog(" RecvThread接收： t=" + t, MediaService.wirtelog);
                        if (t.IndexOf("\"code\":0") > 0)
                        {
                            poststr = "action=userinfo.set_base&app_id=2014042900000006&mobile=" + mobile + "&user_id=" + uid + "&vcode=" + vcode + "&ver=3.01";
                            sign = CommBusiness.StringToMD5Hash(poststr + token).ToLower();
                            t = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=userinfo.set_base&app_id=2014042900000006&user_id=" + uid + "&ver=3.01&sign=" + sign, "mobile=" + mobile + "&vcode=" + vcode, "POST", Encoding.UTF8);

                            MediaService.WriteLog(" RecvThread接收： t=" + t, MediaService.wirtelog);
                            if (t.IndexOf("\"code\":0") > 0)
                            {
                                SqlHelper.ExecuteNonQuery("delete [app_users] where uid='" + uid + "'");
                                return "{\"status\":true}";
                            }
                        }
                    }
                }
            }
            return WriteErrorJson(32);
        }
        #endregion

        #region 获取修改绑定手机号的验证码
        public static string GetBindMobileVcode(string login_key, string password, string mobile)
        {
            string ssss = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=passport_service.login", "login_key=" + login_key + "&password=" + password + "&app_id=2014042900000006&time=" + (DateTime.UtcNow.Ticks / 10000000 - 62135596800), "POST", Encoding.UTF8);
            MediaService.WriteLog(" RecvThread接收： t=" + ssss, MediaService.wirtelog);
            int k = ssss.IndexOf("\"token\":\"");
            if (k > 0)
            {
                int end = ssss.IndexOf("\"", k + 9);
                if (end > 0)
                {
                    string token = ssss.Substring(k + 9, end - k - 9);
                    k = ssss.IndexOf("\"user_id\":\"");
                    end = ssss.IndexOf("\"", k + 11);
                    string uid = ssss.Substring(k + 11, end - k - 11);

                    if (uid != "") //通过UID获取个人信息
                    {
                        string poststr = "action=verifycode.request_send_code&app_id=2014042900000006&keyword=" + mobile + "&lang=zh&user_id=" + uid + "&ver=3.01";
                        string sign = CommBusiness.StringToMD5Hash(poststr + token).ToLower();
                        string t = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=verifycode.request_send_code&app_id=2014042900000006&user_id=" + uid + "&ver=3.01&sign=" + sign, "keyword=" + mobile + "&lang=zh", "POST", Encoding.UTF8); ;
                        MediaService.WriteLog(" RecvThread接收： t=" + t, MediaService.wirtelog);
                        if (t.IndexOf("\"code\":0") > 0)
                        {
                            return "{\"status\":true}";
                        }
                        else
                        {
                            return "{\"status\":false}";
                        }
                    }
                }
            }
            return WriteErrorJson(32);
        }
        #endregion

        #region 更新缓存中的会话组用户
        public static void UpdateTalkUser(int tid, int uid, bool move)
        {
            TalkMessage talkmessage = null;
            if (MediaService.talkDic.TryGetValue(tid, out talkmessage))
            {
                List<int> uidlist = talkmessage.uidlist;
                if (move)//加入
                {
                    if (uidlist.Contains(uid) == false)
                        uidlist.Add(uid);
                }
                else//移除
                {
                    uidlist.Remove(uid);
                }
            }
        }
        #endregion

        #region 初始化群组对象缓存
        public static bool InitTalkMessage(int tid)
        {
            TalkMessage talkmessage = null;
            if (MediaService.talkDic.TryGetValue(tid, out talkmessage) == false)
            {
                DataTable dt = SqlHelper.ExecuteTable("select talkname,createuid from [wy_talk] where tid=" + tid);
                if (dt.Rows.Count > 0)
                {
                    string zsn = "";
                    string talkname = dt.Rows[0]["talkname"].ToString();
                    int createuid = Int32.Parse(dt.Rows[0]["createuid"].ToString());
                    object obj = SqlHelper.ExecuteScalar("select glsn from [app_users] where uid='" + createuid + "'");
                    if (obj != null)
                    {
                        zsn = obj.ToString();
                        if (zsn.Length > 8) zsn = zsn.Substring(zsn.Length - 8);
                    }
                    dt = SqlHelper.ExecuteTable("select uid from [wy_talkuser] where tid='" + tid + "' and duijiang=1");
                    if (dt.Rows.Count == 0)
                    {
                        return false;
                    }
                    List<int> uidlist = new List<int>();
                    foreach (DataRow dr in dt.Rows)
                    {
                        uidlist.Add(Int32.Parse(dr[0].ToString()));
                    }
                    TalkMessage newtalkmessage = new TalkMessage(uidlist, createuid, talkname, zsn);
                    MediaService.talkDic.TryAdd(tid, newtalkmessage);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region  检测是否符合频道规则的名称
        /// <summary>
        /// 自驾游项目保留靓号
        /// </summary>
        /// <param name="qhao"></param>
        /// <returns></returns>
        public static bool IsTalkNameOKWithNoToken(string qhao)
        {
            byte[] b = Encoding.ASCII.GetBytes(qhao);
            int c = 0;
            int x = 0;
            for (int i = 0, num = b.Length - 1; i < num; i++)
            {
                //相同数字
                if (b[i] == b[i + 1])
                {
                    c++;
                }
                else
                {
                    c = 0;
                }
                //连续数字
                if (b[i] == b[i + 1] + 1)
                {
                    x++;
                }
                else
                {
                    x = 0;
                }
                //是否退出循环
                if (c == 3 || x == 4)
                {
                    return false;
                }
                else if (b[0] == 9 && b[1] == 5)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsTalkNameOK(string qhao)
        {
            byte[] b = Encoding.ASCII.GetBytes(qhao);
            int c = 0;
            int x = 0;
            for (int i = 0, num = b.Length - 1; i < num; i++)
            {
                //相同数字
                if (b[i] == b[i + 1])
                {
                    c++;
                }
                else
                {
                    c = 0;
                }
                //连续数字
                if (b[i] == b[i + 1] + 1)
                {
                    x++;
                }
                else
                {
                    x = 0;
                }
                //是否退出循环
                if (c == 2 || x == 3)
                {
                    return false;
                }
                else if (b[0] == 9 && b[1] == 5)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        // 远端登陆验证
        public static bool VLogin(string sn, string snkey)
        {
            bool IsSuccess = false;
            string result = HttpRequestRoute("http://api.dbscar.com/gathercenterwebapi/", "methodname=getdevicekey&devicesn=" + sn, "GET", Encoding.UTF8); ;
            string s = HttpUtility.UrlDecode(result);
            string mycar = "";
            int k = s.IndexOf("\"Data\":\"");
            if (k > 0)
            {
                int e = s.IndexOf("\"", k + 8);
                if (e > k + 8)
                {
                    mycar = s.Substring(k + 8, e - k - 8);
                    if (mycar == snkey)
                    {
                        IsSuccess = true;
                    }
                }
            }
            if (!IsSuccess)
            {
                try
                {
                    StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\log\\" + DateTime.Now.ToString("yyyyMMdd") + "-v.txt", true);
                    sw.WriteLine("接收到登陆验证 ： 登陆验证失败" + sn + "，用户：" + snkey + ",系统：" + mycar);
                    sw.Close();
                }
                catch { }
            }
            return IsSuccess;
        }

        private static string GetHongBao(string user_id, string gift_id, string zsn)
        {
            //string url_inner = "http://base.api.dbscar.com/?";
            //String p = "action=gift_inner_service.get_hongbao&communicate_id=a2ad9b4babbba7e9&gift_id=" + gift_id + "&user_id=" + user_id + "&version=3.0.13f7e2d1c29285c76a46ece3ee1276927";
            //string sign = HttpZGoloBusiness.StringToMD5Hash(p).ToLower();
            //String url = url_inner + "action=gift_inner_service.get_hongbao&communicate_id=a2ad9b4babbba7e9&sign=" + sign + "&version=3.0.1";
            //String result = HttpZGoloBusiness.HttpRequestRoute(url, "gift_id=" + gift_id + "&user_id=" + user_id, "POST", Encoding.UTF8);
            //return result;

            ////http://golo.x431.com/system/?action=merchant_hongbao_service.get_hongbao&id=3&user_id=10003&sign=7c6b415e27d39042d9771a2b5062df00
            //string url_inner = "http://golo.x431.com/system/";
            //String p = "7c156207f2f67d74ef1b8b81490a4f71" + user_id;
            //string sign = HttpZGoloBusiness.StringToMD5Hash(p).ToLower();
            //String query = "action=merchant_hongbao_service.get_hongbao&id=" + gift_id + "&user_id=" + user_id + "&sign=" + sign;
            //String result = HttpZGoloBusiness.HttpRequestRoute(url_inner, query, "GET", Encoding.UTF8);
            //return result;

            //http://www.golo365.com.cn/?action=2001&ouid=116&id=50&sign=0ddf8956c24ad060b737341f4b4f1d69&golozsn=91001111    
            string url_inner = "http://sp.golo365.com";
            String p = "f970fd336aefa10b25f721fb1d7cde08" + user_id;
            string sign = CommFunc.StringToMD5Hash(p).ToLower();
            String query = "action=2001&ouid=" + user_id + "&id=" + gift_id + "&sign=" + sign + "&golozsn=" + zsn;
            String result = CommFunc.HttpRequestRoute(url_inner, query, "GET", Encoding.UTF8);
            return result;
        }

        #region 查询流量
        /// <summary>
        /// 查询流量
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string GetFlow(AsyncUserToken token, int packnum)
        {
            StringBuilder sb = new StringBuilder();
            string recv = null;
            if (token.uid != 0)
            {
                try
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    sb.Append("1118 查询流量:" + query);
                    string sn = GetJsonValue(query, "sn", "\"", true);
                    string sim = GetJsonValue(query, "sim", "\"", true);
                    sb.AppendFormat(" 设备sn={0}  sim={1}", sn, sim);

                    string requestQuery = "action=sim_card_service.get_flow_by_serial&serial_no=" + sn;
                    if (sim.Length >= 19)
                        requestQuery += "&sim=" + sim.Substring(0, 19);

                    string result = HttpRequestRoute("http://apps.api.dbscar.com/", requestQuery, "GET", Encoding.UTF8);
                    sb.Append(" result=" + result);
                    string code = GetJsonValue(result, "code", ",", false).Trim();
                    if (code == "0")
                    {
                        string data = GetJsonValue(result, "data", "}", false).Trim().TrimStart('{');//去掉开始处多余的{
                        recv = "{\"status\":true,\"data\":{" + data + ",\"localtotal\":\"8589934592\",\"countrytotal\":\"2147483648\"},\"message\":\"查询成功\"}";
                    }
                    else
                    {
                        string message = GetJsonValue(result, "msg", "\"", true);
                        recv = WriteErrorJson(3, message);
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("查询流量:执行异常：" + err, MediaService.wirtelog);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            MediaService.WriteLog(sb.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 点赞
        /// <summary>
        /// 点赞
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string Praise(AsyncUserToken token, int packnum)
        {
            StringBuilder sb = new StringBuilder("1120---------------------------------------uid=" + token.uid + "   packnum=" + packnum);
            string recv;
            if (token.uid != 0)
            {
                long messageId;
                DateTime date;
                if (packnum >= 16)
                {
                    messageId = BitConverter.ToInt64(token.buffer, 8);
                }
                else
                {
                    messageId = BitConverter.ToInt32(token.buffer, 4);
                }
                sb.Append("---messageId=" + messageId);
                try
                {
                    int ouid = 0;
                    int tid = 0;

                    if (messageId < int.MaxValue)
                    {
                        MessageInfo msg = InMemoryCache.Instance.GetData<MessageInfo>(messageId.ToString());
                        if (msg != null && msg.Time > 0)
                        {
                            MediaService.mapDic.TryGetValue(msg.Senduid, out ouid);
                            tid = msg.Tid;
                        }
                        else
                        {
                            sb.Append(" MessageInfo缓存过期 ");
                        }
                    }
                    else
                    {
                        VogMessage msg = InMemoryCache.Instance.GetData<VogMessage>(messageId.ToString());
                        if (msg == null || msg.CreateTime <= 0)
                        {
                            sb.Append(" VogMessage缓存过期 ");
                            var vmCol = MediaService.mongoDataBase.GetCollection("VogMessage_" + DateTime.Now.ToString("yyyyMMdd"));
                            var vmQuery = new QueryDocument { { "CreateTime", messageId } };
                            msg = vmCol.FindOneAs<VogMessage>(vmQuery);
                        }
                        if (msg != null)
                        {
                            ouid = msg.Ouid;
                            tid = msg.Tid;
                        }
                    }

                    MongoCollection col = MediaService.mongoDataBase.GetCollection("Parise_" + ouid % 10 + "_" + DateTime.Now.ToString("yyyyMM"));
                    var query = new QueryDocument { { "MsgTime", messageId }, { "Uid", token.uid } };
                    Parise p = col.FindOneAs<Parise>(query);
                    int isParise = 1;
                    if (p == null || p.LastModiTime == 0)
                    {
                        Parise parise = new Parise
                        {
                            LastModiTime = Utility.GetTimeStamp(),
                            MsgTime = messageId,
                            Uid = token.uid,
                            IsParise = isParise,
                            Ouid = ouid,
                            Tid = tid
                        };
                        col.Insert(parise);
                        p = parise;
                    }
                    else
                    {
                        isParise = (p.IsParise + 1) % 2;
                        var upd = new UpdateDocument { { "$set", new QueryDocument { { "IsParise", isParise } } } };
                        col.Update(query, upd);
                    }
                    sb.Append(" mongo保存成功， LastModiTime=").Append(p.LastModiTime).Append(" IsParise=").Append(p.IsParise);
                    sb.Append(" Ouid=").Append(p.Ouid).Append(" Tid=").Append(p.Tid);

                    return "{\"status\":true,\"isparise\":" + (isParise == 1).ToString().ToLower() + "}";
                }
                catch (Exception err)
                {
                    sb.Append(" 执行异常：").Append(err);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            MediaService.WriteLog(sb.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 语音搜索城市频道
        /// <summary>
        /// 语音搜索城市频道
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string SearchChannelByVoice(AsyncUserToken token, int packnum)
        {
            if (token.uid <= 0)
            {
                return WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            StringBuilder sb = new StringBuilder("{\"status\":true,\"radiolist\":[");
            try
            {
                string voiceResult = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                MediaService.WriteLog("1203 语音搜索频道:voiceResult=" + voiceResult, MediaService.wirtelog);
                //语音结果是数字就和channelname比较，文字和CityName模糊匹配
                voiceResult = voiceResult.Replace("频道", "");
                string sql = string.Format("SELECT rid, channelname, cityname, senduid, audiourl, channelde, radiotype, imageurl, thumburl, prid, flashimageurl FROM [dbo].[wy_radio] WHERE [channelname] = '{0}' OR [cityname] LIKE '{0}%'", voiceResult);
                DataTable dt = SqlHelper.ExecuteTable(sql);
                foreach (DataRow row in dt.Rows)
                {
                    string cityname = row["cityname"].ToString();
                    if (row["prid"].ToString() != "0")
                    {
                        DataTable subTable = SqlHelper.ExecuteTable(
                            "SELECT rid, channelname, cityname, senduid, audiourl, channelde,  radiotype, imageurl, thumburl, prid, flashimageurl FROM [dbo].[wy_radio] WHERE rid=" +
                            row["prid"]);
                        foreach (DataRow dr in subTable.Rows)
                        {
                            sb.Append(MakeRadioJson(dr, cityname));
                            sb.Append(",");
                        }
                    }
                    else
                    {
                        sb.Append(MakeRadioJson(row, cityname));
                        sb.Append(",");
                    }
                }
                string result = sb.ToString().TrimEnd(',') + "]}";
                MediaService.WriteLog("语音搜索频道结果：" + result, MediaService.wirtelog);
                return result;
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err, MediaService.wirtelog);
                return WriteErrorJson(6);
            }
        }
        /// <summary>
        /// 生成一个频道的json
        /// </summary>
        /// <param name="dr"></param>
        /// <param name="cityname"></param>
        /// <returns></returns>
        private static string MakeRadioJson(DataRow dr, string cityname)
        {
            return "{\"rid\":" + dr["rid"] + ",\"channelname\":\"" + dr["channelname"] + "\",\"cityname\":\"" + cityname +
                   "\",\"senduid\":\"" + dr["senduid"] + "\",\"audiourl\":\"" + dr["audiourl"] + "\",\"channelde\":" + dr["channelde"] +
                   ",\"radiotype\":" + dr["radiotype"] + ",\"imageurl\":\"" + dr["imageurl"] + "\",\"thumburl\":\"" +
                   dr["thumburl"] + "\",\"flashimageurl\":\"" + dr["flashimageurl"] + "\"}";
        }
        #endregion

        #region 语音对讲
        /// <summary>
        /// 语音对讲
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string VoiceIntercom(AsyncUserToken token, int packnum)
        {
            StringBuilder sb = new StringBuilder("1074---------------------------------------" + token.uid + "   " + packnum);
            MediaService.WriteLog(sb + "---start", MediaService.wirtelog);
            if (token.uid != 0)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(token.glsn), 0, token.buffer, 4, 4);
                int tid = BitConverter.ToInt32(token.buffer, 8);
                sb.Append("---tid:" + tid);
                MediaService.talkinfo.Add(new Talkinfo(tid, token.uid, GetTimeStamp()));
                if (InitTalkMessage(tid) == false)
                {
                    try
                    {
                        byte[] b = new byte[12];
                        Buffer.BlockCopy(BitConverter.GetBytes((short)12), 0, b, 0, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(0), 0, b, 4, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(tid), 0, b, 8, 4);
                        token.Socket.Send(b, SocketFlags.None);
                        MediaService.WriteLog("语音发送到对讲组，用户不在此组：uid=" + token.uid, MediaService.wirtelog);
                    }
                    catch { }
                    return null;
                }

                TalkMessage talkmessage;
                if (MediaService.talkDic.TryGetValue(tid, out talkmessage))
                {
                    talkmessage.micticks = DateTime.Now.Ticks;
                    int messageId = 0;
                    int ouid;
                    try
                    {
                        MediaService.mapDic.TryGetValue(token.uid, out ouid);
                        string json = string.Format("{{\"sn\":{0},\"ouid\":\"{1}\"}}", token.glsn, ouid);

                        MessageInfo msg = null;
                        byte packId = token.buffer[12];
                        //                        MediaService.WriteLog("packId=" + packId + "  生成json：" + json, MediaService.wirtelog);
                        IEnumerable<MessageInfo> msgs = QueryMessageInfo(tid, packId, token.uid, DateTime.Now);

                        if (msgs != null && msgs.Any())
                        {
                            int maxTime = msgs.Max(m => m.Time);
                            msg = msgs.LastOrDefault(m => m.Time == maxTime);
                        }

                        if (msg == null || msg.Time == 0)
                        {
                            msg = new MessageInfo
                            {
                                Time = int.Parse(DateTime.Now.ToString("HHmmssfff")),
                                Tid = tid,
                                Senduid = token.uid,
                                Message = json,
                                PackId = packId
                            };
                            MongoCollection col = MediaService.mongoDataBase.GetCollection("MessageInfo_" + DateTime.Now.ToString("yyyyMMdd"));
                            col.Insert(msg);
                            InMemoryCache.Instance.Add(msg.Time.ToString(), msg, DateTime.Now.AddMinutes(2));
                        }


                        messageId = msg.Time;
                        MediaService.WriteLog("msg.Time=" + msg.Time, MediaService.wirtelog);
                    }
                    catch (Exception ex)
                    {
                        MediaService.WriteLog("Mongo记录聊天消息出错：" + ex.Message, MediaService.wirtelog);
                        return null;
                    }

                    byte[] bf = new byte[packnum - 4];
                    try
                    {
                        Buffer.BlockCopy(BitConverter.GetBytes(messageId), 0, token.buffer, 8, 4);

                        Buffer.BlockCopy(token.buffer, 0, bf, 0, 13);
                        Buffer.BlockCopy(BitConverter.GetBytes(packnum - 4), 0, bf, 0, 2);
                        Buffer.BlockCopy(BitConverter.GetBytes(CommType.sendAudioToTalk), 0, bf, 2, 2);
                        //messageId覆盖tid（语音数据前4byte），数据预留区中去掉ouid
                        Buffer.BlockCopy(BitConverter.GetBytes(messageId), 0, bf, 8, 4);
                        Buffer.BlockCopy(token.buffer, 17, bf, 13, packnum - 17);

                        //测试通话质量
                        //byte[] bf1 = new byte[packnum];
                        //Buffer.BlockCopy(token.buffer, 0, bf1, 0, packnum);
                        //new Task(() =>
                        //{
                        //    string datetime = DateTime.Now.ToString("yyyyMMddHHmmssffff");
                        //    WriteTalk(bf1, 17, bf1.Length - 17, tid, token.glsn, datetime + "_1074_1074");
                        //    WriteTalk(bf, 13, bf.Length - 13, tid, token.glsn, datetime + "_1074_1067");
                        //}).Start();
                    }
                    catch (Exception ex)
                    {
                        MediaService.WriteLog("生成buffer出错：" + ex.Message, MediaService.wirtelog);
                    }
                    Task parent = new Task(() =>
                    {
                        sb.Append("-uidcount:" + talkmessage.uidlist.Count);
                        bool state = false;
                        foreach (int uid in talkmessage.uidlist)
                        {
                            UserObject uo;
                            if (uid != token.uid)
                            {
                                if (MediaService.userDic.TryGetValue(uid, out uo))
                                {
                                    if (uo.socket != null && uo.socket[token.appid] != null)
                                    {
                                        if (uo.socket[token.appid].Connected)
                                        {
                                            new Task(() =>
                                            {
                                                try
                                                {
                                                    if (uo.ver < 100)
                                                    {
                                                        uo.socket[token.appid].Send(bf, 0, bf.Length, SocketFlags.None);
                                                    }
                                                    else
                                                    {
                                                        uo.socket[token.appid].Send(token.buffer, 0, packnum, SocketFlags.None);
                                                    }
                                                    MediaService.WriteLog("语音发送完成，目标uid=" + uid, MediaService.wirtelog);
                                                }
                                                catch (Exception err)
                                                {
                                                    MediaService.WriteLog("语音发送异常：uid=" + uid + "     " + err.Message, MediaService.wirtelog);
                                                }
                                            }, TaskCreationOptions.AttachedToParent).Start();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                state = true;
                            }
                        }
                        if (state == false)
                        {
                            try
                            {
                                byte[] b = new byte[12];
                                Buffer.BlockCopy(BitConverter.GetBytes((short)12), 0, b, 0, 2);
                                Buffer.BlockCopy(BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                                Buffer.BlockCopy(BitConverter.GetBytes(0), 0, b, 4, 4);
                                Buffer.BlockCopy(BitConverter.GetBytes(tid), 0, b, 8, 4);
                                token.Socket.Send(b, SocketFlags.None);
                                MediaService.WriteLog("语音发送到对讲组，用户不在此组：uid=" + token.uid, MediaService.wirtelog);
                            }
                            catch { }
                        }
                    });
                    parent.Start();
                    parent.Wait();
                    MediaService.WriteLog(sb.ToString(), MediaService.wirtelog);
                }
            }
            return null;
        }
        #endregion

        #region 新-获取我的频道列表
        #region 获取频道数
        public static void GetTalkNum(int tid, ref int totalnum, int appid, ref int usernum)
        {
            totalnum = 0;
            usernum = 0;
            TalkMessage tm;
            object obj = SqlHelper.ExecuteScalar("SELECT count(*) FROM wy_talkuser WHERE tid=" + tid);
            if (obj != null)
            {
                totalnum = Int32.Parse(obj.ToString());
                if (MediaService.talkDic.TryGetValue(tid, out tm))
                {
                    UserObject uo = null;
                    foreach (int uid in tm.uidlist)
                    {
                        if (MediaService.userDic.TryGetValue(uid, out uo))
                        {
                            if (uo.socket != null && uo.socket[appid] != null)
                            {
                                if (uo.socket[appid].Connected)
                                {
                                    usernum++;
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// 新-获取我的频道列表
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string NewGetMyAllChannel(AsyncUserToken token, int packnum)
        {
            string result;
            StringBuilder sb = new StringBuilder("{\"status\":true,\"talklist\":[");
            StringBuilder log = new StringBuilder("1121 获取我的频道列表：uid=" + token.uid + ",sn=" + token.glsn);
            try
            {
                List<int> tids = new List<int>();
                //old sql
                //string sql = string.Format(
                //        "SELECT t.*,u.glsn,tu.remark FROM [dbo].[wy_talkuser] tu INNER JOIN [dbo].[wy_talk] t ON tu.[uid]={0} AND tu.tid=t.tid INNER JOIN [dbo].[app_users] u ON t.[createuid]=u.[uid]",
                //        token.uid.ToString());
                //string sql = string.Format(
                //        "SELECT t.*,tu.remark FROM [dbo].[wy_talkuser] tu INNER JOIN [dbo].[wy_talk] t ON tu.[uid]={0} AND tu.tid=t.tid",token.uid.ToString());

                string sql =
                    string.Format(
                        "select *, null as glsn from( SELECT t.*,tu.remark FROM [dbo].[wy_talkuser] tu INNER JOIN [dbo].[wy_talk] t ON tu.[uid]={0} AND tu.tid=t.tid) t where t.createuid=-100 union SELECT t.*,tu.remark,u.glsn FROM [dbo].[wy_talkuser] tu,[dbo].[wy_talk] t, [dbo].[app_users] u where tu.[uid]={0} and tu.tid=t.tid and t.[createuid]=u.[uid] union select wt.*,wu.remark,null as glsn from wy_talk wt , wy_talkuser wu where  wt.tid=wu.tid and wu.uid=(select ouid from wy_uidmap t where t.uid={0})", token.uid.ToString()); //此sql要union上当前uid所属的ouid加入的频道列表.不然和手机app上看到频道数不一样.
                MediaService.WriteLog("1121sql" + sql, MediaService.wirtelog);
                //string sqlouid =
                //    string.Format(
                //        "select wt.*,wu.remark,null as glsn from wy_talk wt , wy_talkuser wu where  wt.tid=wu.tid and wu.uid=(select ouid from wy_uidmap t where t.uid={0})", token.uid);//根据ouid查询加入的手机创建的频道
                //MediaService.WriteLog("1121sql---" + sqlouid, MediaService.wirtelog);
                foreach (DataRow row in SqlHelper.ExecuteTable(sql).Rows)
                {
                    int tid;
                    int.TryParse(row["tid"].ToString(), out tid);
                    if (!tids.Contains(tid))
                        tids.Add(tid);
                    else
                        continue;

                    bool create = false;
                    int createuid;
                    int.TryParse(row["createuid"].ToString(), out createuid);
                    int totalnum = 0;
                    int usernum = 0;
                    GetTalkNum(tid, ref totalnum, token.appid, ref usernum);

                    if (createuid == token.uid)
                    {
                        create = true;
                    }
                    else if (usernum <= 20)
                    {
                        create = true;
                    }
                    if (row["type"]!=null && row["type"].ToString() == "3")
                    {
                        sb.Append("{\"tid\":" + row["tid"] + ",\"talkname\":\"" + row["talkname"] + "\",\"auth\":\"" + (row["auth"] ?? "") + "\",\"remark\":\"" + (row["talknotice"] ?? "") + "\",\"createuid\":" + (row["createuid"] ?? "0") + ",\"usernum\":" + usernum + ",\"talkmode\":" + row["talkmode"] + ",\"totalnum\":" + totalnum + ",\"type\":" + (row["type"] ?? "0") + ",\"imageurl\":\"" + (row["imageurl"] ?? "") + "\",\"create\":" + create.ToString().ToLower() + "},"); //   ,\"glsn\":\"\"
                    }
                    else
                    {
                        if (row["glsn"] != null && row["glsn"].ToString() != string.Empty)
                        {
                            var remark = row["remark"] ?? "";
                            if (row["talknotice"] != null)
                                remark = row["talknotice"].ToString();
                            
                            sb.Append("{\"tid\":" + row["tid"] + ",\"talkname\":\"" + row["talkname"] + "\",\"auth\":\"" + (row["auth"] ?? "") + "\",\"remark\":\"" + remark + "\",\"createuid\":" + (row["createuid"] ?? "0") + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + ",\"glsn\":" + row["glsn"] + ",\"talkmode\":" + row["talkmode"] + ",\"type\":" + (row["type"] ?? "0") + ",\"imageurl\":\"" + (row["imageurl"] ?? "") + "\",\"create\":" + create.ToString().ToLower() + "},");
                        }
                        else
                        {
                            var remark = row["remark"] ?? "";
                            if (row["talknotice"] != null)
                                remark = row["talknotice"].ToString();
                            
                            sb.Append("{\"tid\":" + row["tid"] + ",\"talkname\":\"" + row["talkname"] + "\",\"auth\":\"" + (row["auth"] ?? "") + "\",\"remark\":\"" + remark + "\",\"createuid\":" + (row["createuid"] ?? "0") + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + ",\"talkmode\":" + row["talkmode"] + ",\"type\":" + (row["type"] ?? "0") + ",\"imageurl\":\"" + (row["imageurl"] ?? "") + "\",\"create\":" + create.ToString().ToLower() + "},");     //   \"glsn\":\"" + (row["glsn"] ?? "") + "\",
                        }
                    }
                }
                result = sb.ToString().TrimEnd(',');
                MediaService.WriteLog("sb-----------" + result, MediaService.wirtelog);
                sb.Clear();
                sb.Append("],\"radiolist\":[");
                List<string> ridlist = token.praido.Split(',').ToList();
                foreach (KeyValuePair<int, RadioObject> kv in MediaService.radioDic)
                {
                    RadioObject ro = kv.Value;
                    if (ro.prid == 0 && ro.radiotype != 0)
                    {
                        string rid = kv.Key.ToString();
                        if (ro.radiotype == 2 && !ridlist.Contains(rid))
                        {
                            continue;
                        }

                        string senduid;
                        if (ro.sendtype == 1)
                        {
                            senduid = (ro.senduid != null && ro.senduid.Contains(token.uid)) ? "0" : "-1";
                        }
                        else if (ro.sendtype == 2)
                        {
                            senduid = (ro.senduid != null && ro.senduid.Contains(token.uid)) ? "-1" : "0";
                        }
                        else
                        {
                            senduid = ro.sendtype.ToString();
                        }
                        sb.Append("{\"rid\":" + rid + ",\"channelname\":\"" + ro.channelname + "\",\"cityname\":\"" + ro.cityname + "\",\"senduid\":\"" + senduid + "\",\"audiourl\":\"" + ro.audiourl + "\",\"channelde\":" + ro.channelde + ",\"radiotype\":" + ro.radiotype + ",\"imageurl\":\"" + ro.imageurl + "\",\"thumburl\":\"" + ro.thumburl + "\",\"flashimageurl\":\"" + ro.flashimageurl + "\"},");
                    }
                }
                result += sb.ToString().TrimEnd(',');
                result += "]}";
                MediaService.WriteLog("result-----------" + result, MediaService.wirtelog);
            }
            catch (Exception err)
            {
                log.Append(" 执行异常：").Append(err);
                return WriteErrorJson(6);
            }
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return result;
        }
        #endregion

        #region 删除通讯录---DeleteContact = 1122;
        /// <summary>
        /// 删除通讯录
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string DeleteContact(AsyncUserToken token, int packnum)
        {
            string recv = "";
            if (token.uid != 0)
            {
                try
                {
                    int fuid = BitConverter.ToInt32(token.buffer, 8);
                    MediaService.WriteLog("接收到1122 删除通讯录 ：uid=" + token.uid + ",fuid =" + fuid, MediaService.wirtelog);
                    if (fuid <= 0)
                        return WriteErrorJson(6, "fuid error");

                    StringBuilder strSql = new StringBuilder();
                    int ouid;
                    MediaService.mapDic.TryGetValue(token.uid, out ouid);
                    strSql.Append("delete from [wy_userrelation] where (uid =" + token.uid + " or ouid=" + ouid + ") and fuid =" + fuid);
                    strSql.Append(";UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + token.uid);
                    int count = SqlHelper.ExecuteNonQuery(strSql.ToString());
                    MediaService.WriteLog("删除通讯录 执行sql count=" + count, MediaService.wirtelog);
                    if (count > 0)
                    {
                        return "{\"status\":true,\"fuid\":" + fuid + "}";
                    }
                    return WriteErrorJson(6, "没有该好友");
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("删除通讯录 执行异常：" + err, MediaService.wirtelog);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 添加wifi---AddUserWifi = 1093
        /// <summary>
        /// 添加wifi
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string AddUserWifi(AsyncUserToken token, int packnum)
        {
            /* NameValueCollection 值列表
             * ouid,name,password,iswave
             */

            string recv;
            var log = new StringBuilder(128);
            if (token.uid != 0)
            {
                try
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    log.Append("1093 添加wifi query:").Append(query);
                    int ouid = GetJsonValue(query, "ouid", ",", false).ToInt();
                    string name = GetJsonValue(query, "name", "\"", true);
                    string password = GetJsonValue(query, "password", "\"", true);
                    string iswave = GetJsonValue(query, "iswave", "\"", true);

                    string sql = @"
        IF NOT EXISTS(SELECT 1 FROM wy_userwifi WHERE ouid=@ouid AND name=@name)
        BEGIN
            INSERT INTO [wy_userwifi]([ouid],[name],[password])VALUES(@ouid,@name,@password);SELECT @@IDENTITY;
        END
        ELSE SELECT -1;";
                    SqlParameter[] paras =
                    {
                        new SqlParameter("@ouid", ouid)
                        ,new SqlParameter("@name", name)
                        ,new SqlParameter("@password", password)
                    };
                    int id = SqlHelper.ExecuteScalar(sql, paras).ToString().ToInt();
                    log.Append(" id=").Append(id);
                    if (id > 0)
                    {
                        recv = string.Format("{{\"status\":true,\"id\":{0},\"name\":\"{1}\",\"password\":\"{2}\",\"iswave\":\"{3}\"}}", id.ToString(), name, password, iswave);
                    }
                    else if (iswave == "0")
                    {
                        recv = WriteErrorJson(6, "该wifi已存在");
                    }
                    else
                    {
                        sql = "SELECT id FROM wy_userwifi WHERE ouid=@ouid AND name=@name";
                        SqlParameter[] para =
                        {
                            new SqlParameter("@ouid", ouid)
                            ,new SqlParameter("@name", name)
                        };
                        id = SqlHelper.ExecuteScalar(sql, para).ToString().ToInt();
                        recv = string.Format("{{\"status\":true,\"id\":{0},\"name\":\"{1}\",\"password\":\"{2}\",\"iswave\":\"{3}\"}}", id.ToString(), name, password, iswave);
                    }
                }
                catch (Exception e)
                {
                    log.Append(" 执行异常：").Append(e);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 删除wifi---DeleteUserWifi = 1092
        /// <summary>
        /// 删除wifi
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        public static string DeleteUserWifi(AsyncUserToken token, int packnum)
        {
            /* NameValueCollection 值
             * {"ouid":1871558,"id":33727}
             */

            string recv;
            var log = new StringBuilder(128);
            if (token.uid != 0)
            {
                try
                {
                    string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                    log.Append("1092 删除wifi query:").Append(query);
                    int ouid = GetJsonValue(query, "ouid", ",", false).ToInt();
                    int id = GetJsonValue(query, "id", "}", false).ToInt();

                    var paras = new List<SqlParameter> { new SqlParameter("@id", id) };
                    string sql = "SELECT name FROM wy_userwifi WHERE id=@id";
                    var obj = SqlHelper.ExecuteScalar(sql, paras.ToArray());
                    string name = "";
                    if (obj != null)
                    {
                        name = obj.ToString();
                    }
                    sql = "DELETE FROM wy_userwifi WHERE id=@id AND ouid=@ouid";
                    paras.Add(new SqlParameter("@ouid", ouid));
                    SqlHelper.ExecuteNonQuery(sql, paras.ToArray());
                    ///todo:待上线
                    //                    recv = "{\"status\":true,\"id\":" + id + ",\"name\":\"" + name + "\"}";
                    recv = "{\"status\":true,\"id\":" + id + "}";
                }
                catch (Exception e)
                {
                    log.Append(" 执行异常：").Append(e.Message);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 送赞---GivePraise--- 1124
        /// <summary>
        /// 送赞
        /// </summary>
        /// <param name="token"></param>
        /// <param name="packnum"></param>
        /// <returns></returns>
        internal static string GivePraise(AsyncUserToken token, int packnum)
        {
            StringBuilder sb = new StringBuilder("1124------uid=" + token.uid + "   packnum=" + packnum);
            string recv;
            if (token.uid != 0)
            {
                try
                {
                   string query = Encoding.UTF8.GetString(token.buffer, 8, packnum - 8);
                }
                catch (Exception err)
                {
                    sb.Append(" 执行异常：").Append(err);
                    recv = WriteErrorJson(6);
                }
            }
            else
            {
                recv = WriteErrorJson(3, "你还没有登陆，请稍后再试！");
            }
            return null;
        }
        #endregion

        #region 获取点赞刷新数据---GivePraiseRefresh---1125
        internal static string GivePraiseRefresh(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 当前用户的可用赞数---UsablePraises---1126
        internal static string UsablePraises(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 获取自定义歌单列表--GetCustomSoundCategories--1127
        internal static string GetCustomSoundCategories(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion 

        #region 删除歌单--DeleteSoundCategories--1128
        internal static string DeleteSoundCategories(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 获取喜欢的媒体列表--GetFavoriteSoundList--1129
        internal static string GetFavoriteSoundList(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 获取媒体订阅列表--GetSubscribeSoundList--1130
        internal static string GetSubscribeSoundList(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 添加媒体到自定义歌单--AddToCustomSoundCategories--1131
        internal static string AddToCustomSoundCategories(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 播放专辑--PlayAlbum--1132
        internal static string PlayAlbum(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 播放歌单--PlaySoundCategories--1133
        internal static string PlaySoundCategories(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 播放媒体--PlaySound--1134
        internal static string PlaySound(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 播放模式--PlayMode--1135
        internal static string PlayMode(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 电台订阅--RadioSubscribe--1136
        internal static string RadioSubscribe(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 推送导航--PushNavigation--1137
        internal static string PushNavigation(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region 开启热点--OpenHotspot--1138
        internal static string OpenHotspot(AsyncUserToken token, int packnum)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}