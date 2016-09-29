using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MediaService
{
    /// <summary>
    /// 一些特殊需求的业务逻辑
    /// </summary>
    public abstract class HttpNewSpecialBusiness
    {
        public static string UserBindingCheckReseller(NameValueCollection qs)
        {
            /* NameValueCollection 值列表
             * ouid,sn,token,appid,[gender],[imei],[vcode]
             */
            #region uid
            string recv =CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["token"] == null || qs["sn"] == null || qs["appid"] == null)
            {
                MediaService.WriteLog("接收到用户绑定设备信息 ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var appid = qs["appid"];
                MediaService.WriteLog("接收到用户绑定设备信息 ：ouid =" + qs["ouid"] + " token =" + qs["token"].ToString() + " sn =" + qs["sn"].ToString() + "appid =" + qs["appid"].ToString(), MediaService.wirtelog);

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
                //查询是否注册SN
                string strUidMap = "SELECT count(*) FROM wy_uidmap  where uid = " + uid;
                object sqlUidMapResult = SqlHelper.ExecuteScalar(strUidMap);
                if (sqlUidMapResult != null)
                {
                    int count = (int)sqlUidMapResult;
                    if (count > 0)
                        return CommFunc.StandardFormat(MessageCode.SNExist);
                }
                if (sn.Substring(0, 6) == ConstStrings.StrNeedCheckReSeller) //号段属于保护号段区域,需要验证
                {
                    var ouid_binding_sn = "";//ouid下是否有属于保护号段的sn
                    //查询ouid下是否已经绑定了设备 (找到glsn列表)
                    string sqlglsn =
                        string.Format("select glsn from wy_uidmap t1,app_users t2 where t1.ouid={0} and t1.uid=t2.uid",ouid);
                    var uidResult = SqlHelper.ExecuteTable(sqlglsn);
                    if (uidResult.Rows.Count > 0)
                    {
                        foreach (DataRow row in uidResult.Rows)//遍历ouid名下是否有属于保护号段的SN
                        {
                            if (row["glsn"].ToString().Substring(0,6) == ConstStrings.StrNeedCheckReSeller)
                            {
                                ouid_binding_sn = row["glsn"].ToString();
                                break;
                            }
                        }
                        MediaService.WriteLog(string.Format("ouid_binding_sn:{0}", ouid_binding_sn),
                            MediaService.wirtelog);
                        if (!string.IsNullOrEmpty(ouid_binding_sn)) //如果已经找到ouid下保存号段sn,则去判断经销商
                        {
                            const string appkey = ConstStrings.StrNeedCheckReSeller_Release_Appkey;
                            const string constappid = ConstStrings.StrNeedCheckReSeller_Release_AppId;
                            var oldVenderCode = ""; //如果绑定过当前ouid绑定过,则查找任意一个uid对应的供应商code
                            var newVenderCode = ""; //要绑定的SN号的供应端id

                            //----------------------查询已经绑定的SN
                            var strOld = CommFunc.GetVenderInfo(constappid, ouid_binding_sn, appkey);
                            MediaService.WriteLog(
                                string.Format("接收到用户绑定设备信息 glsn:{0} old：{1}", ouid_binding_sn, strOld),MediaService.wirtelog);
                            var oldmodel = JsonConvert.DeserializeObject<Root>(strOld);
                            if (oldmodel.sysProductInfoResult.code == 0)
                            {
                                oldVenderCode = oldmodel.sysProductInfoResult.sysProductInfoDto.venderCode;
                            }
                            //----------------------查询将要绑定的SN
                            var strNew = CommFunc.GetVenderInfo(constappid, sn, appkey);
                            MediaService.WriteLog("接收到用户绑定设备信息 new：" + strNew, MediaService.wirtelog);
                            var newModel = JsonConvert.DeserializeObject<Root>(strNew);
                            if (newModel.sysProductInfoResult.code == 0)
                            {
                                newVenderCode = newModel.sysProductInfoResult.sysProductInfoDto.venderCode;
                            }

                            //-----------------------判断
                            if (oldVenderCode != newVenderCode)
                            {
                                return CommFunc.StandardFormat(MessageCode.BindingFaildOfReSellerError);
                            }
                        }
                    }
                }
                if (qs["vcode"] != null)
                {
                    //通知设备
                    string vcode = qs["vcode"].Replace("'", "");
                    string gender = qs["gender"] == null ? "" : qs["gender"].Replace("'", "");
                    recv = "{\"status\":true,\"uid\":\"" + ouid + "\",\"vcode\":\"" + vcode + "\",\"gender\":\"" + gender +
                           "\"}";
                    PublicClass.SendToOnlineUserList(null, recv, "", new List<int>() {uid}, 99, 0, CommType.userBinding,
                        CommFunc.APPID);
                }
                else
                {
                    //插入操作
                    //不存在插入设备
                    int sex;
                    int.TryParse(qs["gender"], out sex);
                    StringBuilder sql = new StringBuilder();
                    sql.Append("INSERT INTO wy_uidmap(ouid,uid) VALUES(" + ouid + "," + uid + ");");
                    if (qs["imei"] == null)
                    {
                        sql.Append(string.Format("update app_users set gender={0},updatetime = GETDATE() from app_users where uid={1};",sex, uid));
                    }
                    else
                    {
                        sql.Append(string.Format("update app_users set gender={0},snkey='{1}',updatetime = GETDATE() from app_users where uid={2};",sex, qs["imei"], uid));
                    }
                    int countInsert = SqlHelper.ExecuteNonQuery(sql.ToString());
                    if (countInsert < 1)
                    {
                        return CommFunc.WriteErrorJson(3, "用户绑定设备数据插入异常");
                    }
                    SqlHelper.ExecuteNonQuery(string.Format("UPDATE wy_userrelation SET ouid={0} WHERE [uid]={1}", ouid,
                        uid));
                }

                return CommFunc.StandardFormat(MessageCode.Success);
            }
            catch (Exception e)
            {
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
            #endregion
        }
    }
}
