using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// User用户缓存
    /// </summary>
    public class UserCache
    {
        public static void SetUser(string server, int uid, UserObject user)
        {
            CacheHelper.Set(server + "_User_" + uid, user);
        }

        public static bool DeleteUser(string server, int uid)
        {
            return CacheHelper.Delete(server + "_User_" + uid);
        }

        public static UserObject GetUser(string server, int uid)
        {
            return CacheHelper.Get<UserObject>(server + "_User_" + uid);
        }

        public static bool IsExists(string server, int uid)
        {
            return CacheHelper.IsExists(server + "_User_" + uid);
        }

        public static List<int> GetExistsUser(string server, List<int> users)
        {
            List<int> uids = new List<int>();
            users.ForEach(x =>
                {
                    if (IsExists(server, x))
                        uids.Add(x);

                });
            return uids;
        }
    }
}
