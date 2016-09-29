using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Net.Sockets;
using System.Data;
using System.Collections.Specialized;

namespace MediaService
{
    class PublicClass
    {

        #region 服务器推送信息至用户列表
        public static string SendToUserList(byte[] buffer, string onlinecontent, string offlinecontent, List<int> uidlist, int mtype, int index, short comm, int appid)
        {
            int len = 0;
            if (buffer == null)
            {
                byte[] cbyte = Encoding.UTF8.GetBytes(onlinecontent);
                len = cbyte.Length + 8;
                buffer = new byte[len];
                Buffer.BlockCopy(cbyte, 0, buffer, 8, cbyte.Length);
            }
            else
            {
                len = Encoding.UTF8.GetBytes(onlinecontent, 0, onlinecontent.Length, buffer, 8) + 8;
            }
            Buffer.BlockCopy(System.BitConverter.GetBytes((short)len), 0, buffer, 0, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(comm), 0, buffer, 2, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(index), 0, buffer, 4, 4);
            string sql = "";
            bool state = false;
            foreach (int uid in uidlist)
            {
                state = false;
                try
                {
                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        if (uo.socket != null && uo.socket[appid] != null)
                        {
                            uo.socket[appid].Send(buffer, len, SocketFlags.None);
                            state = true;
                        }
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("推送异常：" + err.Message, true);
                }
                if (state == false)
                {
                    sql += " or uid=" + uid;
                }
            }
            if (sql.Length > 0)
            {
                sql = sql.Remove(0, 3);
                IosSend iosSend = new IosSend(sql, offlinecontent, mtype, appid);
                MediaService.iosSendMessage.Add(iosSend);
            }
            MediaService.WriteLog("推送状态  uidcount=" + uidlist.Count + " appid=" + appid, MediaService.wirtelog);
            return sql;
        }
        #endregion

