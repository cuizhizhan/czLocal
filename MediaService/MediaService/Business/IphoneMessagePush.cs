using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Data;

namespace MediaService
{
    class IphoneMessagePush
    {
        public static void IosPushThread()
        {
            while (true)
            {
                try
                {
                    while (MediaService.iosSendMessage.Count > 0)
                    {
                        IosSend iosSend = MediaService.iosSendMessage[0];
                        MediaService.iosSendMessage.RemoveAt(0);

                        if (iosSend.mtype == 0)
                        {
                            iosSend.message += " 发来一条消息";
                        }
                        else if (iosSend.mtype == 6)
                        {
                            iosSend.message += " 发来一段语音";
                        }
                        else if (iosSend.mtype == 4)
                        {
                            iosSend.message += " 发来一张图片";
                        }
                        else if (iosSend.mtype == 5)
                        {
                            iosSend.message += " 发来一段视频";
                        }
                        else if (iosSend.mtype == 1)
                        {
                            iosSend.message += " 发来一个位置";
                        }
                        else if (iosSend.mtype == 2)
                        {
                            iosSend.message += " 发来一个名片";
                        }
                        else if (iosSend.mtype == 3)
                        {
                            iosSend.message += " 发来求救信息";
                        }
                        else if (iosSend.mtype == 7)
                        {
                            iosSend.message += " 发来一个附件";
                        }
                        else if (iosSend.mtype == 8)
                        {
                            iosSend.message += " 发来一个通知";
                        }
                        if (iosSend.userlist.Length != 0)
                        {
                            SqlHelper.ExecuteNonQuery("update [app_ios_token] set msgnum=msgnum+1 where appid=" + iosSend.appid + " and (" + iosSend.userlist + ")");
                            DataTable iosdt = SqlHelper.ExecuteTable("select token,msgnum from [app_ios_token] where appid=" + iosSend.appid + " and (" + iosSend.userlist + ")");
                            foreach (DataRow dr in iosdt.Rows)
                            {
                                string iostoken = dr[0].ToString();
                                string badge = dr[1].ToString();
                                IphoneMessagePush.IphoneMessagePushToUser(iostoken, iosSend.message, "default", badge, iosSend.appid);
                            }
                        }
                    }
                    Thread.Sleep(100);
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("IPHONE推送线程异常：" + err.ToString(), false);
                }
            }
        }
        private static void IphoneMessagePushToUser(string DeviceToken, string alert, string sound, string badge, int appid)
        {
            byte[] buffer = new byte[1024];
            int index = 0;
            Buffer.BlockCopy(new byte[] { 0x00 }, 0, buffer, index, 1);
            index += 1;
            byte[] deviceTokenSize = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Convert.ToInt16(DeviceToken.Length / 2)));
            Buffer.BlockCopy(deviceTokenSize, 0, buffer, index, 2);
            index += 2;
            for (int k = 0; k < DeviceToken.Length / 2; k++)
            {
                buffer[index] = byte.Parse(DeviceToken.Substring(k * 2, 2), System.Globalization.NumberStyles.HexNumber);
                index++;
            }
            //byte[] payload = Encoding.UTF8.GetBytes("{\"aps\":{\"alert\":\"" + alert + "\",\"url\":\"http://www.golo365.com\",\"sound\":\"" + sound + "\",\"badge\":" + badge + "}}");
            byte[] payload = Encoding.UTF8.GetBytes("{\"aps\":{" + alert + ",\"sound\":\"" + sound + "\",\"badge\":" + badge + "}}");
            byte[] payloadSize = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(Convert.ToInt16(payload.Length)));
            Buffer.BlockCopy(payloadSize, 0, buffer, index, 2);
            index += 2;
            Buffer.BlockCopy(payload, 0, buffer, index, payload.Length);
            index += payload.Length;

            if (IphonePushClient(appid))
            {
                try
                {
                    MediaService.sslStream[appid].Write(buffer, 0, index);
                    //MediaService.sslStream[appid].Flush();
                    try
                    {
                        MediaService.sslStream[appid].ReadTimeout = 100;
                        int len = MediaService.sslStream[appid].Read(buffer, 0, buffer.Length);
                        string recv = "len=" + len;
                        for (int i = 0; i < len; i++)
                        {
                            recv += " " + i + "：" + buffer[i];
                        }
                        MediaService.WriteLog("Iphone推送返回：" + recv, MediaService.wirtelog);
                    }
                    catch { }
                }
                catch (Exception err)
                {
                    MediaService.sslStream[appid].Close();
                    MediaService.sslStream[appid] = null;
                    MediaService.WriteLog("发送Iphone推送数据异常：" + err.Message, MediaService.wirtelog);
                }
            }
        }

        private static bool IphonePushClient(int appid)
        {
            try
            {
                if (MediaService.sslStream[appid] == null)
                {
                    DataRow[] dr = MediaService.allapp.Select("id=" + appid);
                    if (dr.Length > 0)
                    {
                        string ioscerts = dr[0]["ioscerts"].ToString();
                        string iospwd = dr[0]["iospwd"].ToString();
                        MediaService.WriteLog("IphonePush :" + ioscerts + "   " + iospwd, MediaService.wirtelog);
                        if (ioscerts != "" && iospwd != "")
                        {
                            string machineName = "gateway.sandbox.push.apple.com";
                            TcpClient IosPush = new TcpClient(machineName, 2195);
                            MediaService.sslStream[appid] = new SslStream(IosPush.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                            X509CertificateCollection certs = new X509CertificateCollection();
                            X509Certificate cert = new X509Certificate2(AppDomain.CurrentDomain.BaseDirectory + "\\" + ioscerts, iospwd, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                            certs.Add(cert);
                            try
                            {
                                MediaService.sslStream[appid].AuthenticateAsClient(machineName, certs, SslProtocols.Tls, false);
                            }
                            catch (AuthenticationException e)
                            {
                                MediaService.WriteLog("AuthenticateAsClient验证异常：" + e.Message, MediaService.wirtelog);
                                if (e.InnerException != null)
                                {
                                    MediaService.WriteLog("AuthenticateAsClient Inner exception 验证异常：" + e.InnerException.Message, MediaService.wirtelog);
                                }
                                IosPush.Close();
                                MediaService.sslStream[appid].Close();
                                MediaService.sslStream[appid] = null;
                                return false;
                            }
                            MediaService.WriteLog("成功创建IphonePush推送连接", MediaService.wirtelog);
                        }
                        else
                        {
                            MediaService.WriteLog("创建IphonePush推送连接失败，p12证书或密码为空", MediaService.wirtelog);
                            return false;
                        }
                    }
                    else
                    {
                        MediaService.WriteLog("创建IphonePush推送连接失败，应用不存在 appid=" + appid, MediaService.wirtelog);
                        return false;
                    }
                }
                return true;
            }
            catch (Exception err)
            {
                MediaService.WriteLog("创建sslStream连接错误：" + err.Message, MediaService.wirtelog);
                return false;
            }
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            else
            {
                MediaService.WriteLog("ValidateServerCertificate验证异常：" + sslPolicyErrors, MediaService.wirtelog);
                return false;
            }
        }
    }

    public class IosSend
    {
        public string userlist = "";
        public string message = "";
        public int mtype = 0;
        public int appid = 0;

        public IosSend(string _userlist, string _message, int _mtype, int _appid)
        {
            userlist = _userlist;
            message = _message;
            mtype = _mtype;
            appid = _appid;
        }
    }
}
