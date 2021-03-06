﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    class CommType
    {
        public const short userLogin = 1000;//登陆
        public const short getRegistCode = 1001;//获取注册验证码
        public const short sendAudioMessage = 1002; //音频消息
        public const short sendUserMessage = 1003; //用户发送私聊信息
        public const short recvUserMessage = 1004; //接收私聊消息
        public const short deleteUserMessage = 1005;//删除私聊消息
        public const short deleteGroupMessage = 1006;//删除群聊消息

        public const short getTalkMessage = 1007;//获取群信息
        public const short getUserToken = 1008;//获取用户最新token
        public const short userJoinTalk = 1009;//用户加入会话组或群
        public const short getTurnUser = 1010;//获取转一转用户列表

        public const short getTalkNocticeList = 1011;//获取群通知列表
        public const short setTalkNocticeState = 1012;//设置群通知状态
        public const short getTalkToKey = 1013;//根据关键字搜索会话组或群

        public const short getTalkModiTime = 1014;  //获取会话或群信息修改状态
        public const short getUserModiTime = 1015;  //获取用户信息修改状态
        public const short getUserMessageList = 1016; //获取用户消息列表
        public const short getNoReadMessageNum = 1017;  //获取未读信息数目
        public const short searchUserPublic = 1018;  //搜索用户公开信息
        public const short getMyFriend = 1019;  //获取我的好友
        public const short addMyFriend = 1020;  //加好友
        public const short setBlackUser = 1021;   //设黑名单
        public const short deleBlackUser = 1022;   //取消黑名单

        public const short getBlackList = 1023;   //获取黑名单列表
        public const short noticUserLineState = 1024; //推送用户状态
        public const short modiUserFace = 1025; //修改用户头像

        public const short pushUserNoticeinfo = 1026; //推送用户通知信息
        public const short pushMessageToUser = 1027;//应用服务器推送信息至用户
        public const short getServiceUrl = 1028;//获取服务的地址
        public const short deleMyFriend = 1029; //删除好友

        public const short recvInvitation = 1030; //发送邀请
        public const short setUserDuiJiangTalk = 1033; //设置用户当前对讲默认组

        public const short noticeUserJoinGroup = 1041;  //通知用户加入群组
        public const short getUserState = 1042;  //获取用户状态
        public const short getUserModi = 1043;  //获取用户信息修改

        public const short createTalk = 1044;//创建会话或群
        public const short addTalkUser = 1045; //添加会话或群用户
        public const short userExitTalk = 1046;//用户退出会话或群
        public const short getTalkListPublic = 1047; //获取会话组和群列表公开信息
        public const short sendTalkMessage = 1050; //客户端发送会话或群消息
        public const short recvTalkMessage = 1051; //客户端接收会话或群消息
        public const short getTalkMessageList = 1052; //获取会话或群消息列表
        public const short userDeleteTalk = 1054;//创建者解散会话或群
        public const short getTalkUser = 1055;//获取会话组或群内的用户
        public const short getMyAllTalk = 1056;//获取我所加入的频道
        public const short modiTalkMessage = 1057; //修改会话组或群的信息
        public const short deleTalkUser = 1058; //创建者删除会话或群里的用户
        public const short getErrorCode = 1059; //获取代码对应的信息
        public const short getUserFace = 1060; //获取用户头像
        public const short urlroute = 1061; //应用路由请求转发
        public const short setIosUserToken = 1062; //获取应用的地址

        public const short getUserListPublic = 1063; //获取用户列表公开信息

        public const short sendUserUploadFile = 1064; //客户端发送文件
        public const short getUserUploadFile = 1065; //客户端下载文件

        public const short getMic = 1066; //获取MIC、权限
        public const short sendAudioToTalk = 1067; //用户发送语音消息到车队
        public const short getDuiJiangTalk = 1068; //获取用户所在的对讲组

        public const short sendDuiJiangState = 1069; //客服对讲状态

        public const short fujinToTalk = 1070;  //附近组切换至对讲组
        public const short talkToFujin = 1071;  //对讲组切换至附近组
        public const short talkToTalk = 1072;  //对讲组切换至新对讲组
        public const short sendAudioToFujin = 1073;  //发送语音至附近组

        public const short userUpdateTalkInfo = 1075; //用户更新频道备注
        public const short getTalkName = 1076; //用户获取分配的频道号
        public const short createMyTalk = 1077; //创建我的频道
        public const short setGoloFmHz = 1078; //设置FM发射频率
        public const short setGoloDebug = 1079; //设置Golo调试模式

        public const short sendKFCallUserState = 1080; //客服呼叫用户

        public const short getMyAllChannel = 1081; //获取我的频道列表
        public const short sendSingleChatState = 1082; //用户单聊状态
        public const short sendSingleChatMessage = 1083; //用户发送单聊信息
        public const short userSingleChatCall = 1084; //用户单聊通过序列号呼叫

        public const short userCallTalkMember = 1085; //群呼频道成员
        public const short getMyContactList = 1086; //获取我的通讯录
        public const short userUploadVogStream = 1087; //用户上传电台语音流
        public const short sendAudioToChannel = 20000; //用户发送语音消息到频道

        public const short userGetMyAllChannel = 1200; //新版获取我的频道列表
        public const short getUserFuJinChannel = 1201; //获取附近频道名称
        public const short userNotInTalk = 1202; //用户不在此组

        //廖佛珍
        public const short userBindingBack = 1095;//用户绑定回调
        public const short createUserTalk = 1096;//创建我的频道
        public const short modifyUserTalk = 1097;//修改频道信息
        public const short userQuitTalk = 1098;//退出频道
        public const short addUserContact = 1099; //添加用户通讯录
        public const short userJoinPersonTalk = 1100; //用户加入频道
        public const short createUserOrder = 1101; //创建用户订单
        public const short forceUserJoinTalk = 1103; // 强制加入频道
        public const short goloZUserJoinTalk = 1104; //goloz用户强制加入频道

        public const short updateUserContact = 1088; //修改用户通讯录
        public const short deleteUserContact = 1089; //删除用户通讯录

        public const short getUserWifiList = 1090;//获取用户wifi列表
        public const short updateUserWifi = 1091;//更新用户wifi信息
        public const short deleteUserWifi = 1092;//删除用户wifi信息
        public const short addUserWifi = 1093;//添加用户wifi信息
        public const short switchUserWifi = 1102; //切换Wifi

        public const short userBinding = 1094;//用户绑定
        public const short userUnBinding = 1123;//解除绑定

        public const short activeRequestLocalTalk = 1105; //主动请求同聊
        public const short passiveReceiveLocalTalk = 1106; //被动接收同聊
        public const short activeJoinLocalTalk = 1107; //主动加入同聊
        public const short passiveJoinLocalTalk = 1108; //被动加入同聊
        public const short activeQuitLocalTalk = 1109; //主动退出
        public const short passiveQuitLocalTalk = 1110; //被动退出
        public const short userLocalTalk = 1111; //聊天        
        
        public const short refreashSim = 1112; //刷新Sim卡信息

        public const short getTalkInfo = 1113;//获取单个频道信息

        public const short requestQuickTalk = 1114;//请求快聊
        public const short responseQuickTalk = 1115;//回应快聊
        public const short noticeQuickTalk = 1117;//通知响应快聊

        public const short addUserFriend = 1116;//添加好友--新版

        public const short responseUserCallTalkMember = 1300;//频道呼叫回复消息
        public const short newUserCallTalkMember = 1301;//新频道呼叫---1085
        public const short newUserUploadVogStream = 1302;//用户上传电台语音流--新 1087
        public const short addEachFriend = 1303;//相互加好友
        public const short addEachFriendResponse = 1304;//相互加好友返回

        public const short modiUserMessage = 2011;//修改用户信息
        public const short registUser = 2013;//注册用户
        public const short getCarList = 2018;//客户端获取车型列表
        public const short userDownCarFile = 2019;//客户端下载车型图标
        public const short sendUserLoLa = 2020; // 上传用户经纬度
        public const short getNearUserFromLoLa = 2021; // 根据用户经纬度获取附近的用户
        public const short getNearGroupFromLoLa = 2022; // 根据用户经纬度获取附近的群
        public const short sendServiceNotice = 2023;//发送服务通知
        public const short otherUserBind = 2024; // 第三方绑定
        public const short uploadUserMediaPlayLog = 2025;//上传用户的媒体播放记录
        public const short uploadUserNewsPlayLog = 2026;//上传用户的新闻播放记录
        public const short modiUserCarMessage = 2027;//修改用户的车辆信息
        public const short getUserCarMessage = 2028;//获取用户的车辆信息

        public const short sendAppFeedback = 2010;//发送应用反馈
        public const short checkAppUpdate = 2012;//检查应用更新
        public const short pushNotice = 2014;//通知用户有新的通知
        public const short getMaxNoticeID = 2015;//获取最大通知ID
        public const short getAllNotice = 2016;//获取所有通知
        public const short setNoticeState = 2017;//设置通知状态

        public const short userExit = 9996; //注销用户000000
        public const short noLogin = 9997;
        public const short lineCheck = 9998; //链路检测 
        public const short disConnect = 9999; //断开连接

        public const short pushNavToUser = 1030; //推送导航信息至用户
        public const short pushNavSettingToUser = 1031; //推送导航配置至用户
        public const short voiceIntercom = 1074; //语音对讲
        public const short getFlow = 1118; //查询流量
        public const short getUsernumInTalk = 1119; //获取频道人数
        public const short praise = 1120; //点赞
        public const short newGetMyAllChannel = 1121; //新-获取我的频道列表
        public const short searchChannelByVoice = 1203; //语音搜索城市频道
        public const short deleteContact = 1122; //删除通讯录

        //V3.0
        public const short GivePraise = 1124;//送赞
        public const short GivePraiseRefresh = 1125; //获取点赞刷新数据
        public const short UsablePraises = 1126;//当前用户的可用赞数
        public const short GetCustomSoundCategories = 1127;//获取自定义歌单列表
        public const short DeleteSoundCategories = 1128;//删除歌单
        public const short GetFavoriteSoundList = 1129;//获取喜欢的媒体列表
        public const short GetSubscribeSoundList = 1130;//获取媒体订阅列表
        public const short AddToCustomSoundCategories = 1131;//添加媒体到自定义歌单
        public const short PlayAlbum = 1132;//播放专辑
        public const short PlaySoundCategories = 1133;//播放歌单
        public const short PlaySound = 1134;//播放媒体
        public const short PlayMode = 1135;//播放模式
        public const short RadioSubscribe = 1136;//电台订阅
        public const short PushNavigation = 1137;//推送导航
        public const short OpenHotspot = 1138;//开启热点

        public const short PushSong = 1139; //歌曲推送 自驾游
        public const short PushMsg = 1140;//消息推送 自驾游
        public const short CommPush = 1142;//通用消息转发
        public const short UploadPower = 1141; //上传电量
        public const short LaunchCall = 1143; //单呼
        public const short AppLaunchCall = 1144;//app发起的呼叫
    }

    internal enum EnumStateType
    {
        正在连接对方 = 0,
        对方不在线 = 1,
        对方正在通话中 = 2,
        连接建立成功 = 3,
        接通成功 = 4,
        对方拒接 = 5,
        对方未接听 = 6,
        对方已挂断 = 7,
        收到单呼 = 8
    }
}
