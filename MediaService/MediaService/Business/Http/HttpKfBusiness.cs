using System;
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
using System.Collections.Concurrent;

namespace MediaService
{
    class KFUserObject
    {
        public DateTime time { get; set; }
        public string username { get; set; }
        public int onwork { get; set; }//1:上班，0:下班
        public string version { get; set; }
    }
    class HttpKfBusiness
    {
        public static ConcurrentDictionary<int, KFUserObject> KFUserDic = new ConcurrentDictionary<int, KFUserObject>();

        #region 获取在线用户列表
        public static string GetOnLineUsers(NameValueCollection qs)
        {
            StringBuilder sb = new StringBuilder();
            if (qs != null && qs["uid"] != null && qs["appid"] != null)
            {
                string[] uid = qs["uid"].ToString().Trim().Split(',');
                int appid = Int32.Parse(qs["appid"].ToString());
                for (int i = 0; i < uid.Length; i++)
                {
                    if (uid[i] != "")
                    {
                        int u = Int32.Parse(uid[i]);
                        UserObject uo = null;
                        if (MediaService.userDic.TryGetValue(u, out uo))
                        {
                            if (uo.socket[appid] != null)
                            {
                                sb.Append(",{\"uid\":" + uid[i] + ",\"line\":1,\"lo\":" + uo.lo[appid] + ",\"la\":" + uo.la[appid] + ",\"vi\":" + uo.vi[appid] + ",\"di\":" + uo.di[appid] + "}");
                            }
                        }
                    }
                }
                if (sb.Length > 0)
                {
                    sb.Remove(0, 1);
                }
                sb.Insert(0, "{\"status\":true,\"list\":[");
                sb.Append("]}");
                return sb.ToString();
            }
            else
            {
                return "{\"status\":false}";
            }
        }
        #endregion

        #region 获取频道在线用户--win
        public static string GetTalkOnLineUser(NameValueCollection qs)
        {
            string recv = "";
            string uidstr = "";
            if (qs["kfuid"] != null && qs["appid"] != null && qs["token"] != null)
            {
                if (!KFUserVerification(Int32.Parse(qs["kfuid"].ToString()), Int32.Parse(qs["appid"].ToString()), qs["token"].ToString()))
                {
                    recv = WriteErrorJson("对不起您没有权限，请确认登陆！");
                    return recv;
                }
            }
            else
            {
                recv = WriteErrorJson("请求参数有误！");
                return recv;
            }
            StringBuilder sb = new StringBuilder();
            if (qs != null && qs["talkname"] != null && qs["appid"] != null)
            {
                object obj = SqlHelper.ExecuteScalar(" select tid from [wy_talk] where [talkname] = '" + qs["talkname"].ToString().Replace("'", "") + "'");
                if (obj != null)
                {
                    int tid = Int32.Parse(obj.ToString());
                    TalkMessage talkmessage = null;
                    if (MediaService.talkDic.TryGetValue(tid, out talkmessage))
                    {
                        if (talkmessage.uidlist.Count > 0)
                        {
                            foreach (int userid in talkmessage.uidlist)
                            {
                                uidstr += userid + ",";
                            }
                        }
                        if (uidstr.Length > 0)
                        {
                            uidstr.Substring(0, uidstr.Length - 1);
                            NameValueCollection qsnvc = new NameValueCollection();
                            qsnvc.Add("uid", uidstr);
                            qsnvc.Add("appid", "7");
                            recv = GetOnLineUsers(qsnvc);
                            if (!recv.Contains("true"))
                                recv = WriteErrorJson("查询失败，请联系管理员！");
                        }
                    }
                    else
                    {
                        recv = WriteErrorJson("频道没有任何在线用户！");
                    }
                }
                else
                {
                    recv = WriteErrorJson("不存在此频道！");
                }
                return recv;
            }
            else
            {
                return WriteErrorJson("参数错误，请联系管理员！");
            }
        }
        #endregion

