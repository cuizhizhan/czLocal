﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Net.Sockets;

namespace MediaService
{
    /*
 * Buffer 规定
 * Int16 2位 数据总长度
 * Int16 2位 命令
 * Int32 4位 格式保留
 * 剩下字节  业务
 */
    class CommAction
    {
        public static void NoticeUserLineState(int uid, int appid, int state)
        {
            if (uid != 0)
            {
                try
                {
                    //DataTable dt = SqlHelper.ExecuteTable("select uid from [wy_userrelation] where fuid=" + uid);
                    //foreach (DataRow dr in dt.Rows)
                    //{
                    //    int suid = Int32.Parse(dr["uid"].ToString());
                    //    UserObject uo = null;
                    //    if (MediaService.userDic.TryGetValue(suid, out uo))
                    //    {
                    //        if (uo.socket[appid] != null)
                    //        {
                    //            long t = DateTime.Now.Ticks;
                    //            try
                    //            {
                    //                string content = "{\"status\":true,\"uid\":" + uid + ",\"appid\":" + appid + ",\"online\":" + state + "}";
                    //                byte[] byt = null;
                    //                byte[] cbyte = Encoding.UTF8.GetBytes(content);
                    //                byt = new byte[cbyte.Length + 8];
                    //                Buffer.BlockCopy(System.BitConverter.GetBytes((short)byt.Length), 0, byt, 0, 2);
                    //                Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.noticUserLineState), 0, byt, 2, 2);
                    //                Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, byt, 4, 4);
                    //                if (cbyte.Length > 0)
                    //                    Buffer.BlockCopy(cbyte, 0, byt, 8, cbyte.Length);
                    //                uo.socket[appid].Send(byt, SocketFlags.None);
                    //            }
                    //            catch (Exception err)
                    //            {
                    //                MediaService.WriteLog("通知好友 " + uid + "下线异常1： t="+t+"t2="+DateTime .Now .Ticks +"     " + err.Message, MediaService.wirtelog);
                    //            }
                    //        }
                    //    }
                    //}




                    //object obj = SqlHelper.ExecuteScalar("select tid from [wy_talkuser] where uid=" + uid+ " and duijiang=1");
                    //if (obj != null)
                    //{
                    //    int tid = Int32.Parse(obj.ToString());
                    //    if(MediaService.talkHT[tid]!=null)
                    //    {
                    //        TalkMessage talkmessage = (TalkMessage)(MediaService.talkHT[tid]);
                    //        List<int> uidlist = talkmessage.uidlist;
                    //        foreach (int u in uidlist)
                    //        {
                    //            if (SocketServer.userHT[u] != null)
                    //            {
                    //                UserObject uo = (UserObject)SocketServer.userHT[u];
                    //                if (uo.socket[appid] != null)
                    //                {
                    //                    try
                    //                    {
                    //                        string content = "{\"status\":true,\"uid\":" + uid + ",\"appid\":" + appid + ",\"online\":" + state + "}";
                    //                        byte[] byt = null;
                    //                        byte[] cbyte = Encoding.UTF8.GetBytes(content);
                    //                        byt = new byte[cbyte.Length + 8];
                    //                        Buffer.BlockCopy(System.BitConverter.GetBytes((short)byt.Length), 0, byt, 0, 2);
                    //                        Buffer.BlockCopy(System.BitConverter.GetBytes(CommType.noticUserLineState), 0, byt, 2, 2);
                    //                        Buffer.BlockCopy(System.BitConverter.GetBytes(0), 0, byt, 4, 4);
                    //                        if (cbyte.Length > 0)
                    //                            Buffer.BlockCopy(cbyte, 0, byt, 8, cbyte.Length);
                    //                        uo.socket[appid].Send(byt, SocketFlags.None);
                    //                    }
                    //                    catch { }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("通知好友 " + uid + "下线异常： " + err.Message, MediaService.wirtelog);
                }
            }
        }

