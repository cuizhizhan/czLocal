using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{
    /// <summary>
    /// 推送歌曲的JSON
    /// </summary>
    public class PushSongModel
    {
        public int id { get; set; }
        public string music_url { get; set; }
        public string title { get; set; }
        public int categories { get; set; }
        public string t_singer { get; set; }
    }
}