        #region 获取工单状态-Win
        public static string GetSeriveByID(NameValueCollection qs)
        {
            if (qs["kfuid"] != null && qs["appid"] != null && qs["token"] != null)
            {
                if (!KFUserVerification(Int32.Parse(qs["kfuid"].ToString()), Int32.Parse(qs["appid"].ToString()), qs["token"].ToString()))
                {
                    return WriteErrorJson("对不起您没有权限，请确认登陆！");
                }
                StringBuilder sb = new StringBuilder("{\"status\":true");
                DataTable dt1 = SqlHelper.ExecuteTable("select top 1 id,uid,state,addtime,serivetype from [app_userserive] where id=" + qs["seriveid"]);
                string uid = "";
                if (dt1.Rows.Count > 0)
                {
                    uid = dt1.Rows[0]["uid"].ToString();
                    sb.Append(",\"id\":\"" + dt1.Rows[0]["id"].ToString() + "\"");
                    sb.Append(",\"uid\":\"" + uid + "\"");
                    sb.Append(",\"state\":\"" + dt1.Rows[0]["state"].ToString() + "\"");
                    sb.Append(",\"addtime\":\"" + dt1.Rows[0]["addtime"].ToString() + "\"");
                    sb.Append(",\"serivetype\":\"" + dt1.Rows[0]["serivetype"].ToString() + "\"");
                }
                else
                {
                    sb.Append(",\"id\":\"0\"");
                    sb.Append(",\"uid\":\"0\"");
                    sb.Append(",\"state\":\"\"");
                    sb.Append(",\"addtime\":\"\"");
                    sb.Append(",\"serivetype\":\"\"");
                }
                sb.Append("}");
                return sb.ToString();
            }
            else
            {
                return "{\"status\":false}";
            }
        }
        #endregion

