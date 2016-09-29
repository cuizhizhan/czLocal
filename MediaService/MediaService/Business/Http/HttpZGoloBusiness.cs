﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net.Sockets;
using System.Threading;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Newtonsoft.Json;
using System.Configuration;
using System.Dynamic;
namespace MediaService
{
    public static class HttpZGoloBusiness
    {
        #region 通讯录

        #region 获取用户通讯录
        /// <summary>
        /// 获取用户通讯录
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string GetUserContact(NameValueCollection qs)
        {
            #region uid
            /* NameValueCollection 值列表
             * ouid,token,uid,appid
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["uid"] == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到获取用户通讯录 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到获取用户通讯录 ：ouid =" + qs["ouid"].ToString() + " uid =" + qs["uid"].ToString() + " token=" + qs["token"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                string strSql = @"SELECT users.uid as uid ,relation.fuid as fuid,users.glsn as sn ,relation.nickname as nickname,users.mobile as mobile,relation.state as state,users.updatetime as updatetime
                                    from app_users as users, (SELECT wy_userrelation.fuid as fuid,wy_userrelation.nickname as nickname,wy_userrelation.state as state 
                                    from wy_userrelation WHERE wy_userrelation.uid = '" + uid +
                                        "' ) as relation WHERE users. uid = relation.fuid";
                //根据用户 uid 查询跟该用户相关的通讯信息
                DataTable dtContacts = SqlHelper.ExecuteTable(strSql);
                string subrecv = "";
                foreach (DataRow dr in dtContacts.Rows)
                {
                    string fuid = dr["fuid"].ToString();
                    string sn = CommFunc.GetUniform8(dr["sn"].ToString());
                    string nickname = dr["nickname"].ToString();
                    string state = dr["state"].ToString();
                    string mobile = dr["mobile"].ToString();
                    string updatetime = CommFunc.ConvertDateTimeInt(dr["updatetime"].ToString()).ToString();
                    subrecv += (subrecv == "" ? "" : ",") + "{\"fuid\":" + fuid + ",\"sn\": \"" + sn + "\",\"nickname\": \"" + nickname + "\",\"state\":" + state + ",\"updatetime\":\"" + updatetime + "\"}";
                }

                return CommFunc.StandardListFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }

        /// <summary>
        /// 新-获取用户通讯录
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string NewGetUserContact(NameValueCollection qs)
        {
            #region qs
            /* NameValueCollection 值列表
             * ouid,token,appid,[updatetime]
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到NewGetUserContact ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到NewGetUserContact ：ouid =" + qs["ouid"] + " token=" + qs["token"] + "appid =" + qs["appid"], MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                //根据用户 ouid 查询跟该用户相关的通讯信息
                string strSql =
                    string.Format(@"SELECT F.fuid,A.glsn as sn,F.nickname,F.[state],F.updatetime,U.face_url
                    FROM wy_userrelation F INNER JOIN app_users A ON F.ouid={0} AND F.[fuid]=A.[uid]
                    LEFT JOIN wy_uidmap M ON F.fuid=M.[uid]
					LEFT JOIN wy_user U ON U.[user_id]=M.ouid", ouid.ToString());
                strSql += " ORDER BY updatetime DESC";
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

                    string sn = CommFunc.GetUniform8(dr["sn"].ToString());
                    string nickname = dr["nickname"].ToString();
                    string state = dr["state"].ToString();
                    string updatetime = CommFunc.ConvertDateTimeInt(dr["updatetime"].ToString()).ToString();
                    string face_url = dr["face_url"] != null ? dr["face_url"].ToString() : "";
                    subrecv += (subrecv == "" ? "" : ",") + "{\"fuid\":" + fuid + ",\"sn\": \"" + sn + "\",\"nickname\": \"" + nickname + "\",\"state\":" + state + ",\"updatetime\":\"" + updatetime + "\",\"face_url\":\"" + face_url + "\"}";
                }

                return CommFunc.StandardListFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
        #endregion

        #region 更新用户通讯录
        /// <summary>
        /// 更新用户通讯录
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string UpdateUserContact(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,uid,appid,fuid,[nickname],[state]?
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["uid"] == null || qs["fuid"] == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
                return recv;
            try
            {
                MediaService.WriteLog("接收到更新用户通讯录 ：ouid =" + qs["ouid"].ToString() + " uid =" + qs["uid"].ToString() + " token=" + qs["token"].ToString() + " fuid=" + qs["fuid"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                int fuid = 0;
                if (!Int32.TryParse(qs["fuid"].ToString(), out fuid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                //构造Sql语句
                StringBuilder sqlstr = new StringBuilder();
                sqlstr.Append("update wy_userrelation set ");
                List<string> conditions = new List<string>();
                conditions.Add("updatetime = getdate() ");
                if (qs.AllKeys.Contains("state"))
                    conditions.Add("state =" + Int32.Parse(qs["state"].ToString()));
                if (qs.AllKeys.Contains("nickname"))
                    conditions.Add(" nickname = '" + qs["nickname"].ToString().Replace("'", "") + "' ");
                for (int i = 0; i < conditions.Count; i++)
                {
                    sqlstr.Append(conditions[i]);
                    if (i != conditions.Count - 1)
                        sqlstr.Append(" , ");
                }
                sqlstr.Append(" where uid =" + uid + " and fuid =" + fuid);

                sqlstr.Append(";UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid);
                //加上当前用户更新时间

                int count = SqlHelper.ExecuteNonQuery(sqlstr.ToString());
                if (count > 0)
                {
                    string subrecv = GetUserRelation(uid, fuid);
                    if (string.IsNullOrWhiteSpace(subrecv))
                        return CommFunc.StandardFormat(MessageCode.UpdateFaild);

                    recv = "{\"status\":true,\"list\":[" + subrecv + "]}";
                    PublicClass.SendToOnlineUserList(null, recv, "", new List<int>() { uid }, 99, 0, CommType.updateUserContact, CommFunc.APPID);

                    return CommFunc.StandardFormat(MessageCode.Success);
                }
                else
                    return CommFunc.StandardFormat(MessageCode.UpdateFaild);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        private static string GetUserRelation(int uid, int fuid)
        {
            string strSql = @"SELECT relation.fuid as fuid,users.glsn as sn ,relation.nickname as nickname,users.mobile as mobile,relation.state as state,users.updatetime as updatetime
                                    from app_users as users, (SELECT wy_userrelation.fuid as fuid,wy_userrelation.nickname as nickname,wy_userrelation.state as state 
                                    from wy_userrelation WHERE wy_userrelation.uid = '" + uid +
                                    "' and wy_userrelation.fuid ='" + fuid + "' ) as relation WHERE users. uid = relation.fuid";
            //根据用户 uid 查询跟该用户相关的通讯信息
            DataTable dtContacts = SqlHelper.ExecuteTable(strSql);
            string subrecv = "";
            foreach (DataRow dr in dtContacts.Rows)
            {
                string sn = dr["sn"].ToString();
                string nickname = dr["nickname"].ToString();
                string state = dr["state"].ToString();
                string mobile = dr["mobile"].ToString();
                string updatetime = CommFunc.ConvertDateTimeInt(dr["updatetime"].ToString()).ToString();
                subrecv += (subrecv == "" ? "" : ",") + "{\"fuid\":" + fuid + ",\"sn\": \"" + sn + "\",\"nickname\": \"" + nickname + "\",\"state\":" + state + ",\"updatetime\":\"" + updatetime + "\"}";
            }
            return subrecv;
        }

        /// <summary>
        /// 新-更新用户通讯录
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string NewUpdateUserContact(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,appid,fuid,[nickname],[state]
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["fuid"] == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
                return recv;
            try
            {
                MediaService.WriteLog("NewUpdateUserContact ：ouid =" + qs["ouid"] + " token=" + qs["token"] + " fuid=" + qs["fuid"] + "appid =" + qs["appid"], MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                int fuid;
                if (!Int32.TryParse(qs["fuid"], out fuid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                //构造Sql语句
                StringBuilder sqlstr = new StringBuilder();
                sqlstr.Append("update wy_userrelation set ");
                List<string> conditions = new List<string>();
                conditions.Add("updatetime = getdate() ");
                if (qs.AllKeys.Contains("state"))
                    conditions.Add("state =" + Int32.Parse(qs["state"]));
                if (qs.AllKeys.Contains("nickname"))
                    conditions.Add(" nickname = '" + qs["nickname"].Replace("'", "") + "' ");
                for (int i = 0; i < conditions.Count; i++)
                {
                    sqlstr.Append(conditions[i]);
                    if (i != conditions.Count - 1)
                        sqlstr.Append(" , ");
                }
                sqlstr.Append(" where ouid =" + ouid + " and fuid =" + fuid);

                sqlstr.Append(";UPDATE wy_user SET relationtime=" + Utility.GetTimeStamp() + " WHERE [user_id]=" + ouid);
                //加上当前用户更新时间

                int count = SqlHelper.ExecuteNonQuery(sqlstr.ToString());
                if (count > 0)
                {
                    List<int> uidlist = new List<int>();
                    string sql = "SELECT [uid] FROM wy_uidmap WHERE ouid=" + ouid;
                    foreach (DataRow row in SqlHelper.ExecuteTable(sql).Rows)
                    {
                        int uid;
                        int.TryParse(row["uid"].ToString(), out uid);
                        if (uid > 0)
                        {
                            uidlist.Add(uid);
                        }
                    }
                    if (uidlist.Count > 0)
                    {
                        string subrecv = NewGetUserRelation(ouid, fuid);
                        if (subrecv.Length > 0)
                        {
                            recv = "{\"status\":true,\"list\":[" + subrecv + "]}";
                            PublicClass.SendToOnlineUserList(null, recv, "", uidlist, 99, 0, CommType.updateUserContact, CommFunc.APPID);
                        }
                    }
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
                else
                {
                    return CommFunc.StandardFormat(MessageCode.UpdateFaild);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("NewUpdateUserContact 出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        /// <summary>
        /// 新-获取用户关系信息
        /// </summary>
        /// <param name="ouid"></param>
        /// <param name="fuid"></param>
        /// <returns></returns>
        private static string NewGetUserRelation(int ouid, int fuid)
        {
            string strSql = "SELECT A.glsn AS sn,R.nickname,[state],R.updatetime FROM wy_userrelation R INNER JOIN app_users A ON R.ouid=" + ouid + " AND R.fuid=" + fuid + " AND R.fuid=A.[uid]";
            //根据用户 ouid 查询跟该用户相关的通讯信息
            DataTable dtContacts = SqlHelper.ExecuteTable(strSql);
            string subrecv = "";
            foreach (DataRow dr in dtContacts.Rows)
            {
                string sn = dr["sn"].ToString();
                string nickname = dr["nickname"].ToString();
                string state = dr["state"].ToString();
                string updatetime = CommFunc.ConvertDateTimeInt(dr["updatetime"].ToString()).ToString();
                subrecv += (subrecv == "" ? "" : ",") + "{\"fuid\":" + fuid + ",\"sn\": \"" + sn + "\",\"nickname\": \"" + nickname + "\",\"state\":" + state + ",\"updatetime\":\"" + updatetime + "\"}";
            }
            return subrecv;
        }
        #endregion

        #region 删除用户通讯录
        /// <summary>
        /// 删除用户通讯录
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string DeleteUserContact(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,uid,fuid,appid
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["uid"] == null || qs["fuid"] == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
                return recv;
            try
            {
                MediaService.WriteLog("接收到删除用户通讯录 ：ouid =" + qs["ouid"].ToString() + " uid =" + qs["uid"].ToString() + " token=" + qs["token"].ToString() + " fuid=" + qs["fuid"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                int fuid = 0;
                if (!Int32.TryParse(qs["fuid"].ToString(), out fuid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                MediaService.WriteLog("接收到删除用户通讯录 ：uid=" + uid + ",fuid =" + fuid, MediaService.wirtelog);
                StringBuilder strSql = new StringBuilder();
                strSql.Append("delete from [wy_userrelation] where uid =" + uid + " and fuid =" + fuid);
                strSql.Append(";UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid);
                int count = SqlHelper.ExecuteNonQuery(strSql.ToString());
                if (count > 0)
                {
                    recv = "{\"status\":true,\"fuid\":\"" + fuid + "\"}";
                    PublicClass.SendToOnlineUserList(null, recv, "", new List<int>() { uid }, 99, 0, CommType.deleteUserContact, CommFunc.APPID);
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
                else
                    return CommFunc.StandardFormat(MessageCode.DeleteFaild);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        /// <summary>
        /// 新-删除用户通讯录
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string NewDeleteUserContact(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,fuid,appid
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["fuid"] == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
                return recv;
            try
            {
                MediaService.WriteLog("NewDeleteUserContact ：ouid =" + qs["ouid"] + " token=" + qs["token"] + " fuid=" + qs["fuid"] + "appid =" + qs["appid"], MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                int fuid;
                if (!Int32.TryParse(qs["fuid"], out fuid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                string sql = "SELECT [uid] FROM wy_uidmap WHERE ouid=" + ouid;
                List<int> uidlist = new List<int>();
                foreach (DataRow row in SqlHelper.ExecuteTable(sql).Rows)
                {
                    int uid;
                    int.TryParse(row["uid"].ToString(), out uid);
                    if (uid > 0)
                    {
                        uidlist.Add(uid);
                    }
                }
                StringBuilder strSql = new StringBuilder();
                strSql.Append("delete from [wy_userrelation] where ouid =" + ouid + " and fuid =" + fuid);
                strSql.Append(";UPDATE wy_user SET relationtime=" + Utility.GetTimeStamp() + " WHERE [user_id]=" + ouid);
                int count = SqlHelper.ExecuteNonQuery(strSql.ToString());
                if (count > 0)
                {
                    recv = "{\"status\":true,\"fuid\":\"" + fuid + "\"}";
                    PublicClass.SendToOnlineUserList(null, recv, "", uidlist, 99, 0, CommType.deleteUserContact, CommFunc.APPID);
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
                return CommFunc.StandardFormat(MessageCode.DeleteFaild);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 新增用户通讯录
        /// <summary>
        /// 新增用户通讯录
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string AddUserContact(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,uid,appid,sn,nickname
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["sn"] == null)
            {
                MediaService.WriteLog("接收到新增用户通讯录 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到新增用户通讯录 ：ouid =" + qs["ouid"].ToString() + "token =" + qs["token"].ToString() + "appid =" + qs["appid"].ToString() + "uid =" + qs["uid"].ToString() + "sn =" + qs["sn"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                string sn = CommFunc.GetUniform12(qs["sn"].ToString());

                //查询设备是否存在
                string strSql = "SELECT uid FROM app_users WHERE glsn = '" + sn + "'";
                object sqlResult = SqlHelper.ExecuteScalar(strSql);
                if (sqlResult == null)
                    return CommFunc.StandardFormat(MessageCode.DeviceNotExist);
                int fuid = (int)sqlResult;

                strSql = "select count(*) from wy_userrelation where uid =" + uid + " and fuid =" + fuid;
                object result = SqlHelper.ExecuteScalar(strSql);
                if (result != null && ((int)result > 0))
                    return CommFunc.StandardFormat(MessageCode.ContactExist);
                string nickname = qs["nickname"] == null ? "" : qs["nickname"].ToString().Replace("'", "");
                //构造Sql语句
                strSql = @"INSERT into wy_userrelation(uid,fuid,nickname,state,ouid) VALUES(" + uid + "," + fuid + ",'" + nickname + "',0," + ouid + ")";
                strSql += ";UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid;
                int count = SqlHelper.ExecuteNonQuery(strSql);
                if (count > 0)
                {
                    string subrecv = GetUserRelation(uid, fuid);
                    if (string.IsNullOrWhiteSpace(subrecv))
                        return CommFunc.StandardFormat(MessageCode.InsertFaild);

                    recv = "{\"status\":true,\"list\":[" + subrecv + "]}";
                    PublicClass.SendToOnlineUserList(null, recv, "", new List<int>() { uid }, 99, 0, CommType.addUserContact, CommFunc.APPID);

                    string data = "{\"fuid\":" + fuid + "}";
                    return CommFunc.StandardObjectFormat(MessageCode.Success, data);
                }
                else
                    return CommFunc.StandardFormat(MessageCode.InsertFaild);

            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        /// <summary>
        /// 新-新增用户通讯录
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string NewAddUserContact(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,appid,sn,nickname,[isselfdevice]
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["sn"] == null)
            {
                MediaService.WriteLog("接收到新增用户通讯录 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到新增用户通讯录 ：ouid=" + qs["ouid"] + " token=" + qs["token"] + " appid=" + qs["appid"] + " sn=" + qs["sn"], MediaService.wirtelog);

                int isselfdevice = 0; //通讯录所属设备是否是自己绑定的设备 0、其他人设备的通讯录，默认为0；1、自己设备的通讯录 
                if (qs["isselfdevice"] != null)
                {
                    if (!Int32.TryParse(qs["isselfdevice"].ToString(), out isselfdevice))
                        return CommFunc.StandardFormat(MessageCode.FormatError);
                }
                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                string sn = CommFunc.GetUniform12(qs["sn"]);

                //查询设备是否存在
                string strSql = "SELECT uid FROM app_users WHERE glsn = '" + sn + "'";
                object sqlResult = SqlHelper.ExecuteScalar(strSql);
                if (sqlResult == null)
                    return CommFunc.StandardFormat(MessageCode.DeviceNotExist);
                int fuid = (int)sqlResult;

                string nickname = qs["nickname"] == null ? "" : qs["nickname"].Replace("'", "");
                if (isselfdevice == 1) //如果是自己的设备
                {
                    strSql = string.Format(@"SELECT COUNT(1) FROM wy_userrelation WHERE ouid={0} AND (fuid={1} OR nickname='{2}') AND isselfdevice=1", ouid.ToString(), fuid.ToString(), nickname);
                }
                else
                {
                    strSql = string.Format(@"SELECT COUNT(1) FROM wy_userrelation WHERE ouid={0} AND (fuid={1} OR nickname='{2}')", ouid.ToString(), fuid.ToString(), nickname);
                }
                
                object result = SqlHelper.ExecuteScalar(strSql);
                if (result != null && int.Parse(result.ToString()) > 0)
                    return CommFunc.StandardFormat(MessageCode.ContactExist);

                strSql = "SELECT [uid] FROM wy_uidmap WHERE ouid=" + ouid;
                List<int> uidlist = new List<int>();
                foreach (DataRow row in SqlHelper.ExecuteTable(strSql).Rows)
                {
                    int uid;
                    int.TryParse(row["uid"].ToString(), out uid);
                    if (uid > 0)
                    {
                        uidlist.Add(uid);
                    }
                }
                //构造Sql语句
                strSql = string.Format("INSERT into wy_userrelation(uid,fuid,nickname,ouid,isselfdevice) VALUES(0,{0},'{1}',{2},{3})", fuid.ToString(), nickname, ouid.ToString(),isselfdevice);
                strSql += ";UPDATE wy_user SET relationtime=" + Utility.GetTimeStamp() + " WHERE [user_id]=" + ouid;
                int count = SqlHelper.ExecuteNonQuery(strSql);
                if (count > 0)
                {
                    string subrecv = NewGetUserRelation(ouid, fuid);
                    if (string.IsNullOrWhiteSpace(subrecv))
                        return CommFunc.StandardFormat(MessageCode.InsertFaild);

                    recv = "{\"status\":true,\"list\":[" + subrecv + "]}";
                    MediaService.WriteLog("新增用户通讯录 推送json：" + recv, MediaService.wirtelog);

                    PublicClass.SendToOnlineUserList(null, recv, "", uidlist, 99, 0, CommType.addUserContact, CommFunc.APPID);

                    int fouid;
                    MediaService.mapDic.TryGetValue(fuid, out fouid);
                    string sql = "SELECT face_url FROM wy_user WHERE [user_id]=" + fouid;
                    string faceUrl = SqlHelper.ExecuteScalar(sql) + "";
                    string data = "{\"fuid\":" + fuid + ",\"face_url\":\"" + faceUrl + "\"}";
                    return CommFunc.StandardObjectFormat(MessageCode.Success, data);
                }
                return CommFunc.StandardFormat(MessageCode.InsertFaild);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion
        #endregion

        #region wifi管理

        #region 获取用户下面的uid
        private static List<int> GetUserUid(int ouid)
        {
            string strSql = "SELECT uid FROM wy_uidmap WHERE ouid=" + ouid;
            DataTable dtContacts = SqlHelper.ExecuteTable(strSql);
            List<int> uids = new List<int>();
            foreach (DataRow dr in dtContacts.Rows)
            {
                string id = dr["uid"].ToString();
                int uid;
                if (Int32.TryParse(id, out uid))
                    uids.Add(uid);
            }
            return uids;
        }
        #endregion

        #region 获取用户wifi列表
        /// <summary>
        /// 获取用户wifi列表
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string GetUserWifis(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * uid,appid,ouid,token
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["token"] == null || qs["ouid"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到获取用户wifi列表 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到获取用户wifi列表 ：ouid =" + qs["ouid"].ToString() + " token=" + qs["token"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                MediaService.WriteLog("接收到获取用户wifi列表 ：ouid=" + ouid, MediaService.wirtelog);
                string strSql = @"SELECT id,name,password,updatetime from wy_userwifi WHERE ouid = " + ouid;
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
                return CommFunc.StandardListFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.MissKey, e.Message);
            }
            #endregion
        }
        #endregion

        #region 更新用户wifi信息
        /// <summary>
        /// 更新用户wifi信息
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string UpdateUserWifi(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,uid,token,id,name,password
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["id"] == null || qs["token"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到更新用户wifi信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到更新用户wSuccessifi信息 ：ouid =" + qs["ouid"].ToString() + "&id =" + qs["id"].ToString() + "&token=" + qs["token"].ToString() + "&appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                int id;
                if (!Int32.TryParse(qs["id"].ToString(), out id))
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                string name = qs["name"] == null ? "" : qs["name"].ToString().Replace("'", "");
                string password = qs["password"] == null ? "" : qs["password"].ToString().Replace("'", "");
                StringBuilder strSql = new StringBuilder();
                //构造Sql语句
                List<int> uids = GetUserUid(ouid);
                strSql.Append(@"update wy_userwifi set name = '" + name + "', password ='" + password + "', updatetime = getdate()  where id =  " + id);
                if (uids.Count > 0)
                {
                    strSql.Append(UpdateUserWifiUpdatetime(uids));
                }
                int count = SqlHelper.ExecuteNonQuery(strSql.ToString());
                if (count > 0)
                {
                    recv = "{\"status\":true,\"id\":" + id + ",\"name\":\"" + name + "\",\"password\":\"" + password + "\"}";
                    uids.ForEach(uid => PublicClass.SendToOnlineUserList(null, recv, "", new List<int>() { uid }, 99, 0, CommType.updateUserWifi, CommFunc.APPID));
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
                else
                    return CommFunc.StandardFormat(MessageCode.UpdateFaild);


            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        private static string UpdateUserWifiUpdatetime(List<int> uids)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(";UPDATE app_users SET wifitime=" + ConvertToLongTime() + " WHERE ");
            uids.ForEach(uid =>
            {
                if (uid == uids.First())
                    sb.Append(" uid =" + uid);
                else
                    sb.Append(" or uid =" + uid);
            });
            sb.Append(";");
            return sb.ToString();
        }

        private static long ConvertToLongTime()
        {
            return DateTime.UtcNow.Ticks / 10000000 - 62135596800;
        }
        #endregion

        #region 删除用户wifi信息
        /// <summary>
        /// 删除用户wifi信息
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string DeleteUserWifi(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,uid,token,id,appid
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["token"] == null || qs["id"] == null || qs["ouid"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到删除用户wifi信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到删除用户wifi信息 ：ouid =" + qs["ouid"].ToString() + " id =" + qs["id"].ToString() + " token=" + qs["token"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                int id;
                if (!Int32.TryParse(qs["id"].ToString(), out id))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                List<int> uids = GetUserUid(ouid);
                StringBuilder strSql = new StringBuilder();
                strSql.Append("delete from [wy_userwifi] where id =" + id);
                if (uids.Count > 0)
                {
                    strSql.Append(UpdateUserWifiUpdatetime(uids));
                }
                int count = SqlHelper.ExecuteNonQuery(strSql.ToString());
                if (count > 0)
                {
                    recv = "{\"status\":true,\"id\":" + id + "}";
                    uids.ForEach(uid => PublicClass.SendToOnlineUserList(null, recv, "", new List<int>() { uid }, 99, 0, CommType.deleteUserWifi, CommFunc.APPID));
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
                else
                    return CommFunc.StandardFormat(MessageCode.DeleteFaild);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 添加用户wifi信息
        /// <summary>
        /// 添加用户wifi信息
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string AddUserWifi(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,name,password,appid
             */
            var log = new StringBuilder("接收到添加用户wifi信息 AddUserWifi ");
            string recv = "";
            if (qs == null || qs["token"] == null || qs["ouid"] == null || qs["name"] == null || qs["password"] == null || qs["appid"] == null)
            {
                recv = CommFunc.StandardFormat(MessageCode.MissKey);
            }
            else
            {
                try
                {
                    log.Append(" ouid =" + qs["ouid"] + " token=" + qs["token"] + " name =" + qs["name"] + " password =" + qs["password"] + " appid =" + qs["appid"]);

                    int ouid = 0;
                    bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                    if (!isVerToken)
                    {
                        log.Append(" result=").Append(recv);
                        MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
                        return recv;
                    }

                    string name = qs["name"] == null ? "" : qs["name"].Replace("'", "");
                    string password = qs["password"] == null ? "" : qs["password"].Replace("'", "");

                    //构造Sql语句
                    string strSql = @"
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
                    int id = SqlHelper.ExecuteScalar(strSql, paras).ToString().ToInt();
                    if (id > 0)
                    {
                        recv = "{\"status\":true,\"id\":" + id + ",\"name\":\"" + name + "\",\"password\":\"" + password + "\"}";
                        List<int> uids = GetUserUid(ouid);
                        if (uids.Count > 0)
                        {
                            strSql = UpdateUserWifiUpdatetime(uids);
                            SqlHelper.ExecuteNonQuery(strSql);
                        }
                        uids.ForEach(uid => PublicClass.SendToOnlineUserList(null, recv, "", new List<int> { uid }, 99, 0, CommType.addUserWifi, CommFunc.APPID));
                        string data = "{\"id\":" + id + "}";
                        recv = CommFunc.StandardObjectFormat(MessageCode.Success, data);
                    }
                    else
                        recv = CommFunc.StandardFormat(MessageCode.InsertFaild, "该wifi已存在");
                }
                catch (Exception e)
                {
                    log.Append(" 出现异常： ").Append(e.Message);
                    recv = CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
                }
            }
            log.Append(" result=").Append(recv);
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 切换用户wifi信息
        /// <summary>
        /// 切换用户wifi信息
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string SwitchUserWifi(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,appid,id
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["token"] == null || qs["ouid"] == null || qs["id"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到切换用户wifi信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到切换用户wifi信息 ：ouid =" + qs["ouid"].ToString() + "&token=" + qs["token"].ToString() + "&id =" + qs["id"].ToString() + "&appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                int id;
                if (!Int32.TryParse(qs["id"].ToString(), out id))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                string strSql = @"SELECT name,password,updatetime from wy_userwifi WHERE id = " + id;
                //根据用户 uid 查询跟该用户相关的wifi列表
                DataTable dtWifi = SqlHelper.ExecuteTable(strSql);
                if (dtWifi == null || dtWifi.Rows.Count == 0)
                    return CommFunc.StandardFormat(MessageCode.WifiNotExist);
                string subrecv = "";
                recv = "";
                string name = dtWifi.Rows[0]["name"].ToString();
                string password = dtWifi.Rows[0]["password"].ToString();
                string updatetime = CommFunc.ConvertDateTimeInt(dtWifi.Rows[0]["updatetime"].ToString()).ToString();
                subrecv = "{\"id\":" + id + ",\"name\": \"" + name + "\",\"password\":\"" + password + "\",\"updatetime\":\"" + updatetime + "\"}";
                recv = "{\"status\":true,\"id\":" + id + ",\"name\": \"" + name + "\",\"password\":\"" + password + "\",\"updatetime\":\"" + updatetime + "\"}";

                List<int> uids = GetUserUid(ouid);
                uids.ForEach(uid => PublicClass.SendToOnlineUserList(null, recv, "", new List<int>() { uid }, 99, 0, CommType.switchUserWifi, CommFunc.APPID));

                return CommFunc.StandardObjectFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #endregion

        #region 绑定

        #region 获取用户绑定设备信息
        /// <summary>
        /// 获取用户绑定设备信息
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string UserBinding(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,sn,token,vcode,appid,gender
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["sn"] == null || qs["vcode"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到用户绑定设备信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到用户绑定设备信息 ：ouid =" + qs["ouid"].ToString() + " token =" + qs["token"].ToString() + " sn =" + qs["sn"].ToString() + " vcode =" + qs["vcode"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                //查询设备是否存在
                string sn = CommFunc.GetUniform12(qs["sn"].ToString());

                string strSql = "SELECT uid FROM app_users WHERE glsn = '" + sn + "'";
                object sqlResult = SqlHelper.ExecuteScalar(strSql);
                if (sqlResult == null)
                    return CommFunc.StandardFormat(MessageCode.DeviceNotExist);
                int uid = (int)sqlResult;

                //查询设备是否在线
                UserObject uo = null;
                recv = CommFunc.StandardFormat(MessageCode.DeviceOutLine);
                if (!MediaService.userDic.TryGetValue(uid, out uo))
                    return recv;
                if (uo == null || uo.socket[CommFunc.APPID] == null)
                    return recv;

                //查询是否注册SN
                string strUidMap = "SELECT count(*) FROM wy_uidmap  where uid = " + uid;//"SELECT count(*) FROM wy_uidmap  where ouid =" + ouid + " and uid = " + uid;
                object sqlUidMapResult = SqlHelper.ExecuteScalar(strUidMap);
                if (sqlUidMapResult != null)
                {
                    int count = (int)sqlUidMapResult;
                    if (count > 0)
                        return CommFunc.StandardFormat(MessageCode.SNExist);
                }
                string vcode = qs["vcode"].Replace("'", "");
                string gender = qs["gender"] == null ? "" : qs["gender"].Replace("'", "");

                recv = "{\"status\":true,\"uid\":\"" + ouid + "\",\"vcode\":\"" + vcode + "\",\"gender\":\"" + gender + "\"}";
                PublicClass.SendToOnlineUserList(null, recv, "", new List<int>() { uid }, 99, 0, CommType.userBinding, CommFunc.APPID);

                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }

        /// <summary>
        /// 用户解除绑定
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string UserUnBinding(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,sn,token,appid
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["sn"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到UserUnBinding ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到UserUnBinding ：ouid =" + qs["ouid"] + "&token =" + qs["token"] + "&sn =" + qs["sn"] + "&appid =" + qs["appid"], MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                //查询设备是否存在
                string sn = CommFunc.GetUniform12(qs["sn"]);
                string strSql = "SELECT uid FROM app_users WHERE glsn = '" + sn + "'";
                object sqlResult = SqlHelper.ExecuteScalar(strSql);
                if (sqlResult == null)
                    return CommFunc.StandardFormat(MessageCode.DeviceNotExist);
                int uid = (int)sqlResult;
                //查询是否注册SN
                string strUidMap = "SELECT count(*) FROM wy_uidmap  where ouid =" + ouid + " and uid = " + uid;
                object sqlUidMapResult = SqlHelper.ExecuteScalar(strUidMap);
                if (sqlUidMapResult != null)
                {
                    int count = (int)sqlUidMapResult;
                    if (count > 0)
                    {
                        string strdelete = "delete from wy_uidmap  where ouid=" + ouid + " and uid=" + uid;
                        int deleteresult = SqlHelper.ExecuteNonQuery(strdelete);
                        if (deleteresult > 0)
                        {
                            MediaService.mapDic.TryRemove(uid, out ouid);
                            PublicClass.SendToOnlineUserList(null, recv, "", new List<int>() { uid }, 99, 0, CommType.userUnBinding, CommFunc.APPID);
                            return CommFunc.StandardFormat(MessageCode.Success);
                        }
                        return CommFunc.StandardFormat(MessageCode.DeleteFaild);
                    }
                }
                return CommFunc.StandardFormat(MessageCode.DefaultError, "设备未绑定");
            }
            catch (Exception e)
            {
                MediaService.WriteLog("接收到UserUnBinding异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }

        /// <summary>
        /// 获取用户绑定设备信息
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string UserBindingTest(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,sn,token,vcode
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["sn"] == null)
            {
                MediaService.WriteLog("接收到获取用户绑定设备信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到用户绑定设备信息 ：ouid =" + qs["ouid"].ToString() + " sn =" + qs["sn"].ToString(), MediaService.wirtelog);

                int ouid;
                if (!Int32.TryParse(qs["ouid"].ToString(), out ouid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                //查询设备是否存在
                string sn = CommFunc.GetUniform12(qs["sn"].ToString());
                string strSql = "SELECT uid FROM app_users WHERE glsn = '" + sn + "'";
                object sqlResult = SqlHelper.ExecuteScalar(strSql);
                if (sqlResult == null)
                    return CommFunc.StandardFormat(MessageCode.DeviceNotExist);
                int uid = (int)sqlResult;
                //查询是否注册SN
                string strUidMap = "SELECT count(*) FROM wy_uidmap  where ouid =" + ouid + " and uid = " + uid;
                object sqlUidMapResult = SqlHelper.ExecuteScalar(strUidMap);
                if (sqlUidMapResult != null)
                {
                    int count = (int)sqlUidMapResult;
                    if (count > 0)
                        return CommFunc.StandardFormat(MessageCode.SNExist);
                }
                //不存在插入设备
                string strInsert = "INSERT INTO wy_uidmap(ouid,uid) VALUES(" + ouid + "," + uid + ")";
                int countInsert = SqlHelper.ExecuteNonQuery(strInsert);
                if (countInsert < 1)
                    return CommFunc.StandardFormat(MessageCode.InsertFaild);

                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }

        /// <summary>
        /// 解除用户绑定设备信息
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string DeleteUserBindingTest(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,sn
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["sn"] == null)
            {
                MediaService.WriteLog("接收到获取用户绑定设备信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到用户绑定设备信息 ：ouid =" + qs["ouid"].ToString() + " sn =" + qs["sn"].ToString(), MediaService.wirtelog);

                int ouid;
                if (!Int32.TryParse(qs["ouid"].ToString(), out ouid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                //查询设备是否存在
                string sn = CommFunc.GetUniform12(qs["sn"].ToString());
                string strSql = "SELECT uid FROM app_users WHERE glsn = '" + sn + "'";
                object sqlResult = SqlHelper.ExecuteScalar(strSql);
                if (sqlResult == null)
                    return CommFunc.StandardFormat(MessageCode.DeviceNotExist);
                int uid = (int)sqlResult;
                //查询是否注册SN
                string strUidMap = "SELECT count(*) FROM wy_uidmap  where ouid =" + ouid + " and uid = " + uid;
                object sqlUidMapResult = SqlHelper.ExecuteScalar(strUidMap);
                if (sqlUidMapResult != null)
                {
                    int count = (int)sqlUidMapResult;
                    if (count > 0)
                    {
                        string strdelete = "delete from wy_uidmap  where ouid=" + ouid + " and uid=" + uid;
                        int deleteresult = SqlHelper.ExecuteNonQuery(strdelete);
                        if (deleteresult > 0)
                        {
                            return CommFunc.StandardFormat(MessageCode.Success);
                        }
                        else
                            return CommFunc.StandardFormat(MessageCode.DeleteFaild);
                    }
                }
                return CommFunc.StandardFormat(MessageCode.DefaultError, "设备未绑定");
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
        #endregion

        #region 查询用户设备绑定信息
        /// <summary>
        /// 查询用户设备绑定信息
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetUserBindingInfo(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * ouid,token,appid
            */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到查询用户设备绑定信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到查询用户设备绑定信息 ：ouid =" + qs["ouid"].ToString() + "token =" + qs["token"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                //查询是否注册SN
                string strSql = "SELECT wy_uidmap.ouid as ouid,wy_uidmap.uid as uid ,app_users.glsn as sn,wy_uidmap.sim as sim,app_users.gender as gender,app_users.nickname as nickname from wy_uidmap,app_users WHERE wy_uidmap.uid = app_users.uid and wy_uidmap.ouid =" + ouid;
                DataTable dtContacts = SqlHelper.ExecuteTable(strSql);
                string subrecv = "";
                foreach (DataRow dr in dtContacts.Rows)
                {
                    string uid = dr["uid"].ToString();
                    string sn = CommFunc.GetUniform8(dr["sn"].ToString());
                    string sim = dr["sim"] == null ? "" : dr["sim"].ToString();
                    string gender = dr["gender"].ToString();
                    string nickname = dr["nickname"].ToString();
                    subrecv += (subrecv == "" ? "" : ",") + "{\"uid\":" + uid + ",\"sn\":\"" + sn + "\",\"nickname\":\"" + nickname + "\",\"sim\":\"" + sim + "\",\"gender\":\"" + gender + "\"}";
                }
                return CommFunc.StandardListFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
        //查询用户设备绑定信息 (remark取通讯录中的remark)
        public static string GetBindingInfoWithContactRemark(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * ouid,token,appid
            */
            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到查询用户设备绑定信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到查询用户设备绑定信息 ：ouid =" + qs["ouid"].ToString() + "token =" + qs["token"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                //查询是否注册SN
                string strSql =
                    string.Format(
                        "SELECT a.ouid as ouid,a.uid as uid ,b.glsn as sn,a.sim as sim,b.gender as gender,c.nickname as nickname ,c.id,c.fuid from wy_uidmap a join app_users b on a.uid = b.uid and a.ouid ={0} left join wy_userrelation c on a.ouid=c.ouid and a.uid=c.fuid and c.isselfdevice=1;",ouid);
                DataTable dtContacts = SqlHelper.ExecuteTable(strSql);
                string subrecv = "";
                foreach (DataRow dr in dtContacts.Rows)
                {
                    string uid = dr["uid"].ToString();
                    string sn = CommFunc.GetUniform8(dr["sn"].ToString());
                    string sim = dr["sim"] == null ? "" : dr["sim"].ToString();
                    string gender = dr["gender"].ToString();
                    string nickname = dr["nickname"]==null?"":dr["nickname"].ToString();
                    string id = dr["id"] == null ? "" : dr["id"].ToString();
                    string fuid = dr["fuid"] == null ? "" : dr["fuid"].ToString();
                    subrecv += (subrecv == "" ? "" : ",") + "{\"uid\":" + uid + ",\"sn\":\"" + sn + "\",\"nickname\":\"" + nickname + "\",\"sim\":\"" + sim + "\",\"gender\":\"" + gender + "\",\"fuid\":\"" + fuid + "\",\"id\":\"" + id + "\"}";
                }
                return CommFunc.StandardListFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
        #endregion

        #region 数据库接口
        /// <summary>
        /// 修改数据库
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string QueryDatabase(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * query
            */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["query"] == null)
            {
                MediaService.WriteLog("接收到查询数据库 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                //MediaService.WriteLog("接收到查询数据库 ：query =" + qs["query"].ToString(), MediaService.wirtelog);

                //查询是否注册SN
                string strSql = qs["query"].ToString().Replace("?", "%");
                DataTable dtTable = SqlHelper.ExecuteTable(strSql);

                List<string> columnNames = new List<string>();
                foreach (DataColumn dc in dtTable.Columns)
                {
                    columnNames.Add(dc.ColumnName);
                }

                string subrecv = "";
                string subcolumn = "";
                foreach (DataRow dr in dtTable.Rows)
                {
                    subcolumn = "";
                    foreach (var name in columnNames)
                    {
                        string value = dr[name] == null ? "" : dr[name].ToString();
                        subcolumn += (subcolumn == "" ? "" : ",") + "\"" + name + "\":\"" + value + "\"";
                    }
                    subrecv += (subrecv == "" ? "" : ",") + "{" + subcolumn + "}";
                }
                return CommFunc.StandardListFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }

        /// <summary>
        /// 修改数据库
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string ModifyDatabase(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * modify
            */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["modify"] == null)
            {
                MediaService.WriteLog("接收到修改数据库 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                //MediaService.WriteLog("接收到修改数据库 ：modify =" + qs["modify"].ToString(), MediaService.wirtelog);

                //查询是否注册SN
                string strSql = qs["modify"].ToString();
                if (strSql.Trim().StartsWith("(") && strSql.Trim().EndsWith(")"))
                {
                    strSql = strSql.Substring(1, strSql.Length - 2);
                }
                int count = SqlHelper.ExecuteNonQuery(strSql);
                string subrecv = "{\"count\":" + count + "}";
                return CommFunc.StandardObjectFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
        #endregion

        #region 查询约聊
        /// <summary>
        /// 查询约聊
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string QueryState(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * 
            */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null)
            {
                MediaService.WriteLog("接收到查询约聊实时数据 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到查询约聊实时数据 ", MediaService.wirtelog);

                string subrecv = "";
                foreach (KeyValuePair<int, TalkState> ts in MediaService.stateDic)
                {
                    string key = ts.Key.ToString();
                    subrecv += (subrecv == "" ? "" : ",") + "{\"key\":" + key + ",\"talkstate\":" + ts.Value.ToString() + "}";
                }
                return CommFunc.StandardListFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
        #endregion

        #region 查询约聊记录
        /// <summary>
        /// 查询约聊记录
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string QueryRecord(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * 
            */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null)
            {
                MediaService.WriteLog("接收到查询约聊记录 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到查询约聊实时记录 ", MediaService.wirtelog);
                return CommFunc.StandardListFormat(MessageCode.Success, TalkRecordManager.Instance.GetRecord());
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
        #endregion

        #region 查询绑定GoloZ的数量
        /// <summary>
        /// 查询绑定GoloZ的数量
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetBindCount(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * starttime,endtime
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["starttime"] == null || qs["endtime"] == null)
            {
                MediaService.WriteLog("接收到查询绑定GoloZ的数量:" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到查询绑定GoloZ的数量 starttime:" + qs["starttime"].ToString() + "endtime:" + qs["endtime"].ToString(), MediaService.wirtelog);
                string starttime = qs["starttime"] == null ? "" : qs["starttime"].ToString();
                string endtime = qs["endtime"] == null ? "" : qs["endtime"].ToString();
                string sql = "";
                int time_count = 0;
                int total_count = 0;
                if (!string.IsNullOrWhiteSpace(starttime) && !string.IsNullOrWhiteSpace(endtime))
                {
                    DateTime stime = CommFunc.StampToDateTime(starttime);
                    DateTime etime = CommFunc.StampToDateTime(endtime);
                    DateTime temp = DateTime.Now;
                    if (stime > etime)
                    {
                        temp = etime;
                        etime = stime;
                        stime = temp;
                    }
                    sql = "SELECT t1.time_count,t2.total_count FROM (select count(uid) as time_count from wy_uidmap where bindtime between '" + stime.ToString() + "' and '" + etime.ToString() + "' ) as t1, (select count(uid) AS total_count from wy_uidmap where bindtime is null or bindtime < '" + etime.ToString() + "') as t2";
                    DataTable dt = SqlHelper.ExecuteTable(sql);
                    if (dt.Rows.Count > 0)
                    {
                        time_count = Convert.ToInt32(dt.Rows[0]["time_count"].ToString());
                        total_count = Convert.ToInt32(dt.Rows[0]["total_count"].ToString());
                    }
                }
                string subrecv = "{\"total_count\":" + total_count + ",\"time_count\":" + time_count + "}";
                return CommFunc.StandardObjectFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 修改性别信息
        /// <summary>
        /// 修改性别信息
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string ModifyGender(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,appid,gender,uid
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["gender"] == null)
            {
                MediaService.WriteLog("接收到修改性别信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到修改性别信息 ：ouid=" + qs["ouid"].ToString() + "&token=" + qs["token"].ToString() + "&appid=" + qs["appid"].ToString() + "&gender=" + qs["gender"].ToString() + "&uid=" + qs["uid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                int gender = 0;
                string sex = qs["gender"].ToString();
                if (!int.TryParse(sex, out gender))
                {
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                }
                string sql = string.Format("UPDATE app_users  SET gender ='{0}' WHERE uid ={1}", gender, uid);
                int count = SqlHelper.ExecuteNonQuery(sql);
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
        #endregion

        #endregion

        #region 频道管理

        #region 获取频道数
        public static void GetTalkNum(int tid, ref int totalnum, ref int usernum)
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
                            if (uo.socket != null && uo.socket[CommFunc.APPID] != null)
                            {
                                if (uo.socket[CommFunc.APPID].Connected)
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

        #region 获取频道号码
        /// <summary>
        /// 获取频道号码
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetTalkName(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * [uid],appid,ouid,token
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到获取频道号码：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到获取频道号码 ：ouid =" + qs["ouid"].ToString() + "token =" + qs["token"].ToString() + "appid =" + qs["appid"].ToString() + "uid =" + qs["uid"], MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                string talkname = null;
                string verifi = null;
                if (GetTalkName(ref talkname, ref verifi))
                {
                    string data = "{\"talkname\":\"" + talkname + "\",\"verification\":\"" + verifi + "\"}";
                    recv = CommFunc.StandardObjectFormat(MessageCode.Success, data);
                }
                else
                {
                    recv = CommFunc.StandardFormat(MessageCode.TalkAllocationFaild);
                }
                return recv;
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        /// <summary>
        /// 获取频道号码,不验证token
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetTalkNameWithOutToken(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * 
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            try
            {
                string talkname = null;
                string verifi = null;
                label:
                if (GetTalkNameWithNoToken(ref talkname, ref verifi))
                {
                    string sql = string.Format("select talkname from wy_talk where talkname={0}", talkname);
                    if (SqlHelper.ExecuteScalar(sql) != null) //验证生成的talkname是否已经存在,如果不存在就继续生成
                    {
                        talkname = verifi = null;
                        goto label;
                    }
                    string data = "{\"talkname\":\"" + talkname + "\",\"verification\":\"" + verifi + "\"}";
                    recv = CommFunc.StandardObjectFormat(MessageCode.Success, data);
                }
                else
                {
                    recv = CommFunc.StandardFormat(MessageCode.TalkAllocationFaild);
                }
                /* 自增逻辑不用了. 因为要保留靓号功能.
                //string verifi = null;
                //var talkname = ConfigurationManager.AppSettings["talknamestart"];
                //if (GetTalkName(talkname, ref verifi))
                //{
                //    string data = "{\"talkname\":\"" + talkname + "\",\"verification\":\"" + verifi + "\"}";
                //    recv = StandardObjectFormat(MessageCode.Success, data);
                //    Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                //    cfa.AppSettings.Settings["talknamestart"].Value = (Int32.Parse(talkname) + 1).ToString();
                //    cfa.Save(ConfigurationSaveMode.Modified);
                //    ConfigurationManager.RefreshSection("appSettings");
                //}
                //else
                //{
                //    recv = StandardFormat(MessageCode.TalkAllocationFaild);
                //}
                 */
                return recv;
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        /// <summary>
        /// 获取频道name, 从1000000开始 为自驾游服务
        /// </summary>
        /// <param name="talkname"></param>
        /// <param name="verifi"></param>
        /// <returns></returns>
        internal static bool GetTalkNameWithNoToken(ref string talkname, ref string verifi)
        {
            Random ran = new Random((int)DateTime.Now.Ticks);
            for (int k = 0; k < 10; k++)
            {
                string qhao = ran.Next(1000000, 10000000).ToString();
                if (CommBusiness.IsTalkNameOKWithNoToken(qhao) == true)
                {
                    talkname = qhao.PadLeft(5, '0');
                    break;
                }
            }
            if (talkname != null)
            {
                verifi = CommFunc.StringToMD5Hash(talkname + MediaService.Verification);
                return true;
            }
            return false;
        }
        internal static bool GetTalkName(ref string talkname, ref string verifi)
        {
            Random ran = new Random((int)DateTime.Now.Ticks);
            for (int k = 0; k < 10; k++)
            {
                string qhao = ran.Next(100, 100000).ToString();
                if (CommBusiness.IsTalkNameOK(qhao) == true)
                {
                    talkname = qhao.PadLeft(5, '0');
                    break;
                }
            }
            if (talkname != null)
            {
                verifi = CommFunc.StringToMD5Hash(talkname + MediaService.Verification);
                return true;
            }
            return false;
        }
        #endregion

        #region 创建我的频道
        /// <summary>
        /// 创建我的频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string CreateMyTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * uid,appid,ouid,token,talkname,verification,[info],[auth],[imageurl],[sn],[talkmode] //sn号为自驾游项目提供. 其它可以不传
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["talkname"] == null && qs["verification"] == null)
            {
                MediaService.WriteLog("接收到创建我的频道 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到创建我的频道 ：ouid =" + qs["ouid"].ToString() + "&token =" + qs["token"].ToString() + "&appid =" + qs["appid"].ToString() + "&uid =" + qs["uid"].ToString() + "&talkname =" + qs["talkname"].ToString() + "&verification =" + qs["verification"].ToString() + "&sn号 =" + qs["sn"], MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                int talkmode;
                if (int.TryParse(qs["talkmode"], out talkmode))
                {
                    if (talkmode != (int) EnumTalkMode.Nomal && talkmode != (int) EnumTalkMode.JIT)
                    {
                        talkmode = -1;
                    }
                }
                else
                {
                    talkmode = -1;
                }
                recv = CreateMyTalk(qs["verification"].ToString().Replace("'", ""), qs["talkname"].ToString().Replace("'", ""), qs["info"], qs["auth"], qs["talknotice"], qs["imageurl"], uid, qs["sn"],talkmode);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.TalkCreateFaild, e.Message);
            }
        }
        /// <summary>
        /// 创建我的频道, 不验证token
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string CreateMyTalkWithOutToken(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,talkname,verification,[auth],info,leaderouid,talkmode
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["talkname"] == null || qs["verification"] == null || qs["leaderouid"]==null)
            {
                MediaService.WriteLog("CreateMyTalkWithOutToken 返回值：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("CreateMyTalkWithOutToken 返回值 ：" + "&ouid =" + qs["ouid"] + "&talkname =" + qs["talkname"], MediaService.wirtelog);

                int ouid = Convert.ToInt32(qs["ouid"]);
                int leaderouid = 0;
                if (!int.TryParse(qs["leaderouid"], out leaderouid))
                {
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                }

                int talkmode;
                if (int.TryParse(qs["talkmode"], out talkmode))
                {
                    if (talkmode != (int)EnumTalkMode.Nomal && talkmode != (int)EnumTalkMode.JIT)
                    {
                        talkmode = -1;
                    }
                }
                else
                {
                    talkmode = -1;
                }
                recv = CreateMyTalk(qs["verification"].Replace("'", ""), qs["talkname"].Replace("'", ""), qs["auth"], ouid, qs["info"], leaderouid, talkmode);
                MediaService.WriteLog("CreateMyTalkWithOutToken返回值 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.TalkCreateFaild, e.Message);
            }
        }

        //创建频道 (验证token,type必需,固定传4. remark必需)
        public static string CreateTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * uid,appid,token,ouid,talkname,verification,[auth],[imageurl],remark,type,[talkmode]
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["appid"] == null || qs["token"] == null || qs["ouid"] == null || qs["talkname"] == null || qs["verification"] == null || qs["remark"] == null || qs["type"] == null || qs["uid"] == null)
            {
                MediaService.WriteLog("CreateTalk 返回值：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("CreateTalk 返回值 ：" + "&ouid =" + qs["ouid"] + "&talkname =" + qs["talkname"] + "&appid =" + qs["appid"] + "&token =" + qs["token"] + "&verification =" + qs["verification"] + "&remark =" + qs["remark"] + "&type =" + qs["type"] + "&uid =" + qs["uid"], MediaService.wirtelog);
                int type = 0;
                if (!int.TryParse(qs["type"], out type))
                {
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                }
                //验证token
                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["uid"], qs["appid"], qs["token"], ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;
                //end

                var remark = qs["remark"];
                var verification = qs["verification"].Replace("'", "");
                var talkname = qs["talkname"].Replace("'", "");
                int talkmode;
                if (int.TryParse(qs["talkmode"], out talkmode))
                {
                    if (talkmode != (int)EnumTalkMode.Nomal && talkmode != (int)EnumTalkMode.JIT)
                    {
                        talkmode = -1;
                    }
                }
                else
                {
                    talkmode = -1;
                }
                recv = createTalk(verification, talkname, qs["auth"], uid, remark, type, qs["imageurl"], talkmode);
                MediaService.WriteLog("CreateTalk 返回值 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.TalkCreateFaild, e.Message);
            }
        }

        internal static string createTalk(string verification, string talkname, object auth,int uid, string remark,int type, object imageurl,int talkmode=-1)
        {
            string recv = "";
            if (verification == CommFunc.StringToMD5Hash(talkname + MediaService.Verification))
            {
                object obj =
                    SqlHelper.ExecuteScalar("select count(tid) from [wy_talk] where createuid=" + uid + " and type=4"); //type=4
                if (obj != null && Convert.ToInt32(obj) <20 )//频道数放宽到20个
                {
                    if (CommBusiness.IsValiNum(talkname) && CommBusiness.IsTalkNameOKWithNoToken(talkname) == true)
                    {
                        auth = auth == null ? "" : auth.ToString().Replace("'", "");
                        var _imageurl = imageurl ?? "";
                        obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname + "'");
                        if (obj == null)
                        {
                            obj =SqlHelper.ExecuteScalar(string.Format("insert [wy_talk] (talkname,auth,createuid,type,talknotice,imageurl,talkmode) values ('{0}','{1}',{2},{3},'{4}','{5}',{6});select scope_identity()",talkname,auth,uid,type,remark,_imageurl,talkmode));
                            
                            if (obj != null)
                            {
                                obj =SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname +"'");
                                string tid = obj.ToString();
                                SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid,xuhao) values (" + tid + "," + uid + ",'1')");
                                string data = "{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\"}";
                                SqlHelper.ExecuteNonQuery("UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid);

                                #region 通知Goloz

                                SendToGoloZTalkInfo(uid, tid, CommType.createUserTalk);

                                #endregion

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

        internal static string CreateMyTalk(string verification, string talkname, object auth, int ouid, string info, int leaderouid, int talkmode=-1)
        {
            MediaService.WriteLog(
                "接收到创建我的频道不验证token ：" + "&verification =" + verification + "&talkname =" + talkname + "&uid=" + ouid,
                MediaService.wirtelog);
            string recv = "";
            if (verification == CommFunc.StringToMD5Hash(talkname + MediaService.Verification))
            {
                object obj =
                    SqlHelper.ExecuteScalar("select count(tid) from [wy_talk] where createuid=" + ouid + " and type=0");
                //if (obj != null && (obj.ToString() == "0" || obj.ToString() == "1")) //去除频道数过多限制
                //{
                //if (talkname.Length == 5 && CommBusiness.IsValiNum(talkname) &&
                //    CommBusiness.IsTalkNameOK(talkname) == true)
                //{
                    auth = auth == null ? "" : auth.ToString().Replace("'", "");
                    //ran.Next(100, 1000).ToString() : qs["auth"].Replace("'", "");

                    obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname + "'");
                    if (obj == null)
                    {
                        obj =
                            SqlHelper.ExecuteScalar("insert [wy_talk] (talkname,auth,createuid,type,talknotice,ouid,talkmode) values ('" + talkname + "','" + auth + "','" + ouid + "','" + 3 + "','" + info + "','" + leaderouid + "','" + talkmode + "');select scope_identity()"); //添加ouid字段,并把leaderouid传进去.
                        if (obj != null)
                        {
                            obj =
                                SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname +"'");
                            string tid = obj.ToString();
                            SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid,xuhao) values (" + tid + "," +ouid + ",'1')");
                            string data = "{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" +auth + "\"}";
                            SqlHelper.ExecuteNonQuery("UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " +ouid);

                            //todo 处理leaderouid
                            #region  处理leaderouid

                            var collon = new NameValueCollection();
                            collon.Set("ouids", leaderouid.ToString());
                            collon.Set("tid", tid);
                            UserJoinTalkWithOutToken(collon);
                            #endregion

                            #region 通知Goloz

                            SendToGoloZTalkInfo(ouid, tid, CommType.createUserTalk);

                            #endregion

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
                //}
                //else
                //{
                //    recv = StandardFormat(MessageCode.TalkInvalid);
                //}
            }
            //else
            //{
            //    recv = StandardFormat(MessageCode.TalkFull);
            //}
            //}
            else
            {
                recv = CommFunc.StandardFormat(MessageCode.TalkVerificationFaild);
            }
            return recv;
        }

        internal static string CreateMyTalk(string verification, string talkname, object info, object auth, object talknotice, object imageurl, int uid, object sn,int talkmode=-1) //sn号为自驾游项目提供. 其它可以不传
        {
            #region 新逻辑
            string recv = "";
            if (verification == CommFunc.StringToMD5Hash(talkname + MediaService.Verification))
            {
                var _obj = SqlHelper.ExecuteTable("select zsn,glsn from [app_users] where uid=" + uid + "");
                if (_obj != null && (_obj.Rows[0]["zsn"].ToString() == "91002211" || _obj.Rows[0]["zsn"].ToString() == "92010863" || _obj.Rows[0]["zsn"].ToString() == "92015755" || _obj.Rows[0]["zsn"].ToString() == "92011026" || _obj.Rows[0]["zsn"].ToString() == "92010866") || _obj.Rows[0]["zsn"].ToString() == "91001919")//测试用 
                {
                    MediaService.WriteLog("CreateMyTalk: success enter", MediaService.wirtelog);
                    object obj = SqlHelper.ExecuteScalar("select count(tid) from [wy_talk] where createuid=" + uid + " and type=3"); //type=3表示新设备 2016.4.13
                    if (obj != null && (Convert.ToInt32(obj) <20 ))//(obj.ToString() == "0" || obj.ToString() == "1")
                    {
                        if (talkname.Length == 5 && CommBusiness.IsValiNum(talkname) && CommBusiness.IsTalkNameOK(talkname) == true)
                        {
                            info = info == null ? "" : info.ToString().Replace("'", "");
                            auth = auth == null ? "" : auth.ToString().Replace("'", "");//ran.Next(100, 1000).ToString() : qs["auth"].Replace("'", "");
                            talknotice = talknotice == null ? "" : talknotice.ToString().Replace("'", "");
                            imageurl = imageurl == null ? "" : imageurl.ToString();

                            obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname + "'");
                            if (obj == null)
                            {
                                var sql = string.Format("insert [wy_talk] (talkname,auth,createuid,info,talknotice,imageurl,type,talkmode) values ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}');select scope_identity()", talkname, auth, uid, info, talknotice, imageurl, (int)EnumTalkType.SelfTravel, talkmode);
                                MediaService.WriteLog("接收到创建我的频道 插入sql：'" + sql + "'", MediaService.wirtelog);
                                obj = SqlHelper.ExecuteScalar(sql);
                                if (obj != null)
                                {
                                    obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname + "'");
                                    string tid = obj.ToString();
                                    SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid,xuhao) values (" + tid + "," + uid + ",'1')");
                                    string data = "{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\"}";
                                    SqlHelper.ExecuteNonQuery("UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid);

                                    #region 通知Goloz
                                    SendToGoloZTalkInfo(uid, tid, CommType.createUserTalk);
                                    #endregion

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
                    object obj = SqlHelper.ExecuteScalar("select count(tid) from [wy_talk] where createuid=" + uid + " and type=0");
                    if (obj != null && Convert.ToInt32(obj) < 20) //放宽到20人
                    {
                        if (talkname.Length == 5 && CommBusiness.IsValiNum(talkname) && CommBusiness.IsTalkNameOK(talkname) == true)
                        {
                            info = info == null ? "" : info.ToString().Replace("'", "");
                            auth = auth == null ? "" : auth.ToString().Replace("'", "");//ran.Next(100, 1000).ToString() : qs["auth"].Replace("'", "");
                            talknotice = talknotice == null ? "" : talknotice.ToString().Replace("'", "");
                            imageurl = imageurl == null ? "" : imageurl.ToString();

                            obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname + "'");
                            if (obj == null)
                            {
                                //obj = SqlHelper.ExecuteScalar("insert [wy_talk] (talkname,auth,createuid,info,talknotice,imageurl) values ('" + talkname + "','" + auth + "','" + uid + "','" + info + "','" + talknotice + "','" + imageurl + "');select scope_identity()");
                                obj = SqlHelper.ExecuteScalar(string.Format("insert [wy_talk] (talkname,auth,createuid,info,talknotice,imageurl,talkmode) values ('{0}','{1}','{2}','{3}','{4}','{5}','{6}');select scope_identity()", talkname, auth, uid, info, talknotice, imageurl, talkmode));
                                if (obj != null)
                                {
                                    obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname + "'");
                                    string tid = obj.ToString();
                                    SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid,xuhao) values (" + tid + "," + uid + ",'1')");
                                    string data = "{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\"}";
                                    SqlHelper.ExecuteNonQuery("UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid);

                                    #region 通知Goloz
                                    SendToGoloZTalkInfo(uid, tid, CommType.createUserTalk);
                                    #endregion

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
            }
            else
            {
                recv = CommFunc.StandardFormat(MessageCode.TalkVerificationFaild);
            }
            return recv;
            #endregion
            #region 原逻辑
            /* old
            string recv = "";
            if (verification == StringToMD5Hash(talkname + MediaService.Verification))
            {
                object obj = SqlHelper.ExecuteScalar("select count(tid) from [wy_talk] where createuid=" + uid + " and type=0");
                if (obj != null && (obj.ToString() == "0" || obj.ToString() == "1"))
                {
                    if (talkname.Length == 5 && CommBusiness.IsValiNum(talkname) && CommBusiness.IsTalkNameOK(talkname) == true)
                    {
                        info = info == null ? "" : info.ToString().Replace("'", "");
                        auth = auth == null ? "" : auth.ToString().Replace("'", "");//ran.Next(100, 1000).ToString() : qs["auth"].Replace("'", "");
                        talknotice = talknotice == null ? "" : talknotice.ToString().Replace("'", "");
                        imageurl = imageurl == null ? "" : imageurl.ToString();

                        obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname + "'");
                        if (obj == null)
                        {
                            obj = SqlHelper.ExecuteScalar("insert [wy_talk] (talkname,auth,createuid,info,talknotice,imageurl) values ('" + talkname + "','" + auth + "','" + uid + "','" + info + "','" + talknotice + "','" + imageurl + "');select scope_identity()");
                            if (obj != null)
                            {
                                obj = SqlHelper.ExecuteScalar("select tid from [wy_talk] where talkname='" + talkname + "'");
                                string tid = obj.ToString();
                                SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid,xuhao) values (" + tid + "," + uid + ",'1')");
                                string data = "{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\"}";
                                SqlHelper.ExecuteNonQuery("UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid);

                                #region 通知Goloz
                                SendToGoloZTalkInfo(uid, tid, CommType.createUserTalk);
                                #endregion

                                recv = StandardObjectFormat(MessageCode.Success, data);
                            }
                            else
                            {
                                recv = StandardFormat(MessageCode.TalkCreateFaild);
                            }
                        }
                        else
                        {
                            recv = StandardFormat(MessageCode.TalkExist);
                        }
                    }
                    else
                    {
                        recv = StandardFormat(MessageCode.TalkInvalid);
                    }
                }
                else
                {
                    recv = StandardFormat(MessageCode.TalkFull);
                }
            }
            else
            {
                recv = StandardFormat(MessageCode.TalkVerificationFaild);
            }
            return recv;
             */
            #endregion
        }

        //批量加入企业频道
        public static string BatchJoinCorpTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * uid,appid,token,ouid,uids,tid
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["uid"] == null || qs["appid"] == null || qs["token"] == null || qs["ouid"] == null || qs["uids"] == null || qs["tid"] == null)
            {
                return recv;
            }
            try
            {
                //验证token
                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["uid"], qs["appid"], qs["token"], ref ouid, ref uid,
                    ref recv);
                if (!isVerToken)
                    return recv;

                //验证频道号
                int tid = 0;
                int.TryParse(qs["tid"], out tid);
                if (PublicClass.FindTalkType(tid) != 2)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkInvalid);
                }
                //验证频道创建者
                var sqlCreate = string.Format("select createuid from [wy_talk] where tid={0} and createuid={1}", tid, uid);
                var sqlCreateResult = SqlHelper.ExecuteScalar(sqlCreate);
                if (sqlCreateResult == null)
                {
                    return CommFunc.StandardFormat(MessageCode.NoAuthMustCreater);
                }
                //进行添加操作
                var sqlInsert = new StringBuilder().Append(string.Format("insert into [wy_talkuser](tid,uid) values "));
                var num = 0;
                foreach (var s in qs["uids"].Split(','))
                {
                    var obj =
                        SqlHelper.ExecuteScalar(string.Format("select id from [wy_talkuser] where tid={0} and  uid={1}",
                            tid, s));
                    if (obj == null)
                    {
                        num++;
                        sqlInsert.Append(string.Format("({0},{1}),", tid, s));
                    }
                }
                MediaService.WriteLog(string.Format("sqlinsert:{0}",sqlInsert), MediaService.wirtelog);
                if (num != 0)
                {
                    sqlInsert = sqlInsert.Remove(sqlInsert.Length - 1, 1);
                    int i=SqlHelper.ExecuteNonQuery(string.Format("{0};update [wy_talk] set usernum=usernum+{1} where tid={2};", sqlInsert, num, tid));

                    if (i > 0) //插入成功,推送设备
                    {
                        foreach (var _uid in qs["uids"].Split(','))
                        {
                            SendToGoloZTalkInfo(Convert.ToInt32(_uid), tid.ToString(), CommType.userJoinPersonTalk);
                        }

                        var uidInts = qs["uids"].Split(',').ToList().Select(j => Convert.ToInt32(j)).ToList();
                        var talkinfo = CommFunc.GetTalkInfoNum(tid);
                        PublicClass.SendToOnlineUserList(null, talkinfo, "", uidInts, 99, 0, CommType.getUsernumInTalk, CommFunc.APPID);
                    }
                }
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, e.Message);
            }
        }

        //批量删除企业频道人数
        public static string BatchDelCorpTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * uid,appid,token,ouid,uids,tid
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["uid"] == null || qs["appid"] == null || qs["token"] == null || qs["ouid"] == null || qs["uids"] == null || qs["tid"] == null)
            {
                return recv;
            }
            try
            {
                //验证token
                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["uid"], qs["appid"], qs["token"], ref ouid, ref uid,
                    ref recv);
                if (!isVerToken)
                    return recv;

                //验证频道号
                int tid = 0;
                int.TryParse(qs["tid"], out tid);
                if (PublicClass.FindTalkType(tid) != 2)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkInvalid);
                }
                //验证频道创建者
                var sqlCreate = string.Format("select createuid from [wy_talk] where tid={0} and createuid={1}", tid, uid);
                var sqlCreateResult = SqlHelper.ExecuteScalar(sqlCreate);
                if (sqlCreateResult == null)
                {
                    return CommFunc.StandardFormat(MessageCode.NoAuthMustCreater);
                }
                //进行批量删除操作
                var sqlDelete = string.Format("delete from [wy_talkuser] where tid={0} and uid in ({1})",tid,qs["uids"]);
                var num = SqlHelper.ExecuteNonQuery(sqlDelete);
                SqlHelper.ExecuteNonQuery(string.Format("update [wy_talk] set usernum=usernum-{0} where tid={1};", num, tid));

                if (num > 0) //推送设备
                {
                    var uidInts= qs["uids"].Split(',').ToList().Select(i => Convert.ToInt32(i)).ToList();
                    string sendrecv = "{\"status\":true,\"tid\":" + tid + ",\"createuid\":" + uid + "}";
                    PublicClass.SendToOnlineUserList(null, sendrecv, "", uidInts, 99, 0,
                        CommType.userQuitTalk, CommFunc.APPID);

                    var talkinfo = CommFunc.GetTalkInfoNum(tid);
                    PublicClass.SendToOnlineUserList(null, talkinfo, "", uidInts, 99, 0, CommType.getUsernumInTalk, CommFunc.APPID);
                }
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, e.Message);
            }
        }

        //获取频道成员列表
        public static string GetTalkUserList(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * uid,appid,token,ouid,tid,pageindex,pagesize
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["uid"] == null || qs["appid"] == null || qs["token"] == null || qs["ouid"] == null || qs["tid"] == null) // || qs["pageindex"] == null || qs["pagesize"] == null
            {
                return recv;
            }
            try
            {
                //验证token
                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["uid"], qs["appid"], qs["token"], ref ouid, ref uid,
                    ref recv);
                if (!isVerToken)
                    return recv;
                
                int pageindex, pagesize;
                //if (!int.TryParse(qs["pageindex"], out pageindex) || !int.TryParse(qs["pagesize"], out pagesize))
                //{
                //    return StandardFormat(MessageCode.FormatError);
                //}
                //验证频道号
                int tid = 0;
                int.TryParse(qs["tid"], out tid);
                if (PublicClass.FindTalkType(tid) != 2)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkInvalid);
                }

                var sqlselect = string.Format("select * from wy_talkuser t where t.tid={0} and t.uid={1}", tid, uid);
                if (SqlHelper.ExecuteScalar(sqlselect) == null)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkVerificationFaild);
                }

                //string sql = string.Format("SELECT TOP {0} * FROM (SELECT ROW_NUMBER() OVER (ORDER BY tid) AS RowNumber,t.uid,t.tid,t.remark,t.info,au.glsn,au.gender FROM [weiyun].[dbo].wy_talkuser t,[weiyun].[dbo].app_users au where t.uid=au.uid and t.tid=10331) A WHERE RowNumber > {0}*({1}-1)", pagesize, pageindex);
                string sql = string.Format("SELECT ROW_NUMBER() OVER (ORDER BY tid) AS RowNumber,t.uid,t.tid,t.remark,t.info,au.glsn,au.gender FROM [weiyun].[dbo].wy_talkuser t,[weiyun].[dbo].app_users au where t.uid=au.uid and t.tid={0}",tid);
                var datatable = SqlHelper.ExecuteTable(sql);

                var talkUsers = new List<TalkUserListModel>();
                foreach (DataRow row in datatable.Rows)
                {
                    talkUsers.Add(new TalkUserListModel
                                  {
                                      glsn = row["glsn"].ToString(),
                                      remark = row["remark"].ToString(),
                                      tid = row["tid"].ToString(),
                                      uid = row["uid"].ToString(),
                                      gender=row["gender"].ToString()
                                  });
                }
                var jsonCommModel = new AppJsonResultModel<List<TalkUserListModel>>((int)MessageCode.Success, MessageCodeDiscription.GetMessageCodeDiscription(MessageCode.Success), talkUsers);
                var jsonstr = JsonConvert.SerializeObject(jsonCommModel);
                return jsonstr;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.SearchFaild, e.Message);
            }
        }

        

        private static bool SendToGoloZTalkInfo(int uid, string tid, short comm, bool hasCreate = true)
        {
            #region 通知Goloz
            StringBuilder sb = new StringBuilder();
            string sql;
            List<SqlParameter> paras = new List<SqlParameter> { new SqlParameter("@tid", tid) };
            if (hasCreate)
            {
                //sql =
                //    "select T1.id,T1.xuhao,T1.duijiang,T1.remark,T2.* from (select id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid =@uid) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid  and T2.tid =@tid";
                sql = "select t3.glsn,T1.id,T1.xuhao,T1.duijiang,T1.remark,T2.* from (select id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid =@uid) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid  and T2.tid =@tid inner join app_users as t3 on t3.uid=t2.createuid";
                paras.Add(new SqlParameter("@uid", uid));
            }
            else
            {
                //sql = "select tid, talkname, auth, createuid, muid, info, talknotice, moditime, usernum, imageurl, [type] from [wy_talk]  WHERE tid=@tid";
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
            GetTalkNum(Convert.ToInt32(tid), ref totalnum, ref usernum);
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
        #endregion

        #region 获取我所加入的频道
        /// <summary>
        /// 获取我所加入的频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetMyAllTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,appid,ouid,token,minitid
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["minitid"] == null)
            {
                MediaService.WriteLog("接收到获取我所加入的频道 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            StringBuilder log = new StringBuilder("接收到获取我所加入的频道 ：ouid=" + qs["ouid"] + " token=" + qs["token"] + " appid=" + qs["appid"] + " uid=" + qs["uid"] + " minitid=" + qs["minitid"]);
            try
            {
                int minitid;
                if (!Int32.TryParse(qs["minitid"], out minitid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["uid"], qs["appid"], qs["token"], ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                StringBuilder sb = new StringBuilder();
                string sql;
                if (minitid == 0)
                {
                    //sql = "select T1.id,T1.tid,T1.xuhao,T1.duijiang,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.imageurl from (select top 20 id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = " + uid + " order by id desc) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid order by T1.id desc";
                    //"select T1.id,T1.tid,T1.xuhao,T1.duijiang,t2.talknotice,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.imageurl,T2.talkmode,T3.glsn from (select id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = {0}) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid inner join app_users as t3 on t3.uid=t2.createuid order by T1.id desc;"  这个sql关联了SN
                    sql = string.Format(
                        "select  T1.tid,T1.xuhao,T1.duijiang,t2.talknotice,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.imageurl,T2.talkmode from (select id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = {0} or uid ={1}) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid order by T1.tid desc;",
                        uid,ouid);
                }
                else
                {
                    //sql = "select T1.id,T1.tid,T1.xuhao,T1.duijiang,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.imageurl from (select top 20 id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = " + uid + " and id<" + minitid + " order by id desc) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid order by T1.id desc";
                    //select T1.id,T1.tid,T1.xuhao,T1.duijiang,t2.talknotice,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.talkmode,T2.imageurl,T3.glsn from (select  id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = {0} and id<{1}) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid inner join app_users as t3 on t3.uid=t2.createuid order by T1.id desc;
                    sql =
                        string.Format(
                            "select T1.tid,T1.xuhao,T1.duijiang,t2.talknotice,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.talkmode,T2.imageurl from (select  id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = {0} or uid={1} and id<{2}) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid order by T1.tid desc;",
                            uid,ouid, minitid);
                }
                /*  不知道为谁改的业务逻辑,暂时先不用. 到时候再讨论
                List<int> uidsList = new List<int>();
                var table = SqlHelper.ExecuteTable(string.Format("select uid from wy_uidmap where ouid ={0}", ouid)); //根据ouid查到绑定的uid
                if (table != null)
                {
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        uidsList.Add(table.Rows[i]["uid"].ToString().ToInt());
                    }
                }
                */
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
                    string sn = "";
                    string talkname = dt.Rows[i]["talkname"].ToString();
                    string auth = dt.Rows[i]["auth"].ToString();
                    string remark = dt.Rows[i]["remark"].ToString();
                    string talkmode = dt.Rows[i]["talkmode"].ToString();
                    if (dt.Rows[i]["type"] != null) // && dt.Rows[i]["type"].ToString() == "4"
                    {
                        if (dt.Rows[i]["talknotice"] != null && dt.Rows[i]["talknotice"].ToString() != "")
                            //如果频道表已经有备注, 取频道表的备注
                        {
                            remark = dt.Rows[i]["talknotice"].ToString();
                        }
                    }
                    string create = "false";
                    //if (dt.Rows[i]["createuid"].ToString() == uid.ToString())
                    //{
                    //    create = "true";
                    //}
                    //if (uidsList.Contains(Convert.ToInt32(dt.Rows[i]["createuid"])))
                    //{
                    //    create = "true";
                    //}
                    if (dt.Rows[i]["createuid"].ToString() == uid.ToString() || dt.Rows[i]["createuid"].ToString() == ouid.ToString())
                    {
                        create = "true";
                    }
                    string imageurl = dt.Rows[i]["imageurl"] == null ? "" : dt.Rows[i]["imageurl"].ToString();
                    string type = dt.Rows[i]["type"].ToString();
                    int totalnum = 0;
                    int usernum = 0;
                    GetTalkNum(tid, ref totalnum, ref usernum);
                    sb.Append(",{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"talkmode\":\"" + talkmode + "\",\"auth\":\"" + auth + "\",\"remark\":\"" + remark + "\",\"create\":" + create + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + ",\"imageurl\":\"" + imageurl + "\",\"type\":\"" + type + "\",\"sn\":\"" + sn + "\"}");
                }
                if (dt.Rows.Count > 0)
                {
                    //minitid = Int32.Parse(dt.Rows[dt.Rows.Count - 1]["tid"].ToString());
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

        #endregion

        #region 获取频道列表

        public static string SearchTalkList(NameValueCollection qs)
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
                    sql = string.Format("SELECT TOP {0} * FROM (SELECT ROW_NUMBER() OVER (ORDER BY tid) AS RowNumber,t.* FROM [weiyun].[dbo].wy_talk t where  t.type in(0,4) and (t.talknotice like '%{1}%' or t.talkname like '%{1}%')) A WHERE RowNumber > {2}*({3}-1)", pagesize, keyword, pagesize, pageindex);
                }
                else
                {
                    sql=string.Format("SELECT TOP {0} * FROM (SELECT ROW_NUMBER() OVER (ORDER BY tid) AS RowNumber,t.*,au.glsn FROM [weiyun].[dbo].wy_talk t,[weiyun].[dbo].app_users au where t.createuid=au.uid and t.type in(0,4) and (t.talknotice like '%{1}%' or t.talkname like '%{1}%')) A WHERE RowNumber > {2}*({3}-1)", pagesize, keyword, pagesize, pageindex);
                }
                
                var datatable = SqlHelper.ExecuteTable(sql);
                List<int> tids = new List<int>();
                var searchTalkListModels=new List<SearchTalkListModel>();
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
                    GetTalkNum(tid, ref totalnum, ref usernum);

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
                                                             type = row["type"].ToString()
                                                         };
                    if (qs["tid"] != null)
                    {
                        searchTalkList.sn = row["glsn"] != null ? row["glsn"].ToString().Remove(0, 4) : "";
                    }
                    else
                    {
                        searchTalkList.sn = "";
                    }
                    searchTalkListModels.Add(searchTalkList);
                }
                //if (datatable.Rows.Count > 0)
                //{
                //    minitid = Int32.Parse(datatable.Rows[datatable.Rows.Count - 1]["id"].ToString());
                //}
                var jsonCommModel = new AppJsonResultModel<List<SearchTalkListModel>>((int) MessageCode.Success,MessageCodeDiscription.GetMessageCodeDiscription(MessageCode.Success), searchTalkListModels);
                var jsonstr = JsonConvert.SerializeObject(jsonCommModel);
                return jsonstr;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("获取频道列表" + e, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        #endregion

        #region 获取所在区域的公共频道
        //areaid 城市id（区域号），
        //senduid ，允许说话的用户id，0，允许所有人发言，-1，不允许所有人发言  
        //audiourl  :直播地址，
        //uploadurl  上传地址，dj，0，用户不在当前频道，1用户在当前频道

        /// <summary>
        /// 获取所在区域的公共频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetCurrentRadio(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,appid,ouid,token,areaid
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["areaid"] == null)
            {
                MediaService.WriteLog("接收到获取所在区域的公共频道 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到获取所在区域的公共频道 ：ouid =" + qs["ouid"].ToString() + "token =" + qs["token"].ToString() + "appid =" + qs["appid"].ToString() + "uid =" + qs["uid"].ToString() + "areaid =" + qs["areaid"].ToString(), MediaService.wirtelog);

                int areaid;
                if (!Int32.TryParse(qs["areaid"].ToString(), out areaid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;
                object obj = SqlHelper.ExecuteScalar("select pradio from [app_users] where uid='" + qs["uid"].ToString().Replace("'", "") + "'");
                if (obj != null)
                {
                    string pradio = obj.ToString() + ",";
                    StringBuilder sb = new StringBuilder();
                    string sql = "SELECT rid,channelname,producer,compere,imageurl,radiotype,thumburl,flashimageurl FROM wy_radio WHERE (areaid = " + areaid + " or areaid =0) and radiotype >0";
                    DataTable dt = SqlHelper.ExecuteTable(sql);
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string rid = dt.Rows[i]["rid"].ToString();
                        string channelname = dt.Rows[i]["channelname"].ToString();
                        string producer = dt.Rows[i]["producer"].ToString();
                        string compere = dt.Rows[i]["compere"].ToString();
                        string imageurl = dt.Rows[i]["imageurl"].ToString();
                        string radiotype = dt.Rows[i]["radiotype"].ToString();
                        string thumburl = dt.Rows[i]["thumburl"] == null ? "" : dt.Rows[i]["thumburl"].ToString();
                        string flashImageUrl = dt.Rows[i]["flashimageurl"].ToString();
                        if (radiotype != "2" || pradio.IndexOf(rid + ",") >= 0)
                        {
                            sb.Append(",{\"rid\":" + rid + ",\"channelname\":\"" + channelname + "\",\"producer\":\"" + producer + "\",\"compere\":\"" + compere + "\",\"totalnum\":" + 0 + ",\"usernum\":" + 0 + ",\"imageurl\":\"" + imageurl + "\",\"thumburl\":\"" + thumburl + "\",\"flashimageurl\":\"" + flashImageUrl + "\"}");
                        }
                    }
                    if (dt.Rows.Count > 0)
                    {
                        sb.Remove(0, 1);
                    }
                    return CommFunc.StandardListFormat(MessageCode.Success, sb.ToString());
                }
                else
                {
                    return CommFunc.StandardFormat(MessageCode.TalkNotExist);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, true);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        /// <summary>
        /// 新-获取所在区域的公共频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string NewGetCurrentRadio(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,appid,ouid,token,areaid
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["areaid"] == null)
            {
                MediaService.WriteLog("接收到NewGetCurrentRadio ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到NewGetCurrentRadio ：ouid =" + qs["ouid"] + "token =" + qs["token"] + "appid =" + qs["appid"] + "uid =" + qs["uid"] + "areaid =" + qs["areaid"], MediaService.wirtelog);

                int areaid;
                if (!Int32.TryParse(qs["areaid"], out areaid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["uid"], qs["appid"], qs["token"], ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;
                object obj = SqlHelper.ExecuteScalar("select pradio from [app_users] where uid='" + qs["uid"].Replace("'", "") + "'");
                if (obj != null)
                {
                    string pradio = obj + ",";
                    StringBuilder sb = new StringBuilder();
                    string sql = "SELECT rid,channelname,producer,compere,imageurl,radiotype,thumburl,flashimageurl FROM wy_radio WHERE (areaid = " + areaid + " or areaid =0) and radiotype>0 and radiotype<>3";
                    DataTable dt = SqlHelper.ExecuteTable(sql);
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string rid = dt.Rows[i]["rid"].ToString();
                        string channelname = dt.Rows[i]["channelname"].ToString();
                        string producer = dt.Rows[i]["producer"].ToString();
                        string compere = dt.Rows[i]["compere"].ToString();
                        string imageurl = dt.Rows[i]["imageurl"].ToString();
                        string radiotype = dt.Rows[i]["radiotype"].ToString();
                        string thumburl = dt.Rows[i]["thumburl"] == null ? "" : dt.Rows[i]["thumburl"].ToString();
                        string flashImageUrl = dt.Rows[i]["flashimageurl"].ToString();
                        if (radiotype != "2" || pradio.IndexOf(rid + ",") >= 0)
                        {
                            sb.Append(",{\"rid\":" + rid + ",\"channelname\":\"" + channelname + "\",\"producer\":\"" + producer + "\",\"compere\":\"" + compere + "\",\"totalnum\":" + 0 + ",\"usernum\":" + 0 + ",\"imageurl\":\"" + imageurl + "\",\"thumburl\":\"" + thumburl + "\",\"flashimageurl\":\"" + flashImageUrl + "\"}");
                        }
                    }
                    if (dt.Rows.Count > 0)
                    {
                        sb.Remove(0, 1);
                    }
                    return CommFunc.StandardListFormat(MessageCode.Success, sb.ToString());
                }
                else
                {
                    return CommFunc.StandardFormat(MessageCode.TalkNotExist);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, true);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 获取热门公共频道
        /// <summary>
        /// 获取热门公共频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetPopRadio(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,appid,ouid,token
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null)
            {
                MediaService.WriteLog("接收到获取热门公共频道 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到获取热门公共频道 ：ouid =" + qs["ouid"].ToString() + "token =" + qs["token"].ToString() + "appid =" + qs["appid"].ToString() + "uid =" + qs["uid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                StringBuilder sb = new StringBuilder();
                string sql = "select * from wy_radio where radiotype=1";
                DataTable dt = SqlHelper.ExecuteTable(sql);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string rid = dt.Rows[i]["rid"].ToString();
                    string channelname = dt.Rows[i]["channelname"].ToString();
                    string producer = dt.Rows[i]["producer"].ToString();
                    string compere = dt.Rows[i]["compere"].ToString();
                    string imageurl = dt.Rows[i]["imageurl"].ToString();
                    string radiotype = dt.Rows[i]["radiotype"].ToString();
                    string thumburl = dt.Rows[i]["thumburl"] == null ? "" : dt.Rows[i]["thumburl"].ToString();
                    string flashImageUrl = dt.Rows[i]["flashimageurl"].ToString();

                    sb.Append(",{\"rid\":" + rid + ",\"channelname\":\"" + channelname + "\",\"producer\":\"" + producer + "\",\"compere\":\"" + compere + "\",\"totalnum\":" + 0 + ",\"usernum\":" + 0 + ",\"imageurl\":\"" + imageurl + "\",\"thumburl\":\"" + thumburl + "\",\"flashimageurl\":\"" + flashImageUrl + "\"}");
                }
                if (dt.Rows.Count > 0)
                {
                    sb.Remove(0, 1);
                }
                return CommFunc.StandardListFormat(MessageCode.Success, sb.ToString());
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, true);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 用户加入频道
        /// <summary>
        /// 用户加入频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string UserJoinTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,appid,ouid,token,talkname,[auth]
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["talkname"] == null)
            {
                MediaService.WriteLog("接收到用户加入频道 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到用户加入频道 ：ouid =" + qs["ouid"].ToString() + "token =" + qs["token"].ToString() + "appid =" + qs["appid"].ToString() + "uid =" + qs["uid"].ToString() + "talkname =" + qs["talkname"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                string auth = qs["auth"] == null ? "" : qs["auth"].Replace("'", "");
                string talkname = qs["talkname"] == null ? "" : qs["talkname"].Replace("'", "");

                if (PublicClass.FindTalkType(talkname) == 2)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkJoinFaild);
                }
                return UserJoinTalk(talkname, auth, uid);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        /// <summary>
        /// GoloZ用户加入频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GoloZUserJoinTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * sn,talkname,[auth]
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["sn"] == null || qs["talkname"] == null)
            {
                MediaService.WriteLog("接收到GoloZ用户加入频道 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到GoloZ用户加入频道 ：sn =" + qs["sn"].ToString() + "&talkname =" + qs["talkname"].ToString(), MediaService.wirtelog);

                string sn = qs["sn"].ToString().Replace("'", "");
                string sql = "select uid from app_users where glsn='" + CommFunc.GetUniform12(sn) + "'";
                object obj = SqlHelper.ExecuteScalar(sql);
                if (obj == null)
                    return CommFunc.StandardFormat(MessageCode.DeviceNotExist);
                int uid = (int)obj;

                string auth = qs["auth"] == null ? "" : qs["auth"].Replace("'", "");
                string talkname = qs["talkname"] == null ? "" : qs["talkname"].Replace("'", "");

                if (PublicClass.FindTalkType(talkname) == 2)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkJoinFaild);
                }

                StringBuilder sb = new StringBuilder();
                string sqltalkname = "select tid,auth from wy_talk where talkname='" + talkname + "'";
                DataTable dt = SqlHelper.ExecuteTable(sqltalkname);
                if (dt.Rows.Count < 1)
                    return CommFunc.StandardFormat(MessageCode.TalkNotExist);

                string stid = dt.Rows[0]["tid"].ToString();
                string sauth = dt.Rows[0]["auth"].ToString();
                if (sauth != auth)
                    return CommFunc.StandardFormat(MessageCode.TalkVerificationFaild);
                bool isSucceed = SendToGoloZTalkInfo(uid, stid, CommType.forceUserJoinTalk, false);
                if (!isSucceed)
                    return CommFunc.StandardFormat(MessageCode.TalkJoinFaild);
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        internal static string UserJoinTalk(string talkname, string auth, int uid, short comm = CommType.userJoinPersonTalk)
        {
            //string data = PublicClass.JoinTalk(uid, auth, talkname);
            string data = PublicClass.JoinTalkLimit(uid, auth, talkname);
            string status = CommFunc.GetJsonValue(data, "status", ",", false);
            string message = CommFunc.GetJsonValue(data, "message", "\"", true);
            if (status == "false")
                return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, message);

            #region 获取频道信息
            string tid = "";
            StringBuilder sb = new StringBuilder();
            string sql = "select T1.id,T1.tid,T1.xuhao,T1.duijiang,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.imageurl,T2.type,T2.talkmode from (select top 20 id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid = " + uid + " order by id desc) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid and T2.talkname='" + talkname + "' order by T1.id desc";
            DataTable dt = SqlHelper.ExecuteTable(sql);
            if (dt.Rows.Count < 1)
                return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, message);

            tid = dt.Rows[0]["tid"].ToString();
            talkname = dt.Rows[0]["talkname"].ToString();
            auth = dt.Rows[0]["auth"].ToString();
            string remark = dt.Rows[0]["remark"].ToString();
            string create = "false";
            if (dt.Rows[0]["createuid"].ToString() == uid.ToString())
                create = "true";
            string imageurl = dt.Rows[0]["imageurl"] == null ? "" : dt.Rows[0]["imageurl"].ToString();
            string type = dt.Rows[0]["type"].ToString();
            int totalnum = 0;
            int usernum = 0;
            GetTalkNum(Convert.ToInt32(tid), ref totalnum, ref usernum);
            var talkmode = dt.Rows[0]["talkmode"].ToString();
            sb.Append("{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\",\"remark\":\"" + remark + "\",\"create\":" + create + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + ",\"imageurl\":\"" + imageurl + "\",\"type\":\"" + type + "\",\"talkmode\":\"" + talkmode + "\"}");
            #endregion

            //通知Goloz
            if (!string.IsNullOrWhiteSpace(tid))
                SendToGoloZTalkInfo(uid, tid, comm);
            else
                MediaService.WriteLog("用户加入频道推送到GoloZ消息失败。", MediaService.wirtelog);

            return CommFunc.StandardObjectFormat(MessageCode.Success, sb.ToString());
        }

        /// <summary>
        /// 新-用户加入频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string NewUserJoinTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,appid,ouid,token,talkname,[auth]
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["talkname"] == null)
            {
                MediaService.WriteLog("接收到NewUserJoinTalk ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到NewUserJoinTalk ：ouid =" + qs["ouid"] + "token =" + qs["token"] + "appid =" + qs["appid"] + "uid =" + qs["uid"] + "talkname =" + qs["talkname"], MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["uid"], qs["appid"], qs["token"], ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                string auth = qs["auth"] == null ? "" : qs["auth"].Replace("'", "");
                string talkname = qs["talkname"] == null ? "" : qs["talkname"].Replace("'", "");

                if (PublicClass.FindTalkType(talkname) == 2)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, "目标为企业频道");
                }

                #region return UserJoinTalk(talkname, auth, uid)

                #region PublicClass.JoinTalk(uid, auth, talkname)
                DataTable dt = SqlHelper.ExecuteTable("select tid,auth from [wy_talk] where talkname='" + talkname + "'");
                string tid;
                if (dt.Rows.Count > 0)
                {
                    tid = dt.Rows[0]["tid"].ToString();
                    if (dt.Rows[0]["auth"].ToString() == "" || dt.Rows[0]["auth"].ToString() == auth)
                    {
                        Object obj = SqlHelper.ExecuteScalar("select 1 from [wy_talkuser] where tid=" + tid + " and uid=" + uid);
                        if (obj == null)
                        {
                            SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid) values (" + tid + "," + uid + ");update [wy_talk] set usernum=usernum+1 where tid=" + tid);
                        }
                        else
                        {
                            return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, "重复加入");
                        }
                    }
                    else
                    {
                        if (auth == "")
                            return CommFunc.StandardFormat(MessageCode.TalkVerificationFaild, "需要输入群组验证码");
                        else
                            return CommFunc.StandardFormat(MessageCode.TalkVerificationFaild, "输入的群组验证码错误");
                    }
                }
                else
                {
                    return CommFunc.StandardFormat(MessageCode.TalkNotExist);
                }

                #endregion

                #region 获取频道信息
                tid = "";
                StringBuilder sb = new StringBuilder();
                string sql = "select T1.id,T1.tid,T1.xuhao,T1.duijiang,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.imageurl,T2.[type],T1.sharelocation from (select top 20 id,tid,xuhao,duijiang,remark,sharelocation from [wy_talkuser] where [uid] = " + uid + " order by id desc) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid and T2.talkname='" + talkname + "' order by T1.id desc";
                dt = SqlHelper.ExecuteTable(sql);
                if (dt.Rows.Count < 1)
                    return CommFunc.StandardFormat(MessageCode.TalkJoinFaild);

                tid = dt.Rows[0]["tid"].ToString();
                talkname = dt.Rows[0]["talkname"].ToString();
                auth = dt.Rows[0]["auth"].ToString();
                string remark = dt.Rows[0]["remark"].ToString();
                string create = "false";
                if (dt.Rows[0]["createuid"].ToString() == uid.ToString())
                    create = "true";
                string imageurl = dt.Rows[0]["imageurl"] == null ? "" : dt.Rows[0]["imageurl"].ToString();
                string type = dt.Rows[0]["type"].ToString();
                int sharelocation;
                int.TryParse(dt.Rows[0]["sharelocation"].ToString(), out sharelocation);
                int totalnum = 0;
                int usernum = 0;
                GetTalkNum(Convert.ToInt32(tid), ref totalnum, ref usernum);
                sb.Append("{\"tid\":" + tid + ",\"talkname\":\"" + talkname + "\",\"auth\":\"" + auth + "\",\"remark\":\"" + remark + "\",\"create\":" + create + ",\"usernum\":" + usernum + ",\"totalnum\":" + totalnum + ",\"imageurl\":\"" + imageurl + "\",\"type\":\"" + type + "\",\"sharelocation\":" + sharelocation + "}");
                #endregion

                //通知Goloz
                if (!string.IsNullOrWhiteSpace(tid))
                    SendToGoloZTalkInfo(uid, tid, CommType.userJoinPersonTalk);
                else
                    MediaService.WriteLog("用户加入频道推送到GoloZ消息失败。", MediaService.wirtelog);

                PublicClass.SendToOnlineUserList(null, sb.ToString(), "", new List<int>(uid), 99, 0, CommType.getUsernumInTalk, CommFunc.APPID);

                return CommFunc.StandardObjectFormat(MessageCode.Success, sb.ToString());
                #endregion
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        /// <summary>
        /// 用户批量加入频道 不验证token
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string UserJoinTalkWithOutToken(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * string[] ouids , tid 
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouids"] == null || qs["tid"] == null)
            {
                MediaService.WriteLog("接收到UserJoinTalkWithOutToken：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到UserJoinTalkWithOutToken ：ouids =" + qs["ouids"] + "tid =" + qs["tid"], MediaService.wirtelog);

                int _tid;
                int.TryParse(qs["tid"], out _tid);
                if (PublicClass.FindTalkType(_tid) == 2)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, "目标为企业频道");
                }

                #region return UserJoinTalk(talkname, auth, uid)
                string[] strOuids = qs["ouids"].Split(',');
                var ouidlist = strOuids.Aggregate("", (current, variable) => current + ("," + "'" + variable + "'")).Remove(0, 1);

                ThreadPool.QueueUserWorkItem(s =>
                {
                    MediaService.WriteLog("接收到UserJoinTalkWithOutToken：" + ouidlist, MediaService.wirtelog);
                    #region 此处为了手机app和设备之间也可以对讲,所以也需要保存ouid到app_talkuser表
                    StringBuilder sb = new StringBuilder();
                    foreach (var strOuid in strOuids)
                    {
                        Object obj =
                            SqlHelper.ExecuteScalar("select 1 from [wy_talkuser] where tid=" + _tid + " and uid=" + strOuid);
                        if (obj == null)
                        {
                            sb.Append("insert [wy_talkuser] (tid,uid,uidtype) values (" + _tid + "," + strOuid + "," + 1 +");update [wy_talk] set usernum=usernum+1 where tid=" + _tid);
                        }
                    }
                    SqlHelper.ExecuteNonQuery(sb.ToString());

                    #endregion
                    var table = SqlHelper.ExecuteTable("select uid from wy_uidmap where ouid in ( " + ouidlist + " )"); //根据ouid查到绑定的uid
                    if (table != null)
                    {
                        foreach (DataRow row in table.Rows) //遍历当前ouid下绑定的uid
                        {
                            var uid = Convert.ToInt32(row["uid"].ToString());
                            Object obj =
                                SqlHelper.ExecuteScalar("select 1 from [wy_talkuser] where tid=" + _tid + " and uid=" + uid);
                            if (obj == null)
                            {
                                SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid) values (" + _tid + "," + uid + ");update [wy_talk] set usernum=usernum+1 where tid=" + _tid);
                            }
                            else
                            {
                                MediaService.WriteLog("接收到UserJoinTalkWithOutToken 重复加入：tid =" + _tid + "uid =" + uid,
                                    MediaService.wirtelog);
                                continue;
                            }

                            //通知Goloz
                            if (_tid != 0)
                                SendToGoloZTalkInfo(uid, _tid.ToString(), CommType.userJoinPersonTalk);
                            else
                                MediaService.WriteLog("用户加入频道推送到GoloZ消息失败。", MediaService.wirtelog);
                        }
                    }
                });

                return CommFunc.StandardFormat(MessageCode.Success);
                #endregion
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        //批量用户加入频道(验证token)  自驾游app调用
        public static string UserJoinTalkWithToken(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * appid,token, ouids , tid ,[auth],[uid]
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouids"] == null || qs["tid"] == null || qs["appid"] == null || qs["token"] == null)
            {
                MediaService.WriteLog("接收到UserJoinTalkWithToken：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog(
                    "接收到UserJoinTalkWithToken ：ouids =" + qs["ouids"] + "tid =" + qs["tid"] + "appid =" + qs["appid"] +
                    "token =" + qs["token"] + "uid =" + qs["uid"], MediaService.wirtelog);
                string auth = qs["auth"] == null ? "" : qs["auth"].Replace("'", "");
                int _tid;
                int.TryParse(qs["tid"], out _tid);
                if (PublicClass.FindTalkType(_tid) == 2)
                {
                    return CommFunc.StandardFormat(MessageCode.TalkJoinFaild, "目标为企业频道");
                }
                int _uid = 0;
                if (qs["uid"] != null && !int.TryParse(qs["uid"], out _uid))
                {
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                }

                //验证token  app调用时要验证token
                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouids"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;
                //end

                #region return UserJoinTalk(talkname, auth, uid)

                string[] strOuids = qs["ouids"].Split(',');
                var ouidlist =
                    strOuids.Aggregate("", (current, variable) => current + ("," + "'" + variable + "'")).Remove(0, 1);
                DataTable dt = SqlHelper.ExecuteTable("select tid,auth from [wy_talk] where tid='" + _tid + "'");
                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["auth"].ToString() == "" || dt.Rows[0]["auth"].ToString() == auth)
                    {

                        MediaService.WriteLog("接收到UserJoinTalkWithToken：" + ouidlist, MediaService.wirtelog);

                        #region 此处为了手机app和设备之间也可以对讲,所以也需要保存ouid到app_talkuser表

                        StringBuilder sb = new StringBuilder();
                        foreach (var strOuid in strOuids)
                        {
                            Object obj =SqlHelper.ExecuteScalar("select 1 from [wy_talkuser] where tid=" + _tid +" and uid=" + strOuid);
                            if (obj == null)
                            {
                                sb.Append("insert [wy_talkuser] (tid,uid,uidtype) values (" + _tid + "," + strOuid +"," + 1 + ");update [wy_talk] set usernum=usernum+1 where tid=" + _tid);
                            }
                        }
                        if (sb.ToString() != string.Empty)
                            SqlHelper.ExecuteNonQuery(sb.ToString());

                        #endregion

                        if (_uid != 0)
                        {
                            Object obj =SqlHelper.ExecuteScalar("select 1 from [wy_talkuser] where tid=" + _tid +" and uid=" + _uid);
                            if (obj == null)
                            {
                                SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid) values (" + _tid + "," + _uid +");update [wy_talk] set usernum=usernum+1 where tid=" + _tid);
                                SendToGoloZTalkInfo(_uid, _tid.ToString(), CommType.userJoinPersonTalk);
                            }
                            else
                            {
                                return CommFunc.StandardFormat(MessageCode.UidExistedInWyTalk);
                            }
                        }
                        else
                        {
                            var table =
                                SqlHelper.ExecuteTable("select uid from wy_uidmap where ouid in ( " + ouidlist + " )");
                            //根据ouid查到绑定的uid
                            if (table != null)
                            {
                                foreach (DataRow row in table.Rows) //遍历当前ouid下绑定的uid
                                {
                                    ThreadPool.QueueUserWorkItem(s =>
                                    {
                                        var uid = Convert.ToInt32(row["uid"].ToString());
                                        Object obj =
                                            SqlHelper.ExecuteScalar("select 1 from [wy_talkuser] where tid=" + _tid +" and uid=" + uid);
                                        if (obj == null)
                                        {
                                            SqlHelper.ExecuteNonQuery("insert [wy_talkuser] (tid,uid) values (" + _tid +"," + uid +");update [wy_talk] set usernum=usernum+1 where tid=" +_tid);
                                            //通知Goloz
                                            if (_tid != 0)
                                                SendToGoloZTalkInfo(uid, _tid.ToString(), CommType.userJoinPersonTalk);
                                            else
                                                MediaService.WriteLog("用户加入频道推送到GoloZ消息失败。", MediaService.wirtelog);
                                        }
                                        else
                                        {
                                            MediaService.WriteLog(
                                                "接收到UserJoinTalkWithOutToken 重复加入：tid =" + _tid + "uid =" + uid,MediaService.wirtelog);
                                        }
                                    });
                                }
                            }
                        }
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

                #endregion
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        #endregion

        #region 修改频道的信息
        /// <summary>
        /// 修改频道的信息
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string ModifyTalkMessage(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,appid,ouid,token,tid,[auth],[remark]
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["tid"] == null)
            {
                MediaService.WriteLog("接收到修改频道的信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到修改频道的信息 ：ouid =" + qs["ouid"].ToString() + "token =" + qs["token"].ToString() + "appid =" + qs["appid"].ToString() + "uid =" + qs["uid"].ToString() + "tid =" + qs["tid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                string tid = qs["tid"].ToString().Replace("'", "");
                object obj = SqlHelper.ExecuteScalar("select auth from [wy_talk] where tid='" + tid + "' and createuid=" + uid);
                StringBuilder sb = new StringBuilder();
                sb.Append("{\"status\":true,\"tid\":" + tid);
                if (obj != null)
                {
                    if (qs["auth"] != null)
                    {
                        string auth = qs["auth"].ToString().Replace(",", "");
                        if ((auth.Length == 3 && CommBusiness.IsValiNum(auth) && obj.ToString() != auth) || (auth == ""))
                        {
                            SqlHelper.ExecuteNonQuery("update [wy_talk] set auth='" + qs["auth"].Replace("'", "") + "',usernum=1 where tid='" + tid + "';delete [wy_talkuser] where tid='" + tid + "' and uid!='" + uid + "'");
                            SqlHelper.ExecuteNonQuery("UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid);
                            sb.Append(",\"auth\":\"" + auth + "\"");
                        }
                        TalkMessage talkmessage = null;
                        if (MediaService.talkDic.TryRemove(Int32.Parse(tid), out talkmessage))
                        {
                            UserObject uo = null;
                            foreach (int oruid in talkmessage.uidlist)
                            {
                                if (uid != oruid)
                                {
                                    try
                                    {
                                        if (MediaService.userDic.TryGetValue(oruid, out uo))
                                        {
                                            if (uo.socket != null && uo.socket[CommFunc.APPID] != null)
                                            {
                                                if (uo.socket[CommFunc.APPID].Connected)
                                                {
                                                    byte[] b = new byte[12];
                                                    Buffer.BlockCopy(System.BitConverter.GetBytes((short)12), 0, b, 0, 2);
                                                    Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                                                    Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                                                    Buffer.BlockCopy(System.BitConverter.GetBytes(Int32.Parse(tid)), 0, b, 8, 4);
                                                    uo.socket[CommFunc.APPID].Send(b, SocketFlags.None);
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
                if (qs["remark"] != null)
                {
                    sb.Append(",\"remark\":\"" + qs["remark"].ToString().Replace("'", "") + "\"");
                    SqlHelper.ExecuteNonQuery("update [wy_talkuser] set remark='" + qs["remark"].Replace("'", "").Trim() + "' where tid=" + tid + " and uid=" + uid);

                    object _obj =SqlHelper.ExecuteScalar(string.Format("select type,createuid from [wy_talk] where tid='{0}' and createuid={1} and type=4", tid,uid));
                    if (_obj != null)
                    {
                        SqlHelper.ExecuteNonQuery(string.Format("update [wy_talk] set talknotice='{0}' where tid={1}",qs["remark"].Replace("'", "").Trim(), tid)); //用户更新频道信息时如果此频道是创建者,同时更新talk的notice. 为了兼容老版本.
                    }
                }

                sb.Append("}");
                PublicClass.SendToOnlineUserList(null, sb.ToString(), "", new List<int>() { uid }, 99, 0, CommType.modifyUserTalk, CommFunc.APPID);
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 解除我的频道/退出我所加入的频道
        /// <summary>
        /// 解除我的频道/退出我所加入的频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string UserQuitTalk(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,appid,ouid,token,tid
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["tid"] == null)
            {
                MediaService.WriteLog("接收到解除我的频道/退出我所加入的频道 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到解除我的频道/退出我所加入的频道 ：ouid =" + qs["ouid"].ToString() + "token =" + qs["token"].ToString() + "appid =" + qs["appid"].ToString() + "uid =" + qs["uid"].ToString() + "tid =" + qs["tid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
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

                //if (PublicClass.FindTalkType(tid) == 2)
                //{
                //    return CommFunc.StandardFormat(MessageCode.TalkNotExist);
                //}

                return UserQuitTalk(tid, uid);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        public static string UserQuitTalkWithOutToken(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * ouids,tid
            */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouids"] == null || qs["tid"] == null)
            {
                MediaService.WriteLog("接收到解除我的频道/退出我所加入的频道不验证token：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到解除我的频道/退出我所加入的频道不验证token ：ouids =" + qs["ouids"] + "tid =" + qs["tid"],
                    MediaService.wirtelog);

                string[] strOuids = qs["ouids"].Split(',');
                var ouidlist = strOuids.Aggregate("", (current, variable) => current + ("," + "'" + variable + "'")).Remove(0, 1);
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

                ThreadPool.QueueUserWorkItem(s =>
                {
                    StringBuilder sb = new StringBuilder();
                    MediaService.WriteLog("ouids =" + qs["ouids"] + "ouidlist =" + ouidlist,
                    MediaService.wirtelog);
                    if ("-100" == qs["ouids"]) //如果是管理员,就解散频道
                    {
                        sb.Append("delete [wy_talk] where tid='" + tid + "';");
                        sb.Append("select uid from [wy_talkuser] where tid='" + tid + "';");
                        var table = SqlHelper.ExecuteTable(sb.ToString());
                        List<int> uids = new List<int>();
                        foreach (DataRow row in table.Rows) //遍历当前ouid下绑定的uid 并做退出操作
                        {
                            var uid = Convert.ToInt32(row["uid"].ToString());
                            UserObject uo = null;
                            if (MediaService.userDic.TryGetValue(uid, out uo))
                            {
                                if (uo.socket != null && uo.socket[CommFunc.APPID] != null)
                                {
                                    if (uo.socket[CommFunc.APPID].Connected)
                                    {
                                        byte[] b = new byte[12];
                                        Buffer.BlockCopy(System.BitConverter.GetBytes((short)12), 0, b, 0, 2);
                                        Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                                        Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                                        Buffer.BlockCopy(System.BitConverter.GetBytes(tid), 0, b, 8, 4);
                                        uo.socket[CommFunc.APPID].Send(b, SocketFlags.None);
                                    }
                                }
                            }
                            uids.Add(uid);
                        }
                        string tmprecv = "{\"status\":true,\"tid\":" + tid + ",\"createuid\":" + ouidlist + "}";
                        PublicClass.SendToOnlineUserList(null, tmprecv, "", uids, 99, 0, CommType.userQuitTalk, CommFunc.APPID);
                        sb.Clear();
                        sb.Append("delete [wy_talkuser] where tid='" + tid + "';");
                        SqlHelper.ExecuteNonQuery(sb.ToString());
                    }
                    else
                    {
                        var table = SqlHelper.ExecuteTable("select uid from wy_uidmap where ouid in ( " + ouidlist + " )"); //根据ouid查到绑定的uid
                        //根据ouid查到绑定的uid
                        MediaService.WriteLog("..... ouids" + ouidlist, MediaService.wirtelog);
                        if (table != null)
                        {
                            #region 此处为了手机app和设备之间也可以对讲,所以也需要保存ouid到app_talkuser表,退出的时候也删除

                            sb.Clear();
                            foreach (var ouid in strOuids)
                            {
                                Object obj =
                                SqlHelper.ExecuteScalar("select 1 from [wy_talkuser] where tid=" + tid + " and uid=" + ouid);
                                if (obj != null)
                                {
                                    sb.Append("delete [wy_talkuser] where tid='" + tid + "' and uid=" + ouid + ";");
                                    sb.Append("UPDATE wy_talk SET usernum=usernum-1 WHERE tid =' " + tid + "';");
                                }
                            }
                            SqlHelper.ExecuteNonQuery(sb.ToString());
                            #endregion

                            foreach (DataRow row in table.Rows) //遍历当前ouid下绑定的uid 并做退出操作
                            {
                                var uid = Convert.ToInt32(row["uid"].ToString());
                                UserQuitTalk(tid, uid);
                            }
                        }
                    }
                });

                MediaService.WriteLog("解除我的频道/退出我所加入的频道不验证token end.....", MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        internal static string UserQuitTalk(int tid, int uid)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT createuid FROM wy_talk WHERE tid = " + tid);
            object result = SqlHelper.ExecuteScalar(sb.ToString());
            if (result == null)
                return CommFunc.StandardFormat(MessageCode.TalkNotExist);
            int cuid = (int)result;

            sb.Clear();
            sb.Append("SELECT count(*) FROM wy_talkuser,wy_talk WHERE wy_talk.tid=wy_talkuser.tid AND wy_talk.tid = " + tid + " AND wy_talkuser.uid=" + uid);
            result = SqlHelper.ExecuteScalar(sb.ToString());
            if (result != null && ((int)result) > 0)
            {
                if (cuid == uid)
                {
                    sb.Clear();
                    List<int> uids = new List<int>();
                    sb.Append("SELECT uid FROM wy_talkuser where tid=" + tid);
                    DataTable uidlist = SqlHelper.ExecuteTable(sb.ToString());
                    if (uidlist != null && uidlist.Rows.Count > 0)
                    {
                        foreach (DataRow dr in uidlist.Rows)
                        {
                            uids.Add(Convert.ToInt32(dr["uid"].ToString()));
                        }
                    }
                    sb.Clear();
                    sb.Append("delete [wy_talk] where tid='" + tid + "';");
                    sb.Append("delete [wy_talkuser] where tid='" + tid + "';");
                    sb.Append("UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid);
                    SqlHelper.ExecuteNonQuery(sb.ToString());

                    TalkMessage talkmessage = null;
                    if (MediaService.talkDic.TryRemove(tid, out talkmessage))
                    {
                        foreach (int oruid in uids)
                        {
                            try
                            {
                                UserObject uo = null;
                                if (MediaService.userDic.TryGetValue(oruid, out uo))
                                {
                                    if (uo.socket != null && uo.socket[CommFunc.APPID] != null)
                                    {
                                        if (uo.socket[CommFunc.APPID].Connected)
                                        {
                                            byte[] b = new byte[12];
                                            Buffer.BlockCopy(System.BitConverter.GetBytes((short)12), 0, b, 0, 2);
                                            Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                                            Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                                            Buffer.BlockCopy(System.BitConverter.GetBytes(tid), 0, b, 8, 4);
                                            uo.socket[CommFunc.APPID].Send(b, SocketFlags.None);
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    sb.Clear();
                    sb.Append("UPDATE wy_talk SET usernum=usernum-1 WHERE tid =' " + tid + "';");
                    sb.Append("delete [wy_talkuser] where tid='" + tid + "' and uid=" + uid + ";");
                    sb.Append("UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid);
                    SqlHelper.ExecuteNonQuery(sb.ToString());
                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        if (uo.socket != null && uo.socket[CommFunc.APPID] != null)
                        {
                            if (uo.socket[CommFunc.APPID].Connected)
                            {
                                byte[] b = new byte[12];
                                Buffer.BlockCopy(System.BitConverter.GetBytes((short)12), 0, b, 0, 2);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.userNotInTalk), 0, b, 2, 2);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, b, 4, 4);
                                Buffer.BlockCopy(System.BitConverter.GetBytes(tid), 0, b, 8, 4);
                                uo.socket[CommFunc.APPID].Send(b, SocketFlags.None);
                            }
                        }
                    }
                }
                SqlHelper.ExecuteNonQuery("UPDATE app_users SET  updatetime = GETDATE() WHERE uid = " + uid);

                string recv = "{\"status\":true,\"tid\":" + tid + ",\"createuid\":" + cuid + "}";
                PublicClass.SendToOnlineUserList(null, recv, "", new List<int>() { uid }, 99, 0, CommType.userQuitTalk, CommFunc.APPID);

                var talkinfo = CommFunc.GetTalkInfoNum(tid);
                PublicClass.SendToOnlineUserList(null, talkinfo, "", new List<int>() { uid }, 99, 0, CommType.userQuitTalk, CommFunc.APPID);
            }
            return CommFunc.StandardFormat(MessageCode.Success);
        }
        #endregion

        #region 修改图片地址
        /// <summary>
        /// 修改图片地址
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string ModifyImageUrl(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * appid,ouid,token,imageurl
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["imageurl"] == null)
            {
                MediaService.WriteLog("接收到ModifyImageUrl ：" + recv, MediaService.wirtelog);
                return recv;
            }
            StringBuilder log = new StringBuilder("接收到ModifyImageUrl ：ouid=" + qs["ouid"] + "&token=" + qs["token"] + "&appid=" + qs["appid"] + "&imageurl=" + qs["imageurl"]);
            try
            {
                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                {
                    log.Append(" recv=").Append(recv);
                    MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
                    return recv;
                }

                string imageurl = qs["imageurl"].Replace("'", "");
                string sql = string.Format("update wy_talk set wy_talk.imageurl='{0}' from wy_talk,wy_uidmap where wy_talk.createuid=wy_uidmap.uid and wy_uidmap.ouid={1}", imageurl, ouid);
                SqlHelper.ExecuteNonQuery(sql);
                const string sqlStr = "UPDATE wy_user SET face_url=@face_url WHERE [user_id]=@user_id";
                SqlParameter[] paras =
                {
                    new SqlParameter("@user_id", ouid)
                    ,new SqlParameter("@face_url", imageurl)
                };
                SqlHelper.ExecuteNonQuery(sqlStr, paras);
                recv = CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                log.Append("执行异常：").Append(e);
                recv = CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 获取电台列表
        /// <summary>
        /// 获取电台列表
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetRadioStation(NameValueCollection qs)
        {
            try
            {
                #region php网站不能提供验证信息，但是需要获取电台列表
                //            string recv = StandardFormat(MessageCode.MissKey);
                //            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
                //            {
                //                MediaService.WriteLog("接收到GetRadioStation ：" + recv, MediaService.wirtelog);
                //                return recv;
                //            }
                MediaService.WriteLog("接收到GetRadioStation ：ouid =" + qs["ouid"] + "token =" + qs["token"] + "appid =" + qs["appid"], MediaService.wirtelog);
                //                int ouid = 0;
                //                bool isVerToken = UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid,  ref recv);
                //                if (!isVerToken)
                //                    return recv; 
                #endregion

                StringBuilder sb = new StringBuilder();
                string sql = "select * from wy_radio where radiotype=3";
                DataTable dt = SqlHelper.ExecuteTable(sql);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string rid = dt.Rows[i]["rid"].ToString();
                    string channelname = dt.Rows[i]["channelname"].ToString();
                    string producer = dt.Rows[i]["producer"].ToString();
                    string compere = dt.Rows[i]["compere"].ToString();
                    string imageurl = dt.Rows[i]["imageurl"].ToString();
                    string thumburl = dt.Rows[i]["thumburl"] == null ? "" : dt.Rows[i]["thumburl"].ToString();
                    string flashImageUrl = dt.Rows[i]["flashimageurl"].ToString();
                    string appimageurl = "";
                    string actname = "";
                    if (rid.Equals("328"))
                    {
                        appimageurl = "http://www.golo365.com/channel/happyway_3.png";
                        actname = GetRealtimeActName();
                    }
                    sb.Append(",{\"rid\":" + rid + ",\"channelname\":\"" + channelname + "\",\"producer\":\"" + producer + "\",\"compere\":\"" + compere + "\",\"totalnum\":" + 0 + ",\"usernum\":" + 0 + ",\"imageurl\":\"" + imageurl + "\",\"thumburl\":\"" + thumburl + "\",\"flashimageurl\":\"" + flashImageUrl + "\",\"appimageurl\":\"" + appimageurl + "\",\"actname\":\"" + actname + "\"}");
                }
                if (dt.Rows.Count > 0)
                {
                    sb.Remove(0, 1);
                }
                return CommFunc.StandardListFormat(MessageCode.Success, sb.ToString());
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, true);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        private static string GetRealtimeActName()
        {
            var time = DateTime.Now;
            var dayOfWeek = time.DayOfWeek;
            if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                return "";

            int h = time.Hour;
            int m = time.Minute;
            string actName = "";
            switch (h)
            {
                case 7:
                    if (m >= 30 && m <= 56)
                        actName = "轱辘早天下";
                    break;
                case 8:
                    if (m <= 50)
                        actName = "快乐早班车";
                    break;
                case 9:
                    if (m <= 30)
                        actName = "华商启示录";
                    break;
                case 10:
                    if (m <= 30)
                        actName = "翻唱音乐会";
                    break;
                case 11:
                    if (m < 40) actName = "体坛烩";
                    else actName = "食全食美";
                    break;
                case 12:
                    if (m <= 10)
                        actName = "食全食美";
                    break;
                case 13:
                    if (m >= 30)
                        actName = "包公笑传";
                    break;
                case 14:
                    if (m <= 10)
                        actName = "包公笑传";
                    else if (m >= 30) actName = "音乐下午茶";
                    break;
                case 15:
                    if (m >= 30)
                        actName = "健康新知";
                    break;
                case 16:
                    actName = "娱乐那点儿事";
                    break;
                case 17:
                    if (m < 20)
                        actName = "娱乐那点儿事";
                    else if (m >= 30)
                        actName = "越说越有趣";
                    break;
                case 18:
                    if (m < 30) actName = "越说越有趣";
                    else if (m >= 40) actName = "疯狂抢麦";
                    break;
                case 19:
                    if (m < 30) actName = "疯狂抢麦";
                    break;
                case 20:
                    if (m <= 30) actName = "怀旧金曲";
                    else actName = "yo豆你玩儿";
                    break;
                case 21:
                    if (m < 30) actName = "yo豆你玩儿";
                    break;
                case 22:
                    actName = "凡人故事";
                    break;
                case 23:
                    if (m < 30) actName = "凡人故事";
                    break;
            }
            return actName;
        }
        #endregion

        #region 是否开启频道位置共享
        /// <summary>
        /// 是否开启频道位置共享
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string SwitchShareLoc(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * uid,appid,ouid,token,tid,sharelocation
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["uid"] == null || qs["tid"] == null || qs["sharelocation"] == null)
            {
                MediaService.WriteLog("接收到SwitchShareLoc ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到SwitchShareLoc ：ouid =" + qs["ouid"] + " token =" + qs["token"] + " appid =" + qs["appid"] + " uid =" + qs["uid"] + " tid =" + qs["tid"] + " sharelocation =" + qs["sharelocation"], MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["uid"], qs["appid"], qs["token"], ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                int sharelocation, tid;
                int.TryParse(qs["tid"], out tid);
                if (tid > 0 && int.TryParse(qs["sharelocation"], out sharelocation))
                {
                    string sql = string.Format("UPDATE wy_talkuser SET sharelocation={0} WHERE tid={1} AND [uid]={2}",
                        sharelocation.ToString(), tid.ToString(), uid.ToString());
                    if (SqlHelper.ExecuteNonQuery(sql) > 0)
                    {
                        return CommFunc.StandardFormat(MessageCode.Success);
                    }
                    return CommFunc.StandardFormat(MessageCode.DefaultError, "Target does not exist!");
                }
                return CommFunc.StandardFormat(MessageCode.FormatError);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 网站获取公共频道
        /// <summary>
        /// 网站获取公共频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetRadioForPhp(NameValueCollection qs)
        {
            try
            {
                MediaService.WriteLog("接收到GetRadioForPhp", MediaService.wirtelog);

                StringBuilder sb = new StringBuilder();
                string sql = "select * from wy_radio where prid =0 and radiotype<>2";
                DataTable dt = SqlHelper.ExecuteTable(sql);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string rid = dt.Rows[i]["rid"].ToString();
                    string channelname = dt.Rows[i]["cityname"].ToString();
                    if ((channelname + "").Length == 0)
                    {
                        channelname = dt.Rows[i]["channelname"].ToString();
                    }
                    string producer = dt.Rows[i]["producer"].ToString();
                    string compere = dt.Rows[i]["compere"].ToString();
                    string imageurl = dt.Rows[i]["imageurl"].ToString();
                    string thumburl = dt.Rows[i]["thumburl"] == null ? "" : dt.Rows[i]["thumburl"].ToString();
                    string flashImageUrl = dt.Rows[i]["flashimageurl"].ToString();

                    sb.Append(",{\"rid\":" + rid + ",\"channelname\":\"" + channelname + "\",\"producer\":\"" + producer + "\",\"compere\":\"" + compere + "\",\"totalnum\":" + 0 + ",\"usernum\":" + 0 + ",\"imageurl\":\"" + imageurl + "\",\"thumburl\":\"" + thumburl + "\",\"flashimageurl\":\"" + flashImageUrl + "\"}");
                }
                if (dt.Rows.Count > 0)
                {
                    sb.Remove(0, 1);
                }
                return CommFunc.StandardListFormat(MessageCode.Success, sb.ToString());
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 获取私人频道聊天人数
        /// <summary>
        /// 获取私人频道聊天人数
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetTalkUserCount(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * date
             */

            StringBuilder log = new StringBuilder("接收到获取私人频道聊天人数 GetTalkUserCount ");
            string recv;
            if (qs == null || qs["date"] == null)
            {
                recv = CommFunc.StandardFormat(MessageCode.MissKey);
            }
            else
            {
                try
                {
                    log.Append(" date=").Append(qs["date"]);

                    var date = DateTime.Parse(qs["date"]);
                    var collectionName = "talk_" + date.ToString("yyyyMMdd");
                    MongoCollection col = MediaService.mongoDataBase.GetCollection(collectionName);
                    var count = col.Distinct("uid").Count();
                    recv = CommFunc.StandardObjectFormat(MessageCode.Success, "{\"usercount\":" + count + "}");
                }
                catch (Exception e)
                {
                    log.Append(" 错误信息：").Append(e.Message);
                    recv = CommFunc.StandardFormat(MessageCode.DefaultError);
                }
            }
            log.Append(" recv:").Append(recv);
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion
        #endregion

        #region 点赞
        /// <summary>
        /// 获取用户点赞信息
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string QueryDianZan(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,uid,appid,time,[up](true:<time 时间，false: >time 时间),[count](默认为 5)
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["uid"] == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["time"] == null)
            {
                MediaService.WriteLog("接收到获取用户点赞信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到获取用户点赞信息 ：ouid =" + qs["ouid"].ToString() + " uid =" + qs["uid"].ToString() + " token=" + qs["token"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);
                if (!isVerToken)
                    return recv;

                long time = 0;
                long.TryParse(qs["time"].ToString(), out time);
                if (time == 0)
                    time = DZAmountManger.Instance.GetTimeStamp();
                int count = 5;
                if (qs["count"] != null && int.TryParse(qs["count"].ToString(), out count))
                {
                    if (count == 0)
                        count = 5;
                }
                bool up = true;
                if (qs["up"] != null && bool.TryParse(qs["up"].ToString(), out up))
                {
                }

                List<DianZanAmountInfo> infos = DZAmountManger.Instance.QueryAmountInfo(uid, count, time, up);

                List<QueryDianZanAmountInfo> queryInfos = FillChannelName(infos);
                StringBuilder sb = new StringBuilder();
                foreach (var item in queryInfos)
                {
                    if (item != queryInfos.FirstOrDefault())
                    {
                        sb.Append(",");
                    }
                    sb.Append(item.ToString());
                }
                return CommFunc.StandardListFormat(MessageCode.Success, sb.ToString());
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.MissKey, e.Message);
            }
        }

        #region 填充频道名称
        private static List<QueryDianZanAmountInfo> FillChannelName(List<DianZanAmountInfo> infos)
        {
            List<QueryDianZanAmountInfo> queryinfos = new List<QueryDianZanAmountInfo>();
            if (infos == null || infos.Count == 0)
                return queryinfos;
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT rid,channelname FROM wy_radio WHERE ");
            foreach (var item in infos)
            {
                if (item != infos.FirstOrDefault())
                    sb.Append(" or ");
                sb.Append(" rid = " + item.channel);
            }
            DataTable dtContacts = SqlHelper.ExecuteTable(sb.ToString());
            foreach (DataRow dr in dtContacts.Rows)
            {
                int id = Convert.ToInt32(dr["rid"].ToString());
                infos.ForEach(x =>
                    {
                        if (x.channel == id)
                        {
                            queryinfos.Add(x.ToCast(dr["channelname"].ToString()));
                        }
                    });
            }
            return queryinfos;
        }

        public static QueryDianZanAmountInfo ToCast(this DianZanAmountInfo info, string channelname)
        {
            return new QueryDianZanAmountInfo(info.channel, info.uid, info.url.Trim(), info.time, info.count, info.tlen, channelname);
        }
        #endregion

        #region 获取点赞数量
        /// <summary>
        /// 获取点赞数量
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetPariseNum(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,appid,messageid,starttime,endtime
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["endtime"] == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["starttime"] == null || qs["messageid"] == null)
            {
                MediaService.WriteLog("接收到GetPariseNum ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到GetPariseNum ：ouid =" + qs["ouid"] + " token=" + qs["token"] + "appid =" + qs["appid"] + " messageid =" + qs["messageid"] + " starttime =" + qs["starttime"] + " endtime =" + qs["endtime"], MediaService.wirtelog);

                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                long messageid;
                int starttime, endtime;
                long.TryParse(qs["messageid"], out messageid);
                int.TryParse(qs["starttime"], out starttime);
                int.TryParse(qs["endtime"], out endtime);
                if (messageid > 0)
                {
                    if (messageid > int.MaxValue)
                    {
                        DateTime time = new DateTime(messageid).AddHours(8d);
                        starttime = Utility.ConvertDateTimeInt(time);
                        endtime = Utility.ConvertDateTimeInt(time.AddMinutes(2));
                    }
                    QueryDocument query = new QueryDocument { { "MsgTime", messageid }, { "IsParise", 1 } };
                    long count = 0;
                    var date = Utility.StampToDateTime(starttime.ToString());
                    var endDate = Utility.StampToDateTime(endtime.ToString());
                    while (date <= endDate)
                    {
                        MongoCollection col = MediaService.mongoDataBase.GetCollection("Parise_" + ouid % 10 + "_" + date.ToString("yyyyMM"));
                        count += col.FindAs<Parise>(query).Count();
                        date = date.AddMonths(1);
                    }
                    return CommFunc.StandardObjectFormat(MessageCode.Success, "{\"praisenum\":" + count + "}");
                }
                return CommFunc.StandardFormat(MessageCode.FormatError);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetPariseNum出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 按天统计点赞数量
        /// <summary>
        /// 按天统计点赞数量
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string CountPraiseByDate(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * date,[end]
             */

            StringBuilder log = new StringBuilder("接收到 按天统计点赞数量 CountPraiseByDate ");
            string recv;
            if (qs != null && qs["date"] != null)
            {
                try
                {
                    var date = qs["date"];
                    var endTime = qs["end"] + "";
                    log.Append(" date=").Append(date).Append(" end=").Append(endTime);

                    var startDate = DateTime.Parse(date).Date;
                    var start = Utility.ConvertDateTimeInt(startDate);
                    int end;
                    DateTime endDate;
                    if (endTime.Length == 0)
                    {
                        endDate = startDate;
                    }
                    else
                    {
                        endDate = DateTime.Parse(endTime).Date;
                    }
                    end = Utility.ConvertDateTimeInt(endDate.AddDays(1));

                    long count = 0;
                    while (endDate >= startDate)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            var collectionName = "Parise_" + i + "_" + startDate.ToString("yyyyMM");
                            MongoCollection col = MediaService.mongoDataBase.GetCollection(collectionName);
                            var querys = new List<IMongoQuery>
                            {
                                Query.EQ("IsParise", 1),
                                Query.GTE("LastModiTime", start),
                                Query.LT("LastModiTime", end)
                            };
                            count += col.Count(Query.And(querys));
                        }
                        startDate = startDate.AddMonths(1);
                    }
                    recv = CommFunc.StandardObjectFormat(MessageCode.Success, "{\"praisenum\":" + count + "}");
                }
                catch (Exception e)
                {
                    log.Append(" 错误信息：").Append(e.Message);
                    recv = CommFunc.StandardFormat(MessageCode.DefaultError);
                }
            }
            else
            {
                recv = CommFunc.StandardFormat(MessageCode.MissKey);
            }
            log.Append(" recv:").Append(recv);
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        #region 获取主持人被赞数量和点赞人
        /// <summary>
        /// 获取主持人被赞数量和点赞人
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetComperePraiseInfo(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,start,end
             */

            var sb = new StringBuilder("接收到GetComperePraiseInfo ");
            string recv;

            if (qs == null || qs["ouid"] == null || qs["start"] == null || qs["end"] == null)
            {
                recv = CommFunc.StandardFormat(MessageCode.MissKey);
            }
            else
            {
                try
                {
                    sb.Append(" ouid=").Append(qs["ouid"]).Append(" start=").Append(qs["start"]).Append(" end=").Append(qs["end"]);

                    int ouid = qs["ouid"].ToInt();
                    var start = qs["start"].ToLong();
                    var end = qs["end"].ToLong();
                    if (ouid > 0 && start > 0 && end > 0)
                    {
                        QueryDocument query = new QueryDocument { { "Ouid", ouid }, { "IsParise", 1 } };
                        long count = 0;
                        var date = DateTime.Now;
                        Dictionary<int, string> dicGolos = new Dictionary<int, string>();
                        while (date >= new DateTime(2016, 3, 1))
                        {
                            MongoCollection col = MediaService.mongoDataBase.GetCollection("Parise_" + ouid % 10 + "_" + date.ToString("yyyyMM"));
                            count += col.Count(query);
                            date = date.AddMonths(-1);
                        }
                        date = new DateTime(start).AddHours(8d);
                        {
                            MongoCollection col = MediaService.mongoDataBase.GetCollection("Parise_" + ouid % 10 + "_" + date.ToString("yyyyMM"));
                            var querys = new List<IMongoQuery>
                            {
                                Query.EQ("Ouid", ouid),
                                Query.EQ("IsParise", 1),
                                Query.GTE("MsgTime", start),
                                Query.LT("MsgTime", end)
                            };
                            var praises = col.FindAs<Parise>(Query.And(querys));
                            foreach (var praise in praises)
                            {
                                if (!dicGolos.ContainsKey(praise.Uid))
                                {
                                    dicGolos.Add(praise.Uid, "");
                                    int praiseOuid;
                                    MediaService.mapDic.TryGetValue(praise.Uid, out praiseOuid);
                                    if (praiseOuid > 0)
                                    {
                                        const string sql = "SELECT [nick_name] FROM wy_user WHERE [user_id]=@ouid";
                                        SqlParameter[] paras = { new SqlParameter("@ouid", praiseOuid) };
                                        var nickname = SqlHelper.ExecuteScalar(sql, paras);
                                        if (nickname != null)
                                        {
                                            dicGolos[praise.Uid] = "\"" + nickname + "\"";
                                        }
                                    }
                                }
                            }
                        }
                        string users = string.Join(",", dicGolos.Values.Where(m => m.Length > 0));
                        recv = CommFunc.StandardObjectFormat(MessageCode.Success, "{\"praisenum\":" + count + ",users:[" + users + "]}");
                    }
                    else
                        recv = CommFunc.StandardFormat(MessageCode.FormatError);
                }
                catch (Exception e)
                {
                    sb.Append(" 出错：").Append(e.Message);
                    recv = CommFunc.StandardFormat(MessageCode.DefaultError);
                }
            }
            MediaService.WriteLog(sb.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion
        #endregion

        

        #region 更新系统频道
        /// <summary>
        /// 更新系统频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string UpdateSysRadio(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * rid,option
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["rid"] == null || qs["option"] == null)
            {
                MediaService.WriteLog("接收到更新系统频道 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到更新系统频道 ：rid =" + qs["rid"].ToString() + "option =" + qs["option"].ToString(), MediaService.wirtelog);

                int option = int.Parse(qs["option"].ToString());
                int rid = int.Parse(qs["rid"].ToString());
                if (option == 0)//add、update
                {
                    DataTable radiodt = SqlHelper.ExecuteTable("select top(1) * from [wy_radio] where rid =" + rid);
                    RadioObject ro = new RadioObject();
                    ro.channelname = radiodt.Rows[0]["channelname"].ToString();
                    ro.cityname = radiodt.Rows[0]["cityname"].ToString();
                    ro.areaid = Int32.Parse(radiodt.Rows[0]["areaid"].ToString());
                    ro.audiourl = radiodt.Rows[0]["audiourl"].ToString();
                    ro.uploadurl = radiodt.Rows[0]["uploadurl"].ToString();
                    ro.sendtype = Int32.Parse(radiodt.Rows[0]["sendtype"].ToString());
                    ro.channelde = Int32.Parse(radiodt.Rows[0]["channelde"].ToString());
                    ro.radiotype = Int32.Parse(radiodt.Rows[0]["radiotype"].ToString());
                    ro.imageurl = radiodt.Rows[0]["imageurl"].ToString();
                    ro.thumburl = radiodt.Rows[0]["thumburl"].ToString();
                    if (ro.sendtype > 0)
                    {
                        string[] uidstr = radiodt.Rows[0]["sendtype"].ToString().Trim(',').Split(',');
                        if (uidstr.Length > 0)
                        {
                            ro.senduid = new int[uidstr.Length];
                            for (int j = 0; j < uidstr.Length; j++)
                            {
                                ro.senduid[j] = Int32.Parse(uidstr[j]);
                            }
                        }
                    }
                    ro.prid = Int32.Parse(radiodt.Rows[0]["prid"].ToString());
                    ro.areacode = radiodt.Rows[0]["areacode"].ToString();
                    ro.flashimageurl = radiodt.Rows[0]["flashimageurl"].ToString();
                    MediaService.radioDic.AddOrUpdate(rid, ro, (k, v) => ro);
                }
                else if (option == 1)//delete
                {
                    RadioObject ro = new RadioObject();
                    MediaService.radioDic.TryRemove(rid, out ro);
                }
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 查询频道呼叫信息
        /// <summary>
        /// 更新系统频道
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string QueryCallTalkInfo(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * starttime,endtime,talkname
             */
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["starttime"] == null || qs["endtime"] == null)
            {
                MediaService.WriteLog("查询频道呼叫信息:" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("查询频道呼叫信息 starttime:" + qs["starttime"].ToString() + "endtime:" + qs["endtime"].ToString(), MediaService.wirtelog);
                string starttime = qs["starttime"] == null ? "" : qs["starttime"].ToString();
                string endtime = qs["endtime"] == null ? "" : qs["endtime"].ToString();
                string talkname = qs["talkname"] == null ? "" : qs["talkname"].ToString();
                string talkresult = "[]";
                string responsetalkresult = "[]";
                if (!string.IsNullOrWhiteSpace(starttime) && !string.IsNullOrWhiteSpace(endtime))
                {
                    DateTime stime = CommFunc.StampToDateTime(starttime);
                    DateTime etime = CommFunc.StampToDateTime(endtime);
                    DateTime temp = DateTime.Now;
                    if (stime > etime)
                    {
                        temp = etime;
                        etime = stime;
                        stime = temp;
                    }
                    var talks = CallTalkMongoDBOption.QueryCallTalk(stime, etime, talkname);
                    talkresult = JsonHelper.JavaScriptSerialize<List<CallTalkInfo>>(talks);
                    var talk = talks.FirstOrDefault();
                    if (talk != null)
                    {
                        string guid = talk.TGuid;
                        responsetalkresult = JsonHelper.JavaScriptSerialize<List<ResponseCallTalk>>(CallTalkMongoDBOption.QueryResponseCallTalk(stime, etime, guid));
                    }
                }
                string subrecv = "{\"talkinfo\":" + talkresult + ",\"responsetalkinfo\":" + responsetalkresult + "}";
                return CommFunc.StandardObjectFormat(MessageCode.Success, subrecv);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 发送在线用户信息

        #endregion

        #region 更新了用户设备
        /// <summary>
        /// 更新了用户设备
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string UpdateUserDevice(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
            * glsns
            */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["glsns"] == null)
            {
                MediaService.WriteLog("接收到更新用户设备 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到更新用户设备 ：glsns =" + qs["glsns"], MediaService.wirtelog);
                string glsns = string.Join(",",
                    qs["glsns"].Split(new[] { ',', '|', '.', '、', '-' }, StringSplitOptions.RemoveEmptyEntries));
                DataTable dt = SqlHelper.ExecuteTable("SELECT uid FROM [app_users] WHERE glsn IN (" + glsns + ")");
                foreach (DataRow row in dt.Rows)
                {
                    int uid = int.Parse(row["uid"].ToString());
                    UserObject uo;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        if (uo.socket != null && uo.socket[CommFunc.APPID] != null)
                        {
                            try
                            {
                                uo.socket[CommFunc.APPID].Shutdown(SocketShutdown.Both);
                            }
                            catch { }
                            uo.socket[CommFunc.APPID].Close();
                            uo.socket[CommFunc.APPID] = null;
                        }
                    }
                }
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("执行异常：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region adapter
        #region 用户登录适配信息
        /// <summary>
        /// 获取用户登录适配信息
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string UserLogin(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * loginkey,password,appid
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["loginkey"] == null || qs["password"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到获取用户登录适配信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到获取用户登录适配信息 ：loginkey =" + qs["loginkey"] + " password=" + qs["password"] + "appid =" + qs["appid"], MediaService.wirtelog);
                dbscarreturnUser user = MyCarAdapter.AppLogin(qs["loginkey"], qs["password"], qs["appid"], ref recv);
                if (user != null && user.code == 0 && user.data != null && user.data.user != null)
                {
                    int count = Convert.ToInt32(SqlHelper.ExecuteScalar("select count(*) from wy_user where user_id='" + user.data.user.user_id + "';"));
                    if (count > 0)
                    {
                        SqlHelper.ExecuteNonQuery("update [wy_user] set user_name='" +
                                                  user.data.user.user_name + "',nation_id='" + user.data.user.nation_id +
                                                  "',nick_name='" + user.data.user.nick_name + "',email='" +
                                                  user.data.user.email + "',mobile='" + user.data.user.mobile +
                                                  "',face_url='" + user.data.user.face_url + "',country='" +
                                                  user.data.user.country + "' where user_id='" +
                                                  user.data.user.user_id + "'");
                    }
                    else
                    {
                        string insert = "insert [wy_user] (user_id,user_name,nick_name,mobile,email,face_url,country,province,city,nation_id) values ('" +
                              user.data.user.user_id + "','" + user.data.user.user_name + "','" + user.data.user.nick_name +
                              "','" + user.data.user.mobile + "','" + user.data.user.email + "','" + user.data.user.face_url + "','" +
                              user.data.user.country + "','" + user.data.user.province + "','" + user.data.user.city + "','" + user.data.user.nation_id + "');";

                        int result = SqlHelper.ExecuteNonQuery(insert);
                    }
                }

                return recv;
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }


        /// <summary>
        /// 获取用户登录适配信息(测试地址)
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string UserLoginTest(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * loginkey,password,appid
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["loginkey"] == null || qs["password"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到获取用户登录适配信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到获取用户登录适配信息 ：loginkey =" + qs["loginkey"] + " password=" + qs["password"] + "appid =" + qs["appid"], MediaService.wirtelog);
                dbscarreturnUser user = MyCarAdapter.AppLoginTest(qs["loginkey"], qs["password"], qs["appid"], ref recv);
                MediaService.WriteLog("接收到获取用户登录适配信息 ：user =" + user.ToString() + " recv=" + recv, MediaService.wirtelog);
                if (user != null && user.code == 0 && user.data != null && user.data.user != null)
                {
                    int count = Convert.ToInt32(SqlHelper.ExecuteScalar("select count(*) from wy_user where user_id='" + user.data.user.user_id + "';"));
                    if (count > 0)
                    {
                        SqlHelper.ExecuteNonQuery("update [wy_user] set user_name='" +
                                                  user.data.user.user_name + "',nation_id='" + user.data.user.nation_id +
                                                  "',nick_name='" + user.data.user.nick_name + "',email='" +
                                                  user.data.user.email + "',mobile='" + user.data.user.mobile +
                                                  "',face_url='" + user.data.user.face_url + "',country='" +
                                                  user.data.user.country + "' where user_id='" +
                                                  user.data.user.user_id + "'");
                    }
                    else
                    {
                        string insert = "insert [wy_user] (user_id,user_name,nick_name,mobile,email,face_url,country,province,city,nation_id) values ('" +
                              user.data.user.user_id + "','" + user.data.user.user_name + "','" + user.data.user.nick_name +
                              "','" + user.data.user.mobile + "','" + user.data.user.email + "','" + user.data.user.face_url + "','" +
                              user.data.user.country + "','" + user.data.user.province + "','" + user.data.user.city + "','" + user.data.user.nation_id + "');";

                        int result = SqlHelper.ExecuteNonQuery(insert);
                    }
                }

                return recv;
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
        #endregion

        #region 请求发送验证码(注册和找回密码用)
        /// <summary>
        /// 请求发送验证码(注册和找回密码用)
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string ReqSendCode(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * req_info,is_check,app_id
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["req_info"] == null || qs["is_check"] == null)
            {
                MediaService.WriteLog("接收到请求发送验证码(注册和找回密码用) ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到请求发送验证码(注册和找回密码用) ：req_info =" + qs["req_info"] + " is_check=" + qs["is_check"], MediaService.wirtelog);
                const string isRes = "3"; //短信验证码
                MyCarAdapter.VerifyCode_Req_Send_Code(qs["req_info"], qs["is_check"], qs["app_id"], isRes, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.MissKey, e.Message);
            }
            #endregion
        }
        #endregion

        #region 验证输入的验证码
        /// <summary>
        /// 验证输入的验证码
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string VerifyCode(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * req_info,verify_code
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["req_info"] == null || qs["verify_code"] == null)
            {
                MediaService.WriteLog("接收到验证输入的验证码 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到验证输入的验证码 ：req_info =" + qs["req_info"].ToString() + " verify_code=" + qs["verify_code"].ToString(), MediaService.wirtelog);
                MyCarAdapter.Verifycode_Verify(qs["req_info"].ToString(), qs["verify_code"].ToString(), ref recv);
                return recv;
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.MissKey, e.Message);
            }
            #endregion
        }
        #endregion

        #region 找回密码
        /// <summary>
        /// 找回密码
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string ResetPass(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * req,pass,confirm_pass,verify_code,appid
             */

            #region uid
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["req"] == null || qs["pass"] == null || qs["confirm_pass"] == null || qs["verify_code"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到找回密码 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appid = qs["appid"];
                MediaService.WriteLog("接收到找回密码 ：req =" + qs["req"] + " pass=" + qs["pass"] + " confirm_pass=" + qs["confirm_pass"] + " verify_code=" + qs["verify_code"] + " appid=" + appid, MediaService.wirtelog);
                MyCarAdapter.Passport_Service_Reset_Pass(appid, qs["req"], qs["pass"], qs["confirm_pass"], qs["verify_code"], ref recv);
                return recv;
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
        #endregion

        #region 设置登录密码
        /// <summary>
        /// 设置登录密码
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string SetPass(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,pass,newpass,appid,token,ver
             */

            #region SetPass
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["pass"] == null || qs["newpass"] == null || qs["appid"] == null || qs["token"] == null || qs["ver"] == null)
            {
                MediaService.WriteLog("接收到设置登录密码 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string ver = qs["ver"];
                MediaService.WriteLog("接收到设置登录密码 ：ouid =" + qs["ouid"] + " pass=" + qs["pass"] + " newpass=" + qs["newpass"] + " app_id=" + appId + " token=" + qs["token"] + " ver=" + ver, MediaService.wirtelog);
                MyCarAdapter.Userinfo_Set_Password(qs["ouid"], appId, qs["token"], qs["pass"], qs["newpass"], ver, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("设置登录密码出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            #endregion
        }
        #endregion

        #region 注册
        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string Register(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * verify_code,pass,appid,loginKey,[nick_name]
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["verify_code"] == null || qs["pass"] == null || qs["appid"] == null || qs["loginKey"] == null)
            {
                MediaService.WriteLog("接收到Register ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string loginKey = qs["loginKey"];
                MediaService.WriteLog("接收到Register ：verify_code =" + qs["verify_code"] + " pass=" + qs["pass"] + " app_id=" + appId + " nick_name=" + qs["nick_name"], MediaService.wirtelog);
                const string nationId = "143";//固定为中国
                MyCarAdapter.Passport_Service_Register(appId, qs["verify_code"], qs["nick_name"] + "", qs["pass"], nationId, loginKey, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("Register出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 修改昵称
        /// <summary>
        /// 修改昵称
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string UpdateNickname(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,token,appid,nickname
             */

            if (qs != null && qs["ouid"] != null && qs["appid"] != null && qs["token"] != null && qs["nickname"] != null)
            {
                try
                {
                    string token = qs["token"];
                    string nickname = qs["nickname"];
                    MediaService.WriteLog("UpdateNickname----ouid=" + qs["ouid"] + " appid=" + qs["appid"] + " token=" + token + " nickname=" + nickname, MediaService.wirtelog);

                    int appid, ouid;
                    int.TryParse(qs["appid"], out appid);
                    int.TryParse(qs["ouid"], out ouid);

                    if (appid > 0 && ouid > 0 && token.Length > 0 && nickname.Length > 0)
                    {
                        string errMessage = "";
                        if (CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                        {
                            Dictionary<string, string> get = new Dictionary<string, string>();
                            get.Add("action", "userinfo.set_base");
                            get.Add("user_id", ouid.ToString());
                            get.Add("ver", "5.0.3");
                            get.Add("app_id", appid.ToString());
                            Dictionary<string, string> post = new Dictionary<string, string>();
                            post.Add("name", nickname);
                            string sign = Utility.GetSign(token, get, post);
                            get.Add("sign", sign);
                            string geturl = Utility.CreateLinkString(get);
                            string posturl = Utility.CreateLinkString(post);
                            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?" + geturl, posturl, "POST", Encoding.UTF8);

                            MediaService.WriteLog("str=" + str, MediaService.wirtelog);
                            if (CommBusiness.GetJsonValue(str, "code", ",", false) == "0")
                            {
                                string sql = "UPDATE wy_user SET nick_name='" + nickname + "' WHERE [user_id]=" + ouid;
                                SqlHelper.ExecuteNonQuery(sql);
                            }
                            return str;
                        }
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    else
                    {
                        return CommFunc.StandardFormat(MessageCode.FormatError);
                    }
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("UpdateNickname执行异常：" + err.Message, MediaService.wirtelog);
                    return CommFunc.StandardFormat(MessageCode.DefaultError, err.Message);
                }
            }
            else
            {
                return CommFunc.StandardFormat(MessageCode.MissKey);
            }
        }
        #endregion

        #region 收货地址
        #region 添加地址
        /// <summary>
        /// 添加地址
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string AddAddress(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid, appid, token, user_name, house_number, address, mobile, region_2, region_3, region_4
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["user_name"] == null || qs["house_number"] == null || qs["address"] == null || qs["mobile"] == null || qs["region_2"] == null || qs["region_3"] == null || qs["region_4"] == null || qs["token"] == null)
            {
                MediaService.WriteLog("接收到AddAddress ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string user_name = qs["user_name"], house_number = qs["house_number"], address = qs["address"], mobile = qs["mobile"];
                string region_2 = qs["region_2"], region_3 = qs["region_3"], region_4 = qs["region_4"];
                MediaService.WriteLog("接收到AddAddress ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " user_name=" + user_name + " house_number=" + qs["house_number"] + " mobile=" + mobile + " region_2=" + qs["region_2"] + " region_3=" + region_3 + " region_4=" + qs["region_4"] + " address=" + address, MediaService.wirtelog);
                MyCarAdapter.AddAddress(qs["ouid"], appId, qs["token"], user_name, house_number, region_2, region_3, region_4, address, mobile, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("AddAddress出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 修改地址
        /// <summary>
        /// 修改地址
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string UpdateAddress(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid, appid, token, id, [user_name, address, mobile, region_2, region_3, region_4]
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["id"] == null)
            {
                MediaService.WriteLog("接收到UpdateAddress ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string id = qs["id"];
                string user_name = qs["user_name"], address = qs["address"], mobile = qs["mobile"];
                string region_2 = qs["region_2"], region_3 = qs["region_3"], region_4 = qs["region_4"];
                MediaService.WriteLog("接收到UpdateAddress ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " user_name=" + user_name + " mobile=" + mobile + " region_2=" + qs["region_2"] + " region_3=" + region_3 + " region_4=" + qs["region_4"] + " address=" + address, MediaService.wirtelog);
                MyCarAdapter.UpdateAddress(qs["ouid"], appId, qs["token"], id, false, user_name, mobile, "143", region_2, region_3, region_4, "0", address, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("UpdateAddress出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }
        #endregion

        #region 获取地址列表
        /// <summary>
        /// 获取地址列表
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetAddressList(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null)
            {
                MediaService.WriteLog("接收到GetAddressList ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                MediaService.WriteLog("接收到GetAddressList ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"], MediaService.wirtelog);
                MyCarAdapter.GetAddress(qs["ouid"], appId, qs["token"], ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetAddressList出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 设置默认地址
        /// <summary>
        /// 设置默认地址
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string SetDefaultAddress(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,id,isdefault(1/0)
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["id"] == null || qs["isdefault"] == null)
            {
                MediaService.WriteLog("接收到SetDefaultAddress ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string id = qs["id"];
                MediaService.WriteLog("接收到SetDefaultAddress ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " id=" + id + " isdefault=" + qs["isdefault"], MediaService.wirtelog);
                MyCarAdapter.UpdateAddress(qs["ouid"], appId, qs["token"], id, qs["isdefault"], ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("SetDefaultAddress出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 删除地址
        /// <summary>
        /// 删除地址
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string DeleteAddress(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,id
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["id"] == null)
            {
                MediaService.WriteLog("接收到DeleteAddress ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string id = qs["id"];
                MediaService.WriteLog("接收到DeleteAddress ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " id=" + id, MediaService.wirtelog);
                MyCarAdapter.DeleteAddress(qs["ouid"], appId, qs["token"], id, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("DeleteAddress出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 获取省份列表
        /// <summary>
        /// 获取省份列表
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetProvince(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,ver
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["ver"] == null)
            {
                MediaService.WriteLog("接收到GetProvince ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                const string ncode = "143";
                MediaService.WriteLog("接收到GetProvince ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " ver=" + qs["ver"] + " ncode=" + ncode, MediaService.wirtelog);
                MyCarAdapter.GetProvince(qs["ouid"], appId, qs["token"], ncode, qs["ver"], ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetProvince出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 获取城市列表
        /// <summary>
        /// 获取城市列表
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetCity(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,ver,pcode
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["ver"] == null || qs["pcode"] == null)
            {
                MediaService.WriteLog("接收到GetCity ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string pcode = qs["pcode"];
                MediaService.WriteLog("接收到GetCity ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " ver=" + qs["ver"] + " pcode=" + pcode, MediaService.wirtelog);
                MyCarAdapter.GetCity(qs["ouid"], appId, qs["token"], pcode, qs["ver"], ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetCity出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 获取区县列表
        /// <summary>
        /// 获取区县列表
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetRegion(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,ver,ccode
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["ver"] == null || qs["ccode"] == null)
            {
                MediaService.WriteLog("接收到GetRegion ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string ccode = qs["ccode"];
                MediaService.WriteLog("接收到GetRegion ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " ver=" + qs["ver"] + " ccode=" + ccode, MediaService.wirtelog);
                MyCarAdapter.GetRegion(qs["ouid"], appId, qs["token"], ccode, qs["ver"], ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetRegion出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion
        #endregion

        #region 查询用户的经纬度
        /// <summary>
        /// 查询用户的经纬度
        /// </summary>
        /// <param name="qs">键值对</param>
        /// <returns></returns>
        public static string GetUserLoLa(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,uid,appid,token
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["uid"] == null || qs["appid"] == null || qs["token"] == null)
            {
                MediaService.WriteLog("接收到查询用户的经纬度：查询关键字缺失！" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"].ToString(), qs["uid"].ToString(), qs["appid"].ToString(), qs["token"].ToString(), ref ouid, ref uid, ref recv);

                MediaService.WriteLog("接收到查询所有用户的经纬度 ：ouid =" + qs["ouid"] + "token =" + qs["token"] + "appid =" + qs["appid"] + "uid =" + qs["uid"], MediaService.wirtelog);
                int appid = 0;
                int.TryParse(qs["appid"].ToString(), out appid);
                if (isVerToken)
                {
                    string subrecv = "";
                    KeyValuePair<int, UserObject> user = MediaService.userDic.FirstOrDefault(x => x.Key == uid);
                    if (user.Value != null)
                    {
                        subrecv = "{\"uid\":" + user.Key + ",\"lo\": " + user.Value.lo[appid] + ",\"la\":" + user.Value.la[appid] + "}";
                    }
                    recv = CommFunc.StandardObjectFormat(MessageCode.Success, subrecv);
                    return recv;
                }
                else
                    return CommFunc.StandardFormat(MessageCode.TokenOverdue);
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 剩余流量查询
        /// <summary>
        /// 剩余流量查询
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetFlowBySerial(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * glsn,sim
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["glsn"] == null || qs["sim"] == null)
            {
                MediaService.WriteLog("接收到GetFlowBySerial ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string sim = qs["sim"];
                string glsn = CommFunc.GetUniform12(qs["glsn"]);
                MediaService.WriteLog("接收到GetFlowBySerial ：glsn =" + glsn + " sim=" + sim, MediaService.wirtelog);
                MyCarAdapter.GetFlowBySerial(sim, glsn, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetFlowBySerial出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 行程体检
        #region 车辆体检报告列表
        /// <summary>
        /// 车辆体检报告列表
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string CarReportList(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,glsn,isstart,time
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["isstart"] == null || qs["glsn"] == null)
            {
                MediaService.WriteLog("接收到CarReportList ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string glsn = CommFunc.GetUniform12(qs["glsn"]);
                bool isstart = qs["isstart"] == "0";//刷新时为0
                var isend = !isstart;
                string time = qs["time"];
                MediaService.WriteLog("接收到CarReportList ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " glsn=" + glsn + " isstart=" + isstart + " time=" + time + " isend=" + isend, MediaService.wirtelog);
                MyCarAdapter.Report_Service_Car_Report_List(qs["ouid"], appId, qs["token"], glsn, isstart, time, isend, time, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("CarReportList出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 足迹接口
        /// <summary>
        /// 足迹接口
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetTripWgs(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,glsn,start_time,end_time,type,isUseType
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["start_time"] == null || qs["glsn"] == null || qs["type"] == null)
            {
                MediaService.WriteLog("接收到GetTripWgs ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string glsn = CommFunc.GetUniform12(qs["glsn"]);
                string type = qs["type"];
                string end_time = qs["end_time"];
                string isUseType = qs["isUseType"];
                MediaService.WriteLog("接收到GetTripWgs ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " glsn=" + glsn + " start_time=" + qs["start_time"] + " end_time=" + end_time + " type=" + type + " isUseType=" + isUseType, MediaService.wirtelog);
                MyCarAdapter.Trip_Service_Get_Trip_Wgs(qs["ouid"], appId, qs["token"], glsn, qs["start_time"], end_time, type, isUseType == "1", ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetTripWgs出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 获取某个月的行程统计数据
        /// <summary>
        /// 获取某个月的行程统计数据
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string MonthCount(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,month,glsn
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["month"] == null || qs["glsn"] == null)
            {
                MediaService.WriteLog("接收到MonthCount ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string glsn = CommFunc.GetUniform12(qs["glsn"]);
                MediaService.WriteLog("接收到MonthCount ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " month=" + qs["month"] + " glsn=" + glsn, MediaService.wirtelog);
                MyCarAdapter.Mileage_Count_Month_Count(qs["ouid"], appId, qs["token"], glsn, qs["month"], ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("MonthCount出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 获取指定日期的行程数据
        /// <summary>
        /// 获取指定日期的行程数据
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetMileage(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,glsn,start_time,end_time
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["start_time"] == null || qs["glsn"] == null || qs["end_time"] == null)
            {
                MediaService.WriteLog("接收到GetMileage ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string glsn = CommFunc.GetUniform12(qs["glsn"]);
                string end_time = qs["end_time"];
                MediaService.WriteLog("接收到GetMileage ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " glsn=" + glsn + " start_time=" + qs["start_time"] + " end_time=" + end_time, MediaService.wirtelog);
                MyCarAdapter.Gps_Info_Service_Get_Mileage(qs["ouid"], appId, qs["token"], glsn, qs["start_time"], end_time, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetMileage出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 根据指定行程id获取详细信息
        /// <summary>
        /// 根据指定行程id获取详细信息
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetGpsInfo(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,mileage_ids
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["mileage_ids"] == null)
            {
                MediaService.WriteLog("接收到GetGpsInfo ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                MediaService.WriteLog("接收到GetGpsInfo ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " mileage_ids=" + qs["mileage_ids"], MediaService.wirtelog);
                MyCarAdapter.Gps_Info_Get_Data2(qs["ouid"], appId, qs["token"], qs["mileage_ids"], ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetGpsInfo出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 查询历史轨迹点（行程详情）
        /// <summary>
        /// 查询历史轨迹点（行程详情）
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetGpsHisitory(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,glsn,start_time,end_time,querydate
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["start_time"] == null || qs["glsn"] == null || qs["end_time"] == null || qs["querydate"] == null)
            {
                MediaService.WriteLog("接收到GetGpsHisitory ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string glsn = CommFunc.GetUniform12(qs["glsn"]);
                string end_time = qs["end_time"];
                string querydate = qs["querydate"];
                MediaService.WriteLog("接收到GetGpsHisitory ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " glsn=" + glsn + " start_time=" + qs["start_time"] + " end_time=" + end_time + " querydate=" + querydate, MediaService.wirtelog);
                MyCarAdapter.Gps_Info_Get_Hisitory_Position_Record_Wgs(qs["ouid"], appId, qs["token"], querydate, qs["start_time"], end_time, glsn, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetGpsHisitory出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 查询实时轨迹点
        /// <summary>
        /// 查询实时轨迹点
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetRealTimeGps(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,g_id,glsn
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["g_id"] == null || qs["glsn"] == null)
            {
                MediaService.WriteLog("接收到GetRealTimeGps ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string glsn = CommFunc.GetUniform12(qs["glsn"]);
                MediaService.WriteLog("接收到GetRealTimeGps ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " g_id=" + qs["g_id"] + " glsn=" + glsn, MediaService.wirtelog);
                MyCarAdapter.Gps_Info_Get_Real_Time_Data_Wgs(qs["ouid"], appId, qs["token"], glsn, qs["g_id"], ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetRealTimeGps出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 获取实时位置时，查询当前未完成里程的轨迹接口
        /// <summary>
        /// 获取实时位置时，查询当前未完成里程的轨迹接口
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetTripRecord(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,glsn
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["glsn"] == null)
            {
                MediaService.WriteLog("接收到GetTripRecord ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string glsn = CommFunc.GetUniform12(qs["glsn"]);
                MediaService.WriteLog("接收到GetTripRecord ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " glsn=" + glsn, MediaService.wirtelog);
                MyCarAdapter.Gps_Info_Get_Trip_Record_Wgs(qs["ouid"], appId, qs["token"], glsn, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetTripRecord出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 获取实时数据流
        /// <summary>
        /// 获取实时数据流
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetdfdatalistNew(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,appid,token,glsn
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["glsn"] == null)
            {
                MediaService.WriteLog("接收到GetdfdatalistNew ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appId = qs["appid"];
                string glsn = CommFunc.GetUniform12(qs["glsn"]);
                MediaService.WriteLog("接收到GetdfdatalistNew ：ouid =" + qs["ouid"] + " app_id=" + appId + " token=" + qs["token"] + " glsn=" + glsn, MediaService.wirtelog);
                MyCarAdapter.Datastream_Getdfdatalistnew(qs["ouid"], appId, qs["token"], glsn, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetdfdatalistNew出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion
        #endregion

        #region 版本比较并更新
        /// <summary>
        /// 版本比较并更新
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetLatestVersion(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * appid,vision_no,ouid,token,is_test
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["appid"] == null || qs["vision_no"] == null || qs["is_test"] == null || qs["ouid"] == null || qs["token"] == null)
            {
                MediaService.WriteLog("接收到GetLatestVersion ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string appid = qs["appid"];
                string vision_no = qs["vision_no"];
                string ouid = qs["ouid"];
                string token = qs["token"];
                string is_test = qs["is_test"];
                MediaService.WriteLog("接收到GetLatestVersion ：appid =" + appid + " vision_no=" + vision_no + " is_test=" + is_test + " ouid=" + ouid + " token=" + token, MediaService.wirtelog);
                MyCarAdapter.Version_Latest(appid, vision_no, ouid, token, is_test, ref recv);
                return recv;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetLatestVersion出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #endregion

        #region 导航
        #region 获取导航设置
        /// <summary>
        /// 获取导航设置
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetNavSetting(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * uid
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["uid"] == null)
            {
                MediaService.WriteLog("接收到GetNavSetting ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string uid = qs["uid"];
                MediaService.WriteLog("接收到GetNavSetting ：uid=" + uid, MediaService.wirtelog);
                string sql = "SELECT navsetting FROM app_users WHERE [uid]=" + uid;
                object obj = SqlHelper.ExecuteScalar(sql);
                if (obj != null)
                {
                    return CommFunc.StandardObjectFormat(MessageCode.Success, string.Format("{{\"navsetting\":{0}}}", obj));
                    //{"code":0,"msg":"","data":{"navsetting":0}}
                }
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetNavSetting出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 修改导航设置
        /// <summary>
        /// 修改导航设置
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string SetNavSetting(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * uid,navsetting
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["uid"] == null || qs["navsetting"] == null)
            {
                MediaService.WriteLog("接收到SetNavSetting ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string navsetting = qs["navsetting"];
                string uid = qs["uid"];
                MediaService.WriteLog("接收到SetNavSetting ：uid =" + uid + " navsetting=" + navsetting, MediaService.wirtelog);
                string sql = "UPDATE app_users SET navsetting=" + navsetting + " WHERE [uid]=" + uid;
                SqlHelper.ExecuteNonQuery(sql);

                string post = string.Format("{{\"status\":true,\"navsetting\":{0}}}", qs["navsetting"]);
                PublicClass.SendToOnlineUserList(null, post, "", new List<int> { int.Parse(uid) }, 99, 0, CommType.pushNavSettingToUser, CommFunc.APPID);
                return CommFunc.StandardObjectFormat(MessageCode.Success, "");
            }
            catch (Exception e)
            {
                MediaService.WriteLog("SetNavSetting出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        /// <summary>
        /// 输入目的地让golo导航
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GoloNavigate(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * uid,name,des,lo,la
             */
            if (qs == null || qs["uid"] == null || qs["name"] == null || qs["des"] == null || qs["lo"] == null || qs["la"] == null)
            {
                string recv = CommFunc.StandardFormat(MessageCode.MissKey);
                MediaService.WriteLog("接收到GoloNavigate ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                int uid;
                if (int.TryParse(qs["uid"], out uid))
                {
                    if (uid > 0)
                    {
                        UserObject uo;
                        int isOnline = 0;
                        if (MediaService.userDic.TryGetValue(uid, out uo))
                        {
                            if (uo != null && uo.socket[CommFunc.APPID] != null)
                                isOnline = 1;
                        }
                        if (isOnline == 0)
                        {
                            return CommFunc.StandardFormat(MessageCode.DeviceOutLine);
                        }
                        string post = string.Format("{{\"status\":true,\"data\":{{\"name\":\"{0}\",\"des\":\"{1}\",\"lo\":{2},\"la\":{3}}}}}", qs["name"], qs["des"], qs["lo"], qs["la"]);
                        PublicClass.SendToOnlineUserList(null, post, "", new List<int>() { uid }, 99, 0, CommType.pushNavToUser, CommFunc.APPID);
                        return CommFunc.StandardObjectFormat(MessageCode.Success, "{\"isonline\":" + isOnline + "}");
                    }
                    return CommFunc.StandardFormat(MessageCode.DeviceNotExist);
                }
                return CommFunc.StandardFormat(MessageCode.FormatError);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GoloNavigate出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }

        /// <summary>
        /// 输入目的地让golo导航(批量导航到非绑定设备)
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GoloNavigateNouid(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * name,des,lo,la,sn
             */
            if (qs == null || qs["name"] == null || qs["des"] == null || qs["lo"] == null || qs["la"] == null || qs["sn"] == null)
            {
                string recv = CommFunc.StandardFormat(MessageCode.MissKey);
                MediaService.WriteLog("接收到GoloNavigateNouid ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var strSn = qs["sn"];
                var ss = strSn.Split(',');
                var snlist = ss.Aggregate("", (current, variable) => current + ("," + "'" + variable + "'")).Remove(0, 1);
                MediaService.WriteLog("接收到GoloNavigateNouid_sn参数：" + snlist, MediaService.wirtelog);
                //通过sn查找设备id
                string strSql = "SELECT uid,glsn FROM [app_users] WHERE glsn in (  " + snlist + " ) order by glsn asc";
                MediaService.WriteLog("接收到GoloNavigateNouid_sql参数：" + strSql, MediaService.wirtelog);
                var dt = SqlHelper.ExecuteTable(strSql);
                List<int> uidList = new List<int>();
                List<string> snList = new List<string>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    uidList.Add(Convert.ToInt32(dt.Rows[i]["uid"]));
                    snList.Add(dt.Rows[i]["glsn"].ToString());
                }

                Dictionary<string, int> dic = new Dictionary<string, int>();
                List<int> uidListOnline = new List<int>();//在线的uid
                for (int i = 0; i < uidList.Count(); i++) ////根据uid查询在线状态
                {
                    UserObject uo;
                    int isOnline = 0;
                    if (MediaService.userDic.TryGetValue(uidList[i], out uo))
                    {
                        if (uo != null && uo.socket[CommFunc.APPID] != null)
                        {
                            isOnline = 1;
                            uidListOnline.Add(uidList[i]);
                        }

                    }
                    if (!dic.ContainsKey(snList[i]))
                        dic.Add(snList[i], isOnline);
                }
                string post =
                    string.Format(
                        "{{\"status\":true,\"data\":{{\"name\":\"{0}\",\"des\":\"{1}\",\"lo\":{2},\"la\":{3}}}}}",
                        qs["name"], qs["des"], qs["lo"], qs["la"]);
                MediaService.WriteLog("post ：" + post, MediaService.wirtelog);
                var tmp = PublicClass.SendToOnlineUserList(null, post, "", uidListOnline, 99, 0, CommType.pushNavToUser, CommFunc.APPID);
                List<NavigationModel> model = dic.Select(i => new NavigationModel
                {
                    sn = i.Key,
                    isonline = i.Value
                }).ToList();
                var jsonmodel = JsonConvert.SerializeObject(model);
                MediaService.WriteLog("转换的json对象 ：" + jsonmodel, MediaService.wirtelog);
                MediaService.WriteLog("输出信息 ：" + CommFunc.StandardObjectFormat(MessageCode.Success, jsonmodel), MediaService.wirtelog);
                return CommFunc.StandardObjectFormat(MessageCode.Success, jsonmodel);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GoloNavigateNouid出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }

       /// <summary>
        /// 输入目的地让golo导航(批量导航到固定100台设备(自驾游使用))
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GoloNavigateSpecific(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * name,des,lo,la
             */
            MediaService.WriteLog("接收到GoloNavigateSpecific start：", MediaService.wirtelog);
            if (qs == null || qs["name"] == null || qs["des"] == null || qs["lo"] == null || qs["la"] == null || qs["tid"]==null)
            {
                string recv = CommFunc.StandardFormat(MessageCode.MissKey);
                MediaService.WriteLog("接收到GoloNavigateSpecific ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                //var strSn = "";
                //for (long i = 971693000000; i <= 971693000100; i++)
                //{
                //    strSn += "," + i;
                //}
                //var _ss = strSn.Remove(0, 1).Split(',');
                //var snlist = _ss.Aggregate("", (current, variable) => current + ("," + "'" + variable + "'")).Remove(0, 1);
                //通过sn查找设备id
                //string strSql = "SELECT uid,glsn FROM [app_users] WHERE glsn in (  " + snlist + " ) order by glsn asc";
                int tid;
                if (!int.TryParse(qs["tid"], out tid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                string strSql_GetSNByTid =string.Format(
                        "SELECT a.uid, glsn FROM [weiyun].[dbo].[wy_talkuser] a,[weiyun].[dbo].app_users b where a.tid={0} and a.uid=b.uid and ISNULL(a.uidtype,'')<>1",
                        tid);
                MediaService.WriteLog("接收到GoloNavigateNouid_sql参数：" + strSql_GetSNByTid, MediaService.wirtelog);
                var dt = SqlHelper.ExecuteTable(strSql_GetSNByTid);
                List<int> uidList = new List<int>();
                List<string> snList = new List<string>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    uidList.Add(Convert.ToInt32(dt.Rows[i]["uid"]));
                    snList.Add(dt.Rows[i]["glsn"].ToString());
                }

                Dictionary<string, int> dic = new Dictionary<string, int>();
                List<int> uidListOnline = new List<int>();//在线的uid
                for (int i = 0; i < uidList.Count(); i++) ////根据uid查询在线状态
                {
                    UserObject uo;
                    int isOnline = 0;
                    if (MediaService.userDic.TryGetValue(uidList[i], out uo))
                    {
                        if (uo != null && uo.socket[CommFunc.APPID] != null)
                        {
                            isOnline = 1;
                            uidListOnline.Add(uidList[i]);
                        }

                    }
                    if (!dic.ContainsKey(snList[i]))
                        dic.Add(snList[i], isOnline);
                }
                string post =
                    string.Format(
                        "{{\"status\":true,\"data\":{{\"name\":\"{0}\",\"des\":\"{1}\",\"lo\":{2},\"la\":{3}}}}}",
                        qs["name"], qs["des"], qs["lo"], qs["la"]);
                MediaService.WriteLog("post ：" + post, MediaService.wirtelog);
                var tmp = PublicClass.SendToOnlineUserList(null, post, "", uidListOnline, 99, 0, CommType.pushNavToUser, CommFunc.APPID);
                List<NavigationModel> model = dic.Select(i => new NavigationModel
                {
                    sn = i.Key,
                    isonline = i.Value
                }).ToList();
                var jsonmodel = JsonConvert.SerializeObject(model);
                MediaService.WriteLog("转换的json对象 ：" + jsonmodel, MediaService.wirtelog);
                MediaService.WriteLog("输出信息 ：" + CommFunc.StandardObjectFormat(MessageCode.Success, jsonmodel), MediaService.wirtelog);
                return CommFunc.StandardObjectFormat(MessageCode.Success, jsonmodel);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GoloNavigateNouid出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        } 
        #endregion

        #region 自驾游推送
        /// <summary>
        /// 客户端推送歌曲到设备
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GoloPushSong(NameValueCollection qs)
        {
            try
            {
                if (qs == null || qs["tid"] == null || qs["id"] == null || qs["music_url"] == null || qs["title"] == null ||
                qs["categories"] == null || qs["t_singer"] == null)
                {
                    string recv = CommFunc.StandardFormat(MessageCode.MissKey);
                    MediaService.WriteLog("接收到GoloPushSong ：" + recv, MediaService.wirtelog);
                    return recv;
                }
                int id, categories;
                if (!int.TryParse(qs["id"], out id) || !int.TryParse(qs["categories"], out categories))
                {
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                }
                // start 构建json
                PushSongModel songModel=new PushSongModel();
                songModel.id = id;
                songModel.categories = categories;
                songModel.music_url = qs["music_url"];
                songModel.t_singer = qs["t_singer"];
                songModel.title = qs["title"];
                var jsonCommModel = new JsonCommModel<PushSongModel>(true, songModel);
                var jsonstr = JsonConvert.SerializeObject(jsonCommModel);
                //end
                //start 找到uid列表
                int tid;
                if (!int.TryParse(qs["tid"], out tid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                string strSql_GetSNByTid = string.Format(
                        "SELECT a.uid, glsn FROM [weiyun].[dbo].[wy_talkuser] a,[weiyun].[dbo].app_users b where a.tid={0} and a.uid=b.uid and ISNULL(a.uidtype,'')<>1",
                        tid);
                MediaService.WriteLog("接收到GoloPushSong_sql参数：" + strSql_GetSNByTid, MediaService.wirtelog);
                var dt = SqlHelper.ExecuteTable(strSql_GetSNByTid);
                List<int> uidList = new List<int>();
                List<string> snList = new List<string>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    uidList.Add(Convert.ToInt32(dt.Rows[i]["uid"]));
                    snList.Add(dt.Rows[i]["glsn"].ToString());
                }

                Dictionary<string, int> dic = new Dictionary<string, int>();
                List<int> uidListOnline = new List<int>();//在线的uid
                for (int i = 0; i < uidList.Count(); i++) ////根据uid查询在线状态
                {
                    UserObject uo;
                    int isOnline = 0;
                    if (MediaService.userDic.TryGetValue(uidList[i], out uo))
                    {
                        if (uo != null && uo.socket[CommFunc.APPID] != null)
                        {
                            isOnline = 1;
                            uidListOnline.Add(uidList[i]);
                        }

                    }
                    //dic.Add(snList[i], isOnline);
                }
                //end
                var tmp = PublicClass.SendToOnlineUserList(null, jsonstr, "", uidListOnline, 99, 0, CommType.PushSong, CommFunc.APPID);
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GoloPushSong出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            
        }

        /// <summary>
        /// 推送消息
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GoloPushTTSOrAlarm(NameValueCollection qs)
        {
            try
            {
                if (qs == null || qs["tid"] == null || qs["text"] == null || qs["timing"] == null)
                {
                    string recv = CommFunc.StandardFormat(MessageCode.MissKey);
                    MediaService.WriteLog("接收到GoloPushTTSOrAlarm ：" + recv, MediaService.wirtelog);
                    return recv;
                }
                long timeing;
                if (!long.TryParse(qs["timing"], out timeing))
                {
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                }
                // start 构建json
                PushMsgModel msgModel = new PushMsgModel();
                msgModel.text = qs["text"];
                msgModel.timing = timeing;
                var jsonCommModel = new JsonCommModel<PushMsgModel>(true, msgModel);
                var jsonstr = JsonConvert.SerializeObject(jsonCommModel);
                //end
                //start 找到uid列表
                int tid;
                if (!int.TryParse(qs["tid"], out tid))
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                string strSql_GetSNByTid = string.Format(
                        "SELECT a.uid, glsn FROM [weiyun].[dbo].[wy_talkuser] a,[weiyun].[dbo].app_users b where a.tid={0} and a.uid=b.uid and ISNULL(a.uidtype,'')<>1",
                        tid);
                MediaService.WriteLog("接收到GoloPushTTSOrAlarm参数：" + strSql_GetSNByTid, MediaService.wirtelog);
                var dt = SqlHelper.ExecuteTable(strSql_GetSNByTid);
                List<int> uidList = new List<int>();
                List<string> snList = new List<string>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    uidList.Add(Convert.ToInt32(dt.Rows[i]["uid"]));
                    snList.Add(dt.Rows[i]["glsn"].ToString());
                }

                Dictionary<string, int> dic = new Dictionary<string, int>();
                List<int> uidListOnline = new List<int>();//在线的uid
                for (int i = 0; i < uidList.Count(); i++) ////根据uid查询在线状态
                {
                    UserObject uo;
                    int isOnline = 0;
                    if (MediaService.userDic.TryGetValue(uidList[i], out uo))
                    {
                        if (uo != null && uo.socket[CommFunc.APPID] != null)
                        {
                            isOnline = 1;
                            uidListOnline.Add(uidList[i]);
                        }

                    }
                    //dic.Add(snList[i], isOnline);
                }
                //end
                var tmp = PublicClass.SendToOnlineUserList(null, jsonstr, "", uidListOnline, 99, 0, CommType.PushMsg, CommFunc.APPID);
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GoloPushTTSOrAlarm出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }

        /// <summary>
        /// 公通的http请求
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GoloCommPush(NameValueCollection qs)
        {
            /*ouid,appid,token,uidlist,extData(要转发的json)*/
            try
            {
                string recv = CommFunc.StandardFormat(MessageCode.MissKey);
                if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["uidlist"] == null ||
                    qs["extData"] == null)
                {
                    MediaService.WriteLog("接收到GoloCommPush ：" + recv, MediaService.wirtelog);
                    return recv;
                }
                //将逗号分割的字符串转为list
                List<string> uidList = qs["uidList"].Split(',').ToList();
                //验证token
                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                List<int> uidListOnline = new List<int>();//在线的uid
                foreach (var uid in uidList)
                {
                    UserObject uo;
                    int tmpuid = Convert.ToInt32(uid);
                    if (MediaService.userDic.TryGetValue(tmpuid, out uo))
                    {
                        if (uo != null && uo.socket[CommFunc.APPID] != null)
                        {
                            uidListOnline.Add(tmpuid);
                        }

                    }
                } //根据uid查询在线状态

                PublicClass.SendToOnlineUserList(null, qs["extData"], "", uidListOnline, 99, 0, CommType.CommPush, CommFunc.APPID);
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GoloCommPush出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }

        //根据Ouid查询该ouid下所有绑定设备加入的所有频道
        public static string GetAllTalkByOuid(NameValueCollection qs)
        {
            /*ouid,token,appid, pageindex, pagesize*/
            try
            {
                string recv = CommFunc.StandardFormat(MessageCode.MissKey);
                if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["pageindex"] == null || qs["pagesize"] == null)
                {
                    MediaService.WriteLog("接收到GetAllTalkByOuid ：" + recv, MediaService.wirtelog);
                    return recv;
                }
                int pageindex, pagesize;
                if (!int.TryParse(qs["pageindex"], out pageindex) || !int.TryParse(qs["pagesize"], out pagesize))
                {
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                }

                //验证token
                int ouid = 0;
                int uid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                //查数据
                var strSql =
                    string.Format(
                        "select TOP {0} * FROM (select distinct ROW_NUMBER() OVER (ORDER BY T1.tid desc) AS RowNumber, T1.tid,T1.duijiang,t2.talknotice,T1.remark,T2.talkname,T2.auth,T2.createuid,T2.usernum,T2.type,T2.imageurl,T2.talkmode,T3.glsn from (select id,tid,xuhao,duijiang,remark from [wy_talkuser] where uid in (select t.uid from wy_uidmap t where t.ouid={2})) AS T1 INNER JOIN [wy_talk] AS T2 ON T1.tid = T2.tid inner join app_users t3 on t3.uid=t2.createuid) A WHERE RowNumber > {0}*({1}-1)", pagesize, pageindex,ouid);
                var datatable = SqlHelper.ExecuteTable(strSql);
                List<GetMyTalkResultModel> talkResultModels=new List<GetMyTalkResultModel>();
                if (datatable != null && datatable.Rows.Count > 0)
                {
                    foreach (DataRow dataRow in datatable.Rows)
                    {
                        talkResultModels.Add(new GetMyTalkResultModel()
                                             {
                                                 tid = dataRow["tid"].ToString(),
                                                 duijiang = dataRow["duijiang"].ToString(),
                                                 talknotice = dataRow["talknotice"].ToString(),
                                                 remark = dataRow["remark"].ToString(),
                                                 talkname = dataRow["talkname"].ToString(),
                                                 auth = dataRow["auth"].ToString(),
                                                 createuid = dataRow["createuid"].ToString(),
                                                 usernum = dataRow["usernum"].ToString(),
                                                 type = dataRow["type"].ToString(),
                                                 imageurl = dataRow["imageurl"].ToString(),
                                                 talkmode = dataRow["talkmode"].ToString(),
                                                 glsn = dataRow["glsn"].ToString(),
                                             });
                    }
                }

                var model = new AppJsonResultModel<List<GetMyTalkResultModel>>((int)MessageCode.Success,MessageCodeDiscription.GetMessageCodeDiscription(MessageCode.Success),talkResultModels);
                return JsonConvert.SerializeObject(model);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GoloCommPush出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 设备
        /// <summary>
        /// 设备在线状态
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetOnlineStatus(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * uid
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["uid"] == null)
            {
                MediaService.WriteLog("接收到GetOnlineStatus ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                int uid;
                if (int.TryParse(qs["uid"], out uid))
                {
                    if (uid > 0)
                    {
                        UserObject uo;
                        int isOnline = 0;
                        if (MediaService.userDic.TryGetValue(uid, out uo))
                        {
                            if (uo != null && uo.socket[CommFunc.APPID] != null)
                                isOnline = 1;
                        }
                        return CommFunc.StandardObjectFormat(MessageCode.Success, "{\"isonline\":" + isOnline + "}");
                    }
                    return CommFunc.StandardFormat(MessageCode.DeviceNotExist);
                }
                return CommFunc.StandardFormat(MessageCode.FormatError);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetOnlineStatus出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }

        /// <summary>
        /// 获取设备信息（JAVA调用）
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetDeviceInfo(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * sn
             */
            StringBuilder log = new StringBuilder("接收到GetDeviceInfo：");
            string recv;
            if (qs == null || qs["sn"] == null)
            {
                recv = CommFunc.StandardFormat(MessageCode.MissKey);
                log.Append(recv);
            }
            else
            {
                try
                {
                    //查询设备是否存在
                    string glsn = CommFunc.GetUniform12(qs["sn"]);
                    log.Append(" glsn=").Append(glsn);
                    const string strSql = "SELECT uid FROM app_users WHERE glsn = @glsn";
                    SqlParameter[] paras = { new SqlParameter("@glsn", glsn) };
                    object sqlResult = SqlHelper.ExecuteScalar(strSql, paras);
                    if (sqlResult == null)
                    { recv = CommFunc.StandardFormat(MessageCode.DeviceNotExist); }
                    else
                    {
                        int uid = int.Parse(sqlResult.ToString());
                        recv = CommFunc.StandardObjectFormat(MessageCode.Success, "{\"uid\":" + uid + "}");
                    }
                }
                catch (Exception e)
                {
                    log.Append(" 错误信息：").Append(e);
                    recv = CommFunc.StandardFormat(MessageCode.DefaultError);
                }
            }
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }

        public static string GetDeviceInfoByUid(NameValueCollection qs)
        {
            /*ouid,token,appid,uids*/
            try
            {
                string recv = CommFunc.StandardFormat(MessageCode.MissKey);
                if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null || qs["pageindex"] == null || qs["pagesize"] == null || qs["uids"]==null)
                {
                    MediaService.WriteLog("接收到GetDeviceInfoByUid ：" + recv, MediaService.wirtelog);
                    return recv;
                }
                int pageindex, pagesize;
                if (!int.TryParse(qs["pageindex"], out pageindex) || !int.TryParse(qs["pagesize"], out pagesize))
                {
                    return CommFunc.StandardFormat(MessageCode.FormatError);
                }

                //验证token
                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                //根据uid查找mongoDB
                SortByDocument sd = new SortByDocument { { "Time", -1 } }; //-1表示DESC,1表示ASC
                FieldsDocument fd = new FieldsDocument { { "_id", 0 } };
                
                List<PowerInfo> resultList=new List<PowerInfo>();
                foreach (var uid in qs["uids"].Split(',').ToList())
                {
                    int iUid = Convert.ToInt32(uid);
                    MongoCollection mongoCollection =
                    MediaService.MongoDatabasePower.GetCollection(string.Format("power_{0}", iUid)); //选择集合
                    var query = new QueryDocument { { "Uid", iUid } };
                    var powerCursor = mongoCollection.FindAs<PowerInfo>(query).SetSortOrder(sd).SetLimit(1).SetFields(fd);
                    foreach (PowerInfo powerinfo in powerCursor)
                    {
                        resultList.Add(new PowerInfo()
                                       {
                                           Power = powerinfo.Power,
                                           State = powerinfo.State,
                                           Time = powerinfo.Time,
                                           Uid = powerinfo.Uid
                                       });
                        break;
                    }
                }
                var resultModel = new AppJsonResultModel<List<PowerInfo>>((int)MessageCode.Success,
                    MessageCodeDiscription.GetMessageCodeDiscription(MessageCode.Success), resultList);
                return JsonConvert.SerializeObject(resultModel);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetDeviceInfoByUid出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }

        public static string SendStateToDevices(NameValueCollection qs)
        {
            /*ouid,token,appid,state,[tid],uids,nickname*/
            try
            {
                string recv = CommFunc.StandardFormat(MessageCode.MissKey);
                if (qs == null || qs["state"] == null || qs["uids"] == null || qs["nickname"] == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
                {
                    MediaService.WriteLog("接收到GetDeviceInfoByUid ：" + recv, MediaService.wirtelog);
                    return recv;
                }
                int tid = 0;
                var uidList = qs["uids"].Split(',').ToList();
                if (qs["tid"] == null)
                {
                    tid = CommFunc.GenerateTid();
                }
                else
                {
                    tid = Convert.ToInt32(qs["tid"]);
                }
                //验证token
                int ouid = 0;
                bool isVerToken = CommFunc.UniformVerification(qs["ouid"], qs["appid"], qs["token"], ref ouid, ref recv);
                if (!isVerToken)
                    return recv;

                List<int> uidListOnline = new List<int>();//在线的uid
                List<UidIsOnLineModel> uidIsOnLineModels=new List<UidIsOnLineModel>();
                foreach (var uid in uidList)
                {
                    UserObject uo;
                    int tmpuid = Convert.ToInt32(uid);
                    if (MediaService.userDic.TryGetValue(tmpuid, out uo))
                    {
                        if (uo != null && uo.socket[CommFunc.APPID] != null)
                        {
                            uidListOnline.Add(tmpuid);
                            uidIsOnLineModels.Add(new UidIsOnLineModel()
                                                  {
                                                      isonline = 1,
                                                      uid = uid
                                                  });
                        }
                        else
                        {
                            uidIsOnLineModels.Add(new UidIsOnLineModel()
                            {
                                isonline = 0,
                                uid = uid
                            });
                        }
                    }
                    else
                    {
                        var tmp = new UidIsOnLineModel()
                                  {
                                      isonline = 0,
                                      uid = uid
                                  };
                        if (!uidIsOnLineModels.Contains(tmp))
                        {
                            uidIsOnLineModels.Add(tmp);
                        }
                    }
                } //根据uid查询在线状态
                var data = new {Tid = tid, State = qs["state"], NickName = qs["nickname"]};
                var json = JsonConvert.SerializeObject(data);
                PublicClass.SendToOnlineUserList(null, json, "", uidListOnline, 99, 0, CommType.AppLaunchCall, CommFunc.APPID); //向在线设备转发呼叫状态

                AppCallDeviceModel model = new AppCallDeviceModel {tid = tid, status = uidIsOnLineModels};
                var returnStr = new AppJsonResultModel<AppCallDeviceModel>((int)MessageCode.Success,MessageCodeDiscription.GetMessageCodeDiscription(MessageCode.Success),model);
                return JsonConvert.SerializeObject(returnStr);
            }
            catch (Exception ex)
            {
                MediaService.WriteLog(string.Format("SendStateToDevices异常:{0}",ex.Message),true);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }

        #endregion
        #region GetSimBySN根据SN获取SIM卡号

        public static string GetSimBySN(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * sn
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["sn"] == null)
            {
                MediaService.WriteLog("接收到GetSimBySN ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                if(SqlHelper.ExecuteScalar(string.Format("select 1 from weiyun.dbo.app_users where glsn='{0}'", qs["sn"]))==null)
                {
                    return CommFunc.StandardFormat(MessageCode.DeviceNotExist);
                }
                var sql =
                    string.Format(
                        "select sim from weiyun.dbo.wy_uidmap um,weiyun.dbo.app_users au where um.uid=au.uid and au.glsn='{0}'",
                        qs["sn"]);
                var sim = SqlHelper.ExecuteScalar(sql);
                if (sim != null)
                {
                    string data = string.Format("{{\"sim\":\"{0}\"}}", sim);
                    return CommFunc.StandardObjectFormat(MessageCode.Success, data);
                }
                else
                {
                    return CommFunc.StandardFormat(MessageCode.DefaultError);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetSimBySN出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion
        #region 节目单
        /// <summary>
        /// 返回节目列表Json
        /// </summary>
        /// <returns></returns>
        public static string ReturnActListJson(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * rid
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["rid"] == null)
            {
                MediaService.WriteLog("接收到getactlist ：" + recv, MediaService.wirtelog);
                return recv;
            }
            DayOfWeek day = DateTime.Today.DayOfWeek;
            if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
            {
                return "{\"code\":0,\"msg\":\"\",\"data\":[]}";
            }
            //节目列表
            const string act = "{\"ActCategory\":\"新闻类\",\"Time\":\"7:30--7:55\",\"ActName\":\"轱辘早天下\",\"Compere\":\"录制节目\",\"Content\":\"国内外新闻资讯\",\"Hot\":5},"
                               + "{\"ActCategory\":\"段子搞笑类\",\"Time\":\"8:00--8:50\",\"ActName\":\"快乐早班车\",\"Compere\":\"酥饼\",\"Content\":\"新闻段子/搞笑段子\",\"Hot\":5},"
                               + "{\"ActCategory\":\"创业类\",\"Time\":\"9:00---9:30\",\"ActName\":\"华商启示录\",\"Compere\":\"录制节目\",\"Content\":\"分享赚钱，管理经验\",\"Hot\":5},"
                               + "{\"ActCategory\":\"音乐类\",\"Time\":\"10:00--10:30\",\"ActName\":\"翻唱音乐会\",\"Compere\":\"录制节目\",\"Content\":\"各种翻唱音乐\",\"Hot\":5},"
                               + "{\"ActCategory\":\"体育类\",\"Time\":\"11:00---11:40\",\"ActName\":\"体坛烩\",\"Compere\":\"一泽\",\"Content\":\"体育项目类\",\"Hot\":5},"
                               + "{\"ActCategory\":\"健康类\",\"Time\":\"11:40--12:10\",\"ActName\":\"食全食美\",\"Compere\":\"录制节目\",\"Content\":\"分享美食，供人们午餐参考\",\"Hot\":5},"
                               + "{\"ActCategory\":\"历史新编搞笑类\",\"Time\":\"13:30--14:10\",\"ActName\":\"包公笑传\",\"Compere\":\"录制节目\",\"Content\":\"不同时期历史人物爆笑合集\",\"Hot\":5},"
                               + "{\"ActCategory\":\"音乐类\",\"Time\":\"14:30--15:00\",\"ActName\":\"音乐下午茶\",\"Compere\":\"录制节目\",\"Content\":\"歌曲串烧\",\"Hot\":5},"
                               + "{\"ActCategory\":\"健康养生\",\"Time\":\"15:30--16:00\",\"ActName\":\"健康新知\",\"Compere\":\"录制节目\",\"Content\":\"养生健康窍门\",\"Hot\":5},"
                               + "{\"ActCategory\":\"娱乐八卦类\",\"Time\":\"16:00--17:20\",\"ActName\":\"娱乐那点儿事\",\"Compere\":\"吧啦+艺涵\",\"Content\":\"趣闻新闻\",\"Hot\":5},"
                               + "{\"ActCategory\":\"搞笑段子类\",\"Time\":\"17:30--18:30\",\"ActName\":\"越说越有趣\",\"Compere\":\"YOYO\",\"Content\":\"各种八卦娱乐\",\"Hot\":5},"
                               + "{\"ActCategory\":\"脱口秀类\",\"Time\":\"18:40--19:30\",\"ActName\":\"疯狂抢麦\",\"Compere\":\"酥饼+yoyo+一泽\",\"Content\":\"猜歌曲，赢奖品\",\"Hot\":5},"
                               + "{\"ActCategory\":\"音乐类\",\"Time\":\"20:00--20:30\",\"ActName\":\"怀旧金曲\",\"Compere\":\"录制节目\",\"Content\":\"怀旧情歌\",\"Hot\":5},"
                               + "{\"ActCategory\":\"脱口秀\",\"Time\":\"20:30--21:30\",\"ActName\":\"yo豆你玩儿\",\"Compere\":\"YOYO,吧啦\",\"Content\":\"搞笑段子\",\"Hot\":5},"
                               + "{\"ActCategory\":\"情感夜话类\",\"Time\":\"22:00--23:30\",\"ActName\":\"凡人故事\",\"Compere\":\"录制节目\",\"Content\":\"情感故事分享\",\"Hot\":5}";
            return "{\"code\":0,\"msg\":\"\",\"data\":[" + act + "]}";
        }
        #endregion

        #region 用户
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns></returns>
        public static string GetUserInfo(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null)
            {
                MediaService.WriteLog("接收到GetUserInfo ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到GetUserInfo   ouid=" + qs["ouid"], MediaService.wirtelog);
                int ouid;
                int.TryParse(qs["ouid"], out ouid);
                if (ouid > 0)
                {
                    string sql = "SELECT nick_name,face_url FROM wy_user WHERE [user_id]=" + ouid;
                    var dt = SqlHelper.ExecuteTable(sql);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        var row = dt.Rows[0];
                        string data = string.Format("{{\"nick_name\":\"{0}\",\"face_url\":\"{1}\"}}", row["nick_name"], row["face_url"]);
                        return CommFunc.StandardObjectFormat(MessageCode.Success, data);
                    }
                    return CommFunc.StandardFormat(MessageCode.UserNotExist);
                }
                return CommFunc.StandardFormat(MessageCode.FormatError);
            }
            catch (Exception ex)
            {
                MediaService.WriteLog("接收到GetUserInfo出错：" + ex.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }

        /// <summary>
        /// 获取用户信息和点赞数量
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetUserPraiseNum(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * sn
             */
            StringBuilder log = new StringBuilder("接收到GetUserPraiseNum ");
            string recv;
            if (qs == null || qs["sn"] == null)
            {
                recv = CommFunc.StandardFormat(MessageCode.MissKey);
            }
            else
            {
                try
                {
                    string glsn = CommFunc.GetUniform12(qs["sn"]);
                    log.Append(" glsn=").Append(glsn);

                    const string strSql = "SELECT A.[uid],M.ouid,U.nick_name,U.face_url FROM app_users A INNER JOIN wy_uidmap M ON A.glsn=@glsn AND A.[uid]=M.[uid] LEFT JOIN wy_user U ON M.ouid=U.[user_id]";
                    SqlParameter[] paras = { new SqlParameter("@glsn", glsn) };
                    var dt = SqlHelper.ExecuteTable(strSql, paras);
                    if (dt == null || dt.Rows.Count == 0)
                    { recv = CommFunc.StandardFormat(MessageCode.DeviceNotExist); }
                    else
                    {
                        var row = dt.Rows[0];
                        int uid = int.Parse(row["uid"].ToString());
                        var nickname = row["nick_name"].ToString();
                        var faceUrl = row["face_url"].ToString();

                        recv = CommFunc.StandardObjectFormat(MessageCode.Success, string.Format("{{\"uid\":{0},\"nickname\":\"{1}\",\"faceurl\":\"{2}\",\"praisenum\":0}}", uid.ToString(), nickname, faceUrl));
                    }
                }
                catch (Exception e)
                {
                    log.Append(" 错误信息：").Append(e);
                    recv = CommFunc.StandardFormat(MessageCode.DefaultError);
                }
            }
            log.Append(recv);
            MediaService.WriteLog(log.ToString(), MediaService.wirtelog);
            return recv;
        }
        #endregion

        public static ConcurrentDictionary<int, string> DicTalk = new ConcurrentDictionary<int, string>();
        #region 获取我参与的活动
        /// <summary>
        /// 获取我参与的活动
        /// </summary>
        /// <returns></returns>
        public static string GetMyActivities(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null)
            {
                MediaService.WriteLog("接收到GetMyActivities ：" + recv, MediaService.wirtelog);
                return recv;
            }
            int ouid;
            int.TryParse(qs["ouid"], out ouid);
            if (ouid > 0)
            {
                try
                {
                    var query = new QueryDocument { { "Ouid", ouid } };
                    var sb = new StringBuilder(128);
                    List<long> msgIds = new List<long>();

                    DateTime date = DateTime.Today;
                    while (date >= new DateTime(2016, 1, 1))
                    {
                        MongoCollection col = MediaService.mongoDataBase.GetCollection("Parise_" + ouid % 10 + "_" + date.ToString("yyyyMM"));
                        var parises = col.FindAs<Parise>(query).OrderByDescending(m => m.LastModiTime);
                        foreach (var p in parises)
                        {
                            if (p.Tid == 0) continue;
                            if (!msgIds.Contains(p.MsgTime))
                            {
                                msgIds.Add(p.MsgTime);

                                DateTime time = Utility.StampToDateTime(p.LastModiTime.ToString());
                                string name = "";
                                if (p.MsgTime > int.MaxValue)
                                {
                                    RadioObject radio;
                                    MediaService.radioDic.TryGetValue(p.Tid, out radio);
                                    if (radio != null)
                                    {
                                        string radioName = radio.cityname;
                                        if ((radioName + "").Length == 0)
                                        {
                                            radioName = radio.channelname;
                                        }
                                        name = radioName;
                                    }
                                }
                                else
                                {
                                    if (DicTalk.ContainsKey(p.Tid))
                                    {
                                        name = DicTalk[p.Tid];
                                    }
                                    else
                                    {
                                        string sql = "SELECT talkname FROM wy_talk WHERE tid=" + p.Tid;
                                        var obj = SqlHelper.ExecuteScalar(sql);
                                        if (obj != null) name = obj.ToString();
                                        else name = "此群组已删除";
                                        DicTalk.TryAdd(p.Tid, name);
                                    }
                                }
                                sb.AppendFormat(",{{\"activityname\":\"\",\"radioname\":\"{0}\",\"time\":\"{1}\",\"messageid\":\"{2}\",\"radioid\":\"{3}\"}}", name, time, p.MsgTime, p.Tid.ToString());//activityname暂时未定
                            }
                        }
                        date = date.AddMonths(-1);
                    }
                    if (sb.Length > 0)
                    {
                        sb.Remove(0, 1);
                    }
                    MediaService.WriteLog("接收到GetMyActivities result：" + sb, MediaService.wirtelog);
                    return CommFunc.StandardListFormat(MessageCode.Success, sb.ToString());
                }
                catch (Exception ex)
                {
                    MediaService.WriteLog("接收到GetMyActivities 出错：" + ex, MediaService.wirtelog);
                    return CommFunc.StandardFormat(MessageCode.DefaultError);
                }
            }
            else
            {
                return CommFunc.StandardFormat(MessageCode.FormatError);
            }
        }
        #endregion

        #region 车牌

        #region 增加车牌
        /// <summary>
        /// 增加车牌
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string AddPlate(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,plate
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["plate"] == null)
            {
                MediaService.WriteLog("接收到AddPlate ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string plate = qs["plate"];
                string ouid = qs["ouid"];
                MediaService.WriteLog("接收到AddPlate ：ouid=" + ouid + " plate=" + plate, MediaService.wirtelog);
                string sql = string.Format("SELECT 1 FROM wy_userplate WHERE plate='{0}'", plate);
                object obj = SqlHelper.ExecuteScalar(sql);
                if (obj == null)
                {
                    sql = string.Format("INSERT INTO wy_userplate(ouid,plate) VALUES({0},'{1}');SELECT @@IDENTITY;", ouid, plate);
                    obj = SqlHelper.ExecuteScalar(sql);
                    MediaService.WriteLog("AddPlate pid=" + obj, MediaService.wirtelog);
                    return CommFunc.StandardObjectFormat(MessageCode.Success, "{\"pid\":" + obj + "}");
                }
                return CommFunc.StandardFormat(MessageCode.InsertFaild, "车牌号已存在");
            }
            catch (Exception e)
            {
                MediaService.WriteLog("AddPlate出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 获取车牌
        /// <summary>
        /// 获取车牌
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string GetPlateList(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null)
            {
                MediaService.WriteLog("接收到GetPlateList ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string ouid = qs["ouid"];
                MediaService.WriteLog("接收到GetPlateList ：ouid=" + ouid, MediaService.wirtelog);
                string sql = "SELECT pid,plate FROM wy_userplate WHERE [ouid]=" + ouid;
                StringBuilder sb = new StringBuilder(64);
                foreach (DataRow row in SqlHelper.ExecuteTable(sql).Rows)
                {
                    sb.AppendFormat(",{{\"pid\":{0},\"plate\":\"{1}\"}}", row["pid"], row["plate"]);
                }
                if (sb.Length > 0)
                {
                    sb.Remove(0, 1);
                }
                MediaService.WriteLog("GetPlateList result json： " + sb, MediaService.wirtelog);
                return CommFunc.StandardListFormat(MessageCode.Success, sb.ToString());
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetPlateList出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 修改车牌
        /// <summary>
        /// 修改车牌
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string UpdatePlate(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,pid,plate
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["pid"] == null || qs["plate"] == null)
            {
                MediaService.WriteLog("接收到UpdatePlate ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string plate = qs["plate"];
                string pid = qs["pid"];
                string ouid = qs["ouid"];
                MediaService.WriteLog("接收到UpdatePlate ：ouid =" + ouid + " pid=" + pid + " plate=" + plate, MediaService.wirtelog);
                string sql = string.Format("UPDATE wy_userplate SET plate='{0}' WHERE [ouid]={1} AND pid={2}", plate, ouid, pid);
                SqlHelper.ExecuteNonQuery(sql);
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("UpdatePlate出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        #region 删除车牌
        /// <summary>
        /// 删除车牌
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        public static string DeletePlate(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,pid
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["pid"] == null)
            {
                MediaService.WriteLog("接收到DeletePlate ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                string pid = qs["pid"];
                string ouid = qs["ouid"];
                MediaService.WriteLog("接收到DeletePlate ：ouid =" + ouid + " pid=" + pid, MediaService.wirtelog);
                string sql = "DELETE FROM wy_userplate WHERE pid=" + pid + " AND [ouid]=" + ouid;
                SqlHelper.ExecuteNonQuery(sql);
                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("DeletePlate出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion
        #endregion

        #region 接收头像接口
        public static string SetFace(NameValueCollection qs, byte[] fileContext)
        {
            /* NameValueCollection 值列表
             * ouid,token,appid
             */

            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到ReceiveImg ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                MediaService.WriteLog("接收到ReceiveImg ：ouid =" + qs["ouid"] + "&token =" + qs["token"] + "appid =" + qs["appid"], MediaService.wirtelog);

                MyCarAdapter.User_Service_Setface(qs["ouid"], qs["appid"], qs["token"], fileContext, ref recv);

                return recv;
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        }

        #endregion

        #region 获取全部省市区
        /// <summary>
        /// 获取全部省市区
        /// </summary>
        /// <returns></returns>
        public static string GetAllArea()
        {
            try
            {
                MediaService.WriteLog("接收到 获取全部省市区 ", MediaService.wirtelog);
                #region 省市区
                const string result = "{\"code\":0,\"msg\":\"成功\",\"data\":[{\"city\":[{\"ccode\":1431001014,\"cid\":6205,\"display\":\"蚌埠市\",\"region\":[{\"display\":\"蚌山区\",\"rcode\":14310010141000,\"rid\":3327},{\"display\":\"禹会区\",\"rcode\":14310010141001,\"rid\":3328},{\"display\":\"淮上区\",\"rcode\":14310010141002,\"rid\":3329},{\"display\":\"怀远县\",\"rcode\":14310010141003,\"rid\":3330},{\"display\":\"五河县\",\"rcode\":14310010141004,\"rid\":3331},{\"display\":\"固镇县\",\"rcode\":14310010141005,\"rid\":3332},{\"display\":\"龙子湖区\",\"rcode\":14310010141006,\"rid\":3333}]},{\"ccode\":1431001006,\"cid\":6204,\"display\":\"安庆市\",\"region\":[{\"display\":\"大观区\",\"rcode\":14310010061000,\"rid\":3421},{\"display\":\"宜秀区\",\"rcode\":14310010061001,\"rid\":3422},{\"display\":\"怀宁县\",\"rcode\":14310010061002,\"rid\":3423},{\"display\":\"枞阳县\",\"rcode\":14310010061003,\"rid\":3424},{\"display\":\"潜山县\",\"rcode\":14310010061004,\"rid\":3425},{\"display\":\"太湖县\",\"rcode\":14310010061005,\"rid\":3426},{\"display\":\"宿松县\",\"rcode\":14310010061006,\"rid\":3427},{\"display\":\"望江县\",\"rcode\":14310010061007,\"rid\":3428},{\"display\":\"岳西县\",\"rcode\":14310010061008,\"rid\":3429},{\"display\":\"桐城市\",\"rcode\":14310010061009,\"rid\":3430},{\"display\":\"迎江区\",\"rcode\":14310010061010,\"rid\":3431}]},{\"ccode\":1431001029,\"cid\":6206,\"display\":\"亳州市\",\"region\":[{\"display\":\"涡阳县\",\"rcode\":14310010291000,\"rid\":3334},{\"display\":\"蒙城县\",\"rcode\":14310010291001,\"rid\":3335},{\"display\":\"利辛县\",\"rcode\":14310010291002,\"rid\":3336},{\"display\":\"谯城区\",\"rcode\":14310010291003,\"rid\":3337}]},{\"ccode\":1431001038,\"cid\":6207,\"display\":\"巢湖市\",\"region\":[]},{\"ccode\":1431001045,\"cid\":6208,\"display\":\"池州市\",\"region\":[{\"display\":\"东至县\",\"rcode\":14310010451000,\"rid\":3338},{\"display\":\"石台县\",\"rcode\":14310010451001,\"rid\":3339},{\"display\":\"青阳县\",\"rcode\":14310010451002,\"rid\":3340},{\"display\":\"贵池区\",\"rcode\":14310010451003,\"rid\":3341}]},{\"ccode\":1431001049,\"cid\":6209,\"display\":\"滁州市\",\"region\":[{\"display\":\"南谯区\",\"rcode\":14310010491000,\"rid\":3342},{\"display\":\"来安县\",\"rcode\":14310010491001,\"rid\":3343},{\"display\":\"全椒县\",\"rcode\":14310010491002,\"rid\":3344},{\"display\":\"定远县\",\"rcode\":14310010491003,\"rid\":3345},{\"display\":\"凤阳县\",\"rcode\":14310010491004,\"rid\":3346},{\"display\":\"天长市\",\"rcode\":14310010491005,\"rid\":3347},{\"display\":\"明光市\",\"rcode\":14310010491006,\"rid\":3348},{\"display\":\"琅琊区\",\"rcode\":14310010491007,\"rid\":3349}]},{\"ccode\":1431001071,\"cid\":6210,\"display\":\"阜阳市\",\"region\":[{\"display\":\"颍东区\",\"rcode\":14310010711000,\"rid\":3350},{\"display\":\"颍泉区\",\"rcode\":14310010711001,\"rid\":3351},{\"display\":\"临泉县\",\"rcode\":14310010711002,\"rid\":3352},{\"display\":\"太和县\",\"rcode\":14310010711003,\"rid\":3353},{\"display\":\"阜南县\",\"rcode\":14310010711004,\"rid\":3354},{\"display\":\"颍上县\",\"rcode\":14310010711005,\"rid\":3355},{\"display\":\"界首市\",\"rcode\":14310010711006,\"rid\":3356},{\"display\":\"颍州区\",\"rcode\":14310010711007,\"rid\":3357}]},{\"ccode\":1431001098,\"cid\":6211,\"display\":\"合肥市\",\"region\":[{\"display\":\"庐阳区\",\"rcode\":14310010981000,\"rid\":3358},{\"display\":\"蜀山区\",\"rcode\":14310010981001,\"rid\":3359},{\"display\":\"包河区\",\"rcode\":14310010981002,\"rid\":3360},{\"display\":\"长丰县\",\"rcode\":14310010981003,\"rid\":3361},{\"display\":\"肥东县\",\"rcode\":14310010981004,\"rid\":3362},{\"display\":\"肥西县\",\"rcode\":14310010981005,\"rid\":3363},{\"display\":\"庐江县\",\"rcode\":14310010981006,\"rid\":3364},{\"display\":\"巢湖市\",\"rcode\":14310010981007,\"rid\":3365},{\"display\":\"瑶海区\",\"rcode\":14310010981008,\"rid\":3366}]},{\"ccode\":1431001109,\"cid\":6212,\"display\":\"淮北市\",\"region\":[{\"display\":\"相山区\",\"rcode\":14310011091000,\"rid\":3367},{\"display\":\"烈山区\",\"rcode\":14310011091001,\"rid\":3368},{\"display\":\"濉溪县\",\"rcode\":14310011091002,\"rid\":3369},{\"display\":\"杜集区\",\"rcode\":14310011091003,\"rid\":3370}]},{\"ccode\":1431001111,\"cid\":6213,\"display\":\"淮南市\",\"region\":[{\"display\":\"田家庵区\",\"rcode\":14310011111000,\"rid\":3371},{\"display\":\"谢家集区\",\"rcode\":14310011111001,\"rid\":3372},{\"display\":\"八公山区\",\"rcode\":14310011111002,\"rid\":3373},{\"display\":\"潘集区\",\"rcode\":14310011111003,\"rid\":3374},{\"display\":\"凤台县\",\"rcode\":14310011111004,\"rid\":3375},{\"display\":\"大通区\",\"rcode\":14310011111005,\"rid\":3376}]},{\"ccode\":1431001115,\"cid\":6214,\"display\":\"黄山市\",\"region\":[{\"display\":\"黄山区\",\"rcode\":14310011151000,\"rid\":3377},{\"display\":\"徽州区\",\"rcode\":14310011151001,\"rid\":3378},{\"display\":\"歙　县\",\"rcode\":14310011151002,\"rid\":3379},{\"display\":\"休宁县\",\"rcode\":14310011151003,\"rid\":3380},{\"display\":\"黟　县\",\"rcode\":14310011151004,\"rid\":3381},{\"display\":\"祁门县\",\"rcode\":14310011151005,\"rid\":3382},{\"display\":\"屯溪区\",\"rcode\":14310011151006,\"rid\":3383}]},{\"ccode\":1431001165,\"cid\":6215,\"display\":\"六安市\",\"region\":[{\"display\":\"裕安区\",\"rcode\":14310011651000,\"rid\":3384},{\"display\":\"寿　县\",\"rcode\":14310011651001,\"rid\":3385},{\"display\":\"霍邱县\",\"rcode\":14310011651002,\"rid\":3386},{\"display\":\"舒城县\",\"rcode\":14310011651003,\"rid\":3387},{\"display\":\"金寨县\",\"rcode\":14310011651004,\"rid\":3388},{\"display\":\"霍山县\",\"rcode\":14310011651005,\"rid\":3389},{\"display\":\"金安区\",\"rcode\":14310011651006,\"rid\":3390}]},{\"ccode\":1431001176,\"cid\":6216,\"display\":\"马鞍山市\",\"region\":[{\"display\":\"雨山区\",\"rcode\":14310011761000,\"rid\":3391},{\"display\":\"博望区\",\"rcode\":14310011761001,\"rid\":3392},{\"display\":\"当涂县\",\"rcode\":14310011761002,\"rid\":3393},{\"display\":\"含山县\",\"rcode\":14310011761003,\"rid\":3394},{\"display\":\"和县\",\"rcode\":14310011761004,\"rid\":3395},{\"display\":\"花山区\",\"rcode\":14310011761005,\"rid\":3396}]},{\"ccode\":1431001247,\"cid\":6217,\"display\":\"宿州市\",\"region\":[{\"display\":\"砀山县\",\"rcode\":14310012471000,\"rid\":3397},{\"display\":\"萧　县\",\"rcode\":14310012471001,\"rid\":3398},{\"display\":\"灵璧县\",\"rcode\":14310012471002,\"rid\":3399},{\"display\":\"泗　县\",\"rcode\":14310012471003,\"rid\":3400},{\"display\":\"埇桥区\",\"rcode\":14310012471004,\"rid\":3401}]},{\"ccode\":1431001262,\"cid\":6218,\"display\":\"铜陵市\",\"region\":[{\"display\":\"狮子山区\",\"rcode\":14310012621000,\"rid\":3402},{\"display\":\"郊　区\",\"rcode\":14310012621001,\"rid\":3403},{\"display\":\"铜陵县\",\"rcode\":14310012621002,\"rid\":3404},{\"display\":\"铜官山区\",\"rcode\":14310012621003,\"rid\":3405}]},{\"ccode\":1431001272,\"cid\":6219,\"display\":\"芜湖市\",\"region\":[{\"display\":\"弋江区\",\"rcode\":14310012721000,\"rid\":3406},{\"display\":\"鸠江区\",\"rcode\":14310012721001,\"rid\":3407},{\"display\":\"三山区\",\"rcode\":14310012721002,\"rid\":3408},{\"display\":\"芜湖县\",\"rcode\":14310012721003,\"rid\":3409},{\"display\":\"繁昌县\",\"rcode\":14310012721004,\"rid\":3410},{\"display\":\"南陵县\",\"rcode\":14310012721005,\"rid\":3411},{\"display\":\"无为县\",\"rcode\":14310012721006,\"rid\":3412},{\"display\":\"镜湖区\",\"rcode\":14310012721007,\"rid\":3413}]},{\"ccode\":1431001296,\"cid\":6220,\"display\":\"宣城市\",\"region\":[{\"display\":\"郎溪县\",\"rcode\":14310012961000,\"rid\":3414},{\"display\":\"广德县\",\"rcode\":14310012961001,\"rid\":3415},{\"display\":\"泾　县\",\"rcode\":14310012961002,\"rid\":3416},{\"display\":\"绩溪县\",\"rcode\":14310012961003,\"rid\":3417},{\"display\":\"旌德县\",\"rcode\":14310012961004,\"rid\":3418},{\"display\":\"宁国市\",\"rcode\":14310012961005,\"rid\":3419},{\"display\":\"宣州区\",\"rcode\":14310012961006,\"rid\":3420}]}],\"display\":\"安徽\",\"pcode\":143100,\"pid\":682},{\"city\":[{\"ccode\":1431011023,\"cid\":6221,\"display\":\"北京市\",\"region\":[{\"display\":\"朝阳\",\"rcode\":14310110231000,\"rid\":548},{\"display\":\"大兴\",\"rcode\":14310110231002,\"rid\":550},{\"display\":\"东城\",\"rcode\":14310110231003,\"rid\":551},{\"display\":\"房山\",\"rcode\":14310110231004,\"rid\":552},{\"display\":\"丰台\",\"rcode\":14310110231005,\"rid\":553},{\"display\":\"海淀\",\"rcode\":14310110231006,\"rid\":554},{\"display\":\"怀柔\",\"rcode\":14310110231007,\"rid\":555},{\"display\":\"门头沟\",\"rcode\":14310110231008,\"rid\":556},{\"display\":\"密云\",\"rcode\":14310110231009,\"rid\":557},{\"display\":\"平谷\",\"rcode\":14310110231010,\"rid\":558},{\"display\":\"石景山\",\"rcode\":14310110231011,\"rid\":559},{\"display\":\"顺义\",\"rcode\":14310110231012,\"rid\":560},{\"display\":\"通州\",\"rcode\":14310110231013,\"rid\":561},{\"display\":\"西城\",\"rcode\":14310110231014,\"rid\":562},{\"display\":\"延庆\",\"rcode\":14310110231016,\"rid\":564},{\"display\":\"昌平\",\"rcode\":14310110231017,\"rid\":565}]}],\"display\":\"北京\",\"pcode\":143101,\"pid\":683},{\"city\":[{\"ccode\":1431021046,\"cid\":6223,\"display\":\"重庆市\",\"region\":[{\"display\":\"北碚\",\"rcode\":14310210461000,\"rid\":566},{\"display\":\"璧山\",\"rcode\":14310210461001,\"rid\":567},{\"display\":\"长寿\",\"rcode\":14310210461002,\"rid\":568},{\"display\":\"城口\",\"rcode\":14310210461003,\"rid\":569},{\"display\":\"大渡口\",\"rcode\":14310210461004,\"rid\":570},{\"display\":\"大足\",\"rcode\":14310210461005,\"rid\":571},{\"display\":\"垫江\",\"rcode\":14310210461006,\"rid\":572},{\"display\":\"丰都\",\"rcode\":14310210461007,\"rid\":573},{\"display\":\"奉节\",\"rcode\":14310210461008,\"rid\":574},{\"display\":\"涪陵\",\"rcode\":14310210461009,\"rid\":575},{\"display\":\"合川\",\"rcode\":14310210461010,\"rid\":576},{\"display\":\"江北\",\"rcode\":14310210461011,\"rid\":577},{\"display\":\"江津\",\"rcode\":14310210461012,\"rid\":578},{\"display\":\"九龙坡\",\"rcode\":14310210461013,\"rid\":579},{\"display\":\"开县\",\"rcode\":14310210461014,\"rid\":580},{\"display\":\"梁平\",\"rcode\":14310210461015,\"rid\":581},{\"display\":\"南岸\",\"rcode\":14310210461016,\"rid\":582},{\"display\":\"南川\",\"rcode\":14310210461017,\"rid\":583},{\"display\":\"彭水\",\"rcode\":14310210461018,\"rid\":584},{\"display\":\"黔江\",\"rcode\":14310210461019,\"rid\":585},{\"display\":\"綦江\",\"rcode\":14310210461020,\"rid\":586},{\"display\":\"荣昌\",\"rcode\":14310210461021,\"rid\":587},{\"display\":\"沙坪坝\",\"rcode\":14310210461022,\"rid\":588},{\"display\":\"石柱\",\"rcode\":14310210461023,\"rid\":589},{\"display\":\"双桥\",\"rcode\":14310210461024,\"rid\":590},{\"display\":\"铜梁\",\"rcode\":14310210461025,\"rid\":591},{\"display\":\"潼南\",\"rcode\":14310210461026,\"rid\":592},{\"display\":\"万盛\",\"rcode\":14310210461027,\"rid\":593},{\"display\":\"万州\",\"rcode\":14310210461028,\"rid\":594},{\"display\":\"武隆\",\"rcode\":14310210461029,\"rid\":595},{\"display\":\"巫山\",\"rcode\":14310210461030,\"rid\":596},{\"display\":\"巫溪\",\"rcode\":14310210461031,\"rid\":597},{\"display\":\"秀山\",\"rcode\":14310210461032,\"rid\":598},{\"display\":\"永川\",\"rcode\":14310210461033,\"rid\":599},{\"display\":\"酉阳\",\"rcode\":14310210461034,\"rid\":600},{\"display\":\"渝北\",\"rcode\":14310210461035,\"rid\":601},{\"display\":\"云阳\",\"rcode\":14310210461036,\"rid\":602},{\"display\":\"渝中\",\"rcode\":14310210461037,\"rid\":603},{\"display\":\"忠县\",\"rcode\":14310210461038,\"rid\":604},{\"display\":\"巴南\",\"rcode\":14310210461039,\"rid\":605},{\"display\":\"两江新区\",\"rcode\":14310210461040,\"rid\":606}]}],\"display\":\"重庆\",\"pcode\":143102,\"pid\":684},{\"city\":[{\"ccode\":1431031072,\"cid\":6225,\"display\":\"福州市\",\"region\":[{\"display\":\"台江区\",\"rcode\":14310310721000,\"rid\":679},{\"display\":\"仓山区\",\"rcode\":14310310721001,\"rid\":680},{\"display\":\"马尾区\",\"rcode\":14310310721002,\"rid\":681},{\"display\":\"晋安区\",\"rcode\":14310310721003,\"rid\":682},{\"display\":\"闽侯县\",\"rcode\":14310310721004,\"rid\":683},{\"display\":\"连江县\",\"rcode\":14310310721005,\"rid\":684},{\"display\":\"罗源县\",\"rcode\":14310310721006,\"rid\":685},{\"display\":\"闽清县\",\"rcode\":14310310721007,\"rid\":686},{\"display\":\"永泰县\",\"rcode\":14310310721008,\"rid\":687},{\"display\":\"平潭县\",\"rcode\":14310310721009,\"rid\":688},{\"display\":\"福清市\",\"rcode\":14310310721010,\"rid\":689},{\"display\":\"长乐市\",\"rcode\":14310310721011,\"rid\":690},{\"display\":\"鼓楼区\",\"rcode\":14310310721012,\"rid\":691}]},{\"ccode\":1431031170,\"cid\":6226,\"display\":\"龙岩市\",\"region\":[{\"display\":\"长汀县\",\"rcode\":14310311701000,\"rid\":607},{\"display\":\"永定县\",\"rcode\":14310311701001,\"rid\":608},{\"display\":\"上杭县\",\"rcode\":14310311701002,\"rid\":609},{\"display\":\"武平县\",\"rcode\":14310311701003,\"rid\":610},{\"display\":\"连城县\",\"rcode\":14310311701004,\"rid\":611},{\"display\":\"漳平市\",\"rcode\":14310311701005,\"rid\":612},{\"display\":\"新罗区\",\"rcode\":14310311701006,\"rid\":613}]},{\"ccode\":1431031187,\"cid\":6227,\"display\":\"南平市\",\"region\":[{\"display\":\"顺昌县\",\"rcode\":14310311871000,\"rid\":614},{\"display\":\"浦城县\",\"rcode\":14310311871001,\"rid\":615},{\"display\":\"光泽县\",\"rcode\":14310311871002,\"rid\":616},{\"display\":\"松溪县\",\"rcode\":14310311871003,\"rid\":617},{\"display\":\"政和县\",\"rcode\":14310311871004,\"rid\":618},{\"display\":\"邵武市\",\"rcode\":14310311871005,\"rid\":619},{\"display\":\"武夷山市\",\"rcode\":14310311871006,\"rid\":620},{\"display\":\"建瓯市\",\"rcode\":14310311871007,\"rid\":621},{\"display\":\"建阳市\",\"rcode\":14310311871008,\"rid\":622},{\"display\":\"延平区\",\"rcode\":14310311871009,\"rid\":623}]},{\"ccode\":1431031193,\"cid\":6228,\"display\":\"宁德市\",\"region\":[{\"display\":\"霞浦县\",\"rcode\":14310311931000,\"rid\":624},{\"display\":\"古田县\",\"rcode\":14310311931001,\"rid\":625},{\"display\":\"屏南县\",\"rcode\":14310311931002,\"rid\":626},{\"display\":\"寿宁县\",\"rcode\":14310311931003,\"rid\":627},{\"display\":\"周宁县\",\"rcode\":14310311931004,\"rid\":628},{\"display\":\"柘荣县\",\"rcode\":14310311931005,\"rid\":629},{\"display\":\"福安市\",\"rcode\":14310311931006,\"rid\":630},{\"display\":\"福鼎市\",\"rcode\":14310311931007,\"rid\":631},{\"display\":\"蕉城区\",\"rcode\":14310311931008,\"rid\":632}]},{\"ccode\":1431031200,\"cid\":6229,\"display\":\"莆田市\",\"region\":[{\"display\":\"涵江区\",\"rcode\":14310312001000,\"rid\":633},{\"display\":\"荔城区\",\"rcode\":14310312001001,\"rid\":634},{\"display\":\"秀屿区\",\"rcode\":14310312001002,\"rid\":635},{\"display\":\"仙游县\",\"rcode\":14310312001003,\"rid\":636},{\"display\":\"城厢区\",\"rcode\":14310312001004,\"rid\":637}]},{\"ccode\":1431031212,\"cid\":6230,\"display\":\"泉州市\",\"region\":[{\"display\":\"丰泽区\",\"rcode\":14310312121000,\"rid\":638},{\"display\":\"洛江区\",\"rcode\":14310312121001,\"rid\":639},{\"display\":\"泉港区\",\"rcode\":14310312121002,\"rid\":640},{\"display\":\"惠安县\",\"rcode\":14310312121003,\"rid\":641},{\"display\":\"安溪县\",\"rcode\":14310312121004,\"rid\":642},{\"display\":\"永春县\",\"rcode\":14310312121005,\"rid\":643},{\"display\":\"德化县\",\"rcode\":14310312121006,\"rid\":644},{\"display\":\"金门县\",\"rcode\":14310312121007,\"rid\":645},{\"display\":\"石狮市\",\"rcode\":14310312121008,\"rid\":646},{\"display\":\"晋江市\",\"rcode\":14310312121009,\"rid\":647},{\"display\":\"南安市\",\"rcode\":14310312121010,\"rid\":648},{\"display\":\"鲤城区\",\"rcode\":14310312121011,\"rid\":649}]},{\"ccode\":1431031218,\"cid\":6231,\"display\":\"三明市\",\"region\":[{\"display\":\"三元区\",\"rcode\":14310312181000,\"rid\":650},{\"display\":\"明溪县\",\"rcode\":14310312181001,\"rid\":651},{\"display\":\"清流县\",\"rcode\":14310312181002,\"rid\":652},{\"display\":\"宁化县\",\"rcode\":14310312181003,\"rid\":653},{\"display\":\"大田县\",\"rcode\":14310312181004,\"rid\":654},{\"display\":\"尤溪县\",\"rcode\":14310312181005,\"rid\":655},{\"display\":\"沙　县\",\"rcode\":14310312181006,\"rid\":656},{\"display\":\"将乐县\",\"rcode\":14310312181007,\"rid\":657},{\"display\":\"泰宁县\",\"rcode\":14310312181008,\"rid\":658},{\"display\":\"建宁县\",\"rcode\":14310312181009,\"rid\":659},{\"display\":\"永安市\",\"rcode\":14310312181010,\"rid\":660},{\"display\":\"梅列区\",\"rcode\":14310312181011,\"rid\":661}]},{\"ccode\":1431031280,\"cid\":6232,\"display\":\"厦门市\",\"region\":[{\"display\":\"海沧区\",\"rcode\":14310312801000,\"rid\":662},{\"display\":\"湖里区\",\"rcode\":14310312801001,\"rid\":663},{\"display\":\"集美区\",\"rcode\":14310312801002,\"rid\":664},{\"display\":\"同安区\",\"rcode\":14310312801003,\"rid\":665},{\"display\":\"翔安区\",\"rcode\":14310312801004,\"rid\":666},{\"display\":\"思明区\",\"rcode\":14310312801005,\"rid\":667}]},{\"ccode\":1431031329,\"cid\":6233,\"display\":\"漳州市\",\"region\":[{\"display\":\"龙文区\",\"rcode\":14310313291000,\"rid\":668},{\"display\":\"云霄县\",\"rcode\":14310313291001,\"rid\":669},{\"display\":\"漳浦县\",\"rcode\":14310313291002,\"rid\":670},{\"display\":\"诏安县\",\"rcode\":14310313291003,\"rid\":671},{\"display\":\"长泰县\",\"rcode\":14310313291004,\"rid\":672},{\"display\":\"东山县\",\"rcode\":14310313291005,\"rid\":673},{\"display\":\"南靖县\",\"rcode\":14310313291006,\"rid\":674},{\"display\":\"平和县\",\"rcode\":14310313291007,\"rid\":675},{\"display\":\"华安县\",\"rcode\":14310313291008,\"rid\":676},{\"display\":\"龙海市\",\"rcode\":14310313291009,\"rid\":677},{\"display\":\"芗城区\",\"rcode\":14310313291010,\"rid\":678}]}],\"display\":\"福建\",\"pcode\":143103,\"pid\":685},{\"city\":[{\"ccode\":1431041013,\"cid\":6234,\"display\":\"白银市\",\"region\":[{\"display\":\"永昌县\",\"rcode\":14310410131000,\"rid\":776},{\"display\":\"金川区\",\"rcode\":14310410131001,\"rid\":777}]},{\"ccode\":1431041060,\"cid\":6235,\"display\":\"定西市\",\"region\":[{\"display\":\"通渭县\",\"rcode\":14310410601000,\"rid\":692},{\"display\":\"陇西县\",\"rcode\":14310410601001,\"rid\":693},{\"display\":\"渭源县\",\"rcode\":14310410601002,\"rid\":694},{\"display\":\"临洮县\",\"rcode\":14310410601003,\"rid\":695},{\"display\":\"漳　县\",\"rcode\":14310410601004,\"rid\":696},{\"display\":\"岷　县\",\"rcode\":14310410601005,\"rid\":697},{\"display\":\"安定区\",\"rcode\":14310410601006,\"rid\":698}]},{\"ccode\":1431041074,\"cid\":6236,\"display\":\"甘南藏族自治州\",\"region\":[{\"display\":\"临潭县\",\"rcode\":14310410741000,\"rid\":699},{\"display\":\"卓尼县\",\"rcode\":14310410741001,\"rid\":700},{\"display\":\"舟曲县\",\"rcode\":14310410741002,\"rid\":701},{\"display\":\"迭部县\",\"rcode\":14310410741003,\"rid\":702},{\"display\":\"玛曲县\",\"rcode\":14310410741004,\"rid\":703},{\"display\":\"碌曲县\",\"rcode\":14310410741005,\"rid\":704},{\"display\":\"夏河县\",\"rcode\":14310410741006,\"rid\":705},{\"display\":\"合作市\",\"rcode\":14310410741007,\"rid\":706}]},{\"ccode\":1431041127,\"cid\":6237,\"display\":\"嘉峪关市\",\"region\":[{\"display\":\"麦积区\",\"rcode\":14310411271000,\"rid\":707},{\"display\":\"清水县\",\"rcode\":14310411271001,\"rid\":708},{\"display\":\"秦安县\",\"rcode\":14310411271002,\"rid\":709},{\"display\":\"甘谷县\",\"rcode\":14310411271003,\"rid\":710},{\"display\":\"武山县\",\"rcode\":14310411271004,\"rid\":711},{\"display\":\"张家川回族自治县\",\"rcode\":14310411271005,\"rid\":712},{\"display\":\"秦州区\",\"rcode\":14310411271006,\"rid\":713}]},{\"ccode\":1431041130,\"cid\":6238,\"display\":\"金昌市\",\"region\":[]},{\"ccode\":1431041140,\"cid\":6239,\"display\":\"酒泉市\",\"region\":[{\"display\":\"金塔县\",\"rcode\":14310411401000,\"rid\":714},{\"display\":\"瓜州县\",\"rcode\":14310411401001,\"rid\":715},{\"display\":\"肃北蒙古族自治县\",\"rcode\":14310411401002,\"rid\":716},{\"display\":\"阿克塞哈萨克族自治县\",\"rcode\":14310411401003,\"rid\":717},{\"display\":\"玉门市\",\"rcode\":14310411401004,\"rid\":718},{\"display\":\"敦煌市\",\"rcode\":14310411401005,\"rid\":719},{\"display\":\"肃州区\",\"rcode\":14310411401006,\"rid\":720}]},{\"ccode\":1431041150,\"cid\":6240,\"display\":\"兰州市\",\"region\":[{\"display\":\"七里河区\",\"rcode\":14310411501000,\"rid\":721},{\"display\":\"西固区\",\"rcode\":14310411501001,\"rid\":722},{\"display\":\"安宁区\",\"rcode\":14310411501002,\"rid\":723},{\"display\":\"红古区\",\"rcode\":14310411501003,\"rid\":724},{\"display\":\"永登县\",\"rcode\":14310411501004,\"rid\":725},{\"display\":\"皋兰县\",\"rcode\":14310411501005,\"rid\":726},{\"display\":\"榆中县\",\"rcode\":14310411501006,\"rid\":727},{\"display\":\"城关区\",\"rcode\":14310411501007,\"rid\":728}]},{\"ccode\":1431041161,\"cid\":6241,\"display\":\"临夏回族自治州\",\"region\":[{\"display\":\"临夏县\",\"rcode\":14310411611000,\"rid\":729},{\"display\":\"康乐县\",\"rcode\":14310411611001,\"rid\":730},{\"display\":\"永靖县\",\"rcode\":14310411611002,\"rid\":731},{\"display\":\"广河县\",\"rcode\":14310411611003,\"rid\":732},{\"display\":\"和政县\",\"rcode\":14310411611004,\"rid\":733},{\"display\":\"东乡族自治县\",\"rcode\":14310411611005,\"rid\":734},{\"display\":\"积石山保安族东乡族撒拉族自治县\",\"rcode\":14310411611006,\"rid\":735},{\"display\":\"临夏市\",\"rcode\":14310411611007,\"rid\":736}]},{\"ccode\":1431041169,\"cid\":6242,\"display\":\"陇南市\",\"region\":[{\"display\":\"成　县\",\"rcode\":14310411691000,\"rid\":737},{\"display\":\"文　县\",\"rcode\":14310411691001,\"rid\":738},{\"display\":\"宕昌县\",\"rcode\":14310411691002,\"rid\":739},{\"display\":\"康　县\",\"rcode\":14310411691003,\"rid\":740},{\"display\":\"西和县\",\"rcode\":14310411691004,\"rid\":741},{\"display\":\"礼　县\",\"rcode\":14310411691005,\"rid\":742},{\"display\":\"徽　县\",\"rcode\":14310411691006,\"rid\":743},{\"display\":\"两当县\",\"rcode\":14310411691007,\"rid\":744},{\"display\":\"武都区\",\"rcode\":14310411691008,\"rid\":745}]},{\"ccode\":1431041198,\"cid\":6243,\"display\":\"平凉市\",\"region\":[{\"display\":\"泾川县\",\"rcode\":14310411981000,\"rid\":746},{\"display\":\"灵台县\",\"rcode\":14310411981001,\"rid\":747},{\"display\":\"崇信县\",\"rcode\":14310411981002,\"rid\":748},{\"display\":\"华亭县\",\"rcode\":14310411981003,\"rid\":749},{\"display\":\"庄浪县\",\"rcode\":14310411981004,\"rid\":750},{\"display\":\"静宁县\",\"rcode\":14310411981005,\"rid\":751},{\"display\":\"崆峒区\",\"rcode\":14310411981006,\"rid\":752}]},{\"ccode\":1431041206,\"cid\":6244,\"display\":\"庆阳市\",\"region\":[{\"display\":\"庆城县\",\"rcode\":14310412061000,\"rid\":753},{\"display\":\"环　县\",\"rcode\":14310412061001,\"rid\":754},{\"display\":\"华池县\",\"rcode\":14310412061002,\"rid\":755},{\"display\":\"合水县\",\"rcode\":14310412061003,\"rid\":756},{\"display\":\"正宁县\",\"rcode\":14310412061004,\"rid\":757},{\"display\":\"宁　县\",\"rcode\":14310412061005,\"rid\":758},{\"display\":\"镇原县\",\"rcode\":14310412061006,\"rid\":759},{\"display\":\"西峰区\",\"rcode\":14310412061007,\"rid\":760}]},{\"ccode\":1431041257,\"cid\":6245,\"display\":\"天水市\",\"region\":[{\"display\":\"平川区\",\"rcode\":14310412571000,\"rid\":761},{\"display\":\"靖远县\",\"rcode\":14310412571001,\"rid\":762},{\"display\":\"会宁县\",\"rcode\":14310412571002,\"rid\":763},{\"display\":\"景泰县\",\"rcode\":14310412571003,\"rid\":764},{\"display\":\"白银区\",\"rcode\":14310412571004,\"rid\":765}]},{\"ccode\":1431041275,\"cid\":6246,\"display\":\"武威市\",\"region\":[{\"display\":\"民勤县\",\"rcode\":14310412751000,\"rid\":766},{\"display\":\"古浪县\",\"rcode\":14310412751001,\"rid\":767},{\"display\":\"天祝藏族自治县\",\"rcode\":14310412751002,\"rid\":768},{\"display\":\"凉州区\",\"rcode\":14310412751003,\"rid\":769}]},{\"ccode\":1431041328,\"cid\":6247,\"display\":\"张掖市\",\"region\":[{\"display\":\"肃南裕固族自治县\",\"rcode\":14310413281000,\"rid\":770},{\"display\":\"民乐县\",\"rcode\":14310413281001,\"rid\":771},{\"display\":\"临泽县\",\"rcode\":14310413281002,\"rid\":772},{\"display\":\"高台县\",\"rcode\":14310413281003,\"rid\":773},{\"display\":\"山丹县\",\"rcode\":14310413281004,\"rid\":774},{\"display\":\"甘州区\",\"rcode\":14310413281005,\"rid\":775}]}],\"display\":\"甘肃\",\"pcode\":143104,\"pid\":686},{\"city\":[{\"ccode\":1431051040,\"cid\":6248,\"display\":\"潮州市\",\"region\":[{\"display\":\"潮安县\",\"rcode\":14310510401000,\"rid\":896},{\"display\":\"饶平县\",\"rcode\":14310510401001,\"rid\":897},{\"display\":\"湘桥区\",\"rcode\":14310510401002,\"rid\":898}]},{\"ccode\":1431051062,\"cid\":6249,\"display\":\"东莞市\",\"region\":[]},{\"ccode\":1431051068,\"cid\":6250,\"display\":\"佛山市\",\"region\":[{\"display\":\"南海区\",\"rcode\":14310510681000,\"rid\":778},{\"display\":\"顺德区\",\"rcode\":14310510681001,\"rid\":779},{\"display\":\"三水区\",\"rcode\":14310510681002,\"rid\":780},{\"display\":\"高明区\",\"rcode\":14310510681003,\"rid\":781},{\"display\":\"禅城区\",\"rcode\":14310510681004,\"rid\":782}]},{\"ccode\":1431051079,\"cid\":6251,\"display\":\"广州市\",\"region\":[{\"display\":\"越秀区\",\"rcode\":14310510791000,\"rid\":783},{\"display\":\"海珠区\",\"rcode\":14310510791001,\"rid\":784},{\"display\":\"天河区\",\"rcode\":14310510791002,\"rid\":785},{\"display\":\"白云区\",\"rcode\":14310510791003,\"rid\":786},{\"display\":\"黄埔区\",\"rcode\":14310510791004,\"rid\":787},{\"display\":\"番禺区\",\"rcode\":14310510791005,\"rid\":788},{\"display\":\"花都区\",\"rcode\":14310510791006,\"rid\":789},{\"display\":\"南沙区\",\"rcode\":14310510791007,\"rid\":790},{\"display\":\"萝岗区\",\"rcode\":14310510791008,\"rid\":791},{\"display\":\"增城市\",\"rcode\":14310510791009,\"rid\":792},{\"display\":\"从化市\",\"rcode\":14310510791010,\"rid\":793},{\"display\":\"荔湾区\",\"rcode\":14310510791011,\"rid\":794}]},{\"ccode\":1431051104,\"cid\":6252,\"display\":\"河源市\",\"region\":[{\"display\":\"紫金县\",\"rcode\":14310511041000,\"rid\":795},{\"display\":\"龙川县\",\"rcode\":14310511041001,\"rid\":796},{\"display\":\"连平县\",\"rcode\":14310511041002,\"rid\":797},{\"display\":\"和平县\",\"rcode\":14310511041003,\"rid\":798},{\"display\":\"东源县\",\"rcode\":14310511041004,\"rid\":799},{\"display\":\"源城区\",\"rcode\":14310511041005,\"rid\":800}]},{\"ccode\":1431051117,\"cid\":6253,\"display\":\"惠州市\",\"region\":[{\"display\":\"惠阳区\",\"rcode\":14310511171000,\"rid\":801},{\"display\":\"博罗县\",\"rcode\":14310511171001,\"rid\":802},{\"display\":\"惠东县\",\"rcode\":14310511171002,\"rid\":803},{\"display\":\"龙门县\",\"rcode\":14310511171003,\"rid\":804},{\"display\":\"惠城区\",\"rcode\":14310511171004,\"rid\":805}]},{\"ccode\":1431051124,\"cid\":6254,\"display\":\"江门市\",\"region\":[{\"display\":\"江海区\",\"rcode\":14310511241000,\"rid\":806},{\"display\":\"新会区\",\"rcode\":14310511241001,\"rid\":807},{\"display\":\"台山市\",\"rcode\":14310511241002,\"rid\":808},{\"display\":\"开平市\",\"rcode\":14310511241003,\"rid\":809},{\"display\":\"鹤山市\",\"rcode\":14310511241004,\"rid\":810},{\"display\":\"恩平市\",\"rcode\":14310511241005,\"rid\":811},{\"display\":\"蓬江区\",\"rcode\":14310511241006,\"rid\":812}]},{\"ccode\":1431051128,\"cid\":6255,\"display\":\"揭阳市\",\"region\":[{\"display\":\"揭东县\",\"rcode\":14310511281000,\"rid\":813},{\"display\":\"揭西县\",\"rcode\":14310511281001,\"rid\":814},{\"display\":\"惠来县\",\"rcode\":14310511281002,\"rid\":815},{\"display\":\"普宁市\",\"rcode\":14310511281003,\"rid\":816},{\"display\":\"榕城区\",\"rcode\":14310511281004,\"rid\":817}]},{\"ccode\":1431051177,\"cid\":6256,\"display\":\"茂名市\",\"region\":[{\"display\":\"茂港区\",\"rcode\":14310511771000,\"rid\":818},{\"display\":\"电白县\",\"rcode\":14310511771001,\"rid\":819},{\"display\":\"高州市\",\"rcode\":14310511771002,\"rid\":820},{\"display\":\"化州市\",\"rcode\":14310511771003,\"rid\":821},{\"display\":\"信宜市\",\"rcode\":14310511771004,\"rid\":822},{\"display\":\"茂南区\",\"rcode\":14310511771005,\"rid\":823}]},{\"ccode\":1431051179,\"cid\":6257,\"display\":\"梅州市\",\"region\":[{\"display\":\"梅　县\",\"rcode\":14310511791000,\"rid\":824},{\"display\":\"大埔县\",\"rcode\":14310511791001,\"rid\":825},{\"display\":\"丰顺县\",\"rcode\":14310511791002,\"rid\":826},{\"display\":\"五华县\",\"rcode\":14310511791003,\"rid\":827},{\"display\":\"平远县\",\"rcode\":14310511791004,\"rid\":828},{\"display\":\"蕉岭县\",\"rcode\":14310511791005,\"rid\":829},{\"display\":\"兴宁市\",\"rcode\":14310511791006,\"rid\":830},{\"display\":\"梅江区\",\"rcode\":14310511791007,\"rid\":831}]},{\"ccode\":1431051207,\"cid\":6258,\"display\":\"清远市\",\"region\":[{\"display\":\"佛冈县\",\"rcode\":14310512071000,\"rid\":832},{\"display\":\"阳山县\",\"rcode\":14310512071001,\"rid\":833},{\"display\":\"连山壮族瑶族自治县\",\"rcode\":14310512071002,\"rid\":834},{\"display\":\"连南瑶族自治县\",\"rcode\":14310512071003,\"rid\":835},{\"display\":\"清新县\",\"rcode\":14310512071004,\"rid\":836},{\"display\":\"英德市\",\"rcode\":14310512071005,\"rid\":837},{\"display\":\"连州市\",\"rcode\":14310512071006,\"rid\":838},{\"display\":\"清城区\",\"rcode\":14310512071007,\"rid\":839}]},{\"ccode\":1431051226,\"cid\":6259,\"display\":\"汕头市\",\"region\":[{\"display\":\"金平区\",\"rcode\":14310512261000,\"rid\":840},{\"display\":\"濠江区\",\"rcode\":14310512261001,\"rid\":841},{\"display\":\"潮阳区\",\"rcode\":14310512261002,\"rid\":842},{\"display\":\"潮南区\",\"rcode\":14310512261003,\"rid\":843},{\"display\":\"澄海区\",\"rcode\":14310512261004,\"rid\":844},{\"display\":\"南澳县\",\"rcode\":14310512261005,\"rid\":845},{\"display\":\"龙湖区\",\"rcode\":14310512261006,\"rid\":846}]},{\"ccode\":1431051227,\"cid\":6260,\"display\":\"汕尾市\",\"region\":[{\"display\":\"海丰县\",\"rcode\":14310512271000,\"rid\":847},{\"display\":\"陆河县\",\"rcode\":14310512271001,\"rid\":848},{\"display\":\"陆丰市\",\"rcode\":14310512271002,\"rid\":849},{\"display\":\"城　区\",\"rcode\":14310512271003,\"rid\":850}]},{\"ccode\":1431051228,\"cid\":6261,\"display\":\"韶关市\",\"region\":[{\"display\":\"浈江区\",\"rcode\":14310512281000,\"rid\":851},{\"display\":\"曲江区\",\"rcode\":14310512281001,\"rid\":852},{\"display\":\"始兴县\",\"rcode\":14310512281002,\"rid\":853},{\"display\":\"仁化县\",\"rcode\":14310512281003,\"rid\":854},{\"display\":\"翁源县\",\"rcode\":14310512281004,\"rid\":855},{\"display\":\"乳源瑶族自治县\",\"rcode\":14310512281005,\"rid\":856},{\"display\":\"新丰县\",\"rcode\":14310512281006,\"rid\":857},{\"display\":\"乐昌市\",\"rcode\":14310512281007,\"rid\":858},{\"display\":\"南雄市\",\"rcode\":14310512281008,\"rid\":859},{\"display\":\"武江区\",\"rcode\":14310512281009,\"rid\":860}]},{\"ccode\":1431051234,\"cid\":6262,\"display\":\"深圳市\",\"region\":[{\"display\":\"福田区\",\"rcode\":14310512341000,\"rid\":861},{\"display\":\"南山区\",\"rcode\":14310512341001,\"rid\":862},{\"display\":\"宝安区\",\"rcode\":14310512341002,\"rid\":863},{\"display\":\"龙岗区\",\"rcode\":14310512341003,\"rid\":864},{\"display\":\"盐田区\",\"rcode\":14310512341004,\"rid\":865},{\"display\":\"罗湖区\",\"rcode\":14310512341005,\"rid\":866}]},{\"ccode\":1431051303,\"cid\":6263,\"display\":\"阳江市\",\"region\":[{\"display\":\"阳西县\",\"rcode\":14310513031000,\"rid\":867},{\"display\":\"阳东县\",\"rcode\":14310513031001,\"rid\":868},{\"display\":\"阳春市\",\"rcode\":14310513031002,\"rid\":869},{\"display\":\"江城区\",\"rcode\":14310513031003,\"rid\":870}]},{\"ccode\":1431051322,\"cid\":6264,\"display\":\"云浮市\",\"region\":[{\"display\":\"新兴县\",\"rcode\":14310513221000,\"rid\":871},{\"display\":\"郁南县\",\"rcode\":14310513221001,\"rid\":872},{\"display\":\"云安县\",\"rcode\":14310513221002,\"rid\":873},{\"display\":\"罗定市\",\"rcode\":14310513221003,\"rid\":874},{\"display\":\"云城区\",\"rcode\":14310513221004,\"rid\":875}]},{\"ccode\":1431051330,\"cid\":6265,\"display\":\"湛江市\",\"region\":[{\"display\":\"霞山区\",\"rcode\":14310513301000,\"rid\":876},{\"display\":\"坡头区\",\"rcode\":14310513301001,\"rid\":877},{\"display\":\"麻章区\",\"rcode\":14310513301002,\"rid\":878},{\"display\":\"遂溪县\",\"rcode\":14310513301003,\"rid\":879},{\"display\":\"徐闻县\",\"rcode\":14310513301004,\"rid\":880},{\"display\":\"廉江市\",\"rcode\":14310513301005,\"rid\":881},{\"display\":\"雷州市\",\"rcode\":14310513301006,\"rid\":882},{\"display\":\"吴川市\",\"rcode\":14310513301007,\"rid\":883},{\"display\":\"赤坎区\",\"rcode\":14310513301008,\"rid\":884}]},{\"ccode\":1431051331,\"cid\":6266,\"display\":\"肇庆市\",\"region\":[{\"display\":\"鼎湖区\",\"rcode\":14310513311000,\"rid\":885},{\"display\":\"广宁县\",\"rcode\":14310513311001,\"rid\":886},{\"display\":\"怀集县\",\"rcode\":14310513311002,\"rid\":887},{\"display\":\"封开县\",\"rcode\":14310513311003,\"rid\":888},{\"display\":\"德庆县\",\"rcode\":14310513311004,\"rid\":889},{\"display\":\"高要市\",\"rcode\":14310513311005,\"rid\":890},{\"display\":\"四会市\",\"rcode\":14310513311006,\"rid\":891},{\"display\":\"端州区\",\"rcode\":14310513311007,\"rid\":892}]},{\"ccode\":1431051335,\"cid\":6267,\"display\":\"中山市\",\"region\":[]},{\"ccode\":1431051338,\"cid\":6268,\"display\":\"珠海市\",\"region\":[{\"display\":\"斗门区\",\"rcode\":14310513381000,\"rid\":893},{\"display\":\"金湾区\",\"rcode\":14310513381001,\"rid\":894},{\"display\":\"香洲区\",\"rcode\":14310513381002,\"rid\":895}]},{\"ccode\":1431051339,\"cid\":26685,\"display\":\"东沙群岛\",\"region\":[]}],\"display\":\"广东\",\"pcode\":143105,\"pid\":687},{\"city\":[{\"ccode\":1431061011,\"cid\":6269,\"display\":\"百色市\",\"region\":[{\"display\":\"田阳县\",\"rcode\":14310610111000,\"rid\":996},{\"display\":\"田东县\",\"rcode\":14310610111001,\"rid\":997},{\"display\":\"平果县\",\"rcode\":14310610111002,\"rid\":998},{\"display\":\"德保县\",\"rcode\":14310610111003,\"rid\":999},{\"display\":\"靖西县\",\"rcode\":14310610111004,\"rid\":1000},{\"display\":\"那坡县\",\"rcode\":14310610111005,\"rid\":1001},{\"display\":\"凌云县\",\"rcode\":14310610111006,\"rid\":1002},{\"display\":\"乐业县\",\"rcode\":14310610111007,\"rid\":1003},{\"display\":\"田林县\",\"rcode\":14310610111008,\"rid\":1004},{\"display\":\"西林县\",\"rcode\":14310610111009,\"rid\":1005},{\"display\":\"隆林各族自治县\",\"rcode\":14310610111010,\"rid\":1006},{\"display\":\"右江区\",\"rcode\":14310610111011,\"rid\":1007}]},{\"ccode\":1431061022,\"cid\":6270,\"display\":\"北海市\",\"region\":[{\"display\":\"银海区\",\"rcode\":14310610221000,\"rid\":899},{\"display\":\"铁山港区\",\"rcode\":14310610221001,\"rid\":900},{\"display\":\"合浦县\",\"rcode\":14310610221002,\"rid\":901},{\"display\":\"海城区\",\"rcode\":14310610221003,\"rid\":902}]},{\"ccode\":1431061067,\"cid\":6271,\"display\":\"防城港市\",\"region\":[{\"display\":\"防城区\",\"rcode\":14310610671000,\"rid\":910},{\"display\":\"上思县\",\"rcode\":14310610671001,\"rid\":911},{\"display\":\"东兴市\",\"rcode\":14310610671002,\"rid\":912},{\"display\":\"港口区\",\"rcode\":14310610671003,\"rid\":913}]},{\"ccode\":1431061080,\"cid\":6272,\"display\":\"贵港市\",\"region\":[{\"display\":\"港南区\",\"rcode\":14310610801000,\"rid\":914},{\"display\":\"覃塘区\",\"rcode\":14310610801001,\"rid\":915},{\"display\":\"平南县\",\"rcode\":14310610801002,\"rid\":916},{\"display\":\"桂平市\",\"rcode\":14310610801003,\"rid\":917},{\"display\":\"港北区\",\"rcode\":14310610801004,\"rid\":918}]},{\"ccode\":1431061081,\"cid\":6273,\"display\":\"桂林市\",\"region\":[{\"display\":\"叠彩区\",\"rcode\":14310610811000,\"rid\":919},{\"display\":\"象山区\",\"rcode\":14310610811001,\"rid\":920},{\"display\":\"七星区\",\"rcode\":14310610811002,\"rid\":921},{\"display\":\"雁山区\",\"rcode\":14310610811003,\"rid\":922},{\"display\":\"阳朔县\",\"rcode\":14310610811004,\"rid\":923},{\"display\":\"临桂县\",\"rcode\":14310610811005,\"rid\":924},{\"display\":\"灵川县\",\"rcode\":14310610811006,\"rid\":925},{\"display\":\"全州县\",\"rcode\":14310610811007,\"rid\":926},{\"display\":\"兴安县\",\"rcode\":14310610811008,\"rid\":927},{\"display\":\"永福县\",\"rcode\":14310610811009,\"rid\":928},{\"display\":\"灌阳县\",\"rcode\":14310610811010,\"rid\":929},{\"display\":\"龙胜各族自治县\",\"rcode\":14310610811011,\"rid\":930},{\"display\":\"资源县\",\"rcode\":14310610811012,\"rid\":931},{\"display\":\"平乐县\",\"rcode\":14310610811013,\"rid\":932},{\"display\":\"荔浦县\",\"rcode\":14310610811014,\"rid\":933},{\"display\":\"恭城瑶族自治县\",\"rcode\":14310610811015,\"rid\":934},{\"display\":\"秀峰区\",\"rcode\":14310610811016,\"rid\":935}]},{\"ccode\":1431061097,\"cid\":6274,\"display\":\"河池市\",\"region\":[{\"display\":\"南丹县\",\"rcode\":14310610971000,\"rid\":936},{\"display\":\"天峨县\",\"rcode\":14310610971001,\"rid\":937},{\"display\":\"凤山县\",\"rcode\":14310610971002,\"rid\":938},{\"display\":\"东兰县\",\"rcode\":14310610971003,\"rid\":939},{\"display\":\"罗城仫佬族自治县\",\"rcode\":14310610971004,\"rid\":940},{\"display\":\"环江毛南族自治县\",\"rcode\":14310610971005,\"rid\":941},{\"display\":\"巴马瑶族自治县\",\"rcode\":14310610971006,\"rid\":942},{\"display\":\"都安瑶族自治县\",\"rcode\":14310610971007,\"rid\":943},{\"display\":\"大化瑶族自治县\",\"rcode\":14310610971008,\"rid\":944},{\"display\":\"宜州市\",\"rcode\":14310610971009,\"rid\":945},{\"display\":\"金城江区\",\"rcode\":14310610971010,\"rid\":946}]},{\"ccode\":1431061106,\"cid\":6275,\"display\":\"贺州市\",\"region\":[{\"display\":\"昭平县\",\"rcode\":14310611061000,\"rid\":947},{\"display\":\"钟山县\",\"rcode\":14310611061001,\"rid\":948},{\"display\":\"富川瑶族自治县\",\"rcode\":14310611061002,\"rid\":949},{\"display\":\"八步区\",\"rcode\":14310611061003,\"rid\":950}]},{\"ccode\":1431061167,\"cid\":6276,\"display\":\"柳州市\",\"region\":[{\"display\":\"鱼峰区\",\"rcode\":14310611671000,\"rid\":957},{\"display\":\"柳南区\",\"rcode\":14310611671001,\"rid\":958},{\"display\":\"柳北区\",\"rcode\":14310611671002,\"rid\":959},{\"display\":\"柳江县\",\"rcode\":14310611671003,\"rid\":960},{\"display\":\"柳城县\",\"rcode\":14310611671004,\"rid\":961},{\"display\":\"鹿寨县\",\"rcode\":14310611671005,\"rid\":962},{\"display\":\"融安县\",\"rcode\":14310611671006,\"rid\":963},{\"display\":\"融水苗族自治县\",\"rcode\":14310611671007,\"rid\":964},{\"display\":\"三江侗族自治县\",\"rcode\":14310611671008,\"rid\":965},{\"display\":\"城中区\",\"rcode\":14310611671009,\"rid\":966}]},{\"ccode\":1431061168,\"cid\":26681,\"display\":\"来宾市\",\"region\":[{\"display\":\"忻城县\",\"rcode\":14310611681000,\"rid\":951},{\"display\":\"象州县\",\"rcode\":14310611681001,\"rid\":952},{\"display\":\"武宣县\",\"rcode\":14310611681002,\"rid\":953},{\"display\":\"金秀瑶族自治县\",\"rcode\":14310611681003,\"rid\":954},{\"display\":\"合山市\",\"rcode\":14310611681004,\"rid\":955},{\"display\":\"兴宾区\",\"rcode\":14310611681005,\"rid\":956}]},{\"ccode\":1431061185,\"cid\":6278,\"display\":\"南宁市\",\"region\":[{\"display\":\"青秀区\",\"rcode\":14310611851000,\"rid\":967},{\"display\":\"江南区\",\"rcode\":14310611851001,\"rid\":968},{\"display\":\"西乡塘区\",\"rcode\":14310611851002,\"rid\":969},{\"display\":\"良庆区\",\"rcode\":14310611851003,\"rid\":970},{\"display\":\"邕宁区\",\"rcode\":14310611851004,\"rid\":971},{\"display\":\"武鸣县\",\"rcode\":14310611851005,\"rid\":972},{\"display\":\"隆安县\",\"rcode\":14310611851006,\"rid\":973},{\"display\":\"马山县\",\"rcode\":14310611851007,\"rid\":974},{\"display\":\"上林县\",\"rcode\":14310611851008,\"rid\":975},{\"display\":\"宾阳县\",\"rcode\":14310611851009,\"rid\":976},{\"display\":\"横　县\",\"rcode\":14310611851010,\"rid\":977},{\"display\":\"兴宁区\",\"rcode\":14310611851011,\"rid\":978}]},{\"ccode\":1431061186,\"cid\":26682,\"display\":\"崇左市\",\"region\":[{\"display\":\"扶绥县\",\"rcode\":14310611861000,\"rid\":903},{\"display\":\"宁明县\",\"rcode\":14310611861001,\"rid\":904},{\"display\":\"龙州县\",\"rcode\":14310611861002,\"rid\":905},{\"display\":\"大新县\",\"rcode\":14310611861003,\"rid\":906},{\"display\":\"天等县\",\"rcode\":14310611861004,\"rid\":907},{\"display\":\"凭祥市\",\"rcode\":14310611861005,\"rid\":908},{\"display\":\"江洲区\",\"rcode\":14310611861006,\"rid\":909}]},{\"ccode\":1431061209,\"cid\":6280,\"display\":\"钦州市\",\"region\":[{\"display\":\"钦北区\",\"rcode\":14310612091000,\"rid\":979},{\"display\":\"灵山县\",\"rcode\":14310612091001,\"rid\":980},{\"display\":\"浦北县\",\"rcode\":14310612091002,\"rid\":981},{\"display\":\"钦南区\",\"rcode\":14310612091003,\"rid\":982}]},{\"ccode\":1431061278,\"cid\":6281,\"display\":\"梧州市\",\"region\":[{\"display\":\"蝶山区\",\"rcode\":14310612781000,\"rid\":983},{\"display\":\"长洲区\",\"rcode\":14310612781001,\"rid\":984},{\"display\":\"苍梧县\",\"rcode\":14310612781002,\"rid\":985},{\"display\":\"藤　县\",\"rcode\":14310612781003,\"rid\":986},{\"display\":\"蒙山县\",\"rcode\":14310612781004,\"rid\":987},{\"display\":\"岑溪市\",\"rcode\":14310612781005,\"rid\":988},{\"display\":\"万秀区\",\"rcode\":14310612781006,\"rid\":989}]},{\"ccode\":1431061319,\"cid\":6282,\"display\":\"玉林市\",\"region\":[{\"display\":\"容　县\",\"rcode\":14310613191000,\"rid\":990},{\"display\":\"陆川县\",\"rcode\":14310613191001,\"rid\":991},{\"display\":\"博白县\",\"rcode\":14310613191002,\"rid\":992},{\"display\":\"兴业县\",\"rcode\":14310613191003,\"rid\":993},{\"display\":\"北流市\",\"rcode\":14310613191004,\"rid\":994},{\"display\":\"玉州区\",\"rcode\":14310613191005,\"rid\":995}]}],\"display\":\"广西\",\"pcode\":143106,\"pid\":688},{\"city\":[{\"ccode\":1431071008,\"cid\":6283,\"display\":\"安顺市\",\"region\":[{\"display\":\"平坝县\",\"rcode\":14310710081000,\"rid\":1090},{\"display\":\"普定县\",\"rcode\":14310710081001,\"rid\":1091},{\"display\":\"镇宁布依族苗族自治县\",\"rcode\":14310710081002,\"rid\":1092},{\"display\":\"关岭布依族苗族自治县\",\"rcode\":14310710081003,\"rid\":1093},{\"display\":\"紫云苗族布依族自治县\",\"rcode\":14310710081004,\"rid\":1094},{\"display\":\"西秀区\",\"rcode\":14310710081005,\"rid\":1095}]},{\"ccode\":1431071026,\"cid\":6284,\"display\":\"毕节地区\",\"region\":[{\"display\":\"大方县\",\"rcode\":14310710261000,\"rid\":1008},{\"display\":\"黔西县\",\"rcode\":14310710261001,\"rid\":1009},{\"display\":\"金沙县\",\"rcode\":14310710261002,\"rid\":1010},{\"display\":\"织金县\",\"rcode\":14310710261003,\"rid\":1011},{\"display\":\"纳雍县\",\"rcode\":14310710261004,\"rid\":1012},{\"display\":\"威宁彝族回族苗族自治县\",\"rcode\":14310710261005,\"rid\":1013},{\"display\":\"赫章县\",\"rcode\":14310710261006,\"rid\":1014},{\"display\":\"毕节市\",\"rcode\":14310710261007,\"rid\":1015}]},{\"ccode\":1431071082,\"cid\":6285,\"display\":\"贵阳市\",\"region\":[{\"display\":\"云岩区\",\"rcode\":14310710821000,\"rid\":1016},{\"display\":\"花溪区\",\"rcode\":14310710821001,\"rid\":1017},{\"display\":\"乌当区\",\"rcode\":14310710821002,\"rid\":1018},{\"display\":\"白云区\",\"rcode\":14310710821003,\"rid\":1019},{\"display\":\"小河区\",\"rcode\":14310710821004,\"rid\":1020},{\"display\":\"开阳县\",\"rcode\":14310710821005,\"rid\":1021},{\"display\":\"息烽县\",\"rcode\":14310710821006,\"rid\":1022},{\"display\":\"修文县\",\"rcode\":14310710821007,\"rid\":1023},{\"display\":\"清镇市\",\"rcode\":14310710821008,\"rid\":1024},{\"display\":\"南明区\",\"rcode\":14310710821009,\"rid\":1025}]},{\"ccode\":1431071166,\"cid\":6286,\"display\":\"六盘水市\",\"region\":[{\"display\":\"六枝特区\",\"rcode\":14310711661000,\"rid\":1026},{\"display\":\"水城县\",\"rcode\":14310711661001,\"rid\":1027},{\"display\":\"盘　县\",\"rcode\":14310711661002,\"rid\":1028},{\"display\":\"钟山区\",\"rcode\":14310711661003,\"rid\":1029}]},{\"ccode\":1431071202,\"cid\":6287,\"display\":\"黔东南苗族侗族自治州\",\"region\":[{\"display\":\"黄平县\",\"rcode\":14310712021000,\"rid\":1030},{\"display\":\"施秉县\",\"rcode\":14310712021001,\"rid\":1031},{\"display\":\"三穗县\",\"rcode\":14310712021002,\"rid\":1032},{\"display\":\"镇远县\",\"rcode\":14310712021003,\"rid\":1033},{\"display\":\"岑巩县\",\"rcode\":14310712021004,\"rid\":1034},{\"display\":\"天柱县\",\"rcode\":14310712021005,\"rid\":1035},{\"display\":\"锦屏县\",\"rcode\":14310712021006,\"rid\":1036},{\"display\":\"剑河县\",\"rcode\":14310712021007,\"rid\":1037},{\"display\":\"台江县\",\"rcode\":14310712021008,\"rid\":1038},{\"display\":\"黎平县\",\"rcode\":14310712021009,\"rid\":1039},{\"display\":\"榕江县\",\"rcode\":14310712021010,\"rid\":1040},{\"display\":\"从江县\",\"rcode\":14310712021011,\"rid\":1041},{\"display\":\"雷山县\",\"rcode\":14310712021012,\"rid\":1042},{\"display\":\"麻江县\",\"rcode\":14310712021013,\"rid\":1043},{\"display\":\"丹寨县\",\"rcode\":14310712021014,\"rid\":1044},{\"display\":\"凯里市\",\"rcode\":14310712021015,\"rid\":1045}]},{\"ccode\":1431071203,\"cid\":6288,\"display\":\"黔南布依族苗族自治州\",\"region\":[{\"display\":\"福泉市\",\"rcode\":14310712031000,\"rid\":1046},{\"display\":\"荔波县\",\"rcode\":14310712031001,\"rid\":1047},{\"display\":\"贵定县\",\"rcode\":14310712031002,\"rid\":1048},{\"display\":\"瓮安县\",\"rcode\":14310712031003,\"rid\":1049},{\"display\":\"独山县\",\"rcode\":14310712031004,\"rid\":1050},{\"display\":\"平塘县\",\"rcode\":14310712031005,\"rid\":1051},{\"display\":\"罗甸县\",\"rcode\":14310712031006,\"rid\":1052},{\"display\":\"长顺县\",\"rcode\":14310712031007,\"rid\":1053},{\"display\":\"龙里县\",\"rcode\":14310712031008,\"rid\":1054},{\"display\":\"惠水县\",\"rcode\":14310712031009,\"rid\":1055},{\"display\":\"三都水族自治县\",\"rcode\":14310712031010,\"rid\":1056},{\"display\":\"都匀市\",\"rcode\":14310712031011,\"rid\":1057}]},{\"ccode\":1431071204,\"cid\":6289,\"display\":\"黔西南布依族苗族自治州\",\"region\":[{\"display\":\"兴仁县\",\"rcode\":14310712041000,\"rid\":1058},{\"display\":\"普安县\",\"rcode\":14310712041001,\"rid\":1059},{\"display\":\"晴隆县\",\"rcode\":14310712041002,\"rid\":1060},{\"display\":\"贞丰县\",\"rcode\":14310712041003,\"rid\":1061},{\"display\":\"望谟县\",\"rcode\":14310712041004,\"rid\":1062},{\"display\":\"册亨县\",\"rcode\":14310712041005,\"rid\":1063},{\"display\":\"安龙县\",\"rcode\":14310712041006,\"rid\":1064},{\"display\":\"兴义市\",\"rcode\":14310712041007,\"rid\":1065}]},{\"ccode\":1431071263,\"cid\":6290,\"display\":\"铜仁地区\",\"region\":[{\"display\":\"江口县\",\"rcode\":14310712631000,\"rid\":1066},{\"display\":\"玉屏侗族自治县\",\"rcode\":14310712631001,\"rid\":1067},{\"display\":\"石阡县\",\"rcode\":14310712631002,\"rid\":1068},{\"display\":\"思南县\",\"rcode\":14310712631003,\"rid\":1069},{\"display\":\"印江土家族苗族自治县\",\"rcode\":14310712631004,\"rid\":1070},{\"display\":\"德江县\",\"rcode\":14310712631005,\"rid\":1071},{\"display\":\"沿河土家族自治县\",\"rcode\":14310712631006,\"rid\":1072},{\"display\":\"松桃苗族自治县\",\"rcode\":14310712631007,\"rid\":1073},{\"display\":\"万山特区\",\"rcode\":14310712631008,\"rid\":1074},{\"display\":\"铜仁市\",\"rcode\":14310712631009,\"rid\":1075}]},{\"ccode\":1431071344,\"cid\":6291,\"display\":\"遵义市\",\"region\":[{\"display\":\"汇川区\",\"rcode\":14310713441000,\"rid\":1076},{\"display\":\"遵义县\",\"rcode\":14310713441001,\"rid\":1077},{\"display\":\"桐梓县\",\"rcode\":14310713441002,\"rid\":1078},{\"display\":\"绥阳县\",\"rcode\":14310713441003,\"rid\":1079},{\"display\":\"正安县\",\"rcode\":14310713441004,\"rid\":1080},{\"display\":\"道真仡佬族苗族自治县\",\"rcode\":14310713441005,\"rid\":1081},{\"display\":\"务川仡佬族苗族自治县\",\"rcode\":14310713441006,\"rid\":1082},{\"display\":\"凤冈县\",\"rcode\":14310713441007,\"rid\":1083},{\"display\":\"湄潭县\",\"rcode\":14310713441008,\"rid\":1084},{\"display\":\"余庆县\",\"rcode\":14310713441009,\"rid\":1085},{\"display\":\"习水县\",\"rcode\":14310713441010,\"rid\":1086},{\"display\":\"赤水市\",\"rcode\":14310713441011,\"rid\":1087},{\"display\":\"仁怀市\",\"rcode\":14310713441012,\"rid\":1088},{\"display\":\"红花岗区\",\"rcode\":14310713441013,\"rid\":1089}]}],\"display\":\"贵州\",\"pcode\":143107,\"pid\":689},{\"city\":[{\"ccode\":1431081088,\"cid\":6292,\"display\":\"海口市\",\"region\":[{\"display\":\"龙华区\",\"rcode\":14310810881000,\"rid\":1096},{\"display\":\"琼山区\",\"rcode\":14310810881001,\"rid\":1097},{\"display\":\"美兰区\",\"rcode\":14310810881002,\"rid\":1098},{\"display\":\"秀英区\",\"rcode\":14310810881003,\"rid\":1099}]},{\"ccode\":1431081089,\"cid\":6293,\"display\":\"省直辖县级行政单位\",\"region\":[]},{\"ccode\":1431081219,\"cid\":6294,\"display\":\"三亚市\",\"region\":[]}],\"display\":\"海南\",\"pcode\":143108,\"pid\":690},{\"city\":[{\"ccode\":1431091015,\"cid\":6295,\"display\":\"保定市\",\"region\":[{\"display\":\"北市区\",\"rcode\":14310910151000,\"rid\":1247},{\"display\":\"南市区\",\"rcode\":14310910151001,\"rid\":1248},{\"display\":\"满城县\",\"rcode\":14310910151002,\"rid\":1249},{\"display\":\"清苑县\",\"rcode\":14310910151003,\"rid\":1250},{\"display\":\"涞水县\",\"rcode\":14310910151004,\"rid\":1251},{\"display\":\"阜平县\",\"rcode\":14310910151005,\"rid\":1252},{\"display\":\"徐水县\",\"rcode\":14310910151006,\"rid\":1253},{\"display\":\"定兴县\",\"rcode\":14310910151007,\"rid\":1254},{\"display\":\"唐　县\",\"rcode\":14310910151008,\"rid\":1255},{\"display\":\"高阳县\",\"rcode\":14310910151009,\"rid\":1256},{\"display\":\"容城县\",\"rcode\":14310910151010,\"rid\":1257},{\"display\":\"涞源县\",\"rcode\":14310910151011,\"rid\":1258},{\"display\":\"望都县\",\"rcode\":14310910151012,\"rid\":1259},{\"display\":\"安新县\",\"rcode\":14310910151013,\"rid\":1260},{\"display\":\"易　县\",\"rcode\":14310910151014,\"rid\":1261},{\"display\":\"曲阳县\",\"rcode\":14310910151015,\"rid\":1262},{\"display\":\"蠡　县\",\"rcode\":14310910151016,\"rid\":1263},{\"display\":\"顺平县\",\"rcode\":14310910151017,\"rid\":1264},{\"display\":\"博野县\",\"rcode\":14310910151018,\"rid\":1265},{\"display\":\"雄　县\",\"rcode\":14310910151019,\"rid\":1266},{\"display\":\"涿州市\",\"rcode\":14310910151020,\"rid\":1267},{\"display\":\"定州市\",\"rcode\":14310910151021,\"rid\":1268},{\"display\":\"安国市\",\"rcode\":14310910151022,\"rid\":1269},{\"display\":\"高碑店市\",\"rcode\":14310910151023,\"rid\":1270},{\"display\":\"新市区\",\"rcode\":14310910151024,\"rid\":1271}]},{\"ccode\":1431091030,\"cid\":6296,\"display\":\"沧州市\",\"region\":[{\"display\":\"运河区\",\"rcode\":14310910301000,\"rid\":1100},{\"display\":\"沧　县\",\"rcode\":14310910301001,\"rid\":1101},{\"display\":\"青　县\",\"rcode\":14310910301002,\"rid\":1102},{\"display\":\"东光县\",\"rcode\":14310910301003,\"rid\":1103},{\"display\":\"海兴县\",\"rcode\":14310910301004,\"rid\":1104},{\"display\":\"盐山县\",\"rcode\":14310910301005,\"rid\":1105},{\"display\":\"肃宁县\",\"rcode\":14310910301006,\"rid\":1106},{\"display\":\"南皮县\",\"rcode\":14310910301007,\"rid\":1107},{\"display\":\"吴桥县\",\"rcode\":14310910301008,\"rid\":1108},{\"display\":\"献　县\",\"rcode\":14310910301009,\"rid\":1109},{\"display\":\"孟村回族自治县\",\"rcode\":14310910301010,\"rid\":1110},{\"display\":\"泊头市\",\"rcode\":14310910301011,\"rid\":1111},{\"display\":\"任丘市\",\"rcode\":14310910301012,\"rid\":1112},{\"display\":\"黄骅市\",\"rcode\":14310910301013,\"rid\":1113},{\"display\":\"河间市\",\"rcode\":14310910301014,\"rid\":1114},{\"display\":\"新华区\",\"rcode\":14310910301015,\"rid\":1115}]},{\"ccode\":1431091041,\"cid\":6297,\"display\":\"承德市\",\"region\":[{\"display\":\"双滦区\",\"rcode\":14310910411000,\"rid\":1116},{\"display\":\"鹰手营子矿区\",\"rcode\":14310910411001,\"rid\":1117},{\"display\":\"承德县\",\"rcode\":14310910411002,\"rid\":1118},{\"display\":\"兴隆县\",\"rcode\":14310910411003,\"rid\":1119},{\"display\":\"平泉县\",\"rcode\":14310910411004,\"rid\":1120},{\"display\":\"滦平县\",\"rcode\":14310910411005,\"rid\":1121},{\"display\":\"隆化县\",\"rcode\":14310910411006,\"rid\":1122},{\"display\":\"丰宁满族自治县\",\"rcode\":14310910411007,\"rid\":1123},{\"display\":\"宽城满族自治县\",\"rcode\":14310910411008,\"rid\":1124},{\"display\":\"围场满族蒙古族自治县\",\"rcode\":14310910411009,\"rid\":1125},{\"display\":\"双桥区\",\"rcode\":14310910411010,\"rid\":1126}]},{\"ccode\":1431091093,\"cid\":6298,\"display\":\"邯郸市\",\"region\":[{\"display\":\"丛台区\",\"rcode\":14310910931000,\"rid\":1127},{\"display\":\"复兴区\",\"rcode\":14310910931001,\"rid\":1128},{\"display\":\"峰峰矿区\",\"rcode\":14310910931002,\"rid\":1129},{\"display\":\"邯郸县\",\"rcode\":14310910931003,\"rid\":1130},{\"display\":\"临漳县\",\"rcode\":14310910931004,\"rid\":1131},{\"display\":\"成安县\",\"rcode\":14310910931005,\"rid\":1132},{\"display\":\"大名县\",\"rcode\":14310910931006,\"rid\":1133},{\"display\":\"涉　县\",\"rcode\":14310910931007,\"rid\":1134},{\"display\":\"磁　县\",\"rcode\":14310910931008,\"rid\":1135},{\"display\":\"肥乡县\",\"rcode\":14310910931009,\"rid\":1136},{\"display\":\"永年县\",\"rcode\":14310910931010,\"rid\":1137},{\"display\":\"邱　县\",\"rcode\":14310910931011,\"rid\":1138},{\"display\":\"鸡泽县\",\"rcode\":14310910931012,\"rid\":1139},{\"display\":\"广平县\",\"rcode\":14310910931013,\"rid\":1140},{\"display\":\"馆陶县\",\"rcode\":14310910931014,\"rid\":1141},{\"display\":\"魏　县\",\"rcode\":14310910931015,\"rid\":1142},{\"display\":\"曲周县\",\"rcode\":14310910931016,\"rid\":1143},{\"display\":\"武安市\",\"rcode\":14310910931017,\"rid\":1144},{\"display\":\"邯山区\",\"rcode\":14310910931018,\"rid\":1145}]},{\"ccode\":1431091101,\"cid\":6299,\"display\":\"衡水市\",\"region\":[{\"display\":\"枣强县\",\"rcode\":14310911011000,\"rid\":1146},{\"display\":\"武邑县\",\"rcode\":14310911011001,\"rid\":1147},{\"display\":\"武强县\",\"rcode\":14310911011002,\"rid\":1148},{\"display\":\"饶阳县\",\"rcode\":14310911011003,\"rid\":1149},{\"display\":\"安平县\",\"rcode\":14310911011004,\"rid\":1150},{\"display\":\"故城县\",\"rcode\":14310911011005,\"rid\":1151},{\"display\":\"景　县\",\"rcode\":14310911011006,\"rid\":1152},{\"display\":\"阜城县\",\"rcode\":14310911011007,\"rid\":1153},{\"display\":\"冀州市\",\"rcode\":14310911011008,\"rid\":1154},{\"display\":\"深州市\",\"rcode\":14310911011009,\"rid\":1155},{\"display\":\"桃城区\",\"rcode\":14310911011010,\"rid\":1156}]},{\"ccode\":1431091149,\"cid\":6300,\"display\":\"廊坊市\",\"region\":[{\"display\":\"广阳区\",\"rcode\":14310911491000,\"rid\":1157},{\"display\":\"固安县\",\"rcode\":14310911491001,\"rid\":1158},{\"display\":\"永清县\",\"rcode\":14310911491002,\"rid\":1159},{\"display\":\"香河县\",\"rcode\":14310911491003,\"rid\":1160},{\"display\":\"大城县\",\"rcode\":14310911491004,\"rid\":1161},{\"display\":\"文安县\",\"rcode\":14310911491005,\"rid\":1162},{\"display\":\"大厂回族自治县\",\"rcode\":14310911491006,\"rid\":1163},{\"display\":\"霸州市\",\"rcode\":14310911491007,\"rid\":1164},{\"display\":\"三河市\",\"rcode\":14310911491008,\"rid\":1165},{\"display\":\"安次区\",\"rcode\":14310911491009,\"rid\":1166}]},{\"ccode\":1431091208,\"cid\":6301,\"display\":\"秦皇岛市\",\"region\":[{\"display\":\"山海关区\",\"rcode\":14310912081000,\"rid\":1167},{\"display\":\"北戴河区\",\"rcode\":14310912081001,\"rid\":1168},{\"display\":\"青龙满族自治县\",\"rcode\":14310912081002,\"rid\":1169},{\"display\":\"昌黎县\",\"rcode\":14310912081003,\"rid\":1170},{\"display\":\"抚宁县\",\"rcode\":14310912081004,\"rid\":1171},{\"display\":\"卢龙县\",\"rcode\":14310912081005,\"rid\":1172},{\"display\":\"海港区\",\"rcode\":14310912081006,\"rid\":1173}]},{\"ccode\":1431091235,\"cid\":6302,\"display\":\"石家庄市\",\"region\":[{\"display\":\"桥东区\",\"rcode\":14310912351000,\"rid\":1174},{\"display\":\"桥西区\",\"rcode\":14310912351001,\"rid\":1175},{\"display\":\"新华区\",\"rcode\":14310912351002,\"rid\":1176},{\"display\":\"井陉矿区\",\"rcode\":14310912351003,\"rid\":1177},{\"display\":\"裕华区\",\"rcode\":14310912351004,\"rid\":1178},{\"display\":\"井陉县\",\"rcode\":14310912351005,\"rid\":1179},{\"display\":\"正定县\",\"rcode\":14310912351006,\"rid\":1180},{\"display\":\"栾城县\",\"rcode\":14310912351007,\"rid\":1181},{\"display\":\"行唐县\",\"rcode\":14310912351008,\"rid\":1182},{\"display\":\"灵寿县\",\"rcode\":14310912351009,\"rid\":1183},{\"display\":\"高邑县\",\"rcode\":14310912351010,\"rid\":1184},{\"display\":\"深泽县\",\"rcode\":14310912351011,\"rid\":1185},{\"display\":\"赞皇县\",\"rcode\":14310912351012,\"rid\":1186},{\"display\":\"无极县\",\"rcode\":14310912351013,\"rid\":1187},{\"display\":\"平山县\",\"rcode\":14310912351014,\"rid\":1188},{\"display\":\"元氏县\",\"rcode\":14310912351015,\"rid\":1189},{\"display\":\"赵县\",\"rcode\":14310912351016,\"rid\":1190},{\"display\":\"辛集市\",\"rcode\":14310912351017,\"rid\":1191},{\"display\":\"藁城市\",\"rcode\":14310912351018,\"rid\":1192},{\"display\":\"晋州市\",\"rcode\":14310912351019,\"rid\":1193},{\"display\":\"新乐市\",\"rcode\":14310912351020,\"rid\":1194},{\"display\":\"鹿泉市\",\"rcode\":14310912351021,\"rid\":1195},{\"display\":\"长安区\",\"rcode\":14310912351022,\"rid\":1196}]},{\"ccode\":1431091254,\"cid\":6303,\"display\":\"唐山市\",\"region\":[{\"display\":\"路北区\",\"rcode\":14310912541000,\"rid\":1197},{\"display\":\"古冶区\",\"rcode\":14310912541001,\"rid\":1198},{\"display\":\"开平区\",\"rcode\":14310912541002,\"rid\":1199},{\"display\":\"丰南区\",\"rcode\":14310912541003,\"rid\":1200},{\"display\":\"丰润区\",\"rcode\":14310912541004,\"rid\":1201},{\"display\":\"曹妃甸区\",\"rcode\":14310912541005,\"rid\":1202},{\"display\":\"滦　县\",\"rcode\":14310912541006,\"rid\":1203},{\"display\":\"滦南县\",\"rcode\":14310912541007,\"rid\":1204},{\"display\":\"乐亭县\",\"rcode\":14310912541008,\"rid\":1205},{\"display\":\"迁西县\",\"rcode\":14310912541009,\"rid\":1206},{\"display\":\"玉田县\",\"rcode\":14310912541010,\"rid\":1207},{\"display\":\"遵化市\",\"rcode\":14310912541011,\"rid\":1208},{\"display\":\"迁安市\",\"rcode\":14310912541012,\"rid\":1209},{\"display\":\"路南区\",\"rcode\":14310912541013,\"rid\":1210}]},{\"ccode\":1431091289,\"cid\":6304,\"display\":\"邢台市\",\"region\":[{\"display\":\"桥西区\",\"rcode\":14310912891000,\"rid\":1211},{\"display\":\"邢台县\",\"rcode\":14310912891001,\"rid\":1212},{\"display\":\"临城县\",\"rcode\":14310912891002,\"rid\":1213},{\"display\":\"内丘县\",\"rcode\":14310912891003,\"rid\":1214},{\"display\":\"柏乡县\",\"rcode\":14310912891004,\"rid\":1215},{\"display\":\"隆尧县\",\"rcode\":14310912891005,\"rid\":1216},{\"display\":\"任　县\",\"rcode\":14310912891006,\"rid\":1217},{\"display\":\"南和县\",\"rcode\":14310912891007,\"rid\":1218},{\"display\":\"宁晋县\",\"rcode\":14310912891008,\"rid\":1219},{\"display\":\"巨鹿县\",\"rcode\":14310912891009,\"rid\":1220},{\"display\":\"新河县\",\"rcode\":14310912891010,\"rid\":1221},{\"display\":\"广宗县\",\"rcode\":14310912891011,\"rid\":1222},{\"display\":\"平乡县\",\"rcode\":14310912891012,\"rid\":1223},{\"display\":\"威　县\",\"rcode\":14310912891013,\"rid\":1224},{\"display\":\"清河县\",\"rcode\":14310912891014,\"rid\":1225},{\"display\":\"临西县\",\"rcode\":14310912891015,\"rid\":1226},{\"display\":\"南宫市\",\"rcode\":14310912891016,\"rid\":1227},{\"display\":\"沙河市\",\"rcode\":14310912891017,\"rid\":1228},{\"display\":\"桥东区\",\"rcode\":14310912891018,\"rid\":1229}]},{\"ccode\":1431091327,\"cid\":6305,\"display\":\"张家口市\",\"region\":[{\"display\":\"桥西区\",\"rcode\":14310913271000,\"rid\":1230},{\"display\":\"宣化区\",\"rcode\":14310913271001,\"rid\":1231},{\"display\":\"下花园区\",\"rcode\":14310913271002,\"rid\":1232},{\"display\":\"宣化县\",\"rcode\":14310913271003,\"rid\":1233},{\"display\":\"张北县\",\"rcode\":14310913271004,\"rid\":1234},{\"display\":\"康保县\",\"rcode\":14310913271005,\"rid\":1235},{\"display\":\"沽源县\",\"rcode\":14310913271006,\"rid\":1236},{\"display\":\"尚义县\",\"rcode\":14310913271007,\"rid\":1237},{\"display\":\"蔚　县\",\"rcode\":14310913271008,\"rid\":1238},{\"display\":\"阳原县\",\"rcode\":14310913271009,\"rid\":1239},{\"display\":\"怀安县\",\"rcode\":14310913271010,\"rid\":1240},{\"display\":\"万全县\",\"rcode\":14310913271011,\"rid\":1241},{\"display\":\"怀来县\",\"rcode\":14310913271012,\"rid\":1242},{\"display\":\"涿鹿县\",\"rcode\":14310913271013,\"rid\":1243},{\"display\":\"赤城县\",\"rcode\":14310913271014,\"rid\":1244},{\"display\":\"崇礼县\",\"rcode\":14310913271015,\"rid\":1245},{\"display\":\"桥东区\",\"rcode\":14310913271016,\"rid\":1246}]}],\"display\":\"河北\",\"pcode\":143109,\"pid\":691},{\"city\":[{\"ccode\":1431101053,\"cid\":6306,\"display\":\"大庆市\",\"region\":[{\"display\":\"龙凤区\",\"rcode\":14311010531000,\"rid\":1272},{\"display\":\"让胡路区\",\"rcode\":14311010531001,\"rid\":1273},{\"display\":\"红岗区\",\"rcode\":14311010531002,\"rid\":1274},{\"display\":\"大同区\",\"rcode\":14311010531003,\"rid\":1275},{\"display\":\"肇州县\",\"rcode\":14311010531004,\"rid\":1276},{\"display\":\"肇源县\",\"rcode\":14311010531005,\"rid\":1277},{\"display\":\"林甸县\",\"rcode\":14311010531006,\"rid\":1278},{\"display\":\"杜尔伯特蒙古族自治县\",\"rcode\":14311010531007,\"rid\":1279},{\"display\":\"萨尔图区\",\"rcode\":14311010531008,\"rid\":1280}]},{\"ccode\":1431101055,\"cid\":6307,\"display\":\"大兴安岭地区\",\"region\":[{\"display\":\"塔河县\",\"rcode\":14311010551000,\"rid\":1397},{\"display\":\"漠河县\",\"rcode\":14311010551001,\"rid\":1398},{\"display\":\"呼玛县\",\"rcode\":14311010551002,\"rid\":1399}]},{\"ccode\":1431101085,\"cid\":6308,\"display\":\"哈尔滨市\",\"region\":[{\"display\":\"南岗区\",\"rcode\":14311010851000,\"rid\":1281},{\"display\":\"道外区\",\"rcode\":14311010851001,\"rid\":1282},{\"display\":\"平房区\",\"rcode\":14311010851002,\"rid\":1283},{\"display\":\"松北区\",\"rcode\":14311010851003,\"rid\":1284},{\"display\":\"香坊区\",\"rcode\":14311010851004,\"rid\":1285},{\"display\":\"呼兰区\",\"rcode\":14311010851005,\"rid\":1286},{\"display\":\"阿城区\",\"rcode\":14311010851006,\"rid\":1287},{\"display\":\"依兰县\",\"rcode\":14311010851007,\"rid\":1288},{\"display\":\"方正县\",\"rcode\":14311010851008,\"rid\":1289},{\"display\":\"宾　县\",\"rcode\":14311010851009,\"rid\":1290},{\"display\":\"巴彦县\",\"rcode\":14311010851010,\"rid\":1291},{\"display\":\"木兰县\",\"rcode\":14311010851011,\"rid\":1292},{\"display\":\"通河县\",\"rcode\":14311010851012,\"rid\":1293},{\"display\":\"延寿县\",\"rcode\":14311010851013,\"rid\":1294},{\"display\":\"双城市\",\"rcode\":14311010851014,\"rid\":1295},{\"display\":\"尚志市\",\"rcode\":14311010851015,\"rid\":1296},{\"display\":\"五常市\",\"rcode\":14311010851016,\"rid\":1297},{\"display\":\"道里区\",\"rcode\":14311010851017,\"rid\":1298}]},{\"ccode\":1431101099,\"cid\":6309,\"display\":\"鹤岗市\",\"region\":[{\"display\":\"工农区\",\"rcode\":14311010991000,\"rid\":1299},{\"display\":\"南山区\",\"rcode\":14311010991001,\"rid\":1300},{\"display\":\"兴安区\",\"rcode\":14311010991002,\"rid\":1301},{\"display\":\"东山区\",\"rcode\":14311010991003,\"rid\":1302},{\"display\":\"兴山区\",\"rcode\":14311010991004,\"rid\":1303},{\"display\":\"萝北县\",\"rcode\":14311010991005,\"rid\":1304},{\"display\":\"绥滨县\",\"rcode\":14311010991006,\"rid\":1305},{\"display\":\"向阳区\",\"rcode\":14311010991007,\"rid\":1306}]},{\"ccode\":1431101100,\"cid\":6310,\"display\":\"黑河市\",\"region\":[{\"display\":\"嫩江县\",\"rcode\":14311011001000,\"rid\":1307},{\"display\":\"逊克县\",\"rcode\":14311011001001,\"rid\":1308},{\"display\":\"孙吴县\",\"rcode\":14311011001002,\"rid\":1309},{\"display\":\"北安市\",\"rcode\":14311011001003,\"rid\":1310},{\"display\":\"五大连池市\",\"rcode\":14311011001004,\"rid\":1311},{\"display\":\"爱辉区\",\"rcode\":14311011001005,\"rid\":1312}]},{\"ccode\":1431101123,\"cid\":6311,\"display\":\"佳木斯市\",\"region\":[{\"display\":\"前进区\",\"rcode\":14311011231000,\"rid\":1313},{\"display\":\"东风区\",\"rcode\":14311011231001,\"rid\":1314},{\"display\":\"郊　区\",\"rcode\":14311011231002,\"rid\":1315},{\"display\":\"桦南县\",\"rcode\":14311011231003,\"rid\":1316},{\"display\":\"桦川县\",\"rcode\":14311011231004,\"rid\":1317},{\"display\":\"汤原县\",\"rcode\":14311011231005,\"rid\":1318},{\"display\":\"抚远县\",\"rcode\":14311011231006,\"rid\":1319},{\"display\":\"同江市\",\"rcode\":14311011231007,\"rid\":1320},{\"display\":\"富锦市\",\"rcode\":14311011231008,\"rid\":1321},{\"display\":\"向阳区\",\"rcode\":14311011231009,\"rid\":1322}]},{\"ccode\":1431101141,\"cid\":6312,\"display\":\"鸡西市\",\"region\":[{\"display\":\"恒山区\",\"rcode\":14311011411000,\"rid\":1323},{\"display\":\"滴道区\",\"rcode\":14311011411001,\"rid\":1324},{\"display\":\"梨树区\",\"rcode\":14311011411002,\"rid\":1325},{\"display\":\"城子河区\",\"rcode\":14311011411003,\"rid\":1326},{\"display\":\"麻山区\",\"rcode\":14311011411004,\"rid\":1327},{\"display\":\"鸡东县\",\"rcode\":14311011411005,\"rid\":1328},{\"display\":\"虎林市\",\"rcode\":14311011411006,\"rid\":1329},{\"display\":\"密山市\",\"rcode\":14311011411007,\"rid\":1330},{\"display\":\"鸡冠区\",\"rcode\":14311011411008,\"rid\":1331}]},{\"ccode\":1431101181,\"cid\":6313,\"display\":\"牡丹江市\",\"region\":[{\"display\":\"阳明区\",\"rcode\":14311011811000,\"rid\":1332},{\"display\":\"爱民区\",\"rcode\":14311011811001,\"rid\":1333},{\"display\":\"西安区\",\"rcode\":14311011811002,\"rid\":1334},{\"display\":\"东宁县\",\"rcode\":14311011811003,\"rid\":1335},{\"display\":\"林口县\",\"rcode\":14311011811004,\"rid\":1336},{\"display\":\"绥芬河市\",\"rcode\":14311011811005,\"rid\":1337},{\"display\":\"海林市\",\"rcode\":14311011811006,\"rid\":1338},{\"display\":\"宁安市\",\"rcode\":14311011811007,\"rid\":1339},{\"display\":\"穆棱市\",\"rcode\":14311011811008,\"rid\":1340},{\"display\":\"东安区\",\"rcode\":14311011811009,\"rid\":1341}]},{\"ccode\":1431101210,\"cid\":6314,\"display\":\"齐齐哈尔市\",\"region\":[{\"display\":\"建华区\",\"rcode\":14311012101000,\"rid\":1342},{\"display\":\"铁锋区\",\"rcode\":14311012101001,\"rid\":1343},{\"display\":\"昂昂溪区\",\"rcode\":14311012101002,\"rid\":1344},{\"display\":\"富拉尔基区\",\"rcode\":14311012101003,\"rid\":1345},{\"display\":\"碾子山区\",\"rcode\":14311012101004,\"rid\":1346},{\"display\":\"梅里斯达斡尔族区\",\"rcode\":14311012101005,\"rid\":1347},{\"display\":\"龙江县\",\"rcode\":14311012101006,\"rid\":1348},{\"display\":\"依安县\",\"rcode\":14311012101007,\"rid\":1349},{\"display\":\"泰来县\",\"rcode\":14311012101008,\"rid\":1350},{\"display\":\"甘南县\",\"rcode\":14311012101009,\"rid\":1351},{\"display\":\"富裕县\",\"rcode\":14311012101010,\"rid\":1352},{\"display\":\"克山县\",\"rcode\":14311012101011,\"rid\":1353},{\"display\":\"克东县\",\"rcode\":14311012101012,\"rid\":1354},{\"display\":\"拜泉县\",\"rcode\":14311012101013,\"rid\":1355},{\"display\":\"讷河市\",\"rcode\":14311012101014,\"rid\":1356},{\"display\":\"龙沙区\",\"rcode\":14311012101015,\"rid\":1357}]},{\"ccode\":1431101211,\"cid\":6315,\"display\":\"七台河市\",\"region\":[{\"display\":\"桃山区\",\"rcode\":14311012111000,\"rid\":1358},{\"display\":\"茄子河区\",\"rcode\":14311012111001,\"rid\":1359},{\"display\":\"勃利县\",\"rcode\":14311012111002,\"rid\":1360},{\"display\":\"新兴区\",\"rcode\":14311012111003,\"rid\":1361}]},{\"ccode\":1431101238,\"cid\":6316,\"display\":\"双鸭山市\",\"region\":[{\"display\":\"岭东区\",\"rcode\":14311012381000,\"rid\":1362},{\"display\":\"四方台区\",\"rcode\":14311012381001,\"rid\":1363},{\"display\":\"宝山区\",\"rcode\":14311012381002,\"rid\":1364},{\"display\":\"集贤县\",\"rcode\":14311012381003,\"rid\":1365},{\"display\":\"友谊县\",\"rcode\":14311012381004,\"rid\":1366},{\"display\":\"宝清县\",\"rcode\":14311012381005,\"rid\":1367},{\"display\":\"饶河县\",\"rcode\":14311012381006,\"rid\":1368},{\"display\":\"尖山区\",\"rcode\":14311012381007,\"rid\":1369}]},{\"ccode\":1431101243,\"cid\":6317,\"display\":\"绥化市\",\"region\":[{\"display\":\"望奎县\",\"rcode\":14311012431000,\"rid\":1370},{\"display\":\"兰西县\",\"rcode\":14311012431001,\"rid\":1371},{\"display\":\"青冈县\",\"rcode\":14311012431002,\"rid\":1372},{\"display\":\"庆安县\",\"rcode\":14311012431003,\"rid\":1373},{\"display\":\"明水县\",\"rcode\":14311012431004,\"rid\":1374},{\"display\":\"绥棱县\",\"rcode\":14311012431005,\"rid\":1375},{\"display\":\"安达市\",\"rcode\":14311012431006,\"rid\":1376},{\"display\":\"肇东市\",\"rcode\":14311012431007,\"rid\":1377},{\"display\":\"海伦市\",\"rcode\":14311012431008,\"rid\":1378},{\"display\":\"北林区\",\"rcode\":14311012431009,\"rid\":1379}]},{\"ccode\":1431101309,\"cid\":6318,\"display\":\"伊春市\",\"region\":[{\"display\":\"南岔区\",\"rcode\":14311013091000,\"rid\":1380},{\"display\":\"友好区\",\"rcode\":14311013091001,\"rid\":1381},{\"display\":\"西林区\",\"rcode\":14311013091002,\"rid\":1382},{\"display\":\"翠峦区\",\"rcode\":14311013091003,\"rid\":1383},{\"display\":\"新青区\",\"rcode\":14311013091004,\"rid\":1384},{\"display\":\"美溪区\",\"rcode\":14311013091005,\"rid\":1385},{\"display\":\"金山屯区\",\"rcode\":14311013091006,\"rid\":1386},{\"display\":\"五营区\",\"rcode\":14311013091007,\"rid\":1387},{\"display\":\"乌马河区\",\"rcode\":14311013091008,\"rid\":1388},{\"display\":\"汤旺河区\",\"rcode\":14311013091009,\"rid\":1389},{\"display\":\"带岭区\",\"rcode\":14311013091010,\"rid\":1390},{\"display\":\"乌伊岭区\",\"rcode\":14311013091011,\"rid\":1391},{\"display\":\"红星区\",\"rcode\":14311013091012,\"rid\":1392},{\"display\":\"上甘岭区\",\"rcode\":14311013091013,\"rid\":1393},{\"display\":\"嘉荫县\",\"rcode\":14311013091014,\"rid\":1394},{\"display\":\"铁力市\",\"rcode\":14311013091015,\"rid\":1395},{\"display\":\"伊春区\",\"rcode\":14311013091016,\"rid\":1396}]}],\"display\":\"黑龙江\",\"pcode\":143110,\"pid\":692},{\"city\":[{\"ccode\":1431111009,\"cid\":6319,\"display\":\"安阳市\",\"region\":[{\"display\":\"北关区\",\"rcode\":14311110091000,\"rid\":1549},{\"display\":\"殷都区\",\"rcode\":14311110091001,\"rid\":1550},{\"display\":\"龙安区\",\"rcode\":14311110091002,\"rid\":1551},{\"display\":\"安阳县\",\"rcode\":14311110091003,\"rid\":1552},{\"display\":\"汤阴县\",\"rcode\":14311110091004,\"rid\":1553},{\"display\":\"滑　县\",\"rcode\":14311110091005,\"rid\":1554},{\"display\":\"内黄县\",\"rcode\":14311110091006,\"rid\":1555},{\"display\":\"林州市\",\"rcode\":14311110091007,\"rid\":1556},{\"display\":\"文峰区\",\"rcode\":14311110091008,\"rid\":1557}]},{\"ccode\":1431111096,\"cid\":6320,\"display\":\"鹤壁市\",\"region\":[{\"display\":\"山城区\",\"rcode\":14311110961000,\"rid\":1400},{\"display\":\"淇滨区\",\"rcode\":14311110961001,\"rid\":1401},{\"display\":\"浚　县\",\"rcode\":14311110961002,\"rid\":1402},{\"display\":\"淇　县\",\"rcode\":14311110961003,\"rid\":1403},{\"display\":\"鹤山区\",\"rcode\":14311110961004,\"rid\":1404}]},{\"ccode\":1431111125,\"cid\":6321,\"display\":\"焦作市\",\"region\":[{\"display\":\"中站区\",\"rcode\":14311111251000,\"rid\":1405},{\"display\":\"马村区\",\"rcode\":14311111251001,\"rid\":1406},{\"display\":\"山阳区\",\"rcode\":14311111251002,\"rid\":1407},{\"display\":\"修武县\",\"rcode\":14311111251003,\"rid\":1408},{\"display\":\"博爱县\",\"rcode\":14311111251004,\"rid\":1409},{\"display\":\"武陟县\",\"rcode\":14311111251005,\"rid\":1410},{\"display\":\"温　县\",\"rcode\":14311111251006,\"rid\":1411},{\"display\":\"沁阳市\",\"rcode\":14311111251007,\"rid\":1412},{\"display\":\"孟州市\",\"rcode\":14311111251008,\"rid\":1413},{\"display\":\"解放区\",\"rcode\":14311111251009,\"rid\":1414}]},{\"ccode\":1431111142,\"cid\":6322,\"display\":\"济源市\",\"region\":[]},{\"ccode\":1431111143,\"cid\":6323,\"display\":\"开封市\",\"region\":[{\"display\":\"顺河回族区\",\"rcode\":14311111431000,\"rid\":1415},{\"display\":\"鼓楼区\",\"rcode\":14311111431001,\"rid\":1416},{\"display\":\"禹王台区\",\"rcode\":14311111431002,\"rid\":1417},{\"display\":\"金明区\",\"rcode\":14311111431003,\"rid\":1418},{\"display\":\"杞　县\",\"rcode\":14311111431004,\"rid\":1419},{\"display\":\"通许县\",\"rcode\":14311111431005,\"rid\":1420},{\"display\":\"尉氏县\",\"rcode\":14311111431006,\"rid\":1421},{\"display\":\"开封县\",\"rcode\":14311111431007,\"rid\":1422},{\"display\":\"兰考县\",\"rcode\":14311111431008,\"rid\":1423},{\"display\":\"龙亭区\",\"rcode\":14311111431009,\"rid\":1424}]},{\"ccode\":1431111172,\"cid\":6324,\"display\":\"漯河市\",\"region\":[{\"display\":\"郾城区\",\"rcode\":14311111721000,\"rid\":1425},{\"display\":\"召陵区\",\"rcode\":14311111721001,\"rid\":1426},{\"display\":\"舞阳县\",\"rcode\":14311111721002,\"rid\":1427},{\"display\":\"临颍县\",\"rcode\":14311111721003,\"rid\":1428},{\"display\":\"源汇区\",\"rcode\":14311111721004,\"rid\":1429}]},{\"ccode\":1431111173,\"cid\":6325,\"display\":\"洛阳市\",\"region\":[{\"display\":\"西工区\",\"rcode\":14311111731000,\"rid\":1430},{\"display\":\"瀍河回族区\",\"rcode\":14311111731001,\"rid\":1431},{\"display\":\"涧西区\",\"rcode\":14311111731002,\"rid\":1432},{\"display\":\"吉利区\",\"rcode\":14311111731003,\"rid\":1433},{\"display\":\"洛龙区\",\"rcode\":14311111731004,\"rid\":1434},{\"display\":\"孟津县\",\"rcode\":14311111731005,\"rid\":1435},{\"display\":\"新安县\",\"rcode\":14311111731006,\"rid\":1436},{\"display\":\"栾川县\",\"rcode\":14311111731007,\"rid\":1437},{\"display\":\"嵩　县\",\"rcode\":14311111731008,\"rid\":1438},{\"display\":\"汝阳县\",\"rcode\":14311111731009,\"rid\":1439},{\"display\":\"宜阳县\",\"rcode\":14311111731010,\"rid\":1440},{\"display\":\"洛宁县\",\"rcode\":14311111731011,\"rid\":1441},{\"display\":\"伊川县\",\"rcode\":14311111731012,\"rid\":1442},{\"display\":\"偃师市\",\"rcode\":14311111731013,\"rid\":1443},{\"display\":\"老城区\",\"rcode\":14311111731014,\"rid\":1444}]},{\"ccode\":1431111189,\"cid\":6326,\"display\":\"南阳市\",\"region\":[{\"display\":\"卧龙区\",\"rcode\":14311111891000,\"rid\":1445},{\"display\":\"南召县\",\"rcode\":14311111891001,\"rid\":1446},{\"display\":\"方城县\",\"rcode\":14311111891002,\"rid\":1447},{\"display\":\"西峡县\",\"rcode\":14311111891003,\"rid\":1448},{\"display\":\"镇平县\",\"rcode\":14311111891004,\"rid\":1449},{\"display\":\"内乡县\",\"rcode\":14311111891005,\"rid\":1450},{\"display\":\"淅川县\",\"rcode\":14311111891006,\"rid\":1451},{\"display\":\"社旗县\",\"rcode\":14311111891007,\"rid\":1452},{\"display\":\"唐河县\",\"rcode\":14311111891008,\"rid\":1453},{\"display\":\"新野县\",\"rcode\":14311111891009,\"rid\":1454},{\"display\":\"桐柏县\",\"rcode\":14311111891010,\"rid\":1455},{\"display\":\"邓州市\",\"rcode\":14311111891011,\"rid\":1456},{\"display\":\"宛城区\",\"rcode\":14311111891012,\"rid\":1457}]},{\"ccode\":1431111197,\"cid\":6327,\"display\":\"平顶山市\",\"region\":[{\"display\":\"卫东区\",\"rcode\":14311111971000,\"rid\":1458},{\"display\":\"石龙区\",\"rcode\":14311111971001,\"rid\":1459},{\"display\":\"湛河区\",\"rcode\":14311111971002,\"rid\":1460},{\"display\":\"宝丰县\",\"rcode\":14311111971003,\"rid\":1461},{\"display\":\"叶　县\",\"rcode\":14311111971004,\"rid\":1462},{\"display\":\"鲁山县\",\"rcode\":14311111971005,\"rid\":1463},{\"display\":\"郏　县\",\"rcode\":14311111971006,\"rid\":1464},{\"display\":\"舞钢市\",\"rcode\":14311111971007,\"rid\":1465},{\"display\":\"汝州市\",\"rcode\":14311111971008,\"rid\":1466},{\"display\":\"新华区\",\"rcode\":14311111971009,\"rid\":1467}]},{\"ccode\":1431111201,\"cid\":6328,\"display\":\"濮阳市\",\"region\":[{\"display\":\"清丰县\",\"rcode\":14311112011000,\"rid\":1468},{\"display\":\"南乐县\",\"rcode\":14311112011001,\"rid\":1469},{\"display\":\"范　县\",\"rcode\":14311112011002,\"rid\":1470},{\"display\":\"台前县\",\"rcode\":14311112011003,\"rid\":1471},{\"display\":\"濮阳县\",\"rcode\":14311112011004,\"rid\":1472},{\"display\":\"华龙区\",\"rcode\":14311112011005,\"rid\":1473}]},{\"ccode\":1431111217,\"cid\":6329,\"display\":\"三门峡市\",\"region\":[{\"display\":\"渑池县\",\"rcode\":14311112171000,\"rid\":1474},{\"display\":\"陕　县\",\"rcode\":14311112171001,\"rid\":1475},{\"display\":\"卢氏县\",\"rcode\":14311112171002,\"rid\":1476},{\"display\":\"义马市\",\"rcode\":14311112171003,\"rid\":1477},{\"display\":\"灵宝市\",\"rcode\":14311112171004,\"rid\":1478},{\"display\":\"湖滨区\",\"rcode\":14311112171005,\"rid\":1479}]},{\"ccode\":1431111223,\"cid\":6330,\"display\":\"商丘市\",\"region\":[{\"display\":\"睢阳区\",\"rcode\":14311112231000,\"rid\":1480},{\"display\":\"民权县\",\"rcode\":14311112231001,\"rid\":1481},{\"display\":\"睢　县\",\"rcode\":14311112231002,\"rid\":1482},{\"display\":\"宁陵县\",\"rcode\":14311112231003,\"rid\":1483},{\"display\":\"柘城县\",\"rcode\":14311112231004,\"rid\":1484},{\"display\":\"虞城县\",\"rcode\":14311112231005,\"rid\":1485},{\"display\":\"夏邑县\",\"rcode\":14311112231006,\"rid\":1486},{\"display\":\"永城市\",\"rcode\":14311112231007,\"rid\":1487},{\"display\":\"梁园区\",\"rcode\":14311112231008,\"rid\":1488}]},{\"ccode\":1431111291,\"cid\":6331,\"display\":\"新乡市\",\"region\":[{\"display\":\"卫滨区\",\"rcode\":14311112911000,\"rid\":1489},{\"display\":\"凤泉区\",\"rcode\":14311112911001,\"rid\":1490},{\"display\":\"牧野区\",\"rcode\":14311112911002,\"rid\":1491},{\"display\":\"新乡县\",\"rcode\":14311112911003,\"rid\":1492},{\"display\":\"获嘉县\",\"rcode\":14311112911004,\"rid\":1493},{\"display\":\"原阳县\",\"rcode\":14311112911005,\"rid\":1494},{\"display\":\"延津县\",\"rcode\":14311112911006,\"rid\":1495},{\"display\":\"封丘县\",\"rcode\":14311112911007,\"rid\":1496},{\"display\":\"长垣县\",\"rcode\":14311112911008,\"rid\":1497},{\"display\":\"卫辉市\",\"rcode\":14311112911009,\"rid\":1498},{\"display\":\"辉县市\",\"rcode\":14311112911010,\"rid\":1499},{\"display\":\"红旗区\",\"rcode\":14311112911011,\"rid\":1500}]},{\"ccode\":1431111292,\"cid\":6332,\"display\":\"信阳市\",\"region\":[{\"display\":\"平桥区\",\"rcode\":14311112921000,\"rid\":1501},{\"display\":\"罗山县\",\"rcode\":14311112921001,\"rid\":1502},{\"display\":\"光山县\",\"rcode\":14311112921002,\"rid\":1503},{\"display\":\"新　县\",\"rcode\":14311112921003,\"rid\":1504},{\"display\":\"商城县\",\"rcode\":14311112921004,\"rid\":1505},{\"display\":\"固始县\",\"rcode\":14311112921005,\"rid\":1506},{\"display\":\"潢川县\",\"rcode\":14311112921006,\"rid\":1507},{\"display\":\"淮滨县\",\"rcode\":14311112921007,\"rid\":1508},{\"display\":\"息　县\",\"rcode\":14311112921008,\"rid\":1509},{\"display\":\"浉河区\",\"rcode\":14311112921009,\"rid\":1510}]},{\"ccode\":1431111297,\"cid\":6333,\"display\":\"许昌市\",\"region\":[{\"display\":\"许昌县\",\"rcode\":14311112971000,\"rid\":1511},{\"display\":\"鄢陵县\",\"rcode\":14311112971001,\"rid\":1512},{\"display\":\"襄城县\",\"rcode\":14311112971002,\"rid\":1513},{\"display\":\"禹州市\",\"rcode\":14311112971003,\"rid\":1514},{\"display\":\"长葛市\",\"rcode\":14311112971004,\"rid\":1515},{\"display\":\"魏都区\",\"rcode\":14311112971005,\"rid\":1516}]},{\"ccode\":1431111333,\"cid\":6334,\"display\":\"郑州市\",\"region\":[{\"display\":\"二七区\",\"rcode\":14311113331000,\"rid\":1517},{\"display\":\"管城回族区\",\"rcode\":14311113331001,\"rid\":1518},{\"display\":\"金水区\",\"rcode\":14311113331002,\"rid\":1519},{\"display\":\"上街区\",\"rcode\":14311113331003,\"rid\":1520},{\"display\":\"惠济区\",\"rcode\":14311113331004,\"rid\":1521},{\"display\":\"中牟县\",\"rcode\":14311113331005,\"rid\":1522},{\"display\":\"巩义市\",\"rcode\":14311113331006,\"rid\":1523},{\"display\":\"荥阳市\",\"rcode\":14311113331007,\"rid\":1524},{\"display\":\"新密市\",\"rcode\":14311113331008,\"rid\":1525},{\"display\":\"新郑市\",\"rcode\":14311113331009,\"rid\":1526},{\"display\":\"登封市\",\"rcode\":14311113331010,\"rid\":1527},{\"display\":\"中原区\",\"rcode\":14311113331011,\"rid\":1528}]},{\"ccode\":1431111336,\"cid\":6335,\"display\":\"周口市\",\"region\":[{\"display\":\"扶沟县\",\"rcode\":14311113361000,\"rid\":1529},{\"display\":\"西华县\",\"rcode\":14311113361001,\"rid\":1530},{\"display\":\"商水县\",\"rcode\":14311113361002,\"rid\":1531},{\"display\":\"沈丘县\",\"rcode\":14311113361003,\"rid\":1532},{\"display\":\"郸城县\",\"rcode\":14311113361004,\"rid\":1533},{\"display\":\"淮阳县\",\"rcode\":14311113361005,\"rid\":1534},{\"display\":\"太康县\",\"rcode\":14311113361006,\"rid\":1535},{\"display\":\"鹿邑县\",\"rcode\":14311113361007,\"rid\":1536},{\"display\":\"项城市\",\"rcode\":14311113361008,\"rid\":1537},{\"display\":\"川汇区\",\"rcode\":14311113361009,\"rid\":1538}]},{\"ccode\":1431111339,\"cid\":6336,\"display\":\"驻马店市\",\"region\":[{\"display\":\"西平县\",\"rcode\":14311113391000,\"rid\":1539},{\"display\":\"上蔡县\",\"rcode\":14311113391001,\"rid\":1540},{\"display\":\"平舆县\",\"rcode\":14311113391002,\"rid\":1541},{\"display\":\"正阳县\",\"rcode\":14311113391003,\"rid\":1542},{\"display\":\"确山县\",\"rcode\":14311113391004,\"rid\":1543},{\"display\":\"泌阳县\",\"rcode\":14311113391005,\"rid\":1544},{\"display\":\"汝南县\",\"rcode\":14311113391006,\"rid\":1545},{\"display\":\"遂平县\",\"rcode\":14311113391007,\"rid\":1546},{\"display\":\"新蔡县\",\"rcode\":14311113391008,\"rid\":1547},{\"display\":\"驿城区\",\"rcode\":14311113391009,\"rid\":1548}]}],\"display\":\"河南\",\"pcode\":143111,\"pid\":693},{\"city\":[{\"ccode\":1431121064,\"cid\":6337,\"display\":\"恩施土家族苗族自治州\",\"region\":[{\"display\":\"利川市\",\"rcode\":14311210641000,\"rid\":1667},{\"display\":\"建始县\",\"rcode\":14311210641001,\"rid\":1668},{\"display\":\"巴东县\",\"rcode\":14311210641002,\"rid\":1669},{\"display\":\"宣恩县\",\"rcode\":14311210641003,\"rid\":1670},{\"display\":\"咸丰县\",\"rcode\":14311210641004,\"rid\":1671},{\"display\":\"来凤县\",\"rcode\":14311210641005,\"rid\":1672},{\"display\":\"鹤峰县\",\"rcode\":14311210641006,\"rid\":1673},{\"display\":\"恩施市\",\"rcode\":14311210641007,\"rid\":1674}]},{\"ccode\":1431121065,\"cid\":6338,\"display\":\"鄂州市\",\"region\":[{\"display\":\"华容区\",\"rcode\":14311210651000,\"rid\":1576},{\"display\":\"鄂城区\",\"rcode\":14311210651001,\"rid\":1577},{\"display\":\"梁子湖区\",\"rcode\":14311210651002,\"rid\":1578}]},{\"ccode\":1431121113,\"cid\":6339,\"display\":\"黄冈市\",\"region\":[{\"display\":\"团风县\",\"rcode\":14311211131000,\"rid\":1579},{\"display\":\"红安县\",\"rcode\":14311211131001,\"rid\":1580},{\"display\":\"罗田县\",\"rcode\":14311211131002,\"rid\":1581},{\"display\":\"英山县\",\"rcode\":14311211131003,\"rid\":1582},{\"display\":\"浠水县\",\"rcode\":14311211131004,\"rid\":1583},{\"display\":\"蕲春县\",\"rcode\":14311211131005,\"rid\":1584},{\"display\":\"黄梅县\",\"rcode\":14311211131006,\"rid\":1585},{\"display\":\"麻城市\",\"rcode\":14311211131007,\"rid\":1586},{\"display\":\"武穴市\",\"rcode\":14311211131008,\"rid\":1587},{\"display\":\"黄州区\",\"rcode\":14311211131009,\"rid\":1588}]},{\"ccode\":1431121116,\"cid\":6340,\"display\":\"黄石市\",\"region\":[{\"display\":\"西塞山区\",\"rcode\":14311211161000,\"rid\":1589},{\"display\":\"下陆区\",\"rcode\":14311211161001,\"rid\":1590},{\"display\":\"铁山区\",\"rcode\":14311211161002,\"rid\":1591},{\"display\":\"阳新县\",\"rcode\":14311211161003,\"rid\":1592},{\"display\":\"大冶市\",\"rcode\":14311211161004,\"rid\":1593},{\"display\":\"黄石港区\",\"rcode\":14311211161005,\"rid\":1594}]},{\"ccode\":1431121133,\"cid\":6341,\"display\":\"荆门市\",\"region\":[{\"display\":\"掇刀区\",\"rcode\":14311211331000,\"rid\":1595},{\"display\":\"京山县\",\"rcode\":14311211331001,\"rid\":1596},{\"display\":\"沙洋县\",\"rcode\":14311211331002,\"rid\":1597},{\"display\":\"钟祥市\",\"rcode\":14311211331003,\"rid\":1598},{\"display\":\"东宝区\",\"rcode\":14311211331004,\"rid\":1599}]},{\"ccode\":1431121134,\"cid\":6342,\"display\":\"荆州市\",\"region\":[{\"display\":\"荆州区\",\"rcode\":14311211341000,\"rid\":1600},{\"display\":\"公安县\",\"rcode\":14311211341001,\"rid\":1601},{\"display\":\"监利县\",\"rcode\":14311211341002,\"rid\":1602},{\"display\":\"江陵县\",\"rcode\":14311211341003,\"rid\":1603},{\"display\":\"石首市\",\"rcode\":14311211341004,\"rid\":1604},{\"display\":\"洪湖市\",\"rcode\":14311211341005,\"rid\":1605},{\"display\":\"松滋市\",\"rcode\":14311211341006,\"rid\":1606},{\"display\":\"沙市区\",\"rcode\":14311211341007,\"rid\":1607}]},{\"ccode\":1431121231,\"cid\":6343,\"display\":\"天门市\",\"region\":[]},{\"ccode\":1431121236,\"cid\":6344,\"display\":\"十堰市\",\"region\":[{\"display\":\"张湾区\",\"rcode\":14311212361000,\"rid\":1608},{\"display\":\"郧　县\",\"rcode\":14311212361001,\"rid\":1609},{\"display\":\"郧西县\",\"rcode\":14311212361002,\"rid\":1610},{\"display\":\"竹山县\",\"rcode\":14311212361003,\"rid\":1611},{\"display\":\"竹溪县\",\"rcode\":14311212361004,\"rid\":1612},{\"display\":\"房　县\",\"rcode\":14311212361005,\"rid\":1613},{\"display\":\"丹江口市\",\"rcode\":14311212361006,\"rid\":1614},{\"display\":\"茅箭区\",\"rcode\":14311212361007,\"rid\":1615}]},{\"ccode\":1431121245,\"cid\":6345,\"display\":\"随州市\",\"region\":[{\"display\":\"随县\",\"rcode\":14311212451000,\"rid\":1616},{\"display\":\"广水市\",\"rcode\":14311212451001,\"rid\":1617},{\"display\":\"曾都区\",\"rcode\":14311212451002,\"rid\":1618}]},{\"ccode\":1431121271,\"cid\":6346,\"display\":\"武汉市\",\"region\":[{\"display\":\"江汉区\",\"rcode\":14311212711000,\"rid\":1619},{\"display\":\"硚口区\",\"rcode\":14311212711001,\"rid\":1620},{\"display\":\"汉阳区\",\"rcode\":14311212711002,\"rid\":1621},{\"display\":\"武昌区\",\"rcode\":14311212711003,\"rid\":1622},{\"display\":\"青山区\",\"rcode\":14311212711004,\"rid\":1623},{\"display\":\"洪山区\",\"rcode\":14311212711005,\"rid\":1624},{\"display\":\"东西湖区\",\"rcode\":14311212711006,\"rid\":1625},{\"display\":\"汉南区\",\"rcode\":14311212711007,\"rid\":1626},{\"display\":\"蔡甸区\",\"rcode\":14311212711008,\"rid\":1627},{\"display\":\"江夏区\",\"rcode\":14311212711009,\"rid\":1628},{\"display\":\"黄陂区\",\"rcode\":14311212711010,\"rid\":1629},{\"display\":\"新洲区\",\"rcode\":14311212711011,\"rid\":1630},{\"display\":\"江岸区\",\"rcode\":14311212711012,\"rid\":1631}]},{\"ccode\":1431121281,\"cid\":6347,\"display\":\"襄樊市\",\"region\":[{\"display\":\"樊城区\",\"rcode\":14311212811000,\"rid\":1632},{\"display\":\"襄州区\",\"rcode\":14311212811001,\"rid\":1633},{\"display\":\"南漳县\",\"rcode\":14311212811002,\"rid\":1634},{\"display\":\"谷城县\",\"rcode\":14311212811003,\"rid\":1635},{\"display\":\"保康县\",\"rcode\":14311212811004,\"rid\":1636},{\"display\":\"老河口市\",\"rcode\":14311212811005,\"rid\":1637},{\"display\":\"枣阳市\",\"rcode\":14311212811006,\"rid\":1638},{\"display\":\"宜城市\",\"rcode\":14311212811007,\"rid\":1639},{\"display\":\"襄城区\",\"rcode\":14311212811008,\"rid\":1640}]},{\"ccode\":1431121284,\"cid\":6348,\"display\":\"咸宁市\",\"region\":[{\"display\":\"嘉鱼县\",\"rcode\":14311212841000,\"rid\":1641},{\"display\":\"通城县\",\"rcode\":14311212841001,\"rid\":1642},{\"display\":\"崇阳县\",\"rcode\":14311212841002,\"rid\":1643},{\"display\":\"通山县\",\"rcode\":14311212841003,\"rid\":1644},{\"display\":\"赤壁市\",\"rcode\":14311212841004,\"rid\":1645},{\"display\":\"咸安区\",\"rcode\":14311212841005,\"rid\":1646}]},{\"ccode\":1431121286,\"cid\":6349,\"display\":\"孝感市\",\"region\":[{\"display\":\"孝昌县\",\"rcode\":14311212861000,\"rid\":1647},{\"display\":\"大悟县\",\"rcode\":14311212861001,\"rid\":1648},{\"display\":\"云梦县\",\"rcode\":14311212861002,\"rid\":1649},{\"display\":\"应城市\",\"rcode\":14311212861003,\"rid\":1650},{\"display\":\"安陆市\",\"rcode\":14311212861004,\"rid\":1651},{\"display\":\"汉川市\",\"rcode\":14311212861005,\"rid\":1652},{\"display\":\"孝南区\",\"rcode\":14311212861006,\"rid\":1653}]},{\"ccode\":1431121308,\"cid\":6350,\"display\":\"宜昌市\",\"region\":[{\"display\":\"伍家岗区\",\"rcode\":14311213081000,\"rid\":1654},{\"display\":\"点军区\",\"rcode\":14311213081001,\"rid\":1655},{\"display\":\"猇亭区\",\"rcode\":14311213081002,\"rid\":1656},{\"display\":\"夷陵区\",\"rcode\":14311213081003,\"rid\":1657},{\"display\":\"远安县\",\"rcode\":14311213081004,\"rid\":1658},{\"display\":\"兴山县\",\"rcode\":14311213081005,\"rid\":1659},{\"display\":\"秭归县\",\"rcode\":14311213081006,\"rid\":1660},{\"display\":\"长阳土家族自治县\",\"rcode\":14311213081007,\"rid\":1661},{\"display\":\"五峰土家族自治县\",\"rcode\":14311213081008,\"rid\":1662},{\"display\":\"宜都市\",\"rcode\":14311213081009,\"rid\":1663},{\"display\":\"当阳市\",\"rcode\":14311213081010,\"rid\":1664},{\"display\":\"枝江市\",\"rcode\":14311213081011,\"rid\":1665},{\"display\":\"西陵区\",\"rcode\":14311213081012,\"rid\":1666}]}],\"display\":\"湖北\",\"pcode\":143112,\"pid\":694},{\"city\":[{\"ccode\":1431131032,\"cid\":6351,\"display\":\"常德市\",\"region\":[{\"display\":\"鼎城区\",\"rcode\":14311310321000,\"rid\":1788},{\"display\":\"安乡县\",\"rcode\":14311310321001,\"rid\":1789},{\"display\":\"汉寿县\",\"rcode\":14311310321002,\"rid\":1790},{\"display\":\"澧　县\",\"rcode\":14311310321003,\"rid\":1791},{\"display\":\"临澧县\",\"rcode\":14311310321004,\"rid\":1792},{\"display\":\"桃源县\",\"rcode\":14311310321005,\"rid\":1793},{\"display\":\"石门县\",\"rcode\":14311310321006,\"rid\":1794},{\"display\":\"津市市\",\"rcode\":14311310321007,\"rid\":1795},{\"display\":\"武陵区\",\"rcode\":14311310321008,\"rid\":1796}]},{\"ccode\":1431131035,\"cid\":6352,\"display\":\"长沙市\",\"region\":[{\"display\":\"天心区\",\"rcode\":14311310351000,\"rid\":1675},{\"display\":\"岳麓区\",\"rcode\":14311310351001,\"rid\":1676},{\"display\":\"开福区\",\"rcode\":14311310351002,\"rid\":1677},{\"display\":\"雨花区\",\"rcode\":14311310351003,\"rid\":1678},{\"display\":\"长沙县\",\"rcode\":14311310351004,\"rid\":1679},{\"display\":\"望城区\",\"rcode\":14311310351005,\"rid\":1680},{\"display\":\"宁乡县\",\"rcode\":14311310351006,\"rid\":1681},{\"display\":\"浏阳市\",\"rcode\":14311310351007,\"rid\":1682},{\"display\":\"芙蓉区\",\"rcode\":14311310351008,\"rid\":1683}]},{\"ccode\":1431131043,\"cid\":6353,\"display\":\"郴州市\",\"region\":[{\"display\":\"苏仙区\",\"rcode\":14311310431000,\"rid\":1684},{\"display\":\"桂阳县\",\"rcode\":14311310431001,\"rid\":1685},{\"display\":\"宜章县\",\"rcode\":14311310431002,\"rid\":1686},{\"display\":\"永兴县\",\"rcode\":14311310431003,\"rid\":1687},{\"display\":\"嘉禾县\",\"rcode\":14311310431004,\"rid\":1688},{\"display\":\"临武县\",\"rcode\":14311310431005,\"rid\":1689},{\"display\":\"汝城县\",\"rcode\":14311310431006,\"rid\":1690},{\"display\":\"桂东县\",\"rcode\":14311310431007,\"rid\":1691},{\"display\":\"安仁县\",\"rcode\":14311310431008,\"rid\":1692},{\"display\":\"资兴市\",\"rcode\":14311310431009,\"rid\":1693},{\"display\":\"北湖区\",\"rcode\":14311310431010,\"rid\":1694}]},{\"ccode\":1431131102,\"cid\":6354,\"display\":\"衡阳市\",\"region\":[{\"display\":\"雁峰区\",\"rcode\":14311311021000,\"rid\":1695},{\"display\":\"石鼓区\",\"rcode\":14311311021001,\"rid\":1696},{\"display\":\"蒸湘区\",\"rcode\":14311311021002,\"rid\":1697},{\"display\":\"南岳区\",\"rcode\":14311311021003,\"rid\":1698},{\"display\":\"衡阳县\",\"rcode\":14311311021004,\"rid\":1699},{\"display\":\"衡南县\",\"rcode\":14311311021005,\"rid\":1700},{\"display\":\"衡山县\",\"rcode\":14311311021006,\"rid\":1701},{\"display\":\"衡东县\",\"rcode\":14311311021007,\"rid\":1702},{\"display\":\"祁东县\",\"rcode\":14311311021008,\"rid\":1703},{\"display\":\"耒阳市\",\"rcode\":14311311021009,\"rid\":1704},{\"display\":\"常宁市\",\"rcode\":14311311021010,\"rid\":1705},{\"display\":\"珠晖区\",\"rcode\":14311311021011,\"rid\":1706}]},{\"ccode\":1431131110,\"cid\":6355,\"display\":\"怀化市\",\"region\":[{\"display\":\"中方县\",\"rcode\":14311311101000,\"rid\":1707},{\"display\":\"沅陵县\",\"rcode\":14311311101001,\"rid\":1708},{\"display\":\"辰溪县\",\"rcode\":14311311101002,\"rid\":1709},{\"display\":\"溆浦县\",\"rcode\":14311311101003,\"rid\":1710},{\"display\":\"会同县\",\"rcode\":14311311101004,\"rid\":1711},{\"display\":\"麻阳苗族自治县\",\"rcode\":14311311101005,\"rid\":1712},{\"display\":\"新晃侗族自治县\",\"rcode\":14311311101006,\"rid\":1713},{\"display\":\"芷江侗族自治县\",\"rcode\":14311311101007,\"rid\":1714},{\"display\":\"靖州苗族侗族自治县\",\"rcode\":14311311101008,\"rid\":1715},{\"display\":\"通道侗族自治县\",\"rcode\":14311311101009,\"rid\":1716},{\"display\":\"洪江市\",\"rcode\":14311311101010,\"rid\":1717},{\"display\":\"鹤城区\",\"rcode\":14311311101011,\"rid\":1718}]},{\"ccode\":1431131171,\"cid\":6356,\"display\":\"娄底市\",\"region\":[{\"display\":\"双峰县\",\"rcode\":14311311711000,\"rid\":1719},{\"display\":\"新化县\",\"rcode\":14311311711001,\"rid\":1720},{\"display\":\"冷水江市\",\"rcode\":14311311711002,\"rid\":1721},{\"display\":\"涟源市\",\"rcode\":14311311711003,\"rid\":1722},{\"display\":\"娄星区\",\"rcode\":14311311711004,\"rid\":1723}]},{\"ccode\":1431131230,\"cid\":6357,\"display\":\"邵阳市\",\"region\":[{\"display\":\"大祥区\",\"rcode\":14311312301000,\"rid\":1724},{\"display\":\"北塔区\",\"rcode\":14311312301001,\"rid\":1725},{\"display\":\"邵东县\",\"rcode\":14311312301002,\"rid\":1726},{\"display\":\"新邵县\",\"rcode\":14311312301003,\"rid\":1727},{\"display\":\"邵阳县\",\"rcode\":14311312301004,\"rid\":1728},{\"display\":\"隆回县\",\"rcode\":14311312301005,\"rid\":1729},{\"display\":\"洞口县\",\"rcode\":14311312301006,\"rid\":1730},{\"display\":\"绥宁县\",\"rcode\":14311312301007,\"rid\":1731},{\"display\":\"新宁县\",\"rcode\":14311312301008,\"rid\":1732},{\"display\":\"城步苗族自治县\",\"rcode\":14311312301009,\"rid\":1733},{\"display\":\"武冈市\",\"rcode\":14311312301010,\"rid\":1734},{\"display\":\"双清区\",\"rcode\":14311312301011,\"rid\":1735}]},{\"ccode\":1431131282,\"cid\":6358,\"display\":\"湘潭市\",\"region\":[{\"display\":\"岳塘区\",\"rcode\":14311312821000,\"rid\":1736},{\"display\":\"湘潭县\",\"rcode\":14311312821001,\"rid\":1737},{\"display\":\"湘乡市\",\"rcode\":14311312821002,\"rid\":1738},{\"display\":\"韶山市\",\"rcode\":14311312821003,\"rid\":1739},{\"display\":\"雨湖区\",\"rcode\":14311312821004,\"rid\":1740}]},{\"ccode\":1431131283,\"cid\":6359,\"display\":\"湘西土家族苗族自治州\",\"region\":[{\"display\":\"泸溪县\",\"rcode\":14311312831000,\"rid\":1741},{\"display\":\"凤凰县\",\"rcode\":14311312831001,\"rid\":1742},{\"display\":\"花垣县\",\"rcode\":14311312831002,\"rid\":1743},{\"display\":\"保靖县\",\"rcode\":14311312831003,\"rid\":1744},{\"display\":\"古丈县\",\"rcode\":14311312831004,\"rid\":1745},{\"display\":\"永顺县\",\"rcode\":14311312831005,\"rid\":1746},{\"display\":\"龙山县\",\"rcode\":14311312831006,\"rid\":1747},{\"display\":\"吉首市\",\"rcode\":14311312831007,\"rid\":1748}]},{\"ccode\":1431131316,\"cid\":6360,\"display\":\"益阳市\",\"region\":[{\"display\":\"赫山区\",\"rcode\":14311313161000,\"rid\":1749},{\"display\":\"南　县\",\"rcode\":14311313161001,\"rid\":1750},{\"display\":\"桃江县\",\"rcode\":14311313161002,\"rid\":1751},{\"display\":\"安化县\",\"rcode\":14311313161003,\"rid\":1752},{\"display\":\"沅江市\",\"rcode\":14311313161004,\"rid\":1753},{\"display\":\"资阳区\",\"rcode\":14311313161005,\"rid\":1754}]},{\"ccode\":1431131317,\"cid\":6361,\"display\":\"永州市\",\"region\":[{\"display\":\"冷水滩区\",\"rcode\":14311313171000,\"rid\":1755},{\"display\":\"祁阳县\",\"rcode\":14311313171001,\"rid\":1756},{\"display\":\"东安县\",\"rcode\":14311313171002,\"rid\":1757},{\"display\":\"双牌县\",\"rcode\":14311313171003,\"rid\":1758},{\"display\":\"道　县\",\"rcode\":14311313171004,\"rid\":1759},{\"display\":\"江永县\",\"rcode\":14311313171005,\"rid\":1760},{\"display\":\"宁远县\",\"rcode\":14311313171006,\"rid\":1761},{\"display\":\"蓝山县\",\"rcode\":14311313171007,\"rid\":1762},{\"display\":\"新田县\",\"rcode\":14311313171008,\"rid\":1763},{\"display\":\"江华瑶族自治县\",\"rcode\":14311313171009,\"rid\":1764},{\"display\":\"零陵区\",\"rcode\":14311313171010,\"rid\":1765}]},{\"ccode\":1431131318,\"cid\":6362,\"display\":\"岳阳市\",\"region\":[{\"display\":\"云溪区\",\"rcode\":14311313181000,\"rid\":1766},{\"display\":\"君山区\",\"rcode\":14311313181001,\"rid\":1767},{\"display\":\"岳阳县\",\"rcode\":14311313181002,\"rid\":1768},{\"display\":\"华容县\",\"rcode\":14311313181003,\"rid\":1769},{\"display\":\"湘阴县\",\"rcode\":14311313181004,\"rid\":1770},{\"display\":\"平江县\",\"rcode\":14311313181005,\"rid\":1771},{\"display\":\"汨罗市\",\"rcode\":14311313181006,\"rid\":1772},{\"display\":\"临湘市\",\"rcode\":14311313181007,\"rid\":1773},{\"display\":\"岳阳楼区\",\"rcode\":14311313181008,\"rid\":1774}]},{\"ccode\":1431131326,\"cid\":6363,\"display\":\"张家界市\",\"region\":[{\"display\":\"武陵源区\",\"rcode\":14311313261000,\"rid\":1775},{\"display\":\"慈利县\",\"rcode\":14311313261001,\"rid\":1776},{\"display\":\"桑植县\",\"rcode\":14311313261002,\"rid\":1777},{\"display\":\"永定区\",\"rcode\":14311313261003,\"rid\":1778}]},{\"ccode\":1431131340,\"cid\":6364,\"display\":\"株洲市\",\"region\":[{\"display\":\"芦淞区\",\"rcode\":14311313401000,\"rid\":1779},{\"display\":\"石峰区\",\"rcode\":14311313401001,\"rid\":1780},{\"display\":\"天元区\",\"rcode\":14311313401002,\"rid\":1781},{\"display\":\"株洲县\",\"rcode\":14311313401003,\"rid\":1782},{\"display\":\"攸　县\",\"rcode\":14311313401004,\"rid\":1783},{\"display\":\"茶陵县\",\"rcode\":14311313401005,\"rid\":1784},{\"display\":\"炎陵县\",\"rcode\":14311313401006,\"rid\":1785},{\"display\":\"醴陵市\",\"rcode\":14311313401007,\"rid\":1786},{\"display\":\"荷塘区\",\"rcode\":14311313401008,\"rid\":1787}]}],\"display\":\"湖南\",\"pcode\":143113,\"pid\":695},{\"city\":[{\"ccode\":1431141037,\"cid\":6365,\"display\":\"常州市\",\"region\":[{\"display\":\"钟楼区\",\"rcode\":14311410371000,\"rid\":1995},{\"display\":\"戚墅堰区\",\"rcode\":14311410371001,\"rid\":1996},{\"display\":\"新北区\",\"rcode\":14311410371002,\"rid\":1997},{\"display\":\"武进区\",\"rcode\":14311410371003,\"rid\":1998},{\"display\":\"溧阳市\",\"rcode\":14311410371004,\"rid\":1999},{\"display\":\"金坛市\",\"rcode\":14311410371005,\"rid\":2000},{\"display\":\"天宁区\",\"rcode\":14311410371006,\"rid\":2001}]},{\"ccode\":1431141112,\"cid\":6366,\"display\":\"淮安市\",\"region\":[{\"display\":\"淮安区\",\"rcode\":14311411121000,\"rid\":1898},{\"display\":\"淮阴区\",\"rcode\":14311411121001,\"rid\":1899},{\"display\":\"清浦区\",\"rcode\":14311411121002,\"rid\":1900},{\"display\":\"涟水县\",\"rcode\":14311411121003,\"rid\":1901},{\"display\":\"洪泽县\",\"rcode\":14311411121004,\"rid\":1902},{\"display\":\"盱眙县\",\"rcode\":14311411121005,\"rid\":1903},{\"display\":\"金湖县\",\"rcode\":14311411121006,\"rid\":1904},{\"display\":\"清河区\",\"rcode\":14311411121007,\"rid\":1905}]},{\"ccode\":1431141154,\"cid\":6367,\"display\":\"连云港市\",\"region\":[{\"display\":\"新浦区\",\"rcode\":14311411541000,\"rid\":1906},{\"display\":\"海州区\",\"rcode\":14311411541001,\"rid\":1907},{\"display\":\"赣榆县\",\"rcode\":14311411541002,\"rid\":1908},{\"display\":\"东海县\",\"rcode\":14311411541003,\"rid\":1909},{\"display\":\"灌云县\",\"rcode\":14311411541004,\"rid\":1910},{\"display\":\"灌南县\",\"rcode\":14311411541005,\"rid\":1911},{\"display\":\"连云区\",\"rcode\":14311411541006,\"rid\":1912}]},{\"ccode\":1431141184,\"cid\":6368,\"display\":\"南京市\",\"region\":[{\"display\":\"白下区\",\"rcode\":14311411841000,\"rid\":1913},{\"display\":\"秦淮区\",\"rcode\":14311411841001,\"rid\":1914},{\"display\":\"建邺区\",\"rcode\":14311411841002,\"rid\":1915},{\"display\":\"鼓楼区\",\"rcode\":14311411841003,\"rid\":1916},{\"display\":\"下关区\",\"rcode\":14311411841004,\"rid\":1917},{\"display\":\"浦口区\",\"rcode\":14311411841005,\"rid\":1918},{\"display\":\"栖霞区\",\"rcode\":14311411841006,\"rid\":1919},{\"display\":\"雨花台区\",\"rcode\":14311411841007,\"rid\":1920},{\"display\":\"江宁区\",\"rcode\":14311411841008,\"rid\":1921},{\"display\":\"六合区\",\"rcode\":14311411841009,\"rid\":1922},{\"display\":\"溧水县\",\"rcode\":14311411841010,\"rid\":1923},{\"display\":\"高淳县\",\"rcode\":14311411841011,\"rid\":1924},{\"display\":\"玄武区\",\"rcode\":14311411841012,\"rid\":1925}]},{\"ccode\":1431141188,\"cid\":6369,\"display\":\"南通市\",\"region\":[{\"display\":\"港闸区\",\"rcode\":14311411881000,\"rid\":1926},{\"display\":\"海安县\",\"rcode\":14311411881001,\"rid\":1927},{\"display\":\"如东县\",\"rcode\":14311411881002,\"rid\":1928},{\"display\":\"启东市\",\"rcode\":14311411881003,\"rid\":1929},{\"display\":\"如皋市\",\"rcode\":14311411881004,\"rid\":1930},{\"display\":\"通州市\",\"rcode\":14311411881005,\"rid\":1931},{\"display\":\"海门市\",\"rcode\":14311411881006,\"rid\":1932},{\"display\":\"崇川区\",\"rcode\":14311411881007,\"rid\":1933}]},{\"ccode\":1431141246,\"cid\":6370,\"display\":\"宿迁市\",\"region\":[{\"display\":\"宿豫区\",\"rcode\":14311412461000,\"rid\":1934},{\"display\":\"沭阳县\",\"rcode\":14311412461001,\"rid\":1935},{\"display\":\"泗阳县\",\"rcode\":14311412461002,\"rid\":1936},{\"display\":\"泗洪县\",\"rcode\":14311412461003,\"rid\":1937},{\"display\":\"宿城区\",\"rcode\":14311412461004,\"rid\":1938}]},{\"ccode\":1431141248,\"cid\":26684,\"display\":\"苏州市\",\"region\":[{\"display\":\"虎丘区\",\"rcode\":14311412481000,\"rid\":1939},{\"display\":\"吴中区\",\"rcode\":14311412481001,\"rid\":1940},{\"display\":\"相城区\",\"rcode\":14311412481002,\"rid\":1941},{\"display\":\"吴江区\",\"rcode\":14311412481003,\"rid\":1942},{\"display\":\"常熟市\",\"rcode\":14311412481004,\"rid\":1943},{\"display\":\"张家港市\",\"rcode\":14311412481005,\"rid\":1944},{\"display\":\"昆山市\",\"rcode\":14311412481006,\"rid\":1945},{\"display\":\"太仓市\",\"rcode\":14311412481007,\"rid\":1946},{\"display\":\"姑苏区\",\"rcode\":14311412481008,\"rid\":1947}]},{\"ccode\":1431141252,\"cid\":6372,\"display\":\"泰州市\",\"region\":[{\"display\":\"高港区\",\"rcode\":14311412521000,\"rid\":1948},{\"display\":\"兴化市\",\"rcode\":14311412521001,\"rid\":1949},{\"display\":\"靖江市\",\"rcode\":14311412521002,\"rid\":1950},{\"display\":\"泰兴市\",\"rcode\":14311412521003,\"rid\":1951},{\"display\":\"姜堰市\",\"rcode\":14311412521004,\"rid\":1952},{\"display\":\"海陵区\",\"rcode\":14311412521005,\"rid\":1953}]},{\"ccode\":1431141276,\"cid\":6373,\"display\":\"无锡市\",\"region\":[{\"display\":\"南长区\",\"rcode\":14311412761000,\"rid\":1954},{\"display\":\"北塘区\",\"rcode\":14311412761001,\"rid\":1955},{\"display\":\"锡山区\",\"rcode\":14311412761002,\"rid\":1956},{\"display\":\"惠山区\",\"rcode\":14311412761003,\"rid\":1957},{\"display\":\"滨湖区\",\"rcode\":14311412761004,\"rid\":1958},{\"display\":\"江阴市\",\"rcode\":14311412761005,\"rid\":1959},{\"display\":\"宜兴市\",\"rcode\":14311412761006,\"rid\":1960},{\"display\":\"崇安区\",\"rcode\":14311412761007,\"rid\":1961}]},{\"ccode\":1431141298,\"cid\":6374,\"display\":\"徐州市\",\"region\":[{\"display\":\"云龙区\",\"rcode\":14311412981000,\"rid\":1962},{\"display\":\"九里区\",\"rcode\":14311412981001,\"rid\":1963},{\"display\":\"贾汪区\",\"rcode\":14311412981002,\"rid\":1964},{\"display\":\"泉山区\",\"rcode\":14311412981003,\"rid\":1965},{\"display\":\"丰　县\",\"rcode\":14311412981004,\"rid\":1966},{\"display\":\"沛　县\",\"rcode\":14311412981005,\"rid\":1967},{\"display\":\"铜山县\",\"rcode\":14311412981006,\"rid\":1968},{\"display\":\"睢宁县\",\"rcode\":14311412981007,\"rid\":1969},{\"display\":\"新沂市\",\"rcode\":14311412981008,\"rid\":1970},{\"display\":\"邳州市\",\"rcode\":14311412981009,\"rid\":1971},{\"display\":\"鼓楼区\",\"rcode\":14311412981010,\"rid\":1972}]},{\"ccode\":1431141302,\"cid\":6375,\"display\":\"盐城市\",\"region\":[{\"display\":\"盐都区\",\"rcode\":14311413021000,\"rid\":1973},{\"display\":\"响水县\",\"rcode\":14311413021001,\"rid\":1974},{\"display\":\"滨海县\",\"rcode\":14311413021002,\"rid\":1975},{\"display\":\"阜宁县\",\"rcode\":14311413021003,\"rid\":1976},{\"display\":\"射阳县\",\"rcode\":14311413021004,\"rid\":1977},{\"display\":\"建湖县\",\"rcode\":14311413021005,\"rid\":1978},{\"display\":\"东台市\",\"rcode\":14311413021006,\"rid\":1979},{\"display\":\"大丰市\",\"rcode\":14311413021007,\"rid\":1980},{\"display\":\"亭湖区\",\"rcode\":14311413021008,\"rid\":1981}]},{\"ccode\":1431141305,\"cid\":6376,\"display\":\"扬州市\",\"region\":[{\"display\":\"邗江区\",\"rcode\":14311413051000,\"rid\":1982},{\"display\":\"维扬区\",\"rcode\":14311413051001,\"rid\":1983},{\"display\":\"宝应县\",\"rcode\":14311413051002,\"rid\":1984},{\"display\":\"仪征市\",\"rcode\":14311413051003,\"rid\":1985},{\"display\":\"高邮市\",\"rcode\":14311413051004,\"rid\":1986},{\"display\":\"江都市\",\"rcode\":14311413051005,\"rid\":1987},{\"display\":\"广陵区\",\"rcode\":14311413051006,\"rid\":1988}]},{\"ccode\":1431141334,\"cid\":6377,\"display\":\"镇江市\",\"region\":[{\"display\":\"润州区\",\"rcode\":14311413341000,\"rid\":1989},{\"display\":\"丹徒区\",\"rcode\":14311413341001,\"rid\":1990},{\"display\":\"丹阳市\",\"rcode\":14311413341002,\"rid\":1991},{\"display\":\"扬中市\",\"rcode\":14311413341003,\"rid\":1992},{\"display\":\"句容市\",\"rcode\":14311413341004,\"rid\":1993},{\"display\":\"京口区\",\"rcode\":14311413341005,\"rid\":1994}]}],\"display\":\"江苏\",\"pcode\":143114,\"pid\":696},{\"city\":[{\"ccode\":1431151075,\"cid\":6379,\"display\":\"赣州市\",\"region\":[{\"display\":\"赣　县\",\"rcode\":14311510751000,\"rid\":2002},{\"display\":\"信丰县\",\"rcode\":14311510751001,\"rid\":2003},{\"display\":\"大余县\",\"rcode\":14311510751002,\"rid\":2004},{\"display\":\"上犹县\",\"rcode\":14311510751003,\"rid\":2005},{\"display\":\"崇义县\",\"rcode\":14311510751004,\"rid\":2006},{\"display\":\"安远县\",\"rcode\":14311510751005,\"rid\":2007},{\"display\":\"龙南县\",\"rcode\":14311510751006,\"rid\":2008},{\"display\":\"定南县\",\"rcode\":14311510751007,\"rid\":2009},{\"display\":\"全南县\",\"rcode\":14311510751008,\"rid\":2010},{\"display\":\"宁都县\",\"rcode\":14311510751009,\"rid\":2011},{\"display\":\"于都县\",\"rcode\":14311510751010,\"rid\":2012},{\"display\":\"兴国县\",\"rcode\":14311510751011,\"rid\":2013},{\"display\":\"会昌县\",\"rcode\":14311510751012,\"rid\":2014},{\"display\":\"寻乌县\",\"rcode\":14311510751013,\"rid\":2015},{\"display\":\"石城县\",\"rcode\":14311510751014,\"rid\":2016},{\"display\":\"瑞金市\",\"rcode\":14311510751015,\"rid\":2017},{\"display\":\"南康市\",\"rcode\":14311510751016,\"rid\":2018},{\"display\":\"章贡区\",\"rcode\":14311510751017,\"rid\":2019}]},{\"ccode\":1431151121,\"cid\":6380,\"display\":\"吉安市\",\"region\":[{\"display\":\"青原区\",\"rcode\":14311511211000,\"rid\":2020},{\"display\":\"吉安县\",\"rcode\":14311511211001,\"rid\":2021},{\"display\":\"吉水县\",\"rcode\":14311511211002,\"rid\":2022},{\"display\":\"峡江县\",\"rcode\":14311511211003,\"rid\":2023},{\"display\":\"新干县\",\"rcode\":14311511211004,\"rid\":2024},{\"display\":\"永丰县\",\"rcode\":14311511211005,\"rid\":2025},{\"display\":\"泰和县\",\"rcode\":14311511211006,\"rid\":2026},{\"display\":\"遂川县\",\"rcode\":14311511211007,\"rid\":2027},{\"display\":\"万安县\",\"rcode\":14311511211008,\"rid\":2028},{\"display\":\"安福县\",\"rcode\":14311511211009,\"rid\":2029},{\"display\":\"永新县\",\"rcode\":14311511211010,\"rid\":2030},{\"display\":\"井冈山市\",\"rcode\":14311511211011,\"rid\":2031},{\"display\":\"吉州区\",\"rcode\":14311511211012,\"rid\":2032}]},{\"ccode\":1431151132,\"cid\":6381,\"display\":\"景德镇市\",\"region\":[{\"display\":\"珠山区\",\"rcode\":14311511321000,\"rid\":2033},{\"display\":\"浮梁县\",\"rcode\":14311511321001,\"rid\":2034},{\"display\":\"乐平市\",\"rcode\":14311511321002,\"rid\":2035},{\"display\":\"昌江区\",\"rcode\":14311511321003,\"rid\":2036}]},{\"ccode\":1431151139,\"cid\":6382,\"display\":\"九江市\",\"region\":[{\"display\":\"浔阳区\",\"rcode\":14311511391000,\"rid\":2037},{\"display\":\"九江县\",\"rcode\":14311511391001,\"rid\":2038},{\"display\":\"武宁县\",\"rcode\":14311511391002,\"rid\":2039},{\"display\":\"修水县\",\"rcode\":14311511391003,\"rid\":2040},{\"display\":\"永修县\",\"rcode\":14311511391004,\"rid\":2041},{\"display\":\"德安县\",\"rcode\":14311511391005,\"rid\":2042},{\"display\":\"星子县\",\"rcode\":14311511391006,\"rid\":2043},{\"display\":\"都昌县\",\"rcode\":14311511391007,\"rid\":2044},{\"display\":\"湖口县\",\"rcode\":14311511391008,\"rid\":2045},{\"display\":\"彭泽县\",\"rcode\":14311511391009,\"rid\":2046},{\"display\":\"瑞昌市\",\"rcode\":14311511391010,\"rid\":2047},{\"display\":\"庐山区\",\"rcode\":14311511391011,\"rid\":2048}]},{\"ccode\":1431151182,\"cid\":6383,\"display\":\"南昌市\",\"region\":[{\"display\":\"西湖区\",\"rcode\":14311511821000,\"rid\":2049},{\"display\":\"青云谱区\",\"rcode\":14311511821001,\"rid\":2050},{\"display\":\"湾里区\",\"rcode\":14311511821002,\"rid\":2051},{\"display\":\"青山湖区\",\"rcode\":14311511821003,\"rid\":2052},{\"display\":\"南昌县\",\"rcode\":14311511821004,\"rid\":2053},{\"display\":\"新建县\",\"rcode\":14311511821005,\"rid\":2054},{\"display\":\"安义县\",\"rcode\":14311511821006,\"rid\":2055},{\"display\":\"进贤县\",\"rcode\":14311511821007,\"rid\":2056},{\"display\":\"东湖区\",\"rcode\":14311511821008,\"rid\":2057}]},{\"ccode\":1431151199,\"cid\":6384,\"display\":\"萍乡市\",\"region\":[{\"display\":\"湘东区\",\"rcode\":14311511991000,\"rid\":2058},{\"display\":\"莲花县\",\"rcode\":14311511991001,\"rid\":2059},{\"display\":\"上栗县\",\"rcode\":14311511991002,\"rid\":2060},{\"display\":\"芦溪县\",\"rcode\":14311511991003,\"rid\":2061},{\"display\":\"安源区\",\"rcode\":14311511991004,\"rid\":2062}]},{\"ccode\":1431151224,\"cid\":6385,\"display\":\"上饶市\",\"region\":[{\"display\":\"上饶县\",\"rcode\":14311512241000,\"rid\":2063},{\"display\":\"广丰县\",\"rcode\":14311512241001,\"rid\":2064},{\"display\":\"玉山县\",\"rcode\":14311512241002,\"rid\":2065},{\"display\":\"铅山县\",\"rcode\":14311512241003,\"rid\":2066},{\"display\":\"横峰县\",\"rcode\":14311512241004,\"rid\":2067},{\"display\":\"弋阳县\",\"rcode\":14311512241005,\"rid\":2068},{\"display\":\"余干县\",\"rcode\":14311512241006,\"rid\":2069},{\"display\":\"鄱阳县\",\"rcode\":14311512241007,\"rid\":2070},{\"display\":\"万年县\",\"rcode\":14311512241008,\"rid\":2071},{\"display\":\"婺源县\",\"rcode\":14311512241009,\"rid\":2072},{\"display\":\"德兴市\",\"rcode\":14311512241010,\"rid\":2073},{\"display\":\"信州区\",\"rcode\":14311512241011,\"rid\":2074}]},{\"ccode\":1431151293,\"cid\":6386,\"display\":\"新余市\",\"region\":[{\"display\":\"分宜县\",\"rcode\":14311512931000,\"rid\":2075},{\"display\":\"渝水区\",\"rcode\":14311512931001,\"rid\":2076}]},{\"ccode\":1431151315,\"cid\":6388,\"display\":\"鹰潭市\",\"region\":[{\"display\":\"余江县\",\"rcode\":14311513151000,\"rid\":2087},{\"display\":\"贵溪市\",\"rcode\":14311513151001,\"rid\":2088},{\"display\":\"月湖区\",\"rcode\":14311513151002,\"rid\":2089}]},{\"ccode\":1431151316,\"cid\":26686,\"display\":\"抚州市\",\"region\":[{\"display\":\"南城县\",\"rcode\":14311513161000,\"rid\":2090},{\"display\":\"黎川县\",\"rcode\":14311513161001,\"rid\":2091},{\"display\":\"南丰县\",\"rcode\":14311513161002,\"rid\":2092},{\"display\":\"崇仁县\",\"rcode\":14311513161003,\"rid\":2093},{\"display\":\"乐安县\",\"rcode\":14311513161004,\"rid\":2094},{\"display\":\"宜黄县\",\"rcode\":14311513161005,\"rid\":2095},{\"display\":\"金溪县\",\"rcode\":14311513161006,\"rid\":2096},{\"display\":\"资溪县\",\"rcode\":14311513161007,\"rid\":2097},{\"display\":\"东乡县\",\"rcode\":14311513161008,\"rid\":2098},{\"display\":\"广昌县\",\"rcode\":14311513161009,\"rid\":2099},{\"display\":\"临川区\",\"rcode\":14311513161010,\"rid\":2100}]},{\"ccode\":1431151317,\"cid\":26687,\"display\":\"宜春市\",\"region\":[{\"display\":\"奉新县\",\"rcode\":14311513171000,\"rid\":2077},{\"display\":\"万载县\",\"rcode\":14311513171001,\"rid\":2078},{\"display\":\"上高县\",\"rcode\":14311513171002,\"rid\":2079},{\"display\":\"宜丰县\",\"rcode\":14311513171003,\"rid\":2080},{\"display\":\"靖安县\",\"rcode\":14311513171004,\"rid\":2081},{\"display\":\"铜鼓县\",\"rcode\":14311513171005,\"rid\":2082},{\"display\":\"丰城市\",\"rcode\":14311513171006,\"rid\":2083},{\"display\":\"樟树市\",\"rcode\":14311513171007,\"rid\":2084},{\"display\":\"高安市\",\"rcode\":14311513171008,\"rid\":2085},{\"display\":\"袁州区\",\"rcode\":14311513171009,\"rid\":2086}]}],\"display\":\"江西\",\"pcode\":143115,\"pid\":697},{\"city\":[{\"ccode\":1431161010,\"cid\":6389,\"display\":\"白城市\",\"region\":[{\"display\":\"镇赉县\",\"rcode\":14311610101000,\"rid\":2156},{\"display\":\"通榆县\",\"rcode\":14311610101001,\"rid\":2157},{\"display\":\"洮南市\",\"rcode\":14311610101002,\"rid\":2158},{\"display\":\"大安市\",\"rcode\":14311610101003,\"rid\":2159},{\"display\":\"洮北区\",\"rcode\":14311610101004,\"rid\":2160}]},{\"ccode\":1431161012,\"cid\":6390,\"display\":\"白山市\",\"region\":[{\"display\":\"江源区\",\"rcode\":14311610121000,\"rid\":2101},{\"display\":\"抚松县\",\"rcode\":14311610121001,\"rid\":2102},{\"display\":\"靖宇县\",\"rcode\":14311610121002,\"rid\":2103},{\"display\":\"长白朝鲜族自治县\",\"rcode\":14311610121003,\"rid\":2104},{\"display\":\"临江市\",\"rcode\":14311610121004,\"rid\":2105},{\"display\":\"八道江区\",\"rcode\":14311610121005,\"rid\":2106}]},{\"ccode\":1431161031,\"cid\":6391,\"display\":\"长春市\",\"region\":[{\"display\":\"宽城区\",\"rcode\":14311610311000,\"rid\":2107},{\"display\":\"朝阳区\",\"rcode\":14311610311001,\"rid\":2108},{\"display\":\"二道区\",\"rcode\":14311610311002,\"rid\":2109},{\"display\":\"绿园区\",\"rcode\":14311610311003,\"rid\":2110},{\"display\":\"双阳区\",\"rcode\":14311610311004,\"rid\":2111},{\"display\":\"农安县\",\"rcode\":14311610311005,\"rid\":2112},{\"display\":\"九台市\",\"rcode\":14311610311006,\"rid\":2113},{\"display\":\"榆树市\",\"rcode\":14311610311007,\"rid\":2114},{\"display\":\"德惠市\",\"rcode\":14311610311008,\"rid\":2115},{\"display\":\"南关区\",\"rcode\":14311610311009,\"rid\":2116}]},{\"ccode\":1431161129,\"cid\":6392,\"display\":\"吉林市\",\"region\":[{\"display\":\"龙潭区\",\"rcode\":14311611291000,\"rid\":2117},{\"display\":\"船营区\",\"rcode\":14311611291001,\"rid\":2118},{\"display\":\"丰满区\",\"rcode\":14311611291002,\"rid\":2119},{\"display\":\"永吉县\",\"rcode\":14311611291003,\"rid\":2120},{\"display\":\"蛟河市\",\"rcode\":14311611291004,\"rid\":2121},{\"display\":\"桦甸市\",\"rcode\":14311611291005,\"rid\":2122},{\"display\":\"舒兰市\",\"rcode\":14311611291006,\"rid\":2123},{\"display\":\"磐石市\",\"rcode\":14311611291007,\"rid\":2124},{\"display\":\"昌邑区\",\"rcode\":14311611291008,\"rid\":2125}]},{\"ccode\":1431161157,\"cid\":6393,\"display\":\"辽源市\",\"region\":[{\"display\":\"西安区\",\"rcode\":14311611571000,\"rid\":2126},{\"display\":\"东丰县\",\"rcode\":14311611571001,\"rid\":2127},{\"display\":\"东辽县\",\"rcode\":14311611571002,\"rid\":2128},{\"display\":\"龙山区\",\"rcode\":14311611571003,\"rid\":2129}]},{\"ccode\":1431161241,\"cid\":6394,\"display\":\"四平市\",\"region\":[{\"display\":\"铁东区\",\"rcode\":14311612411000,\"rid\":2130},{\"display\":\"梨树县\",\"rcode\":14311612411001,\"rid\":2131},{\"display\":\"伊通满族自治县\",\"rcode\":14311612411002,\"rid\":2132},{\"display\":\"公主岭市\",\"rcode\":14311612411003,\"rid\":2133},{\"display\":\"双辽市\",\"rcode\":14311612411004,\"rid\":2134},{\"display\":\"铁西区\",\"rcode\":14311612411005,\"rid\":2135}]},{\"ccode\":1431161242,\"cid\":6395,\"display\":\"松原市\",\"region\":[{\"display\":\"前郭尔罗斯蒙古族自治县\",\"rcode\":14311612421000,\"rid\":2136},{\"display\":\"长岭县\",\"rcode\":14311612421001,\"rid\":2137},{\"display\":\"乾安县\",\"rcode\":14311612421002,\"rid\":2138},{\"display\":\"扶余县\",\"rcode\":14311612421003,\"rid\":2139},{\"display\":\"宁江区\",\"rcode\":14311612421004,\"rid\":2140}]},{\"ccode\":1431161260,\"cid\":6396,\"display\":\"通化市\",\"region\":[{\"display\":\"二道江区\",\"rcode\":14311612601000,\"rid\":2141},{\"display\":\"通化县\",\"rcode\":14311612601001,\"rid\":2142},{\"display\":\"辉南县\",\"rcode\":14311612601002,\"rid\":2143},{\"display\":\"柳河县\",\"rcode\":14311612601003,\"rid\":2144},{\"display\":\"梅河口市\",\"rcode\":14311612601004,\"rid\":2145},{\"display\":\"集安市\",\"rcode\":14311612601005,\"rid\":2146},{\"display\":\"东昌区\",\"rcode\":14311612601006,\"rid\":2147}]},{\"ccode\":1431161301,\"cid\":6397,\"display\":\"延边朝鲜族自治州\",\"region\":[{\"display\":\"图们市\",\"rcode\":14311613011000,\"rid\":2148},{\"display\":\"敦化市\",\"rcode\":14311613011001,\"rid\":2149},{\"display\":\"珲春市\",\"rcode\":14311613011002,\"rid\":2150},{\"display\":\"龙井市\",\"rcode\":14311613011003,\"rid\":2151},{\"display\":\"和龙市\",\"rcode\":14311613011004,\"rid\":2152},{\"display\":\"汪清县\",\"rcode\":14311613011005,\"rid\":2153},{\"display\":\"安图县\",\"rcode\":14311613011006,\"rid\":2154},{\"display\":\"延吉市\",\"rcode\":14311613011007,\"rid\":2155}]}],\"display\":\"吉林\",\"pcode\":143116,\"pid\":698},{\"city\":[{\"ccode\":1431171007,\"cid\":6398,\"display\":\"鞍山市\",\"region\":[{\"display\":\"铁西区\",\"rcode\":14311710071000,\"rid\":2254},{\"display\":\"立山区\",\"rcode\":14311710071001,\"rid\":2255},{\"display\":\"千山区\",\"rcode\":14311710071002,\"rid\":2256},{\"display\":\"台安县\",\"rcode\":14311710071003,\"rid\":2257},{\"display\":\"岫岩满族自治县\",\"rcode\":14311710071004,\"rid\":2258},{\"display\":\"海城市\",\"rcode\":14311710071005,\"rid\":2259},{\"display\":\"铁东区\",\"rcode\":14311710071006,\"rid\":2260}]},{\"ccode\":1431171025,\"cid\":6399,\"display\":\"本溪市\",\"region\":[{\"display\":\"溪湖区\",\"rcode\":14311710251000,\"rid\":2161},{\"display\":\"明山区\",\"rcode\":14311710251001,\"rid\":2162},{\"display\":\"南芬区\",\"rcode\":14311710251002,\"rid\":2163},{\"display\":\"本溪满族自治县\",\"rcode\":14311710251003,\"rid\":2164},{\"display\":\"桓仁满族自治县\",\"rcode\":14311710251004,\"rid\":2165},{\"display\":\"平山区\",\"rcode\":14311710251005,\"rid\":2166}]},{\"ccode\":1431171039,\"cid\":6400,\"display\":\"朝阳市\",\"region\":[{\"display\":\"龙城区\",\"rcode\":14311710391000,\"rid\":2167},{\"display\":\"朝阳县\",\"rcode\":14311710391001,\"rid\":2168},{\"display\":\"建平县\",\"rcode\":14311710391002,\"rid\":2169},{\"display\":\"喀喇沁左翼蒙古族自治县\",\"rcode\":14311710391003,\"rid\":2170},{\"display\":\"北票市\",\"rcode\":14311710391004,\"rid\":2171},{\"display\":\"凌源市\",\"rcode\":14311710391005,\"rid\":2172},{\"display\":\"双塔区\",\"rcode\":14311710391006,\"rid\":2173}]},{\"ccode\":1431171051,\"cid\":6401,\"display\":\"大连市\",\"region\":[{\"display\":\"西岗区\",\"rcode\":14311710511000,\"rid\":2174},{\"display\":\"沙河口区\",\"rcode\":14311710511001,\"rid\":2175},{\"display\":\"甘井子区\",\"rcode\":14311710511002,\"rid\":2176},{\"display\":\"旅顺口区\",\"rcode\":14311710511003,\"rid\":2177},{\"display\":\"金州区\",\"rcode\":14311710511004,\"rid\":2178},{\"display\":\"长海县\",\"rcode\":14311710511005,\"rid\":2179},{\"display\":\"瓦房店市\",\"rcode\":14311710511006,\"rid\":2180},{\"display\":\"普兰店市\",\"rcode\":14311710511007,\"rid\":2181},{\"display\":\"庄河市\",\"rcode\":14311710511008,\"rid\":2182},{\"display\":\"中山区\",\"rcode\":14311710511009,\"rid\":2183}]},{\"ccode\":1431171052,\"cid\":6402,\"display\":\"丹东市\",\"region\":[{\"display\":\"振兴区\",\"rcode\":14311710521000,\"rid\":2184},{\"display\":\"振安区\",\"rcode\":14311710521001,\"rid\":2185},{\"display\":\"宽甸满族自治县\",\"rcode\":14311710521002,\"rid\":2186},{\"display\":\"东港市\",\"rcode\":14311710521003,\"rid\":2187},{\"display\":\"凤城市\",\"rcode\":14311710521004,\"rid\":2188},{\"display\":\"元宝区\",\"rcode\":14311710521005,\"rid\":2189}]},{\"ccode\":1431171069,\"cid\":6403,\"display\":\"抚顺市\",\"region\":[{\"display\":\"东洲区\",\"rcode\":14311710691000,\"rid\":2190},{\"display\":\"望花区\",\"rcode\":14311710691001,\"rid\":2191},{\"display\":\"顺城区\",\"rcode\":14311710691002,\"rid\":2192},{\"display\":\"抚顺县\",\"rcode\":14311710691003,\"rid\":2193},{\"display\":\"新宾满族自治县\",\"rcode\":14311710691004,\"rid\":2194},{\"display\":\"清原满族自治县\",\"rcode\":14311710691005,\"rid\":2195},{\"display\":\"新抚区\",\"rcode\":14311710691006,\"rid\":2196}]},{\"ccode\":1431171070,\"cid\":6404,\"display\":\"阜新市\",\"region\":[{\"display\":\"新邱区\",\"rcode\":14311710701000,\"rid\":2197},{\"display\":\"太平区\",\"rcode\":14311710701001,\"rid\":2198},{\"display\":\"清河门区\",\"rcode\":14311710701002,\"rid\":2199},{\"display\":\"细河区\",\"rcode\":14311710701003,\"rid\":2200},{\"display\":\"阜新蒙古族自治县\",\"rcode\":14311710701004,\"rid\":2201},{\"display\":\"彰武县\",\"rcode\":14311710701005,\"rid\":2202},{\"display\":\"海州区\",\"rcode\":14311710701006,\"rid\":2203}]},{\"ccode\":1431171118,\"cid\":6405,\"display\":\"葫芦岛市\",\"region\":[{\"display\":\"龙港区\",\"rcode\":14311711181000,\"rid\":2204},{\"display\":\"南票区\",\"rcode\":14311711181001,\"rid\":2205},{\"display\":\"绥中县\",\"rcode\":14311711181002,\"rid\":2206},{\"display\":\"建昌县\",\"rcode\":14311711181003,\"rid\":2207},{\"display\":\"兴城市\",\"rcode\":14311711181004,\"rid\":2208},{\"display\":\"连山区\",\"rcode\":14311711181005,\"rid\":2209}]},{\"ccode\":1431171138,\"cid\":6406,\"display\":\"锦州市\",\"region\":[{\"display\":\"凌河区\",\"rcode\":14311711381000,\"rid\":2210},{\"display\":\"太和区\",\"rcode\":14311711381001,\"rid\":2211},{\"display\":\"黑山县\",\"rcode\":14311711381002,\"rid\":2212},{\"display\":\"义　县\",\"rcode\":14311711381003,\"rid\":2213},{\"display\":\"凌海市\",\"rcode\":14311711381004,\"rid\":2214},{\"display\":\"北镇市\",\"rcode\":14311711381005,\"rid\":2215},{\"display\":\"古塔区\",\"rcode\":14311711381006,\"rid\":2216}]},{\"ccode\":1431171156,\"cid\":6407,\"display\":\"辽阳市\",\"region\":[{\"display\":\"文圣区\",\"rcode\":14311711561000,\"rid\":2217},{\"display\":\"宏伟区\",\"rcode\":14311711561001,\"rid\":2218},{\"display\":\"弓长岭区\",\"rcode\":14311711561002,\"rid\":2219},{\"display\":\"太子河区\",\"rcode\":14311711561003,\"rid\":2220},{\"display\":\"辽阳县\",\"rcode\":14311711561004,\"rid\":2221},{\"display\":\"灯塔市\",\"rcode\":14311711561005,\"rid\":2222},{\"display\":\"白塔区\",\"rcode\":14311711561006,\"rid\":2223}]},{\"ccode\":1431171195,\"cid\":6408,\"display\":\"盘锦市\",\"region\":[{\"display\":\"兴隆台区\",\"rcode\":14311711951000,\"rid\":2224},{\"display\":\"大洼县\",\"rcode\":14311711951001,\"rid\":2225},{\"display\":\"盘山县\",\"rcode\":14311711951002,\"rid\":2226},{\"display\":\"双台子区\",\"rcode\":14311711951003,\"rid\":2227}]},{\"ccode\":1431171233,\"cid\":6409,\"display\":\"沈阳市\",\"region\":[{\"display\":\"沈河区\",\"rcode\":14311712331000,\"rid\":2228},{\"display\":\"大东区\",\"rcode\":14311712331001,\"rid\":2229},{\"display\":\"皇姑区\",\"rcode\":14311712331002,\"rid\":2230},{\"display\":\"铁西区\",\"rcode\":14311712331003,\"rid\":2231},{\"display\":\"苏家屯区\",\"rcode\":14311712331004,\"rid\":2232},{\"display\":\"东陵区\",\"rcode\":14311712331005,\"rid\":2233},{\"display\":\"沈北新区\",\"rcode\":14311712331006,\"rid\":2234},{\"display\":\"于洪区\",\"rcode\":14311712331007,\"rid\":2235},{\"display\":\"辽中县\",\"rcode\":14311712331008,\"rid\":2236},{\"display\":\"康平县\",\"rcode\":14311712331009,\"rid\":2237},{\"display\":\"法库县\",\"rcode\":14311712331010,\"rid\":2238},{\"display\":\"新民市\",\"rcode\":14311712331011,\"rid\":2239},{\"display\":\"和平区\",\"rcode\":14311712331012,\"rid\":2240}]},{\"ccode\":1431171258,\"cid\":6410,\"display\":\"铁岭市\",\"region\":[{\"display\":\"清河区\",\"rcode\":14311712581000,\"rid\":2241},{\"display\":\"铁岭县\",\"rcode\":14311712581001,\"rid\":2242},{\"display\":\"西丰县\",\"rcode\":14311712581002,\"rid\":2243},{\"display\":\"昌图县\",\"rcode\":14311712581003,\"rid\":2244},{\"display\":\"调兵山市\",\"rcode\":14311712581004,\"rid\":2245},{\"display\":\"开原市\",\"rcode\":14311712581005,\"rid\":2246},{\"display\":\"银州区\",\"rcode\":14311712581006,\"rid\":2247}]},{\"ccode\":1431171314,\"cid\":6411,\"display\":\"营口市\",\"region\":[{\"display\":\"西市区\",\"rcode\":14311713141000,\"rid\":2248},{\"display\":\"鲅鱼圈区\",\"rcode\":14311713141001,\"rid\":2249},{\"display\":\"老边区\",\"rcode\":14311713141002,\"rid\":2250},{\"display\":\"盖州市\",\"rcode\":14311713141003,\"rid\":2251},{\"display\":\"大石桥市\",\"rcode\":14311713141004,\"rid\":2252},{\"display\":\"站前区\",\"rcode\":14311713141005,\"rid\":2253}]}],\"display\":\"辽宁\",\"pcode\":143117,\"pid\":699},{\"city\":[{\"ccode\":1431181002,\"cid\":6412,\"display\":\"阿拉善盟\",\"region\":[{\"display\":\"阿拉善右旗\",\"rcode\":14311810021000,\"rid\":1895},{\"display\":\"额济纳旗\",\"rcode\":14311810021001,\"rid\":1896},{\"display\":\"阿拉善左旗\",\"rcode\":14311810021002,\"rid\":1897}]},{\"ccode\":1431181018,\"cid\":6413,\"display\":\"包头市\",\"region\":[{\"display\":\"昆都仑区\",\"rcode\":14311810181000,\"rid\":1797},{\"display\":\"青山区\",\"rcode\":14311810181001,\"rid\":1798},{\"display\":\"石拐区\",\"rcode\":14311810181002,\"rid\":1799},{\"display\":\"白云鄂博矿区\",\"rcode\":14311810181003,\"rid\":1800},{\"display\":\"九原区\",\"rcode\":14311810181004,\"rid\":1801},{\"display\":\"土默特右旗\",\"rcode\":14311810181005,\"rid\":1802},{\"display\":\"固阳县\",\"rcode\":14311810181006,\"rid\":1803},{\"display\":\"达尔罕茂明安联合旗\",\"rcode\":14311810181007,\"rid\":1804},{\"display\":\"东河区\",\"rcode\":14311810181008,\"rid\":1805}]},{\"ccode\":1431181019,\"cid\":6414,\"display\":\"巴彦淖尔市\",\"region\":[{\"display\":\"五原县\",\"rcode\":14311810191000,\"rid\":1806},{\"display\":\"磴口县\",\"rcode\":14311810191001,\"rid\":1807},{\"display\":\"乌拉特前旗\",\"rcode\":14311810191002,\"rid\":1808},{\"display\":\"乌拉特中旗\",\"rcode\":14311810191003,\"rid\":1809},{\"display\":\"乌拉特后旗\",\"rcode\":14311810191004,\"rid\":1810},{\"display\":\"杭锦后旗\",\"rcode\":14311810191005,\"rid\":1811},{\"display\":\"临河区\",\"rcode\":14311810191006,\"rid\":1812}]},{\"ccode\":1431181044,\"cid\":6415,\"display\":\"赤峰市\",\"region\":[{\"display\":\"元宝山区\",\"rcode\":14311810441000,\"rid\":1813},{\"display\":\"松山区\",\"rcode\":14311810441001,\"rid\":1814},{\"display\":\"阿鲁科尔沁旗\",\"rcode\":14311810441002,\"rid\":1815},{\"display\":\"巴林左旗\",\"rcode\":14311810441003,\"rid\":1816},{\"display\":\"巴林右旗\",\"rcode\":14311810441004,\"rid\":1817},{\"display\":\"林西县\",\"rcode\":14311810441005,\"rid\":1818},{\"display\":\"克什克腾旗\",\"rcode\":14311810441006,\"rid\":1819},{\"display\":\"翁牛特旗\",\"rcode\":14311810441007,\"rid\":1820},{\"display\":\"喀喇沁旗\",\"rcode\":14311810441008,\"rid\":1821},{\"display\":\"宁城县\",\"rcode\":14311810441009,\"rid\":1822},{\"display\":\"敖汉旗\",\"rcode\":14311810441010,\"rid\":1823},{\"display\":\"红山区\",\"rcode\":14311810441011,\"rid\":1824}]},{\"ccode\":1431181066,\"cid\":6416,\"display\":\"鄂尔多斯市\",\"region\":[{\"display\":\"达拉特旗\",\"rcode\":14311810661000,\"rid\":1847},{\"display\":\"准格尔旗\",\"rcode\":14311810661001,\"rid\":1848},{\"display\":\"鄂托克前旗\",\"rcode\":14311810661002,\"rid\":1849},{\"display\":\"鄂托克旗\",\"rcode\":14311810661003,\"rid\":1850},{\"display\":\"杭锦旗\",\"rcode\":14311810661004,\"rid\":1851},{\"display\":\"乌审旗\",\"rcode\":14311810661005,\"rid\":1852},{\"display\":\"伊金霍洛旗\",\"rcode\":14311810661006,\"rid\":1853},{\"display\":\"东胜区\",\"rcode\":14311810661007,\"rid\":1854}]},{\"ccode\":1431181107,\"cid\":6417,\"display\":\"呼和浩特\",\"region\":[{\"display\":\"回民区\",\"rcode\":14311811071000,\"rid\":1825},{\"display\":\"玉泉区\",\"rcode\":14311811071001,\"rid\":1826},{\"display\":\"赛罕区\",\"rcode\":14311811071002,\"rid\":1827},{\"display\":\"土默特左旗\",\"rcode\":14311811071003,\"rid\":1828},{\"display\":\"托克托县\",\"rcode\":14311811071004,\"rid\":1829},{\"display\":\"和林格尔县\",\"rcode\":14311811071005,\"rid\":1830},{\"display\":\"清水河县\",\"rcode\":14311811071006,\"rid\":1831},{\"display\":\"武川县\",\"rcode\":14311811071007,\"rid\":1832},{\"display\":\"新城区\",\"rcode\":14311811071008,\"rid\":1833}]},{\"ccode\":1431181119,\"cid\":6418,\"display\":\"呼伦贝尔市\",\"region\":[{\"display\":\"阿荣旗\",\"rcode\":14311811191000,\"rid\":1834},{\"display\":\"莫力达瓦达斡尔族自治旗\",\"rcode\":14311811191001,\"rid\":1835},{\"display\":\"鄂伦春自治旗\",\"rcode\":14311811191002,\"rid\":1836},{\"display\":\"鄂温克族自治旗\",\"rcode\":14311811191003,\"rid\":1837},{\"display\":\"陈巴尔虎旗\",\"rcode\":14311811191004,\"rid\":1838},{\"display\":\"新巴尔虎左旗\",\"rcode\":14311811191005,\"rid\":1839},{\"display\":\"新巴尔虎右旗\",\"rcode\":14311811191006,\"rid\":1840},{\"display\":\"满洲里市\",\"rcode\":14311811191007,\"rid\":1841},{\"display\":\"牙克石市\",\"rcode\":14311811191008,\"rid\":1842},{\"display\":\"扎兰屯市\",\"rcode\":14311811191009,\"rid\":1843},{\"display\":\"额尔古纳市\",\"rcode\":14311811191010,\"rid\":1844},{\"display\":\"根河市\",\"rcode\":14311811191011,\"rid\":1845},{\"display\":\"海拉尔区\",\"rcode\":14311811191012,\"rid\":1846}]},{\"ccode\":1431181261,\"cid\":6419,\"display\":\"通辽市\",\"region\":[{\"display\":\"科尔沁左翼中旗\",\"rcode\":14311812611000,\"rid\":1855},{\"display\":\"科尔沁左翼后旗\",\"rcode\":14311812611001,\"rid\":1856},{\"display\":\"开鲁县\",\"rcode\":14311812611002,\"rid\":1857},{\"display\":\"库伦旗\",\"rcode\":14311812611003,\"rid\":1858},{\"display\":\"奈曼旗\",\"rcode\":14311812611004,\"rid\":1859},{\"display\":\"扎鲁特旗\",\"rcode\":14311812611005,\"rid\":1860},{\"display\":\"霍林郭勒市\",\"rcode\":14311812611006,\"rid\":1861},{\"display\":\"科尔沁区\",\"rcode\":14311812611007,\"rid\":1862}]},{\"ccode\":1431181270,\"cid\":6420,\"display\":\"乌海市\",\"region\":[{\"display\":\"海南区\",\"rcode\":14311812701000,\"rid\":1874},{\"display\":\"乌达区\",\"rcode\":14311812701001,\"rid\":1875},{\"display\":\"海勃湾区\",\"rcode\":14311812701002,\"rid\":1876}]},{\"ccode\":1431181273,\"cid\":6421,\"display\":\"乌兰察布市\",\"region\":[{\"display\":\"卓资县\",\"rcode\":14311812731000,\"rid\":1863},{\"display\":\"化德县\",\"rcode\":14311812731001,\"rid\":1864},{\"display\":\"商都县\",\"rcode\":14311812731002,\"rid\":1865},{\"display\":\"兴和县\",\"rcode\":14311812731003,\"rid\":1866},{\"display\":\"凉城县\",\"rcode\":14311812731004,\"rid\":1867},{\"display\":\"察哈尔右翼前旗\",\"rcode\":14311812731005,\"rid\":1868},{\"display\":\"察哈尔右翼中旗\",\"rcode\":14311812731006,\"rid\":1869},{\"display\":\"察哈尔右翼后旗\",\"rcode\":14311812731007,\"rid\":1870},{\"display\":\"四子王旗\",\"rcode\":14311812731008,\"rid\":1871},{\"display\":\"丰镇市\",\"rcode\":14311812731009,\"rid\":1872},{\"display\":\"集宁区\",\"rcode\":14311812731010,\"rid\":1873}]},{\"ccode\":1431181287,\"cid\":6422,\"display\":\"锡林郭勒盟\",\"region\":[{\"display\":\"锡林浩特市\",\"rcode\":14311812871000,\"rid\":1877},{\"display\":\"阿巴嘎旗\",\"rcode\":14311812871001,\"rid\":1878},{\"display\":\"苏尼特左旗\",\"rcode\":14311812871002,\"rid\":1879},{\"display\":\"苏尼特右旗\",\"rcode\":14311812871003,\"rid\":1880},{\"display\":\"东乌珠穆沁旗\",\"rcode\":14311812871004,\"rid\":1881},{\"display\":\"西乌珠穆沁旗\",\"rcode\":14311812871005,\"rid\":1882},{\"display\":\"太仆寺旗\",\"rcode\":14311812871006,\"rid\":1883},{\"display\":\"镶黄旗\",\"rcode\":14311812871007,\"rid\":1884},{\"display\":\"正镶白旗\",\"rcode\":14311812871008,\"rid\":1885},{\"display\":\"正蓝旗\",\"rcode\":14311812871009,\"rid\":1886},{\"display\":\"多伦县\",\"rcode\":14311812871010,\"rid\":1887},{\"display\":\"二连浩特市\",\"rcode\":14311812871011,\"rid\":1888}]},{\"ccode\":1431181288,\"cid\":6423,\"display\":\"兴安盟\",\"region\":[{\"display\":\"阿尔山市\",\"rcode\":14311812881000,\"rid\":1889},{\"display\":\"科尔沁右翼前旗\",\"rcode\":14311812881001,\"rid\":1890},{\"display\":\"科尔沁右翼中旗\",\"rcode\":14311812881002,\"rid\":1891},{\"display\":\"扎赉特旗\",\"rcode\":14311812881003,\"rid\":1892},{\"display\":\"突泉县\",\"rcode\":14311812881004,\"rid\":1893},{\"display\":\"乌兰浩特市\",\"rcode\":14311812881005,\"rid\":1894}]}],\"display\":\"内蒙古\",\"pcode\":143118,\"pid\":700},{\"city\":[{\"ccode\":1431191084,\"cid\":6424,\"display\":\"固原市\",\"region\":[{\"display\":\"西吉县\",\"rcode\":14311910841000,\"rid\":2285},{\"display\":\"隆德县\",\"rcode\":14311910841001,\"rid\":2286},{\"display\":\"泾源县\",\"rcode\":14311910841002,\"rid\":2287},{\"display\":\"彭阳县\",\"rcode\":14311910841003,\"rid\":2288},{\"display\":\"原州区\",\"rcode\":14311910841004,\"rid\":2289}]},{\"ccode\":1431191237,\"cid\":6425,\"display\":\"石嘴山市\",\"region\":[{\"display\":\"惠农区\",\"rcode\":14311912371000,\"rid\":2268},{\"display\":\"平罗县\",\"rcode\":14311912371001,\"rid\":2269},{\"display\":\"大武口区\",\"rcode\":14311912371002,\"rid\":2270}]},{\"ccode\":1431191277,\"cid\":6426,\"display\":\"吴忠市\",\"region\":[{\"display\":\"红寺堡区\",\"rcode\":14311912771000,\"rid\":2271},{\"display\":\"盐池县\",\"rcode\":14311912771001,\"rid\":2272},{\"display\":\"同心县\",\"rcode\":14311912771002,\"rid\":2273},{\"display\":\"青铜峡市\",\"rcode\":14311912771003,\"rid\":2274},{\"display\":\"利通区\",\"rcode\":14311912771004,\"rid\":2275}]},{\"ccode\":1431191313,\"cid\":6427,\"display\":\"银川市\",\"region\":[{\"display\":\"西夏区\",\"rcode\":14311913131000,\"rid\":2276},{\"display\":\"金凤区\",\"rcode\":14311913131001,\"rid\":2277},{\"display\":\"永宁县\",\"rcode\":14311913131002,\"rid\":2278},{\"display\":\"贺兰县\",\"rcode\":14311913131003,\"rid\":2279},{\"display\":\"灵武市\",\"rcode\":14311913131004,\"rid\":2280},{\"display\":\"兴庆区\",\"rcode\":14311913131005,\"rid\":2281}]},{\"ccode\":1431191315,\"cid\":26688,\"display\":\"中卫市\",\"region\":[{\"display\":\"中宁县\",\"rcode\":14311913151000,\"rid\":2282},{\"display\":\"海原县\",\"rcode\":14311913151001,\"rid\":2283},{\"display\":\"沙坡头区\",\"rcode\":14311913151002,\"rid\":2284}]}],\"display\":\"宁夏\",\"pcode\":143119,\"pid\":701},{\"city\":[{\"ccode\":1431201083,\"cid\":6428,\"display\":\"果洛藏族自治州\",\"region\":[{\"display\":\"班玛县\",\"rcode\":14312010831000,\"rid\":2327},{\"display\":\"甘德县\",\"rcode\":14312010831001,\"rid\":2328},{\"display\":\"达日县\",\"rcode\":14312010831002,\"rid\":2329},{\"display\":\"久治县\",\"rcode\":14312010831003,\"rid\":2330},{\"display\":\"玛多县\",\"rcode\":14312010831004,\"rid\":2331},{\"display\":\"玛沁县\",\"rcode\":14312010831005,\"rid\":2332}]},{\"ccode\":1431201086,\"cid\":6429,\"display\":\"海北藏族自治州\",\"region\":[{\"display\":\"祁连县\",\"rcode\":14312010861000,\"rid\":2290},{\"display\":\"海晏县\",\"rcode\":14312010861001,\"rid\":2291},{\"display\":\"刚察县\",\"rcode\":14312010861002,\"rid\":2292},{\"display\":\"门源回族自治县\",\"rcode\":14312010861003,\"rid\":2293}]},{\"ccode\":1431201087,\"cid\":6430,\"display\":\"海东地区\",\"region\":[{\"display\":\"民和回族土族自治县\",\"rcode\":14312010871000,\"rid\":2294},{\"display\":\"乐都县\",\"rcode\":14312010871001,\"rid\":2295},{\"display\":\"互助土族自治县\",\"rcode\":14312010871002,\"rid\":2296},{\"display\":\"化隆回族自治县\",\"rcode\":14312010871003,\"rid\":2297},{\"display\":\"循化撒拉族自治县\",\"rcode\":14312010871004,\"rid\":2298},{\"display\":\"平安县\",\"rcode\":14312010871005,\"rid\":2299}]},{\"ccode\":1431201090,\"cid\":6431,\"display\":\"海南藏族自治州\",\"region\":[{\"display\":\"同德县\",\"rcode\":14312010901000,\"rid\":2300},{\"display\":\"贵德县\",\"rcode\":14312010901001,\"rid\":2301},{\"display\":\"兴海县\",\"rcode\":14312010901002,\"rid\":2302},{\"display\":\"贵南县\",\"rcode\":14312010901003,\"rid\":2303},{\"display\":\"共和县\",\"rcode\":14312010901004,\"rid\":2304}]},{\"ccode\":1431201091,\"cid\":6432,\"display\":\"海西蒙古族藏族自治州\",\"region\":[{\"display\":\"德令哈市\",\"rcode\":14312010911000,\"rid\":2305},{\"display\":\"乌兰县\",\"rcode\":14312010911001,\"rid\":2306},{\"display\":\"都兰县\",\"rcode\":14312010911002,\"rid\":2307},{\"display\":\"天峻县\",\"rcode\":14312010911003,\"rid\":2308},{\"display\":\"格尔木市\",\"rcode\":14312010911004,\"rid\":2309}]},{\"ccode\":1431201114,\"cid\":6433,\"display\":\"黄南藏族自治州\",\"region\":[{\"display\":\"尖扎县\",\"rcode\":14312011141000,\"rid\":2310},{\"display\":\"泽库县\",\"rcode\":14312011141001,\"rid\":2311},{\"display\":\"河南蒙古族自治县\",\"rcode\":14312011141002,\"rid\":2312},{\"display\":\"同仁县\",\"rcode\":14312011141003,\"rid\":2313}]},{\"ccode\":1431201290,\"cid\":6434,\"display\":\"西宁市\",\"region\":[{\"display\":\"城中区\",\"rcode\":14312012901000,\"rid\":2314},{\"display\":\"城西区\",\"rcode\":14312012901001,\"rid\":2315},{\"display\":\"城北区\",\"rcode\":14312012901002,\"rid\":2316},{\"display\":\"大通回族土族自治县\",\"rcode\":14312012901003,\"rid\":2317},{\"display\":\"湟中县\",\"rcode\":14312012901004,\"rid\":2318},{\"display\":\"湟源县\",\"rcode\":14312012901005,\"rid\":2319},{\"display\":\"城东区\",\"rcode\":14312012901006,\"rid\":2320}]},{\"ccode\":1431201323,\"cid\":6435,\"display\":\"玉树藏族自治州\",\"region\":[{\"display\":\"杂多县\",\"rcode\":14312013231000,\"rid\":2321},{\"display\":\"称多县\",\"rcode\":14312013231001,\"rid\":2322},{\"display\":\"治多县\",\"rcode\":14312013231002,\"rid\":2323},{\"display\":\"囊谦县\",\"rcode\":14312013231003,\"rid\":2324},{\"display\":\"曲麻莱县\",\"rcode\":14312013231004,\"rid\":2325},{\"display\":\"玉树县\",\"rcode\":14312013231005,\"rid\":2326}]}],\"display\":\"青海\",\"pcode\":143120,\"pid\":702},{\"city\":[{\"ccode\":1431211005,\"cid\":6436,\"display\":\"安康市\",\"region\":[]},{\"ccode\":1431211016,\"cid\":6437,\"display\":\"宝鸡市\",\"region\":[]},{\"ccode\":1431211095,\"cid\":6438,\"display\":\"汉中市\",\"region\":[]},{\"ccode\":1431211222,\"cid\":6439,\"display\":\"商洛市\",\"region\":[]},{\"ccode\":1431211259,\"cid\":6440,\"display\":\"铜川市\",\"region\":[]},{\"ccode\":1431211267,\"cid\":6441,\"display\":\"渭南市\",\"region\":[]},{\"ccode\":1431211279,\"cid\":6442,\"display\":\"西安市\",\"region\":[]},{\"ccode\":1431211285,\"cid\":6443,\"display\":\"咸阳市\",\"region\":[]},{\"ccode\":1431211300,\"cid\":6444,\"display\":\"延安市\",\"region\":[]},{\"ccode\":1431211308,\"cid\":26683,\"display\":\"榆林市\",\"region\":[]}],\"display\":\"陕西\",\"pcode\":143121,\"pid\":703},{\"city\":[{\"ccode\":1431221266,\"cid\":6459,\"display\":\"威海市\",\"region\":[{\"display\":\"文登市\",\"rcode\":14312212661000,\"rid\":2543},{\"display\":\"荣成市\",\"rcode\":14312212661001,\"rid\":2544},{\"display\":\"乳山市\",\"rcode\":14312212661002,\"rid\":2545},{\"display\":\"环翠区\",\"rcode\":14312212661003,\"rid\":2546}]},{\"ccode\":1431221306,\"cid\":6460,\"display\":\"烟台市\",\"region\":[{\"display\":\"福山区\",\"rcode\":14312213061000,\"rid\":2547},{\"display\":\"牟平区\",\"rcode\":14312213061001,\"rid\":2548},{\"display\":\"莱山区\",\"rcode\":14312213061002,\"rid\":2549},{\"display\":\"长岛县\",\"rcode\":14312213061003,\"rid\":2550},{\"display\":\"龙口市\",\"rcode\":14312213061004,\"rid\":2551},{\"display\":\"莱阳市\",\"rcode\":14312213061005,\"rid\":2552},{\"display\":\"莱州市\",\"rcode\":14312213061006,\"rid\":2553},{\"display\":\"蓬莱市\",\"rcode\":14312213061007,\"rid\":2554},{\"display\":\"招远市\",\"rcode\":14312213061008,\"rid\":2555},{\"display\":\"栖霞市\",\"rcode\":14312213061009,\"rid\":2556},{\"display\":\"海阳市\",\"rcode\":14312213061010,\"rid\":2557},{\"display\":\"芝罘区\",\"rcode\":14312213061011,\"rid\":2558}]},{\"ccode\":1431221325,\"cid\":6461,\"display\":\"枣庄市\",\"region\":[{\"display\":\"薛城区\",\"rcode\":14312213251000,\"rid\":2559},{\"display\":\"峄城区\",\"rcode\":14312213251001,\"rid\":2560},{\"display\":\"台儿庄区\",\"rcode\":14312213251002,\"rid\":2561},{\"display\":\"山亭区\",\"rcode\":14312213251003,\"rid\":2562},{\"display\":\"滕州市\",\"rcode\":14312213251004,\"rid\":2563},{\"display\":\"市中区\",\"rcode\":14312213251005,\"rid\":2564}]},{\"ccode\":1431221341,\"cid\":6462,\"display\":\"淄博市\",\"region\":[{\"display\":\"张店区\",\"rcode\":14312213411000,\"rid\":2565},{\"display\":\"博山区\",\"rcode\":14312213411001,\"rid\":2566},{\"display\":\"临淄区\",\"rcode\":14312213411002,\"rid\":2567},{\"display\":\"周村区\",\"rcode\":14312213411003,\"rid\":2568},{\"display\":\"桓台县\",\"rcode\":14312213411004,\"rid\":2569},{\"display\":\"高青县\",\"rcode\":14312213411005,\"rid\":2570},{\"display\":\"沂源县\",\"rcode\":14312213411006,\"rid\":2571},{\"display\":\"淄川区\",\"rcode\":14312213411007,\"rid\":2572}]},{\"ccode\":1431221027,\"cid\":6446,\"display\":\"滨州市\",\"region\":[{\"display\":\"惠民县\",\"rcode\":14312210271000,\"rid\":2573},{\"display\":\"阳信县\",\"rcode\":14312210271001,\"rid\":2574},{\"display\":\"无棣县\",\"rcode\":14312210271002,\"rid\":2575},{\"display\":\"沾化县\",\"rcode\":14312210271003,\"rid\":2576},{\"display\":\"博兴县\",\"rcode\":14312210271004,\"rid\":2577},{\"display\":\"邹平县\",\"rcode\":14312210271005,\"rid\":2578},{\"display\":\"滨城区\",\"rcode\":14312210271006,\"rid\":2579}]},{\"ccode\":1431221059,\"cid\":6447,\"display\":\"德州市\",\"region\":[{\"display\":\"陵　县\",\"rcode\":14312210591000,\"rid\":2440},{\"display\":\"宁津县\",\"rcode\":14312210591001,\"rid\":2441},{\"display\":\"庆云县\",\"rcode\":14312210591002,\"rid\":2442},{\"display\":\"临邑县\",\"rcode\":14312210591003,\"rid\":2443},{\"display\":\"齐河县\",\"rcode\":14312210591004,\"rid\":2444},{\"display\":\"平原县\",\"rcode\":14312210591005,\"rid\":2445},{\"display\":\"夏津县\",\"rcode\":14312210591006,\"rid\":2446},{\"display\":\"武城县\",\"rcode\":14312210591007,\"rid\":2447},{\"display\":\"乐陵市\",\"rcode\":14312210591008,\"rid\":2448},{\"display\":\"禹城市\",\"rcode\":14312210591009,\"rid\":2449},{\"display\":\"德城区\",\"rcode\":14312210591010,\"rid\":2450}]},{\"ccode\":1431221063,\"cid\":6448,\"display\":\"东营市\",\"region\":[{\"display\":\"河口区\",\"rcode\":14312210631000,\"rid\":2451},{\"display\":\"垦利县\",\"rcode\":14312210631001,\"rid\":2452},{\"display\":\"利津县\",\"rcode\":14312210631002,\"rid\":2453},{\"display\":\"广饶县\",\"rcode\":14312210631003,\"rid\":2454},{\"display\":\"东营区\",\"rcode\":14312210631004,\"rid\":2455}]},{\"ccode\":1431221105,\"cid\":6449,\"display\":\"荷泽市\",\"region\":[{\"display\":\"曹　县\",\"rcode\":14312211051000,\"rid\":2456},{\"display\":\"单　县\",\"rcode\":14312211051001,\"rid\":2457},{\"display\":\"成武县\",\"rcode\":14312211051002,\"rid\":2458},{\"display\":\"巨野县\",\"rcode\":14312211051003,\"rid\":2459},{\"display\":\"郓城县\",\"rcode\":14312211051004,\"rid\":2460},{\"display\":\"鄄城县\",\"rcode\":14312211051005,\"rid\":2461},{\"display\":\"定陶县\",\"rcode\":14312211051006,\"rid\":2462},{\"display\":\"东明县\",\"rcode\":14312211051007,\"rid\":2463},{\"display\":\"牡丹区\",\"rcode\":14312211051008,\"rid\":2464}]},{\"ccode\":1431221122,\"cid\":6450,\"display\":\"济南市\",\"region\":[{\"display\":\"市中区\",\"rcode\":14312211221000,\"rid\":2465},{\"display\":\"槐荫区\",\"rcode\":14312211221001,\"rid\":2466},{\"display\":\"天桥区\",\"rcode\":14312211221002,\"rid\":2467},{\"display\":\"历城区\",\"rcode\":14312211221003,\"rid\":2468},{\"display\":\"长清区\",\"rcode\":14312211221004,\"rid\":2469},{\"display\":\"平阴县\",\"rcode\":14312211221005,\"rid\":2470},{\"display\":\"济阳县\",\"rcode\":14312211221006,\"rid\":2471},{\"display\":\"商河县\",\"rcode\":14312211221007,\"rid\":2472},{\"display\":\"章丘市\",\"rcode\":14312211221008,\"rid\":2473},{\"display\":\"历下区\",\"rcode\":14312211221009,\"rid\":2474}]},{\"ccode\":1431221136,\"cid\":6451,\"display\":\"济宁市\",\"region\":[{\"display\":\"任城区\",\"rcode\":14312211361000,\"rid\":2475},{\"display\":\"微山县\",\"rcode\":14312211361001,\"rid\":2476},{\"display\":\"鱼台县\",\"rcode\":14312211361002,\"rid\":2477},{\"display\":\"金乡县\",\"rcode\":14312211361003,\"rid\":2478},{\"display\":\"嘉祥县\",\"rcode\":14312211361004,\"rid\":2479},{\"display\":\"汶上县\",\"rcode\":14312211361005,\"rid\":2480},{\"display\":\"泗水县\",\"rcode\":14312211361006,\"rid\":2481},{\"display\":\"梁山县\",\"rcode\":14312211361007,\"rid\":2482},{\"display\":\"曲阜市\",\"rcode\":14312211361008,\"rid\":2483},{\"display\":\"兖州市\",\"rcode\":14312211361009,\"rid\":2484},{\"display\":\"邹城市\",\"rcode\":14312211361010,\"rid\":2485},{\"display\":\"市中区\",\"rcode\":14312211361011,\"rid\":2486}]},{\"ccode\":1431221148,\"cid\":6452,\"display\":\"莱芜市\",\"region\":[{\"display\":\"钢城区\",\"rcode\":14312211481000,\"rid\":2487},{\"display\":\"莱城区\",\"rcode\":14312211481001,\"rid\":2488}]},{\"ccode\":1431221155,\"cid\":6453,\"display\":\"聊城市\",\"region\":[{\"display\":\"阳谷县\",\"rcode\":14312211551000,\"rid\":2489},{\"display\":\"莘　县\",\"rcode\":14312211551001,\"rid\":2490},{\"display\":\"茌平县\",\"rcode\":14312211551002,\"rid\":2491},{\"display\":\"东阿县\",\"rcode\":14312211551003,\"rid\":2492},{\"display\":\"冠　县\",\"rcode\":14312211551004,\"rid\":2493},{\"display\":\"高唐县\",\"rcode\":14312211551005,\"rid\":2494},{\"display\":\"临清市\",\"rcode\":14312211551006,\"rid\":2495},{\"display\":\"东昌府区\",\"rcode\":14312211551007,\"rid\":2496}]},{\"ccode\":1431221162,\"cid\":6454,\"display\":\"临沂市\",\"region\":[{\"display\":\"罗庄区\",\"rcode\":14312211621000,\"rid\":2497},{\"display\":\"河东区\",\"rcode\":14312211621001,\"rid\":2498},{\"display\":\"沂南县\",\"rcode\":14312211621002,\"rid\":2499},{\"display\":\"郯城县\",\"rcode\":14312211621003,\"rid\":2500},{\"display\":\"沂水县\",\"rcode\":14312211621004,\"rid\":2501},{\"display\":\"苍山县\",\"rcode\":14312211621005,\"rid\":2502},{\"display\":\"费　县\",\"rcode\":14312211621006,\"rid\":2503},{\"display\":\"平邑县\",\"rcode\":14312211621007,\"rid\":2504},{\"display\":\"莒南县\",\"rcode\":14312211621008,\"rid\":2505},{\"display\":\"蒙阴县\",\"rcode\":14312211621009,\"rid\":2506},{\"display\":\"临沭县\",\"rcode\":14312211621010,\"rid\":2507},{\"display\":\"兰山区\",\"rcode\":14312211621011,\"rid\":2508}]},{\"ccode\":1431221205,\"cid\":6455,\"display\":\"青岛市\",\"region\":[{\"display\":\"市北区\",\"rcode\":14312212051000,\"rid\":2509},{\"display\":\"四方区\",\"rcode\":14312212051001,\"rid\":2510},{\"display\":\"黄岛区\",\"rcode\":14312212051002,\"rid\":2511},{\"display\":\"崂山区\",\"rcode\":14312212051003,\"rid\":2512},{\"display\":\"李沧区\",\"rcode\":14312212051004,\"rid\":2513},{\"display\":\"城阳区\",\"rcode\":14312212051005,\"rid\":2514},{\"display\":\"胶州市\",\"rcode\":14312212051006,\"rid\":2515},{\"display\":\"即墨市\",\"rcode\":14312212051007,\"rid\":2516},{\"display\":\"平度市\",\"rcode\":14312212051008,\"rid\":2517},{\"display\":\"胶南市\",\"rcode\":14312212051009,\"rid\":2518},{\"display\":\"莱西市\",\"rcode\":14312212051010,\"rid\":2519},{\"display\":\"市南区\",\"rcode\":14312212051011,\"rid\":2520}]},{\"ccode\":1431221216,\"cid\":6456,\"display\":\"日照市\",\"region\":[{\"display\":\"岚山区\",\"rcode\":14312212161000,\"rid\":2521},{\"display\":\"五莲县\",\"rcode\":14312212161001,\"rid\":2522},{\"display\":\"莒　县\",\"rcode\":14312212161002,\"rid\":2523},{\"display\":\"东港区\",\"rcode\":14312212161003,\"rid\":2524}]},{\"ccode\":1431221250,\"cid\":6457,\"display\":\"泰安市\",\"region\":[{\"display\":\"岱岳区\",\"rcode\":14312212501000,\"rid\":2525},{\"display\":\"宁阳县\",\"rcode\":14312212501001,\"rid\":2526},{\"display\":\"东平县\",\"rcode\":14312212501002,\"rid\":2527},{\"display\":\"新泰市\",\"rcode\":14312212501003,\"rid\":2528},{\"display\":\"肥城市\",\"rcode\":14312212501004,\"rid\":2529},{\"display\":\"泰山区\",\"rcode\":14312212501005,\"rid\":2530}]},{\"ccode\":1431221265,\"cid\":6458,\"display\":\"潍坊市\",\"region\":[{\"display\":\"寒亭区\",\"rcode\":14312212651000,\"rid\":2531},{\"display\":\"坊子区\",\"rcode\":14312212651001,\"rid\":2532},{\"display\":\"奎文区\",\"rcode\":14312212651002,\"rid\":2533},{\"display\":\"临朐县\",\"rcode\":14312212651003,\"rid\":2534},{\"display\":\"昌乐县\",\"rcode\":14312212651004,\"rid\":2535},{\"display\":\"青州市\",\"rcode\":14312212651005,\"rid\":2536},{\"display\":\"诸城市\",\"rcode\":14312212651006,\"rid\":2537},{\"display\":\"寿光市\",\"rcode\":14312212651007,\"rid\":2538},{\"display\":\"安丘市\",\"rcode\":14312212651008,\"rid\":2539},{\"display\":\"高密市\",\"rcode\":14312212651009,\"rid\":2540},{\"display\":\"昌邑市\",\"rcode\":14312212651010,\"rid\":2541},{\"display\":\"潍城区\",\"rcode\":14312212651011,\"rid\":2542}]}],\"display\":\"山东\",\"pcode\":143122,\"pid\":704},{\"city\":[{\"ccode\":1431231220,\"cid\":6463,\"display\":\"上海市\",\"region\":[{\"display\":\"长宁\",\"rcode\":14312312201000,\"rid\":2580},{\"display\":\"崇明\",\"rcode\":14312312201001,\"rid\":2581},{\"display\":\"奉贤\",\"rcode\":14312312201002,\"rid\":2582},{\"display\":\"虹口\",\"rcode\":14312312201003,\"rid\":2583},{\"display\":\"黄浦\",\"rcode\":14312312201004,\"rid\":2584},{\"display\":\"嘉定\",\"rcode\":14312312201005,\"rid\":2585},{\"display\":\"静安\",\"rcode\":14312312201006,\"rid\":2586},{\"display\":\"金山\",\"rcode\":14312312201007,\"rid\":2587},{\"display\":\"卢湾\",\"rcode\":14312312201008,\"rid\":2588},{\"display\":\"闵行\",\"rcode\":14312312201009,\"rid\":2589},{\"display\":\"浦东新区\",\"rcode\":14312312201011,\"rid\":2591},{\"display\":\"普陀\",\"rcode\":14312312201012,\"rid\":2592},{\"display\":\"青浦\",\"rcode\":14312312201013,\"rid\":2593},{\"display\":\"松江\",\"rcode\":14312312201014,\"rid\":2594},{\"display\":\"徐汇\",\"rcode\":14312312201015,\"rid\":2595},{\"display\":\"杨浦\",\"rcode\":14312312201016,\"rid\":2596},{\"display\":\"闸北\",\"rcode\":14312312201017,\"rid\":2597},{\"display\":\"宝山\",\"rcode\":14312312201018,\"rid\":2598}]}],\"display\":\"上海\",\"pcode\":143123,\"pid\":705},{\"city\":[{\"ccode\":1431241036,\"cid\":6465,\"display\":\"长治市\",\"region\":[{\"display\":\"郊　区\",\"rcode\":14312410361000,\"rid\":2705},{\"display\":\"长治县\",\"rcode\":14312410361001,\"rid\":2706},{\"display\":\"襄垣县\",\"rcode\":14312410361002,\"rid\":2707},{\"display\":\"屯留县\",\"rcode\":14312410361003,\"rid\":2708},{\"display\":\"平顺县\",\"rcode\":14312410361004,\"rid\":2709},{\"display\":\"黎城县\",\"rcode\":14312410361005,\"rid\":2710},{\"display\":\"壶关县\",\"rcode\":14312410361006,\"rid\":2711},{\"display\":\"长子县\",\"rcode\":14312410361007,\"rid\":2712},{\"display\":\"武乡县\",\"rcode\":14312410361008,\"rid\":2713},{\"display\":\"沁　县\",\"rcode\":14312410361009,\"rid\":2714},{\"display\":\"沁源县\",\"rcode\":14312410361010,\"rid\":2715},{\"display\":\"潞城市\",\"rcode\":14312410361011,\"rid\":2716},{\"display\":\"城　区\",\"rcode\":14312410361012,\"rid\":2717}]},{\"ccode\":1431241054,\"cid\":6466,\"display\":\"大同市\",\"region\":[{\"display\":\"矿　区\",\"rcode\":14312410541000,\"rid\":2599},{\"display\":\"南郊区\",\"rcode\":14312410541001,\"rid\":2600},{\"display\":\"新荣区\",\"rcode\":14312410541002,\"rid\":2601},{\"display\":\"阳高县\",\"rcode\":14312410541003,\"rid\":2602},{\"display\":\"天镇县\",\"rcode\":14312410541004,\"rid\":2603},{\"display\":\"广灵县\",\"rcode\":14312410541005,\"rid\":2604},{\"display\":\"灵丘县\",\"rcode\":14312410541006,\"rid\":2605},{\"display\":\"浑源县\",\"rcode\":14312410541007,\"rid\":2606},{\"display\":\"左云县\",\"rcode\":14312410541008,\"rid\":2607},{\"display\":\"大同县\",\"rcode\":14312410541009,\"rid\":2608},{\"display\":\"城　区\",\"rcode\":14312410541010,\"rid\":2609}]},{\"ccode\":1431241131,\"cid\":6467,\"display\":\"晋城市\",\"region\":[{\"display\":\"沁水县\",\"rcode\":14312411311000,\"rid\":2610},{\"display\":\"阳城县\",\"rcode\":14312411311001,\"rid\":2611},{\"display\":\"陵川县\",\"rcode\":14312411311002,\"rid\":2612},{\"display\":\"泽州县\",\"rcode\":14312411311003,\"rid\":2613},{\"display\":\"高平市\",\"rcode\":14312411311004,\"rid\":2614},{\"display\":\"城　区\",\"rcode\":14312411311005,\"rid\":2615}]},{\"ccode\":1431241137,\"cid\":6468,\"display\":\"晋中市\",\"region\":[{\"display\":\"榆社县\",\"rcode\":14312411371000,\"rid\":2616},{\"display\":\"左权县\",\"rcode\":14312411371001,\"rid\":2617},{\"display\":\"和顺县\",\"rcode\":14312411371002,\"rid\":2618},{\"display\":\"昔阳县\",\"rcode\":14312411371003,\"rid\":2619},{\"display\":\"寿阳县\",\"rcode\":14312411371004,\"rid\":2620},{\"display\":\"太谷县\",\"rcode\":14312411371005,\"rid\":2621},{\"display\":\"祁　县\",\"rcode\":14312411371006,\"rid\":2622},{\"display\":\"平遥县\",\"rcode\":14312411371007,\"rid\":2623},{\"display\":\"灵石县\",\"rcode\":14312411371008,\"rid\":2624},{\"display\":\"介休市\",\"rcode\":14312411371009,\"rid\":2625},{\"display\":\"榆次区\",\"rcode\":14312411371010,\"rid\":2626}]},{\"ccode\":1431241160,\"cid\":6469,\"display\":\"临汾市\",\"region\":[{\"display\":\"曲沃县\",\"rcode\":14312411601000,\"rid\":2627},{\"display\":\"翼城县\",\"rcode\":14312411601001,\"rid\":2628},{\"display\":\"襄汾县\",\"rcode\":14312411601002,\"rid\":2629},{\"display\":\"洪洞县\",\"rcode\":14312411601003,\"rid\":2630},{\"display\":\"古　县\",\"rcode\":14312411601004,\"rid\":2631},{\"display\":\"安泽县\",\"rcode\":14312411601005,\"rid\":2632},{\"display\":\"浮山县\",\"rcode\":14312411601006,\"rid\":2633},{\"display\":\"吉　县\",\"rcode\":14312411601007,\"rid\":2634},{\"display\":\"乡宁县\",\"rcode\":14312411601008,\"rid\":2635},{\"display\":\"大宁县\",\"rcode\":14312411601009,\"rid\":2636},{\"display\":\"隰　县\",\"rcode\":14312411601010,\"rid\":2637},{\"display\":\"永和县\",\"rcode\":14312411601011,\"rid\":2638},{\"display\":\"蒲　县\",\"rcode\":14312411601012,\"rid\":2639},{\"display\":\"汾西县\",\"rcode\":14312411601013,\"rid\":2640},{\"display\":\"侯马市\",\"rcode\":14312411601014,\"rid\":2641},{\"display\":\"霍州市\",\"rcode\":14312411601015,\"rid\":2642},{\"display\":\"尧都区\",\"rcode\":14312411601016,\"rid\":2643}]},{\"ccode\":1431241175,\"cid\":6470,\"display\":\"吕梁市\",\"region\":[{\"display\":\"文水县\",\"rcode\":14312411751000,\"rid\":2644},{\"display\":\"交城县\",\"rcode\":14312411751001,\"rid\":2645},{\"display\":\"兴　县\",\"rcode\":14312411751002,\"rid\":2646},{\"display\":\"临　县\",\"rcode\":14312411751003,\"rid\":2647},{\"display\":\"柳林县\",\"rcode\":14312411751004,\"rid\":2648},{\"display\":\"石楼县\",\"rcode\":14312411751005,\"rid\":2649},{\"display\":\"岚　县\",\"rcode\":14312411751006,\"rid\":2650},{\"display\":\"方山县\",\"rcode\":14312411751007,\"rid\":2651},{\"display\":\"中阳县\",\"rcode\":14312411751008,\"rid\":2652},{\"display\":\"交口县\",\"rcode\":14312411751009,\"rid\":2653},{\"display\":\"孝义市\",\"rcode\":14312411751010,\"rid\":2654},{\"display\":\"汾阳市\",\"rcode\":14312411751011,\"rid\":2655},{\"display\":\"离石区\",\"rcode\":14312411751012,\"rid\":2656}]},{\"ccode\":1431241239,\"cid\":6471,\"display\":\"朔州市\",\"region\":[{\"display\":\"平鲁区\",\"rcode\":14312412391000,\"rid\":2657},{\"display\":\"山阴县\",\"rcode\":14312412391001,\"rid\":2658},{\"display\":\"应　县\",\"rcode\":14312412391002,\"rid\":2659},{\"display\":\"右玉县\",\"rcode\":14312412391003,\"rid\":2660},{\"display\":\"怀仁县\",\"rcode\":14312412391004,\"rid\":2661},{\"display\":\"朔城区\",\"rcode\":14312412391005,\"rid\":2662}]},{\"ccode\":1431241251,\"cid\":6472,\"display\":\"太原市\",\"region\":[{\"display\":\"迎泽区\",\"rcode\":14312412511000,\"rid\":2663},{\"display\":\"杏花岭区\",\"rcode\":14312412511001,\"rid\":2664},{\"display\":\"尖草坪区\",\"rcode\":14312412511002,\"rid\":2665},{\"display\":\"万柏林区\",\"rcode\":14312412511003,\"rid\":2666},{\"display\":\"晋源区\",\"rcode\":14312412511004,\"rid\":2667},{\"display\":\"清徐县\",\"rcode\":14312412511005,\"rid\":2668},{\"display\":\"阳曲县\",\"rcode\":14312412511006,\"rid\":2669},{\"display\":\"娄烦县\",\"rcode\":14312412511007,\"rid\":2670},{\"display\":\"古交市\",\"rcode\":14312412511008,\"rid\":2671},{\"display\":\"小店区\",\"rcode\":14312412511009,\"rid\":2672}]},{\"ccode\":1431241294,\"cid\":6473,\"display\":\"忻州市\",\"region\":[{\"display\":\"定襄县\",\"rcode\":14312412941000,\"rid\":2673},{\"display\":\"五台县\",\"rcode\":14312412941001,\"rid\":2674},{\"display\":\"代　县\",\"rcode\":14312412941002,\"rid\":2675},{\"display\":\"繁峙县\",\"rcode\":14312412941003,\"rid\":2676},{\"display\":\"宁武县\",\"rcode\":14312412941004,\"rid\":2677},{\"display\":\"静乐县\",\"rcode\":14312412941005,\"rid\":2678},{\"display\":\"神池县\",\"rcode\":14312412941006,\"rid\":2679},{\"display\":\"五寨县\",\"rcode\":14312412941007,\"rid\":2680},{\"display\":\"岢岚县\",\"rcode\":14312412941008,\"rid\":2681},{\"display\":\"河曲县\",\"rcode\":14312412941009,\"rid\":2682},{\"display\":\"保德县\",\"rcode\":14312412941010,\"rid\":2683},{\"display\":\"偏关县\",\"rcode\":14312412941011,\"rid\":2684},{\"display\":\"原平市\",\"rcode\":14312412941012,\"rid\":2685},{\"display\":\"忻府区\",\"rcode\":14312412941013,\"rid\":2686}]},{\"ccode\":1431241304,\"cid\":6474,\"display\":\"阳泉市\",\"region\":[{\"display\":\"矿　区\",\"rcode\":14312413041000,\"rid\":2687},{\"display\":\"郊　区\",\"rcode\":14312413041001,\"rid\":2688},{\"display\":\"平定县\",\"rcode\":14312413041002,\"rid\":2689},{\"display\":\"盂　县\",\"rcode\":14312413041003,\"rid\":2690},{\"display\":\"城　区\",\"rcode\":14312413041004,\"rid\":2691}]},{\"ccode\":1431241321,\"cid\":6475,\"display\":\"运城市\",\"region\":[{\"display\":\"临猗县\",\"rcode\":14312413211000,\"rid\":2692},{\"display\":\"万荣县\",\"rcode\":14312413211001,\"rid\":2693},{\"display\":\"闻喜县\",\"rcode\":14312413211002,\"rid\":2694},{\"display\":\"稷山县\",\"rcode\":14312413211003,\"rid\":2695},{\"display\":\"新绛县\",\"rcode\":14312413211004,\"rid\":2696},{\"display\":\"绛　县\",\"rcode\":14312413211005,\"rid\":2697},{\"display\":\"垣曲县\",\"rcode\":14312413211006,\"rid\":2698},{\"display\":\"夏　县\",\"rcode\":14312413211007,\"rid\":2699},{\"display\":\"平陆县\",\"rcode\":14312413211008,\"rid\":2700},{\"display\":\"芮城县\",\"rcode\":14312413211009,\"rid\":2701},{\"display\":\"永济市\",\"rcode\":14312413211010,\"rid\":2702},{\"display\":\"河津市\",\"rcode\":14312413211011,\"rid\":2703},{\"display\":\"盐湖区\",\"rcode\":14312413211012,\"rid\":2704}]}],\"display\":\"山西\",\"pcode\":143124,\"pid\":706},{\"city\":[{\"ccode\":1431251000,\"cid\":6476,\"display\":\"阿坝藏族羌族自治州\",\"region\":[{\"display\":\"理　县\",\"rcode\":14312510001000,\"rid\":2886},{\"display\":\"茂　县\",\"rcode\":14312510001001,\"rid\":2887},{\"display\":\"松潘县\",\"rcode\":14312510001002,\"rid\":2888},{\"display\":\"九寨沟县\",\"rcode\":14312510001003,\"rid\":2889},{\"display\":\"金川县\",\"rcode\":14312510001004,\"rid\":2890},{\"display\":\"小金县\",\"rcode\":14312510001005,\"rid\":2891},{\"display\":\"黑水县\",\"rcode\":14312510001006,\"rid\":2892},{\"display\":\"马尔康县\",\"rcode\":14312510001007,\"rid\":2893},{\"display\":\"壤塘县\",\"rcode\":14312510001008,\"rid\":2894},{\"display\":\"阿坝县\",\"rcode\":14312510001009,\"rid\":2895},{\"display\":\"若尔盖县\",\"rcode\":14312510001010,\"rid\":2896},{\"display\":\"红原县\",\"rcode\":14312510001011,\"rid\":2897},{\"display\":\"汶川县\",\"rcode\":14312510001012,\"rid\":2898}]},{\"ccode\":1431251021,\"cid\":6477,\"display\":\"巴中市\",\"region\":[{\"display\":\"通江县\",\"rcode\":14312510211000,\"rid\":2718},{\"display\":\"南江县\",\"rcode\":14312510211001,\"rid\":2719},{\"display\":\"平昌县\",\"rcode\":14312510211002,\"rid\":2720},{\"display\":\"巴州区\",\"rcode\":14312510211003,\"rid\":2721}]},{\"ccode\":1431251042,\"cid\":6478,\"display\":\"成都市\",\"region\":[{\"display\":\"青羊区\",\"rcode\":14312510421000,\"rid\":2722},{\"display\":\"金牛区\",\"rcode\":14312510421001,\"rid\":2723},{\"display\":\"武侯区\",\"rcode\":14312510421002,\"rid\":2724},{\"display\":\"成华区\",\"rcode\":14312510421003,\"rid\":2725},{\"display\":\"龙泉驿区\",\"rcode\":14312510421004,\"rid\":2726},{\"display\":\"青白江区\",\"rcode\":14312510421005,\"rid\":2727},{\"display\":\"新都区\",\"rcode\":14312510421006,\"rid\":2728},{\"display\":\"温江区\",\"rcode\":14312510421007,\"rid\":2729},{\"display\":\"金堂县\",\"rcode\":14312510421008,\"rid\":2730},{\"display\":\"双流县\",\"rcode\":14312510421009,\"rid\":2731},{\"display\":\"郫　县\",\"rcode\":14312510421010,\"rid\":2732},{\"display\":\"大邑县\",\"rcode\":14312510421011,\"rid\":2733},{\"display\":\"蒲江县\",\"rcode\":14312510421012,\"rid\":2734},{\"display\":\"新津县\",\"rcode\":14312510421013,\"rid\":2735},{\"display\":\"都江堰市\",\"rcode\":14312510421014,\"rid\":2736},{\"display\":\"彭州市\",\"rcode\":14312510421015,\"rid\":2737},{\"display\":\"邛崃市\",\"rcode\":14312510421016,\"rid\":2738},{\"display\":\"崇州市\",\"rcode\":14312510421017,\"rid\":2739},{\"display\":\"锦江区\",\"rcode\":14312510421018,\"rid\":2740}]},{\"ccode\":1431251056,\"cid\":6479,\"display\":\"达州市\",\"region\":[{\"display\":\"达　县\",\"rcode\":14312510561000,\"rid\":2741},{\"display\":\"宣汉县\",\"rcode\":14312510561001,\"rid\":2742},{\"display\":\"开江县\",\"rcode\":14312510561002,\"rid\":2743},{\"display\":\"大竹县\",\"rcode\":14312510561003,\"rid\":2744},{\"display\":\"渠　县\",\"rcode\":14312510561004,\"rid\":2745},{\"display\":\"万源市\",\"rcode\":14312510561005,\"rid\":2746},{\"display\":\"通川区\",\"rcode\":14312510561006,\"rid\":2747}]},{\"ccode\":1431251058,\"cid\":6480,\"display\":\"德阳市\",\"region\":[{\"display\":\"中江县\",\"rcode\":14312510581000,\"rid\":2748},{\"display\":\"罗江县\",\"rcode\":14312510581001,\"rid\":2749},{\"display\":\"广汉市\",\"rcode\":14312510581002,\"rid\":2750},{\"display\":\"什邡市\",\"rcode\":14312510581003,\"rid\":2751},{\"display\":\"绵竹市\",\"rcode\":14312510581004,\"rid\":2752},{\"display\":\"旌阳区\",\"rcode\":14312510581005,\"rid\":2753}]},{\"ccode\":1431251076,\"cid\":6481,\"display\":\"甘孜藏族自治州\",\"region\":[{\"display\":\"泸定县\",\"rcode\":14312510761000,\"rid\":2754},{\"display\":\"丹巴县\",\"rcode\":14312510761001,\"rid\":2755},{\"display\":\"九龙县\",\"rcode\":14312510761002,\"rid\":2756},{\"display\":\"雅江县\",\"rcode\":14312510761003,\"rid\":2757},{\"display\":\"道孚县\",\"rcode\":14312510761004,\"rid\":2758},{\"display\":\"炉霍县\",\"rcode\":14312510761005,\"rid\":2759},{\"display\":\"甘孜县\",\"rcode\":14312510761006,\"rid\":2760},{\"display\":\"新龙县\",\"rcode\":14312510761007,\"rid\":2761},{\"display\":\"德格县\",\"rcode\":14312510761008,\"rid\":2762},{\"display\":\"白玉县\",\"rcode\":14312510761009,\"rid\":2763},{\"display\":\"石渠县\",\"rcode\":14312510761010,\"rid\":2764},{\"display\":\"色达县\",\"rcode\":14312510761011,\"rid\":2765},{\"display\":\"理塘县\",\"rcode\":14312510761012,\"rid\":2766},{\"display\":\"巴塘县\",\"rcode\":14312510761013,\"rid\":2767},{\"display\":\"乡城县\",\"rcode\":14312510761014,\"rid\":2768},{\"display\":\"稻城县\",\"rcode\":14312510761015,\"rid\":2769},{\"display\":\"得荣县\",\"rcode\":14312510761016,\"rid\":2770},{\"display\":\"康定县\",\"rcode\":14312510761017,\"rid\":2771}]},{\"ccode\":1431251077,\"cid\":6482,\"display\":\"广安市\",\"region\":[{\"display\":\"岳池县\",\"rcode\":14312510771000,\"rid\":2772},{\"display\":\"武胜县\",\"rcode\":14312510771001,\"rid\":2773},{\"display\":\"邻水县\",\"rcode\":14312510771002,\"rid\":2774},{\"display\":\"华蓥市\",\"rcode\":14312510771003,\"rid\":2775},{\"display\":\"广安区\",\"rcode\":14312510771004,\"rid\":2776}]},{\"ccode\":1431251078,\"cid\":6483,\"display\":\"广元市\",\"region\":[{\"display\":\"元坝区\",\"rcode\":14312510781000,\"rid\":2777},{\"display\":\"朝天区\",\"rcode\":14312510781001,\"rid\":2778},{\"display\":\"旺苍县\",\"rcode\":14312510781002,\"rid\":2779},{\"display\":\"青川县\",\"rcode\":14312510781003,\"rid\":2780},{\"display\":\"剑阁县\",\"rcode\":14312510781004,\"rid\":2781},{\"display\":\"苍溪县\",\"rcode\":14312510781005,\"rid\":2782},{\"display\":\"市中区\",\"rcode\":14312510781006,\"rid\":2783}]},{\"ccode\":1431251152,\"cid\":6484,\"display\":\"乐山市\",\"region\":[{\"display\":\"沙湾区\",\"rcode\":14312511521000,\"rid\":2784},{\"display\":\"五通桥区\",\"rcode\":14312511521001,\"rid\":2785},{\"display\":\"金口河区\",\"rcode\":14312511521002,\"rid\":2786},{\"display\":\"犍为县\",\"rcode\":14312511521003,\"rid\":2787},{\"display\":\"井研县\",\"rcode\":14312511521004,\"rid\":2788},{\"display\":\"夹江县\",\"rcode\":14312511521005,\"rid\":2789},{\"display\":\"沐川县\",\"rcode\":14312511521006,\"rid\":2790},{\"display\":\"峨边彝族自治县\",\"rcode\":14312511521007,\"rid\":2791},{\"display\":\"马边彝族自治县\",\"rcode\":14312511521008,\"rid\":2792},{\"display\":\"峨眉山市\",\"rcode\":14312511521009,\"rid\":2793},{\"display\":\"市中区\",\"rcode\":14312511521010,\"rid\":2794}]},{\"ccode\":1431251153,\"cid\":6485,\"display\":\"凉山彝族自治州\",\"region\":[{\"display\":\"木里藏族自治县\",\"rcode\":14312511531000,\"rid\":2795},{\"display\":\"盐源县\",\"rcode\":14312511531001,\"rid\":2796},{\"display\":\"德昌县\",\"rcode\":14312511531002,\"rid\":2797},{\"display\":\"会理县\",\"rcode\":14312511531003,\"rid\":2798},{\"display\":\"会东县\",\"rcode\":14312511531004,\"rid\":2799},{\"display\":\"宁南县\",\"rcode\":14312511531005,\"rid\":2800},{\"display\":\"普格县\",\"rcode\":14312511531006,\"rid\":2801},{\"display\":\"布拖县\",\"rcode\":14312511531007,\"rid\":2802},{\"display\":\"金阳县\",\"rcode\":14312511531008,\"rid\":2803},{\"display\":\"昭觉县\",\"rcode\":14312511531009,\"rid\":2804},{\"display\":\"喜德县\",\"rcode\":14312511531010,\"rid\":2805},{\"display\":\"冕宁县\",\"rcode\":14312511531011,\"rid\":2806},{\"display\":\"越西县\",\"rcode\":14312511531012,\"rid\":2807},{\"display\":\"甘洛县\",\"rcode\":14312511531013,\"rid\":2808},{\"display\":\"美姑县\",\"rcode\":14312511531014,\"rid\":2809},{\"display\":\"雷波县\",\"rcode\":14312511531015,\"rid\":2810},{\"display\":\"西昌市\",\"rcode\":14312511531016,\"rid\":2811}]},{\"ccode\":1431251174,\"cid\":6486,\"display\":\"泸州市\",\"region\":[{\"display\":\"纳溪区\",\"rcode\":14312511741000,\"rid\":2812},{\"display\":\"龙马潭区\",\"rcode\":14312511741001,\"rid\":2813},{\"display\":\"泸　县\",\"rcode\":14312511741002,\"rid\":2814},{\"display\":\"合江县\",\"rcode\":14312511741003,\"rid\":2815},{\"display\":\"叙永县\",\"rcode\":14312511741004,\"rid\":2816},{\"display\":\"古蔺县\",\"rcode\":14312511741005,\"rid\":2817},{\"display\":\"江阳区\",\"rcode\":14312511741006,\"rid\":2818}]},{\"ccode\":1431251178,\"cid\":6487,\"display\":\"眉山市\",\"region\":[{\"display\":\"仁寿县\",\"rcode\":14312511781000,\"rid\":2819},{\"display\":\"彭山县\",\"rcode\":14312511781001,\"rid\":2820},{\"display\":\"洪雅县\",\"rcode\":14312511781002,\"rid\":2821},{\"display\":\"丹棱县\",\"rcode\":14312511781003,\"rid\":2822},{\"display\":\"青神县\",\"rcode\":14312511781004,\"rid\":2823},{\"display\":\"东坡区\",\"rcode\":14312511781005,\"rid\":2824}]},{\"ccode\":1431251180,\"cid\":6488,\"display\":\"绵阳市\",\"region\":[{\"display\":\"游仙区\",\"rcode\":14312511801000,\"rid\":2825},{\"display\":\"三台县\",\"rcode\":14312511801001,\"rid\":2826},{\"display\":\"盐亭县\",\"rcode\":14312511801002,\"rid\":2827},{\"display\":\"安　县\",\"rcode\":14312511801003,\"rid\":2828},{\"display\":\"梓潼县\",\"rcode\":14312511801004,\"rid\":2829},{\"display\":\"北川羌族自治县\",\"rcode\":14312511801005,\"rid\":2830},{\"display\":\"平武县\",\"rcode\":14312511801006,\"rid\":2831},{\"display\":\"江油市\",\"rcode\":14312511801007,\"rid\":2832},{\"display\":\"涪城区\",\"rcode\":14312511801008,\"rid\":2833}]},{\"ccode\":1431251183,\"cid\":6489,\"display\":\"南充市\",\"region\":[{\"display\":\"高坪区\",\"rcode\":14312511831000,\"rid\":2834},{\"display\":\"嘉陵区\",\"rcode\":14312511831001,\"rid\":2835},{\"display\":\"南部县\",\"rcode\":14312511831002,\"rid\":2836},{\"display\":\"营山县\",\"rcode\":14312511831003,\"rid\":2837},{\"display\":\"蓬安县\",\"rcode\":14312511831004,\"rid\":2838},{\"display\":\"仪陇县\",\"rcode\":14312511831005,\"rid\":2839},{\"display\":\"西充县\",\"rcode\":14312511831006,\"rid\":2840},{\"display\":\"阆中市\",\"rcode\":14312511831007,\"rid\":2841},{\"display\":\"顺庆区\",\"rcode\":14312511831008,\"rid\":2842}]},{\"ccode\":1431251191,\"cid\":6490,\"display\":\"内江市\",\"region\":[{\"display\":\"东兴区\",\"rcode\":14312511911000,\"rid\":2843},{\"display\":\"威远县\",\"rcode\":14312511911001,\"rid\":2844},{\"display\":\"资中县\",\"rcode\":14312511911002,\"rid\":2845},{\"display\":\"隆昌县\",\"rcode\":14312511911003,\"rid\":2846},{\"display\":\"市中区\",\"rcode\":14312511911004,\"rid\":2847}]},{\"ccode\":1431251196,\"cid\":6491,\"display\":\"攀枝花市\",\"region\":[{\"display\":\"西　区\",\"rcode\":14312511961000,\"rid\":2848},{\"display\":\"仁和区\",\"rcode\":14312511961001,\"rid\":2849},{\"display\":\"米易县\",\"rcode\":14312511961002,\"rid\":2850},{\"display\":\"盐边县\",\"rcode\":14312511961003,\"rid\":2851},{\"display\":\"东　区\",\"rcode\":14312511961004,\"rid\":2852}]},{\"ccode\":1431251244,\"cid\":6492,\"display\":\"遂宁市\",\"region\":[{\"display\":\"安居区\",\"rcode\":14312512441000,\"rid\":2853},{\"display\":\"蓬溪县\",\"rcode\":14312512441001,\"rid\":2854},{\"display\":\"射洪县\",\"rcode\":14312512441002,\"rid\":2855},{\"display\":\"大英县\",\"rcode\":14312512441003,\"rid\":2856},{\"display\":\"船山区\",\"rcode\":14312512441004,\"rid\":2857}]},{\"ccode\":1431251299,\"cid\":6493,\"display\":\"雅安市\",\"region\":[{\"display\":\"名山县\",\"rcode\":14312512991000,\"rid\":2858},{\"display\":\"荥经县\",\"rcode\":14312512991001,\"rid\":2859},{\"display\":\"汉源县\",\"rcode\":14312512991002,\"rid\":2860},{\"display\":\"石棉县\",\"rcode\":14312512991003,\"rid\":2861},{\"display\":\"天全县\",\"rcode\":14312512991004,\"rid\":2862},{\"display\":\"芦山县\",\"rcode\":14312512991005,\"rid\":2863},{\"display\":\"宝兴县\",\"rcode\":14312512991006,\"rid\":2864},{\"display\":\"雨城区\",\"rcode\":14312512991007,\"rid\":2865}]},{\"ccode\":1431251307,\"cid\":6494,\"display\":\"宜宾市\",\"region\":[{\"display\":\"宜宾县\",\"rcode\":14312513071000,\"rid\":2866},{\"display\":\"南溪县\",\"rcode\":14312513071001,\"rid\":2867},{\"display\":\"江安县\",\"rcode\":14312513071002,\"rid\":2868},{\"display\":\"长宁县\",\"rcode\":14312513071003,\"rid\":2869},{\"display\":\"高　县\",\"rcode\":14312513071004,\"rid\":2870},{\"display\":\"珙　县\",\"rcode\":14312513071005,\"rid\":2871},{\"display\":\"筠连县\",\"rcode\":14312513071006,\"rid\":2872},{\"display\":\"兴文县\",\"rcode\":14312513071007,\"rid\":2873},{\"display\":\"屏山县\",\"rcode\":14312513071008,\"rid\":2874},{\"display\":\"翠屏区\",\"rcode\":14312513071009,\"rid\":2875}]},{\"ccode\":1431251342,\"cid\":6495,\"display\":\"自贡市\",\"region\":[{\"display\":\"贡井区\",\"rcode\":14312513421000,\"rid\":2876},{\"display\":\"大安区\",\"rcode\":14312513421001,\"rid\":2877},{\"display\":\"沿滩区\",\"rcode\":14312513421002,\"rid\":2878},{\"display\":\"荣　县\",\"rcode\":14312513421003,\"rid\":2879},{\"display\":\"富顺县\",\"rcode\":14312513421004,\"rid\":2880},{\"display\":\"自流井区\",\"rcode\":14312513421005,\"rid\":2881}]},{\"ccode\":1431251343,\"cid\":6496,\"display\":\"资阳市\",\"region\":[{\"display\":\"安岳县\",\"rcode\":14312513431000,\"rid\":2882},{\"display\":\"乐至县\",\"rcode\":14312513431001,\"rid\":2883},{\"display\":\"简阳市\",\"rcode\":14312513431002,\"rid\":2884},{\"display\":\"雁江区\",\"rcode\":14312513431003,\"rid\":2885}]}],\"display\":\"四川\",\"pcode\":143125,\"pid\":707},{\"city\":[{\"ccode\":1431261255,\"cid\":6497,\"display\":\"天津市\",\"region\":[{\"display\":\"北辰\",\"rcode\":14312612551000,\"rid\":2922},{\"display\":\"东丽\",\"rcode\":14312612551002,\"rid\":2924},{\"display\":\"河北\",\"rcode\":14312612551004,\"rid\":2926},{\"display\":\"河东\",\"rcode\":14312612551005,\"rid\":2927},{\"display\":\"和平\",\"rcode\":14312612551006,\"rid\":2928},{\"display\":\"河西\",\"rcode\":14312612551007,\"rid\":2929},{\"display\":\"红桥\",\"rcode\":14312612551008,\"rid\":2930},{\"display\":\"静海\",\"rcode\":14312612551009,\"rid\":2931},{\"display\":\"津南\",\"rcode\":14312612551010,\"rid\":2932},{\"display\":\"蓟县\",\"rcode\":14312612551011,\"rid\":2933},{\"display\":\"南开\",\"rcode\":14312612551012,\"rid\":2934},{\"display\":\"宁河\",\"rcode\":14312612551013,\"rid\":2935},{\"display\":\"武清\",\"rcode\":14312612551015,\"rid\":2937},{\"display\":\"西青\",\"rcode\":14312612551016,\"rid\":2938},{\"display\":\"宝坻\",\"rcode\":14312612551017,\"rid\":2939},{\"display\":\"滨海新区\",\"rcode\":14312612551018,\"rid\":2940}]}],\"display\":\"天津\",\"pcode\":143126,\"pid\":708},{\"city\":[{\"ccode\":1431271001,\"cid\":6499,\"display\":\"阿克苏地区\",\"region\":[{\"display\":\"温宿县\",\"rcode\":14312710011000,\"rid\":3099},{\"display\":\"库车县\",\"rcode\":14312710011001,\"rid\":3100},{\"display\":\"沙雅县\",\"rcode\":14312710011002,\"rid\":3101},{\"display\":\"新和县\",\"rcode\":14312710011003,\"rid\":3102},{\"display\":\"拜城县\",\"rcode\":14312710011004,\"rid\":3103},{\"display\":\"乌什县\",\"rcode\":14312710011005,\"rid\":3104},{\"display\":\"阿瓦提县\",\"rcode\":14312710011006,\"rid\":3105},{\"display\":\"柯坪县\",\"rcode\":14312710011007,\"rid\":3106},{\"display\":\"阿克苏市\",\"rcode\":14312710011008,\"rid\":3107}]},{\"ccode\":1431271003,\"cid\":6500,\"display\":\"阿勒泰地区\",\"region\":[{\"display\":\"布尔津县\",\"rcode\":14312710031000,\"rid\":3014},{\"display\":\"富蕴县\",\"rcode\":14312710031001,\"rid\":3015},{\"display\":\"福海县\",\"rcode\":14312710031002,\"rid\":3016},{\"display\":\"哈巴河县\",\"rcode\":14312710031003,\"rid\":3017},{\"display\":\"青河县\",\"rcode\":14312710031004,\"rid\":3018},{\"display\":\"吉木乃县\",\"rcode\":14312710031005,\"rid\":3019},{\"display\":\"阿勒泰市\",\"rcode\":14312710031006,\"rid\":3020}]},{\"ccode\":1431271314,\"cid\":26692,\"display\":\"石河子市\",\"region\":[]},{\"ccode\":1431271315,\"cid\":26693,\"display\":\"阿拉尔市\",\"region\":[]},{\"ccode\":1431271316,\"cid\":26694,\"display\":\"图木舒克市\",\"region\":[]},{\"ccode\":1431271317,\"cid\":26695,\"display\":\"五家渠市\",\"region\":[]},{\"ccode\":1431271020,\"cid\":6501,\"display\":\"巴音郭楞蒙古自治州\",\"region\":[{\"display\":\"轮台县\",\"rcode\":14312710201000,\"rid\":3021},{\"display\":\"尉犁县\",\"rcode\":14312710201001,\"rid\":3022},{\"display\":\"若羌县\",\"rcode\":14312710201002,\"rid\":3023},{\"display\":\"且末县\",\"rcode\":14312710201003,\"rid\":3024},{\"display\":\"焉耆回族自治县\",\"rcode\":14312710201004,\"rid\":3025},{\"display\":\"和静县\",\"rcode\":14312710201005,\"rid\":3026},{\"display\":\"和硕县\",\"rcode\":14312710201006,\"rid\":3027},{\"display\":\"博湖县\",\"rcode\":14312710201007,\"rid\":3028},{\"display\":\"库尔勒市\",\"rcode\":14312710201008,\"rid\":3029}]},{\"ccode\":1431271028,\"cid\":6502,\"display\":\"博尔塔拉蒙古自治州\",\"region\":[{\"display\":\"精河县\",\"rcode\":14312710281000,\"rid\":3030},{\"display\":\"温泉县\",\"rcode\":14312710281001,\"rid\":3031},{\"display\":\"博乐市\",\"rcode\":14312710281002,\"rid\":3032}]},{\"ccode\":1431271034,\"cid\":6503,\"display\":\"昌吉回族自治州\",\"region\":[{\"display\":\"阜康市\",\"rcode\":14312710341000,\"rid\":3033},{\"display\":\"呼图壁县\",\"rcode\":14312710341001,\"rid\":3034},{\"display\":\"玛纳斯县\",\"rcode\":14312710341002,\"rid\":3035},{\"display\":\"奇台县\",\"rcode\":14312710341003,\"rid\":3036},{\"display\":\"吉木萨尔县\",\"rcode\":14312710341004,\"rid\":3037},{\"display\":\"木垒哈萨克自治县\",\"rcode\":14312710341005,\"rid\":3038},{\"display\":\"昌吉市\",\"rcode\":14312710341006,\"rid\":3039}]},{\"ccode\":1431271092,\"cid\":6504,\"display\":\"哈密地区\",\"region\":[{\"display\":\"巴里坤哈萨克自治县\",\"rcode\":14312710921000,\"rid\":3040},{\"display\":\"伊吾县\",\"rcode\":14312710921001,\"rid\":3041},{\"display\":\"哈密市\",\"rcode\":14312710921002,\"rid\":3042}]},{\"ccode\":1431271103,\"cid\":6505,\"display\":\"和田地区\",\"region\":[{\"display\":\"和田县\",\"rcode\":14312711031000,\"rid\":3043},{\"display\":\"墨玉县\",\"rcode\":14312711031001,\"rid\":3044},{\"display\":\"皮山县\",\"rcode\":14312711031002,\"rid\":3045},{\"display\":\"洛浦县\",\"rcode\":14312711031003,\"rid\":3046},{\"display\":\"策勒县\",\"rcode\":14312711031004,\"rid\":3047},{\"display\":\"于田县\",\"rcode\":14312711031005,\"rid\":3048},{\"display\":\"民丰县\",\"rcode\":14312711031006,\"rid\":3049},{\"display\":\"和田市\",\"rcode\":14312711031007,\"rid\":3050}]},{\"ccode\":1431271144,\"cid\":6506,\"display\":\"喀什地区\",\"region\":[{\"display\":\"疏附县\",\"rcode\":14312711441000,\"rid\":3065},{\"display\":\"疏勒县\",\"rcode\":14312711441001,\"rid\":3066},{\"display\":\"英吉沙县\",\"rcode\":14312711441002,\"rid\":3067},{\"display\":\"泽普县\",\"rcode\":14312711441003,\"rid\":3068},{\"display\":\"莎车县\",\"rcode\":14312711441004,\"rid\":3069},{\"display\":\"叶城县\",\"rcode\":14312711441005,\"rid\":3070},{\"display\":\"麦盖提县\",\"rcode\":14312711441006,\"rid\":3071},{\"display\":\"岳普湖县\",\"rcode\":14312711441007,\"rid\":3072},{\"display\":\"伽师县\",\"rcode\":14312711441008,\"rid\":3073},{\"display\":\"巴楚县\",\"rcode\":14312711441009,\"rid\":3074},{\"display\":\"塔什库尔干塔吉克自治县\",\"rcode\":14312711441010,\"rid\":3075},{\"display\":\"喀什市\",\"rcode\":14312711441011,\"rid\":3076}]},{\"ccode\":1431271145,\"cid\":6507,\"display\":\"克拉玛依市\",\"region\":[{\"display\":\"克拉玛依区\",\"rcode\":14312711451000,\"rid\":3061},{\"display\":\"白碱滩区\",\"rcode\":14312711451001,\"rid\":3062},{\"display\":\"乌尔禾区\",\"rcode\":14312711451002,\"rid\":3063},{\"display\":\"独山子区\",\"rcode\":14312711451003,\"rid\":3064}]},{\"ccode\":1431271146,\"cid\":6508,\"display\":\"克孜勒苏柯尔克孜自治州\",\"region\":[{\"display\":\"阿克陶县\",\"rcode\":14312711461000,\"rid\":3077},{\"display\":\"阿合奇县\",\"rcode\":14312711461001,\"rid\":3078},{\"display\":\"乌恰县\",\"rcode\":14312711461002,\"rid\":3079},{\"display\":\"阿图什市\",\"rcode\":14312711461003,\"rid\":3080}]},{\"ccode\":1431271249,\"cid\":6510,\"display\":\"塔城地区\",\"region\":[{\"display\":\"乌苏市\",\"rcode\":14312712491000,\"rid\":3081},{\"display\":\"额敏县\",\"rcode\":14312712491001,\"rid\":3082},{\"display\":\"沙湾县\",\"rcode\":14312712491002,\"rid\":3083},{\"display\":\"托里县\",\"rcode\":14312712491003,\"rid\":3084},{\"display\":\"裕民县\",\"rcode\":14312712491004,\"rid\":3085},{\"display\":\"和布克赛尔蒙古自治县\",\"rcode\":14312712491005,\"rid\":3086},{\"display\":\"塔城市\",\"rcode\":14312712491006,\"rid\":3087}]},{\"ccode\":1431271264,\"cid\":6511,\"display\":\"吐鲁番地区\",\"region\":[{\"display\":\"鄯善县\",\"rcode\":14312712641000,\"rid\":3088},{\"display\":\"托克逊县\",\"rcode\":14312712641001,\"rid\":3089},{\"display\":\"吐鲁番市\",\"rcode\":14312712641002,\"rid\":3090}]},{\"ccode\":1431271274,\"cid\":6512,\"display\":\"乌鲁木齐市\",\"region\":[{\"display\":\"沙依巴克区\",\"rcode\":14312712741000,\"rid\":3091},{\"display\":\"新市区\",\"rcode\":14312712741001,\"rid\":3092},{\"display\":\"水磨沟区\",\"rcode\":14312712741002,\"rid\":3093},{\"display\":\"头屯河区\",\"rcode\":14312712741003,\"rid\":3094},{\"display\":\"达坂城区\",\"rcode\":14312712741004,\"rid\":3095},{\"display\":\"米东区\",\"rcode\":14312712741005,\"rid\":3096},{\"display\":\"乌鲁木齐县\",\"rcode\":14312712741006,\"rid\":3097},{\"display\":\"天山区\",\"rcode\":14312712741007,\"rid\":3098}]},{\"ccode\":1431271312,\"cid\":6514,\"display\":\"伊犁哈萨克自治州\",\"region\":[{\"display\":\"奎屯市\",\"rcode\":14312713121000,\"rid\":3051},{\"display\":\"伊宁县\",\"rcode\":14312713121001,\"rid\":3052},{\"display\":\"察布查尔锡伯自治县\",\"rcode\":14312713121002,\"rid\":3053},{\"display\":\"霍城县\",\"rcode\":14312713121003,\"rid\":3054},{\"display\":\"巩留县\",\"rcode\":14312713121004,\"rid\":3055},{\"display\":\"新源县\",\"rcode\":14312713121005,\"rid\":3056},{\"display\":\"昭苏县\",\"rcode\":14312713121006,\"rid\":3057},{\"display\":\"特克斯县\",\"rcode\":14312713121007,\"rid\":3058},{\"display\":\"尼勒克县\",\"rcode\":14312713121008,\"rid\":3059},{\"display\":\"伊宁市\",\"rcode\":14312713121009,\"rid\":3060}]}],\"display\":\"新疆\",\"pcode\":143127,\"pid\":709},{\"city\":[{\"ccode\":1431281004,\"cid\":6515,\"display\":\"阿里地区\",\"region\":[{\"display\":\"札达县\",\"rcode\":14312810041000,\"rid\":2951},{\"display\":\"噶尔县\",\"rcode\":14312810041001,\"rid\":2952},{\"display\":\"日土县\",\"rcode\":14312810041002,\"rid\":2953},{\"display\":\"革吉县\",\"rcode\":14312810041003,\"rid\":2954},{\"display\":\"改则县\",\"rcode\":14312810041004,\"rid\":2955},{\"display\":\"措勤县\",\"rcode\":14312810041005,\"rid\":2956},{\"display\":\"普兰县\",\"rcode\":14312810041006,\"rid\":2957}]},{\"ccode\":1431281033,\"cid\":6516,\"display\":\"昌都地区\",\"region\":[{\"display\":\"江达县\",\"rcode\":14312810331000,\"rid\":2965},{\"display\":\"贡觉县\",\"rcode\":14312810331001,\"rid\":2966},{\"display\":\"类乌齐县\",\"rcode\":14312810331002,\"rid\":2967},{\"display\":\"丁青县\",\"rcode\":14312810331003,\"rid\":2968},{\"display\":\"察雅县\",\"rcode\":14312810331004,\"rid\":2969},{\"display\":\"八宿县\",\"rcode\":14312810331005,\"rid\":2970},{\"display\":\"左贡县\",\"rcode\":14312810331006,\"rid\":2971},{\"display\":\"芒康县\",\"rcode\":14312810331007,\"rid\":2972},{\"display\":\"洛隆县\",\"rcode\":14312810331008,\"rid\":2973},{\"display\":\"边坝县\",\"rcode\":14312810331009,\"rid\":2974},{\"display\":\"昌都县\",\"rcode\":14312810331010,\"rid\":2975}]},{\"ccode\":1431281151,\"cid\":6517,\"display\":\"拉萨市\",\"region\":[{\"display\":\"林周县\",\"rcode\":14312811511000,\"rid\":3006},{\"display\":\"当雄县\",\"rcode\":14312811511001,\"rid\":3007},{\"display\":\"尼木县\",\"rcode\":14312811511002,\"rid\":3008},{\"display\":\"曲水县\",\"rcode\":14312811511003,\"rid\":3009},{\"display\":\"堆龙德庆县\",\"rcode\":14312811511004,\"rid\":3010},{\"display\":\"达孜县\",\"rcode\":14312811511005,\"rid\":3011},{\"display\":\"墨竹工卡县\",\"rcode\":14312811511006,\"rid\":3012},{\"display\":\"城关区\",\"rcode\":14312811511007,\"rid\":3013}]},{\"ccode\":1431281163,\"cid\":6518,\"display\":\"林芝地区\",\"region\":[{\"display\":\"工布江达县\",\"rcode\":14312811631000,\"rid\":2958},{\"display\":\"米林县\",\"rcode\":14312811631001,\"rid\":2959},{\"display\":\"墨脱县\",\"rcode\":14312811631002,\"rid\":2960},{\"display\":\"波密县\",\"rcode\":14312811631003,\"rid\":2961},{\"display\":\"察隅县\",\"rcode\":14312811631004,\"rid\":2962},{\"display\":\"朗　县\",\"rcode\":14312811631005,\"rid\":2963},{\"display\":\"林芝县\",\"rcode\":14312811631006,\"rid\":2964}]},{\"ccode\":1431281190,\"cid\":6519,\"display\":\"那曲地区\",\"region\":[{\"display\":\"嘉黎县\",\"rcode\":14312811901000,\"rid\":2941},{\"display\":\"比如县\",\"rcode\":14312811901001,\"rid\":2942},{\"display\":\"聂荣县\",\"rcode\":14312811901002,\"rid\":2943},{\"display\":\"安多县\",\"rcode\":14312811901003,\"rid\":2944},{\"display\":\"申扎县\",\"rcode\":14312811901004,\"rid\":2945},{\"display\":\"索　县\",\"rcode\":14312811901005,\"rid\":2946},{\"display\":\"班戈县\",\"rcode\":14312811901006,\"rid\":2947},{\"display\":\"巴青县\",\"rcode\":14312811901007,\"rid\":2948},{\"display\":\"尼玛县\",\"rcode\":14312811901008,\"rid\":2949},{\"display\":\"那曲县\",\"rcode\":14312811901009,\"rid\":2950}]},{\"ccode\":1431281215,\"cid\":6520,\"display\":\"日喀则地区\",\"region\":[{\"display\":\"南木林县\",\"rcode\":14312812151000,\"rid\":2988},{\"display\":\"江孜县\",\"rcode\":14312812151001,\"rid\":2989},{\"display\":\"定日县\",\"rcode\":14312812151002,\"rid\":2990},{\"display\":\"萨迦县\",\"rcode\":14312812151003,\"rid\":2991},{\"display\":\"拉孜县\",\"rcode\":14312812151004,\"rid\":2992},{\"display\":\"昂仁县\",\"rcode\":14312812151005,\"rid\":2993},{\"display\":\"谢通门县\",\"rcode\":14312812151006,\"rid\":2994},{\"display\":\"白朗县\",\"rcode\":14312812151007,\"rid\":2995},{\"display\":\"仁布县\",\"rcode\":14312812151008,\"rid\":2996},{\"display\":\"康马县\",\"rcode\":14312812151009,\"rid\":2997},{\"display\":\"定结县\",\"rcode\":14312812151010,\"rid\":2998},{\"display\":\"仲巴县\",\"rcode\":14312812151011,\"rid\":2999},{\"display\":\"亚东县\",\"rcode\":14312812151012,\"rid\":3000},{\"display\":\"吉隆县\",\"rcode\":14312812151013,\"rid\":3001},{\"display\":\"聂拉木县\",\"rcode\":14312812151014,\"rid\":3002},{\"display\":\"萨嘎县\",\"rcode\":14312812151015,\"rid\":3003},{\"display\":\"岗巴县\",\"rcode\":14312812151016,\"rid\":3004},{\"display\":\"日喀则市\",\"rcode\":14312812151017,\"rid\":3005}]},{\"ccode\":1431281225,\"cid\":6521,\"display\":\"山南地区\",\"region\":[{\"display\":\"扎囊县\",\"rcode\":14312812251000,\"rid\":2976},{\"display\":\"贡嘎县\",\"rcode\":14312812251001,\"rid\":2977},{\"display\":\"桑日县\",\"rcode\":14312812251002,\"rid\":2978},{\"display\":\"琼结县\",\"rcode\":14312812251003,\"rid\":2979},{\"display\":\"曲松县\",\"rcode\":14312812251004,\"rid\":2980},{\"display\":\"措美县\",\"rcode\":14312812251005,\"rid\":2981},{\"display\":\"洛扎县\",\"rcode\":14312812251006,\"rid\":2982},{\"display\":\"加查县\",\"rcode\":14312812251007,\"rid\":2983},{\"display\":\"隆子县\",\"rcode\":14312812251008,\"rid\":2984},{\"display\":\"错那县\",\"rcode\":14312812251009,\"rid\":2985},{\"display\":\"浪卡子县\",\"rcode\":14312812251010,\"rid\":2986},{\"display\":\"乃东县\",\"rcode\":14312812251011,\"rid\":2987}]}],\"display\":\"西藏\",\"pcode\":143128,\"pid\":710},{\"city\":[{\"ccode\":1431291334,\"cid\":26689,\"display\":\"保山市\",\"region\":[]},{\"ccode\":1431291048,\"cid\":6523,\"display\":\"楚雄彝族自治州\",\"region\":[{\"display\":\"双柏县\",\"rcode\":14312910481000,\"rid\":3108},{\"display\":\"牟定县\",\"rcode\":14312910481001,\"rid\":3109},{\"display\":\"南华县\",\"rcode\":14312910481002,\"rid\":3110},{\"display\":\"姚安县\",\"rcode\":14312910481003,\"rid\":3111},{\"display\":\"大姚县\",\"rcode\":14312910481004,\"rid\":3112},{\"display\":\"永仁县\",\"rcode\":14312910481005,\"rid\":3113},{\"display\":\"元谋县\",\"rcode\":14312910481006,\"rid\":3114},{\"display\":\"武定县\",\"rcode\":14312910481007,\"rid\":3115},{\"display\":\"禄丰县\",\"rcode\":14312910481008,\"rid\":3116},{\"display\":\"楚雄市\",\"rcode\":14312910481009,\"rid\":3117}]},{\"ccode\":1431291050,\"cid\":6524,\"display\":\"大理白族自治州\",\"region\":[{\"display\":\"漾濞彝族自治县\",\"rcode\":14312910501000,\"rid\":3118},{\"display\":\"祥云县\",\"rcode\":14312910501001,\"rid\":3119},{\"display\":\"宾川县\",\"rcode\":14312910501002,\"rid\":3120},{\"display\":\"弥渡县\",\"rcode\":14312910501003,\"rid\":3121},{\"display\":\"南涧彝族自治县\",\"rcode\":14312910501004,\"rid\":3122},{\"display\":\"巍山彝族回族自治县\",\"rcode\":14312910501005,\"rid\":3123},{\"display\":\"永平县\",\"rcode\":14312910501006,\"rid\":3124},{\"display\":\"云龙县\",\"rcode\":14312910501007,\"rid\":3125},{\"display\":\"洱源县\",\"rcode\":14312910501008,\"rid\":3126},{\"display\":\"剑川县\",\"rcode\":14312910501009,\"rid\":3127},{\"display\":\"鹤庆县\",\"rcode\":14312910501010,\"rid\":3128},{\"display\":\"大理市\",\"rcode\":14312910501011,\"rid\":3129}]},{\"ccode\":1431291057,\"cid\":6525,\"display\":\"德宏傣族景颇族自治州\",\"region\":[{\"display\":\"潞西市\",\"rcode\":14312910571000,\"rid\":3130},{\"display\":\"梁河县\",\"rcode\":14312910571001,\"rid\":3131},{\"display\":\"盈江县\",\"rcode\":14312910571002,\"rid\":3132},{\"display\":\"陇川县\",\"rcode\":14312910571003,\"rid\":3133},{\"display\":\"瑞丽市\",\"rcode\":14312910571004,\"rid\":3134}]},{\"ccode\":1431291061,\"cid\":6526,\"display\":\"迪庆藏族自治州\",\"region\":[{\"display\":\"德钦县\",\"rcode\":14312910611000,\"rid\":3135},{\"display\":\"维西傈僳族自治县\",\"rcode\":14312910611001,\"rid\":3136},{\"display\":\"香格里拉县\",\"rcode\":14312910611002,\"rid\":3137}]},{\"ccode\":1431291108,\"cid\":6527,\"display\":\"红河哈尼族彝族自治州\",\"region\":[{\"display\":\"开远市\",\"rcode\":14312911081000,\"rid\":3138},{\"display\":\"蒙自县\",\"rcode\":14312911081001,\"rid\":3139},{\"display\":\"屏边苗族自治县\",\"rcode\":14312911081002,\"rid\":3140},{\"display\":\"建水县\",\"rcode\":14312911081003,\"rid\":3141},{\"display\":\"石屏县\",\"rcode\":14312911081004,\"rid\":3142},{\"display\":\"弥勒县\",\"rcode\":14312911081005,\"rid\":3143},{\"display\":\"泸西县\",\"rcode\":14312911081006,\"rid\":3144},{\"display\":\"元阳县\",\"rcode\":14312911081007,\"rid\":3145},{\"display\":\"红河县\",\"rcode\":14312911081008,\"rid\":3146},{\"display\":\"金平苗族瑶族傣族自治县\",\"rcode\":14312911081009,\"rid\":3147},{\"display\":\"绿春县\",\"rcode\":14312911081010,\"rid\":3148},{\"display\":\"河口瑶族自治县\",\"rcode\":14312911081011,\"rid\":3149},{\"display\":\"个旧市\",\"rcode\":14312911081012,\"rid\":3150}]},{\"ccode\":1431291147,\"cid\":6528,\"display\":\"昆明市\",\"region\":[{\"display\":\"盘龙区\",\"rcode\":14312911471000,\"rid\":3151},{\"display\":\"官渡区\",\"rcode\":14312911471001,\"rid\":3152},{\"display\":\"西山区\",\"rcode\":14312911471002,\"rid\":3153},{\"display\":\"东川区\",\"rcode\":14312911471003,\"rid\":3154},{\"display\":\"呈贡县\",\"rcode\":14312911471004,\"rid\":3155},{\"display\":\"晋宁县\",\"rcode\":14312911471005,\"rid\":3156},{\"display\":\"富民县\",\"rcode\":14312911471006,\"rid\":3157},{\"display\":\"宜良县\",\"rcode\":14312911471007,\"rid\":3158},{\"display\":\"石林彝族自治县\",\"rcode\":14312911471008,\"rid\":3159},{\"display\":\"嵩明县\",\"rcode\":14312911471009,\"rid\":3160},{\"display\":\"禄劝彝族苗族自治县\",\"rcode\":14312911471010,\"rid\":3161},{\"display\":\"寻甸回族彝族自治县\",\"rcode\":14312911471011,\"rid\":3162},{\"display\":\"安宁市\",\"rcode\":14312911471012,\"rid\":3163},{\"display\":\"五华区\",\"rcode\":14312911471013,\"rid\":3164}]},{\"ccode\":1431291158,\"cid\":6529,\"display\":\"丽江市\",\"region\":[{\"display\":\"玉龙纳西族自治县\",\"rcode\":14312911581000,\"rid\":3165},{\"display\":\"永胜县\",\"rcode\":14312911581001,\"rid\":3166},{\"display\":\"华坪县\",\"rcode\":14312911581002,\"rid\":3167},{\"display\":\"宁蒗彝族自治县\",\"rcode\":14312911581003,\"rid\":3168},{\"display\":\"古城区\",\"rcode\":14312911581004,\"rid\":3169}]},{\"ccode\":1431291159,\"cid\":6530,\"display\":\"临沧市\",\"region\":[{\"display\":\"凤庆县\",\"rcode\":14312911591000,\"rid\":3170},{\"display\":\"云　县\",\"rcode\":14312911591001,\"rid\":3171},{\"display\":\"永德县\",\"rcode\":14312911591002,\"rid\":3172},{\"display\":\"镇康县\",\"rcode\":14312911591003,\"rid\":3173},{\"display\":\"双江拉祜族佤族布朗族傣族自治县\",\"rcode\":14312911591004,\"rid\":3174},{\"display\":\"耿马傣族佤族自治县\",\"rcode\":14312911591005,\"rid\":3175},{\"display\":\"沧源佤族自治县\",\"rcode\":14312911591006,\"rid\":3176},{\"display\":\"临翔区\",\"rcode\":14312911591007,\"rid\":3177}]},{\"ccode\":1431291194,\"cid\":6531,\"display\":\"怒江傈僳族自治州\",\"region\":[{\"display\":\"福贡县\",\"rcode\":14312911941000,\"rid\":3178},{\"display\":\"贡山独龙族怒族自治县\",\"rcode\":14312911941001,\"rid\":3179},{\"display\":\"兰坪白族普米族自治县\",\"rcode\":14312911941002,\"rid\":3180},{\"display\":\"泸水县\",\"rcode\":14312911941003,\"rid\":3181}]},{\"ccode\":1431291213,\"cid\":6532,\"display\":\"曲靖市\",\"region\":[{\"display\":\"马龙县\",\"rcode\":14312912131000,\"rid\":3192},{\"display\":\"陆良县\",\"rcode\":14312912131001,\"rid\":3193},{\"display\":\"师宗县\",\"rcode\":14312912131002,\"rid\":3194},{\"display\":\"罗平县\",\"rcode\":14312912131003,\"rid\":3195},{\"display\":\"富源县\",\"rcode\":14312912131004,\"rid\":3196},{\"display\":\"会泽县\",\"rcode\":14312912131005,\"rid\":3197},{\"display\":\"沾益县\",\"rcode\":14312912131006,\"rid\":3198},{\"display\":\"宣威市\",\"rcode\":14312912131007,\"rid\":3199},{\"display\":\"麒麟区\",\"rcode\":14312912131008,\"rid\":3200}]},{\"ccode\":1431291240,\"cid\":6533,\"display\":\"普洱市\",\"region\":[{\"display\":\"宁洱哈尼族彝族自治县\",\"rcode\":14312912401000,\"rid\":3182},{\"display\":\"墨江哈尼族自治县\",\"rcode\":14312912401001,\"rid\":3183},{\"display\":\"景东彝族自治县\",\"rcode\":14312912401002,\"rid\":3184},{\"display\":\"景谷傣族彝族自治县\",\"rcode\":14312912401003,\"rid\":3185},{\"display\":\"镇沅彝族哈尼族拉祜族自治县\",\"rcode\":14312912401004,\"rid\":3186},{\"display\":\"江城哈尼族彝族自治县\",\"rcode\":14312912401005,\"rid\":3187},{\"display\":\"孟连傣族拉祜族佤族自治县\",\"rcode\":14312912401006,\"rid\":3188},{\"display\":\"澜沧拉祜族自治县\",\"rcode\":14312912401007,\"rid\":3189},{\"display\":\"西盟佤族自治县\",\"rcode\":14312912401008,\"rid\":3190},{\"display\":\"思茅区\",\"rcode\":14312912401009,\"rid\":3191}]},{\"ccode\":1431291268,\"cid\":6534,\"display\":\"文山壮族苗族自治州\",\"region\":[{\"display\":\"砚山县\",\"rcode\":14312912681000,\"rid\":3201},{\"display\":\"西畴县\",\"rcode\":14312912681001,\"rid\":3202},{\"display\":\"麻栗坡县\",\"rcode\":14312912681002,\"rid\":3203},{\"display\":\"马关县\",\"rcode\":14312912681003,\"rid\":3204},{\"display\":\"丘北县\",\"rcode\":14312912681004,\"rid\":3205},{\"display\":\"广南县\",\"rcode\":14312912681005,\"rid\":3206},{\"display\":\"富宁县\",\"rcode\":14312912681006,\"rid\":3207},{\"display\":\"文山县\",\"rcode\":14312912681007,\"rid\":3208}]},{\"ccode\":1431291295,\"cid\":6535,\"display\":\"西双版纳傣族自治州\",\"region\":[{\"display\":\"勐海县\",\"rcode\":14312912951000,\"rid\":3209},{\"display\":\"勐腊县\",\"rcode\":14312912951001,\"rid\":3210},{\"display\":\"景洪市\",\"rcode\":14312912951002,\"rid\":3211}]},{\"ccode\":1431291324,\"cid\":6536,\"display\":\"玉溪市\",\"region\":[{\"display\":\"江川县\",\"rcode\":14312913241000,\"rid\":3212},{\"display\":\"澄江县\",\"rcode\":14312913241001,\"rid\":3213},{\"display\":\"通海县\",\"rcode\":14312913241002,\"rid\":3214},{\"display\":\"华宁县\",\"rcode\":14312913241003,\"rid\":3215},{\"display\":\"易门县\",\"rcode\":14312913241004,\"rid\":3216},{\"display\":\"峨山彝族自治县\",\"rcode\":14312913241005,\"rid\":3217},{\"display\":\"新平彝族傣族自治县\",\"rcode\":14312913241006,\"rid\":3218},{\"display\":\"元江哈尼族彝族傣族自治县\",\"rcode\":14312913241007,\"rid\":3219},{\"display\":\"红塔区\",\"rcode\":14312913241008,\"rid\":3220}]},{\"ccode\":1431291332,\"cid\":6537,\"display\":\"昭通市\",\"region\":[{\"display\":\"鲁甸县\",\"rcode\":14312913321000,\"rid\":3221},{\"display\":\"巧家县\",\"rcode\":14312913321001,\"rid\":3222},{\"display\":\"盐津县\",\"rcode\":14312913321002,\"rid\":3223},{\"display\":\"大关县\",\"rcode\":14312913321003,\"rid\":3224},{\"display\":\"永善县\",\"rcode\":14312913321004,\"rid\":3225},{\"display\":\"绥江县\",\"rcode\":14312913321005,\"rid\":3226},{\"display\":\"镇雄县\",\"rcode\":14312913321006,\"rid\":3227},{\"display\":\"彝良县\",\"rcode\":14312913321007,\"rid\":3228},{\"display\":\"威信县\",\"rcode\":14312913321008,\"rid\":3229},{\"display\":\"水富县\",\"rcode\":14312913321009,\"rid\":3230},{\"display\":\"昭阳区\",\"rcode\":14312913321010,\"rid\":3231}]}],\"display\":\"云南\",\"pcode\":143129,\"pid\":711},{\"city\":[{\"ccode\":1431301339,\"cid\":26690,\"display\":\"台州市\",\"region\":[{\"display\":\"黄岩区\",\"rcode\":14313013391000,\"rid\":3290},{\"display\":\"路桥区\",\"rcode\":14313013391001,\"rid\":3291},{\"display\":\"玉环县\",\"rcode\":14313013391002,\"rid\":3292},{\"display\":\"三门县\",\"rcode\":14313013391003,\"rid\":3293},{\"display\":\"天台县\",\"rcode\":14313013391004,\"rid\":3294},{\"display\":\"仙居县\",\"rcode\":14313013391005,\"rid\":3295},{\"display\":\"温岭市\",\"rcode\":14313013391006,\"rid\":3296},{\"display\":\"临海市\",\"rcode\":14313013391007,\"rid\":3297},{\"display\":\"椒江区\",\"rcode\":14313013391008,\"rid\":3298}]},{\"ccode\":1431301094,\"cid\":6538,\"display\":\"杭州市\",\"region\":[{\"display\":\"下城区\",\"rcode\":14313010941000,\"rid\":3314},{\"display\":\"江干区\",\"rcode\":14313010941001,\"rid\":3315},{\"display\":\"拱墅区\",\"rcode\":14313010941002,\"rid\":3316},{\"display\":\"西湖区\",\"rcode\":14313010941003,\"rid\":3317},{\"display\":\"滨江区\",\"rcode\":14313010941004,\"rid\":3318},{\"display\":\"萧山区\",\"rcode\":14313010941005,\"rid\":3319},{\"display\":\"余杭区\",\"rcode\":14313010941006,\"rid\":3320},{\"display\":\"桐庐县\",\"rcode\":14313010941007,\"rid\":3321},{\"display\":\"淳安县\",\"rcode\":14313010941008,\"rid\":3322},{\"display\":\"建德市\",\"rcode\":14313010941009,\"rid\":3323},{\"display\":\"富阳市\",\"rcode\":14313010941010,\"rid\":3324},{\"display\":\"临安市\",\"rcode\":14313010941011,\"rid\":3325},{\"display\":\"上城区\",\"rcode\":14313010941012,\"rid\":3326}]},{\"ccode\":1431301120,\"cid\":6539,\"display\":\"湖州市\",\"region\":[{\"display\":\"南浔区\",\"rcode\":14313011201000,\"rid\":3237},{\"display\":\"德清县\",\"rcode\":14313011201001,\"rid\":3238},{\"display\":\"长兴县\",\"rcode\":14313011201002,\"rid\":3239},{\"display\":\"安吉县\",\"rcode\":14313011201003,\"rid\":3240},{\"display\":\"吴兴区\",\"rcode\":14313011201004,\"rid\":3241}]},{\"ccode\":1431301126,\"cid\":6540,\"display\":\"嘉兴市\",\"region\":[{\"display\":\"秀洲区\",\"rcode\":14313011261000,\"rid\":3242},{\"display\":\"嘉善县\",\"rcode\":14313011261001,\"rid\":3243},{\"display\":\"海盐县\",\"rcode\":14313011261002,\"rid\":3244},{\"display\":\"海宁市\",\"rcode\":14313011261003,\"rid\":3245},{\"display\":\"平湖市\",\"rcode\":14313011261004,\"rid\":3246},{\"display\":\"桐乡市\",\"rcode\":14313011261005,\"rid\":3247},{\"display\":\"南湖区\",\"rcode\":14313011261006,\"rid\":3248}]},{\"ccode\":1431301135,\"cid\":6541,\"display\":\"金华市\",\"region\":[{\"display\":\"金东区\",\"rcode\":14313011351000,\"rid\":3249},{\"display\":\"武义县\",\"rcode\":14313011351001,\"rid\":3250},{\"display\":\"浦江县\",\"rcode\":14313011351002,\"rid\":3251},{\"display\":\"磐安县\",\"rcode\":14313011351003,\"rid\":3252},{\"display\":\"兰溪市\",\"rcode\":14313011351004,\"rid\":3253},{\"display\":\"义乌市\",\"rcode\":14313011351005,\"rid\":3254},{\"display\":\"东阳市\",\"rcode\":14313011351006,\"rid\":3255},{\"display\":\"永康市\",\"rcode\":14313011351007,\"rid\":3256},{\"display\":\"婺城区\",\"rcode\":14313011351008,\"rid\":3257}]},{\"ccode\":1431301164,\"cid\":6542,\"display\":\"丽水市\",\"region\":[{\"display\":\"青田县\",\"rcode\":14313011641000,\"rid\":3258},{\"display\":\"缙云县\",\"rcode\":14313011641001,\"rid\":3259},{\"display\":\"遂昌县\",\"rcode\":14313011641002,\"rid\":3260},{\"display\":\"松阳县\",\"rcode\":14313011641003,\"rid\":3261},{\"display\":\"云和县\",\"rcode\":14313011641004,\"rid\":3262},{\"display\":\"庆元县\",\"rcode\":14313011641005,\"rid\":3263},{\"display\":\"景宁畲族自治县\",\"rcode\":14313011641006,\"rid\":3264},{\"display\":\"龙泉市\",\"rcode\":14313011641007,\"rid\":3265},{\"display\":\"莲都区\",\"rcode\":14313011641008,\"rid\":3266}]},{\"ccode\":1431301192,\"cid\":6543,\"display\":\"宁波市\",\"region\":[{\"display\":\"江东区\",\"rcode\":14313011921000,\"rid\":3267},{\"display\":\"江北区\",\"rcode\":14313011921001,\"rid\":3268},{\"display\":\"北仑区\",\"rcode\":14313011921002,\"rid\":3269},{\"display\":\"镇海区\",\"rcode\":14313011921003,\"rid\":3270},{\"display\":\"鄞州区\",\"rcode\":14313011921004,\"rid\":3271},{\"display\":\"象山县\",\"rcode\":14313011921005,\"rid\":3272},{\"display\":\"宁海县\",\"rcode\":14313011921006,\"rid\":3273},{\"display\":\"余姚市\",\"rcode\":14313011921007,\"rid\":3274},{\"display\":\"慈溪市\",\"rcode\":14313011921008,\"rid\":3275},{\"display\":\"奉化市\",\"rcode\":14313011921009,\"rid\":3276},{\"display\":\"海曙区\",\"rcode\":14313011921010,\"rid\":3277}]},{\"ccode\":1431301214,\"cid\":6544,\"display\":\"衢州市\",\"region\":[{\"display\":\"衢江区\",\"rcode\":14313012141000,\"rid\":3278},{\"display\":\"常山县\",\"rcode\":14313012141001,\"rid\":3279},{\"display\":\"开化县\",\"rcode\":14313012141002,\"rid\":3280},{\"display\":\"龙游县\",\"rcode\":14313012141003,\"rid\":3281},{\"display\":\"江山市\",\"rcode\":14313012141004,\"rid\":3282},{\"display\":\"柯城区\",\"rcode\":14313012141005,\"rid\":3283}]},{\"ccode\":1431301229,\"cid\":6545,\"display\":\"绍兴市\",\"region\":[{\"display\":\"绍兴县\",\"rcode\":14313012291000,\"rid\":3284},{\"display\":\"新昌县\",\"rcode\":14313012291001,\"rid\":3285},{\"display\":\"诸暨市\",\"rcode\":14313012291002,\"rid\":3286},{\"display\":\"上虞市\",\"rcode\":14313012291003,\"rid\":3287},{\"display\":\"嵊州市\",\"rcode\":14313012291004,\"rid\":3288},{\"display\":\"越城区\",\"rcode\":14313012291005,\"rid\":3289}]},{\"ccode\":1431301269,\"cid\":6547,\"display\":\"温州市\",\"region\":[{\"display\":\"龙湾区\",\"rcode\":14313012691000,\"rid\":3299},{\"display\":\"瓯海区\",\"rcode\":14313012691001,\"rid\":3300},{\"display\":\"洞头县\",\"rcode\":14313012691002,\"rid\":3301},{\"display\":\"永嘉县\",\"rcode\":14313012691003,\"rid\":3302},{\"display\":\"平阳县\",\"rcode\":14313012691004,\"rid\":3303},{\"display\":\"苍南县\",\"rcode\":14313012691005,\"rid\":3304},{\"display\":\"文成县\",\"rcode\":14313012691006,\"rid\":3305},{\"display\":\"泰顺县\",\"rcode\":14313012691007,\"rid\":3306},{\"display\":\"瑞安市\",\"rcode\":14313012691008,\"rid\":3307},{\"display\":\"乐清市\",\"rcode\":14313012691009,\"rid\":3308},{\"display\":\"鹿城区\",\"rcode\":14313012691010,\"rid\":3309}]},{\"ccode\":1431301337,\"cid\":6548,\"display\":\"舟山市\",\"region\":[{\"display\":\"普陀区\",\"rcode\":14313013371000,\"rid\":3310},{\"display\":\"岱山县\",\"rcode\":14313013371001,\"rid\":3311},{\"display\":\"嵊泗县\",\"rcode\":14313013371002,\"rid\":3312},{\"display\":\"定海区\",\"rcode\":14313013371003,\"rid\":3313}]}],\"display\":\"浙江\",\"pcode\":143130,\"pid\":712}]}";
                #endregion
                return result;
            }
            catch (Exception e)
            {
                MediaService.WriteLog("获取全部省市区 出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
        }
        #endregion

        
    }
}