        #region 服务器推送信息至用户
        public static bool SendToUser(byte[] buffer, string onlinecontent, string offlinecontent, int recvuid, int mtype, int index, short comm, int appid)
        {
            int len = 0;
            if (buffer == null)
            {
                byte[] cbyte = Encoding.UTF8.GetBytes(onlinecontent);
                len = cbyte.Length + 8;
                buffer = new byte[len];
                Buffer.BlockCopy(cbyte, 0, buffer, 8, cbyte.Length);
            }
            else
            {
                len = Encoding.UTF8.GetBytes(onlinecontent, 0, onlinecontent.Length, buffer, 8) + 8;
            }
            Buffer.BlockCopy(System.BitConverter.GetBytes((short)len), 0, buffer, 0, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(comm), 0, buffer, 2, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(index), 0, buffer, 4, 4);
            bool state = false;
            try
            {
                UserObject uo = null;
                if (MediaService.userDic.TryGetValue(recvuid, out uo))
                {
                    if (uo.socket != null && uo.socket[appid] != null)
                    {
                        uo.socket[appid].Send(buffer, 0, len, SocketFlags.None);
                        state = true;
                    }
                }
                if (state == false)
                {
                    IosSend iosSend = new IosSend("uid = " + recvuid, offlinecontent, mtype, appid);
                    MediaService.iosSendMessage.Add(iosSend);
                    state = true;
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("命令：执行发送IPHONE推送异常：" + err.ToString(), false);
            }
            return state;
        }
        #endregion

        #region 服务器推送信息至在线用户
        public static string SendToOnlineUserList(byte[] buffer, string onlinecontent, string offlinecontent, List<int> uidlist, int mtype, int index, short comm, int appid)
        {
            StringBuilder sb = new StringBuilder();
            int len = 0;
            if (buffer == null)
            {
                byte[] cbyte = Encoding.UTF8.GetBytes(onlinecontent);
                len = cbyte.Length + 8;
                buffer = new byte[len];
                Buffer.BlockCopy(cbyte, 0, buffer, 8, cbyte.Length);
            }
            else
            {
                len = Encoding.UTF8.GetBytes(onlinecontent, 0, onlinecontent.Length, buffer, 8) + 8;
            }
            Buffer.BlockCopy(System.BitConverter.GetBytes((short)len), 0, buffer, 0, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(comm), 0, buffer, 2, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(index), 0, buffer, 4, 4);
            foreach (int uid in uidlist)
            {
                try
                {
                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        if (uo.socket != null && uo.socket[appid] != null)
                        {
                            uo.socket[appid].Send(buffer, len, SocketFlags.None);
                            sb.Append(",{\"uid\":" + uid + "}");
                        }
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("推送异常：" + err.Message, true);
                }
            }
            if (sb.Length > 0) sb.Remove(0, 1);
            sb.Insert(0, "{\"status\":true,\"list\":[");
            sb.Append("]}");
            return sb.ToString();
        }
        #endregion

        #region 主动推送至所有用户
        public static bool SendToAllUser(byte[] buffer, string online, string offline, int mtype, int index, short comm, int appid)
        {
            int len = 0;
            if (buffer == null)
            {
                byte[] cbyte = Encoding.UTF8.GetBytes(online);
                len = cbyte.Length + 8;
                buffer = new byte[len];
                Buffer.BlockCopy(cbyte, 0, buffer, 8, cbyte.Length);
            }
            else
            {
                len = Encoding.UTF8.GetBytes(online, 0, online.Length, buffer, 8) + 8;
            }
            Buffer.BlockCopy(System.BitConverter.GetBytes((short)len), 0, buffer, 0, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(comm), 0, buffer, 2, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(index), 0, buffer, 4, 4);

            List<string> uidlist = new List<string>();
            DataTable dt = SqlHelper.ExecuteTable("select uid from [app_ios_token] where appid=" + appid);
            foreach (DataRow dr in dt.Rows)
            {
                uidlist.Add(dr[0].ToString());
            }
            foreach (var item in MediaService.userDic)
            {
                bool state = false;
                try
                {
                    if (item.Value.socket != null && item.Value.socket[appid] != null)
                    {
                        item.Value.socket[appid].Send(buffer);
                        state = true;
                        uidlist.Remove(item.Key.ToString());
                    }
                    MediaService.WriteLog("推送状态：state=" + state + "  uid=" + item.Key + " appid=" + appid, MediaService.wirtelog);
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("推送异常：  uid=" + item.Key + " appid=" + appid + "    error:" + err.Message, true);
                }
            }
            MediaService.WriteLog("推送ios：", true);
            if (uidlist.Count > 0)
            {
                string sql = "uid=" + uidlist[0];
                for (int i = 1; i < uidlist.Count; i++)
                {
                    sql += " or uid=" + uidlist[i];
                }
                SqlHelper.ExecuteTable("select uid from [app_ios_token] where appid=" + appid + " and (" + sql + ")");
                IosSend iosSend = new IosSend(sql, offline, mtype, appid);
                MediaService.iosSendMessage.Add(iosSend);
            }
            return true;
        }
        #endregion

        #region 主动推送至所有在线用户
        public static string SendToAllOnlineUser(byte[] buffer, string online, string offline, int mtype, int index, short comm, int appid)
        {
            StringBuilder sb = new StringBuilder();
            int len = 0;
            if (buffer == null)
            {
                byte[] cbyte = Encoding.UTF8.GetBytes(online);
                len = cbyte.Length + 8;
                buffer = new byte[len];
                Buffer.BlockCopy(cbyte, 0, buffer, 8, cbyte.Length);
            }
            else
            {
                len = Encoding.UTF8.GetBytes(online, 0, online.Length, buffer, 8) + 8;
            }
            Buffer.BlockCopy(System.BitConverter.GetBytes((short)len), 0, buffer, 0, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(comm), 0, buffer, 2, 2);
            Buffer.BlockCopy(System.BitConverter.GetBytes(index), 0, buffer, 4, 4);


            foreach (var item in MediaService.userDic)
            {
                try
                {
                    if (item.Value.socket != null && item.Value.socket[appid] != null)
                    {
                        item.Value.socket[appid].Send(buffer);
                        sb.Append(",{\"uid\":" + item.Key + "}");
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("推送异常：  uid=" + item.Key + " appid=" + appid + "    error:" + err.Message, true);
                }
            }
            if (sb.Length > 0) sb.Remove(0, 1);
            sb.Insert(0, "{\"status\":true,\"list\":[");
            sb.Append("]}");
            return sb.ToString();
        }
        #endregion

        //群相关公共方法

        #region 设置默认对讲组
        public static void SetDefaultDuiJiang(int tid, int uid)
        {
            if (tid != 0)
            {
                TalkMessage talkmessage = null;
                object obj = SqlHelper.ExecuteScalar("select tid from [wy_talkuser] where uid=" + uid + " and duijiang=1");
                if (obj != null)
                {
                    int nowtid = Int32.Parse(obj.ToString());
                    SqlHelper.ExecuteScalar("update [wy_talkuser] set duijiang=0 where uid=" + uid + " and duijiang=1");
                    if (MediaService.talkDic.TryGetValue(nowtid, out talkmessage))
                    {
                        talkmessage.uidlist.Remove(uid);
                    }
                }
                SqlHelper.ExecuteScalar("update [wy_talkuser] set duijiang=1 where uid=" + uid + " and tid=" + tid);
                if (MediaService.talkDic.TryGetValue(tid, out talkmessage))
                {
                    if (talkmessage.uidlist.Contains(uid) == false)
                        talkmessage.uidlist.Add(uid);
                }
            }
        }
        #endregion

        #region 获取用户所在的组
        public static string GetMyTalkList(int uid, int minitid)
        {
            StringBuilder sb = new StringBuilder();
            string sql = "";
            if (minitid == 0)
            {
                sql = "select T1.id,T1.tid,T1.xuhao,T1.duijiang,T2.talkname,T2.auth,T2.createuid from (select top 20 id,tid,xuhao,duijiang from [wy_talkuser] where uid = " + uid + " order by id desc) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid order by T1.id desc";
            }
            else
            {
                sql = "select T1.id,T1.tid,T1.xuhao,T1.duijiang,T2.talkname,T2.auth,T2.createuid from (select top 20 id,tid,xuhao,duijiang from [wy_talkuser] where uid = " + uid + " and id<" + minitid + " order by id desc) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid order by T1.id desc";
            }
            DataTable dt = SqlHelper.ExecuteTable(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string ztid = dt.Rows[i]["tid"].ToString();
                string talkname = dt.Rows[i]["talkname"].ToString();
                string xuhao = dt.Rows[i]["xuhao"].ToString();
                string dj = dt.Rows[i]["duijiang"].ToString();
                string auth = dt.Rows[i]["auth"].ToString();
                string createuid = dt.Rows[i]["createuid"].ToString();
                sb.Append(",{\"tid\":" + ztid + ",\"talkname\":\"" + talkname + "\",\"xuhao\":" + xuhao + ",\"auth\":\"" + auth + "\",\"dj\":" + dj + ",\"createuid\":" + createuid + "}");
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
        #endregion

        #region 创建对讲组
        public static string CreateTalk(int uid, NameValueCollection qs, int type = 0)
        {
            string recv = "";
            object obj = SqlHelper.ExecuteScalar("select count(tid) from [wy_talk] where createuid=" + uid);
            if (obj != null)
            {
                int quncount = Int32.Parse(obj.ToString());
                if (quncount < MediaService.talk_count + 100)
                {
                    Random ran = new Random((int)DateTime.Now.Ticks);
                    string qunname = null;
                    for (int k = 0; k < 10; k++)
                    {
                        int qhao = ran.Next(100, 100000);
                        byte[] b = System.BitConverter.GetBytes(qhao);
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
                                break;
                            }
                        }
                        if (c == 2 || x == 3)
                            continue;
                        else
                        {
                            qunname = qhao.ToString().PadLeft(5, '0');
                            obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + qunname + "'");
                            if (obj == null)
                            {
                                break;
                            }
                            else
                            {
                                qunname = null;
                            }
                        }
                    }
                    if (qunname != null)
                    {
                        string talkname = qunname;
                        string info = qs["info"] == null ? "" : qs["info"].Replace("'", "");
                        string auth = qs["auth"] == null ? "" : qs["auth"].Replace("'", "");//ran.Next(100, 1000).ToString() : qs["auth"].Replace("'", "");
                        string talknotice = qs["talknotice"] == null ? "" : qs["talknotice"].Replace("'", "");
                        try
                        {
                            obj = SqlHelper.ExecuteScalar("insert [wy_talk] (talkname,auth,createuid,info,talknotice,type) values ('" + talkname + "','" + auth + "','" + uid + "','" + info + "','" + talknotice + "','" + type + "');select scope_identity()");
                            if (obj != null)
                            {
                                string tid = obj.ToString();
                                SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid,xuhao) values (" + tid + "," + uid + ",'1')");
                                recv = "{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\"}";
                            }
                            else
                            {
                                recv = HttpService.WriteErrorJson("创建组写入失败，请稍后再试！");
                            }
                        }
                        catch (Exception err)
                        {
                            MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                            recv = HttpService.WriteErrorJson("创建组操作失败，请稍后再试！");
                        }
                    }
                    else
                    {
                        recv = HttpService.WriteErrorJson("创建组ID失败，请稍后再试！");
                    }
                }
                else
                {
                    recv = HttpService.WriteErrorJson("您创建的组超过最大数，请解散无用组再创建！");
                }
            }
            else
            {
                recv = HttpService.WriteErrorJson("创建组失败，请稍后再试！");
            }
            return recv;
        }
        #endregion

