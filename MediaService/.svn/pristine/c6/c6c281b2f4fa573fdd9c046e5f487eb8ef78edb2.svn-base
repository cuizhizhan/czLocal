using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 频道呼叫重复发送信息
    /// </summary>
    public class CallBackInfo
    {
        /// <summary>
        /// 发送者uid
        /// </summary>
        public int Uid { get; private set; }
        /// <summary>
        /// appid
        /// </summary>
        public int Appid { get; set; }
        /// <summary>
        /// 待发送uid
        /// </summary>
        public List<int> SendUid { get; private set; }
        /// <summary>
        /// 接受者uid
        /// </summary>
        public List<string> SendSN { get; private set; }
        /// <summary>
        /// 发送频道id
        /// </summary>
        public int Tid { get; private set; }
        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTime SendTime { get; private set; }
        /// <summary>
        /// 是否接受
        /// </summary>
        public bool IsReceive { get; set; }
        /// <summary>
        /// 要发送的Buffer
        /// </summary>
        public byte[] Buffer { get; private set; }
        /// <summary>
        /// 接受者uid
        /// </summary>
        private List<int> ReceiveUid = new List<int>();
        /// <summary>
        /// 被呼叫用户
        /// </summary>
        public string CalledUid
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in SendUid)
                {
                    sb.Append(item);
                    if (item != SendUid.LastOrDefault())
                        sb.Append(",");
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// 被呼叫用户
        /// </summary>
        public string CalledSN
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in SendSN)
                {
                    sb.Append(item);
                    if (item != SendSN.LastOrDefault())
                        sb.Append(",");
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="senduid"></param>
        /// <param name="tid"></param>
        /// <param name="buffer"></param>
        public CallBackInfo(int uid, int appid, List<int> senduid, int tid, byte[] buffer,List<string> sendsn)
        {
            Uid = uid;
            Appid = appid;
            SendUid = senduid;
            Tid = tid;
            Buffer = buffer;
            SendTime = DateTime.Now;
            IsReceive = false;
            SendSN = sendsn;
        }
        /// <summary>
        /// 
        /// </summary>
        public void SetReceive(int uid)
        {
            if (!ReceiveUid.Contains(uid))
                ReceiveUid.Add(uid);
        }
        /// <summary>
        /// 检测是否已经发送完成
        /// </summary>
        public bool IsCompleted()
        {
            return SendUid.Except(ReceiveUid).Count() == 0 || DateTime.Now.Subtract(SendTime).TotalSeconds > 3 * 60;
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        public void SendBuffer()
        {
            if (IsCompleted() || DateTime.Now.Subtract(SendTime).TotalSeconds < 5)
                return;
            var task = Task.Factory.StartNew(() =>
            {
                SendUid.Except(ReceiveUid).ToList<int>().ForEach(x => Send(x));
            });
        }

        //发送
        private void Send(int uid)
        {
            bool calltrue = false;
            UserObject uo = null;
            if (MediaService.userDic.TryGetValue(uid, out uo))
            {
                try
                {
                    if (uo.socket[Appid] != null)
                    {
                        uo.socket[Appid].Send(Buffer, 0, Buffer.Length, SocketFlags.None);
                        calltrue = true;
                    }
                }
                catch { }
                if (!calltrue)
                {
                    try
                    {
                        if (uo.socket[8] != null)
                        {
                            uo.socket[8].Send(Buffer, 0, Buffer.Length, SocketFlags.None);
                            calltrue = true;
                        }
                    }
                    catch { }
                }
                if (calltrue)
                    MediaService.WriteLog("1085 群呼频道成员重复发送：uid=" + Uid + "&to senduid=" + uid + "&tid=" + Tid, true);
            }
        }
    }
}
