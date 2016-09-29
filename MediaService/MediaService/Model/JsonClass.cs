using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Data;

namespace MediaService
{
    //服务器返回JSON类
    public class dbscarreturnUser
    {
        public int code { get; set; }
        public string msg { get; set; }
        public jsondata data;
        public class jsondata
        {
            public string token;
            public jsonuser user;
            public class jsonuser
            {
                public string user_id;
                public string user_name;
                public string nick_name;
                public string mobile;
                public string email;
                public string signature;
                public string face_url;
                //后加
                public string set_face_time;
                public string reg_zone;
                public string reg_source;
                public string roles;
                public string country;
                public string province;
                public string city;
                public string nation_id;
            }
        }
    }

    //服务器返回JSON类
    public class dbscarBaseInfo
    {
        public int code { get; set; }
        public string msg { get; set; }
        public jsondata data;
        public class jsondata
        {
            public string province;
            public string city;
            public string country;
            public string tag;
            public string driving_age;
            public string profession;
            public string expiry_date;
            public string issue_date;
            public string weixin;
            public string qq;
            public string address;
            public string contact;
            public string company;
            public string face_ver;
            public string thumb;
            public string url;
            public string login_time;
            public string reg_time;
            public string age;
            public string birthday;
            public string reg_zone;
            public string identity_tag;
            public string roles;
            public string is_bind_mobile;
            public string is_bind_email;
            public string user_name;
            public string email;
            public string mobile;
            public string set_face_time;
            public string signature;
            public string nick_name;
            public string sex;
        }
    }

    //收货地址
    public class dbscarAddress
    {
        public int code { get; set; }
        public string msg { get; set; }
        public jsondata data;
        public class jsondata
        {
            public string id;
            public string user_name;
            public string mobile;
            public string is_default;
            public string region_1;
            public string region_2;
            public string region_3;
            public string region_4;
            public string region_5;
            public string house_number;
            public string address;
        }
    }

    [DataContract]
    public class UserLoginJson
    {
        [DataMember(Name = "status")]
        private bool status;
        [DataMember(Name = "uid")]
        private int uid;
        [DataMember(Name = "username")]
        private string username;
        [DataMember(Name = "nickname")]
        private string nickname;
        [DataMember(Name = "gender")]
        private int gender;
        [DataMember(Name = "email")]
        private string email;
        [DataMember(Name = "mobile")]
        private string mobile;
        [DataMember(Name = "buffersize")]
        private int buffersize;
        [DataMember(Name = "cachetime")]
        private int cachetime;
        [DataMember(Name = "hearttime")]
        private int hearttime;
        [DataMember(Name = "lolatime")]
        private int lolatime;
        [DataMember(Name = "appid")]
        private int appid;
        [DataMember(Name = "token")]
        private string token;
        [DataMember(Name = "fm")]
        private int fm;
        [DataMember(Name = "netpercent")]
        private int netpercent;
        [DataMember(Name = "debug")]
        private int debug;
        [DataMember(Name = "micover")]
        private int micover;
        [DataMember(Name = "nowtime")]
        private long nowtime;
        [DataMember(Name = "glsn")]
        private string glsn;
        [DataMember(Name = "radiomoditime")]
        private long radiomoditime;
        [DataMember(Name = "txlmoditime")]
        private long txlmoditime;
        [DataMember(Name = "wifitime")]
        private long wifitime;
        [DataMember(Name = "istalk")]
        private int istalk;
        [DataMember(Name = "istalkreceive")]
        private int istalkreceive;
        [DataMember(Name = "issearch")]
        private int issearch;
        [DataMember(Name = "iswifi")]
        private int iswifi;
        [DataMember(Name = "ouid")]
        private int ouid;
        [DataMember(Name = "navsetting")]
        private int navsetting;

        public UserLoginJson(bool _status, int _uid, string _username, string _nickname, int _gender, string _email, string _mobile, int _buffersize, int _hearttime, int _lolatime, int _appid, string _token, int _fm, int _netpercent, int _debug, int _cachetime, int _micover, long _nowtime, string _glsn, long _txlmoditime, long _wifitime, long _radiomoditime, int _istalk, int _istalkreceive, int _issearch, int _iswifi, int _ouid, int _navsetting)
        {
            status = _status;
            uid = _uid;
            username = _username;
            nickname = _nickname;
            gender = _gender;
            email = _email;
            mobile = _mobile;
            buffersize = _buffersize;
            hearttime = _hearttime;
            lolatime = _lolatime;
            appid = _appid;
            token = _token;
            fm = _fm;
            cachetime = _cachetime;
            micover = _micover;
            netpercent = _netpercent;
            debug = _debug;
            nowtime = _nowtime;
            glsn = _glsn;
            txlmoditime = _txlmoditime;
            wifitime = _wifitime;
            radiomoditime = _radiomoditime;
            istalk = _istalk;
            istalkreceive = _istalkreceive;
            issearch = _issearch;
            iswifi = _iswifi;
            ouid = _ouid;
            navsetting = _navsetting;
        }
    }