        #region 获取工单-Win
        public static string GetOneUserSerive(NameValueCollection qs)
        {
            if (qs["kfuid"] != null && qs["appid"] != null && qs["token"] != null)
            {
                if (!KFUserVerification(Int32.Parse(qs["kfuid"].ToString()), Int32.Parse(qs["appid"].ToString()), qs["token"].ToString()))
                {
                    return WriteErrorJson("对不起您没有权限，请确认登陆！");
                }
                //记录在线客服
                if (qs["kfuid"] != null && qs["kfuid"].ToString() != "")
                {
                    int kfuid = Int32.Parse(qs["kfuid"].ToString());
                    KFUserObject kfuo = null;
                    if (KFUserDic.TryGetValue(kfuid, out kfuo))
                    {
                        kfuo.onwork = 1;
                        kfuo.time = DateTime.Now;
                    }
                }

                string getSeriveStr = "";
                if (qs["sid"] != null && qs["sid"].ToString() != "")
                {
                    getSeriveStr = " select top 1 id,uid,state,addtime,serivetype from [app_userserive] where state=0 and id=" + qs["sid"].ToString();
                }
                else if (qs["uid"] != null)
                {
                    int useruid = Int32.Parse(qs["uid"].ToString());
                    getSeriveStr = "select top 1 id,uid,state,addtime,serivetype from [app_userserive] where state=0 and serivetype=100 and uid=" + useruid;
                }
                else
                {
                    string seriveType = "0";
                    string usertype = "";
                    if (qs["usertype"] != null)
                    {
                        int utype = Int32.Parse(qs["usertype"].ToString());
                        if (utype == 1)
                            usertype = " and uid<10000000";
                        else
                            usertype = " and uid>10000000";
                    }
                    else
                    {
                        usertype = " and uid>10000000";
                    }
                    if (qs["serivetype"] != null && qs["serivetype"].ToString() != "")
                        seriveType = qs["serivetype"].ToString();

                    string[] types = seriveType.Split(',');
                    string sqlwhere = "";
                    if (types.Length > 0)
                    {
                        foreach (string type in types)
                        {
                            sqlwhere += " or  serivetype='" + type + "' ";
                        }
                        if (sqlwhere.StartsWith(" or"))
                            sqlwhere = sqlwhere.Substring(3);
                    }
                    getSeriveStr = "select top 1 id,uid,state,addtime,serivetype from [app_userserive] where state=0 and (" + sqlwhere + ")" + usertype;
                }

                DataTable dt1 = SqlHelper.ExecuteTable(getSeriveStr);
                string uid = "";
                int serivetype = 0;
                StringBuilder sb = new StringBuilder("{\"status\":true");
                if (dt1.Rows.Count > 0)
                {
                    if (dt1.Rows[0]["serivetype"] != null && dt1.Rows[0]["serivetype"].ToString() != "")
                        serivetype = Int32.Parse(dt1.Rows[0]["serivetype"].ToString());
                    uid = dt1.Rows[0]["uid"].ToString();
                    sb.Append(",\"id\":\"" + dt1.Rows[0]["id"].ToString() + "\"");
                    sb.Append(",\"uid\":\"" + uid + "\"");
                    sb.Append(",\"state\":\"" + dt1.Rows[0]["state"].ToString() + "\"");
                    sb.Append(",\"addtime\":\"" + dt1.Rows[0]["addtime"].ToString() + "\"");
                    sb.Append(",\"serivetype\":\"" + serivetype + "\"");
                }
                else
                {
                    uid = "0";
                    sb.Append(",\"id\":\"0\"");
                    sb.Append(",\"uid\":\"0\"");
                    sb.Append(",\"state\":\"\"");
                    sb.Append(",\"addtime\":\"\"");
                    sb.Append(",\"serivetype\":\"0\"");
                }
                string sqlStr = @"SELECT [uid],glsn,[username],[nickname] ,[email],[gender] ,[mobile]
                              FROM [app_users] where uid=" + uid;
                DataTable dt = SqlHelper.ExecuteTable(sqlStr);
                string username = "";
                string nickname = "";
                if (dt.Rows.Count > 0)
                {
                    username = dt.Rows[0]["username"].ToString();
                    nickname = dt.Rows[0]["nickname"].ToString();
                    sb.Append(",\"glsn\":\"" + dt.Rows[0]["glsn"].ToString() + "\"");
                    sb.Append(",\"uname\":\"" + dt.Rows[0]["username"].ToString() + "\"");
                    sb.Append(",\"nickname\":\"" + dt.Rows[0]["nickname"].ToString() + "\"");
                    sb.Append(",\"email\":\"" + dt.Rows[0]["email"].ToString() + "\"");
                    sb.Append(",\"gender\":\"" + dt.Rows[0]["gender"].ToString() + "\"");
                    sb.Append(",\"mobile\":\"" + dt.Rows[0]["mobile"].ToString() + "\"");
                }
                else
                {
                    sb.Append(",\"glsn\":\"\"");
                    sb.Append(",\"uname\":\"\"");
                    sb.Append(",\"nickname\":\"\"");
                    sb.Append(",\"email\":\"\"");
                    sb.Append(",\"gender\":\"\"");
                    sb.Append(",\"mobile\":\"\"");
                }
                sqlStr = @"select top 15 id,uid,solveremark,solvename,solvetype,soundurl,solvestate,state,serivetype,solvetime from app_userserive where uid=" + uid + " order by addtime desc";
                DataTable dt3 = SqlHelper.ExecuteTable(sqlStr);
                if (dt3 != null && dt3.Rows.Count > 0)
                {
                    sb.Append(",\"serivelist\":[");
                    foreach (DataRow item in dt3.Rows)
                    {
                        string solvestate = "";
                        string userserivetype = "";
                        if (item["solvestate"] != null)
                        {
                            switch (item["solvestate"].ToString())
                            {
                                case "0":
                                    solvestate = "未解决";
                                    break;
                                case "1":
                                    solvestate = "已解决";
                                    break;
                            }
                        }
                        if (item["serivetype"] != null)
                        {
                            switch (item["serivetype"].ToString())
                            {
                                case "0":
                                    userserivetype = "普通";
                                    break;
                                case "3":
                                    userserivetype = "转接";
                                    break;
                                case "100":
                                    userserivetype = "回拨";
                                    break;
                            }
                        }
                        sb.Append("{");
                        sb.Append("\"id\":\"" + item["id"].ToString() + "\"");
                        sb.Append(",\"uid\":\"" + item["uid"].ToString() + "\"");
                        sb.Append(",\"username\":\"" + username + "\"");
                        sb.Append(",\"nickname\":\"" + nickname + "\"");
                        sb.Append(",\"solveremark\":\"" + item["solveremark"].ToString() + "\"");
                        sb.Append(",\"solvename\":\"" + item["solvename"].ToString() + "\"");
                        sb.Append(",\"solvestate\":\"" + solvestate + "\"");
                        sb.Append(",\"solvetype\":\"" + item["solvetype"].ToString() + "\"");
                        sb.Append(",\"serivetype\":\"" + userserivetype + "\"");
                        sb.Append(",\"solvetime\":\"" + item["solvetime"].ToString() + "\"");
                        sb.Append(",\"soundurl\":\"" + item["soundurl"].ToString() + "\"");
                        sb.Append("},");
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("]");
                }
                sb.Append("}");
                return sb.ToString();
            }
            else
            {
                return "{\"status\":false}";
            }
        }
        #endregion

        #region 更新工单信息-Win
        public static string UpdateSerive(NameValueCollection qs)
        {
            if (qs["kfuid"] != null && qs["appid"] != null && qs["token"] != null)
            {
                if (!KFUserVerification(Int32.Parse(qs["kfuid"].ToString()), Int32.Parse(qs["appid"].ToString()), qs["token"].ToString()))
                {
                    return WriteErrorJson("对不起您没有权限，请确认登陆！");
                }
                StringBuilder sb = new StringBuilder("{\"status\":");
                int value = 0;
                if (qs["serivetype"] != null && qs["serivetype"] != "")
                    value = SqlHelper.ExecuteNonQuery("update [app_userserive] set  state=" + qs["state"] + ",serivetype=" + qs["serivetype"] + " where uid='" + qs["uid"] + "' and state=" + qs["oldstate"]);
                else if (qs["seriveid"] != null && qs["seriveid"].ToString() != "" && Int32.Parse(qs["seriveid"]) != 0)
                    value = SqlHelper.ExecuteNonQuery("update [app_userserive] set solveremark='" + qs["solveremark"] + "',solvetime='" + DateTime.Now.ToString() + "',solvename='" + qs["username"] + "',[solvetype]='" + qs["solvetype"] + "',[solvestate]=" + qs["solvestate"] + " where uid='" + qs["uid"] + "' and id=" + qs["seriveid"]);
                else
                    value = SqlHelper.ExecuteNonQuery("update [app_userserive] set state=" + qs["state"] + " where uid='" + qs["uid"] + "' and state=" + qs["oldstate"]);
                if (value > 0)
                {
                    sb = sb.Append("true");
                }
                else
                {
                    sb.Append("false");
                }
                sb.Append("}");
                return sb.ToString();
            }
            else
            {
                return "{\"status\":false}";
            }
        }
        #endregion

        #region 获取客服工单-Win
        public static string GetKefuSerive(NameValueCollection qs)
        {
            if (qs["kfuid"] != null && qs["appid"] != null && qs["token"] != null)
            {
                if (!KFUserVerification(Int32.Parse(qs["kfuid"].ToString()), Int32.Parse(qs["appid"].ToString()), qs["token"].ToString()))
                {
                    return WriteErrorJson("对不起您没有权限，请确认登陆！");
                }
                string sqlStr = "";
                StringBuilder sb = new StringBuilder();
                sqlStr = @"SELECT top " + qs["pagesize"] + " a.id, a.uid,b.username,b.nickname, [solveremark] ,[solvename],solvetype,soundurl ,case [solvestate] when 0 then '未解决' when 1 then '已解决' else ' ' end as solvestate,[solvetime] FROM [app_userserive] a"
                        + " left join app_users b on a.uid=b.uid "
                        + " where  a.id not in (select top " + (Int32.Parse(qs["pageindex"]) - 1) * Int32.Parse(qs["pagesize"]) + " id from app_userserive where solvename='" + qs["username"] + "' order by [solvetime] desc ) and solvename='" + qs["username"] + "' order by [solvetime] desc";
                DataTable dt = SqlHelper.ExecuteTable(sqlStr);
                if (dt != null && dt.Rows.Count > 0)
                {
                    sb.Append("{\"serivelist\":[");
                    foreach (DataRow item in dt.Rows)
                    {
                        sb.Append("{");
                        sb.Append("\"id\":\"" + item["id"].ToString() + "\"");
                        sb.Append(",\"uid\":\"" + item["uid"].ToString() + "\"");
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
                    return "{\"status\":false}";
            }
            else
            {
                return "{\"status\":false}";
            }
        }
        #endregion

        #region 更新工单的录音记录URL--KF
        public static string UpdateSeriveSountUrl(NameValueCollection qs)
        {
            string recv = "";
            try
            {
                if (qs["kfuid"] != null && qs["appid"] != null && qs["token"] != null)
                {
                    if (!KFUserVerification(Int32.Parse(qs["kfuid"].ToString()), Int32.Parse(qs["appid"].ToString()), qs["token"].ToString()))
                    {
                        return WriteErrorJson("对不起您没有权限，请确认登陆！");
                    }
                    string seriveid = qs["seriveid"].ToString().Replace("'", "");
                    string soundurl = qs["soundurl"].ToString().Replace("'", "");
                    string sqlStr = @" update [app_userserive] set [soundurl]='" + soundurl + "' where id='" + seriveid + "'";
                    SqlHelper.ExecuteNonQuery(sqlStr);
                    recv = "{\"status\":true}";
                }
                else
                {
                    recv = WriteErrorJson("请求参数有误！");
                }
            }
            catch
            {
                recv = WriteErrorJson("操作失败,请稍后再试");
            }
            return recv;
        }
        #endregion

        #region 获取用户基本信息-Win
        public static string GetUserMessage(NameValueCollection qs)
        {
            DataTable dt = new DataTable();
            if (qs != null)
            {
                if (qs["kfuid"] != null && qs["appid"] != null && qs["token"] != null)
                {
                    if (!KFUserVerification(Int32.Parse(qs["kfuid"].ToString()), Int32.Parse(qs["appid"].ToString()), qs["token"].ToString()))
                    {
                        return WriteErrorJson("对不起您没有权限，请确认登陆！");
                    }
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
                        return WriteErrorJson("获取个人信息失败！");
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
                        return WriteErrorJson("获取个人信息失败！");
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
                    return WriteErrorJson("没有找到此用户！");
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
                return WriteErrorJson("请求参数不正确！");
            }
        }
        #endregion

        #region 客服登陆验证
        public static string KFUserLoginVerification(NameValueCollection qs)
        {
            string recv = "{\"status\":true}";
            int uid = 0;
            int appid = 7;
            string token = "";
            if (qs["uid"] != null && qs["uid"].ToString() != "" && qs["appid"] != null && qs["appid"].ToString() != "" && qs["token"] != null)
            {
                uid = Int32.Parse(qs["uid"].ToString());
                appid = Int32.Parse(qs["appid"].ToString());
                token = qs["token"].ToString();
                if (!KFUserVerification(uid, appid, token))
                {
                    recv = WriteErrorJson("身份验证失败!");
                }

            }
            else
            {
                recv = WriteErrorJson("参数不正确!");
            }
            return recv;
        }
        #endregion


        /*********************公共函数*********************/

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

        #region 写错误JSON
        public static string WriteErrorJson(string message)
        {
            return "{\"status\":false,\"message\":\"" + message + "\"}";
        }
        #endregion

        #region KF新版用户验证
        public static bool KFUserVerification(int uid, int appid, string token)
        {
            UserObject uo = null;
            if (MediaService.userDic.TryGetValue(uid, out uo))
            {
                if (uo.token[appid] != null && uo.token[appid] != "")
                {
                    if (uo.token[appid] == token)
                    {
                        KFUserObject kfuo = null;
                        if (KFUserDic.TryGetValue(uid, out kfuo))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion

    }
}
