using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MediaService
{
    /// <summary>
    /// 一些公共的方法
    /// </summary>
    public abstract class CommFunc
    {
        #region Token验证

        private static ConcurrentDictionary<int, string> _TokenDic = new ConcurrentDictionary<int, string>();
        private static ConcurrentDictionary<int, List<int>> _UidMapDic = new ConcurrentDictionary<int, List<int>>();

        //统一Token验证
        internal static bool UniformVerification(string strouid, string struid, string strappid, string token, ref int ouid, ref int uid, ref string errMessage)
        {
            int appid;
            if (!Int32.TryParse(strouid, out ouid) || !Int32.TryParse(struid, out uid) || !Int32.TryParse(strappid, out appid))
            {
                errMessage = StandardFormat(MessageCode.FormatError);
                return false;
            }
            if (!VerificationToken(ouid, uid, appid, token.Replace("'", ""), ref errMessage))
                return false;
            return true;
        }

        //统一Token验证
        internal static bool UniformVerification(string strouid, string strappid, string token, ref int ouid, ref string errMessage)
        {
            int appid;
            if (!Int32.TryParse(strouid, out ouid) || !Int32.TryParse(strappid, out appid))
            {
                errMessage = StandardFormat(MessageCode.FormatError);
                return false;
            }
            if (!VerificationToken(ouid, appid, token.Replace("'", ""), ref errMessage))
                return false;
            return true;
        }

        /// <summary>
        /// 验证Token是否有效
        /// </summary>
        /// <param name="ouid">南山服务器的用户UID</param>
        /// <param name="uid">golo uid</param>
        /// <param name="token">Token码</param>
        /// <returns>验证是否成功：true：成功  false：失败</returns>
        internal static bool VerificationToken(int ouid, int uid, int appid, string token, ref string errMessage)
        {
            //Uid 是否存在
            if (!IsContainsUid(ouid, uid))
            {
                errMessage = StandardFormat(MessageCode.DeviceNotBinding);
                return false;
            }
            //Token 是否有效
            if (IsContainToken(ouid, appid, token, ref errMessage))
                return true;
            return false;
        }
        /// <summary>
        /// 验证Token是否有效
        /// </summary>
        /// <param name="ouid">南山服务器的用户UID</param>
        /// <param name="token">Token码</param>
        /// <returns>验证是否成功：true：成功  false：失败</returns>
        internal static bool VerificationToken(int ouid, int appid, string token, ref string errMessage)
        {
            //Token 是否有效
            if (IsContainToken(ouid, appid, token, ref errMessage))
                return true;
            return false;
        }

        //验证是否存在合法的Token,内存-->数据库-->远端服务
        internal static bool IsContainToken(int ouid, int appid, string token, ref string errMessage)
        {
            //不存在本地
            if (!_TokenDic.ContainsKey(ouid))
            {
                //不存在，找数据库是否存在
                string strQueryToken = "SELECT token FROM wy_usertoken WHERE ouid = " + ouid;
                object strToken = SqlHelper.ExecuteScalar(strQueryToken);

                //数据库存在
                if (strToken != null && !String.IsNullOrWhiteSpace((string)strToken))
                {
                    string localToken = strToken as string;
                    //token 有效
                    if (localToken.Equals(token))
                    {
                        _TokenDic.TryAdd(ouid, token);
                        return true;
                    }
                    else //token 无效
                    {
                        //远端验证
                        if (RemoteVerificationToken(ouid, appid, token, ref errMessage))
                        {
                            //远端验证通过
                            _TokenDic.TryAdd(ouid, token);
                            string strUpdateToken = "UPDATE wy_usertoken SET token = \'" + token + "\' WHERE ouid = " + ouid;
                            SqlHelper.ExecuteNonQuery(strUpdateToken);
                            return true;
                        }
                        else
                            //验证不通过
                            return false;
                    }

                }
                else//数据库不存在
                {
                    //远端验证
                    if (RemoteVerificationToken(ouid, appid, token, ref errMessage))
                    {
                        //远端验证通过
                        _TokenDic.TryAdd(ouid, token);
                        string strInsertToken = "insert into wy_usertoken(ouid,token) values (" + ouid + ",\'" + token + "\')";
                        SqlHelper.ExecuteNonQuery(strInsertToken);
                        return true;
                    }
                    else
                        //验证不通过
                        return false;
                }
            }
            else //存在本地
            {
                string localToken = String.Empty;
                _TokenDic.TryGetValue(ouid, out localToken);
                //有效
                if (localToken.Equals(token))
                {
                    return true;
                }
                else //无效
                {
                    //远端验证
                    if (RemoteVerificationToken(ouid, appid, token, ref errMessage))
                    {
                        //远端验证通过
                        _TokenDic.TryUpdate(ouid, token, token);
                        string strUpdateToken = "UPDATE wy_usertoken SET token = \'" + token + "\' WHERE ouid = " + ouid;
                        SqlHelper.ExecuteNonQuery(strUpdateToken);
                        return true;
                    }
                    else
                        //验证不通过
                        return false;
                }
            }
        }

        //验证是否OUid下是否存在该Uid设备,内存-->数据库
        internal static bool IsContainsUid(int ouid, int uid)
        {
            List<int> uids = new List<int>();
            if (_UidMapDic.ContainsKey(ouid))
            {
                _UidMapDic.TryGetValue(ouid, out uids);
                if (uids.Contains(uid))
                    return true;
            }
            string localToken = String.Empty;
            string strSql = @"SELECT count(*) from wy_uidmap WHERE ouid = " + ouid + " and uid =" + uid;
            //查询数据库是否存在匹配的设备
            object count = SqlHelper.ExecuteScalar(strSql);
            if (((int)count) != 0)
            {
                uids.Add(uid);
                if (!_UidMapDic.ContainsKey(ouid))
                    _UidMapDic.TryAdd(ouid, uids);
                else
                    _UidMapDic.TryUpdate(ouid, uids, uids);
                return true;
            }
            return false;

        }

        //从南山服务器验证Token码
        internal static bool RemoteVerificationToken(int ouid, int appid, string token, ref string errMessage)
        {
            #region 深圳请求认证取消
            string tokenmd5 = StringToMD5Hash(token).ToLower();
            string poststr = "action=userinfo.check_token&app_id=" + appid + "&token=" + tokenmd5 + "&user_id=" + ouid + "&ver=1.0.0";
            string sign = StringToMD5Hash(poststr + token).ToLower();
            string result = HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD + "", "action=userinfo.check_token&token=" + tokenmd5 + "&app_id=" + appid + "&user_id=" + ouid + "&ver=1.0.0&sign=" + sign, "GET", Encoding.UTF8); ;

            MessageFormat message = JsonHelper.JavaScriptSerialize<MessageFormat>(result);
            if (message.code == 0)
                return true;
            else
            {
                if (message.msg == null)
                    message.msg = "";
                if (message.data == null || String.IsNullOrWhiteSpace(message.data.ToString()))
                    message.data = new object();
                errMessage = JsonHelper.JavaScriptSerialize<MessageFormat>(message);
                return false;
            }
            #endregion
        }

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
                    MediaService.WriteLog("http请求POST请求：" + siteurl + "POST:" + query, MediaService.wirtelog);
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
                WebResponse response = (HttpWebResponse)request.GetResponse();
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

        #endregion
        /// <summary>
        /// 统一格式输出
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        internal static string StandardFormat(MessageCode code)
        {
            return StandardFormat((int)code, MessageCodeDiscription.GetMessageCodeDiscription(code));
        }

        #region 统一格式输出--发送到App

        /// <summary>
        /// 统一列表输出
        /// </summary>
        /// <param name="code"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static string StandardListFormat(MessageCode code, string data)
        {
            return StandardFormat((int)code, MessageCodeDiscription.GetMessageCodeDiscription(code), " [ " + data + " ] ");
        }

        /// <summary>
        /// 统一对象输出
        /// </summary>
        /// <param name="code"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static string StandardObjectFormat(MessageCode code, string data)
        {
            if (String.IsNullOrWhiteSpace(data))
                return StandardFormat(code);
            return StandardFormat((int)code, MessageCodeDiscription.GetMessageCodeDiscription(code), data);
        }

        /// <summary>
        /// 错误输出，添加错误描述
        /// </summary>
        /// <param name="code"></param>
        /// <param name="errormessage"></param>
        /// <returns></returns>
        internal static string StandardFormat(MessageCode code, string errormessage)
        {
            return StandardFormat((int)code, MessageCodeDiscription.GetMessageCodeDiscription(code) + " : " + errormessage);
        }

        /// <summary>
        /// 统一格式输出,默认data：{}
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static string StandardFormat(int code, string message, string data = "{ }")
        {
            return "{\"code\":" + code + ",\"msg\":\"" + message + "\",\"data\":" + data + "}";
        }

        #endregion

        #region MD5加密
        internal static string StringToMD5Hash(string inputString)
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

        #region 取Json字符值
        internal static string GetJsonValue(string str, string substr, string laststr, bool isString)
        {
            int k = str.IndexOf("\"" + substr + "\":" + (isString ? "\"" : ""));
            if (k > 0)
            {
                int length = substr.Length + 3 + (isString ? 1 : 0);
                int e = str.IndexOf(laststr, k + length);
                return str.Substring(k + length, e - k - length);
            }
            return String.Empty;
        }
        #endregion

        #region 公共方法

        #region 统一SN位数

        internal const string PREFIXSN = "9716";

        //统一12位的sn号

        internal static string GetUniform12(string sn)
        {
            //#warning 待正式环境改回
            //            return sn;
            sn = sn.Trim().Replace("'", "");
            if (String.IsNullOrWhiteSpace(sn))
                return sn;
            if (sn.Length == 12)
                return sn;
            if (sn.Length == 8)
                return PREFIXSN + sn;
            return sn;
        }

        //统一8位的sn号

        internal static string GetUniform8(string sn)
        {
            //#warning 待正式环境改回
            //            return sn;
            sn = sn.Trim();
            if (String.IsNullOrWhiteSpace(sn))
                return sn;
            if (sn.Length == 8)
                return sn;
            if (sn.Length == 12)
                return sn.Remove(0, 4);
            return sn;
        }

        #endregion


        #region 统一格式输出--发送到Goloz

        internal const int APPID = 7;

        //待定
        #endregion

        #region 错误输出格式

        #region 写错误JSON

        internal static string WriteErrorJson(int code, string message)
        {
            return "{\"status\":false,\"code\":" + code + ",\"message\":\"" + message + "\"}";
        }

        #endregion

        #region 写错误JSON

        internal static string WriteErrorJson(int code)
        {
            return "{\"status\":false,\"code\":" + code + ",\"message\":\"" + "message" + "\"}";
        }

        #endregion

        #endregion

        #region 时间戳

        /// <summary>
        /// DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name=”time”></param>
        /// <returns></returns>
        private static int ConvertDateTimeInt(DateTime time)
        {
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        }

        public static int ConvertDateTimeInt(string time)
        {
            DateTime dt;
            if (DateTime.TryParse(time, out dt))
            {
                return ConvertDateTimeInt(dt);
            }
            return 0;
        }

        // 时间戳转为C#格式时间

        public static DateTime StampToDateTime(string timeStamp)
        {
            DateTime dateTimeStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = Int64.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dateTimeStart.Add(toNow);
        }

        #endregion

        #endregion

        /// <summary>
        /// 查询已经绑定用户的经销商信息
        /// </summary>
        /// <param name="appid"></param>
        /// <param name="glsn"></param>
        /// <param name="appkey"></param>
        /// <returns></returns>
        internal static string GetVenderInfo(string appid, object glsn, string appkey)
        {
            Dictionary<string, string> post = new Dictionary<string, string>();
            var sign = StringToMD5Hash(appid + glsn + appkey);
            post.Add("appId", appid);
            post.Add("serialNo", glsn.ToString());
            post.Add("sign", sign);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute(ConstStrings.StrNeedCheckReSeller_Release_Url, posturl, "POST",
                Encoding.UTF8);
            return str;
        }



        private static bool SendToGoloZTalkInfo(int uid, string tid, short comm, bool hasCreate = true)
        {
            #region 通知Goloz
            StringBuilder sb = new StringBuilder();
            string sql;
            List<SqlParameter> paras = new List<SqlParameter> { new SqlParameter("@tid", tid) };
            if (hasCreate)
            {
                sql = "select t3.glsn,T1.id,T1.xuhao,T1.duijiang,T1.remark,T2.* from (select id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid =@uid) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid  and T2.tid =@tid inner join app_users as t3 on t3.uid=t2.createuid";
                paras.Add(new SqlParameter("@uid", uid));
            }
            else
            {
                sql = "select t3.glsn, tid, talkname, auth, createuid, muid, info, talknotice, t1.moditime, usernum, imageurl, [type],talkmode from [wy_talk] t1 inner join app_users as t3 on t3.uid=t1.createuid WHERE tid=@tid";
            }
            DataTable dt = SqlHelper.ExecuteTable(sql, paras.ToArray());
            if (dt.Rows.Count < 1)
            {
                MediaService.WriteLog("执行异常：推送到Goloz的频道信息消息失败，uid=" + uid + " tid=" + tid, MediaService.wirtelog);
                return false;
            }
            MediaService.WriteLog("打印SQL，sql=" + sql + "", MediaService.wirtelog);
            string xuhao = "";
            string dj = "";
            string remark = "";
            var row = dt.Rows[0];
            if (hasCreate)
            {
                xuhao = row["xuhao"].ToString();
                dj = row["duijiang"].ToString();
                remark = row["remark"].ToString();
            }
            int totalnum = 0;
            int usernum = 0;
            HttpZGoloBusiness.GetTalkNum(Convert.ToInt32(tid), ref totalnum, ref usernum);
            string auth = row["auth"].ToString();
            string talkname = row["talkname"].ToString();
            string create = "false";
            if (row["createuid"].ToString() == uid.ToString() || totalnum <= 20)
                create = "true";
            var talkmode = row["talkmode"].ToString();
            if (row["type"] != null && row["type"].ToString() == "3")
            {
                sb.Append("{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"xuhao\":\"" + xuhao + "\",\"auth\":\"" + auth + "\",\"remark\":\"" + row["talknotice"] + "\",\"dj\":\"" + dj + "\",\"create\":" + create + ",\"usernum\":\"" + usernum + "\",\"muid\":\"" + row["muid"] + "\",\"info\":\"" + row["info"] + "\",\"talknotice\":\"" + row["talknotice"] + "\",\"glsn\":\"" + row["glsn"] + "\",\"moditime\":" + row["moditime"] + ",\"imageurl\":\"" + row["imageurl"] + "\",\"type\":" + row["type"] + ",\"talkmode\":" + talkmode + "}");
            }
            else
            {
                if (row["talknotice"] != null && row["talknotice"].ToString() != string.Empty)
                {
                    remark = row["talknotice"].ToString();
                }
                else if (hasCreate && row["remark"] != null && row["remark"].ToString() != string.Empty)
                {
                    remark = row["remark"].ToString();
                }
                sb.Append("{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"xuhao\":\"" + xuhao + "\",\"auth\":\"" + auth + "\",\"remark\":\"" + remark + "\",\"dj\":\"" + dj + "\",\"create\":" + create + ",\"usernum\":\"" + usernum + "\",\"muid\":\"" + row["muid"] + "\",\"info\":\"" + row["info"] + "\",\"talknotice\":\"" + row["talknotice"] + "\",\"glsn\":\"" + row["glsn"] + "\",\"moditime\":" + row["moditime"] + ",\"imageurl\":\"" + row["imageurl"] + "\",\"type\":" + row["type"] + ",\"talkmode\":" + talkmode + "}");
            }

            return PublicClass.SendToUser(null, sb.ToString(), "", uid, 99, 0, comm, CommFunc.APPID);
            #endregion
        }

        /// <summary>
        /// 获取频道信息
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static string GetTalkInfoNum(int tid)
        {
            string recv = "";
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
                string imageurl = dt.Rows[0]["imageurl"] == null ? "" : dt.Rows[0]["imageurl"].ToString();
                if (imageurl.Equals("<null>"))
                    imageurl = "";
                int totalnum = 0;
                int usernum = 0;
                HttpZGoloBusiness.GetTalkNum(tid, ref totalnum, ref usernum);
                recv="{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\",\"remark\":\"" + remark + "\",\"create\":" + create + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + ",\"imageurl\":\"" + imageurl + "\"}";
            }
            return recv;
        }

        /// <summary>
        /// 生成不大于int最大值的随机数
        /// </summary>
        /// <returns></returns>
        public static int GenerateTid()
        {
            return Convert.ToInt32("9" + new Random().Next(1,999999) + "99");
        }
    }
}
