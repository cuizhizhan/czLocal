﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace MediaService
{
    /// <summary>
    /// 请求车云网数据
    /// </summary>
    public static class MyCarAdapter
    {
        #region 登录
        /// <summary>
        /// 远端登陆验证(Service端)
        /// </summary>
        /// <param name="login_key"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static dbscarreturnUser ServiceLogin(string login_key, string password, ref string result)
        {
            //string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=passport_service.login", "login_key=" + login_key + "&password=" + password + "&app_id=2014042900000006&time=" + Utility.GetTimeStamp(), "POST", Encoding.UTF8);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=passport_service.login", "login_key=" + login_key + "&password=" + password + "&app_id=2014042900000006&time=" + Utility.GetTimeStamp(), "POST", Encoding.UTF8);
            MediaService.WriteLog("登录提交至GOLO:" + str, MediaService.wirtelog);
            result = str;
            if (string.IsNullOrWhiteSpace(str))
                return null;
            dbscarreturnUser user = Utility.Deserialize<dbscarreturnUser>(str.Trim());
            return user;
        }

        /// <summary>
        /// 远端登陆验证(App端)
        /// </summary>
        /// <param name="login_key"></param>
        /// <param name="password"></param>
        /// <param name="app_id"></param>
        /// <returns></returns>
        public static dbscarreturnUser AppLogin(string login_key, string password, string app_id, ref string result)
        {
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD + "?action=passport_service.login", "login_key=" + login_key + "&password=" + password + "&app_id=" + app_id + "&time=" + Utility.GetTimeStamp(), "POST", Encoding.UTF8);
            MediaService.WriteLog("登录提交至GOLO:" + str, MediaService.wirtelog);
            result = str;
            if (string.IsNullOrWhiteSpace(str))
                return null;
            dbscarreturnUser user = Utility.Deserialize<dbscarreturnUser>(str.Trim());
            return user;
        }

        public static dbscarreturnUser AppLoginTest(string login_key, string password, string app_id, ref string result)
        {
            MediaService.WriteLog("AppLoginTest 进入方法 ：login_key =" + login_key.ToString() + " password=" + password, MediaService.wirtelog);
            string str = Utility.HttpRequestRoute("http://golo.test.x431.com:8008/dev/?action=passport_service.login", "login_key=" + login_key + "&password=" + password + "&app_id=" + app_id + "&time=" + Utility.GetTimeStamp(), "POST", Encoding.UTF8);
            MediaService.WriteLog("登录提交至GOLO:" + str, MediaService.wirtelog);
            result = str;
            if (string.IsNullOrWhiteSpace(str))
                return null;
            dbscarreturnUser user = Utility.Deserialize<dbscarreturnUser>(str.Trim());
            return user;
        }

        /// <summary>
        /// 获取远端基础数据
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static dbscarBaseInfo GetBaseInfo(int uid)
        {
            string tokenlogin = Utility.StringToMD5Hash(DateTime.Now.Ticks.ToString());
            string poststr = "action=userinfo.get_base_info_car_logo&app_id=2014042900000006&lan=zh&user_id=" + uid + "&ver=3.01";
            string sign = Utility.StringToMD5Hash(poststr + tokenlogin).ToLower();
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD + "?action=userinfo.get_base_info_car_logo&app_id=2014042900000006&user_id=" + uid + "&ver=3.01&sign=" + sign, "lan=zh", "POST", Encoding.UTF8); ;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return null;
            dbscarBaseInfo user = Utility.Deserialize<dbscarBaseInfo>(str.Trim());
            return user;
            //{"code":0,"msg":"","data":{"sex":"1","nick_name":"发布锋","signature":"","set_face_time":"0","mobile":"","email":"","user_name":"norhon007","is_bind_email":"0","is_bind_mobile":"0","roles":"0","identity_tag":"","reg_zone":"1","birthday":null,"age":0,"reg_time":"2015-10-29 10:51:10","login_time":"2015-12-28 18:06:05","url":null,"thumb":null,"face_ver":"0","company":"","contact":"","address":"","zipcode":"","qq":"","weixin":"","issue_date":"","expiry_date":"","profession":"","driving_age":"0","tag":"","hobby":"","country":"中国","province":"","city":"","tech":{"user_id":"1776790","tech_level":"0","non_brand":"","brand":[],"wunit":"","wtime":"","experience":"","roles":"0","is_expert":"0","permit":{"url":null,"thumb":"nullm"},"work_address":"","tage":"0","twork":"","country":"{\"s\":143}","repair":null,"repair_addr":null,"tname":null,"tcontact":null,"is_bind":"0","permit_new":[{"url":null,"thumb":"nullm","id":0}]},"review":null,"car_logo_url":[],"car_id":[],"car_series_name":[],"auto_code":[],"point":0,"point_level":0,"driving_tag":"","often_haunt":"","group_car":null,"group_act":[],"emergency":{"sum":0,"m_num":0},"photo":[{"url":null,"thumb":null}],"honor":"0","pub_account":{"pub_id":"","pub_name":""},"qrcode_content":"http:\/\/base.api.dbscar.com\/?action=qrcode_service.run&TVRjM05qYzVNQzh5TURFME1EUXlPVEF3TURBd01EQTJMM3Bv_TWc","company_name":"","company_logo":"","company_phone":"","company_fax":"","company_address":"","apply_status":"-1","public_id":null,"public_name":null,"public_face":null,"public_signature":null,"public_address":null}}
        }

        /// <summary>
        /// 请求发送验证码(注册和找回密码用)
        /// </summary>
        /// <param name="req_info">手机号/邮箱/用户名（必须存在字母，可以存在数字和下划线）</param>
        /// <param name="is_check">2图形验证码 3短信验证码|邮箱验证码</param>
        /// <param name="isres">
        /// <param name="app_id"/>
        /// 1--注册检查，如果req_info被注册，抛出错误码110001
        /// 2--找回密码检查，--如果req_info为邮箱且未被注册，抛出错误码110002，--如果为手机号码且未被绑定，爆出错误码30030
        /// </param>
        /// <returns></returns>
        public static bool VerifyCode_Req_Send_Code(string req_info, string is_check,string app_id, string isres, ref string result)
        {
            Dictionary<string, string> maps = new Dictionary<string, string>();
            maps.Add("req_info", req_info);
            maps.Add("is_check", is_check);
            maps.Add("isres", isres);
            string url = Utility.CreateLinkString(maps);
            string str = "";
            if (string.IsNullOrEmpty(app_id))
            {
                str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD + "?action=verifycode.req_send_code", url,
                    "POST", Encoding.UTF8);
            }
            else
            {
                str =
                    Utility.HttpRequestRoute(
                        ConstStrings.REQUEST_URL_HEAD + "?action=verifycode.req_send_code" +
                        string.Format("&app_id={0}", app_id), url, "POST", Encoding.UTF8);
            }
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }

        /// <summary>
        /// 验证输入的验证码
        /// </summary>
        /// <param name="req_info"></param>
        /// <param name="verify_code"></param>
        /// <returns></returns>
        public static bool Verifycode_Verify(string req_info, string verify_code, ref string result)
        {
            Dictionary<string, string> maps = new Dictionary<string, string>();
            maps.Add("req_info", req_info);
            maps.Add("verify_code", verify_code);
            string url = Utility.CreateLinkString(maps);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=verifycode.verify", url, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="req_info"></param>
        /// <param name="is_check"></param>
        /// <param name="isres"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool Passport_Service_Register(string app_id, string verify_code, string nick_name, string password, string nation_id, string loginKey, ref string result)
        {
            Dictionary<string, string> maps = new Dictionary<string, string>();
            maps.Add("nation_id", nation_id);
            maps.Add("loginKey", loginKey);
            maps.Add("app_id", app_id);
            maps.Add("verify_code", verify_code);
            maps.Add("password", password);
            maps.Add("nick_name", nick_name);
            string url = Utility.CreateLinkString(maps);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=passport_service.register", url, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        /// <param name="req"></param>
        /// <param name="pass"></param>
        /// <param name="confirm_pass"></param>
        /// <param name="verify_code"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Passport_Service_Reset_Pass(string app_id, string req, string pass, string confirm_pass, string verify_code, ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "passport_service.reset_pass");
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            Dictionary<string, string> post = new Dictionary<string, string>();
            post.Add("req", req);
            post.Add("pass", pass);
            post.Add("confirm_pass", confirm_pass);
            post.Add("verify_code", verify_code);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?" + geturl, posturl, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }

        /// <summary>
        /// 用户修改密码
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="pw"></param>
        /// <param name="chpw"></param>
        /// <param name="ver"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Userinfo_Set_Password(string user_id, string app_id, string token, string pw, string chpw, string ver, ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "userinfo.set_password");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            Dictionary<string, string> post = new Dictionary<string, string>();
            post.Add("pw", pw);
            post.Add("chpw", chpw);
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?" + geturl, posturl, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 设置省份

        #region 获取省份列表
        public static bool GetProvince(string user_id, string app_id, string token, string ncode, string ver, ref string result)
        {
            Dictionary<string, string> maps = new Dictionary<string, string>();
            maps.Add("user_id", user_id);
            maps.Add("app_id", app_id);
            maps.Add("ncode", ncode);
            maps.Add("ver", ver);
            string sign = Utility.GetSign(token, maps);
            maps.Add("sign", sign);
            maps.Add("lan", "zh");
            string url = Utility.CreateLinkString(maps);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=area_service.get_province", url, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            return CommBusiness.GetJsonValue(result, "code", ",", false).Trim().Equals("0");
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 获取城市列表
        public static bool GetCity(string user_id, string app_id, string token, string pcode, string ver, ref string result)
        {
            Dictionary<string, string> maps = new Dictionary<string, string>();
            maps.Add("user_id", user_id);
            maps.Add("app_id", app_id);
            maps.Add("pcode", pcode);
            maps.Add("ver", ver);
            string sign = Utility.GetSign(token, maps);
            maps.Add("sign", sign);
            maps.Add("lan", "zh");
            string url = Utility.CreateLinkString(maps);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=area_service.get_city", url, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            return CommBusiness.GetJsonValue(result, "code", ",", false).Trim().Equals("0");
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 获取区县列表
        public static bool GetRegion(string user_id, string app_id, string token, string ccode, string ver, ref string result)
        {
            Dictionary<string, string> maps = new Dictionary<string, string>();
            maps.Add("user_id", user_id);
            maps.Add("app_id", app_id);
            maps.Add("ccode", ccode);
            maps.Add("ver", ver);
            string sign = Utility.GetSign(token, maps);
            maps.Add("sign", sign);
            maps.Add("lan", "zh");
            string url = Utility.CreateLinkString(maps);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?action=area_service.get_region", url, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            return CommBusiness.GetJsonValue(result, "code", ",", false).Trim().Equals("0");
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #endregion

        #region 收货地址

        #region 获取收货地址
        /// <summary>
        /// 获取收货地址
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="result"></param>
        /// <param name="ver"></param>
        /// <returns></returns>
        public static bool GetAddress(string user_id, string app_id, string token, ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "gra_service.get");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            Dictionary<string, string> post = new Dictionary<string, string>();
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?" + geturl, posturl, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            var baseinfo = Utility.Deserialize<dbscarAddress>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 添加收货地址
        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_name"></param>
        /// <param name="house_number"></param>
        /// <param name="region_2"></param>
        /// <param name="region_3"></param>
        /// <param name="region_4"></param>
        /// <param name="address"></param>
        /// <param name="mobile"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool AddAddress(string user_id, string app_id, string token,
            string user_name, string house_number, string region_2, string region_3, string region_4, string address, string mobile,
            ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "gra_service.add");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            Dictionary<string, string> post = new Dictionary<string, string>();
            post.Add("user_name", user_name);
            post.Add("house_number", house_number);
            post.Add("region_1", "143");
            post.Add("region_2", region_2);
            post.Add("region_3", region_3);
            post.Add("region_4", region_4);
            post.Add("region_5", "0");
            post.Add("address", address);
            post.Add("mobile", mobile);
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?" + geturl, posturl, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 删除收货地址
        /// <summary>
        /// 删除收货地址
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="result"></param>
        /// <param name="ver"></param>
        /// <returns></returns>
        public static bool DeleteAddress(string user_id, string app_id, string token, string id, ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "gra_service.delete");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            Dictionary<string, string> post = new Dictionary<string, string>();
            post.Add("id", id);
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?" + geturl, posturl, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 更新收货地址
        /// <summary>
        /// 更新收货地址
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="id">收货表主键id</param>
        /// <param name="is_default">1默认0非默认</param>
        /// <param name="user_name">收货人名字</param>
        /// <param name="mobile">收货人手机号码</param>
        /// <param name="region_1">1级行政区域编码 如国家</param>
        /// <param name="region_2">2级行政区域编码 如省</param>
        /// <param name="region_3">3级行政区域编码 如市</param>
        /// <param name="region_4">4级行政区域编码 如县|区</param>
        /// <param name="region_5">5级行政区域编码 如乡|镇</param>
        /// <param name="address">详细|街道地址</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool UpdateAddress(string user_id, string app_id, string token, string id, bool is_default,
            string user_name, string mobile, string region_1, string region_2, string region_3, string region_4, string region_5, string address,
            ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "gra_service.update");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            Dictionary<string, string> post = new Dictionary<string, string>();
            post.Add("id", id);
            if ((user_name + "").Length > 0) { post.Add("user_name", user_name); }
            if ((mobile + "").Length > 0) { post.Add("mobile", mobile); }
            if ((region_1 + "").Length > 0) { post.Add("region_1", region_1); }
            if ((region_2 + "").Length > 0) { post.Add("region_2", region_2); }
            if ((region_3 + "").Length > 0) { post.Add("region_3", region_3); }
            if ((region_4 + "").Length > 0) { post.Add("region_4", region_4); }
            if ((region_5 + "").Length > 0) { post.Add("region_5", region_5); }
            if ((address + "").Length > 0) { post.Add("address", address); }
            //post.Add("is_default", is_default.ToString());
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?" + geturl, posturl, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 设置默认地址接口
        /// <summary>
        /// 设置默认地址接口
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="result"></param>
        /// <param name="is_default"></param>
        /// <returns></returns>
        public static bool UpdateAddress(string user_id, string app_id, string token, string id, string is_default,
            ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "gra_service.update");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            Dictionary<string, string> post = new Dictionary<string, string>();
            post.Add("id", id);
            post.Add("is_default", is_default);
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"?" + geturl, posturl, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion
        #endregion

        #region 剩余流量查询
        public static bool GetFlowBySerial(string sim, string serial_no, ref string result)
        {
            Dictionary<string, string> maps = new Dictionary<string, string>();
            maps.Add("action", "sim_card_service.get_flow_by_serial");
            maps.Add("serial_no", serial_no);
            if (sim.Length >= 19)
                maps.Add("sim", sim.Substring(0, 19));

            string url = Utility.CreateLinkString(maps);
            string str = Utility.HttpRequestRoute("http://apps.api.dbscar.com/", url, "GET", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            return CommBusiness.GetJsonValue(result, "code", ",", false).Trim().Equals("0");
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 行程体检

        #region 体检
        /// <summary>
        /// goloz车辆报告列表
        /// Report_Service_Car_Report_List("1706692", "1311", "021DB03B08B976B6A3838BEE32E686285cch", "971691003942", true, Utility.ConvertDateTimeInt(new DateTime(2016,1,6,8,8,8,0)).ToString(), false, "0", ref result);
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="sn">9716拼接sn号</param>
        /// <param name="isstart">使用since_time时候为：true</param>
        /// <param name="since_time">刷新时候传，与max_time只能传1个</param>
        /// <param name="isend">使用max_time时候为：true</param>
        /// <param name="max_time">加载更多时传</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Report_Service_Car_Report_List(string user_id, string app_id, string token,
            string sn, bool isstart, string since_time, bool isend, string max_time,
            ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "report_service.car_report_list");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            get.Add("sn", sn);
            if (isstart)
                get.Add("since_time", since_time);
            if (isend)
                get.Add("max_time", max_time);
            Dictionary<string, string> post = new Dictionary<string, string>();
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute("http://apps.api.dbscar.com/", geturl, "GET", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 足迹接口
        /// <summary>
        /// 足迹接口
        /// Trip_Service_Get_Trip_Wgs("1706692", "1311", "021DB03B08B976B6A3838BEE32E686285cch", "971691003942", Utility.ConvertDateTimeInt(new DateTime(2016, 1, 5, 8, 8, 8, 0)).ToString(), Utility.ConvertDateTimeInt(new DateTime(2016, 1, 6, 8, 8, 8, 0)).ToString(), "1", true, ref result);
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="serial_no">设备序列号,有接头时为必选参数</param>
        /// <param name="start_time">起始日期 时间戳</param>
        /// <param name="end_time">结束日期 时间戳</param>
        /// <param name="type">1 无接头  0 有接头</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Trip_Service_Get_Trip_Wgs(string user_id, string app_id, string token,
            string serial_no, string start_time, string end_time, string type, bool IsUseType,
            ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "trip_service.get_trip_wgs");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            get.Add("serial_no", serial_no);
            get.Add("start_time", start_time);
            get.Add("end_time", end_time);
            if (IsUseType)
                get.Add("type", type);
            Dictionary<string, string> post = new Dictionary<string, string>();
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute("http://apps.api.dbscar.com/", geturl, "GET", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 根据月份获取天数的里程统计数据
        /// <summary>
        /// 根据月份获取天数的里程统计数据
        /// Mileage_Count_Month_Count("1706692", "1311", "7E8E6824BBCC716C156A662647165B453iru", "971691003942", "2015-12", ref result);
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="sn">序列号</param>
        /// <param name="month">月份</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Mileage_Count_Month_Count(string user_id, string app_id, string token,
            string sn, string month,
            ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "mileage_count_service.month_count");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            get.Add("sn", sn);
            get.Add("month", month);
            Dictionary<string, string> post = new Dictionary<string, string>();
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute("http://apps.api.dbscar.com/", geturl, "GET", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 获取指定日期的行程id
        /// <summary>
        /// 获取里程
        /// Gps_Info_Service_Get_Mileage("1706692", "1311", "E3C3099D3BAD069446129A39F62E0C2Cf59x", "971691003942", "2016-01-01 00:00:00", "2016-01-06 23:00:00", ref result);
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="serial_no">设备序列号</param>
        /// <param name="start_time">开始时间</param>
        /// <param name="end_time">结束时间</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Gps_Info_Service_Get_Mileage(string user_id, string app_id, string token,
            string serial_no, string start_time, string end_time,
            ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "gps_info_service.get_mileage");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            //maps.Add("lan", "zh");
            get.Add("serial_no", serial_no);
            get.Add("start_time", start_time);
            get.Add("end_time", end_time);
            Dictionary<string, string> post = new Dictionary<string, string>();
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute("http://apps.api.dbscar.com/", geturl, "GET", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 根据指定行程id获取详细信息
        /// <summary>
        /// 根据指定行程id获取详细信息
        /// Gps_Info_Get_Data2("1706692", "1311", "7E8E6824BBCC716C156A662647165B453iru", "9086b432-8f92-89bb-dd2b-1f5c8a2dc437", ref result);
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="mileage_ids">单个里程id或多个里程id(以逗号分隔开)</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Gps_Info_Get_Data2(string user_id, string app_id, string token,
        string mileage_ids,
        ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "gps_info_service.get_data2");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            Dictionary<string, string> post = new Dictionary<string, string>();
            post.Add("mileage_ids", mileage_ids);
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute("http://apps.api.dbscar.com/?" + geturl, posturl, "POST", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 查询历史轨迹点（行程详情）
        /// <summary>
        /// 查询历史轨迹点（行程详情）
        /// Gps_Info_Get_Hisitory_Position_Record_Wgs("1706692", "1311", "E675C5DFE71EF5045FCCDF9A312328B6laot", "2015-12-06", "1452045600", "1452078000", "971691003942", ref result);
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="queryDate">日期</param>
        /// <param name="startTime">开始时间（时间戳）</param>
        /// <param name="endTime">结束时间（时间戳）</param>
        /// <param name="serial_no">设备序列号</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Gps_Info_Get_Hisitory_Position_Record_Wgs(string user_id, string app_id, string token,
        string queryDate, string startTime, string endTime, string serial_no,
        ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "gps_info_service.get_hisitory_position_record_wgs");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            get.Add("queryDate", queryDate);
            get.Add("startTime", startTime);
            get.Add("endTime", endTime);
            get.Add("serial_no", serial_no);
            Dictionary<string, string> post = new Dictionary<string, string>();
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute("http://apps.api.dbscar.com/", geturl, "GET", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 查询实时轨迹点
        /// <summary>
        /// 查询实时轨迹点
        /// Gps_Info_Get_Real_Time_Data_Wgs("1706692", "1311", "7E8E6824BBCC716C156A662647165B453iru", "971691003942", "1", ref result);
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="serial_no">设备序列号</param>
        /// <param name="g_id">群id,非必选（没有设置成 ""）</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Gps_Info_Get_Real_Time_Data_Wgs(string user_id, string app_id, string token,
        string serial_no, string g_id,
        ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "gps_info_service.get_real_time_data_wgs");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            get.Add("serial_no", serial_no);
            get.Add("type", "1");
            int g_id_out = 0;
            if (!string.IsNullOrEmpty(g_id) && int.TryParse(g_id, out g_id_out) == true)
            {
                get.Add("g_id", g_id);
            }
            Dictionary<string, string> post = new Dictionary<string, string>();
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute("http://apps.api.dbscar.com/", geturl, "GET", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 获取实时位置时，查询当前未完成里程的轨迹接口(新)
        /// <summary>
        /// 查询实时轨迹点
        /// Gps_Info_Get_Trip_Record_Wgs("1706692", "1311", "7E8E6824BBCC716C156A662647165B453iru", "971691003942", ref result);
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="sn">设备序列号</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Gps_Info_Get_Trip_Record_Wgs(string user_id, string app_id, string token, string sn, ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "gps_info_service.get_trip_record_wgs");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            get.Add("sn", sn);
            get.Add("type", "1");
            Dictionary<string, string> post = new Dictionary<string, string>();
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute("http://apps.api.dbscar.com/", geturl, "GET", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion

        #region 获取实时数据流
        /// <summary>
        /// 获取实时数据流
        /// Datastream_Getdfdatalistnew("1706692", "1311", "7E8E6824BBCC716C156A662647165B453iru", "971691003942", ref result);
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="serial_no">设备序列号</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Datastream_Getdfdatalistnew(string user_id, string app_id, string token,
        string serial_no,
        ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "datastream_service.getdfdatalistnew");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            get.Add("serial_no", serial_no);
            Dictionary<string, string> post = new Dictionary<string, string>();
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string posturl = Utility.CreateLinkString(post);
            string str = Utility.HttpRequestRoute("http://apps.api.dbscar.com/", geturl, "GET", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion
        #endregion

        #region 设置用户头像
        /// <summary>
        /// 设置用户头像
        /// </summary>
        /// <param name="user_id"></param>
        /// <param name="app_id"></param>
        /// <param name="token"></param>
        /// <param name="picbyte"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool User_Service_Setface(string user_id, string app_id, string token, byte[] picbyte, ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "user_service.setface");
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            get.Add("app_id", app_id);
            Dictionary<string, string> post = new Dictionary<string, string>();
            string sign = Utility.GetSign(token, get, post);
            get.Add("sign", sign);
            string geturl = Utility.CreateLinkString(get);
            string siteurl = "http://file.api.dbscar.com/?action=user_service.setface&app_id=" + app_id + "&sign=" + sign + "&user_id=" + user_id + "&ver=5.0.3";
            result = PostImage(siteurl, picbyte);
            return false;
        }

        private static string PostImage(string url, byte[] fileByte)
        {
            const string headstr = "-----------------------------7dd27182c0258\r\n"
                                   + "Content-Disposition: form-data; name=\"pic\"; filename=\"F:\\1.png\"\r\n"
                                   + "Content-Type: audio/mpeg\r\n"
                                   + "\r\n";
            byte[] headbyte = Encoding.UTF8.GetBytes(headstr);

            const string endstr = "\r\n\r\n-----------------------------7dd27182c0258--\r\n";
            byte[] endbyte = Encoding.UTF8.GetBytes(endstr);

            MediaService.WriteLog("上传url：" + url, MediaService.wirtelog);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "multipart/form-data; boundary=---------------------------7dd27182c0258";
            request.ContentLength = headbyte.Length + fileByte.Length + endbyte.Length;
            request.Timeout = 1000 * MediaService.httptimeout;
            request.ReadWriteTimeout = 1000 * MediaService.httptimeout;
            Stream wr = request.GetRequestStream();
            wr.Write(headbyte, 0, headbyte.Length);
            wr.Write(fileByte, 0, fileByte.Length);
            wr.Write(endbyte, 0, endbyte.Length);
            wr.Close();

            WebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader httpreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string json = httpreader.ReadToEnd();
            httpreader.Close();
            response.Close();
            return json.Replace("\\/", "/");
        }
        #endregion

        #region version.版本比较并更新
        /// <summary>
        /// version.版本比较并更新
        /// </summary>
        /// <param name="app_id">引用id</param>
        /// <param name="vision_no">目前版本</param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool Version_Latest(string app_id, string vision_no, string user_id, string token, string is_test, ref string result)
        {
            Dictionary<string, string> get = new Dictionary<string, string>();
            get.Add("action", "vision_service.latest");
            get.Add("app_id", app_id);
            get.Add("vision_no", vision_no);
            get.Add("user_id", user_id);
            get.Add("ver", "5.0.3");
            string sign = Utility.GetSign(token, get);
            get.Add("sign", sign);
            get.Add("is_test", is_test);
            string geturl = Utility.CreateLinkString(get);
            string str = Utility.HttpRequestRoute(ConstStrings.REQUEST_URL_HEAD+"", geturl, "GET", Encoding.UTF8);
            result = str;
            MediaService.WriteLog(" RecvThread接收： t=" + str, MediaService.wirtelog);
            if (string.IsNullOrWhiteSpace(str))
                return false;
            ResponseBaseInfo baseinfo = Utility.Deserialize<ResponseBaseInfo>(str);
            return baseinfo.code == 0;
            //{"code":0,"msg":"","data":[]}
        }
        #endregion
    }
}
