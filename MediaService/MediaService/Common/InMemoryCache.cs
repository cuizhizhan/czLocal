using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// MemoryCache实现
    /// </summary>
    public class InMemoryCache
    {
        #region 单例
        private static InMemoryCache _Instance = null;
        private static object _thisLock = new object();
        /// <summary>
        /// 单例
        /// </summary>
        internal static InMemoryCache Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_thisLock)
                    {
                        if (_Instance == null)
                            _Instance = new InMemoryCache();
                    }
                }
                return _Instance;
            }
        }

        private InMemoryCache()
        {
            m_MemoryCache = MemoryCache.Default;
        }

        #endregion

        #region private member

        private readonly MemoryCache m_MemoryCache;

        #endregion

        #region ICache
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Add(string key, object value)
        {
            return m_MemoryCache.Add(key, value, new DateTimeOffset(DateTime.MaxValue));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiredTime"></param>
        /// <returns></returns>
        public bool Add(string key, object value, DateTime expiredTime)
        {
            return m_MemoryCache.Add(key, value, new DateTimeOffset(expiredTime));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(string key)
        {
            return m_MemoryCache.Contains(key);
        }
        /// <summary>
        /// 
        /// </summary>
        public int Count
        {
            get { return m_MemoryCache.Count(); }
        }
        /// <summary>
        /// 
        /// </summary>
        public void Flush()
        {
            m_MemoryCache.Trim(100);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetData(string key)
        {
            return m_MemoryCache.Get(key);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetData<T>(string key)
        {
            object obj = GetData(key);
            if (obj != null)
            {
                return (T)obj;
            }
            return default(T);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            m_MemoryCache.Remove(key);
            return true;
        }

        #endregion
    }

}
