using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 频道对讲缓存
    /// </summary>
    public class TalkMessageCache
    {
        public static void SetTalkMessage(string server, int tid, TalkMessage talk)
        {
            CacheHelper.Set(server + "_TalkMessage_" + tid, talk);
        }
        public static bool DeleteTalkMessage(string server, int tid)
        {
            return CacheHelper.Delete(server + "_TalkMessage_" + tid);
        }

        public static TalkMessage GetTalkMessage(string server, int tid)
        {
            return CacheHelper.Get<TalkMessage>(server + "_TalkMessage_" + tid);
        }

        public static bool IsExists(string server, int tid)
        {
            return CacheHelper.IsExists(server + "_TalkMessage_" + tid);
        }

        public static List<int> GetExistsTalkMessage(string server, List<int> talks)
        {
            List<int> tids = new List<int>();
            talks.ForEach(x =>
            {
                if (IsExists(server, x))
                    tids.Add(x);

            });
            return tids;
        }
    }
}