    [DataContract]
    public class ErrorCodeJson
    {
        [DataMember(Name = "status")]
        private bool status;
        [DataMember(Name = "list")]
        public ErrorCodeJson_codelist[] list;

        public ErrorCodeJson(bool _status, DataTable dt)
        {
            status = _status;
            list = new ErrorCodeJson_codelist[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                list[i] = new ErrorCodeJson_codelist(Int32.Parse(dt.Rows[i][0].ToString()), dt.Rows[i][1].ToString());
            }
        }
    }

    [DataContract]
    public class ErrorCodeJson_codelist
    {
        [DataMember(Name = "cid")]
        private int cid;
        [DataMember(Name = "msg")]
        private string msg;
        public ErrorCodeJson_codelist(int _cid, string _msg)
        {
            cid = _cid;
            msg = _msg;
        }
    }

    [DataContract]
    public class UserListBaseMessageJson
    {
        [DataMember(Name = "status")]
        private bool status;
        [DataMember(Name = "list")]
        public UserBaseMessage[] list;

        public UserListBaseMessageJson(bool _status, DataTable dt)
        {
            status = _status;
            list = new UserBaseMessage[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                list[i] = new UserBaseMessage(Int32.Parse(dt.Rows[i][0].ToString()), dt.Rows[i][2].ToString(), Int32.Parse(dt.Rows[i][1].ToString()), dt.Rows[i][3].ToString(), Int32.Parse(dt.Rows[i][4].ToString()), Int32.Parse(dt.Rows[i][5].ToString()), Int32.Parse(dt.Rows[i][6].ToString()));
            }
        }
    }

    [DataContract]
    public class UserBaseMessage
    {
        [DataMember(Name = "uid")]
        private int uid;
        [DataMember(Name = "username")]
        private string username;
        [DataMember(Name = "gender")]
        private int gender;
        [DataMember(Name = "nickname")]
        private string nickname;
        [DataMember(Name = "roles")]
        private int roles;
        [DataMember(Name = "district_id")]
        private int district_id;
        [DataMember(Name = "area_id")]
        private int area_id;
        public UserBaseMessage(int _uid, string _username, int _gender, string _nickname, int _roles, int _district_id, int _area_id)
        {
            uid = _uid;
            gender = _gender;
            username = _username;
            nickname = _nickname;
            roles = _roles;
            district_id = _district_id;
            area_id = _area_id;
        }
    }

    [DataContract]
    public class TalkListBaseMessageJson
    {
        [DataMember(Name = "status")]
        private bool status;
        [DataMember(Name = "list")]
        public TalkBaseMessage[] list;

        public TalkListBaseMessageJson(bool _status, DataTable dt)
        {
            status = _status;
            list = new TalkBaseMessage[dt.Rows.Count];
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                list[i] = new TalkBaseMessage(Int32.Parse(dt.Rows[i][0].ToString()), dt.Rows[i][1].ToString(), Double.Parse(dt.Rows[i][2].ToString()), Double.Parse(dt.Rows[i][3].ToString()), Int32.Parse(dt.Rows[i][4].ToString()));
            }
        }
    }

    [DataContract]
    public class TalkBaseMessage
    {
        [DataMember(Name = "tid")]
        private int tid;
        [DataMember(Name = "talkname")]
        private string talkname;
        [DataMember(Name = "lo")]
        private double lo;
        [DataMember(Name = "la")]
        private double la;
        [DataMember(Name = "createuid")]
        private int createuid;
        public TalkBaseMessage(int _tid, string _talkname, double _lo, double _la, int _createuid)
        {
            tid = _tid;
            talkname = _talkname;
            lo = _lo;
            la = _la;
            createuid = _createuid;
        }
    }

    [DataContract]
    public class UserLoginWebJson
    {
        [DataMember(Name = "status")]
        private bool status;
        [DataMember(Name = "uid")]
        private int uid;
        [DataMember(Name = "username")]
        private string username;
        [DataMember(Name = "nickname")]
        private string nickname;
        [DataMember(Name = "gender")]
        private int gender;
        [DataMember(Name = "email")]
        private string email;
        [DataMember(Name = "mobile")]
        private string mobile;
        [DataMember(Name = "token")]
        private string token;
        [DataMember(Name = "password")]
        private string password;

        public UserLoginWebJson(bool _status, int _uid, string _username, string _nickname, int _gender, string _email, string _mobile, string _token, string _password)
        {
            status = _status;
            uid = _uid;
            username = _username;
            nickname = _nickname;
            gender = _gender;
            email = _email;
            mobile = _mobile;
            token = _token;
            password = _password;
        }
    }
}

