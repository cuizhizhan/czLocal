using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace MediaService
{
    public class GoloZVehicleAction
    {
        public static string GetGoloZVehicleRecv(string path, NameValueCollection qs)
        {
            string state = "";
            switch (path)
            {
                #region 群组
                case "gettalkname":   //获取频道号码
                    state = HttpZGoloVehicleBusiness.GetTalkName(qs);
                    break;
                case "creategroup":   //创建车群组
                    state = HttpZGoloVehicleBusiness.CreateGroup(qs);
                    break;
                case "dropgroup":   //删除群组
                    state = HttpZGoloVehicleBusiness.DropGroup(qs);
                    break;
                case "removegroupmember":   //删除群组组员
                    state = HttpZGoloVehicleBusiness.RemoveGroupMember(qs);
                    break;
                case "addgroupmember":   //添加群组组员
                    state = HttpZGoloVehicleBusiness.AddGroupMember(qs);
                    break;
                case "querygroupmemberinfo":   //查询群组组员信息
                    state = HttpZGoloVehicleBusiness.QueryGroupMemberInfo(qs);
                    break;
                #endregion
            }
            return state;
        }
    }
}
