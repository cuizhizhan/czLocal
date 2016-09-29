using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    public class PushMsgModel
    {
        public string text { get; set; }
        public long timing { get; set; }
    }
}
