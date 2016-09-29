using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Threading;

namespace MediaService
{
    class TalkMessagePush
    {
        public static void TalkPushThread()
        {
            byte[] buffer = new byte[MediaService.bufferSize];
            List<int> ulist = new List<int>();
            while (true)
            {
                try
                {
                    while (MediaService.talkSendMessage.Count > 0)
                    {
                        TalkSend talkSend = MediaService.talkSendMessage[0];
                        MediaService.talkSendMessage.RemoveAt(0);


                        DataTable dt = SqlHelper.ExecuteTable("select uid from [wy_talkuser] where tid=" + talkSend.tid + " and uid!=" + talkSend.senduid);
                        ulist.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            ulist.Add(Int32.Parse(dr["uid"].ToString()));
                        }
                        if (ulist.Count > 0)
                        {
                            object obj = SqlHelper.ExecuteNonQuery("insert [wy_talkmessage] (tid,senduid,message) values (" + talkSend.tid + "," + talkSend.senduid + ",'" + talkSend.message + "')");
                            talkSend.message.Insert(1, "\"status\":true,");
                            string sql = PublicClass.SendToUserList(buffer, talkSend.message.ToString(), talkSend.offmessage, ulist, talkSend.mtype, 0, CommType.recvTalkMessage, talkSend.appid);

                            if (sql.Length > 0)
                            {
                                SqlHelper.ExecuteNonQuery("update [wy_talkuser] set noread=noread+1 where tid=" + talkSend.tid + " and (" + sql + ")");
                            }
                        }
                    }
                    Thread.Sleep(100);
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("Talk推送线程异常：" + err.ToString(), false);
                }
            }
        }
    }

    public class TalkSend
    {
        public StringBuilder message;
        public string tid = "";
        public int mtype = 0;
        public int senduid = 0;
        public string offmessage = "";
        public int appid = 0;

        public TalkSend(StringBuilder _message, string _offmessage, int _mtype, string _tid, int _senduid, int _appid)
        {
            message = _message;
            tid = _tid;
            mtype = _mtype;
            senduid = _senduid;
            offmessage = _offmessage;
            appid = _appid;
        }
    }
}
