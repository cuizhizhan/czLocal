using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.IO;
using System.Xml;
using System.Collections;

namespace MediaService
{
    /// <summary>
    /// 针对当前交易系统，定制了内部格式
    /// </summary>
    public static class JsonHelper
    {
        #region JavaScriptSerialize
        /// <summary>
        /// 对象转换成json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string JavaScriptSerialize<T>(T obj)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(obj);
        }
        /// <summary>
        /// json字符串转换成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T JavaScriptSerialize<T>(string json)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = Int32.MaxValue;    //设置为int的最大值 
            return serializer.Deserialize<T>(json);
        }
        #endregion
    }

    public class JsonCommModel<T>
    {
        public JsonCommModel(bool status, T t)
        {
            this.status = status;
            this.data = t;
        }
        public bool status { get; protected set; }
        public T data { get; protected set; }
    }

    public class JsonCommModelWithList<T>
    {
        public JsonCommModelWithList(bool status, T t)
        {
            this.status = status;
            this.list = t;
        }
        public bool status { get; protected set; }
        public T list { get; protected set; }
    }

    public class AppJsonResultModel<T>
    {
        public AppJsonResultModel(int code, string msg, T t)
        {
            this.code = code;
            this.msg = msg;
            this.data = t;
        }

        public int code { get; protected set; }
        public string msg { get; protected set; }
        public T data { get; protected set; }
    }
}
