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
using System.Collections.Concurrent;

namespace MediaService
{
    class HttpWifiBusiness
    {
        public static ConcurrentDictionary<int, string> userWifiDic = new ConcurrentDictionary<int, string>();

        #region 用户登录
        public static string UserLogin(NameValueCollection qs)
        {
            if (qs != null && qs["uid"] != null && qs["ip"] != null )
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                string ip = qs["ip"].ToString();
                if (UserIpVerification(uid, ip))
                {
                    DataTable dt = SqlHelper.ExecuteTable("select uid,username,nickname,gender,email,mobile,avatar,token from[app_users] where uid='" + uid + "'");
                    if (dt.Rows.Count > 0)
                    {
                        string username = dt.Rows[0]["username"].ToString().Trim();
                        string nickname = dt.Rows[0]["nickname"].ToString().Trim();
                        int gender = Int32.Parse(dt.Rows[0]["gender"].ToString());
                        string email = dt.Rows[0]["email"].ToString().Trim();
                        string mobile = dt.Rows[0]["mobile"].ToString();
                        string avatar = dt.Rows[0]["avatar"].ToString();
                        string tokenlogin = CommBusiness.StringToMD5Hash(username + DateTime.Now.Ticks.ToString());
                        userWifiDic.AddOrUpdate(uid, tokenlogin, (key, oldValue) => tokenlogin);
                        UserLoginWebJson userLoginJson = new UserLoginWebJson(true, uid, username, nickname, gender, email, mobile, tokenlogin, "");
                        DataContractJsonSerializer json = new DataContractJsonSerializer(userLoginJson.GetType());
                        using (MemoryStream stream = new MemoryStream())
                        {
                            json.WriteObject(stream, userLoginJson);
                            return Encoding.UTF8.GetString(stream.ToArray());
                        }
                    }
                    else
                    {
                        return HttpService.WriteErrorJson("用户未找到!");
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

        #region
        public static string GetMyMessage(NameValueCollection qs)
        {
            if (qs["uid"] != null && qs["token"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                string token = qs["token"].ToString().Replace("'", "");
                if (UserVerification(uid, token))
                {
                    DataTable dt = SqlHelper.ExecuteTable("select uid,username,nickname,gender,email,mobile,avatar,token from[app_users] where uid=" + uid);
                    if (dt.Rows.Count > 0)
                    {
                        string username = dt.Rows[0]["username"].ToString().Trim();
                        string nickname = dt.Rows[0]["nickname"].ToString().Trim();
                        int gender = Int32.Parse(dt.Rows[0]["gender"].ToString());
                        string email = dt.Rows[0]["email"].ToString().Trim();
                        string mobile = dt.Rows[0]["mobile"].ToString();
                        string avatar = dt.Rows[0]["avatar"].ToString();
                        UserLoginWebJson userLoginJson = new UserLoginWebJson(true, uid, username, nickname, gender, email, mobile, token, "");
                        DataContractJsonSerializer json = new DataContractJsonSerializer(userLoginJson.GetType());
                        using (MemoryStream stream = new MemoryStream())
                        {
                            json.WriteObject(stream, userLoginJson);
                            return Encoding.UTF8.GetString(stream.ToArray());
                        }
                    }
                    else
                    {
                        return CommBusiness.WriteErrorJson(20, "用户不存在！");
                    }
                }
                else
                {
                    return CommBusiness.WriteErrorJson(4, "用户名或密码错误！");
                }
            }
            else
            {
                return CommBusiness.WriteErrorJson(11, "请求格式不正确！");
            }
        }
        #endregion

        #region 获取用户所在的组
        public static string GetMyTalkList(NameValueCollection qs)
        {
            if (qs != null && qs["uid"] != null && qs["ip"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                string ip = qs["ip"].ToString();
                if (UserIpVerification(uid, ip))
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

        #region 用户创建组
        public static string CreateTalk(NameValueCollection qs)
        {
            if (qs != null && qs["uid"] != null && qs["ip"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                string ip = qs["ip"].ToString();
                if (UserIpVerification(uid, ip))
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
                if (qs != null && qs["uid"] != null && qs["ip"] != null)
                {
                    int uid = Int32.Parse(qs["uid"].ToString());
                    string ip = qs["ip"].ToString();
                    if (UserIpVerification(uid, ip))
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
            if (qs != null && qs["uid"] != null && qs["ip"] != null && qs["tid"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                string ip = qs["ip"].ToString();
                int tid = Int32.Parse(qs["tid"].ToString());
                if (UserIpVerification(uid, ip))
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
            if (qs != null && qs["uid"] != null && qs["ip"] != null && qs["tid"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                string ip = qs["ip"].ToString();
                int tid = Int32.Parse(qs["tid"].ToString());
                if (UserIpVerification(uid, ip))
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
            if (qs != null && qs["uid"] != null && qs["ip"] != null && qs["tid"] != null)
            {
                int uid = Int32.Parse(qs["uid"].ToString());
                string ip = qs["ip"].ToString();
                int tid = Int32.Parse(qs["tid"].ToString());
                if (UserIpVerification(uid, ip))
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

        #region 用户验证
        public static bool UserVerification(int uid, string token)
        {
            string tokenlogin="";
            if(userWifiDic.TryGetValue(uid, out tokenlogin))
            {
                if(tokenlogin==token)
                    return true;
            }
            return false;
        }
        #endregion

        #region 用户Ip验证
        public static bool UserIpVerification(int uid, string ip)
        {
            bool state = false;
            object obj = SqlHelper.ExecuteScalar("select uid from [app_users] where uid=" + uid);
            if (obj != null)
            {
                if (ip == "127.0.0.1")
                {
                    state = true;
                }
                else
                {
                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        if (uo.ip != null && uo.ip != "")
                        {
                            if (uo.ip == ip)
                                return true;
                        }
                    }
                }
            }
            return state;
        }
        #endregion

    }
}