﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Data;
using System.Collections.Specialized;
using System.Web;
using System.Collections;
using MongoDB.Driver;


//可信系统服务器IP验证

namespace MediaService
{
    class HttpSysBusiness
    {
        #region 获取用户基本信息
        public static string GetUserMessage(NameValueCollection qs)
        {
            DataTable dt = new DataTable();
            if (SystemVerification())
            {
                if (qs != null)
                {
                    string uid = "";
                    string login_key = "";
                    string nickname = "";
                    string username = "";
                    string email = "";
                    string gender = "";
                    string mobile = "";
                    string fm = "";
                    string debug = "";
                    if (qs["uid"] != null)
                    {
                        uid = qs["uid"].Replace("'", "");
                        if (uid == "0" || uid == "")
                        {
                            return HttpService.WriteErrorJson("获取个人信息失败！");
                        }
                        dt = SqlHelper.ExecuteTable("select uid,gender,username,nickname,email,mobile from [app_users] where uid='" + uid + "'");
                    }
                    else if (qs["login_key"] != null)
                    {
                        login_key = qs["login_key"].ToString();
                        if (login_key == "")
                        {
                            return HttpService.WriteErrorJson("获取个人信息失败！");
                        }
                        string key = "username";
                        if (login_key.Length == 12 && login_key[0] == '9')
                        {
                            key = "glsn";
                        }
                        else if (login_key.IndexOf('@') > 0)
                        {
                            key = "email";
                        }
                        else if (login_key[0] > 47 && login_key[0] < 58)
                        {
                            key = "mobile";
                        }
                        dt = SqlHelper.ExecuteTable("select uid,gender,username,nickname,email,mobile,fm,debug from[app_users] where " + key + "='" + login_key + "'");
                    }
                    if (dt.Rows.Count > 0)
                    {
                        uid = dt.Rows[0]["uid"].ToString();
                        nickname = dt.Rows[0]["nickname"].ToString();
                        username = dt.Rows[0]["username"].ToString();
                        gender = dt.Rows[0]["gender"].ToString();
                        email = dt.Rows[0]["email"].ToString();
                        mobile = dt.Rows[0]["mobile"].ToString();
                        fm = dt.Rows[0]["fm"].ToString();
                        debug = dt.Rows[0]["debug"].ToString();
                    }
                    if (uid == "" || username == "")
                    {
                        return HttpService.WriteErrorJson("获取个人信息失败！");
                    }
                    else
                    {
                        return "{\"status\":true,\"uid\":" + uid + ",\"username\":\"" + username + "\",\"nickname\":\"" + nickname + "\",\"gender\":\"" + gender + "\",\"email\":\"" + email + "\",\"mobile\":\"" + mobile + "\",\"fm\":" + fm + ",\"debug\":" + debug + "}";
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("请求参数不正确！");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("用户认证失败！");
            }
        }
        #endregion

        #region 获取用户详细信息-Win
        public static string GetUserMoreMessage(NameValueCollection qs)
        {
            DataTable dt = new DataTable();
            if (qs != null)
            {
                if (!SystemVerification())
                {
                    return HttpService.WriteErrorJson("对不起您没有权限，请确认登陆！");
                }
                string uid = "";
                string login_key = "";
                string nickname = "";
                string username = "";
                string email = "";
                string gender = "";
                string mobile = "";

                string roles = "";//用户角色
                string glsn = "";
                string reg_zone = "";//注册区域，头像文件将保存在其区域服务器
                string face_ver = "";// 头像版本
                string user_lon = "";//用户经度
                string user_lat = "";//用户纬度
                string car_lon = "";//
                string car_lat = "";
                string car_id = "";
                string car_name = "";
                string car_plate = "";
                string output_volume = "";
                string car_code = "";
                string car_active = "";

                if (qs["uid"] != null)
                {
                    uid = qs["uid"].Replace("'", "");
                    if (uid == "0" || uid == "")
                    {
                        return HttpService.WriteErrorJson("获取个人信息失败！");
                    }
                    string byuidStr = @"  select a.uid,gender,username,nickname,email,roles,mobile,glsn,
              b.car_active,b.car_code,b.car_id,b.car_lat,b.car_lon,b.car_name,b.car_plate,b.face_ver,b.output_volume, b.reg_zone,b.user_lat,b.user_lon
              from [app_users] a left join app_userscar b on a.uid=b.uid
              where a.uid='" + uid + "'";
                    dt = SqlHelper.ExecuteTable(byuidStr);
                }
                else if (qs["login_key"] != null)
                {
                    login_key = qs["login_key"].ToString();
                    if (login_key == "")
                    {
                        return HttpService.WriteErrorJson("获取个人信息失败！");
                    }
                    string key = "username";
                    if (login_key.IndexOf('@') > 0)
                    {
                        key = "email";
                    }
                    else if (login_key[0] > 47 && login_key[0] < 58)
                    {
                        key = "mobile";
                    }
                    string bykeyStr = @"  select a.uid,gender,username,nickname,email,roles,mobile,glsn,
              b.car_active,b.car_code,b.car_id,b.car_lat,b.car_lon,b.car_name,b.car_plate,b.face_ver,b.output_volume, b.reg_zone,b.user_lat,b.user_lon
              from [app_users] a left join app_userscar b on a.uid=b.uid
              where " + key + "='" + login_key + "'";
                    dt = SqlHelper.ExecuteTable(bykeyStr);
                }

                if (dt.Rows.Count > 0)
                {
                    uid = dt.Rows[0]["uid"].ToString();
                    nickname = dt.Rows[0]["nickname"].ToString();
                    username = dt.Rows[0]["username"].ToString();
                    gender = dt.Rows[0]["gender"].ToString();
                    email = dt.Rows[0]["email"].ToString();
                    mobile = dt.Rows[0]["mobile"].ToString();

                    glsn = dt.Rows[0]["glsn"].ToString();
                    roles = dt.Rows[0]["roles"].ToString();
                    reg_zone = dt.Rows[0]["reg_zone"].ToString();
                    face_ver = dt.Rows[0]["face_ver"].ToString();
                    user_lon = dt.Rows[0]["user_lon"].ToString();
                    user_lat = dt.Rows[0]["user_lat"].ToString();
                    car_lon = dt.Rows[0]["car_lon"].ToString();
                    car_lat = dt.Rows[0]["car_lat"].ToString();
                    car_id = dt.Rows[0]["car_id"].ToString();
                    car_name = dt.Rows[0]["car_name"].ToString();
                    car_plate = dt.Rows[0]["car_plate"].ToString();
                    output_volume = dt.Rows[0]["output_volume"].ToString();
                    car_code = dt.Rows[0]["car_code"].ToString();
                    car_active = dt.Rows[0]["car_active"].ToString();
                }
                if (uid == "" || username == "")
                {
                    return HttpService.WriteErrorJson("没有找到此用户！");
                }
                else
                {
                    return "{\"status\":true,\"uid\":" + uid + ",\"username\":\"" + username + "\",\"nickname\":\"" + nickname + "\",\"gender\":\"" + gender + "\",\"email\":\"" + email + "\",\"mobile\":\"" + mobile
                        + "\",\"roles\":\"" + roles
                         + "\",\"glsn\":\"" + glsn
                        + "\",\"reg_zone\":\"" + reg_zone
                        + "\",\"face_ver\":\"" + face_ver
                        + "\",\"user_lon\":\"" + user_lon
                        + "\",\"user_lat\":\"" + user_lat
                        + "\",\"car_lon\":\"" + car_lon
                        + "\",\"car_lat\":\"" + car_lat
                        + "\",\"car_id\":\"" + car_id
                        + "\",\"car_name\":\"" + car_name
                        + "\",\"car_plate\":\"" + car_plate
                        + "\",\"output_volume\":\"" + output_volume
                        + "\",\"car_code\":\"" + car_code
                        + "\",\"car_active\":\"" + car_active
                        + "\"}";
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求参数不正确！");
            }
        }
        #endregion

        #region 获取频道基本信息
        public static string GetTalkMessage(NameValueCollection qs)
        {
            if (SystemVerification())
            {
                if (qs != null && qs["appid"] != null && qs["tid"] != null)
                {
                    string tid = qs["tid"].ToString().Trim(',');
                    if (tid != "")
                    {
                        tid = tid.Replace("'", "").Replace(",", "' or tid='");
                        StringBuilder sb = new StringBuilder();
                        DataTable dt = SqlHelper.ExecuteTable("select tid,talkname from [wy_talk] where tid ='" + tid + "'");
                        foreach (DataRow dr in dt.Rows)
                        {
                            string talkname = dr["talkname"].ToString();
                            string tide = dr["tid"].ToString();
                            sb.Append(",{\"tid\":" + tide + ",\"talkname\":\"" + talkname + "\"}");
                        }
                        if (dt.Rows.Count > 0)
                        {
                            sb.Remove(0, 1);
                        }
                        sb.Insert(0, "{\"status\":true,\"list\":[");
                        sb.Append("]}");
                        return sb.ToString();
                    }
                    else
                    {
                        return HttpService.WriteErrorJson("请求参数错误！");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("请求参数不正确！");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("用户认证失败！");
            }
        }
        #endregion

        #region 根据用户ID列表获取基本信息
        public static string GetUserIdListMessage(NameValueCollection qs)
        {
            DataTable dt = new DataTable();
            if (SystemVerification())
            {
                if (qs != null)
                {
                    StringBuilder sb = new StringBuilder();
                    string uid = "";
                    string nickname = "";
                    string username = "";
                    string email = "";
                    string gender = "";
                    string mobile = "";
                    if (qs["uid"] != null && qs["uid"].ToString() != "")
                    {
                        string uidlist = qs["uid"].Replace("'", "").Trim(',').Replace(",", "' or uid='");
                        dt = SqlHelper.ExecuteTable("select uid,gender,username,nickname,email,mobile from [app_users] where uid='" + uidlist + "'");
                        foreach (DataRow dr in dt.Rows)
                        {
                            uid = dr["uid"].ToString();
                            nickname = dr["nickname"].ToString();
                            username = dr["username"].ToString();
                            gender = dr["gender"].ToString();
                            email = dr["email"].ToString();
                            mobile = dr["mobile"].ToString();
                            sb.Append(",{\"uid\":" + uid + ",\"username\":\"" + username + "\",\"nickname\":\"" + nickname + "\",\"gender\":\"" + gender + "\",\"email\":\"" + email + "\",\"mobile\":\"" + mobile + "\"}");
                        }
                        if (sb.Length > 0) sb.Remove(0, 1);
                        sb.Insert(0, "{\"status\":true,\"list\":[");
                        sb.Append("]}");
                        return sb.ToString();
                    }
                    else
                    {
                        return HttpService.WriteErrorJson("获取个人信息失败！");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("请求参数不正确！");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("用户认证失败！");
            }
        }
        #endregion

        #region 获取系统信息
        public static string GetSystemMessage(NameValueCollection qs)
        {
            if (qs != null && qs["appid"] != null)
            {
                int appid = Int32.Parse(qs["appid"].ToString());
                StringBuilder sb = new StringBuilder("{\"status\":true");
                if (SystemVerification())
                {
                    object obj = SqlHelper.ExecuteScalar("select count(uid) from [app_users]");
                    if (obj != null)
                    {
                        sb.Append(",\"usercount\":" + obj.ToString() + "");
                    }
                    else
                    {
                        sb.Append(",\"usercount\":0");
                    }
                    obj = SqlHelper.ExecuteScalar("select count(uid) from [app_users] where glsn like '9716%'");
                    sb.Append(",\"golozcount\":" + obj.ToString() + "");

                    int useronlinecount = 0;
                    foreach (var item in MediaService.userDic)
                    {
                        if (item.Value.socket != null && item.Value.socket[appid] != null)
                        {
                            ++useronlinecount;
                        }
                    }
                    sb.Append(",\"useronlinecount\":" + useronlinecount);
                    obj = SqlHelper.ExecuteScalar("select count(tid) from [wy_talk]");
                    if (obj != null)
                    {
                        sb.Append(",\"talkcount\":" + obj.ToString() + "");
                    }
                    else
                    {
                        sb.Append(",\"talkcount\":0");
                    }
                }
                sb.Append("}");
                return sb.ToString();
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式不正确！");
            }
        }
        #endregion

        #region 获取在线频道
        public static string GetOnLineGroup(NameValueCollection qs)
        {
            StringBuilder sb = new StringBuilder();
            if (SystemVerification())
            {
                foreach (var item in MediaService.talkDic)
                {
                    if (item.Value.uidlist.Count > 0)
                    {
                        sb.Append(",{\"tid\":" + item.Key + ",\"talkname\":\"" + item.Value.talkname + "\"}");
                    }
                }
                if (sb.Length > 0)
                {
                    sb.Remove(0, 1);
                    sb.Insert(0, "");
                }
            }
            sb.Insert(0, "{\"status\":true,\"list\":[");
            sb.Append("]}");
            return sb.ToString();
        }
        #endregion

        #region 获取用户列表
        public static string GetUserList(NameValueCollection qs)
        {
            if (qs != null)
            {
                DataTable dt = new DataTable();
                if (SystemVerification())
                {
                    int minid = 0;
                    string sql = "select top 20 uid,glsn,gender,username,nickname,email,mobile from [app_users]";
                    if (qs["minid"] != null)
                    {
                        minid = Int32.Parse(qs["minid"].ToString());
                    }
                    if (qs["uid"] != null)
                    {
                        string uid = qs["uid"].Replace("'", "");
                        if (uid == "0" || uid == "")
                        {
                            return HttpService.WriteErrorJson("获取用户列表失败，参数不能为空！");
                        }
                        if (minid == 0)
                        {
                            dt = SqlHelper.ExecuteTable(sql + " where uid='" + uid + "' order by uid desc");
                        }
                        else
                        {
                            dt = SqlHelper.ExecuteTable(sql + " where uid='" + uid + "' and uid<" + minid + " order by uid desc");
                        }
                    }
                    else if (qs["key"] != null)
                    {
                        string key = qs["key"].ToString().Replace("'", "");
                        if (key == "")
                        {
                            return HttpService.WriteErrorJson("获取用户列表失败，参数不能为空！");
                        }
                        else
                        {
                            string where = "";
                            if (minid == 0)
                            {
                                where = "glsn like '%" + key + "%' or username like '%" + key + "%' or email like '%" + key + "%' or mobile like '%" + key + "%'";
                            }
                            else
                            {
                                where = "(glsn like '%" + key + "%' or username like '%" + key + "%' or email like '%" + key + "%' or mobile like '%" + key + "%') and uid<" + minid;
                            }
                            dt = SqlHelper.ExecuteTable(sql + " where " + where + " order by uid desc");
                        }
                    }
                    else
                    {
                        if (minid == 0)
                        {
                            dt = SqlHelper.ExecuteTable(sql + " order by uid desc");
                        }
                        else
                        {
                            dt = SqlHelper.ExecuteTable(sql + " where uid<" + minid + " order by uid desc");
                        }
                    }
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string uid = dt.Rows[i]["uid"].ToString();
                            string nickname = dt.Rows[i]["nickname"].ToString();
                            string glsn = dt.Rows[i]["glsn"].ToString();
                            string username = dt.Rows[i]["username"].ToString();
                            string gender = dt.Rows[i]["gender"].ToString();
                            string email = dt.Rows[i]["email"].ToString();
                            string mobile = dt.Rows[i]["mobile"].ToString();

                            sb.Append(",{\"uid\":" + uid + ",\"glsn\":\"" + glsn + "\",\"username\":\"" + username + "\",\"nickname\":\"" + nickname + "\",\"gender\":\"" + gender + "\",\"email\":\"" + email + "\",\"mobile\":\"" + mobile + "\"}");
                        }
                        if (dt.Rows.Count > 0)
                        {
                            minid = Int32.Parse(dt.Rows[dt.Rows.Count - 1]["uid"].ToString());
                            sb.Remove(0, 1);
                        }
                        sb.Insert(0, "{\"status\":true,\"minitid\":" + minid + ",\"list\":[");
                        sb.Append("]}");
                        return sb.ToString();
                    }
                    catch (Exception err)
                    {
                        MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                        return HttpService.WriteErrorJson("获取异常");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("用户认证失败！");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式不正确！");
            }
        }
        #endregion

        #region 获取在线用户列表
        public static string GetOnlineUserList(NameValueCollection qs)
        {
            if (qs != null && qs["appid"] != null)
            {
                int appid = Int32.Parse(qs["appid"].ToString());
                if (SystemVerification())
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in MediaService.userDic)
                    {
                        if (item.Value.socket != null && item.Value.socket[appid] != null)
                        {
                            sb.Append(" or uid=");
                            sb.Append(item.Key);
                        }
                    }
                    if (sb.Length > 0)
                    {
                        sb.Remove(0, 3);
                        DataTable dt = SqlHelper.ExecuteTable("select uid,glsn,gender,username,nickname,email,mobile from [app_users] where " + sb.ToString() + " order by uid desc");
                        sb.Clear();
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string uid = dt.Rows[i]["uid"].ToString();
                            string nickname = dt.Rows[i]["nickname"].ToString();
                            string glsn = dt.Rows[i]["glsn"].ToString();
                            string username = dt.Rows[i]["username"].ToString();
                            string gender = dt.Rows[i]["gender"].ToString();
                            string email = dt.Rows[i]["email"].ToString();
                            string mobile = dt.Rows[i]["mobile"].ToString();
                            sb.Append(",{\"uid\":" + uid + ",\"glsn\":\"" + glsn + "\",\"username\":\"" + username + "\",\"nickname\":\"" + nickname + "\",\"gender\":\"" + gender + "\",\"email\":\"" + email + "\",\"mobile\":\"" + mobile + "\"}");
                        }
                        if (sb.Length > 0)
                            sb.Remove(0, 1);
                    }
                    sb.Insert(0, "{\"status\":true,\"list\":[");
                    sb.Append("]}");
                    return sb.ToString();
                }
                else
                {
                    return HttpService.WriteErrorJson("用户认证失败！");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式不正确！");
            }
        }
        #endregion

        #region 获取用户所在的组
        public static string GetMyTalkList(NameValueCollection qs)
        {
            if (qs != null && qs["uid"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                if (SystemVerification())
                {
                    int minitid = 0;
                    if (qs["minitid"] != null)
                    {
                        minitid = Int32.Parse(qs["minitid"].ToString());
                    }
                    try
                    {
                        return PublicClass.GetMyTalkList(uid, minitid);
                    }
                    catch (Exception err)
                    {
                        MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                        return HttpService.WriteErrorJson("获取异常");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("用户认证失败！");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式不正确！");
            }
        }
        #endregion

        #region 获取组列表
        public static string GetTalkList(NameValueCollection qs)
        {
            if (qs != null)
            {
                if (SystemVerification())
                {
                    int minitid = 0;
                    string stalkname = "";
                    if (qs["minitid"] != null)
                    {
                        minitid = Int32.Parse(qs["minitid"].ToString());
                    }
                    if (qs["talkname"] != null && qs["talkname"].ToString() != "")
                    {
                        stalkname = qs["talkname"].ToString().Replace("'", "");
                    }
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        string sql = "";
                        if (minitid == 0)
                        {
                            sql = "select top 20 tid,talkname,auth,createuid from [wy_talk] " + (stalkname == "" ? "" : "where talkname='" + stalkname + "'") + " order by tid desc";
                        }
                        else
                        {
                            sql = "select top 20 tid,talkname,auth,createuid from [wy_talk] where  tid<" + minitid + (stalkname == "" ? "" : " and talkname='" + stalkname + "'") + " order by tid desc";
                        }
                        DataTable dt = SqlHelper.ExecuteTable(sql);
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string tid = dt.Rows[i]["tid"].ToString();
                            string talkname = dt.Rows[i]["talkname"].ToString();
                            string auth = dt.Rows[i]["auth"].ToString();
                            string createuid = dt.Rows[i]["createuid"].ToString();
                            sb.Append(",{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\",\"createuid\":" + createuid + "}");
                        }
                        if (dt.Rows.Count > 0)
                        {
                            minitid = Int32.Parse(dt.Rows[dt.Rows.Count - 1]["tid"].ToString());
                            sb.Remove(0, 1);
                        }
                        sb.Insert(0, "{\"status\":true,\"minitid\":" + minitid + ",\"list\":[");
                        sb.Append("]}");
                        return sb.ToString();
                    }
                    catch (Exception err)
                    {
                        MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                        return HttpService.WriteErrorJson("获取异常");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("用户认证失败！");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式不正确！");
            }
        }
        #endregion

        #region 获取组内的用户列表
        public static string GetTalkUserList(NameValueCollection qs)
        {
            if (qs != null && qs["tid"] != null)
            {
                int tid = Int32.Parse(qs["tid"].ToString());
                if (SystemVerification())
                {
                    int minitid = 0;
                    int u = 0;
                    if (qs["minitid"] != null)
                    {
                        minitid = Int32.Parse(qs["minitid"].ToString());
                    }
                    if (qs["uid"] != null && qs["uid"].ToString() != "")
                    {
                        u = Int32.Parse(qs["uid"].ToString());
                    }
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        string sql = "";
                        if (minitid == 0)
                        {
                            sql = "select top 20 id,uid,xuhao,duijiang,info from [wy_talkuser] where tid=" + tid + (u <= 0 ? "" : " and uid=" + u) + " order by id desc";
                        }
                        else
                        {
                            sql = "select top 20 id,uid,xuhao,duijiang,info from [wy_talkuser] where tid=" + tid + (u <= 0 ? "" : " and uid=" + u) + " and id<" + minitid + " order by id desc";
                        }
                        DataTable dt = SqlHelper.ExecuteTable(sql);
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            string uid = dt.Rows[i]["uid"].ToString();
                            string xuhao = dt.Rows[i]["xuhao"].ToString();
                            string dj = dt.Rows[i]["duijiang"].ToString();
                            string info = dt.Rows[i]["info"].ToString();
                            sb.Append(",{\"uid\":" + uid + ",\"xuhao\":" + xuhao + ",\"dj\":" + dj + ",\"info\":\"" + info + "\"}");
                        }
                        if (dt.Rows.Count > 0)
                        {
                            minitid = Int32.Parse(dt.Rows[dt.Rows.Count - 1]["id"].ToString());
                            sb.Remove(0, 1);
                        }
                        sb.Insert(0, "{\"status\":true,\"minitid\":" + minitid + ",\"list\":[");
                        sb.Append("]}");
                        return sb.ToString();
                    }
                    catch (Exception err)
                    {
                        MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                        return HttpService.WriteErrorJson("获取异常");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("用户认证失败！");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式不正确！");
            }
        }
        #endregion

        #region 用户创建组
        public static string CreateTalk(NameValueCollection qs)
        {
            if (qs != null && qs["uid"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                if (SystemVerification())
                {
                    return PublicClass.CreateTalk(uid, qs);
                }
                else
                {
                    return HttpService.WriteErrorJson("用户认证失败！");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式不正确！");
            }
        }
        #endregion

        #region 用户加入组
        public static string JoinTalk(NameValueCollection qs)
        {
            string recv = "";
            try
            {
                if (qs != null && qs["uid"] != null)
                {
                    int uid = Int32.Parse(qs["uid"].ToString());
                    if (SystemVerification())
                    {
                        string auth = qs["auth"] == null ? "" : qs["auth"].Replace("'", "");
                        string talkname = qs["talkname"] == null ? "" : qs["talkname"].Replace("'", "");
                        recv = PublicClass.JoinTalk(uid, auth, talkname);
                    }
                    else
                    {
                        recv = HttpService.WriteErrorJson("用户认证失败!");
                    }
                }
                else
                {
                    recv = HttpService.WriteErrorJson("请求格式错误!");
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                recv = HttpService.WriteErrorJson("加入组异常，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 设置用户默认组
        public static string SetDjTalk(NameValueCollection qs)
        {
            string recv = "{\"status\":true}";
            if (qs != null && qs["uid"] != null && qs["tid"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                int tid = Int32.Parse(qs["tid"].ToString());
                if (SystemVerification())
                {
                    PublicClass.SetDefaultDuiJiang(tid, uid);
                }
                else
                {
                    recv = HttpService.WriteErrorJson("用户认证失败!");
                }
            }
            else
            {
                recv = HttpService.WriteErrorJson("请求格式错误!");
            }
            return recv;
        }
        #endregion

        #region 用户退出组
        public static string ExitTalk(NameValueCollection qs)
        {
            string recv = "{\"status\":true}";
            if (qs != null && qs["uid"] != null && qs["tid"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                int tid = Int32.Parse(qs["tid"].ToString());
                if (SystemVerification())
                {
                    recv = PublicClass.ExitTalk(tid, uid);
                }
                else
                {
                    recv = HttpService.WriteErrorJson("用户认证失败!");
                }
            }
            else
            {
                recv = HttpService.WriteErrorJson("请求格式错误!");
            }
            return recv;
        }
        #endregion

        #region 创建者解散组
        public static string DeleteTalk(NameValueCollection qs)
        {
            string recv = "{\"status\":true}";
            if (qs != null && qs["uid"] != null && qs["tid"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                int tid = Int32.Parse(qs["tid"].ToString());
                if (SystemVerification())
                {
                    recv = PublicClass.DeleteTalk(tid, uid);
                }
                else
                {
                    recv = HttpService.WriteErrorJson("用户认证失败!");
                }
            }
            else
            {
                recv = HttpService.WriteErrorJson("请求格式错误!");
            }
            return recv;
        }
        #endregion

        #region 设置FM频率
        public static string SetFM(NameValueCollection qs)
        {
            if (qs != null && qs["appid"] != null && qs["uid"] != null && qs["fm"] != null)
            {
                int appid = Int32.Parse(qs["appid"].ToString());
                int uid = Int32.Parse(qs["uid"].ToString());
                int fm = Int32.Parse(qs["fm"].ToString());
                if (SystemVerification())
                {
                    if (SqlHelper.ExecuteNonQuery("update [app_users] set fm=" + fm + " where uid=" + uid) > 0)
                    {
                        UserObject uo = null;
                        if (MediaService.userDic.TryGetValue(uid, out uo))
                        {
                            if (uo.socket != null && uo.socket[appid] != null)
                            {
                                try
                                {
                                    uo.socket[appid].Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                }
                                catch { }
                                uo.socket[appid].Close();
                                uo.socket[appid] = null;
                            }
                        }
                        return "{\"status\":true}";
                    }
                    else
                    {
                        return HttpService.WriteErrorJson("用户不存在!");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("用户认证失败!");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式错误!");
            }
        }
        #endregion

        #region 设置Golo调试模式
        public static string SetGoloDebug(NameValueCollection qs)
        {
            if (qs != null && qs["appid"] != null && qs["uid"] != null && qs["debug"] != null)
            {
                int appid = Int32.Parse(qs["appid"].ToString());
                int uid = Int32.Parse(qs["uid"].ToString());
                int debug = Int32.Parse(qs["debug"].ToString());
                if (SystemVerification())
                {
                    if (SqlHelper.ExecuteNonQuery("update [app_users] set debug=" + debug + " where uid=" + uid) > 0)
                    {
                        UserObject uo = null;
                        if (MediaService.userDic.TryGetValue(uid, out uo))
                        {
                            if (uo.socket != null && uo.socket[appid] != null)
                            {
                                try
                                {
                                    uo.socket[appid].Shutdown(System.Net.Sockets.SocketShutdown.Both);
                                }
                                catch { }
                                uo.socket[appid].Close();
                                uo.socket[appid] = null;
                            }
                        }
                        return "{\"status\":true}";
                    }
                    else
                    {
                        return HttpService.WriteErrorJson("用户不存在!");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("用户认证失败!");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式错误!");
            }
        }
        #endregion

        #region 断开用户连接
        public static string LineOff(NameValueCollection qs)
        {
            if (qs != null && qs["appid"] != null && qs["uid"] != null)
            {
                int appid = Int32.Parse(qs["appid"].ToString());
                int uid = Int32.Parse(qs["uid"].ToString());
                if (SystemVerification())
                {
                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        if (uo.socket != null && uo.socket[appid] != null)
                        {
                            try
                            {
                                uo.socket[appid].Shutdown(System.Net.Sockets.SocketShutdown.Both);
                            }
                            catch { }
                            uo.socket[appid].Close();
                            uo.socket[appid] = null;
                            return "{\"status\":true}";
                        }
                        else
                        {
                            return HttpService.WriteErrorJson("用户不在线!");
                        }
                    }
                    else
                    {
                        return HttpService.WriteErrorJson("用户不在线!");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("用户认证失败!");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式错误!");
            }
        }
        #endregion

        #region 用户验证
        public static string UserVerification(NameValueCollection qs)
        {
            if (SystemVerification())
            {
                if (qs != null && qs["appid"] != null && qs["uid"] != null && qs["token"] != null)
                {
                    int appid = Int32.Parse(qs["appid"].ToString());
                    int uid = Int32.Parse(qs["uid"].ToString());
                    string token = qs["token"].ToString();
                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        if (uo.token[appid] != null && uo.token[appid] != "")
                        {
                            if (uo.token[appid] == token)
                                return "{\"status\":true}";
                            else
                                return HttpService.WriteErrorJson("token验证失败!");
                        }
                        else
                        {
                            return HttpService.WriteErrorJson("用户未登录!");
                        }
                    }
                    else
                    {
                        return HttpService.WriteErrorJson("用户未登录!");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("参数不正确!");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("非法请求!");
            }
        }
        #endregion

        #region 获取用户地理位置--SYS
        public static string GetUserLocation(NameValueCollection qs)
        {
            StringBuilder sb = new StringBuilder();

            if (qs != null && qs["uid"] != null && qs["appid"] != null)
            {
                if (!SystemVerification())
                {
                    return HttpService.WriteErrorJson("用户认证失败！");

                }
                string[] uid = qs["uid"].ToString().Trim().Split(',');
                int appid = Int32.Parse(qs["appid"].ToString());
                return GetUserListLocation(uid, appid);
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式错误！");
            }
        }
        #endregion

        #region 获取车队所有用户地理位置--SYS
        public static string GetTalkUserLocation(NameValueCollection qs)
        {
            StringBuilder sb = new StringBuilder();
            if (qs != null && qs["talkname"] != null && qs["appid"] != null)
            {
                if (!SystemVerification())
                {
                    return HttpService.WriteErrorJson("用户认证失败！");
                }
                object obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname = '" + qs["talkname"].ToString().Replace("'", "") + "'");
                if (obj != null)
                {
                    int tid = Int32.Parse(obj.ToString());
                    DataTable dt = SqlHelper.ExecuteTable("select uid from[wy_talkuser] where tid=" + tid);
                    if (dt.Rows.Count > 0)
                    {
                        string[] uid = new string[dt.Rows.Count];
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            uid[i] = dt.Rows[i][0].ToString();
                        }
                        int appid = Int32.Parse(qs["appid"].ToString());
                        return GetUserListLocation(uid, appid);
                    }
                    else
                    {
                        return HttpService.WriteErrorJson("频道没有用户！");
                    }
                }
                else
                {
                    return HttpService.WriteErrorJson("频道不存在，请确认！");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式错误！");
            }
        }
        #endregion

        #region 修改用户信息--SYS
        public static string ModiUserMessage(NameValueCollection qs)
        {
            string recv = "";
            try
            {
                if (!SystemVerification())
                {
                    recv = HttpService.WriteErrorJson("用户认证失败！");
                    return recv;
                }
                //更新app_users表
                StringBuilder sql = new StringBuilder();
                string uid = qs["uid"];
                if (qs["password"] != null && qs["password"].Trim() != "")
                {
                    string password = qs["password"].ToString().ToLower().Replace("'", "").ToLower();
                    if (password.Length == 32)
                    {
                        sql.Append(",password='" + password + "'");
                    }
                }
                if (qs["nickname"] != null)
                {
                    string nickname = qs["nickname"].ToString().Replace("'", "");
                    if (IsValiNumCnEn(nickname) == false)
                        return HttpService.WriteErrorJson("昵称不合法，只能由数字和英文组成！");
                    sql.Append(",nickname='" + qs["nickname"].ToString().Replace("'", "") + "'");
                    sql.Append(",moditime='" + CommBusiness.GetTimeStamp() + "'");
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
                    if (mobile == "" || mobile.Length == 11)
                    {
                        sql.Append(",mobile='" + mobile + "'");
                    }
                    else
                    {
                        return HttpService.WriteErrorJson("电话号码格式有误");
                    }
                }

                if (qs["roles"] != null && qs["roles"].ToString() != "")
                {
                    sql.Append(",roles='" + Int32.Parse(qs["roles"].ToString()) + "'");
                }
                if (sql.Length > 0)
                {
                    if (sql.ToString().StartsWith(","))
                        sql = sql.Remove(0, 1);
                    SqlHelper.ExecuteNonQuery("update [app_users] set " + sql.ToString() + " where uid='" + uid + "'");
                }
                //更新app_userscar表
                if (qs["sn"] != null && qs["sn"].ToString() != "")
                {
                    StringBuilder sqlCar = new StringBuilder();
                    StringBuilder sqlAddCarField = new StringBuilder();
                    StringBuilder sqlAddCarValue = new StringBuilder();
                    sqlAddCarField.Append(",uid");
                    sqlAddCarValue.Append(",'" + uid + "'");
                    if (qs["carid"] != null)
                    {
                        sqlCar.Append(",car_id='" + Int32.Parse(qs["carid"].ToString()) + "'");
                        sqlAddCarField.Append(",car_id");
                        sqlAddCarValue.Append(",'" + qs["carid"].ToString() + "'");
                    }
                    if (qs["carname"] != null)
                    {
                        sqlCar.Append(",car_name='" + qs["carname"].ToString() + "'");
                        sqlAddCarField.Append(",car_name");
                        sqlAddCarValue.Append(",'" + qs["carname"].ToString() + "'");
                    }
                    if (qs["carplate"] != null)
                    {
                        sqlCar.Append(",car_plate='" + qs["carplate"].ToString() + "'");
                        sqlAddCarField.Append(",car_plate");
                        sqlAddCarValue.Append(",'" + qs["carplate"].ToString() + "'");
                    }
                    if (qs["outputvolume"] != null)
                    {
                        sqlCar.Append(",output_volume='" + qs["outputvolume"].ToString() + "'");
                        sqlAddCarField.Append(",output_volume");
                        sqlAddCarValue.Append(",'" + qs["outputvolume"].ToString() + "'");
                    }
                    if (qs["carcode"] != null)
                    {
                        sqlCar.Append(",car_code='" + qs["carcode"].ToString() + "'");
                        sqlAddCarField.Append(",car_code");
                        sqlAddCarValue.Append(",'" + qs["carcode"].ToString() + "'");
                    }
                    if (qs["regzone"] != null)
                    {
                        sqlCar.Append(",reg_zone='" + qs["regzone"].ToString() + "'");
                        sqlAddCarField.Append(",reg_zone");
                        sqlAddCarValue.Append(",'" + qs["regzone"].ToString() + "'");
                    }

                    if (qs["facever"] != null)
                    {
                        sqlCar.Append(",face_ver='" + qs["facever"].ToString() + "'");
                        sqlAddCarField.Append(",face_ver");
                        sqlAddCarValue.Append(",'" + qs["facever"].ToString() + "'");
                    }
                    if (qs["userlon"] != null)
                    {
                        sqlCar.Append(",user_lon='" + qs["userlon"].ToString() + "'");
                        sqlAddCarField.Append(",user_lon");
                        sqlAddCarValue.Append(",'" + qs["userlon"].ToString() + "'");
                    }
                    if (qs["userlat"] != null)
                    {
                        sqlCar.Append(",user_lat='" + qs["userlat"].ToString() + "'");
                        sqlAddCarField.Append(",user_lat");
                        sqlAddCarValue.Append(",'" + qs["userlat"].ToString() + "'");
                    }
                    if (qs["carlon"] != null)
                    {
                        sqlCar.Append(",car_lon='" + qs["carlon"].ToString() + "'");
                        sqlAddCarField.Append(",car_lon");
                        sqlAddCarValue.Append(",'" + qs["carlon"].ToString() + "'");
                    }
                    if (qs["carlat"] != null)
                    {
                        sqlCar.Append(",car_lat='" + qs["carlat"].ToString() + "'");
                        sqlAddCarField.Append(",car_lat");
                        sqlAddCarValue.Append(",'" + qs["carlat"].ToString() + "'");
                    }

                    if (sqlCar.Length > 0 && sqlAddCarField.Length > 0 && sqlAddCarValue.Length > 0)
                    {
                        if (sqlCar.ToString().StartsWith(","))
                            sqlCar = sqlCar.Remove(0, 1);

                        if (sqlAddCarField.ToString().StartsWith(","))
                            sqlAddCarField = sqlAddCarField.Remove(0, 1);

                        if (sqlAddCarValue.ToString().StartsWith(","))
                            sqlAddCarValue = sqlAddCarValue.Remove(0, 1);
                        string strSql = @"if exists(select * from app_userscar where uid=" + uid + ") update [app_userscar] set " + sqlCar.ToString() + " where uid=" + uid
                            + " else INSERT INTO [app_userscar](" + sqlAddCarField + ") VALUES(" + sqlAddCarValue + ")";

                        SqlHelper.ExecuteNonQuery(strSql);
                    }
                    //设置激活状态
                    if (qs["caractive"] != null && qs["caractive"] != "" && qs["configid"] != null)
                    {
                        if (qs["caractive"].ToString() == "1")
                        {
                            SqlHelper.ExecuteNonQuery(" update [app_userscar] set car_active=1,config_id=" + qs["configid"].ToString() + " where uid=" + uid);
                        }
                    }
                }
                if (qs["uname"] != null && qs["uname"].ToString() != "")
                {
                    string username = qs["uname"].ToString().Replace("'", "");
                    if (username.Length < 20 && username.Length > 6)
                    {
                        if (IsValiNumCnEn(username.Replace("_", "")) == false) return HttpService.WriteErrorJson("用户名不合法，只能由数字和英文组成！");
                        object obj = SqlHelper.ExecuteScalar("select uid from[app_users] where username='" + username + "' and uid!=" + uid);
                        if (obj == null)
                        {
                            sql.Append(",username='" + username + "'");
                        }
                        else
                        {
                            return HttpService.WriteErrorJson("该用户名已被占用，换一个试试");
                        }
                    }
                    else
                    {
                        return HttpService.WriteErrorJson("用户名必须6到20字符");
                    }
                }
                recv = "{\"status\":true}";
            }
            catch
            {
                recv = HttpService.WriteErrorJson("操作失败，请联系管理员！");
            }
            return recv;
        }
        #endregion

        #region 修改会话组或群的信息  --SYS
        public static string ModiTalkMessage(NameValueCollection qs)
        {
            string recv = "";

            if (qs["tid"] != null)
            {
                if (!SystemVerification())
                {
                    recv = HttpService.WriteErrorJson("用户认证失败！");
                    return recv;
                }
                string tid = qs["tid"].ToString().Replace("'", "");
                DataTable dt = SqlHelper.ExecuteTable("select talkname from [wy_talk] where tid='" + tid + "'");
                if (dt.Rows.Count > 0)
                {
                    StringBuilder sql = new StringBuilder();
                    sql.Append(",moditime='" + CommBusiness.GetTimeStamp() + "'");
                    if (qs["info"] != null)
                    {
                        sql.Append(",info='" + qs["info"].Replace("'", "").Trim() + "'");
                    }
                    if (qs["auth"] != null)
                    {
                        sql.Append(",auth='" + qs["auth"].Replace("'", "").Trim() + "'");
                    }
                    if (qs["talknotice"] != null)
                    {
                        sql.Append(",talknotice='" + qs["talknotice"].Replace("'", "") + "'");
                    }
                    if (sql.Length > 0)
                    {
                        sql.Remove(0, 1);
                        sql.Insert(0, "update [wy_talk] set ");
                        sql.Append(" where tid='" + tid + "'");
                        int re = SqlHelper.ExecuteNonQuery(sql.ToString());
                        if (re > 0)
                            recv = "{\"status\":true}";
                        else
                            recv = HttpService.WriteErrorJson("更新失败，请稍后再试！");
                    }
                }
            }
            else
            {
                recv = HttpService.WriteErrorJson("请求格式错误!");
            }
            return recv;
        }
        #endregion

        #region 获取用户对讲状态 --SYS
        public static string GetTalkUserDuijiang(NameValueCollection qs)
        {
            string recv = "";
            try
            {
                if (qs["tid"] != null && qs["uid"] != null)
                {


                    if (!SystemVerification())
                    {
                        recv = HttpService.WriteErrorJson("用户认证失败！");
                        return recv;
                    }
                    string tid = qs["tid"].ToString().Replace("'", "");
                    string uid = qs["uid"].ToString().Replace("'", "");
                    string sqlStr = @"  select duijiang from [wy_talkuser] where tid='" + tid + "' and uid='" + uid + "' ";
                    string duijiang = SqlHelper.ExecuteScalar(sqlStr).ToString();
                    recv = "{\"status\":true,\"duijiang\":" + duijiang + "}";
                }
                else
                {
                    recv = HttpService.WriteErrorJson("请求格式错误!");
                }
            }
            catch
            {
                recv = HttpService.WriteErrorJson("操作失败,请联系管理员！");
            }
            return recv;
        }
        #endregion

        #region 获取查询工单列表--SYS
        public static string GetSerives(NameValueCollection qs)
        {
            if (qs["pageIndex"] != null && qs["pageSize"] != null)
            {
                if (!SystemVerification())
                {
                    return HttpService.WriteErrorJson("用户认证失败！");
                }
                string whereStr = " where 1=1 ";
                int pageIndex = Convert.ToInt32(qs["pageIndex"]);
                int pageSize = Convert.ToInt32(qs["pageSize"]);
                if (qs["addusername"] != null && qs["addusername"].Trim() != "")
                {
                    string addusername = qs["addusername"].ToString().Replace("'", "");
                    whereStr += " and (b.username like '%" + addusername + "%' or b.nickname like '%" + addusername + "%' or b.mobile like '%" + addusername + "%') ";
                }
                if (qs["solvename"] != null && qs["solvename"] != "")
                {
                    string solvename = qs["solvename"].ToString().Replace("'", "");
                    whereStr += " and a.solvename like   '%" + solvename + "%'";
                }
                if (qs["state"] != null && qs["state"] != "")
                {
                    string state = qs["state"].ToString().Replace("'", "");
                    whereStr += " and a.state = '" + state + "'";
                }
                if (qs["solvestate"] != null && qs["solvestate"] != "")
                {
                    string solvestate = qs["solvestate"].ToString().Replace("'", "");
                    whereStr += " and a.solvestate = '" + solvestate + "'";
                }
                if (qs["serivetype"] != null && qs["serivetype"] != "")
                {
                    string serivetype = qs["serivetype"].ToString().Replace("'", "");
                    whereStr += " and a.serivetype = '" + serivetype + "'";
                }
                string sqlStr = "";
                string sqlGetCount = @"SELECT  count(*)  FROM [app_userserive] a left join app_users b on a.uid=b.uid " + whereStr;
                int recordCount = Convert.ToInt32(SqlHelper.ExecuteScalar(sqlGetCount));
                StringBuilder sb = new StringBuilder();
                sqlStr = @"WITH OrderInfo AS
	                            (SELECT ROW_NUMBER() OVER(ORDER BY a.id desc) AS number,
                                                   a.id, a.uid,a.state,a.addtime,b.username,b.nickname, [solveremark] ,[solvename],solvetype,soundurl ,case [solvestate] when 0 then '未解决' when 1 then '已解决' else ' ' end as solvestate,[solvetime] FROM [app_userserive] a
                                            left join app_users b on a.uid=b.uid " + whereStr + " ) "
                            + "select * from OrderInfo WHERE number BETWEEN " + ((pageIndex) * pageSize + 1) + "  AND " + (pageIndex + 1) * pageSize;


                DataTable dt = SqlHelper.ExecuteTable(sqlStr);
                if (dt != null && dt.Rows.Count > 0)
                {
                    sb.Append("{\"status\":true,\"count\":" + recordCount + ",\"serivelist\":[");

                    foreach (DataRow item in dt.Rows)
                    {
                        sb.Append("{");
                        sb.Append("\"id\":\"" + item["id"].ToString() + "\"");
                        sb.Append(",\"uid\":\"" + item["uid"].ToString() + "\"");
                        sb.Append(",\"state\":\"" + item["state"].ToString() + "\"");
                        sb.Append(",\"addtime\":\"" + item["addtime"].ToString() + "\"");
                        sb.Append(",\"username\":\"" + item["username"].ToString() + "\"");
                        sb.Append(",\"nickname\":\"" + item["nickname"].ToString() + "\"");
                        sb.Append(",\"solveremark\":\"" + item["solveremark"].ToString() + "\"");
                        sb.Append(",\"solvename\":\"" + item["solvename"].ToString() + "\"");
                        sb.Append(",\"solvestate\":\"" + item["solvestate"].ToString() + "\"");
                        sb.Append(",\"solvetype\":\"" + item["solvetype"].ToString() + "\"");
                        sb.Append(",\"solvetime\":\"" + item["solvetime"].ToString() + "\"");
                        sb.Append(",\"soundurl\":\"" + item["soundurl"].ToString() + "\"");
                        sb.Append("},");
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("]");
                    sb.Append("}");
                    return sb.ToString();
                }
                else
                    return HttpService.WriteErrorJson("未获取到数据!");
            }
            else
            {
                return HttpService.WriteErrorJson("请求格式错误!");
            }
        }
        #endregion

        #region 更新在线客服 --SYS
        public static string UpdateOnLineKFUser(NameValueCollection qs)
        {
            string recv = "{\"status\":true}";
            if (qs["uid"] != null && qs["username"] != null && qs["type"] != null)
            {
                if (!SystemVerification())
                {
                    recv = HttpService.WriteErrorJson("用户认证失败！");
                    return recv;
                }
                int uid = Int32.Parse(qs["uid"].ToString());

                if (qs["type"].ToString() == "1")//添加
                {
                    KFUserObject kfuo = new KFUserObject();
                    kfuo.username = qs["username"].ToString();
                    kfuo.onwork = 0;
                    if (qs["version"] != null)
                        kfuo.version = qs["version"].ToString();
                    HttpKfBusiness.KFUserDic.AddOrUpdate(uid, kfuo, (k, v) => kfuo);
                }
                else if (qs["type"].ToString() == "0")//删除
                {
                    KFUserObject reKFuo = null;
                    HttpKfBusiness.KFUserDic.TryRemove(uid, out reKFuo);
                }
            }
            else
            {
                recv = HttpService.WriteErrorJson("请求格式错误!");
            }
            return recv;
        }
        #endregion

        #region 获取在线客服列表--SYS
        public static string GetOnlineKFList(NameValueCollection qs)
        {
            string recv = "";
            try
            {
                if (!SystemVerification())
                {
                    recv = HttpService.WriteErrorJson("用户认证失败！");
                    return recv;
                }
                int recordCount = 0;
                recv = "{\"status\":true,\"count\":" + recordCount + ",\"list\":[";
                if (HttpKfBusiness.KFUserDic != null && HttpKfBusiness.KFUserDic.Count > 0)
                {
                    foreach (var obj in HttpKfBusiness.KFUserDic)
                    {
                        KFUserObject kf = (KFUserObject)obj.Value;
                        if (DateTime.Now.AddMinutes(-1) < kf.time)
                        {
                            UserObject uo = null;
                            if (MediaService.userDic.TryGetValue(Int32.Parse(obj.Key.ToString()), out uo))
                            {
                                if (uo.socket[7] != null)
                                {
                                    recordCount++;
                                    recv += "{\"uid\":" + obj.Key.ToString()
                                     + ",\"username\":\"" + kf.username
                                     + "\",\"version\":\"" + kf.version
                                     + "\",\"nickname\":\"" + uo.nickname
                                     + "\"},";
                                }
                            }
                        }
                    }
                }

                if (recv.Contains("{\"uid\""))
                    recv = recv.Substring(0, recv.Length - 1);
                recv += "]}";
            }
            catch
            {
                recv = HttpService.WriteErrorJson("操作失败,请联系管理员！");
            }
            return recv;
        }
        #endregion

        #region 根据用户名（或sn）列表获取基本信息
        public static string GetUserNameListMessage(NameValueCollection qs)
        {
            string recv = "";
            try
            {
                if (!SystemVerification())
                {
                    recv = HttpService.WriteErrorJson("用户认证失败！");
                    return recv;
                }
                if (qs["sn"] != null && qs["sn"].ToString() != "")
                {
                    recv = "{\"status\":true,\"list\":[";
                    string[] usersn = qs["sn"].ToString().Split(',');
                    for (int i = 0; i < usersn.Length; i++)
                    {
                        if (usersn[i] == null || usersn[i] == "")
                            continue;
                        DataTable dt = SqlHelper.ExecuteTable("  select glsn,uid from [app_users] where glsn='" + usersn[i] + "'");
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            if (dt.Rows[0]["uid"] != null)
                                recv += "{\"uid\":" + dt.Rows[0]["uid"].ToString() + ",\"glsn\":\"" + usersn[i] + "\"},";
                        }
                    }
                    if (recv.EndsWith(","))
                        recv = recv.Substring(0, recv.Length - 1);
                    recv += "]}";
                }
                else if (qs["usernames"] != null && qs["usernames"].ToString() != "")
                {
                    recv = "{\"status\":true,\"userlist\":[";
                    string[] usersn = qs["usernames"].ToString().Split(',');
                    for (int i = 0; i < usersn.Length; i++)
                    {
                        if (usersn[i] == null || usersn[i] == "")
                            continue;
                        string key = "username";
                        if (usersn[i].IndexOf('@') > 0)
                        {
                            key = "email";
                        }
                        else if (usersn[i][0] > 47 && usersn[i][0] < 58)
                        {
                            key = "mobile";
                        }
                        DataTable dt = SqlHelper.ExecuteTable("  select glsn,uid from [app_users]   where " + key + "='" + usersn[i] + "'");
                        if (dt != null && dt.Rows.Count > 0)
                        {
                            if (dt.Rows[0]["uid"] != null)
                                recv += "{\"uid\":" + dt.Rows[0]["uid"].ToString() + ",\"glsn\":\"" + usersn[i] + "\"},";
                        }
                    }
                    if (recv.EndsWith(","))
                        recv = recv.Substring(0, recv.Length - 1);
                    recv += "]}";
                }
                else
                {
                    recv = HttpService.WriteErrorJson("请求参数有误！");
                    return recv;
                }
            }
            catch
            {
                recv = HttpService.WriteErrorJson("查询失败,请联系管理员！");
            }
            return recv;
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

        #region 用户Ip验证
        public static bool SystemVerification()
        {
            bool state = true;
            return state;
        }
        #endregion

        #region 应用推送信息到在线用户
        public static string PushMessageToOnlineUser(NameValueCollection qs)
        {
            string recv = "{\"status\":false}";
            if (qs != null && qs["uid"] != null && qs["online"] != null && qs["offline"] != null && qs["appid"] != null && qs["type"] != null)
            {
                int appid = Int32.Parse(qs["appid"].ToString());
                string[] user = qs["uid"].ToString().Split(',');
                string online = qs["online"].ToString();
                string offline = qs["offline"].ToString().Trim();
                string type = qs["type"].ToString();
                if (online != "")
                {
                    online = "{\"status\":true,\"type\":" + type + ",\"message\":" + online + "}";

                    if (user.Length == 1 && user[0] == "0")
                    {
                        MediaService.WriteLog("推送信息到所有用户 ：online=" + online, MediaService.wirtelog);
                        recv = PublicClass.SendToAllOnlineUser(null, online, offline, 99, 0, CommType.pushMessageToUser, appid);
                    }
                    else
                    {
                        List<int> uidlist = new List<int>();
                        for (int i = 0; i < user.Length; i++)
                        {
                            if (user[i] != "")
                            {
                                int uid = Int32.Parse(user[i]);
                                uidlist.Add(uid);
                            }
                        }
                        MediaService.WriteLog("推送信息到用户列表 ：online=" + online, MediaService.wirtelog);
                        recv = PublicClass.SendToOnlineUserList(null, online, offline, uidlist, 99, 0, CommType.pushMessageToUser, appid);
                    }
                }
            }
            MediaService.WriteLog("推送状态：" + recv, MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 获取列表中的用户最新位置
        public static string GetUserListLocation(string[] uid, int appid)
        {
            StringBuilder sb = new StringBuilder();
            MongoCollection mongoCollection = MediaService.mongoDataBase.GetCollection("gps_" + DateTime.Now.ToString("yyyyMMdd") + (DateTime.Now.Hour / 2 * 2).ToString("00"));//选择集合
            SortByDocument sd = new SortByDocument { { "t", -1 } };
            FieldsDocument fd = new FieldsDocument { { "_id", 0 } };
            for (int i = 0; i < uid.Length; i++)
            {
                if (uid[i] != "")
                {
                    bool state = false;
                    int u = Int32.Parse(uid[i]);
                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(u, out uo))
                    {
                        if (uo.socket[appid] != null)
                        {
                            sb.Append(",{\"uid\":" + uid[i] + ",\"line\":1,\"lo\":" + uo.lo[appid] + ",\"la\":" + uo.la[appid] + ",\"vi\":" + uo.vi[appid] + ",\"di\":" + uo.di[appid] + "}");
                            state = true;
                        }
                    }
                    if (state == false)
                    {
                        QueryDocument query = new QueryDocument { { "uid", u } };
                        MongoCursor<GPSinfo> gpsCursor = mongoCollection.FindAs<GPSinfo>(query).SetSortOrder(sd).SetLimit(1).SetFields(fd);
                        foreach (GPSinfo gps in gpsCursor)
                        {
                            sb.Append(",{\"uid\":" + uid[i] + ",\"line\":0,\"lo\":" + gps.lo + ",\"la\":" + gps.la + ",\"vi\":" + gps.vi + ",\"di\":" + gps.di + "}");
                            state = true;
                            break;
                        }
                    }
                    if (state == false)
                    {
                        sb.Append(",{\"uid\":" + uid[i] + ",\"line\":0}");
                    }
                }
            }
            if (sb.Length > 0)
            {
                sb.Remove(0, 1);
                sb.Insert(0, "{\"status\":true,\"list\":[");
                sb.Append("]}");
            }
            else
            {
                return HttpService.WriteErrorJson("没有查找到用户位置信息！");
            }
            return sb.ToString();
        }
        #endregion

        #region 交换用户sn
        public static string ExchangeUserSn(NameValueCollection qs)
        {
            string recv = "";
            try
            {
                if (qs != null && qs["uid1"] != null && qs["uid2"] != null)
                {
                    if (!SystemVerification())
                    {
                        recv = HttpService.WriteErrorJson("用户认证失败！");
                        return recv;
                    }

                    int uid1 = Int32.Parse(qs["uid1"].ToString());
                    int uid2 = Int32.Parse(qs["uid2"].ToString());
                    string glsn1 = "";
                    string glsn2 = "";
                    string username1 = "";
                    string username2 = "";

                    DataTable dt1 = SqlHelper.ExecuteTable("select glsn,username from [app_users] where uid =" + uid1);
                    if (dt1.Rows.Count > 0)
                    {
                        glsn1 = dt1.Rows[0]["glsn"].ToString();
                        username1 = dt1.Rows[0]["username"].ToString();
                        SqlHelper.ExecuteNonQuery("update [app_users] set glsn = null,username = null where uid = " + uid1);

                        DataTable dt2 = SqlHelper.ExecuteTable("select glsn,username from [app_users] where uid = " + uid2);
                        if (dt2.Rows.Count > 0)
                        {
                            glsn2 = dt2.Rows[0]["glsn"].ToString();
                            username2 = dt2.Rows[0]["username"].ToString();
                            SqlHelper.ExecuteNonQuery("update [app_users] set glsn = '" + glsn1 + "',username = '" + username1 + "' where uid= " + uid2);
                            SqlHelper.ExecuteNonQuery("update [app_users] set glsn = '" + glsn2 + "',username = '" + username2 + "' where uid= " + uid1);

                            recv = "{\"status\":true}";
                        }
                        else
                        {
                            recv = HttpService.WriteErrorJson("uid为" + uid2 + "的用户不存在");
                        }

                    }
                    else
                    {
                        recv = HttpService.WriteErrorJson("uid为" + uid1 + "的用户不存在");
                    }
                }
                else
                {
                    recv = HttpService.WriteErrorJson("请求格式错误!");
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                recv = HttpService.WriteErrorJson("更换glsn异常!");
            }
            return recv;
        }
        #endregion

        #region 修改用户sn
        public static string ModiUserSn(NameValueCollection qs)
        {
            string recv = "";
            try
            {
                if (qs != null && qs["uid"] != null && qs["glsn"] != null)
                {
                    if (!SystemVerification())
                    {
                        recv = HttpService.WriteErrorJson("用户认证失败！");
                        return recv;
                    }

                    int uid = Int32.Parse(qs["uid"].ToString());
                    string glsn = qs["glsn"].ToString().Replace("'", "");

                    DataTable dt = SqlHelper.ExecuteTable("select uid from [app_users] where glsn = '" + glsn + "'");

                    if (dt.Rows.Count > 0)
                    {
                        recv = HttpService.WriteErrorJson("该sn已被占用");
                    }
                    else
                    {
                        if (SqlHelper.ExecuteNonQuery("update [app_users] set glsn= " + glsn + " where uid=" + uid) > 0)
                        {
                            recv = "{\"status\":true}";
                        }
                        else
                        {
                            recv = HttpService.WriteErrorJson("用户不存在");
                        }
                    }
                }
                else
                {
                    recv = HttpService.WriteErrorJson("请求格式错误!");
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                recv = HttpService.WriteErrorJson("修改用户sn异常!");
            }
            return recv;
        }
        #endregion

        #region 获取电台列表
        public static string GetRadioList()
        {
            string recv = "";
            try
            {
                if (!SystemVerification())
                {
                    recv = HttpService.WriteErrorJson("用户认证失败！");
                    return recv;
                }

                StringBuilder sb = new StringBuilder();
                foreach (var item in MediaService.radioDic.ToArray())
                {
                    int rid = item.Key;
                    RadioObject ro = item.Value;
                    sb.Append("{\"rid\":" + rid
                        + ",\"channelname\":\"" + ro.channelname
                        + "\",\"areaid\":" + ro.areaid
                        + ",\"sendtype\":" + ro.sendtype
                        + ",\"senduid\":\"" + ro.senduid
                        + "\",\"audiourl\":\"" + ro.audiourl
                        + "\",\"uploadurl\":\"" + ro.uploadurl
                        + "\",\"flashimageurl\":\"" + ro.flashimageurl
                        + "\"},");
                }

                if (sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                sb.Insert(0, "{\"status\":true,\"list\":[");
                sb.Append("]}");
                recv = sb.ToString();
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                recv = HttpService.WriteErrorJson("获取电台列表异常!");
            }
            return recv;
        }
        #endregion

        #region 修改电台信息
        public static string ModiRadioInfo(NameValueCollection qs)
        {
            string recv = "";
            try
            {
                if (!SystemVerification())
                {
                    recv = HttpService.WriteErrorJson("用户认证失败！");
                    return recv;
                }

                bool ischange = false;
                int rid = 0;
                if (qs["rid"] != null)
                {
                    rid = Int32.Parse(qs["rid"].ToString());
                }

                if (rid == 0)
                {
                    StringBuilder sqlAddRadioField = new StringBuilder();
                    StringBuilder sqlAddRadioValue = new StringBuilder();
                    if (qs["channelname"] != null)
                    {
                        sqlAddRadioField.Append(",channelname");
                        sqlAddRadioValue.Append(",'" + qs["channelname"].ToString() + "'");
                    }

                    if (qs["areaid"] != null)
                    {
                        sqlAddRadioField.Append(",areaid");
                        sqlAddRadioValue.Append(",'" + qs["areaid"].ToString() + "'");
                    }

                    if (qs["sendtype"] != null)
                    {
                        sqlAddRadioField.Append(",sendtype");
                        sqlAddRadioValue.Append(",'" + qs["sendtype"].ToString() + "'");
                    }

                    if (qs["senduid"] != null)
                    {
                        sqlAddRadioField.Append(",senduid");
                        sqlAddRadioValue.Append(",'" + qs["senduid"].ToString() + "'");
                    }

                    if (qs["audiourl"] != null)
                    {
                        sqlAddRadioField.Append(",audiourl");
                        sqlAddRadioValue.Append(",'" + qs["audiourl"].ToString() + "'");
                    }

                    if (qs["uploadurl"] != null)
                    {
                        sqlAddRadioField.Append(",uploadurl");
                        sqlAddRadioValue.Append(",'" + qs["uploadurl"].ToString() + "'");
                    }

                    string flashImageUrl = qs["flashimageurl"];
                    if (flashImageUrl != null)
                    {
                        sqlAddRadioField.Append(",flashimageurl");
                        sqlAddRadioValue.Append(",'" + flashImageUrl + "'");
                    }

                    if (sqlAddRadioField.Length > 0 && sqlAddRadioValue.Length > 0)
                    {
                        if (sqlAddRadioField.ToString().StartsWith(","))
                            sqlAddRadioField = sqlAddRadioField.Remove(0, 1);

                        if (sqlAddRadioValue.ToString().StartsWith(","))
                            sqlAddRadioValue = sqlAddRadioValue.Remove(0, 1);
                        string strSql = @"INSERT INTO [wy_radio](" + sqlAddRadioField.ToString() + ") VALUES(" + sqlAddRadioValue.ToString() + ")";
                        SqlHelper.ExecuteNonQuery(strSql);
                        ischange = true;
                    }

                }
                else
                {
                    StringBuilder sb = new StringBuilder();

                    if (qs["channelname"] != null)
                    {
                        sb.Append(",channelname='" + qs["channelname"].ToString() + "'");
                    }

                    if (qs["areaid"] != null)
                    {
                        sb.Append(",areaid='" + qs["areaid"].ToString() + "'");
                    }

                    if (qs["sendtype"] != null)
                    {
                        sb.Append(",sendtype='" + qs["sendtype"].ToString() + "'");
                    }

                    if (qs["senduid"] != null)
                    {
                        sb.Append(",senduid='" + qs["senduid"].ToString() + "'");
                    }

                    if (qs["audiourl"] != null)
                    {
                        sb.Append(",audiourl='" + qs["audiourl"].ToString() + "'");
                    }

                    if (qs["uploadurl"] != null)
                    {
                        sb.Append(",uploadurl='" + qs["uploadurl"].ToString() + "'");
                    }

                    string flashImageUrl = qs["flashimageurl"];
                    if (flashImageUrl != null)
                    {
                        sb.Append(",flashimageurl='" + flashImageUrl + "'");
                    }

                    if (sb.Length > 0)
                    {
                        if (sb.ToString().StartsWith(","))
                            sb = sb.Remove(0, 1);
                        SqlHelper.ExecuteNonQuery("update [wy_radio] set " + sb.ToString() + " where rid=" + rid);
                        ischange = true;
                    }
                }

                if (ischange)
                {
                    DataTable radiodt = SqlHelper.ExecuteTable("select * from [wy_radio]");
                    for (int i = 0; i < radiodt.Rows.Count; i++)
                    {
                        int key = Int32.Parse(radiodt.Rows[i]["rid"].ToString());
                        RadioObject ro = new RadioObject();
                        ro.channelname = radiodt.Rows[i]["channelname"].ToString();
                        ro.areaid = Int32.Parse(radiodt.Rows[i]["areaid"].ToString());
                        ro.audiourl = radiodt.Rows[i]["audiourl"].ToString();
                        ro.uploadurl = radiodt.Rows[i]["uploadurl"].ToString();
                        ro.sendtype = Int32.Parse(radiodt.Rows[i]["sendtype"].ToString());
                        if (ro.sendtype > 0)
                        {
                            string[] uidstr = radiodt.Rows[i]["sendtype"].ToString().Trim(',').Split(',');
                            if (uidstr.Length > 0)
                            {
                                ro.senduid = new int[uidstr.Length];
                                for (int j = 0; j < uidstr.Length; j++)
                                {
                                    ro.senduid[j] = Int32.Parse(uidstr[j]);
                                }
                            }
                        }
                        ro.prid = Int32.Parse(radiodt.Rows[i]["prid"].ToString());
                        ro.areacode = radiodt.Rows[i]["areacode"].ToString();
                        ro.flashimageurl = radiodt.Rows[i]["flashimageurl"].ToString();
                        MediaService.radioDic.AddOrUpdate(key, ro, (k, v) => ro);
                    }
                }

                recv = "{\"status\":true}";
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                recv = HttpService.WriteErrorJson("获取电台列表异常!");
            }
            return recv;
        }
        #endregion

        #region 查询所有用户的经纬度
        /// <summary>
        /// 查询所有用户的经纬度
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string GetAllUserLongitudeAndlatitude(NameValueCollection qs)
        {
            #region uid
            /* NameValueCollection 值列表
             * appid
             */

            string recv = "";
            if (qs == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到查询所有用户的经纬度：查询关键字缺失！" + recv, MediaService.wirtelog);
                return HttpService.WriteErrorJson("查询关键字缺失！");
            }
            try
            {
                int appid;
                if (!Int32.TryParse(qs["appid"].ToString(), out appid))
                    return HttpService.WriteErrorJson("appid类型有误！");

                MediaService.WriteLog("接收到查询所有用户的经纬度", MediaService.wirtelog);
                if (SystemVerification())
                {
                    string subrecv = "";
                    foreach (KeyValuePair<int, UserObject> user in MediaService.userDic)
                    {
                        subrecv += (subrecv == "" ? "" : ",") + "{\"uid\":" + user.Key + ",\"lo\": " + user.Value.lo[appid] + ",\"la\":" + user.Value.la[appid] + "}";
                    }
                    recv = "{\"status\":true,\"list\":[" + subrecv + "]}";
                    return recv;
                }
                else
                    return HttpService.WriteErrorJson("用户认证失败！");
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                return HttpService.WriteErrorJson("执行异常：" + err.ToString());
            }
            #endregion
        }
        #endregion


        //#region 查询GoloZ的使用总量
        ///// <summary>
        ///// 查询GoloZ的使用总量
        ///// </summary>
        ///// <param name="qs">键值对</param>
        ///// <returns></returns>
        //public static string GetGoloZTotalUser(NameValueCollection qs)
        //{
        //    #region uid
        //    /* NameValueCollection 值列表
        //     * 
        //     */

        //    string recv = StandardFormat(MessageCode.MissKey);
        //    if (qs == null)//|| qs["uid"] == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
        //    {
        //        MediaService.WriteLog("接收到查询GoloZ的使用总量 ：" + recv, MediaService.wirtelog);
        //        return recv;
        //    }
        //    try
        //    {
        //        MediaService.WriteLog("接收到查询GoloZ的使用总量", MediaService.wirtelog);// ：ouid =" + qs["ouid"].ToString() + " uid =" + qs["uid"].ToString() + " token=" + qs["token"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

        //        //int ouid = 0;
        //        //int uid = 0;
        //        //bool isVerToken = UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
        //        //if (!isVerToken)
        //        //    return recv;

        //        string strSql = @"SELECT COUNT(zsn) FROM app_users WHERE zsn is not null";
        //        object count = SqlHelper.ExecuteScalar(strSql);
        //        string subrecv = "{\"count\":" + count + "}";
        //        return StandardObjectFormat(MessageCode.Success, subrecv);
        //    }
        //    catch (Exception e)
        //    {
        //        return StandardFormat(MessageCode.DefaultError, e.Message);
        //    }
        //    #endregion
        //}
        //#endregion

        #region 验证用户在线状态
        /// <summary>
        /// 验证用户在线状态
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string CheckUserOnlineStatus(NameValueCollection qs)
        {
            if (qs != null && qs["ouid"] != null && qs["appid"] != null && qs["token"] != null)
            {
                try
                {
                    string token = qs["token"];
                    MediaService.WriteLog("CheckUserOnlineStatus----ouid=" + qs["ouid"] + " appid=" + qs["appid"] + " token=" + token, MediaService.wirtelog);

                    int appid, ouid;
                    int.TryParse(qs["appid"], out appid);
                    int.TryParse(qs["ouid"], out ouid);

                    if (appid > 0 && ouid > 0 && token.Length > 0)
                    {
                        string errMessage = "";
                        if (CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                        {
                            string poststr = "action=userinfo.get_base_info_car_logo&app_id=2014042900000006&lan=zh&user_id=" + ouid + "&ver=3.01";
                            string tokenlogin = CommBusiness.StringToMD5Hash(DateTime.Now.Ticks.ToString());
                            string sign = CommBusiness.StringToMD5Hash(poststr + tokenlogin).ToLower();
                            string str = CommBusiness.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=userinfo.get_base_info_car_logo&app_id=2014042900000006&user_id=" + ouid + "&ver=3.01&sign=" + sign, "lan=zh", "POST", Encoding.UTF8);

                            if (CommBusiness.GetJsonValue(str, "code", ",", false) == "0")
                            {
                                string nickname = CommBusiness.GetJsonValue(str, "nick_name", "\"", true);
                                var data = string.Format("{{\"nickname\":\"{0}\"}}", nickname);
                                return CommFunc.StandardObjectFormat(MessageCode.Success, data);
                            }
                            return str;
                        }
                        return errMessage;
                    }
                    else
                    {
                        return CommFunc.StandardFormat(MessageCode.FormatError);
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err, MediaService.wirtelog);
                    return CommFunc.StandardFormat(MessageCode.DefaultError);
                }
            }
            else
            {
                return CommFunc.StandardFormat(MessageCode.MissKey);
            }
        }
        #endregion

        #region 获取用户的所有设备
        /// <summary>
        /// 获取用户的所有设备
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetUsersAllApp(NameValueCollection qs)
        {
            if (qs != null && qs["glsn"] != null)
            {
                try
                {
                    string glsn = qs["glsn"];
                    MediaService.WriteLog("GetUsersAllApp----glsn=" + glsn, MediaService.wirtelog);
                    string sql = string.Format(@"WITH T AS (SELECT a.glsn,m.ouid
                                FROM [dbo].[app_users] a INNER JOIN [dbo].[wy_uidmap] m ON a.[uid]=m.[uid])
                                SELECT glsn FROM T WHERE ouid=(SELECT ouid FROM T WHERE glsn='{0}')", glsn);
                    StringBuilder sb = new StringBuilder();
                    foreach (DataRow row in SqlHelper.ExecuteTable(sql).Rows)
                    {
                        sb.AppendFormat("\"{0}\",", row["glsn"]);
                    }
                    string result = "{\"status\":true,\"data\":[" + sb.ToString().TrimEnd(',') + "]}";
                    MediaService.WriteLog("result:" + result, MediaService.wirtelog);
                    return result;
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("执行异常：" + err, MediaService.wirtelog);
                    return HttpService.WriteErrorJson("Have exception!");
                }
            }
            else
            {
                return HttpService.WriteErrorJson("Request format error!");//请求格式不正确
            }
        }
        #endregion
    }


}
