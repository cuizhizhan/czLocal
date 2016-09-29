using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
/// <summary>
    /// 获取枚举值的描述
    /// </summary>
    public static class GetAttribute
    {
        /// <summary>
        /// 获取enum的值的描述
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetDescriptValue(Type type, int value)
        {
            string str = default(string);
            object enumInstance = type.Assembly.CreateInstance(type.FullName);
            FieldInfo[] fieldInfos = type.GetFields();
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                DescriptAttribute[] attribute =
                    (DescriptAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptAttribute), true);
                if (attribute.FirstOrDefault() != null &&
                    value == Convert.ToInt32(fieldInfo.GetValue(enumInstance)))
                {
                    str = attribute.FirstOrDefault().Descript;
                    return str;
                }
                else
                    str = value.ToString();
            }
            return str;
        }
    }
}
