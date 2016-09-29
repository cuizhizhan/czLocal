using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    public class TalkState
    {
        /// <summary>
        /// 同聊状态(状态：0,1)
        /// </summary>
        public int State { get; set; }
        /// <summary>
        /// 性别：0,1,2
        /// </summary>
        public int Gender { get; set; }
        /// <summary>
        /// 约聊用户
        /// </summary>
        public int Currenttuid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "{\"gender\":" + Gender + ",\"state\":" + State + ",\"currenttuid\":" + Currenttuid + "}";
        }
    }
}
