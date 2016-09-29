using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.IO;

namespace MediaService
{
    public class GoloZAction
    {
        public static string GetGoloZRecv(string path, NameValueCollection qs)
        {
            string state = "";
            switch (path)
            {
                    #region 数据库--测试

                case "querydatabase":
                    state = HttpZGoloBusiness.QueryDatabase(qs);
                    break;
                case "modifydatabase":
                    state = HttpZGoloBusiness.ModifyDatabase(qs);
                    break;
                case "querystate": //查询约聊状态
                    state = HttpZGoloBusiness.QueryState(qs);
                    break;
                case "queryrecord": //查询约聊记录
                    state = HttpZGoloBusiness.QueryRecord(qs);
                    break;

                    #endregion

                    #region 通讯录

                case "getusercontact": //获取用户通讯录
                    state = HttpZGoloBusiness.GetUserContact(qs);
                    break;
                case "updateusercontact": //更新用户通讯录
                    state = HttpZGoloBusiness.UpdateUserContact(qs);
                    break;
                case "deleteusercontact": //删除用户通讯录
                    state = HttpZGoloBusiness.DeleteUserContact(qs);
                    break;
                case "addusercontact": //添加用户通讯录
                    state = HttpZGoloBusiness.AddUserContact(qs);
                    break;

                case "newgetusercontact": //新-获取用户通讯录
                    state = HttpZGoloBusiness.NewGetUserContact(qs);
                    break;
                case "newupdateusercontact": //新-更新用户通讯录
                    state = HttpZGoloBusiness.NewUpdateUserContact(qs);
                    break;
                case "newdeleteusercontact": //新-删除用户通讯录
                    state = HttpZGoloBusiness.NewDeleteUserContact(qs);
                    break;
                case "newaddusercontact": //新-添加用户通讯录
                    state = HttpZGoloBusiness.NewAddUserContact(qs);
                    break;

                    #endregion

                    #region Wifi管理

                case "getuserwifis": //获取用户wifi列表
                    state = HttpZGoloBusiness.GetUserWifis(qs);
                    break;
                case "updateuserwifi": //更新用户wifi信息
                    state = HttpZGoloBusiness.UpdateUserWifi(qs);
                    break;
                case "deleteuserwifi": //删除用户wifi信息
                    state = HttpZGoloBusiness.DeleteUserWifi(qs);
                    break;
                case "adduserwifi": //添加用户wifi信息
                    state = HttpZGoloBusiness.AddUserWifi(qs);
                    break;
                case "switchUserWifi": //切换用户wifi信息
                    state = HttpZGoloBusiness.SwitchUserWifi(qs);
                    break;

                    #endregion

                    #region 绑定

                case "userbinding": //获取用户绑定设备信息
                    state = HttpZGoloBusiness.UserBinding(qs);
                    break;
                case "userbindingcheckreseller": //绑定用户并检测经销商
                    state = HttpNewSpecialBusiness.UserBindingCheckReseller(qs);
                    break;
                case "userunbinding": //用户解除绑定
                    state = HttpZGoloBusiness.UserUnBinding(qs);
                    break;
                case "userbindingtest": //用户绑定--测试用
                    state = HttpZGoloBusiness.UserBindingTest(qs);
                    break;
                case "getuserbindinginfo": //获取用户设备绑定信息
                    state = HttpZGoloBusiness.GetUserBindingInfo(qs);
                    break;
                case "getbindinginfowithcontactremark": //获取设备绑定信息(取通讯录表中的备注)
                    state = HttpZGoloBusiness.GetBindingInfoWithContactRemark(qs);
                    break;
                case "deleteuserbindinginfo": //删除设备绑定信息
                    state = HttpZGoloBusiness.DeleteUserBindingTest(qs);
                    break;
                case "getbindcount": //查询绑定GoloZ的数量
                    state = HttpZGoloBusiness.GetBindCount(qs);
                    break;
                case "modifygender": //修改用户性别
                    state = HttpZGoloBusiness.ModifyGender(qs);
                    break;

                    #endregion

                    #region 频道

                case "gettalkname": //获取频道号码
                    state = HttpZGoloBusiness.GetTalkName(qs);
                    break;
                case "gettalknamewithouttoken"://获取频道号码,不验证token
                    state = HttpZGoloBusiness.GetTalkNameWithOutToken(qs);
                    break;
                case "createmytalk": //创建我的频道
                    state = HttpZGoloBusiness.CreateMyTalk(qs);
                    break;
                case "createmytalkwithouttoken": //创建我的频道,不验证token
                    state = HttpZGoloBusiness.CreateMyTalkWithOutToken(qs);
                    break;
                case "getmyalltalk": //获取我所加入的频道
                    state = HttpZGoloBusiness.GetMyAllTalk(qs);
                    break;
                case "userjointalk": //用户加入频道
                    state = HttpZGoloBusiness.UserJoinTalk(qs);
                    break;
                case "golozuserjointalk": //用户加入频道
                    state = HttpZGoloBusiness.GoloZUserJoinTalk(qs);
                    break;
                case "modifytalkmessage": //修改频道信息
                    state = HttpZGoloBusiness.ModifyTalkMessage(qs);
                    break;
                case "getcurrentradio": //获取所在区域的公共频道
                    state = HttpZGoloBusiness.GetCurrentRadio(qs);
                    break;
                case "getpopradio": //获取热门公共频道
                    state = HttpZGoloBusiness.GetPopRadio(qs);
                    break;
                case "userquittalk": //解除我的频道/退出我所加入的频道
                    state = HttpZGoloBusiness.UserQuitTalk(qs);
                    break;
                case "userquittalkwithouttoken": //解除我的频道/退出我所加入的频道(不验证token)
                    state = HttpZGoloBusiness.UserQuitTalkWithOutToken(qs);
                    break;
                case "userjointalkwithouttoken": //批量用户加入频道(不验证token) 自驾游后台调用
                    state = HttpZGoloBusiness.UserJoinTalkWithOutToken(qs);
                    break;
                case "userjointalkwithtoken": //用户加入频道(验证token)  自驾游app调用
                    state = HttpZGoloBusiness.UserJoinTalkWithToken(qs);
                    break;
                case "searchtalklist":  //查询频道列表 (支持模糊搜索)
                    state = HttpZGoloBusiness.SearchTalkList(qs);
                    break;
                case "createtalk": //创建频道 (验证token,type必需,固定传4. remark必需)
                    state = HttpZGoloBusiness.CreateTalk(qs);
                    break;
                case "modifyimageurl": //修改图片地址
                    state = HttpZGoloBusiness.ModifyImageUrl(qs);
                    break;
                case "getradiostation": //获取电台列表
                    state = HttpZGoloBusiness.GetRadioStation(qs);
                    break;
                case "getactlist": //获取节目单
                    state = HttpZGoloBusiness.ReturnActListJson(qs);
                    break;
                case "newuserjointalk": //新-用户加入频道
                    state = HttpZGoloBusiness.NewUserJoinTalk(qs);
                    break;
                case "switchshareloc": //是否开启频道位置共享
                    state = HttpZGoloBusiness.SwitchShareLoc(qs);
                    break;
                case "newgetcurrentradio": //新-获取所在区域的公共频道
                    state = HttpZGoloBusiness.NewGetCurrentRadio(qs);
                    break;
                case "getradioforphp": //网站获取公共频道
                    state = HttpZGoloBusiness.GetRadioForPhp(qs);
                    break;
                case "gettalkusercount": //获取私人频道聊天人数
                    state = HttpZGoloBusiness.GetTalkUserCount(qs);
                    break;
                case "batchjoincorptalk"://批量加入企业频道 add by jyx 2016.07.20
                    state = HttpZGoloBusiness.BatchJoinCorpTalk(qs);
                    break;
                case "batchdelcorptalk"://批量删除企业频道 add by jyx 2016.07.20
                    state = HttpZGoloBusiness.BatchDelCorpTalk(qs);
                    break;
                case "gettalkuserlist"://获取频道成员列表 add by jyx 2016.07.20
                    state = HttpZGoloBusiness.GetTalkUserList(qs);
                    break;
                case "createvisitortalk"://创建游客频道
                    state = HttpVisitorBusiness.CreateVisitorTalk(qs);
                    break;
                case "searchvisitortalk"://查询游客频道信息
                    state = HttpVisitorBusiness.SearchVisitorTalk(qs);
                    break;
                case "joinvisitortalk"://加入游客频道
                    state = HttpVisitorBusiness.JoinVisitorTalk(qs);
                    break;
                case "exitvisitortalk"://退出/解散游客频道
                    state = HttpVisitorBusiness.ExitVisitorTalk(qs);
                    break;
                case "createtalkwithoutuid"://通过app创建的频道（没有设备,可以解散,可以退出）
                    state = HttpSelfTravelBusiness.CreateTalkWithoutUid(qs);
                    break;
                case "exittalkwithoutuid": //退出 / 解散 app创建的频道
                    state = HttpSelfTravelBusiness.ExitTalkWithoutUid(qs);
                    break;
                case "jointalkwithoutuid":// 加入app创建的频道
                    state = HttpSelfTravelBusiness.JoinTalkWithoutUid(qs);
                    break;
                case "getmytalklistwithoutuid"://获取我的频道列表
                    state = HttpSelfTravelBusiness.GetMyTalkListWithoutUid(qs);
                    break;
                case "updatetalknoticewithoutuid": //修改频道备注
                    state = HttpSelfTravelBusiness.UpdateTalkNoticeWithoutUid(qs);
                    break;
                case "searchtalklistfortravel":  //查询频道列表 (支持模糊搜索)
                    state = HttpSelfTravelBusiness.SearchTalkListForTravel(qs);
                    break;
                    #endregion

                    #region 点赞

                case "querydianzan": //获取用户点赞信息
                    state = HttpZGoloBusiness.QueryDianZan(qs);
                    break;
                case "getparisenum": //获取点赞数量
                    state = HttpZGoloBusiness.GetPariseNum(qs);
                    break;
                case "countpraisebydate": //按天统计点赞数量
                    state = HttpZGoloBusiness.CountPraiseByDate(qs);
                    break;
                case "getcomperepraiseinfo": //获取主持人被赞数量和点赞人
                    state = HttpZGoloBusiness.GetComperePraiseInfo(qs);
                    break;

                    #endregion

                    #region 更改服务器频道信息

                case "updatesysradio": //更改服务器频道信息
                    state = HttpZGoloBusiness.UpdateSysRadio(qs);
                    break;

                    #endregion

                    #region 更新系统频道

                case "querycalltalk": //更改服务器频道信息
                    state = HttpZGoloBusiness.QueryCallTalkInfo(qs);
                    break;

                    #endregion

                    #region 更新了用户设备

                case "updateuserdevice":
                    state = HttpZGoloBusiness.UpdateUserDevice(qs);
                    break;

                    #endregion

                    #region 适配器

                case "userlogin": //用户登录适配信息
                    state = HttpZGoloBusiness.UserLogin(qs);
                    break;
                case "userlogintest": //用户登录适配信息(测试环境)
                    state = HttpZGoloBusiness.UserLoginTest(qs);
                    break;
                case "reqsendcode": //请求发送验证码(注册和找回密码用)
                    state = HttpZGoloBusiness.ReqSendCode(qs);
                    break;
                case "verifycode": //验证输入的验证码
                    state = HttpZGoloBusiness.VerifyCode(qs);
                    break;
                case "resetpass": //找回密码
                    state = HttpZGoloBusiness.ResetPass(qs);
                    break;
                case "setpass": //设置登录密码
                    state = HttpZGoloBusiness.SetPass(qs);
                    break;
                case "register": //注册
                    state = HttpZGoloBusiness.Register(qs);
                    break;
                case "updatenickname": //修改昵称
                    state = HttpZGoloBusiness.UpdateNickname(qs);
                    break;
                case "addaddress": //添加地址
                    state = HttpZGoloBusiness.AddAddress(qs);
                    break;
                case "updateaddress": //修改地址
                    state = HttpZGoloBusiness.UpdateAddress(qs);
                    break;
                case "setdefaultaddress": //设置默认地址
                    state = HttpZGoloBusiness.SetDefaultAddress(qs);
                    break;
                case "deleteaddress": //删除地址
                    state = HttpZGoloBusiness.DeleteAddress(qs);
                    break;
                case "getaddresslist": //获取收货地址列表
                    state = HttpZGoloBusiness.GetAddressList(qs);
                    break;
                case "getallarea": //获取全部省市区
                    state = HttpZGoloBusiness.GetAllArea();
                    break;
                case "getprovince": //获取省份列表
                    state = HttpZGoloBusiness.GetProvince(qs);
                    break;
                case "getcity": //获取城市列表
                    state = HttpZGoloBusiness.GetCity(qs);
                    break;
                case "getregion": //获取区县列表
                    state = HttpZGoloBusiness.GetRegion(qs);
                    break;
                case "carreportlist": //车辆体检报告列表
                    state = HttpZGoloBusiness.CarReportList(qs);
                    break;
                case "gettripwgs": //足迹接口
                    state = HttpZGoloBusiness.GetTripWgs(qs);
                    break;
                case "monthcount": //获取某个月的行程统计数据
                    state = HttpZGoloBusiness.MonthCount(qs);
                    break;
                case "getmileage": //获取指定日期的行程数据
                    state = HttpZGoloBusiness.GetMileage(qs);
                    break;
                case "getgpsinfo": //根据指定行程id获取详细信息
                    state = HttpZGoloBusiness.GetGpsInfo(qs);
                    break;
                case "getgpshisitory": //查询历史轨迹点（行程详情）
                    state = HttpZGoloBusiness.GetGpsHisitory(qs);
                    break;
                case "getrealtimegps": //查询实时轨迹点
                    state = HttpZGoloBusiness.GetRealTimeGps(qs);
                    break;
                case "gettriprecord": //获取实时位置时，查询当前未完成里程的轨迹接口
                    state = HttpZGoloBusiness.GetTripRecord(qs);
                    break;
                case "getdfdatalistnew": //获取实时数据流
                    state = HttpZGoloBusiness.GetdfdatalistNew(qs);
                    break;
                case "getlatestversion": //版本比较并更新
                    state = HttpZGoloBusiness.GetLatestVersion(qs);
                    break;

                    #endregion

                    #region 导航

                case "getnavsetting": //获取导航设置
                    state = HttpZGoloBusiness.GetNavSetting(qs);
                    break;
                case "setnavsetting": //修改导航设置
                    state = HttpZGoloBusiness.SetNavSetting(qs);
                    break;
                case "golonavigate": //输入目的地让golo导航
                    state = HttpZGoloBusiness.GoloNavigate(qs);
                    break;
                case "golonavigatenouid": //输入目的地让golo导航(批量导航到非绑定设备)
                    state = HttpZGoloBusiness.GoloNavigateNouid(qs);
                    break;
                case "golonavigatespecific"://特殊用(自驾游)
                    state = HttpZGoloBusiness.GoloNavigateSpecific(qs);
                    break;
                    #endregion

                    #region 自驾游推送
                case "golopushsong":
                    state = HttpZGoloBusiness.GoloPushSong(qs);
                    break;
                case "golopushttsoralarm":
                    state = HttpZGoloBusiness.GoloPushTTSOrAlarm(qs);
                    break;
                    #endregion

                #region 通用Http转发请求
                case "golocommpush": //通用http转发请求
                    state = HttpZGoloBusiness.GoloCommPush(qs);
                    break;
                #endregion
                #region golo对讲
                case "getalltalkbyouid": //根据ouid返回当前ouid下绑定设备的所有频道
                    state = HttpZGoloBusiness.GetAllTalkByOuid(qs);
                    break;
                case "getdeviceinfobyuid": //根据uid批量返回设备信息(在线/离线状态,上线/离线时间,最后电量)
                    state = HttpZGoloBusiness.GetDeviceInfoByUid(qs);
                    break;
                case "sendstatetodevices": //呼叫设备(发送状态到多个设备)
                    state = HttpZGoloBusiness.SendStateToDevices(qs);
                    break;
                #endregion
                #region 车牌

                case "addplate": //增加车牌
                    state = HttpZGoloBusiness.AddPlate(qs);
                    break;
                case "getplatelist": //获取车牌
                    state = HttpZGoloBusiness.GetPlateList(qs);
                    break;
                case "updateplate": //修改车牌
                    state = HttpZGoloBusiness.UpdatePlate(qs);
                    break;
                case "deleteplate": //删除车牌
                    state = HttpZGoloBusiness.DeletePlate(qs);
                    break;

                    #endregion

                    #region other

                case "getuserlola": //查询用户的经纬度
                    state = HttpZGoloBusiness.GetUserLoLa(qs);
                    break;
                case "getflowbyserial": //剩余流量查询
                    state = HttpZGoloBusiness.GetFlowBySerial(qs);
                    break;
                case "getonlinestatus": //设备在线状态
                    state = HttpZGoloBusiness.GetOnlineStatus(qs);
                    break;
                case "getdeviceinfo": //获取设备信息
                    state = HttpZGoloBusiness.GetDeviceInfo(qs);
                    break;
                case "getuserinfo": //获取用户信息
                    state = HttpZGoloBusiness.GetUserInfo(qs);
                    break;
                case "getuserpraisenum": //获取用户信息和点赞数量
                    state = HttpZGoloBusiness.GetUserPraiseNum(qs);
                    break;
                case "getmyactivities": //获取我参与的活动
                    state = HttpZGoloBusiness.GetMyActivities(qs);
                    break;
                case "getsimbysn"://根据SN号查询sim卡
                    state = HttpZGoloBusiness.GetSimBySN(qs);
                    break;

                    #endregion

                    #region 媒体 V3.0
                case "getlocalmedialist":// 本地音乐
                    state = HttpZGoloBusinessV3.GetLocalMedia(qs);
                    break;
                case "getmyfavoritelist": //我喜欢 媒体列表
                    state = HttpZGoloBusinessV3.GetMyFavoritelist(qs);
                    break;
                case "addmyfavorite": //标记喜欢的媒体
                    state = HttpZGoloBusinessV3.AddMyFavorite(qs);
                    break;
                case "removemyfavorite": //取消标记喜欢的媒体
                    state = HttpZGoloBusinessV3.RemoveMyFavorite(qs);
                    break;
                case "getmysubscribelist": //获取订阅列表
                    state = HttpZGoloBusinessV3.GetMySubscribelist(qs);
                    break;
                case "addmysubscribe": //添加订阅
                    state = HttpZGoloBusinessV3.AddMySubscribe(qs);
                    break;
                case "removemysubscribe"://移除订阅
                    state = HttpZGoloBusinessV3.RemoveMySubscribe(qs);
                    break;
                case "getmymusicmenu":// 我的歌单列表
                    state = HttpZGoloBusinessV3.GetMyMusicMenu(qs);
                    break;
                case "createmusicmenu": //创建歌单
                    state = HttpZGoloBusinessV3.CreateMusicMenu(qs);
                    break;
                case "deletemusicmenu": //删除歌单
                    state = HttpZGoloBusinessV3.DeleteMusicMenu(qs);
                    break;
                case "addmediatomenu": //添加媒体到自定义歌单 (可多选)
                    state = HttpZGoloBusinessV3.AddMediaToMenu(qs);
                    break;
                case "deletemediafrommenu": //从自定义歌单中删除媒体 (可多选)
                    state = HttpZGoloBusinessV3.DeleteMediaFromMenu(qs);
                    break;
                case "sendplaymedia": //播放媒体(可多个)
                    state = HttpZGoloBusinessV3.SendPlayMedia(qs);
                    break;
                case "sendplayalbum": //播放专辑
                    state = HttpZGoloBusinessV3.SendPlayAlbum(qs);
                    break;
                case "sendplaymenu":
                    state = HttpZGoloBusinessV3.SendPlayMenu(qs);
                    break;
                case "sendplaymode"://媒体播放模式
                    state = HttpZGoloBusinessV3.SendPlayModel(qs);
                    break;
                    #endregion

                    #region 电台 V3.0
                case "getsubscriberadio": //获取已订阅的电台列表
                    state = HttpZGoloBusinessV3.GetSubscribeRadio(qs);
                    break;
                case "addsubscriberadio": //订阅电台
                    state = HttpZGoloBusinessV3.AddSubscribeRadio(qs);
                    break;
                case "removesubscriberadio": //取消订阅电台
                    state = HttpZGoloBusinessV3.RemoveSubscribeRadio(qs);
                    break;
                    
                    #endregion

                    #region 导航 V3.0
                case "isonlineisnav":
                    state = HttpZGoloBusinessV3.GetIsOnlineIsNavigation(qs);
                    break;

                    #endregion
            }
            return state;
        }
    }
}
