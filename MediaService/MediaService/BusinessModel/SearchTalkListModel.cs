using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    public class SearchTalkListModel
    {
        public int tid { get; set; }
        public string talkname { get; set; }
        public string auth { get; set; }
        public string remark { get; set; }
        public bool create { get; set; }
        public int usernum { get; set; }
        public int totalnum { get; set; }
        public string imageurl { get; set; }
        public string type { get; set; }
        //public string nickname { get; set; }
        public string sn { get; set; }
    }
}
