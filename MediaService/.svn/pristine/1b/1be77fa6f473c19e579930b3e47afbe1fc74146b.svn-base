using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Memcached.ClientLibrary;

namespace MediaService
{
    /// <summary>
    /// Memcached操作类
    /// </summary>
    public static class CacheHelper
    {

        //单例模式保证进程内实例唯一
        private static readonly SockIOPool sockIOPool;
        /// <summary>
        /// 当前线程池
        /// </summary>
        public static SockIOPool CurrentPool
        {
            get
            {
                return sockIOPool;
            }
        }

        private static MemcachedClient memcacedClient;

        static CacheHelper()
        {
            // Memcached服务器列表
            // 如果有多台服务器，则以逗号分隔，例如："192.168.80.10:11211","192.168.80.11:11211"
            // string[] serverList = { "192.168.80.10:11211","192.168.80.11:11211" };
            string[] serverList = ConfigurationManager.AppSettings["MemcachedServers"].Split(',');
            // 初始化SocketIO池
            string poolName = "MediaService";
            sockIOPool = SockIOPool.GetInstance(poolName);
            // 添加服务器列表
            sockIOPool.SetServers(serverList);
            // 设置连接池初始数目
            sockIOPool.InitConnections = 3;
            // 设置连接池最小连接数目
            sockIOPool.MinConnections = 1;
            // 设置连接池最大连接数目
            sockIOPool.MaxConnections = 200;
            // 设置连接的套接字超时时间（单位：毫秒）
            sockIOPool.SocketConnectTimeout = 1000;
            // 设置套接字超时时间（单位：毫秒）
            sockIOPool.SocketTimeout = 3000;
            // 设置维护线程运行的睡眠时间：如果设置为0，那么维护线程将不会启动
            sockIOPool.MaintenanceSleep = 30;
            // 设置SockIO池的故障标志
            sockIOPool.Failover = true;
            // 是否用nagle算法启动
            sockIOPool.Nagle = false;
            // 正式初始化容器
            sockIOPool.Initialize();

            // 获取Memcached客户端实例
            memcacedClient = new MemcachedClient();
            // 指定客户端访问的SockIO池
            memcacedClient.PoolName = poolName;
            // memcacedClient：如果启用了压缩，数据压缩长于门槛的数据将被储存在压缩的形式
            memcacedClient.EnableCompression = false;
        }

        #region 操作
        
        #region 写(Set)
        /// <summary>
        /// 设置数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void Set(string key, object value)
        {
            memcacedClient.Set(key, value);
        }
        /// <summary>
        /// 设置数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="hashCode">哈希码</param>
        public static void Set(string key, object value, int hashCode)
        {
            memcacedClient.Set(key, value, hashCode);
        }
        /// <summary>
        /// 设置数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        public static void Set(string key, object value, DateTime expiry)
        {
            memcacedClient.Set(key, value, expiry);
        }
        /// <summary>
        /// 设置数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        public static void Set(string key, object value, DateTime expiry, int hashCode)
        {
            memcacedClient.Set(key, value, expiry, hashCode);
        }
        #endregion

        #region 读(Get)

        #region 返回泛型
        /// <summary>
        /// 读取数据缓存
        /// </summary>
        /// <param name="key">键</param>
        public static T Get<T>(string key)
        {
            return (T)memcacedClient.Get(key);
        }
        /// <summary>
        /// 读取数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashCode">哈希码</param>
        public static T Get<T>(string key, int hashCode)
        {
            return (T)memcacedClient.Get(key, hashCode);
        }
        /// <summary>
        /// 读取数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="asString">是否把值作为字符串返回</param>
        public static T Get<T>(string key, object value, bool asString)
        {
            return (T)memcacedClient.Get(key, value, asString);
        }
        #endregion