        #region 查询频道类型
        public static int FindTalkType(int tid)
        {
            object type = SqlHelper.ExecuteScalar("select top(1) type from [wy_talk] where tid='" + tid + "'");
            if (type != null)
            {
                return Convert.ToInt32(type);
            }
            else
            {
                return -1;
            }
        }
        public static int FindTalkType(string talkname)
        {
            object type = SqlHelper.ExecuteScalar("select top(1) type from [wy_talk] where talkname='" + talkname + "'");
            if (type != null)
            {
                return Convert.ToInt32(type);
            }
            else
            {
                return -1;
            }
        }
        #endregion

        #region 用户加入组
        public static string JoinTalk(int uid, string auth, string talkname)
        {
            DataTable dt = SqlHelper.ExecuteTable("select tid,auth from [wy_talk] where talkname='" + talkname + "'");
            if (dt.Rows.Count > 0)
            {
                string tid = dt.Rows[0]["tid"].ToString();
                if (dt.Rows[0]["auth"].ToString() == "" || dt.Rows[0]["auth"].ToString() == auth)
                {
                    object obj = SqlHelper.ExecuteScalar("select id from [wy_talkuser] where tid=" + tid + " and uid=" + uid);
                    if (obj == null)
                        SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid) values (" + tid + "," + uid + ");update [wy_talk] set usernum=usernum+1 where tid=" + tid);
                    return "{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"xuhao\":\"1\"}";
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
                return CommBusiness.WriteErrorJson(34, "没有找到要加入的群组");
            }
        }

