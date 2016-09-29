using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace MediaService
{
    class UdpServer
    {
        private static Socket server;

        public static void UdpServerStart()
        {
            server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            server.Bind(new IPEndPoint(IPAddress.Any, 5102));

            while (true)
            {
                try
                {
                    var client = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
                    var buff = new byte[512];
                    int read = server.ReceiveFrom(buff, ref client);
                    IPEndPoint ipEndPoint = (IPEndPoint)client;

                    MediaService.WriteLog("udp请求：" + read + "字节，客户端ip：" + client, MediaService.wirtelog);
                    string json = "{\"status\":true,\"type\"1,\"ipandport\":\"" + client + "\"}";
                    server.SendTo(Encoding.ASCII .GetBytes(json), client);
                }
                catch (Exception err)
                {
                    MediaService.WriteLog("udp请求异常：" + err.Message, MediaService.wirtelog);
                }
            }
        }
    }
}
