using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// MessageCode信息描述
    /// </summary>
    public static class MessageCodeDiscription
    {
        /// <summary>
        /// MessageCode描述缓存
        /// </summary>
        public static Dictionary<MessageCode, string> MessageCodeDisption = new Dictionary<MessageCode, string>();

        /// <summary>
        /// 取MessageCode的描述信息
        /// </summary>
        /// <param name="code">MessageCode值</param>
        /// <returns></returns>
        public static string GetMessageCodeDiscription(MessageCode code)
        {
            if (!MessageCodeDisption.Any())
            {
                LoadMessageCodeDiscription();
            }
            string disc = "";
            MessageCodeDisption.TryGetValue(code, out disc);
            return disc;
        }

        //初始化加载MessageCode描述信息
        private static void LoadMessageCodeDiscription()
        {
            MessageCodeDisption.Clear();
            var values = Enum.GetValues(typeof(MessageCode));
            foreach (MessageCode value in values)
            {
                string disc = GetAttribute.GetDescriptValue(typeof(MessageCode), (int)value);

                //start 服务启动的时候,此处会报错 old
                //MessageCodeDisption.Add(value, disc);
                //end
                if (!MessageCodeDisption.ContainsKey(value))
                    MessageCodeDisption.Add(value, disc);
            }
        }
    }
}