        //限制不能加入type为3和4的频道
        public static string JoinTalkLimit(int uid, string auth, string talkname)
        {
            DataTable dt = SqlHelper.ExecuteTable("select tid,auth,type from [wy_talk] where talkname='" + talkname + "'");
            if (dt.Rows.Count > 0)
            {
                if (dt.Rows[0]["type"].ToString() == "3")
                {
                    return CommBusiness.WriteErrorJson(35, "不允许加入");
                }
                string tid = dt.Rows[0]["tid"].ToString();
                if (dt.Rows[0]["auth"].ToString() == "" || dt.Rows[0]["auth"].ToString() == auth)
                {
                    object obj = SqlHelper.ExecuteScalar("select id from [wy_talkuser] where tid=" + tid + " and uid=" + uid);
                    if (obj == null)
                        SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid) values (" + tid + "," + uid + ");update [wy_talk] set usernum=usernum+1 where tid=" + tid);
                    return "{\"status\":true,\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"xuhao\":\"1\"}";
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
                return CommBusiness.WriteErrorJson(34, "没有找到要加入的群组");
            }
        }
        #endregion

        #region 用户退出组
        public static string ExitTalk(int tid, int uid)
        {
            if (tid != 0)
            {
                int state = SqlHelper.ExecuteNonQuery("delete [wy_talkuser] where tid=" + tid + " and uid=" + uid);
                if (state > 0)
                {
                    SqlHelper.ExecuteNonQuery("update [wy_talk] set usernum=usernum-1 where tid=" + tid);
                    CommBusiness.UpdateTalkUser(tid, uid, false);
                }
            }
            return "{\"status\":true}";
        }
        #endregion

        #region 创建者解散组
        public static string DeleteTalk(int tid, int uid)
        {
            if (tid != 0)
            {
                int state = SqlHelper.ExecuteNonQuery("delete [wy_talk] where tid=" + tid + " and createuid=" + uid);
                if (state > 0)
                {
                    SqlHelper.ExecuteNonQuery("delete [wy_talkuser] where tid=" + tid);
                    TalkMessage talkmessage = null;
                    MediaService.talkDic.TryRemove(tid, out talkmessage);
                }
            }
            return "{\"status\":true}";
        }
        #endregion

        //好友通讯录

