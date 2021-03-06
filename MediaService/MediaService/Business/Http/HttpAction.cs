﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace MediaService
{
    class HttpAction
    {
        //Golo请求业务处理
        public static string httpGoloAction(string path, NameValueCollection qs)
        {
            string state = "";
            switch (path)
            {
                case "caralarm":   //报警播报
                    state = HttpGoloBusiness.CarAlarm(qs);
                    break;
                case "getproxy":   //获取代理服务地址
                    state = HttpGoloBusiness.GetProxy(qs);
                    break;
            }
            return state;
        }

        //用户请求业务处理
        public static string httpUserAction(string path, NameValueCollection qs)
        {
            string state = "";
            switch (path)
            {
                case "userlogin":   //用户登陆
                    state = HttpUserBusiness.UserLogin(qs);
                    break;
                case "getmymessage":   //获取自己的用户信息
                    state = HttpUserBusiness.GetMyMessage(qs);
                    break;
            }
            return state;
        }

        //客服请求业务处理
        public static string httpKfAction(string path, NameValueCollection qs)
        {
            string state = "";
            switch (path)
            {
                case "getserivebyid":
                    state = HttpKfBusiness.GetSeriveByID(qs);
                    break;
                case "getoneuserserive":
                    state = HttpKfBusiness.GetOneUserSerive(qs);
                    break;
                case "updateserive":
                    state = HttpKfBusiness.UpdateSerive(qs);
                    break;
                case "getkefuserive":
                    state = HttpKfBusiness.GetKefuSerive(qs);
                    break;
                case "getusermessage":
                    state = HttpKfBusiness.GetUserMessage(qs);
                    break;
                case "updateserivesounturl":
                    state = HttpKfBusiness.UpdateSeriveSountUrl(qs);
                    break;
                case "kfuserloginverification":
                    state = HttpKfBusiness.KFUserLoginVerification(qs);
                    break;
                case "gettalkonlineuser":
                    state = HttpKfBusiness.GetTalkOnLineUser(qs);
                    break;
                default:
                    break;
            }
            return state;
        }

        //内部系统请求业务处理
        public static string httpSysAction(string path, NameValueCollection qs)
        {
            string state = "";
            switch (path)
            {
                case "getusermessage":  //获取用户信息
                    state = HttpSysBusiness.GetUserMessage(qs);
                    break;
                case "gettalkmessage":  //获取频道信息
                    state = HttpSysBusiness.GetTalkMessage(qs);
                    break;
                case "getuseridlistmessage":  //根据用户ID列表获取基本信息
                    state = HttpSysBusiness.GetUserIdListMessage(qs);
                    break;
                case "getmytalklist":   //用户获取加入的组
                    state = HttpSysBusiness.GetMyTalkList(qs);
                    break;
                case "gettalklist":   //获取组列表
                    state = HttpSysBusiness.GetTalkList(qs);
                    break;
                case "gettalkuserlist":   //获取组列表
                    state = HttpSysBusiness.GetTalkUserList(qs);
                    break;
                case "createtalk":   //用户创建组
                    state = HttpSysBusiness.CreateTalk(qs);
                    break;
                case "jointalk":   //用户加入组
                    state = HttpSysBusiness.JoinTalk(qs);
                    break;
                case "setdjtalk":   //设置用户默认组
                    state = HttpSysBusiness.SetDjTalk(qs);
                    break;
                case "exittalk":   //用户退出组
                    state = HttpSysBusiness.ExitTalk(qs);
                    break;
                case "deletetalk":   //用户解散组
                    state = HttpSysBusiness.DeleteTalk(qs);
                    break;
                case "getuserlist":   //获取用户列表
                    state = HttpSysBusiness.GetUserList(qs);
                    break;
                case "getonlineuserlist":   //获取在线用户列表
                    state = HttpSysBusiness.GetOnlineUserList(qs);
                    break;
                case "pushmessagetoonlineuser": //系统推送信息至在线用户
                    state = HttpSysBusiness.PushMessageToOnlineUser(qs);
                    break;
                case "getonlinegroup": //获取在线频道
                    state = HttpSysBusiness.GetOnLineGroup(qs);
                    break;
                case "lineoff": //断开用户连接
                    state = HttpSysBusiness.LineOff(qs);
                    break;
                case "userverification": //用户验证
                    state = HttpSysBusiness.UserVerification(qs);
                    break;
                case "setfm": //设置FM频率
                    state = HttpSysBusiness.SetFM(qs);
                    break;
                case "setgolodebug": //设置Golo调试模式
                    state = HttpSysBusiness.SetGoloDebug(qs);
                    break;
                case "getsystemmessage": //获取系统信息
                    state = HttpSysBusiness.GetSystemMessage(qs);
                    break;
                case "getuserlocation": //获取用户地理位置
                    state = HttpSysBusiness.GetUserLocation(qs);
                    break;
                case "gettalkuserlocation": //获取组内所有用户地理位置
                    state = HttpSysBusiness.GetTalkUserLocation(qs);
                    break;
                case "modiusermessage": //修改用户信息
                    state = HttpSysBusiness.ModiUserMessage(qs);
                    break;
                case "moditalkmessage": //修改会话组或群的信息
                    state = HttpSysBusiness.ModiTalkMessage(qs);
                    break;
                case "gettalkuserduijiang": //获取用户对讲状态
                    state = HttpSysBusiness.GetTalkUserDuijiang(qs);
                    break;
                case "getonlinekflist": //获取在线客服列表
                    state = HttpSysBusiness.GetOnlineKFList(qs);
                    break;
                case "getserives": //获取查询工单列表
                    state = HttpSysBusiness.GetSerives(qs);
                    break;
                case "updateonlinekfuser": //更新在线客服
                    state = HttpSysBusiness.UpdateOnLineKFUser(qs);
                    break;
                case "getusernamelistmessage": //根据用户名（或sn）列表获取基本信息
                    state = HttpSysBusiness.GetUserNameListMessage(qs);
                    break;
                case "getusermoremessage"://获取用户详细信息
                    state = HttpSysBusiness.GetUserMoreMessage(qs);
                    break;
                case "exchangeusersn": //交换用户sn
                    state = HttpSysBusiness.ExchangeUserSn(qs);
                    break;
                case "modiusersn": //修改用户sn
                    state = HttpSysBusiness.ModiUserSn(qs);
                    break;
                case "getradiolist": //获取电台列表
                    state = HttpSysBusiness.GetRadioList();
                    break;
                case "modiradioinfo": //修改电台信息
                    state = HttpSysBusiness.ModiRadioInfo(qs);
                    break;
                case "getalluserlola":  //查询所有用户的经纬度
                    state = HttpSysBusiness.GetAllUserLongitudeAndlatitude(qs);
                    break;
                case "checkuseronlinestatus":   //验证用户在线状态
                    state = HttpSysBusiness.CheckUserOnlineStatus(qs);
                    break;
                case "getusersallapp":   //获取用户的所有设备
                    state = HttpSysBusiness.GetUsersAllApp(qs);
                    break;
                //case "getgoloztotaluser":  //查询GoloZ历史使用人数
                //    state = HttpSysBusiness.GetGoloZTotalUser(qs);
                //    break;
            }
            return state;
        }

        //wifi请求业务处理
        public static string httpWifiAction(string path, NameValueCollection qs)
        {
            string state = "";
            switch (path)
            {
                case "userlogin":   //用户获取加入的组
                    state = HttpWifiBusiness.UserLogin(qs);
                    break;
                case "getmytalklist":   //用户获取加入的组
                    state = HttpWifiBusiness.GetMyTalkList(qs);
                    break;
                case "createtalk":   //用户创建组
                    state = HttpWifiBusiness.CreateTalk(qs);
                    break;
                case "jointalk":   //用户加入组
                    state = HttpWifiBusiness.JoinTalk(qs);
                    break;
                case "setdjtalk":   //设置用户默认组
                    state = HttpWifiBusiness.SetDjTalk(qs);
                    break;
                case "exittalk":   //用户退出组
                    state = HttpWifiBusiness.ExitTalk(qs);
                    break;
                case "deletetalk":   //用户解散组
                    state = HttpWifiBusiness.DeleteTalk(qs);
                    break;
                case "getmymessage":   //获取自己的用户信息
                    state = HttpWifiBusiness.GetMyMessage(qs);
                    break;
            }
            return state;
        }

        /// GoloZ请求业务处理
        public static string httpZGoloAction(string path, NameValueCollection qs)
        {
            return GoloZAction.GetGoloZRecv(path, qs);
        }

        /// Golo车主事业部业务处理
        public static string goloZVehicleAction(string path, NameValueCollection qs)
        {
            return GoloZVehicleAction.GetGoloZVehicleRecv(path, qs);
        }

        #region 文件请求业务处理
        static byte[] ReadLineAsBytes(Stream SourceStream)
        {
            var resultStream = new MemoryStream();
            while (true)
            {
                int data = SourceStream.ReadByte();
                resultStream.WriteByte((byte)data);
                if (data == 10)
                    break;
            }
            resultStream.Position = 0;
            byte[] dataBytes = new byte[resultStream.Length];
            resultStream.Read(dataBytes, 0, dataBytes.Length);
            return dataBytes;
        }

        static bool CompareBytes(byte[] source, byte[] comparison)
        {
            int count = source.Length;
            if (source.Length != comparison.Length)
                return false;
            for (int i = 0; i < count; i++)
                if (source[i] != comparison[i])
                    return false;
            return true;
        }

        public class Values
        {
            public int type = 0;//0参数，1文件
            public string name;
            public byte[] datas;
        }

        /// <summary>
        /// 文件请求业务处理
        /// </summary>
        public static string HttpFileGoloAction(HttpListenerContext context)
        {
            try
            {
                HttpListenerRequest request = context.Request;
                if (request.ContentType.Length > 20 &&
                    string.Compare(request.ContentType.Substring(0, 20), "multipart/form-data;", true) == 0)
                {
                    List<Values> lst = new List<Values>();
                    Encoding encoding = request.ContentEncoding;
                    string[] values = request.ContentType.Split(';').Skip(1).ToArray();
                    string boundary = string.Join(";", values).Replace("boundary=", "").Trim();
                    byte[] chunkBoundary = encoding.GetBytes("--" + boundary + "\r\n");
                    byte[] endBoundary = encoding.GetBytes("--" + boundary + "--\r\n");
                    Stream sourceStream = request.InputStream;
                    var resultStream = new MemoryStream();
                    bool canMoveNext = true;
                    Values data = null;
                    while (canMoveNext)
                    {
                        byte[] currentChunk = ReadLineAsBytes(sourceStream);
                        if (!encoding.GetString(currentChunk).Equals("\r\n"))
                            resultStream.Write(currentChunk, 0, currentChunk.Length);
                        if (CompareBytes(chunkBoundary, currentChunk))
                        {
                            byte[] result = new byte[resultStream.Length - chunkBoundary.Length];
                            resultStream.Position = 0;
                            resultStream.Read(result, 0, result.Length);
                            canMoveNext = true;
                            if (result.Length > 0)
                                data.datas = result;
                            data = new Values();
                            lst.Add(data);
                            resultStream.Dispose();
                            resultStream = new MemoryStream();
                        }
                        else if (encoding.GetString(currentChunk).Contains("Content-Disposition"))
                        {
                            byte[] result = new byte[resultStream.Length - 2];
                            resultStream.Position = 0;
                            resultStream.Read(result, 0, result.Length);
                            canMoveNext = true;
                            data.name =
                                encoding.GetString(result)
                                    .Replace("Content-Disposition: form-data; name=\"", "")
                                    .Replace("\"", "")
                                    .Split(';')[0];
                            resultStream.Dispose();
                            resultStream = new MemoryStream();
                        }
                        else if (encoding.GetString(currentChunk).Contains("Content-Type"))
                        {
                            canMoveNext = true;
                            data.type = 1;
                            resultStream.Dispose();
                            resultStream = new MemoryStream();
                        }
                        else if (CompareBytes(endBoundary, currentChunk))
                        {
                            byte[] result = new byte[resultStream.Length - endBoundary.Length - 2];
                            resultStream.Position = 0;
                            resultStream.Read(result, 0, result.Length);
                            data.datas = result;
                            resultStream.Dispose();
                            canMoveNext = false;
                        }
                    }
                    foreach (var key in lst)
                    {
                        if (key.type == 1)
                        {
                            //FileStream fs = new FileStream(MediaService.fileurl + DateTime.Now.ToString("yyyyMMddHHmmssffff")+".png", FileMode.Create);
                            //fs.Write(key.datas, 0, key.datas.Length);//保存文件，调试时使用
                            //fs.Close();
                            //fs.Dispose();
                            NameValueCollection qs = context.Request.QueryString;
                            return HttpZGoloBusiness.SetFace(qs, key.datas);
                        }
                    }
                }
                return CommFunc.StandardFormat(MessageCode.MissKey);
            }
            catch (Exception e)
            {
                MediaService.WriteLog("处理httpFileGoloAction出错 ：" + e.Message, MediaService.wirtelog);
                return CommFunc.StandardFormat(MessageCode.DefaultError, e.Message);
            }
        } 
        #endregion
    }
}
