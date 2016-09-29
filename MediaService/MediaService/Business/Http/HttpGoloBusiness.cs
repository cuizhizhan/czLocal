using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Data;
using System.Collections.Specialized;
using System.Collections;
using System.Configuration;
namespace MediaService
{
    class HttpGoloBusiness
    {
        #region 报警播报
        public static string CarAlarm(NameValueCollection qs)
        {
            string recv = "{\"status\":false}";
            if (qs != null && qs["sn"] != null && qs["message"] != null && qs["appid"] != null && qs["alarmtype"] != null)
            {
                int appid = Int32.Parse(qs["appid"].ToString());
                string sn = qs["sn"].ToString().Replace("'", "");
                string message = qs["message"].ToString();
                string alarmtype = qs["alarmtype"].ToString().Replace("'", "");
                try
                {
                    DateTime dt = DateTime.Now;
                    StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\alarm\\" + dt.ToString("yyyyMMdd") + ".txt", true);
                    sw.WriteLine(dt.ToString("HH:mm:ss") + " sn=" + sn + "  alarmtype=" + alarmtype + "   message=" + message);
                    sw.Close();
                }
                catch { }
                MediaService.WriteLog("接收到报警播报 ：sn=" + sn + "  alarmtype=" + alarmtype + "   message=" + message, MediaService.wirtelog);
                object obj = SqlHelper.ExecuteScalar("select uid from [app_users] where glsn='" + sn + "'");
                if (obj != null)
                {
                    StringBuilder sb = new StringBuilder("{\"status\":true,\"type\":108,\"message\":{\"state\":true,\"alarmtype\":" + alarmtype + "}");
                    //int count = sb.Length;
                    //if (alarmtype == "31")
                    //{
                    //    sb.Append(alarmtype);
                    //    sb.Append(",\"txt\":\"咕噜提醒,您已驶出了电子围栏！\"}");
                    //}
                    //else if (alarmtype == "32")
                    //{
                    //    sb.Append(alarmtype);
                    //    sb.Append(",\"txt\":\"咕噜提醒,您已驶入了电子围栏！\"}");
                    //}
                    //else if (alarmtype == "1")
                    //{
                    //    sb.Append(alarmtype);
                    //    sb.Append(",\"txt\":\"咕噜检测到您的车辆有故障提示,请安全停车后确认！\"}");
                    //}
                    //if (sb.Length > count)
                    //{
                    List<int> uidlist = new List<int>();
                    uidlist.Add(Int32.Parse(obj.ToString()));
                    PublicClass.SendToOnlineUserList(null, sb.ToString(), "", uidlist, 99, 0, CommType.pushMessageToUser, appid);
                    //}
                }
                recv = "{\"status\":true}";
            }
            return recv;
        }
        #endregion

        #region 获取代理服务地址
        public static string GetProxy(NameValueCollection qs)
        {
            var ip = new IpAddress();
            try
            {
                Proxies config = Proxies.Default;
                if (config != null)
                {
                    int count = config.ProxyCol.Count;
                    int i = new Random().Next(count);
                    Proxy proxy = config.ProxyCol[i];
                    ip.ipaddress = proxy.IpAddress;
                    ip.port = proxy.Port;
                }
            }
            catch (Exception e)
            {
                MediaService.WriteLog("获取代理服务地址出错：" + e.Message, MediaService.wirtelog);
            }
            return ip.ToString();
        }
        #endregion

        #region 用户Ip验证
        public static bool UserIpVerification(int uid, string ip)
        {
            bool state = false;
            object obj = SqlHelper.ExecuteScalar("select uid from [app_users] where uid=" + uid);
            if (obj != null)
            {
                if (ip == "127.0.0.1")
                {
                    state = true;
                }
                else
                {
                    UserObject uo = null;
                    if (MediaService.userDic.TryGetValue(uid, out uo))
                    {
                        if (uo.ip != null && uo.ip != "")
                        {
                            if (uo.ip == ip)
                                return true;
                        }
                    }
                }
            }
            return state;
        }
        #endregion

    }

    public class IpAddress
    {
        public string ipaddress { get; set; }
        public int port { get; set; }

        public override string ToString()
        {
            return "{\"ipaddress\":\"" + ipaddress + "\",\"port\":" + port + "}";
        }
    }
    public class Proxies : ConfigurationSection
    {

        //服务模型配置节实例
        private static Proxies _ServiceModelConfig = (Proxies)ConfigurationManager.GetSection("Proxies");
        public static Proxies Default
        {
            get { return _ServiceModelConfig; }
        }

        [ConfigurationProperty("", IsDefaultCollection = true)]
        public ProxyCollection ProxyCol
        {
            get
            {
                return (ProxyCollection)base[""];
            }
        }
    }
    public class ProxyCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new Proxy();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Proxy)element).IpAddress;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMap;
            }
        }

        protected override string ElementName
        {
            get
            {
                return "Proxy";
            }
        }

        public Proxy this[int index]
        {

            get { return (Proxy)BaseGet(index); }
        }
    }
    public class Proxy : ConfigurationElement
    {
        [ConfigurationProperty("ipaddress", IsRequired = true)]
        public string IpAddress
        {
            get { return this["ipaddress"].ToString(); }
            set { this["ipaddress"] = value; }
        }

        [ConfigurationProperty("port", IsRequired = true)]
        public int Port
        {
            get
            {
                int port;
                int.TryParse(this["port"].ToString(), out port);
                return port;
            }
            set { this["port"] = value; }
        }
    }
}