        #region 加好友
        public static bool AddMyFriend(int uid, int fuid, string nickname)
        {
            bool isSuccess = false;
            try
            {
                object obj = SqlHelper.ExecuteScalar("select id from [wy_userrelation] where uid=" + uid + " and fuid=" + fuid);
                if (obj == null)
                {
                    int ouid;
                    MediaService.mapDic.TryGetValue(uid, out ouid);
                    string sql = string.Format("insert [wy_userrelation] (uid,fuid,nickname,ouid) values ({0},{1},'{2}',{3})", uid.ToString(), fuid.ToString(), nickname, ouid.ToString());
                    SqlHelper.ExecuteNonQuery(sql);
                    isSuccess = true;
                }
                else
                {
                    isSuccess = true;
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                isSuccess = false;
            }
            return isSuccess;
        }
        #endregion


        #region 根据uid查询ouid
        public static int QueryOuidByUid(int uid)
        {
            //查询设备是否存在
            string strSql = "SELECT ouid FROM wy_uidmap WHERE uid=" + uid;
            object sqlResult = SqlHelper.ExecuteScalar(strSql);
            if (sqlResult == null)
                return 0;
            else
                return Convert.ToInt32(sqlResult);
        }
        #endregion
        //公共方法

        #region 根据uid查询glsn
        public static string QueryGLSNByUid(int uid)
        {
            //查询设备是否存在
            string strSql = "SELECT glsn FROM app_users WHERE uid=" + uid;
            object sqlResult = SqlHelper.ExecuteScalar(strSql);
            if (sqlResult == null)
                return string.Empty;
            else
                return sqlResult.ToString();
        }
        #endregion

        #region 根据GLSN判断goloz类型
        public static GoloProduct GetGoloProduct(string glsn)
        {
            if (glsn.Length != 12)
                return GoloProduct.GoloX;
            if (!glsn.StartsWith("9716"))
                return GoloProduct.Golo6;
            else if (!glsn.StartsWith("9711"))
                return GoloProduct.Golo6;
            string zsn = glsn.Substring(glsn.Length - 8);
            int z = 0;
            int.TryParse(zsn, out z);
            if (z == 0)
                return GoloProduct.GoloX;
            if (91000000 > z && z > 90000000)
                return GoloProduct.GoloZ1;
            if (z > 91000000)
                return GoloProduct.GoloZN;
            return GoloProduct.GoloX;
        }
        public static GoloProduct GetGoloProduct(int pre, int zsn)
        {
            if (pre != 9716)
                return GoloProduct.Golo6;
            if (zsn == 0)
                return GoloProduct.GoloX;
            if (91000000 > zsn && zsn > 90000000)
                return GoloProduct.GoloZ1;
            if (zsn > 91000000)
                return GoloProduct.GoloZN;
            return GoloProduct.GoloX;
        }
        #endregion
    }

    [Flags]
    public enum GoloProduct
    {
        /// <summary>
        /// 未知golo
        /// </summary>
        GoloX = 0x00,
        /// <summary>
        /// golo6
        /// </summary>
        Golo6 = 0x01,
        /// <summary>
        /// Vog
        /// </summary>
        GoloVog = 0x02,
        /// <summary>
        /// z1
        /// </summary>
        GoloZ1 = 0x04,
        /// <summary>
        /// z2
        /// </summary>
        GoloZ2 = 0x08,
        /// <summary>
        /// z3
        /// </summary>
        GoloZ3 = 0x16,
        /// <summary>
        /// 新goloz
        /// </summary>
        GoloZN = GoloZ2 | GoloZ3,
    }

    public class TalkMessage
    {
        public long micticks = 0;
        public int micuid = 0;
        public string talkname = "";
        public int createuid = 0;
        public string zsn = "";
        public List<int> uidlist = null;

        public TalkMessage(List<int> _uidList, int _createuid, string _talkname, string _zsn)
        {
            uidlist = _uidList;
            talkname = _talkname;
            createuid = _createuid;
            zsn = _zsn;
        }

        public void AddUid(int uid)
        {
            uidlist.Add(uid);
        }
        public void SetMicUid(int uid)
        {
            micuid = uid;
        }
        public void SetTicks(int ticks)
        {
            micticks = ticks;
        }
    }

    public class RadioObject
    {
        public string channelname = "";
        public string cityname = "";
        public int areaid = 0;
        public int sendtype = 0; // 0:允许所有人发言，-1：禁止发言，1：允许一些人发言，2：禁止一些人发言
        public int[] senduid = null;
        public string audiourl = "";
        public string uploadurl = "";
        public int channelde = 0;
        public int radiotype = 0;
        public string imageurl = "";
        public string thumburl = "";
        public int prid = 0;
        public string areacode = "";
        public string flashimageurl = "";   //特效图地址

        public RadioObject()
        { }

        public RadioObject(string _channelname, int _area, int _sendtype, int[] _senduid, string _audiourl, string _uploadurl, int _channelde, int _radiotype, string _cityname, string _imageurl, string _thumburl, int _prid, string _areacode, string _flashimageurl)
        {
            channelname = _channelname;
            cityname = _cityname;
            areaid = _area;
            senduid = _senduid;
            audiourl = _audiourl;
            uploadurl = _uploadurl;
            channelde = _channelde;
            radiotype = _radiotype;
            imageurl = _imageurl;
            thumburl = _thumburl;
            prid = _prid;
            areacode = _areacode;
            flashimageurl = _flashimageurl;
        }
    }

    public class FujinTalk
    {
        public double lo { get; set; }//经度
        public double la { get; set; }//纬度
        public List<int> uidlist { get; set; }//车队用户列表
    }
}
