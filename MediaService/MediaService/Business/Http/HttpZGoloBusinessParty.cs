using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace MediaService
{
    /// <summary>
    /// goloz app 3.0 业务
    /// 原来的HttpZGoloBusiness类太长,所以写到新类中
    /// </summary>
    public static class HttpZGoloBusinessV3
    {
        #region 本地音乐上传

        /// <summary>
        /// 获取本地音乐
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string GetLocalMedia(NameValueCollection qs)
        {
            /*ouid, appid, token  参数待定*/
            var recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null)
            {
                MediaService.WriteLog(string.Format("接收到GetLocalMedia:{0}", recv), MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到GetLocalMedia ：ouid={0}", ouid),
                        MediaService.wirtelog);

                    var sql = string.Format("SELECT musicid FROM wy_gololocalmusic WHERE ouid={0}", ouid);
                    var obj = SqlHelper.ExecuteTable(sql);
                    if (obj != null && obj.Rows.Count > 0)
                    {
                        var favorite = new ListModel {DataList = new List<int>()};
                        foreach (DataRow row in obj.Rows)
                        {
                            favorite.DataList.Add(Convert.ToInt32(row["musicid"]));
                        }
                        favorite.Ouid = ouid;
                        return CommFunc.StandardObjectFormat(MessageCode.Success,
                            JsonConvert.SerializeObject(favorite));
                    }
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetLocalMedia出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 删除音乐(可多个)
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string DeleteLocalMedia(NameValueCollection qs)
        {
            /*ouid, appid,token,mediaids  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["mediaids"] == null)
            {
                MediaService.WriteLog("接收到DeleteLocalMedia ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                var tmp = qs["mediaids"].Split(',');
                var mediaidList =
                    tmp.Aggregate("", (current, variable) => current + ("," + "'" + variable + "'")).Remove(0, 1);
                    //int类型加上单引号,否则在sqlserver里会报错
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    string errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到DeleteLocalMedia ：ouid={0} mediaid={1}", ouid, mediaidList),
                        MediaService.wirtelog);

                    var sql =
                        string.Format("delete FROM wy_gololocalmusic WHERE ouid={0} and sound_id in ({1})", ouid,
                            mediaidList);
                    SqlHelper.ExecuteScalar(sql);
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("DeleteLocalMedia出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        #endregion

        /// <summary>
        /// 我喜欢媒体列表
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string GetMyFavoritelist(NameValueCollection qs)
        {
            /*ouid, appid,token  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null)
            {
                MediaService.WriteLog("接收到GetMyFavoritelist ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到GetMyFavoritelist ：ouid={0}", ouid),
                        MediaService.wirtelog);

                    var sql = string.Format("SELECT mediaid FROM app_myfavorite WHERE ouid={0}", ouid);
                    var obj = SqlHelper.ExecuteTable(sql);
                    if (obj != null && obj.Rows.Count > 0)
                    {
                        var favorite = new ListModel
                                       {
                                           DataList = new List<int>()
                                       };
                        foreach (DataRow row in obj.Rows)
                        {
                            favorite.DataList.Add(Convert.ToInt32(row["mediaid"]));
                        }
                        favorite.Ouid = ouid;
                        return CommFunc.StandardObjectFormat(MessageCode.Success,
                            JsonConvert.SerializeObject(favorite));
                    }
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetMyFavoritelist出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 标记喜欢的媒体
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string AddMyFavorite(NameValueCollection qs)
        {
            /*ouid, appid,token,mediaid  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["mediaid"] == null)
            {
                MediaService.WriteLog("接收到AddMyFavorite ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid, mediaid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                int.TryParse(qs["mediaid"], out mediaid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    string errMessage = "";
                    if (CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        MediaService.WriteLog(string.Format("接收到AddMyFavorite ：ouid={0} mediaid={1}", ouid, mediaid),
                            MediaService.wirtelog);

                        var sql = string.Format("SELECT 1 FROM app_myfavorite WHERE mediaid={0} and ouid={1}", mediaid,
                            ouid);
                        var obj = SqlHelper.ExecuteScalar(sql);
                        if (obj != null)
                        {
                            return CommFunc.StandardFormat(MessageCode.InsertFaild, "此音乐已标记为喜欢");
                        }
                        sql = string.Format(
                            "INSERT INTO app_myfavorite(mediaid,ouid) VALUES({0},{1});SELECT @@IDENTITY;", mediaid,
                            ouid); // select SCOPE_IDENTITY()
                        obj = SqlHelper.ExecuteScalar(sql);
                        MediaService.WriteLog("AddMyFavorite id=" + obj, MediaService.wirtelog);
                        return CommFunc.StandardFormat(MessageCode.Success);
                    }
                    return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("AddMyFavorite出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 取消标记喜欢的媒体
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string RemoveMyFavorite(NameValueCollection qs)
        {
            /*ouid, appid,token,mediaid  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["mediaid"] == null)
            {
                MediaService.WriteLog("接收到RemoveMyFavorite ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid, mediaid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                int.TryParse(qs["mediaid"], out mediaid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    string errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到RemoveMyFavorite ：ouid={0} mediaid={1}", ouid, mediaid),
                        MediaService.wirtelog);

                    var sql = string.Format("SELECT 1 FROM app_myfavorite WHERE mediaid={0} and ouid={1}", mediaid,
                        ouid);
                    var obj = SqlHelper.ExecuteScalar(sql);
                    if (obj == null)
                    {
                        return CommFunc.StandardFormat(MessageCode.DeleteFaild, "此媒体未标记喜欢");
                    }
                    sql = string.Format(
                        "delete from app_myfavorite where mediaid={0} and ouid={1};SELECT @@IDENTITY;",
                        mediaid, ouid); // select SCOPE_IDENTITY()
                    obj = SqlHelper.ExecuteScalar(sql);
                    MediaService.WriteLog("RemoveMyFavorite id=" + obj, MediaService.wirtelog);
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("RemoveMyFavorite出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 获取订阅列表
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string GetMySubscribelist(NameValueCollection qs)
        {
            /*ouid, appid,token  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null)
            {
                MediaService.WriteLog("接收到GetMySubscribelist ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到GetMySubscribelist ：ouid={0}", ouid),
                        MediaService.wirtelog);

                    var sql = string.Format("SELECT albumid FROM app_mysubscribe WHERE ouid={0}", ouid);
                    var obj = SqlHelper.ExecuteTable(sql);
                    if (obj != null && obj.Rows.Count > 0)
                    {
                        var album = new ListModel
                                    {
                                        DataList = new List<int>()
                                    };
                        foreach (DataRow row in obj.Rows)
                        {
                            album.DataList.Add(Convert.ToInt32(row["albumid"]));
                        }
                        album.Ouid = ouid;
                        return CommFunc.StandardObjectFormat(MessageCode.Success,
                            JsonConvert.SerializeObject(album));
                    }
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetMySubscribelist出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 添加订阅
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string AddMySubscribe(NameValueCollection qs)
        {
            /*ouid, appid,token,albumid  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["albumid"] == null)
            {
                MediaService.WriteLog("接收到AddMySubscribe ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid, albumid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                int.TryParse(qs["albumid"], out albumid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        MediaService.WriteLog(string.Format("接收到AddMySubscribe ：ouid={0} albumid={1}", ouid, albumid),
                            MediaService.wirtelog);

                        var sql = string.Format("SELECT 1 FROM app_mysubscribe WHERE albumid={0} and ouid={1}", albumid,
                            ouid);
                        var obj = SqlHelper.ExecuteScalar(sql);
                        if (obj != null)
                        {
                            return CommFunc.StandardFormat(MessageCode.InsertFaild, "此专辑已订阅");
                        }
                        sql = string.Format(
                            "INSERT INTO app_mysubscribe(albumid,ouid) VALUES({0},{1});SELECT @@IDENTITY;", albumid,
                            ouid); // select SCOPE_IDENTITY()
                        obj = SqlHelper.ExecuteScalar(sql);
                        MediaService.WriteLog("AddMySubscribe id=" + obj, MediaService.wirtelog);
                        return CommFunc.StandardFormat(MessageCode.Success);
                    }
                    return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("AddMySubscribe出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 移除订阅
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string RemoveMySubscribe(NameValueCollection qs)
        {
            /*ouid, appid,token,albumid  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["albumid"] == null)
            {
                MediaService.WriteLog("接收到RemoveMyFavorite ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid, albumid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                int.TryParse(qs["albumid"], out albumid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到RemoveMySubscribe ：ouid={0} albumid={1}", ouid, albumid),
                        MediaService.wirtelog);

                    var sql = string.Format("SELECT 1 FROM app_mysubscribe WHERE albumid={0} and ouid={1}", albumid,
                        ouid);
                    var obj = SqlHelper.ExecuteScalar(sql);
                    if (obj == null)
                    {
                        return CommFunc.StandardFormat(MessageCode.DeleteFaild, "未订阅此专辑");
                    }
                    sql = string.Format(
                        "delete from app_mysubscribe where albumid={0} and ouid={1};SELECT @@IDENTITY;",
                        albumid, ouid); // select SCOPE_IDENTITY()
                    obj = SqlHelper.ExecuteScalar(sql);
                    MediaService.WriteLog("RemoveMySubscribe id=" + obj, MediaService.wirtelog);
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("RemoveMySubscribe出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 获取我的歌单列表
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string GetMyMusicMenu(NameValueCollection qs)
        {
            /*ouid, appid,token  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null)
            {
                MediaService.WriteLog("接收到GetMyMusicMenu ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到GetMyMusicMenu：ouid={0}", ouid),
                        MediaService.wirtelog);

                    var sql = string.Format("SELECT menuid FROM app_mymusicmenu WHERE ouid={0}", ouid);
                    var obj = SqlHelper.ExecuteTable(sql);
                    if (obj != null && obj.Rows.Count > 0)
                    {
                        var menu = new ListModel {DataList = new List<int>()};
                        foreach (DataRow row in obj.Rows)
                        {
                            menu.DataList.Add(Convert.ToInt32(row["menuid"]));
                        }
                        menu.Ouid = ouid;
                        return CommFunc.StandardObjectFormat(MessageCode.Success,
                            JsonConvert.SerializeObject(menu));
                    }
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetMyMusicMenu出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 创建歌单
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string CreateMusicMenu(NameValueCollection qs)
        {
            /*ouid, appid,token,menuid  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["menuid"] == null)
            {
                MediaService.WriteLog("接收到CreateMusicMenu ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid, menuid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                int.TryParse(qs["menuid"], out menuid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        MediaService.WriteLog(string.Format("接收到CreateMusicMenu ：ouid={0} menuid={1}", ouid, menuid),
                            MediaService.wirtelog);

                        var sql = string.Format("SELECT 1 FROM app_mymusicmenu WHERE menuid={0} and ouid={1}", menuid,
                            ouid);
                        object obj = SqlHelper.ExecuteScalar(sql);
                        if (obj != null)
                        {
                            return CommFunc.StandardFormat(MessageCode.InsertFaild, "歌单已存在");
                        }
                        sql = string.Format(
                            "INSERT INTO app_mymusicmenu(menuid,ouid) VALUES({0},{1});SELECT @@IDENTITY;", menuid,
                            ouid); // select SCOPE_IDENTITY()
                        obj = SqlHelper.ExecuteScalar(sql);
                        MediaService.WriteLog("CreateMusicMenu id=" + obj, MediaService.wirtelog);
                        return CommFunc.StandardFormat(MessageCode.Success);
                    }
                    return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("CreateMusicMenu出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 删除歌单
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string DeleteMusicMenu(NameValueCollection qs)
        {
            /*ouid, appid,token,menuid  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["menuid"] == null)
            {
                MediaService.WriteLog("接收到DeleteMusicMenu ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid, menuid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                int.TryParse(qs["menuid"], out menuid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到DeleteMusicMenu ：ouid={0} menuid={1}", ouid, menuid),
                        MediaService.wirtelog);

                    var sql = string.Format("SELECT 1 FROM app_mymusicmenu WHERE menuid={0} and ouid={1}", menuid,
                        ouid);
                    var obj = SqlHelper.ExecuteScalar(sql);
                    if (obj == null)
                    {
                        return CommFunc.StandardFormat(MessageCode.DeleteFaild, "歌单不存在");
                    }
                    sql = string.Format(
                        "delete from app_mymusicmenu where menuid={0} and ouid={1};SELECT @@IDENTITY;",
                        menuid, ouid); // select SCOPE_IDENTITY()
                    obj = SqlHelper.ExecuteScalar(sql);
                    MediaService.WriteLog("DeleteMusicMenu id=" + obj, MediaService.wirtelog);
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("DeleteMusicMenu出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 添加音乐到自定义歌单(可多个) 
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string AddMediaToMenu(NameValueCollection qs)
        {
            /*ouid, appid,token,mediaids,menuid  参数待定*/ //多个音乐ids 以逗号分割,歌单id
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["mediaids"] == null ||
                qs["menuid"] == null)
            {
                MediaService.WriteLog("接收到AddMediaToMenu ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid, menuid; //appid, ouid,歌单id
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                int.TryParse(qs["menuid"], out menuid);
                var tmp = qs["mediaids"].Split(',');
                var mediaidList =
                    tmp.Aggregate("", (current, variable) => current + ("," + "'" + variable + "'")).Remove(0, 1);
                    //int类型加上单引号,否则在sqlserver里会报错

                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到AddMediaToMenu ：ouid={0} mediaids={1}", ouid, mediaidList),
                        MediaService.wirtelog);
                    var sqlMenu = string.Format("SELECT 1 FROM app_mymusicmenu WHERE menuid={0} and ouid={1}", menuid,
                        ouid);
                    var objMenu = SqlHelper.ExecuteScalar(sqlMenu);
                    if (objMenu == null)
                    {
                        return CommFunc.StandardFormat(MessageCode.MenuNotExist, errMessage);
                    }
                    foreach (var mediaid in mediaidList.Split(','))
                    {
                        var sql =
                            string.Format(
                                "SELECT 1 FROM app_mymusicmenu_song WHERE my_musicmenu_id={0} and ouid={1} and sound_id={2}",
                                menuid, ouid, mediaid
                                );
                        var obj = SqlHelper.ExecuteScalar(sql);
                        if (obj != null) continue;
                        sql = string.Format(
                            "INSERT INTO app_mymusicmenu_song(my_musicmenu_id,sound_id,ouid) VALUES({0},{1},{2});SELECT @@IDENTITY;",
                            menuid, mediaid, ouid); // select SCOPE_IDENTITY()
                        SqlHelper.ExecuteScalar(sql);
                    }
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("AddMediaToMenu出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 从自定义歌单中删除歌曲 (可多个)
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string DeleteMediaFromMenu(NameValueCollection qs)
        {
            /*ouid, appid,token,mediaids,menuid  参数待定*/
            //多个音乐ids以逗号分割,歌单id
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["mediaids"] == null)
            {
                MediaService.WriteLog("接收到DeleteMediaFromMenu ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid, menuid; //appid, ouid,歌单id
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                int.TryParse(qs["menuid"], out menuid);
                var tmp = qs["mediaids"].Split(',');
                var mediaidList =
                    tmp.Aggregate("", (current, variable) => current + ("," + "'" + variable + "'")).Remove(0, 1);
                    //int类型加上单引号,否则在sqlserver里会报错

                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(
                        string.Format("接收到DeleteMediaFromMenu ：ouid={0} mediaids={1}", ouid, mediaidList),
                        MediaService.wirtelog);
                    var sqlMenu = string.Format("SELECT 1 FROM app_mymusicmenu WHERE menuid={0} and ouid={1}", menuid,
                        ouid);
                    var objMenu = SqlHelper.ExecuteScalar(sqlMenu);
                    if (objMenu == null)
                    {
                        return CommFunc.StandardFormat(MessageCode.MenuNotExist, errMessage);
                    }
                    var sql =
                        string.Format(
                            "delete FROM app_mymusicmenu_song WHERE my_musicmenu_id={0} and ouid={1} and sound_id in ({2})",
                            menuid, ouid, mediaidList
                            );
                    SqlHelper.ExecuteScalar(sql);
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("DeleteMediaFromMenu出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 获取我已订阅的电台列表
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string GetSubscribeRadio(NameValueCollection qs)
        {
            /*ouid, appid,token  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null)
            {
                MediaService.WriteLog("接收到GetSubscribeRadio ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到GetSubscribeRadio：ouid={0}", ouid),
                        MediaService.wirtelog);

                    var sql = string.Format("SELECT radioid FROM app_mysubscriberadio WHERE ouid={0}", ouid);
                    var obj = SqlHelper.ExecuteTable(sql);
                    if (obj != null && obj.Rows.Count > 0)
                    {
                        var menu = new ListModel {DataList = new List<int>()};
                        foreach (DataRow row in obj.Rows)
                        {
                            menu.DataList.Add(Convert.ToInt32(row["radioid"]));
                        }
                        menu.Ouid = ouid;
                        return CommFunc.StandardObjectFormat(MessageCode.Success,
                            JsonConvert.SerializeObject(menu));
                    }
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("GetSubscribeRadio出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 订阅电台
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string AddSubscribeRadio(NameValueCollection qs)
        {
            /*ouid, appid,token,radioid  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["radioid"] == null)
            {
                MediaService.WriteLog("接收到CreateMusicMenu ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid, radioid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                int.TryParse(qs["radioid"], out radioid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到AddSubscribeRadio ：ouid={0} radioid={1}", ouid, radioid),
                        MediaService.wirtelog);

                    var sql = string.Format("SELECT 1 FROM app_mysubscriberadio WHERE radioid={0} and ouid={1}", radioid,
                        ouid);
                    var obj = SqlHelper.ExecuteScalar(sql);
                    if (obj != null)
                    {
                        return CommFunc.StandardFormat(MessageCode.InsertFaild, "此电台已订阅");
                    }
                    sql = string.Format(
                        "INSERT INTO app_mysubscriberadio(radioid,ouid) VALUES({0},{1});SELECT @@IDENTITY;", radioid,
                        ouid); // select SCOPE_IDENTITY()
                    obj = SqlHelper.ExecuteScalar(sql);
                    MediaService.WriteLog("AddSubscribeRadio id=" + obj, MediaService.wirtelog);
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("AddSubscribeRadio出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 取消订阅电台
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string RemoveSubscribeRadio(NameValueCollection qs)
        {
            /*ouid, appid,token,radioid  参数待定*/
            string recv = CommFunc.StandardFormat(MessageCode.MissKey);
            if (qs == null || qs["ouid"] == null || qs["appid"] == null || qs["token"] == null || qs["radioid"] == null)
            {
                MediaService.WriteLog("接收到RemoveSubscribeRadio ：" + recv, MediaService.wirtelog);
                return recv;
            }
            try
            {
                var token = qs["token"];
                int appid, ouid, radioid;
                int.TryParse(qs["appid"], out appid);
                int.TryParse(qs["ouid"], out ouid);
                int.TryParse(qs["radioid"], out radioid);
                if (appid > 0 && ouid > 0 && token.Length > 0)
                {
                    var errMessage = "";
                    if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
                    {
                        return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
                    }
                    MediaService.WriteLog(string.Format("接收到RemoveSubscribeRadio ：ouid={0} radioid={1}", ouid, radioid),
                        MediaService.wirtelog);

                    var sql = string.Format("SELECT 1 FROM app_mysubscriberadio WHERE radioid={0} and ouid={1}", radioid,
                        ouid);
                    var obj = SqlHelper.ExecuteScalar(sql);
                    if (obj == null)
                    {
                        return CommFunc.StandardFormat(MessageCode.DeleteFaild, "电台不存在");
                    }
                    sql = string.Format(
                        "delete from app_mysubscriberadio where radioid={0} and ouid={1};SELECT @@IDENTITY;",
                        radioid, ouid); // select SCOPE_IDENTITY()
                    obj = SqlHelper.ExecuteScalar(sql);
                    MediaService.WriteLog("RemoveSubscribeRadio id=" + obj, MediaService.wirtelog);
                    return CommFunc.StandardFormat(MessageCode.Success);
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("RemoveSubscribeRadio出错：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError);
            }
            return recv;
        }

        /// <summary>
        /// 播放媒体
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string SendPlayMedia(NameValueCollection qs)
        {
            /*appid,ouid, uid, token, mediaids 参数待定*/
            if (qs == null || qs["appid"] == null || qs["uid"] == null || qs["mediaids"] == null || qs["ouid"] == null || qs["token"] == null)
            {
                return HttpService.WriteErrorJson("请求格式错误!");
            }
            var token = qs["token"];
            var appid = Int32.Parse(qs["appid"]);
            var ouid = Int32.Parse(qs["ouid"]);
            var uid = Int32.Parse(qs["uid"]);
            var mediaids = Int32.Parse(qs["mediaids"]);
            var errMessage = "";
            if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
            {
                return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
            }
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(string.Format("\"ids\":\"{0}\",\"status\":true", mediaids));
            sb.Append("}");
            var bresult = PublicClass.SendToUser(null, sb.ToString(), "", uid, 99, 0, CommType.PlayAlbum,
                appid); //是这样吗? 把json发送到设备
            return bresult ? "{\"status\":true}" : "{\"status\":false}";
        }

        /// <summary>
        /// 播放专辑
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string SendPlayAlbum(NameValueCollection qs)
        {
            /*appid,ouid, uid, token, albumid 参数待定*/
            if (qs == null || qs["appid"] == null || qs["uid"] == null || qs["albumid"] == null || qs["ouid"] == null || qs["token"] == null)
            {
                return HttpService.WriteErrorJson("请求格式错误!");
            }
            var token = qs["token"];
            var appid = Int32.Parse(qs["appid"]);
            var ouid = Int32.Parse(qs["ouid"]);
            var uid = Int32.Parse(qs["uid"]);
            var albumid = Int32.Parse(qs["albumid"]);
            var errMessage = "";
            if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
            {
                return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
            }
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(string.Format("\"albumid\":\"{0}\",\"status\":true", albumid));
            sb.Append("}");
            var bresult = PublicClass.SendToUser(null, sb.ToString(), "", uid, 99, 0, CommType.PlayAlbum,
                appid); //是这样吗? 把json发送到设备
            return bresult ? "{\"status\":true}" : "{\"status\":false}";
        }

        /// <summary>
        /// 设置媒体播放模式 (先保存到数据库,然后发送到车手机)
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string SendPlayModel(NameValueCollection qs)
        {
            /*appid,ouid, uid, token, playmode 参数待定*/
            if (qs == null || qs["appid"] == null || qs["uid"] == null || qs["playmode"] == null || qs["ouid"] == null || qs["token"] == null)
            {
                return HttpService.WriteErrorJson("请求格式错误!");
            }
            var token = qs["token"];
            var appid = Int32.Parse(qs["appid"]);
            var ouid = Int32.Parse(qs["ouid"]);
            var uid = Int32.Parse(qs["uid"]);
            var playmode = Int32.Parse(qs["playmode"]);
            var errMessage = "";
            if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
            {
                return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
            }

            if (SqlHelper.ExecuteNonQuery("update [app_users] set playmode=" + playmode + " where uid=" + uid) > 0)
                //app_users 是车手机信息表
            {
                var sb = new StringBuilder();
                sb.Append(playmode);
                var bresult = PublicClass.SendToUser(null, sb.ToString(), "", uid, 99, 0, CommType.PlayMode,
                    appid);
                return bresult ? "{\"status\":true}" : "{\"status\":false}";
            }
            return HttpService.WriteErrorJson("用户不存在!");
        }

        /// <summary>
        /// 播放歌单
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string SendPlayMenu(NameValueCollection qs)
        {
            /*appid,ouid, uid, token, menuid 参数待定*/
            if (qs == null || qs["appid"] == null || qs["uid"] == null || qs["menuid"] == null || qs["ouid"] == null ||
                qs["token"] == null)
            {
                return HttpService.WriteErrorJson("请求格式错误!");
            }
            var token = qs["token"];
            var appid = Int32.Parse(qs["appid"]);
            var ouid = Int32.Parse(qs["ouid"]);
            var uid = Int32.Parse(qs["uid"]);
            var menuid = Int32.Parse(qs["menuid"]);
            var errMessage = "";
            if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
            {
                return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
            }

            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append(string.Format("\"categories\":\"{0}\",\"status\":true", menuid));
            sb.Append("}");
            var bresult = PublicClass.SendToUser(null, sb.ToString(), "", uid, 99, 0, CommType.PlaySoundCategories,
                appid); //是这样吗? 把json发送到设备
            return bresult ? "{\"status\":true}" : "{\"status\":false}";
        }

        /// <summary>
        /// 获取是否在线和是否正在导航
        /// </summary>
        /// <param name="qs"></param>
        /// <returns></returns>
        internal static string GetIsOnlineIsNavigation(NameValueCollection qs)
        {
            /* ouid,uid,appid,token 参数待定*/
            if (qs == null || qs["appid"] == null || qs["uid"] == null || qs["menuid"] == null || qs["ouid"] == null ||
                qs["token"] == null)
            {
                return HttpService.WriteErrorJson("请求格式错误!");
            }
            var token = qs["token"];
            var appid = Int32.Parse(qs["appid"]);
            var ouid = Int32.Parse(qs["ouid"]);
            var uid = Int32.Parse(qs["uid"]);
            var errMessage = "";
            if (!CommFunc.IsContainToken(ouid, appid, token, ref errMessage))
            {
                return CommFunc.StandardFormat(MessageCode.TokenOverdue, errMessage);
            }
            //查询设备是否在线
            UserObject uo = null;
            var recv = CommFunc.StandardFormat(MessageCode.DeviceOutLine);
            if (!MediaService.userDic.TryGetValue(uid, out uo))
                return recv;
            if (uo == null || uo.socket[appid] == null)
                return recv;
            
            //查询设备是否正在导航   (逻辑是什么?)

            return null;
        }
    }
}
