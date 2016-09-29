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

namespace MediaService
{
    class HttpUserBusiness
    {
        public static Hashtable UserHashTable = Hashtable.Synchronized(new Hashtable());

        #region 用户登录
        public static string UserLogin(NameValueCollection qs)
        {
            if (qs != null && qs["__API__[app_key]"] != null && qs["__API__[app_secret]"] != null && qs["login_key"] != null && qs["password"] != null)
            {
                string app_key = qs["__API__[app_key]"].ToString().Replace("'", "");
                string app_secret = qs["__API__[app_secret]"].ToString().Replace("'", "");

                DataRow[] dr = MediaService.allapp.Select("app_key='" + app_key + "' and app_secret='" + app_secret + "'");
                if (dr.Length > 0)
                {
                    int appid = Int32.Parse(dr[0]["id"].ToString());
                    string login_key = qs["login_key"].ToString();
                    string password = qs["password"].ToString() == "" ? "" : CommBusiness.StringToMD5Hash(qs["password"].ToString()).ToLower();
                    int uid = 0;
                    string username = "";
                    string nickname = "";
                    int gender = 0;
                    string email = "";
                    string mobile = "";
                    string avatar = "";
                    string tokenlogin = CommBusiness.StringToMD5Hash(password + DateTime.Now.Ticks.ToString());
                    string key = "username";
                    if (login_key.Length == 12 && login_key[0] == '9')
                    {
                        key = "glsn";
                    }
                    if (login_key.IndexOf('@') > 0)
                    {
                        key = "email";
                    }
                    else if (login_key[0] > 47 && login_key[0] < 58)
                    {
                        key = "mobile";
                    }
                    DataTable dt = SqlHelper.ExecuteTable("select uid,username,nickname,gender,email,mobile,avatar,token from[app_users] where " + key + "='" + login_key + "' and password='" + password + "'");
                    if (dt.Rows.Count > 0)
                    {
                        uid = Int32.Parse(dt.Rows[0]["uid"].ToString());
                        username = dt.Rows[0]["username"].ToString().Trim();
                        nickname = dt.Rows[0]["nickname"].ToString().Trim();
                        gender = Int32.Parse(dt.Rows[0]["gender"].ToString());
                        email = dt.Rows[0]["email"].ToString().Trim();
                        mobile = dt.Rows[0]["mobile"].ToString();
                        avatar = dt.Rows[0]["avatar"].ToString();
                    }
                    if (uid != 0)
                    {
                        if (!UserHashTable.Contains(uid))
                        {
                            UserHashTable.Add(uid, tokenlogin);
                        }
                        else
                        {
                            UserHashTable.Remove(uid);
                            UserHashTable.Add(uid, tokenlogin);
                        }
                        UserLoginWebJson userLoginJson = new UserLoginWebJson(true, uid, username, nickname, gender, email, mobile, tokenlogin, password);
                        DataContractJsonSerializer json = new DataContractJsonSerializer(userLoginJson.GetType());
                        using (MemoryStream stream = new MemoryStream())
                        {
                            json.WriteObject(stream, userLoginJson);
                            return Encoding.UTF8.GetString(stream.ToArray());
                        }
                    }
                    else
                    {
                        return CommBusiness.WriteErrorJson(4, "用户名或密码错误！");
                    }
                }
                else
                {

                    return CommBusiness.WriteErrorJson(15, "应用鉴权失败！");
                }
            }
            else
            {
                return CommBusiness.WriteErrorJson(11, "请求格式不正确！");
            }
        }
        #endregion

        #region
        public static string GetMyMessage(NameValueCollection qs)
        {
            if(qs["uid"]!=null&&qs["token"]!=null)
            {
                int uid=Int32.Parse (qs["uid"].ToString());
                string token=qs["token"].ToString ().Replace("'","");
                if(UserVerification(uid,token))
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

        #region 用户验证
        public static bool UserVerification(int uid, string token)
        {
            if (UserHashTable.Contains(uid) && UserHashTable[uid].ToString() == token)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
}

