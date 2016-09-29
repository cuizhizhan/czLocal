using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaService
{

    public class QueryDianZaninfo : DianZaninfo
    {
        public ObjectId _id;

        public QueryDianZaninfo(int _channel, int _uid, int _ouid, string _file, int _tlen, long _time)
            : base(_channel, _uid, _ouid, _file, _tlen, _time)
        { }
    }
}
