namespace MediaService
{
    /**
 *　　　　　　　　┏┓　　　┏┓+ +
 *　　　　　　　┏┛┻━━━┛┻┓ + +
 *　　　　　　　┃　　　　　　　┃ 　
 *　　　　　　　┃　　　━　　　┃ ++ + + +
 *　　　　　　 ████━████ ┃+
 *　　　　　　　┃　　　　　　　┃ +
 *　　　　　　　┃　　　┻　　　┃
 *　　　　　　　┃　　　　　　　┃ + +
 *　　　　　　　┗━┓　　　┏━┛
 *　　　　　　　　　┃　　　┃　　　　　　　　　　　
 *　　　　　　　　　┃　　　┃ + + + +
 *　　　　　　　　　┃　　　┃　　　　Code is far away from bug with the animal protecting　　　　　　　
 *　　　　　　　　　┃　　　┃ + 　　　　神兽保佑,代码无bug　　
 *　　　　　　　　　┃　　　┃
 *　　　　　　　　　┃　　　┃　　+　　　　　　　　　
 *　　　　　　　　　┃　 　　┗━━━┓ + +
 *　　　　　　　　　┃ 　　　　　　　┣┓
 *　　　　　　　　　┃ 　　　　　　　┏┛
 *　　　　　　　　　┗┓┓┏━┳┓┏┛ + + + +
 *　　　　　　　　　　┃┫┫　┃┫┫
 *　　　　　　　　　　┗┻┛　┗┻┛+ + + +
 */
    /// <summary>
    /// 错误码
    /// </summary>
    public enum MessageCode : short
    {
        /// <summary>
        /// 成功
        /// </summary>
        [DescriptAttribute("成功")]
        Success = 0,

        /// <summary>
        /// 登陆失败
        /// </summary>
        [DescriptAttribute("登陆失败")]
        LoginFail=80,
        /// <summary>
        /// 更新数据失败
        /// </summary>
        [DescriptAttribute("更新数据失败")]
        UpdateFaild = 96,
        /// <summary>
        /// 删除数据失败
        /// </summary>
        [DescriptAttribute("删除数据失败")]
        DeleteFaild = 97,
        /// <summary>
        /// 新增数据失败
        /// </summary>
        [DescriptAttribute("新增数据失败")]
        InsertFaild = 98,
        /// <summary>
        /// 未知错误
        /// </summary>
        [DescriptAttribute("未知错误")]
        DefaultError = 99,

        /// <summary>
        /// 传入的关键字缺失
        /// </summary>
        [DescriptAttribute("传入的关键字缺失")]
        MissKey = 100,
        /// <summary>
        /// Token无效
        /// </summary>
        [DescriptAttribute("Token无效")]
        TokenOverdue = 101,
        /// <summary>
        /// 格式有误
        /// </summary>
        [DescriptAttribute("格式有误")]
        FormatError = 102,
        /// <summary>
        /// 查询设备不在线
        /// </summary>
        [DescriptAttribute("查询设备不在线")]
        DeviceOutLine = 103,
        /// <summary>
        /// 未找到对应的设备
        /// </summary>
        [DescriptAttribute("未找到对应的设备")]
        DeviceNotExist = 104,
        /// <summary>
        /// sn号已经被绑定
        /// </summary>
        [DescriptAttribute("SN号已经被绑定")]
        SNExist = 105,
        /// <summary>
        /// 设备未绑定
        /// </summary>
        [DescriptAttribute("设备未绑定")]
        DeviceNotBinding = 106,

        /// <summary>
        /// 绑定失败,因为经销商不匹配
        /// </summary>
        [DescriptAttribute("绑定失败，请联系购买的经销商获取支持")]
        BindingFaildOfReSellerError=107,

        /// <summary>
        /// 频道号分配失败
        /// </summary>
        [DescriptAttribute("频道号分配失败")]
        TalkAllocationFaild = 200,
        /// <summary>
        /// 频道创建失败
        /// </summary>
        [DescriptAttribute("频道创建失败")]
        TalkCreateFaild = 201,
        /// <summary>
        /// 频道号无效
        /// </summary>
        [DescriptAttribute("频道号无效")]
        TalkInvalid = 202,
        /// <summary>
        /// 频道已被占用
        /// </summary>
        [DescriptAttribute("频道已被占用")]
        TalkExist = 203,
        /// <summary>
        /// 频道验证失败
        /// </summary>
        [DescriptAttribute("频道验证失败")]
        TalkVerificationFaild = 204,
        /// <summary>
        /// 创建频道数量过多
        /// </summary>
        [DescriptAttribute("创建频道数量过多")]
        TalkFull = 205,
        /// <summary>
        /// 频道不存在
        /// </summary>
        [DescriptAttribute("频道不存在")]
        TalkNotExist = 206,
        /// <summary>
        /// 频道加入失败
        /// </summary>
        [DescriptAttribute("频道加入失败")]
        TalkJoinFaild = 207,

        /// <summary>
        /// 没有权限,必须是频道创建者
        /// </summary>
        [DescriptAttribute("没有权限,必须是频道创建者")]
        NoAuthMustCreater = 208,

        /// <summary>
        /// 查询失败
        /// </summary>
        [Descript("查询失败")]
        SearchFaild=209,

        /// <summary>
        /// 新增用户已存在
        /// </summary>
        [DescriptAttribute("新增用户已存在")]
        ContactExist = 300,

        /// <summary>
        /// 用户不存在
        /// </summary>
        [DescriptAttribute("用户不存在")]
        UserNotExist = 301,

        /// <summary>
        /// Wifi不存在
        /// </summary>
        [DescriptAttribute("Wifi不存在")]
        WifiNotExist = 400,

        /// <summary>
        /// IP验证失败
        /// </summary>
        [DescriptAttribute("IP验证失败")]
        IPVerifiFailed=500,

        /// <summary>
        /// 歌单不存在
        /// </summary>
        [Descript("歌单不存在")]
        MenuNotExist=500,

        /// <summary>
        /// 当前设备已经在频道中
        /// </summary>
        [DescriptAttribute("当前设备已经在频道中")]
        UidExistedInWyTalk=501,
    }

    /// <summary>
    /// 频道类型
    /// </summary>
    public enum EnumTalkType
    {
        SelfTravel=3,
        /// <summary>
        /// 游客频道
        /// </summary>
        Visitor = 5
    }

    /// <summary>
    /// 对讲模式
    /// </summary>
    public enum EnumTalkMode
    {
        /// <summary>
        /// 一般对讲模式
        /// </summary>
        Nomal = 0,

        /// <summary>
        /// 实时对讲模式
        /// </summary>
        JIT = 1
    }
}
