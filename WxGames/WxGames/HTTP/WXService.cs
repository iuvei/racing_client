﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using BLL;

namespace WxGames.HTTP
{
    /// <summary>
    /// 微信主要业务逻辑服务类
    /// </summary>
    public class WXService
    {
        private static Dictionary<string, string> _syncKey = new Dictionary<string, string>();

        //微信初始化url
        private static string _init_url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxinit?";
        //获取好友头像
        private static string _geticon_url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxgeticon?username=";
        //获取群聊（组）头像
        private static string _getheadimg_url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxgetheadimg?username=";
        //获取好友列表
        private static string _getcontact_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxgetcontact";
        //同步检查url
        private static string _synccheck_url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/synccheck?sid={0}&uin={1}&synckey={2}&r={3}&skey={4}&deviceid={5}";
        //同步url
        private static string _sync_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxsync?sid=";
        //发送消息url
        private static string _sendmsg_url = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsendmsg?sid=";

        private static string _delete_user = "https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxupdatechatroom?fun=delmember&pass_ticket=";

        public string DeleteUser(string gUserName, string cUserName)
        {
            string msg_json = "";

            StringBuilder s = new StringBuilder();//s数组中存放着需要的数字
            Random ra = new Random();
            for (int i = 0; i < 15; i++)//遍历数组显示结果
            {
                s.Append(ra.Next(1, 10));
            }

            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                msg_json = "{\"BaseRequest\":{ \"DeviceID\":\"e" + s.ToString() + "\",\"Sid\":\"" + sid.Value + "\",\"Skey\":\"" + LoginService.SKey + "\",\"Uin\":" + uin.Value + "},\"DelMemberList\":\"" + cUserName + "\",\"ChatRoomName\":\"" + gUserName + "\"}";
                List<string> listCheckUrl = new List<string>();
                listCheckUrl.Add("wx.qq.com");
                listCheckUrl.Add("wx2.qq.com");
                byte[] bytes = null;
                foreach (string item in listCheckUrl)
                {
                    ///cgi-bin/mmwebwx-bin/webwxupdatechatroom?fun=delmember&lang=zh_CN&pass_ticket=
                    string url = "https://" + item + "/cgi-bin/mmwebwx-bin/webwxupdatechatroom?fun=delmember&lang=zh_CN";
                    bytes = BaseService.SendPostRequest(url + "&pass_ticket=" + LoginService.Pass_Ticket, msg_json);
                    if (bytes == null)
                    {
                        continue;
                    }

                    string send_result = Encoding.UTF8.GetString(bytes);

                    if (!send_result.Contains("\"Ret\": 0"))
                    {
                        continue;
                    }
                    else
                    {
                        return send_result;
                    }
                }
            }
            return "";
        }


