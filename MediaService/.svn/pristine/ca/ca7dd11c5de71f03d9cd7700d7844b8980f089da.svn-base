using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Data;
using System.Collections.Specialized;
using System.Web;
using System.Collections;

namespace MediaService
{
    class HttpService
    {
        static HttpListener httplistener = null;
        public static async void RecvThread()
        {
            try
            {
                httplistener = new HttpListener();
                httplistener.Prefixes.Add("http://" + MediaService.HttpServersUrl + "/");
                httplistener.Start();
                while (true)
                {
                    var context = await httplistener.GetContextAsync();
                    Task.Run(() => ProcessRequest(context));
                }
            }
            catch (Exception err)
            {
                MediaService.WriteLog(" RecvThread错误：" + err.Message + "   " + MediaService.HttpServersUrl, MediaService.wirtelog);
                Thread.Sleep(5000);
                RecvThread();
            }
        }
        public static void ProcessRequest(HttpListenerContext Context)
        {
            try
            {
                string client = Context.Request.RemoteEndPoint.Address.ToString();
                Context.Response.StatusCode = 200;
                Context.Request.Headers.Add("Content-Type", "text/html;charset=UTF-8");
                string[] path = Context.Request.Url.AbsolutePath.Split('/');
                string method = Context.Request.HttpMethod;
                MediaService.WriteLog(" RecvThread接收： method= " + method + " client=" + client + "  " + Context.Request.Url.AbsoluteUri + "", MediaService.wirtelog);
                string state = "false";
                if (path.Length == 3)
                {
                    if (Context.Request.HttpMethod == "GET")
                    {
                        switch (path[1])
                        {
                            case "user":
                                state = HttpAction.httpUserAction(path[2], Context.Request.QueryString);
                                break;
                            case "kf":
                                state = HttpAction.httpKfAction(path[2], HttpUtility.ParseQueryString(Context.Request.Url.Query, Encoding.UTF8));
                                break;
                            case "sys":
                                state = HttpAction.httpSysAction(path[2], Context.Request.QueryString);
                                break;
                            case "wifi":
                                state = HttpAction.httpWifiAction(path[2], Context.Request.QueryString);
                                break;
                            case "golo":
                                state = HttpAction.httpGoloAction(path[2], HttpUtility.ParseQueryString(Context.Request.Url.Query, Encoding.UTF8));
                                break;
                            case "z":
                                state = HttpAction.httpZGoloAction(path[2], HttpUtility.ParseQueryString(Context.Request.Url.Query, Encoding.UTF8));
                                break;

                        }
                    }
                    else
                    {
                        string rawUrl = System.Web.HttpUtility.UrlDecode(Context.Request.RawUrl);
                        int paramStartIndex = rawUrl.IndexOf('?');
                        if (paramStartIndex > 0)
                            rawUrl = rawUrl.Substring(0, paramStartIndex);
                        else if (paramStartIndex == 0)
                            rawUrl = "";
                        if (string.Compare(rawUrl, "/file/", true) == 0)
                        {
                            state = HttpAction.HttpFileGoloAction(Context);
                        }
                        else
                        {
                            StreamReader reader = new StreamReader(Context.Request.InputStream, Encoding.UTF8);
                            string poststr = reader.ReadToEnd();
                            reader.Close();
                            MediaService.WriteLog(" RecvThread接收： poststr=" + poststr, MediaService.wirtelog);
                            NameValueCollection qs = HttpUtility.ParseQueryString(poststr, Encoding.UTF8);
                            switch (path[1])
                            {
                                case "user":
                                    state = HttpAction.httpUserAction(path[2], qs);
                                    break;
                                case "kf":
                                    state = HttpAction.httpKfAction(path[2], qs);
                                    break;
                                case "sys":
                                    state = HttpAction.httpSysAction(path[2], qs);
                                    break;
                                case "wifi":
                                    state = HttpAction.httpWifiAction(path[2], qs);
                                    break;
                                case "golo":
                                    state = HttpAction.httpGoloAction(path[2], qs);
                                    break;
                                case "z":
                                    state = HttpAction.httpZGoloAction(path[2], qs);
                                    break;
                                case "vehicle":
                                    state = HttpAction.goloZVehicleAction(path[2], qs);
                                    break;
                                //case "receiveimg":
                                //    state = HttpAction.httpReceiveFileAction(path[2], qs, Context.Request.InputStream);
                                //    break;
                            }
                        }
                    }
                }
                else if (path.Length == 2)
                {
                    if (Context.Request.HttpMethod == "GET")
                    {
                        switch (path[1])
                        {
                            case "log":
                                state = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>日志</title></head><body>" + MediaService.log + "</body></html>"; ;
                                break;
                            case "favicon.ico":
                                state = "false";
                                break;
                        }
                    }
                }
                StreamWriter writer = new StreamWriter(Context.Response.OutputStream);
                writer.WriteLine(state);
                writer.Close();
                Context.Response.Close();
            }
            catch (Exception err)
            {
                MediaService.WriteLog("执行异常：" + err.ToString(), MediaService.wirtelog);
            }
        }

        #region 写错误JSON
        public static string WriteErrorJson(string message)
        {
            return "{\"status\":false,\"message\":\"" + message + "\"}";
        }
        #endregion
    }
}