        /// <summary>
        /// 读取数据缓存
        /// </summary>
        /// <param name="key">键</param>
        public static object Get(string key)
        {
            return memcacedClient.Get(key);
        }
        /// <summary>
        /// 读取数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashCode">哈希码</param>
        public static object Get(string key, int hashCode)
        {
            return memcacedClient.Get(key, hashCode);
        }
        /// <summary>
        /// 读取数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="asString">是否把值作为字符串返回</param>
        public static object Get(string key, object value, bool asString)
        {
            return memcacedClient.Get(key, value, asString);
        }
        #endregion

        #region 批量写(Set)
        /// <summary>
        /// 批量设置数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void SetMultiple(string[] keys, object[] values)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                memcacedClient.Set(keys[i], values[i]);
            }
        }
        /// <summary>
        /// 批量设置数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="hashCode">哈希码</param>
        public static void SetMultiple(string[] keys, object[] values, int[] hashCodes)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                memcacedClient.Set(keys[i], values[i], hashCodes[i]);
            }
        }
        /// <summary>
        /// 批量设置数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        public static void SetMultiple(string[] keys, object[] values, DateTime[] expirys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                memcacedClient.Set(keys[i], values[i], expirys[i]);
            }
        }
        /// <summary>
        /// 批量设置数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        public static void Set(string[] keys, object[] values, DateTime[] expirys, int[] hashCodes)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                memcacedClient.Set(keys[i], values[i], expirys[i], hashCodes[i]);
            }
        }
        #endregion

        #region 批量读取(Multiple),返回哈希表 Hashtable
        /// <summary>
        /// 批量读取数据缓存
        /// </summary>
        /// <param name="keys">键集合</param>
        public static Hashtable GetMultiple(string[] keys)
        {
            return memcacedClient.GetMultiple(keys);
        }
        /// <summary>
        /// 批量读取数据缓存
        /// </summary>
        /// <param name="keys">键集合</param>
        /// <param name="hashCodes">哈希码集合</param>
        public static Hashtable GetMultiple(string[] keys, int[] hashCodes)
        {
            return memcacedClient.GetMultiple(keys, hashCodes);
        }
        /// <summary>
        /// 批量读取数据缓存
        /// </summary>
        /// <param name="keys">键集合</param>
        /// <param name="hashCodes">哈希码集合</param>
        /// <param name="asString">所有值返回字符</param>
        public static Hashtable GetMultiple(string[] keys, int[] hashCodes, bool asString)
        {
            return memcacedClient.GetMultiple(keys, hashCodes, asString);
        }
        #endregion

        #region 批量读取(Multiple),返回对象数组object[]
        /// <summary>
        /// 批量读取数据缓存
        /// </summary>
        /// <param name="keys">键集合</param>
        public static object[] GetMultipleArray(string[] keys)
        {
            return memcacedClient.GetMultipleArray(keys);
        }
        /// <summary>
        /// 批量读取数据缓存
        /// </summary>
        /// <param name="keys">键集合</param>
        /// <param name="hashCodes">哈希码集合</param>
        public static object[] GetMultipleArray(string[] keys, int[] hashCodes)
        {
            return memcacedClient.GetMultipleArray(keys, hashCodes);
        }
        /// <summary>
        /// 批量读取数据缓存
        /// </summary>
        /// <param name="keys">键集合</param>
        /// <param name="hashCodes">哈希码集合</param>
        /// <param name="asString">所有值返回字符</param>
        public static object[] GetMultipleArray(string[] keys, int[] hashCodes, bool asString)
        {
            return memcacedClient.GetMultipleArray(keys, hashCodes, asString);
        }
        #endregion

        #region 批量读取(Multiple),返回泛型集合List[T]
        /// <summary>
        /// 批量读取数据缓存
        /// </summary>
        /// <param name="keys">键集合</param>
        public static List<T> GetMultipleList<T>(string[] keys)
        {
            object[] obj = memcacedClient.GetMultipleArray(keys);
            List<T> list = new List<T>();
            foreach (object o in obj)
            {
                list.Add((T)o);
            }
            return list;
        }
        /// <summary>
        /// 批量读取数据缓存
        /// </summary>
        /// <param name="keys">键集合</param>
        /// <param name="hashCodes">哈希码集合</param>
        public static List<T> GetMultipleList<T>(string[] keys, int[] hashCodes)
        {
            object[] obj = memcacedClient.GetMultipleArray(keys, hashCodes);
            List<T> list = new List<T>();
            foreach (object o in obj)
            {
                list.Add((T)o);
            }
            return list;
        }
        /// <summary>
        /// 批量读取数据缓存
        /// </summary>
        /// <param name="keys">键集合</param>
        /// <param name="hashCodes">哈希码集合</param>
        /// <param name="asString">所有值返回字符</param>
        public static List<T> GetMultipleList<T>(string[] keys, int[] hashCodes, bool asString)
        {
            object[] obj = memcacedClient.GetMultipleArray(keys, hashCodes, asString);
            List<T> list = new List<T>();
            foreach (object o in obj)
            {
                list.Add((T)o);
            }
            return list;
        }
        #endregion

        #region 替换更新(Replace)
        /// <summary>
        /// 替换更新数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void Replace(string key, object value)
        {
            memcacedClient.Replace(key, value);
        }
        /// <summary>
        /// 替换更新数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="hashCode">哈希码</param>
        public static void Replace(string key, object value, int hashCode)
        {
            memcacedClient.Replace(key, value, hashCode);
        }
        /// <summary>
        /// 替换更新数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        public static void Replace(string key, object value, DateTime expiry)
        {
            memcacedClient.Replace(key, value, expiry);
        }
        /// <summary>
        /// 替换更新数据缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        public static void Replace(string key, object value, DateTime expiry, int hashCode)
        {
            memcacedClient.Replace(key, value, expiry, hashCode);
        }
        #endregion

        #region 删除(Delete)

        /// <summary>
        ///删除指定条件缓存
        /// </summary>
        /// <param name="key">键</param>
        public static bool Delete(string key)
        {
            return memcacedClient.Delete(key);
        }
        /// <summary>
        /// 删除指定条件缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashCode">哈希码</param>
        /// <param name="expiry">过期时间</param>
        public static bool Delete(string key, int hashCode, DateTime expiry)
        {
            return memcacedClient.Delete(key, hashCode, expiry);
        }
        /// <summary>
        /// 删除指定条件缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="expiry">过期时间</param>
        public static bool Delete(string key, DateTime expiry)
        {
            return memcacedClient.Delete(key, expiry);
        }

        /// <summary>
        /// 移除全部缓存
        /// </summary>
        public static void RemovAllCache()
        {
            memcacedClient.FlushAll();
        }
        /// <summary>
        /// 移除全部缓存
        /// </summary>
        /// <param name="list">移除指定服务器缓存</param>
        public static void RemovAllCache(ArrayList list)
        {
            memcacedClient.FlushAll(list);
        }
        #endregion

        #region 是否存在(Exists)
        /// <summary>
        /// 判断指定键的缓存是否存在
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static bool IsExists(string key)
        {
            return memcacedClient.KeyExists(key);
        }
        #endregion

        #endregion

        /// <summary>
        /// 往分布式缓存中写内容
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static bool SetCache(string key, object value, DateTime dt)
        {
            if (!memcacedClient.KeyExists(key))
            {
                return memcacedClient.Add(key, value, dt);
            }
            else
            {
                return memcacedClient.Set(key, value, dt);
            }
        }

        /// <summary>
        /// 从分布式缓存中读取内容
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object GetCache(string key)
        {
            if (!memcacedClient.KeyExists(key))
            {
                return null;
            }
            else
            {
                return memcacedClient.Get(key);
            }
        }

        /// <summary>
        /// 从分布式缓存中删除内容
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool DeleteCache(string key)
        {
            if (memcacedClient.KeyExists(key))
            {
                return memcacedClient.Delete(key);
            }
            else
            {
                return true;
            }
        }
    }
}
