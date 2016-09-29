using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// favorite,subscribe,menu  这几个都用这个model
    /// </summary>
    public class ListModel
    {
        public int Ouid { get; set; }
        public List<int> DataList { get; set; }
    }

    public class MusicModel
    {
        /// <summary>
        /// 媒体url
        /// </summary>
        public string music_url { get; set; }

        public string title { get; set; }
        public string categories { get; set; }
        public string t_singer { get; set; }
        //public  Type { get; set; }
    }
}