        /// <summary>
        /// 微信初始化
        /// </summary>
        /// <returns></returns>
        public JObject WxInit()
        {
            StringBuilder s = new StringBuilder();//s数组中存放着需要的数字
            Random ra = new Random();
            for (int i = 0; i < 15; i++)//遍历数组显示结果
            {
                s.Append(ra.Next(1, 10));
            }

            string init_json = "{{\"BaseRequest\":{{\"Uin\":\"{0}\",\"Sid\":\"{1}\",\"Skey\":\"{2}\",\"DeviceID\":\"e{3}\"}}}}";
            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                init_json = string.Format(init_json, uin.Value, sid.Value, LoginService.SKey, s.ToString());

                string r = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Split(new char[] { '.' })[0];

                List<string> listCheckUrl = new List<string>();
                listCheckUrl.Add("wx.qq.com");
                listCheckUrl.Add("wx2.qq.com");
                byte[] bytes = null;
                string init_str = "";
                foreach (string item in listCheckUrl)
                {
                    string url = "https://" + item + "/cgi-bin/mmwebwx-bin/webwxinit?";
                    bytes = BaseService.SendPostRequest(url + "r=" + r + "&lang=zh_CN&pass_ticket=" + LoginService.Pass_Ticket, init_json);
                    if (bytes != null)
                    {
                        init_str = Encoding.UTF8.GetString(bytes);
                        //if (init_str.Contains("1100"))
                        //{
                        //    continue;
                        //}
                        //break;
                        if (init_str.Contains("\"Ret\": 0"))
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                JObject init_result = JsonConvert.DeserializeObject(init_str) as JObject;

                foreach (JObject synckey in init_result["SyncKey"]["List"])  //同步键值
                {
                    if (_syncKey.ContainsKey(synckey["Key"].ToString()))
                    {
                        continue;
                    }
                    _syncKey.Add(synckey["Key"].ToString(), synckey["Val"].ToString());
                }
                return init_result;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 获取好友头像
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public Image GetIcon(string username)
        {
            byte[] bytes = BaseService.SendGetRequest(_geticon_url + username);


            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }
            return Image.FromStream(new MemoryStream(bytes));
        }
        /// <summary>
        /// 获取微信讨论组头像
        /// </summary>
        /// <param name="usename"></param>
        /// <returns></returns>
        public Image GetHeadImg(string usename)
        {
            byte[] bytes = BaseService.SendGetRequest(_getheadimg_url + usename);
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }
            return Image.FromStream(new MemoryStream(bytes));
        }
        /// <summary>
        /// 获取好友列表
        /// </summary>
        /// <returns></returns>
        public JObject GetContact()
        {
            List<string> listCheckUrl = new List<string>();
            listCheckUrl.Add("wx.qq.com");
            listCheckUrl.Add("wx2.qq.com");
            byte[] bytes = null;

            foreach (var item in listCheckUrl)
            {
                string r = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Split(new char[] { '.' })[0];

                string url = " https://" + item + "/cgi-bin/mmwebwx-bin/webwxgetcontact?" + "r=" + r + "&lang=zh_CN&pass_ticket=" + LoginService.Pass_Ticket;

                bytes = BaseService.SendGetRequest(url);
                if (bytes == null || bytes.Length == 0)
                {
                    continue;
                }
                else
                {
                    if (Encoding.UTF8.GetString(bytes).Contains("\"Ret\": 1"))
                    {
                        continue;
                    }

                    break;
                }
            }

            if (bytes == null)
            {
                foreach (var item in listCheckUrl)
                {
                    string r = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Split(new char[] { '.' })[0];

                    string url = " https://" + item + "/cgi-bin/mmwebwx-bin/webwxgetcontact?" + "r=" + r + "&lang=zh_CN&pass_ticket=" + LoginService.Pass_Ticket;

                    bytes = BaseService.SendGetRequest(url);
                    if (bytes == null || bytes.Length == 0)
                    {
                        continue;
                    }
                    else
                    {
                        if (Encoding.UTF8.GetString(bytes).Contains("\"Ret\": 1"))
                        {
                            continue;
                        }

                        break;
                    }
                }
            }
            if (bytes == null)
            {
                return null;
            }
            string contact_str = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject(contact_str) as JObject;
        }
        /// <summary>
        /// 微信同步检测
        /// </summary>
        /// <returns></returns>
        public string WxSyncCheck()
        {
            string sync_key = "";
            foreach (KeyValuePair<string, string> p in _syncKey)
            {
                sync_key += p.Key + "_" + p.Value + "%7C";
            }
            sync_key = sync_key.TrimEnd('%', '7', 'C');

            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                List<string> listCheckUrl = new List<string>();
                listCheckUrl.Add("webpush.weixin.qq.com");
                listCheckUrl.Add("webpush2.weixin.qq.com");
                listCheckUrl.Add("webpush.wechat.com");
                listCheckUrl.Add("webpush1.wechat.com");
                listCheckUrl.Add("webpush2.wechat.com");
                listCheckUrl.Add("webpush.wechatapp.com");
                listCheckUrl.Add("webpush1.wechatapp.com");
                listCheckUrl.Add("webpush.wx2.qq.com");
                listCheckUrl.Add("webpush2.wx2.qq.com");
                listCheckUrl.Add("webpush.wx.qq.com");

                byte[] bytes = null;
                string ret_msg = "";
                foreach (var item in listCheckUrl)
                {
                    string url = "https://" + item + "/cgi-bin/mmwebwx-bin/synccheck?sid={0}&uin={1}&synckey={2}&r={3}&skey={4}&deviceid={5}";
                    _synccheck_url = string.Format(url, sid.Value, uin.Value, sync_key, (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds, LoginService.SKey.Replace("@", "%40"), "e1615250492");

                    //Log.WriteLogByDate("请求的url:" + _synccheck_url);
                    bytes = BaseService.SendGetRequest(_synccheck_url + "&_=" + DateTime.Now.Ticks);

                    //bytes = BaseService.SendGetRequest(_synccheck_url);
                    if (bytes != null)
                    {
                        ret_msg = Encoding.UTF8.GetString(bytes);
                        //Log.WriteLogByDate("ret_msg:"+ret_msg);
                        if (ret_msg.Contains("1100") || ret_msg.Contains("1101") || ret_msg.Contains("1102"))
                        {
                            continue;
                        }
                        break;
                    }
                    else
                    {
                        //Log.WriteLogByDate("返回值是空");   
                        continue;
                    }

                }
                return ret_msg;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 微信同步
        /// </summary>
        /// <returns></returns>
        public JObject WxSync()
        {
            StringBuilder s = new StringBuilder();//s数组中存放着需要的数字
            Random ra = new Random();
            for (int i = 0; i < 15; i++)//遍历数组显示结果
            {
                s.Append(ra.Next(1, 10));
            }

            string sync_json = "{{\"BaseRequest\" : {{\"DeviceID\":\"e" + s.ToString() + "\",\"Sid\":\"{1}\", \"Skey\":\"{5}\", \"Uin\":\"{0}\"}},\"SyncKey\" : {{\"Count\":{2},\"List\":[{3}]}},\"rr\" :{4}}}";
            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            string sync_keys = "";
            foreach (KeyValuePair<string, string> p in _syncKey)
            {
                sync_keys += "{\"Key\":" + p.Key + ",\"Val\":" + p.Value + "},";
            }

            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            long t = (DateTime.Now.Ticks - startTime.Ticks) / 10000;   //除10000调整为13位      

            int n = (int)t;
            n = ~n;

            sync_keys = sync_keys.TrimEnd(',');
            sync_json = string.Format(sync_json, uin.Value, sid.Value, _syncKey.Count, sync_keys, n, LoginService.SKey);

            if (sid != null && uin != null)
            {

                List<string> listCheckUrl = new List<string>();
                listCheckUrl.Add("https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxsync?sid=");
                listCheckUrl.Add("https://wx2.qq.com/cgi-bin/mmwebwx-bin/webwxsync?sid=");

                byte[] bytes = null;
                foreach (var item in listCheckUrl)
                {
                    try
                    {

                        string url = item + sid.Value + "&lang=zh_CN&skey=" + LoginService.SKey + "&pass_ticket=" + LoginService.Pass_Ticket;
                        //Log.WriteLogByDate("微信同步url:" + url);
                        bytes = BaseService.SendPostRequestAndSetCookies(url, sync_json);
                        //bytes = BaseService.SendPostRequest(item + sid.Value + "&skey=" + LoginService.SKey, sync_json);

                        string sync_str2 = Encoding.UTF8.GetString(bytes);
                        Log.WriteLogByDate("微信返回结果：" + sync_str2);
                        //if (sync_str2.Contains("1100"))
                        //{
                        //    continue;
                        //}
                        //else
                        //{
                        //    break;
                        //}
                        if (sync_str2.Contains("\"Ret\": 0"))
                        {
                            // Log.WriteLogByDate("消息同步返回结果是："+sync_str2);
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {

                        Log.WriteLog(ex);
                        continue;
                    }
                }


                if (bytes == null || bytes.Length == 0)
                {
                    return null;
                }
                string sync_str = Encoding.UTF8.GetString(bytes);

                //Log.WriteLogByDate("同步的消息是：" + sync_str);

                JObject sync_resul = JsonConvert.DeserializeObject(sync_str) as JObject;

                if (sync_resul["SyncKey"]["Count"].ToString() != "0")
                {
                    _syncKey.Clear();
                    foreach (JObject key in sync_resul["SyncKey"]["List"])
                    {
                        _syncKey.Add(key["Key"].ToString(), key["Val"].ToString());
                    }
                }
                return sync_resul;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="type"></param>
        public void SendMsg(string msg, string from, string to, int type)
        {
            StringBuilder s = new StringBuilder();//s数组中存放着需要的数字
            Random ra = new Random();
            for (int i = 0; i < 15; i++)//遍历数组显示结果
            {
                s.Append(ra.Next(1, 10));
            }

            string msg_json = "{{" +
            "\"BaseRequest\":{{" +
                "\"DeviceID\" : \"e" + s.ToString() + "\"," +
                "\"Sid\" : \"{0}\"," +
                "\"Skey\" : \"{6}\"," +
                "\"Uin\" : \"{1}\"" +
            "}}," +
            "\"Msg\" : {{" +
                "\"ClientMsgId\" : {8}," +
                "\"Content\" : \"{2}\"," +
                "\"FromUserName\" : \"{3}\"," +
                "\"LocalID\" : {9}," +
                "\"ToUserName\" : \"{4}\"," +
                "\"Type\" : {5}" +
            "}}," +
            "\"rr\" : {7}" +
            "}}";

            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                msg_json = string.Format(msg_json, sid.Value, uin.Value, msg, from, to, type, LoginService.SKey, DateTime.Now.Millisecond, DateTime.Now.Millisecond, DateTime.Now.Millisecond);

                List<string> listCheckUrl = new List<string>();
                listCheckUrl.Add("wx.qq.com");
                listCheckUrl.Add("wx2.qq.com");
                byte[] bytes = null;
                foreach (string item in listCheckUrl)
                {
                    string url = "https://" + item + "/cgi-bin/mmwebwx-bin/webwxsendmsg?sid=";
                    bytes = BaseService.SendPostRequest(url + sid.Value + "&lang=zh_CN&pass_ticket=" + LoginService.Pass_Ticket, msg_json);
                    if (bytes == null)
                    {
                        continue;
                    }
                    string send_result = Encoding.UTF8.GetString(bytes);

                    if (!send_result.Contains("\"Ret\": 0"))
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }


        public string GetQun(string qunUserName)
        {
            string msg_json = "";

            StringBuilder s = new StringBuilder();//s数组中存放着需要的数字
            Random ra = new Random();
            for (int i = 0; i < 15; i++)//遍历数组显示结果
            {
                s.Append(ra.Next(1, 10));
            }

            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                msg_json = "{\"BaseRequest\":{ \"Uin\":" + uin.Value + ",\"Sid\":\"" + sid.Value + "\",\"Skey\":\"" + LoginService.SKey + "\",\"DeviceID\":\"e" + s.ToString() + "\"},\"Count\":1,\"List\":[{\"UserName\":\"" + qunUserName + "\",\"EncryChatRoomId\":\"\"}]}";


                List<string> listCheckUrl = new List<string>();
                listCheckUrl.Add("wx.qq.com");
                listCheckUrl.Add("wx2.qq.com");
                byte[] bytes = null;
                foreach (string item in listCheckUrl)
                {
                    string url = "https://" + item + "/cgi-bin/mmwebwx-bin/webwxbatchgetcontact?type=ex";
                    bytes = BaseService.SendPostRequest(url + "&r=" + DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Split(new char[] { '.' })[0] + "&pass_ticket=" + LoginService.Pass_Ticket, msg_json);

                    if (bytes == null)
                    {
                        continue;
                    }
                    string send_result = Encoding.UTF8.GetString(bytes);

                    if (!send_result.Contains("\"Ret\": 0"))
                    {
                        continue;
                    }
                    else
                    {
                        return send_result;
                    }
                }
            }
            return "";
        }


        public string SendImage(string fileName, string filePath, string fileSize, string fromUser, string toUser, string fileMD5)
        {
            string ret = "";
            string send_result = "";
            try
            {
                List<string> listCheckUrl = new List<string>();
                listCheckUrl.Add("file.wx.qq.com");
                listCheckUrl.Add("file2.wx.qq.com");
                foreach (string item in listCheckUrl)
                {
                    StringBuilder s = new StringBuilder();//s数组中存放着需要的数字
                    Random ra = new Random();
                    for (int i = 0; i < 15; i++)//遍历数组显示结果
                    {
                        s.Append(ra.Next(1, 10));
                    }

                    string url = "https://" + item + "/cgi-bin/mmwebwx-bin/webwxuploadmedia?f=json";

                    string date = DateTime.Now.ToString("yyyy-MM-dd");
                    date = date.Replace("-", "/");
                    string time = DateTime.Now.ToString("ttH:mm:ss");

                    NameValueCollection nav = new NameValueCollection();
                    nav.Add("id", "WU_FILE_" + frmMainForm.Count);
                    nav.Add("name", fileName);
                    nav.Add("type", "image/png");
                    //nav.Add("type", "application/octet-stream");
                    nav.Add("lastModifiedDate", date + " " + time);
                    //nav.Add("chunks", "2");
                    //nav.Add("chunk", "1");
                    nav.Add("size", fileSize);
                    nav.Add("mediatype", "pic");

                    Cookie sid = BaseService.GetCookie("wxsid");
                    Cookie uin = BaseService.GetCookie("wxuin");

                    if (sid == null)
                    {
                        Log.WriteLogByDate("sid为空了");
                    }
                    if (uin == null)
                    {
                        Log.WriteLogByDate("uin为空了");
                    }

                    string baseRequest = "{ \"UploadType\":2,\"BaseRequest\":{ \"Uin\":" + uin.Value + ",\"Sid\":\"" + sid.Value + "\",\"Skey\":\"" + LoginService.SKey + "\",\"DeviceID\":\"e" + s.ToString() + "\"},\"ClientMediaId\":" + DateTime.Now.DateTimeToUnixTimestamp() + ",\"TotalLen\":" + fileSize + ",\"StartPos\":0,\"DataLen\":" + fileSize + ",\"MediaType\":4,\"FromUserName\":\"" + fromUser + "\",\"ToUserName\":\"" + toUser + "\",\"FileMd5\":\"" + fileMD5 + "\"}";

                    nav.Add("uploadmediarequest", baseRequest);
                    //string webwx_data_ticket = BaseService.GetCookie("webwx_data_ticket").Value;
                    Cookie webwx_data_ticket = BaseService.GetCookie("webwx_data_ticket");

                    if (webwx_data_ticket != null)
                    {
                        LoginService.webwx_data_ticket = webwx_data_ticket.Value;
                    }
                    else
                    {
                        Log.WriteLogByDate("webwx_data_ticket为空了");
                    }

                    nav.Add("webwx_data_ticket", LoginService.webwx_data_ticket);
                    nav.Add("pass_ticket", LoginService.Pass_Ticket);

                    ret = BaseService.HttpUploadFile(url, fileName, fileName, "image/png", nav);
                    //ret = BaseService.HttpUploadFile(url, fileName, fileName, "application/octet-stream", nav);
                    Log.WriteLogByDate("发送图片ret=" + ret);
                    if (string.IsNullOrEmpty(ret))
                    {
                        continue;
                    }

                    if (!ret.Contains("\"Ret\": 0"))
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }



                //发送图片
                JObject jobject = JsonConvert.DeserializeObject(ret) as JObject;
                string mediaId = jobject["MediaId"].ToString();

                List<string> listCheckUrl2 = new List<string>();
                listCheckUrl2.Add("wx.qq.com");
                listCheckUrl2.Add("wx2.qq.com");

                foreach (string item in listCheckUrl2)
                {
                    StringBuilder s = new StringBuilder();//s数组中存放着需要的数字
                    Random ra = new Random();
                    for (int i = 0; i < 15; i++)//遍历数组显示结果
                    {
                        s.Append(ra.Next(1, 10));
                    }

                    string url = "https://" + item + "/cgi-bin/mmwebwx-bin/webwxsendmsgimg?fun=async&f=json&pass_ticket=" + LoginService.Pass_Ticket;

                    Cookie sid = BaseService.GetCookie("wxsid");
                    Cookie uin = BaseService.GetCookie("wxuin");
                    string baseRequest = "{\"BaseRequest\":{ \"Uin\":" + uin.Value + ",\"Sid\":\"" + sid.Value + "\",\"Skey\":\"" + LoginService.SKey + "\",\"DeviceID\":\"e" + s.ToString() + "\"},\"Msg\":{\"Type\":3,\"MediaId\":\"" + mediaId + "\",\"FromUserName\":\"" + fromUser + "\",\"ToUserName\":\"" + toUser + "\",\"LocalID\":" + DateTime.Now.DateTimeToUnixTimestamp() + ",\"ClientMsgId\":" + DateTime.Now.DateTimeToUnixTimestamp() + "}}";

                    byte[] bytes = null;
                    bytes = BaseService.SendPostRequest(url, baseRequest);
                    if (bytes == null)
                    {
                        continue;
                    }
                    send_result = Encoding.UTF8.GetString(bytes);
                    if (!send_result.Contains("\"Ret\":0"))
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                //发生图片
                Log.WriteLogByDate("发送图片失败");
                Log.WriteLog(ex);
            }

            frmMainForm.Count++;
            Log.WriteLogByDate("发送图片返回结果：" + send_result);
            return send_result;
        }
    }
}