        public static int commaction(int packnum, short comm, int index, AsyncUserToken token)
        {
            string recv = "";
            switch (comm)
            {
                case CommType.sendAudioToTalk://用户发送语音消息到车队
                    recv = CommBusiness.SendAudioToTalk(token, packnum);
                    break;
                case CommType.sendAudioToChannel://用户发送语音消息到车队
                    recv = CommBusiness.SendAudioToChannel(token, packnum);
                    break;
                case CommType.userLogin:    //登陆 
                    recv = CommBusiness.userlogin(token, packnum);
                    break;
                case CommType.getUserFace: //获取用户头像
                    recv = CommBusiness.GetUserFace(token, packnum); ;
                    break;
                case CommType.getRegistCode://获取注册验证码
                    recv = CommBusiness.GetRegistCode(token, packnum);
                    break;
                case CommType.getErrorCode://获取代码对应的信息
                    recv = CommBusiness.GetCodeToMessagList(token, packnum);
                    break;
                case CommType.sendAudioMessage:    //发送音频消息
                    recv = CommBusiness.SendAudioMessage(token, packnum);
                    break;
                case CommType.lineCheck:   //链路检测
                    recv = CommBusiness.lineCheck(token);
                    break;
                case CommType.disConnect:   //断开连接
                    recv = null;
                    break;
                case CommType.sendUserLoLa:   //上传经纬度
                    recv = CommBusiness.SendUserLoLa(token, packnum);
                    break;
                case CommType.getNearUserFromLoLa:   //根据用户经纬度获取附近的用户
                    recv = CommBusiness.GetNearUserFromLoLa(token);
                    break;
                case CommType.getNearGroupFromLoLa:   //根据用户经纬度获取附近的群
                    recv = CommBusiness.GetNearGroupFromLoLa(token);
                    break;
                case CommType.sendUserMessage:  //用户发送私聊信息
                    recv = CommBusiness.SendUserMessage(token, packnum);
                    break;
                case CommType.getUserMessageList:  //获取用户消息列表
                    recv = CommBusiness.GetUserMessageList(token);
                    break;
                case CommType.getTalkToKey:    //根据关键字搜索会话组或群
                    recv = CommBusiness.GetTalkToKey(token, packnum);
                    break;
                case CommType.userExit:    //用户注销
                    recv = CommBusiness.UserExit(token);
                    break;
                case CommType.getNoReadMessageNum:      //获取未读信息数目
                    recv = CommBusiness.GetNoReadMessageNum(token);
                    break;
                case CommType.searchUserPublic:      //搜索用户公开信息
                    recv = CommBusiness.SearchUserPublic(token, packnum);
                    break;
                case CommType.getUserListPublic:      //获取用户列表公开信息
                    recv = CommBusiness.GetUserListPublic(token, packnum);
                    break;
                case CommType.getMyFriend: //获取我的好友
                    recv = CommBusiness.GetMyFriend(token);
                    break;
                case CommType.addMyFriend: //加好友
                    recv = CommBusiness.AddMyFriend(token);
                    break;
                case CommType.setBlackUser: //设黑名单
                    recv = CommBusiness.SetBlackUser(token);
                    break;
                case CommType.deleBlackUser: //取消黑名单
                    recv = CommBusiness.DeleBlackUser(token);
                    break;
                case CommType.getBlackList: //取消黑名单
                    recv = CommBusiness.GetBlackList(token);
                    break;
                case CommType.sendAppFeedback: //发送应用反馈
                    recv = CommBusiness.SendAppFeedback(token, packnum);
                    break;
                case CommType.modiUserMessage: //修改用户信息
                    recv = CommBusiness.ModiUserMessage(token, packnum);
                    break;
                case CommType.modiUserCarMessage: //修改用户车辆信息
                    recv = CommBusiness.ModiUserCarMessage(token, packnum);
                    break;
                case CommType.getUserCarMessage: //获取用户车辆信息
                    recv = CommBusiness.GetUserCarMessage(token, packnum);
                    break;
                case CommType.modiUserFace: //修改用户头像
                    recv = CommBusiness.ModiUserFace(token, packnum);
                    break;
                case CommType.deleMyFriend:  //删除好友
                    recv = CommBusiness.DeleMyFriend(token);
                    break;
                case CommType.checkAppUpdate: //检查应用更新
                    recv = CommBusiness.CheckAppUpdate(token, packnum);
                    break;
                case CommType.registUser: //注册用户
                    recv = CommBusiness.RegistUser(token, packnum);
                    break;
                case CommType.getMaxNoticeID: //获取最大通知ID
                    recv = CommBusiness.GetMaxNoticeID(token);
                    break;
                case CommType.getAllNotice: //获取所有通知ID
                    recv = CommBusiness.GetAllNotice(token);
                    break;
                case CommType.setNoticeState://设置通知状态
                    recv = CommBusiness.SetNoticeState(token);
                    break;
                case CommType.deleteUserMessage: //删除私聊消息
                    recv = CommBusiness.DeleteUserMessage(token, packnum, "wy_usermessage");
                    break;
                case CommType.deleteGroupMessage: //删除群聊消息
                    recv = CommBusiness.DeleteUserMessage(token, packnum, "wy_groupmessage");
                    break;
                case CommType.getUserState://获取用户状态
                    recv = CommBusiness.GetUserState(token, packnum);
                    break;
                case CommType.getUserModi://获取用户信息修改
                    recv = CommBusiness.GetUserModi(token);
                    break;
                case CommType.createTalk://创建会话或群
                    recv = CommBusiness.CreateTalk(token, packnum);
                    break;
                case CommType.addTalkUser://添加会话或群用户
                    recv = CommBusiness.AddTalkUser(token, packnum);
                    break;
                case CommType.userExitTalk://用户退出会话
                    recv = CommBusiness.UserExitTalk(token);
                    break;
                case CommType.fujinToTalk:  //附近组切换至对讲组
                    recv = CommBusiness.FujinToTalk(token, packnum);
                    break;
                case CommType.talkToFujin:  //对讲组切换至附近组
                    recv = CommBusiness.TalkToFujin(token, packnum);
                    break;
                case CommType.talkToTalk:  //对讲组切换至新对讲组
                    recv = CommBusiness.TalkToTalk(token, packnum);
                    break;
                case CommType.sendAudioToFujin:  //发送语音至附近组
                    recv = CommBusiness.SendAudioToFujin(token, packnum);
                    break;
                case CommType.userDeleteTalk://创建者解散会话或群
                    recv = CommBusiness.UserDeleteTalk(token);
                    break;
                case CommType.sendTalkMessage://客户端发送会话或群消息
                    recv = CommBusiness.SendTalkMessage(token, packnum);
                    break;
                case CommType.getTalkMessageList://获取会话消息列表
                    recv = CommBusiness.GetTalkMessageList(token);
                    break;
                case CommType.getTalkUser://获取会话组或群内的用户
                    recv = CommBusiness.GetTalkUser(token);
                    break;
                case CommType.getTalkListPublic://获取会话组和群列表公开信息
                    recv = CommBusiness.GetTalkListPublic(token, packnum);
                    break;
                case CommType.getMyAllTalk://获取我所加入的频道
                    recv = CommBusiness.GetMyAllTalk(token);
                    break;
                case CommType.modiTalkMessage://修改会话组或群的信息
                    recv = CommBusiness.ModiTalkMessage(token, packnum);
                    break;
                case CommType.deleTalkUser://创建者删除会话或群里的用户
                    recv = CommBusiness.DeleTalkUser(token);
                    break;
                case CommType.urlroute://应用路由请求转发
                    recv = CommBusiness.UrlRoute(token, packnum); ;
                    break;
                case CommType.sendUserUploadFile://客户端发送文件
                    recv = CommBusiness.SendUserUpdateFile(token, packnum); ;
                    break;
                case CommType.getUserUploadFile://客户端下载文件
                    recv = CommBusiness.GetUserUploadFile(token, packnum); ;
                    break;
                case CommType.setIosUserToken://设置用户IOS Token
                    recv = CommBusiness.SetIosUserToken(token, packnum);
                    break;
                case CommType.getCarList://客户端获取车型列表
                    recv = CommBusiness.GetCarList(token);
                    break;
                case CommType.userDownCarFile://客户端获取车型图标
                    recv = CommBusiness.UserDownCarFile(token, packnum);
                    break;
                case CommType.userJoinTalk://用户加入会话组或群
                    recv = CommBusiness.UserJoinTalk(token, packnum);
                    break;
                case CommType.getUserModiTime://获取用户信息修改状态
                    recv = CommBusiness.GetUserModiTime(token, packnum);
                    break;
                case CommType.getTalkModiTime://获取会话或群信息修改状态
                    recv = CommBusiness.GetTalkModiTime(token, packnum);
                    break;
                case CommType.getTalkMessage://获取群信息
                    recv = CommBusiness.GetTalkMessage(token, packnum);
                    break;
                case CommType.getTalkNocticeList://获取群通知列表
                    recv = CommBusiness.GetTalkNocticeList(token, packnum);
                    break;
                //已阅
                case CommType.getUserToken://获取用户Token
                    recv = CommBusiness.GetUserToken(token, packnum);
                    break;
                case CommType.setTalkNocticeState://设置群通知状态
                    recv = CommBusiness.SetTalkNocticeState(token, packnum);
                    break;
                case CommType.getTurnUser://获取转一转用户列表
                    recv = CommBusiness.GetTurnUser(token);
                    break;
                case CommType.otherUserBind://第三方绑定
                    recv = CommBusiness.OtherUserBind(token, packnum);
                    break;
                case CommType.sendServiceNotice://发送服务通知
                    recv = CommBusiness.SendServiceNotice(token, packnum);
                    break;
                case CommType.getMic://获取MIC、权限
                    recv = CommBusiness.GetMic(token, packnum);
                    break;
                case CommType.uploadUserMediaPlayLog://上传用户的媒体播放记录
                    recv = CommBusiness.UploadUserMediaPlayLog(token, packnum);
                    break;
                case CommType.uploadUserNewsPlayLog://上传用户的新闻播放记录
                    recv = CommBusiness.UploadUserNewsPlayLog(token, packnum);
                    break;
                case CommType.getDuiJiangTalk://获取用户所在的对讲组
                    recv = CommBusiness.GetDuiJiangTalk(token, packnum);
                    break;
                case CommType.sendDuiJiangState://客服对讲状态
                    recv = CommBusiness.SendDuiJiangState(token, packnum);
                    break;
                case CommType.setUserDuiJiangTalk: //设置用户当前对讲默认组
                    recv = CommBusiness.SetUserDuiJiangTalk(token, packnum);
                    break;
                case CommType.getServiceUrl://获取服务的地址
                    recv = CommBusiness.GetServiceUrl(token, packnum);
                    break;
                case CommType.userUpdateTalkInfo://用户更新频道备注
                    recv = CommBusiness.UserUpdateTalkInfo(token, packnum);
                    break;
                case CommType.sendKFCallUserState: //客服呼叫用户
                    recv = CommBusiness.SendKFCallUserState(token, packnum);
                    break;
                case CommType.getTalkName: //用户获取分配的频道号
                    recv = CommBusiness.GetTalkName(token, packnum);
                    break;
                case CommType.createMyTalk: //创建我的频道
                    recv = CommBusiness.CreateMyTalk(token, packnum);
                    break;
                case CommType.setGoloFmHz: //设置FM发射频率
                    recv = CommBusiness.SetGoloFmHz(token, packnum);
                    break;
                case CommType.setGoloDebug: //设置Golo调试模式
                    //recv = CommBusiness.SetGoloDebug(token, packnum);
                    break;
                case CommType.getMyAllChannel://获取我的频道列表
                    recv = CommBusiness.GetMyAllChannel(token);
                    break;
                case CommType.sendSingleChatState://用户单聊状态
                    recv = CommBusiness.SendSingleChatState(token, packnum);
                    break;
                case CommType.sendSingleChatMessage://用户发送单聊信息
                    recv = CommBusiness.SendSingleChatMessage(token, packnum);
                    break;
                case CommType.userSingleChatCall://用户单聊呼叫
                    recv = CommBusiness.UserSingleChatCall(token, packnum);
                    break;
                case CommType.userCallTalkMember://群呼频道成员
                    recv = CommBusiness.UserCallTalkMember(token, packnum);
                    break;
                case CommType.getMyContactList://获取我的通讯录
                    recv = CommBusiness.GetMyContactList(token, packnum);
                    break;
                case CommType.userUploadVogStream://用户上传电台语音流
                    recv = CommBusiness.UserUploadVogStream(token, packnum);
                    break;
                case CommType.userBindingBack: // 绑定设备
                    recv = CommBusiness.UserBindingBack(token, packnum);
                    break;
                case CommType.getUserWifiList: // 获取用户wifi列表
                    recv = CommBusiness.GetUserWifiList(token);
                    break;
                //case CommType.userJoinPersonTalk: // 获取用户通讯录-----这个接口有问题 通知Goloz
                //    recv = CommBusiness.GetUserContactList(token);
                //    break;
                case CommType.userGetMyAllChannel: //获取我的区号频道列表
                    recv = CommBusiness.UserGetMyAllChannel(token);
                    break;
                case CommType.getUserFuJinChannel: //获取附近频道名称
                    recv = CommBusiness.GetUserFuJinChannel(token);
                    break;
                case CommType.createUserOrder: // 用户OK键提交
                    recv = CommBusiness.CreateUserOrder(token, packnum);
                    break;
                case CommType.goloZUserJoinTalk: //goloz用户强制加入频道
                    recv = CommBusiness.GoloZUserJoinTalk(token, packnum);
                    break;
                case CommType.activeJoinLocalTalk: // GoloZ用户请求同聊应答
                    recv = CommBusiness.ActiveJoinLocalTalk(token, packnum);
                    break;
                case CommType.activeQuitLocalTalk: // GoloZ用户退出同聊
                    recv = CommBusiness.QuitLocalTalk(token, packnum);
                    break;
                case CommType.activeRequestLocalTalk:  //GoloZ用户请求约聊
                    recv = CommBusiness.ActiveRequestLocalTalk(token, packnum);
                    break;
                case CommType.userLocalTalk: //GoloZ用户用户同聊
                    recv = CommBusiness.UserLocalTalk(token, packnum);
                    break;
                default:
                    recv = CommBusiness.WriteErrorJson(1);
                    break;
                case CommType.refreashSim://刷新Sim卡信息
                    recv = CommBusiness.RefreashSim(token, packnum);
                    break;
                case CommType.getTalkInfo://获取单个频道信息
                    recv = CommBusiness.GetTalkInfo(token, packnum);
                    break;
                case CommType.requestQuickTalk://请求快聊
                    recv = CommBusiness.RequestQuickTalk(token, packnum);
                    break;
                case CommType.responseQuickTalk://回应快聊
                    recv = CommBusiness.ResponseQuickTalk(token, packnum);
                    break;
                case CommType.addUserFriend://回应快聊
                    recv = CommBusiness.AddUserFriend(token, packnum);
                    break;
                case CommType.responseUserCallTalkMember://1300 频道呼叫回复消息
                    recv = CommBusiness.ResponseUserCallTalkMember(token, packnum);
                    break;
                case CommType.newUserUploadVogStream://用户上传电台语音流--新
                    recv = CommBusiness.NewUserUploadVogStream(token, packnum);
                    break;
                case CommType.getFlow: //查询流量
                    recv = CommBusiness.GetFlow(token, packnum);
                    break;
                case CommType.getUsernumInTalk: //获取频道人数
                    recv = CommBusiness.GetUsernumInTalk(token, packnum);
                    break;
                case CommType.praise:   //点赞
                    recv = CommBusiness.Praise(token, packnum);
                    break;
                case CommType.searchChannelByVoice:    //语音搜索城市频道
                    recv = CommBusiness.SearchChannelByVoice(token, packnum);
                    break;
                case CommType.addEachFriend:   //互相加好友
                    recv = CommBusiness.AddEachFriend(token, packnum);
                    break;
                case CommType.voiceIntercom:    //语音对讲
                    recv = CommBusiness.VoiceIntercom(token, packnum);
                    break;
                case CommType.newGetMyAllChannel:   //新-获取我的频道列表
                    recv = CommBusiness.NewGetMyAllChannel(token, packnum);
                    break;
                case CommType.deleteContact:   //删除通讯录
                    recv = CommBusiness.DeleteContact(token, packnum);
                    break;
                case CommType.addUserWifi:   //添加wifi
                    recv = CommBusiness.AddUserWifi(token, packnum);
                    break;
                case CommType.deleteUserWifi:   //删除wifi
                    recv = CommBusiness.DeleteUserWifi(token, packnum);
                    break;

                #region V3.0
                case CommType.GivePraise: //送赞
                    recv = CommBusiness.GivePraise(token, packnum);
                    break;
                case CommType.GivePraiseRefresh://点赞刷新数据
                    recv = CommBusiness.GivePraiseRefresh(token, packnum);
                    break;
                case CommType.UsablePraises://当前用户的可用赞数
                    recv = CommBusiness.UsablePraises(token, packnum);
                    break;
                case CommType.GetCustomSoundCategories://获取自定义歌单列表
                    recv = CommBusiness.GetCustomSoundCategories(token, packnum);
                    break;
                case CommType.DeleteSoundCategories://删除歌单
                    recv = CommBusiness.DeleteSoundCategories(token, packnum);
                    break;
                case CommType.GetFavoriteSoundList: //获取喜欢的媒体列表
                    recv = CommBusiness.GetFavoriteSoundList(token, packnum);
                    break;
                case CommType.GetSubscribeSoundList://获取媒体订阅列表
                    recv = CommBusiness.GetSubscribeSoundList(token, packnum);
                    break;
                case CommType.AddToCustomSoundCategories://添加媒体到自定义歌单
                    recv = CommBusiness.AddToCustomSoundCategories(token, packnum);
                    break;
                case CommType.PlayAlbum://播放专辑
                    recv = CommBusiness.PlayAlbum(token, packnum);
                    break;
                case CommType.PlaySoundCategories://播放歌单
                    recv = CommBusiness.PlaySoundCategories(token, packnum);
                    break;
                case CommType.PlaySound://播放媒体
                    recv = CommBusiness.PlaySound(token, packnum);
                    break;
                case CommType.PlayMode://播放模式
                    recv = CommBusiness.PlayMode(token, packnum);
                    break;
                case CommType.RadioSubscribe://电台订阅
                    recv = CommBusiness.RadioSubscribe(token, packnum);
                    break;
                case CommType.PushNavigation://推送导航
                    recv = CommBusiness.PushNavigation(token, packnum);
                    break;
                case CommType.OpenHotspot://开启热点
                    recv = CommBusiness.OpenHotspot(token, packnum);
                    break;
                case CommType.UploadPower://上传电量
                    recv = CommBusiness.UploadPower(token, packnum);
                    break;
                case CommType.LaunchCall://单呼
                    recv= CommBusiness.LaunchCall(token, packnum);
                    break;
                    #endregion
            }
            if (comm != 2020 && comm != 1002)
                MediaService.WriteLog("客户端请求包长： " + packnum + " 命令：" + comm + " uid：" + token.uid + " sn：" + token.glsn + "， 服务器返回：" + (recv == null ? "已发送至用户" : recv), MediaService.wirtelog);
            return WirteSendBuffer(token, comm, index, recv);
        }

        #region 写发送缓冲区
        private static int WirteSendBuffer(AsyncUserToken token, short comm, int index, string content)
        {
            if (content != null)
            {
                int len = Encoding.UTF8.GetBytes(content, 0, content.Length, token.buffer, 8) + 8;
                Buffer.BlockCopy(System.BitConverter.GetBytes((short)len), 0, token.buffer, 0, 2);
                Buffer.BlockCopy(System.BitConverter.GetBytes(comm), 0, token.buffer, 2, 2);
                Buffer.BlockCopy(System.BitConverter.GetBytes(index), 0, token.buffer, 4, 4);
                return len;
            }
            else
            {
                return 0;
            }
        }
        #endregion
    }
}
