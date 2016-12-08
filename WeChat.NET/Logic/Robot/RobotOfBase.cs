using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Web;
using Newtonsoft.Json.Linq;

using WeChat.NET.Logic.Common;
using WeChat.NET.Objects;
using WeChat.NET.Logic.Robot;
using WeChat.NET.Logic.Object;

using GameSystem.BaseFunc;

namespace WeChat.NET.Protocol
{
    /// <summary>
    /// 微信主要业务逻辑服务类
    /// </summary>
    public class RobotOfBase
    {
        #region 构造函数及核心信息

        public RobotOfBase(int _id) {
            this._robotId = _id;

            //创建存储临时资源的目录
            temp_pwd = System.Environment.CurrentDirectory + "\\assert";
            if (!Directory.Exists(temp_pwd)) {
                Directory.CreateDirectory(temp_pwd);
            }

            //聊天室管理器
            this.chatroomMgr = new ChatroomManager(this);
        }

        /// <summary>
        /// 机器人唯一编码
        /// </summary>
        public int robotId
        {
            get {
                return _robotId;
            }
        }

        private int _robotId = 0;
        /// <summary>
        /// 临时目录
        /// </summary>
        public string temp_pwd = "";
        /// <summary>
        /// 聊天室管理器
        /// </summary>
        public ChatroomManager chatroomMgr = null;
        /// <summary>
        /// 入群欢迎语
        /// </summary>
        public string welcome_content = "您好:)";
        /// <summary>
        /// 默认聊天室名称
        /// </summary>
        public string defaultRoomName = "273二手车交易群";

        #endregion

        #region 登录管理

        /// <summary>
        /// 获取缓存的二维码获取地址
        /// </summary>
        /// <returns></returns>
        public string getQrCodeUrl()
        {
            return WXConfig._qrcode_url + uuid;
        }
        private bool isLogout = true;
        /// <summary>
        /// 登录扫描检测
        /// http comet:
        /// tip=1, 等待用户扫描二维码,
        ///       201: scaned
        ///       408: timeout
        /// tip = 0, 等待用户确认登录,
        ///       200: confirmed
        /// </summary>
        public void LoginCheck(Action<RobotOfBase> onRefreshContact, Action<RobotOfBase, WXMsg> onMsg)
        {
            while (!Program.StopService)
            {
                try {
                    #region 初始化
                    loginResult ret = new loginResult();
                    uuid = "";
                    #endregion

                    if (get_uuid() == false)
                    {
                        Thread.Sleep(2000);
                        continue;
                    }
                    Console.WriteLine("请刷新页面http://localhost:" + Program.server.LocalPort + "，并使用微信扫描二维码");

                    int tip = 1;
                    int retry_time = 30;
                    while (retry_time > 0)
                    {
                        string login_result = BaseService.SimpleGetStr(
                            "https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login",
                            createJObject(new Dictionary<string, object>() {
                                { "loginicon", "true" },
                                { "tip", tip },
                                { "uuid", uuid },
                                { "_", UserFunc.GetTimeStampWithThousand() },
                            }));
                        if (string.IsNullOrWhiteSpace(login_result)) {
                            continue;
                        }
                        ret.code = (new Regex("window.code=(\\d+);")).Match(login_result).Groups[1].Value;
                        if (ret.code == SyncStatus.SCANED)
                        {//已扫描 未登录
                            tip = 0;

                            //Start：解析登录用户头像 - 不必要的流程，已经封存
                            //string[] results = login_result.Split(new string[] { "\'" }, StringSplitOptions.None);
                            //if (results.Length > 1)
                            //{
                            //    string base64_image = results[1].Split(',')[1];
                            //    byte[] base64_image_bytes = Convert.FromBase64String(base64_image);
                            //    MemoryStream memoryStream = new MemoryStream(base64_image_bytes, 0, base64_image_bytes.Length);
                            //    memoryStream.Write(base64_image_bytes, 0, base64_image_bytes.Length);
                            //    //转成图片
                            //    ret.image = Image.FromStream(memoryStream);
                            //}
                            //else
                            //{//用户有可能没有设置头像
                            //    ret.image = null;
                            //}
                            //End

                            //已扫描 未登录
                            Console.WriteLine("请点击微信上的登录按钮");
                        }
                        else if (ret.code == SyncStatus.SUCCESS)
                        {//已扫描 已登录
                            ret.url = (new Regex("window.redirect_uri=\"(\\S+?)\";")).Match(login_result).Groups[1].Value;

                            redirect_uri = ret.url + "&fun=new";
                            base_uri = redirect_uri.Substring(0, redirect_uri.LastIndexOf("/"));
                            string temp_host = base_uri.Substring(8);
                            base_host = temp_host.Substring(0, temp_host.IndexOf("/"));

                            //已完成登录 访问登录跳转URL
                            if (Flow_Login() == true)
                            {
                                Console.WriteLine("微信登录已成功");
                            }
                            else
                            {
                                ret.code = SyncStatus.TIMEOUT;
                            }
                            break;
                        }
                        else if (ret.code == SyncStatus.TIMEOUT)
                        {//超时
                            break;
                        }

                        retry_time -= 1;
                        Thread.Sleep(1000);
                    }

                    if (ret.code == SyncStatus.SUCCESS)
                    {
                        this.isLogout = false;
                        //进入消息循环
                        this.Flow_Run(onRefreshContact, onMsg);
                    }
                }
                catch(Exception ex) {
                    if (Program.isDebug) {
                        Console.WriteLine(ex.Message);
                    }
                }

                //因为各种原因未能进入消息循环，或已进入后退出：提示用户重新登录
                Thread.Sleep(3000);
                Console.WriteLine("连接异常或已签退，请刷新页面、重新扫描二维码并登录");
            }
        }
        /// <summary>
        /// 登录流程会话ID
        /// </summary>
        private string uuid = "";

        /// <summary>
        /// 获取uuid - 登录流程会话ID
        /// </summary>
        /// <returns></returns>
        private bool get_uuid()
        {
            string init_str = BaseService.SimpleGetStr("https://login.weixin.qq.com/jslogin", 
                createJObject(new Dictionary<string, object>(){
                { "appid", "wx782c26e4c19acffb"},
                { "redirect_uri", "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxnewloginpage"},
                { "fun", "new"},
                { "lang", "zh_CN"},
                { "_", UserFunc.GetTimeStampWithThousand()},
            }));
            Match pm = Regex.Match(init_str, "window.QRLogin.code = (\\d+); window.QRLogin.uuid = \"(\\S+?)\";");
            if (pm != null && pm.Groups.Count > 1)
            {
                string code = pm.Groups[1].Value;
                //设置获取的结果
                uuid = pm.Groups[2].Value;
                return code == "200";
            }
            else
            {
                Console.WriteLine("获取UUID失败");
            }
            return false;
        }

        #endregion

        /// <summary>
        /// 获取远程对象
        /// </summary>
        /// <param name="url"></param>
        /// <param name="_params"></param>
        /// <returns></returns>
        public static JObject fetchObject(string url, JObject _params)
        {
            return BaseService.fetchObject(url, _params);
        }

        /// <summary>
        /// 根据给定的字典，创建JObject对象
        /// </summary>
        /// <param name="list">数据字典</param>
        /// <returns></returns>
        public static JObject createJObject(Dictionary<string, object> list)
        {
            return BaseService.createJObject(list);
        }

        /// <summary>
        /// 根据用户昵称，获取用户对象
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public BaseContact FindByName(string name)
        {
            var _find = this.contactList.Where((item) => { return item.Value.RemarkName == name; });
            if (_find.Count() == 0) {
                _find = this.contactList.Where((item) => { return item.Value.NickName == name; });
            }
            if (_find.Count() == 0) {
                _find = this.contactList.Where((item) => { return item.Value.DisplayName == name; });
            }

            return _find.Count() == 0 ? null : _find.First().Value;
        }

        /// <summary>
        /// 访问地址头，一般为 https://wx.qq.com/cgi-bin/mmwebwx-bin
        /// </summary>
        public string base_uri = "";
        /// <summary>
        /// 访问主机，一般为 wx.qq.com
        /// </summary>
        public string base_host = "";
        /// <summary>
        /// 登录地址，用于login流程
        /// </summary>
        public string redirect_uri = "";
        /// <summary>
        /// 违禁词汇列表
        /// </summary>
        public List<string> blackWords = new List<string>();

        #region 同步所用的key
        private JObject sync_key = new JObject();
        private void RefreshSyncKey(JObject _syncKey)
        {
            sync_key = _syncKey;
            sync_key_str = "";
            foreach (JObject synckey in sync_key["List"])  //同步键值
            {
                if (sync_key_str != "")
                {
                    sync_key_str += "|";// "%7C";
                }
                sync_key_str += synckey["Key"].ToString() + '_' + synckey["Val"].ToString();
            }
        }
        private string sync_key_str = "";
        public string pass_ticket = "";
        /// <summary>
        /// 当前可用推送服务器
        /// </summary>
        public string sync_host = "webpush.";
        #endregion

        /// <summary>
        /// 当前登录微信用户
        /// </summary>
        private BaseContact my_account = null;
        public BaseContact getMyAccount() {
            return my_account;
        }

        /// <summary>
        /// 最近联系人列表
        /// </summary>
        public List<BaseContact> contact_latest = new List<BaseContact>();

        /// <summary>
        /// 所有联系人列表 以联系人标识作为索引
        /// </summary>
        public Dictionary<string, BaseContact> contactList = new Dictionary<string, BaseContact>();

        /// <summary>
        /// 签退流程
        /// </summary>
        /// <returns></returns>
        public bool Flow_Logout() {
            this.isLogout = true;

            JObject _params = createJObject(new Dictionary<string, object>(){
                { "sid", base_request["Sid"]},
                { "uin", base_request["Uin"]},
            });
            string url = string.Format("https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxlogout?redirect=1&type=1&skey={0}", base_request["Skey"]);
            try
            {
                BaseService.fetchObject(url, _params);
            }
            catch
            {
            }
            return true;
        }

        /// <summary>
        /// 登录流程，获取sid uid, 结果放入base_request
        /// </summary>
        public bool Flow_Login()
        {
            string uin = "";
            string sid = "";
            string skey = "";
            string device_id = "e" + (new Random()).Repeat(0, 10, 10);

            XmlElement root = BaseService.fetchXmlDoc(this.redirect_uri);
            if (root == null) {
                return false;
            }
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.Name == "skey")
                {
                    skey = node.ChildNodes[0].Value;
                }
                else if (node.Name == "wxsid")
                {
                    sid = node.ChildNodes[0].Value;
                }
                else if (node.Name == "wxuin")
                {
                    uin = node.ChildNodes[0].Value;
                }
                else if (node.Name == "pass_ticket")
                {
                    pass_ticket = node.ChildNodes[0].Value;
                }
            }

            if (String.IsNullOrEmpty(skey) || String.IsNullOrEmpty(sid) || String.IsNullOrEmpty(uin) || String.IsNullOrEmpty(pass_ticket))
            {
                return false;
            }

            base_request = createJObject(new Dictionary<string, object>(){
                { "Uin", Int64.Parse(uin)},
                { "Sid", sid },
                { "Skey", skey },
                { "DeviceID", device_id },
            });
            return true;
        }
        public JObject base_request = new JObject();

        /// <summary>
        /// 登录后的初始化流程 - 获取联系人列表、监听消息
        /// </summary>
        /// <param name="_init"></param>
        /// <param name="callback"></param>
        public void Flow_Run(Action<RobotOfBase> _init, Action<RobotOfBase, WXMsg> callback) {
            if (Flow_Init()) {
                //获取联系人列表
                Flow_RefreshContact();

                //外部回调
                _init(this);

                //监听消息
                Flow_ProcMsg(callback);
            }
        }

        /// <summary>
        /// 检查新消息列表，遍历执行消息回调
        /// </summary>
        /// <param name="p"></param>
        private void Flow_ProcMsg(Action<RobotOfBase, WXMsg> callback)
        {
            //test_sync_check(); //测试多个Push服务器的可用性，暂时封闭
            while (!this.isLogout)
            {
                Int64 check_time = UserFunc.GetTimeStampWithThousand();
                try
                {
                    List<string> ret = sync_check(); //同步检查
                    string retcode = ret[0];
                    string selector = ret[1];

                    if (retcode == "1100")
                    {//  # 从微信客户端上登出
                        break;
                    }
                    else if (retcode == "1101")
                    { // # 从其它设备上登了网页微信
                        break;
                    }
                    else if (retcode == "0")
                    {
                        if (selector == "2")
                        { //# 有新消息
                            JObject r = sync();
                            if (r != null)
                            {
                                handle_msg(r, callback);
                            }
                        }
                        else if (selector == "3")
                        { //# 未知
                            JObject r = sync();
                            if (r != null)
                            {
                                handle_msg(r, callback);
                            }
                        }
                        else if (selector == "4")
                        {// # 通讯录更新
                            JObject r = sync();
                            if (r != null)
                            {
                                Flow_RefreshContact();
                            }
                        }
                        else if (selector == "6")
                        {//# 可能是红包
                            JObject r = sync();
                            if (r != null)
                            {
                                handle_msg(r, callback);
                            }
                        }
                        else if (selector == "7")
                        {// # 在手机上操作了微信
                            JObject r = sync();
                            if (r != null)
                            {
                                handle_msg(r, callback);
                            }
                        }
                        else if (selector == "0")
                        { //# 无事件
                        }
                        else
                        {
                            JObject r = sync();
                            if (r != null)
                            {
                                handle_msg(r, callback);
                            }
                        }
                    }
                    else {
                        Thread.Sleep(100);
                    }
                    schedule();
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
                check_time = UserFunc.GetTimeStampWithThousand() - check_time;
                if (check_time < 1000) {
                    Thread.Sleep(1000 - (int)check_time);
                }
            }
        }

        /// <summary>
        /// 微信初始化, 获取当前登录账号信息，以及最近联系人列表
        /// </summary>
        /// <returns></returns>
        public bool Flow_Init()
        {
            JObject _params = createJObject(new Dictionary<string, object>() {
                {"BaseRequest", base_request },
            });

            bool retCode = false;
            JObject init_result = fetchObject(String.Format(base_uri + "/webwxinit?r={0}&lang=zh_CN&pass_ticket={1}", UserFunc.GetTimeStampWithThousand(), pass_ticket), _params);
            if (init_result != null) {
                retCode = init_result["BaseResponse"]["Ret"].ToString() == "0";
                if (true == retCode)
                {
                    my_account = new Contact(this, (JObject)init_result["User"]);
                    RefreshSyncKey((JObject)init_result["SyncKey"]);

                    foreach (JObject contact in init_result["ContactList"])  //部分好友名单
                    {
                        UpdateContact(contact);
                        contact_latest.Add(new BaseContact(this, contact));
                    }

                    string url = String.Format(base_uri + "/webwxstatusnotify?lang=zh_CN&pass_ticket={0}", pass_ticket);
                    _params = createJObject(new Dictionary<string, object>(){
                        {"BaseRequest", base_request},
                        {"Code", 3},
                        {"FromUserName", my_account.Id },
                        {"ToUserName", my_account.Id },
                        {"ClientMsgId", UserFunc.GetTimeStampWithThousand()},
                    });
                    init_result = fetchObject(url, _params);
                    return init_result != null && init_result["BaseResponse"]["Ret"].ToString() == "0";
                }
            }

            return retCode;
        }

        /// <summary>
        /// 新增或更新联系人信息
        /// </summary>
        /// <param name="contact"></param>
        private BaseContact UpdateContact(JToken contact) {
            BaseContact item = null;
            if ((contact["VerifyFlag"].ToString().ToInt32() & 8) != 0)
            {// 公众号
                item = new PublicContact(this, (JObject)contact);
            }
            else if (WXConfig.special_users.Contains(contact["UserName"].ToString()))
            {//特殊账户
                item = new SpecialContact(this, (JObject)contact);
            }
            else if (contact["UserName"].ToString().IndexOf("@@") != -1)
            {//群聊
                item = new Chatroom(this, (JObject)contact);
                //同时添加到聊天室列表
                this.chatroomMgr.AddChatroom((Chatroom)item);
            }
            else if (contact["UserName"].ToString() == getMyAccount().Id)
            { //自己
                item = new SelfContact(this, (JObject)contact);
            }
            else
            {
                item = new Contact(this, (JObject)contact);
            }

            if (item != null) {
                this.contactList[item.Id] = item;
            }

            return item;
        }

        /// <summary>
        /// 获取当前账户的所有相关账号(包括联系人、公众号、群聊、特殊账号)
        /// </summary>
        public void Flow_RefreshContact() {
            //通讯录
            string url = base_uri + String.Format("/webwxgetcontact?pass_ticket={0}&skey={1}&r={2}", pass_ticket, base_request["Skey"], UserFunc.GetTimeStampWithThousand());
            JObject _params = new JObject();
            JObject dic = fetchObject(url, _params);
            if (dic != null)
            {
                this.contactList.Clear();
                foreach (JObject contact in dic["MemberList"])  //完整好友名单
                {
                    UpdateContact(contact);
                }
            }

            batch_get_group_members();
        }

        /// <summary>
        /// 索引器 - 获取通讯录对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BaseContact this[string id] {
            get {
                return this.contactList.ContainsKey(id) ? this.contactList[id] : null;
            }
        }

        /// <summary>
        /// 获取指定联系人的名称集合
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public JObject get_contact_name(string uid)
        {
            return this[uid] == null ? null : this[uid].get_name();
        }

        /// <summary>
        /// 获取名称集合中适合显示的名称字符串
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string get_contact_prefer_name(JObject name) {
            string ret = "";
            if (name != null) {
                if (name.hasKey("remark_name"))
                {
                    ret = name.Property("remark_name").Value.ToString();
                }
                else if (name.hasKey("nickname"))
                {
                    ret = name.Property("nickname").Value.ToString();
                }
                else if (name.hasKey("display_name"))
                {
                    ret = name.Property("display_name").Value.ToString();
                }
            }
            return ret;
        }

        /// <summary>
        /// 获取特定账号与自己的关系
        /// </summary>
        /// <param name="wx_user_id">账号id</param>
        /// <returns>与当前账号的关系</returns>
        public ContactTypeEnum get_user_type(string wx_user_id) {
            var contact = this[wx_user_id];
            if (contact != null) {
                return contact.type;
            }
            return ContactTypeEnum.Unknown;
        }

        public bool is_contact(string uid) {
            return this[uid] != null && this[uid].type == ContactTypeEnum.Contact;
        }

        public bool is_public(string uid) {
            return this[uid] != null && this[uid].type == ContactTypeEnum.Public;
        }

        public bool is_special(string uid) {
            return this[uid] != null && this[uid].type == ContactTypeEnum.Special;
        }

        /// <summary>
        /// 微信同步状态
        /// </summary>
        /// <returns></returns>
        public List<string> sync_check()
        {
            JObject _params = createJObject(new Dictionary<string, object>(){
                { "r", UserFunc.GetTimeStampWithThousand()},
                { "sid", base_request["Sid"]},
                { "uin", base_request["Uin"]},
                { "skey", base_request["Skey"]},
                { "deviceid", base_request["DeviceID"]},
                { "synckey", sync_key_str},
                { "_", UserFunc.GetTimeStampWithThousand()},
            });
            string url = string.Format("https://{0}/cgi-bin/mmwebwx-bin/synccheck{1}", this.sync_host + base_host, _params.ToUrl());
            try
            {
                string data = BaseService.fetchString(url);
                Match pm = new Regex("window.synccheck={retcode:\"(\\d+)\",selector:\"(\\d+)\"}").Match(data);
                string retcode = pm.Groups[1].Value;
                string selector = pm.Groups[2].Value;
                return new List<string>() { retcode, selector };
            }
            catch
            {
                return new List<string>() { "-1", "-1" };
            }
        }

        /// <summary>
        /// 微信同步消息
        /// </summary>
        /// <returns></returns>
        public JObject sync()
        {
            string url = String.Format(base_uri + "/webwxsync?sid={0}&skey={1}&lang=zh_CN&pass_ticket={2}", base_request["Sid"], base_request["Skey"], pass_ticket);
            JObject _params = createJObject(new Dictionary<string, object>(){
                {"BaseRequest", base_request },
                {"SyncKey", sync_key },
                {"rr", UserFunc.GetTimeStampWithThousand() },
            });
            try
            {
                JObject dic = fetchObject(url, _params);
                if (dic != null && dic["BaseResponse"]["Ret"].ToString() == "0")
                {
                    RefreshSyncKey((JObject)dic["SyncKey"]);
                }
                return dic;
            }
            catch
            {
                return null;
            }
        }

        public string search_content(string key, string content, string fmat = "attr")
        {
            if (fmat == "attr") {
                Match mc = (new Regex(key + @"\s?=\s?\'([^\'<]+)\'")).Match(content);
                if (mc != null && mc.Groups.Count > 0) {
                    return mc.Groups[1].Value;
                }
            }
            else if (fmat == "xml") {
                Match mc = (new Regex(String.Format("<{0}>([^<]+)</{0}>", key))).Match(content);
                if (mc != null && mc.Groups.Count > 0)
                {
                    return mc.Groups[1].Value;
                }
            }

            return "unknown";
        }

        /// <summary>
        /// 解析附加消息内容
        /// </summary>
        /// <param name="msg_src_type_id">消息来源类型id</param>
        /// <param name="msg">消息结构体</param>
        /// <returns>解析的消息</returns>
        public JObject extract_msg_content(MsgSrcTypeEnum msg_src_type_id, JObject msg)
        {
            int mtype = int.Parse(msg["MsgType"].ToString());
            string content = Microsoft.JScript.GlobalObject.unescape(msg["Content"].ToString());
            long msg_id = long.Parse(msg["MsgId"].ToString());

            JObject msg_content = new JObject();
            if (msg_src_type_id == 0) {
                msg_content["type"] = (int)ContentTypeEnum.Empty;
                msg_content["data"] = "";
                return msg_content;
            }
            else if (msg_src_type_id == MsgSrcTypeEnum.FileHelper) {
                msg_content["type"] = (int)ContentTypeEnum.Text;
                msg_content["data"] = content.Replace("<br/>", "\n");
                return msg_content;
            }
            else if (msg_src_type_id == MsgSrcTypeEnum.Group) {
                int sp = content.IndexOf("<br/>");
                string uid = content.Substring(0, sp);
                uid = uid.Substring(0, uid.Length - 1);
                string contentFinal = content.Substring(sp);
                content = contentFinal.Replace("<br/>", "");

                string name = get_contact_prefer_name(get_contact_name(uid));
                if (String.IsNullOrEmpty(name)) {
                    var room = this.chatroomMgr[msg["FromUserName"].ToString()];
                    if (room != null) {
                        name = room.get_group_member_prefer_name(room.get_group_member_name(uid));
                    }
                }
                if (string.IsNullOrEmpty(name)) {
                    name = "unknown";
                }

                msg_content["user"] = createJObject(new Dictionary<string, object>() {
                    {"id", uid },
                    {"name", name },
                });
            }
            else
            { //  # Self, Contact, Special, Public, Unknown
            }

            string msg_prefix = (msg_content.GetValue("user") != null) ? (msg_content["user"]["name"].ToString() + ":") : "";

            if (mtype == 1) {
                if (content.IndexOf("http://weixin.qq.com/cgi-bin/redirectforward?args=") != -1)
                {
                    string data = BaseService.fetchString(content);
                    string pos = search_content("title", data, "xml");
                    msg_content["type"] = (int)ContentTypeEnum.Location;
                    msg_content["data"] = pos;
                    msg_content["detail"] = data;
                }
                else {
                    msg_content["type"] = (int)ContentTypeEnum.Text;
                    if (msg_src_type_id == MsgSrcTypeEnum.Group || (msg_src_type_id == MsgSrcTypeEnum.Self && msg["ToUserName"].ToString().Substring(0, 2) == "@@"))
                    { //  # Group text message
                        msg_content["data"] = "";
                        msg_content["desc"] = "";
                        msg_content["detail"] = new JArray();
                        if (!String.IsNullOrEmpty(content))
                        {
                            string[] segs = content.Split('\u2005');
                            string str_msg_all = "";
                            string str_msg = "";
                            JArray infos = new JArray();
                            string info_template = "{{\"type\":\"{0}\", \"value\":\"{1}\"}}";
                            if (segs.Length > 1)
                            {
                                for (int i = 0; i < segs.Length - 1; i++)
                                {
                                    segs[i] += '\u2005';
                                    Regex reg = new Regex("@.*\u2005");
                                    string pm = reg.Match(segs[i]).Groups[0].Value;
                                    if (!String.IsNullOrEmpty(pm))
                                    {
                                        string _name = pm.Substring(1).Trim();
                                        string s = segs[i].Replace(pm, String.Empty);
                                        str_msg_all += s + '@' + _name + ' ';
                                        str_msg += s;
                                        if (!String.IsNullOrEmpty(s))
                                        {
                                            infos.Add(JObject.Parse(String.Format(info_template, "str", s)));
                                        }
                                        infos.Add(JObject.Parse(String.Format(info_template, "at", _name)));
                                    }
                                    else
                                    {
                                        infos.Add(JObject.Parse(String.Format(info_template, "str", segs[i])));
                                        str_msg_all += segs[i];
                                        str_msg += segs[i];
                                    }
                                }
                                str_msg_all += segs[segs.Length - 1];
                                str_msg += segs[segs.Length - 1];
                            }
                            else
                            {
                                str_msg_all = content;
                                str_msg = content;
                            }
                            if (segs.Length > 0)
                            {
                                infos.Add(JObject.Parse(String.Format(info_template, "str", segs[segs.Length - 1])));
                            }
                            msg_content["data"] = str_msg_all.Replace("\u2005", String.Empty);
                            msg_content["desc"] = str_msg.Replace("\u2005", String.Empty);
                            msg_content["detail"] = infos;
                        }
                    }
                    else {
                        msg_content["data"] = content;
                    }
                }
            }
            else if (mtype == 3)
            {
                msg_content["type"] = (int)ContentTypeEnum.Image;
                msg_content["data"] = get_msg_img_url(msg_id.ToString());
                //msg_content["img"] = BaseService.GetBytes(msg_content["data"].ToString()); //encode("hex")
            }
            else if (mtype == 34) {
                msg_content["type"] = (int)ContentTypeEnum.Voice;
                msg_content["data"] = get_voice_url(msg_id.ToString());
                //msg_content["voice"] = BaseService.GetBytes(msg_content["data"].ToString());
            }
            else if (mtype == 37) {
                msg_content["type"] = (int)ContentTypeEnum.RecommendInfo;
                msg_content["data"] = msg["RecommendInfo"];
            }
            else if (mtype == 42) {
                msg_content["type"] = (int)ContentTypeEnum.Recommend;
                JToken info = msg["RecommendInfo"];
                string[] sexList = { "unknown", "male", "female" };
                msg_content["data"] = createJObject(new Dictionary<string, object>() {
                    { "nickname", info["NickName"] },
                    { "alias", info["Alias"] },
                    { "province", info["Province"] },
                    { "city", info["City"] },
                    { "gender", sexList[int.Parse(info["Sex"].ToString())] }
                });
            }
            else if (mtype == 47) {
                msg_content["type"] = (int)ContentTypeEnum.Animation;
                msg_content["data"] = search_content("cdnurl", content);
            }
            else if (mtype == 49) {
                msg_content["type"] = (int)ContentTypeEnum.Share;
                string app_msg_type = "";
                if (int.Parse(msg["AppMsgType"].ToString()) == 3) {
                    app_msg_type = "music";
                }
                else if (int.Parse(msg["AppMsgType"].ToString()) == 5) {
                    app_msg_type = "link";
                }
                else if (int.Parse(msg["AppMsgType"].ToString()) == 7) {
                    app_msg_type = "weibo";
                }
                else {
                    app_msg_type = "unknown";
                }
                msg_content["data"] = createJObject(new Dictionary<string, object>(){
                    { "type", app_msg_type},
                    { "title", msg["FileName"]},
                    { "desc",  search_content("des", content, "xml")},
                    { "url", msg["Url"]},
                    { "from", search_content("appname", content, "xml")},
                    { "content", msg["Content"] }, //# 有的公众号会发一次性3 4条链接一个大图,如果只url那只能获取第一条,content里面有所有的链接
                });
            }
            else if (mtype == 62) {
                msg_content["type"] = (int)ContentTypeEnum.Video;
                msg_content["data"] = content;
            }
            else if (mtype == 53) {
                msg_content["type"] = (int)ContentTypeEnum.VideoCall;
                msg_content["data"] = content;
            }
            else if (mtype == 10002) {
                msg_content["type"] = (int)ContentTypeEnum.Redraw;
                msg_content["data"] = content;
            }
            else {
                msg_content["type"] = (int)ContentTypeEnum.Unknown;
                msg_content["data"] = content;
            }
            return msg_content;
        }

        /// <summary>
        /// 处理原始微信消息的内部函数
        /// msg_type_id:
        ///    0 -> Init
        ///    1 -> Self
        ///    2 -> FileHelper
        ///    3 -> Group
        ///    4 -> Contact
        ///    5 -> Public
        ///    6 -> Special
        ///    99 -> Unknown
        /// </summary>
        /// <param name="r">原始微信消息</param>
        private void handle_msg(JObject r, Action<RobotOfBase, WXMsg> callback) {
            //检测联系人列表是否发生变化
            if (r["ModContactCount"].ToString().ToInt32() > 0) {
                foreach (var item in r["ModContactList"])
                {
                    var con = UpdateContact(item);
                    if (con is Chatroom) {
                        Chatroom rm = (Chatroom)con;
                        foreach (var v in item["MemberList"]) {
                            BaseContact mb = new BaseContact(this, (JObject)v);
                            if (rm[mb.NickName] == null) {
                                //对新入群的联系人发送欢迎语
                                send_msg_by_uid(string.Format("@{0} {1}", mb.NickName, rm.welcome_content), con.Id);
                            }
                        }
                        batch_get_group_members(con.Id);
                    }
                }
            }

            //todo...
            if (r["DelContactCount"].ToString().ToInt32() > 0)
            {
                foreach (var item in r["DelContactList"]) {
                }
            }

            //todo...
            if (r["ModChatRoomMemberCount"].ToString().ToInt32() > 0)
            {
                foreach (var item in r["ModChatRoomMemberCount"])
                {
                }
            }

            foreach (JObject msg in r["AddMsgList"]) {
                WXMsg mObj = new WXMsg(msg);
                MsgSrcTypeEnum msg_src_type_id = 0;
                JObject user = createJObject(new Dictionary<string, object>(){
                    { "id", msg["FromUserName"]},
                    { "name", "unknown" },
                });

                if (int.Parse(msg["MsgType"].ToString()) == 51)
                {// # init message
                    msg_src_type_id = MsgSrcTypeEnum.Init;
                    user["name"] = "system";
                }
                else if (int.Parse(msg["MsgType"].ToString()) == 37)
                {//  # friend request
                    msg_src_type_id = MsgSrcTypeEnum.FriendReq;
                    //自动添加好友
                    apply_userAdd_requests((JObject)msg["RecommendInfo"]);
                    continue;
                }
                //# content = msg["Content"]
                //# username = content[content.index("fromusername="): content.index("encryptusername")]
                //# username = username[username.index(""") + 1: username.rindex(""")]
                //# print u"[Friend Request]"
                //# print u"       Nickname：" + msg["RecommendInfo"]["NickName"]
                //# print u"       附加消息："+msg["RecommendInfo"]["Content"]
                //# print u"Ticket："+msg["RecommendInfo"]["Ticket"] # Ticket添加好友时要用
                //# print u"       微信号："+username #未设置微信号的 腾讯会自动生成一段微信ID 但是无法通过搜索 搜索到此人
                else if (msg["FromUserName"].ToString() == getMyAccount().Id)
                {//  # Self
                    msg_src_type_id = MsgSrcTypeEnum.Self;
                    user["name"] = "self";
                }
                else if (msg["ToUserName"].ToString() == "filehelper")
                {//# File Helper
                    msg_src_type_id = MsgSrcTypeEnum.FileHelper;
                    user["name"] = "file_helper";
                }
                else if (msg["FromUserName"].ToString().Substring(0, 2) == "@@")
                {//# Group
                    msg_src_type_id = MsgSrcTypeEnum.Group;
                    user["name"] = get_contact_prefer_name(get_contact_name(user["id"].ToString()));

                    if (mObj.Type == 10000)
                    {//当机器人主动邀请人加入时，会收到10000消息，此时可以发送欢迎语
                        if (mObj.Status == 3 || mObj.Status == 4)//加入
                        {
                            string[] sArray = mObj.Msg.ToString().Split('\"');
                            string m_str;
                            if (sArray.Length <= 3)
                            {
                                m_str = String.Format("@{0}  {1}", sArray[1], this.welcome_content);
                            }
                            else
                            {
                                m_str = String.Format("@{0}  {1}", sArray[3], this.welcome_content);
                            }
                            send_msg(mObj.To, m_str);
                        }
                        continue;
                    }

                }
                else if (is_contact(msg["FromUserName"].ToString()))
                {//  # Contact
                    msg_src_type_id = MsgSrcTypeEnum.Contact;
                    user["name"] = get_contact_prefer_name(get_contact_name(user["id"].ToString()));
                }
                else if (is_public(msg["FromUserName"].ToString()))
                { // # Public
                    msg_src_type_id = MsgSrcTypeEnum.Public;
                    user["name"] = get_contact_prefer_name(get_contact_name(user["id"].ToString()));
                }
                else if (is_special(msg["FromUserName"].ToString()))
                { // # Special
                    msg_src_type_id = MsgSrcTypeEnum.Special;
                    user["name"] = get_contact_prefer_name(get_contact_name(user["id"].ToString()));
                }
                else {
                    msg_src_type_id = MsgSrcTypeEnum.Unknown;
                    user["name"] = "unknown";
                }

                if (user.GetValue("name") == null) {
                    user["name"] = "unknown";
                }
                user["name"] = Microsoft.JScript.GlobalObject.unescape(user["name"].ToString());

                JObject content = extract_msg_content(msg_src_type_id, msg);
                JObject msgObj = createJObject(new Dictionary<string, object>(){
                        { "msg_type_id", msg_src_type_id },
                        { "msg_id", msg["MsgId"]},
                        { "content", content},
                        { "to_user_id", msg["ToUserName"]},
                        { "user", user},
                    });
                ((Action)delegate ()
                {
                    handle_msg_all(msgObj);
                }).BeginInvoke(null, null);

                callback(this, mObj);
            }
        }

        /// <summary>
        /// 做任务型事情的函数，如果需要，可以在子类中覆盖此函数
        /// 此函数在处理消息的间隙被调用，请不要长时间阻塞此函数
        /// </summary>
        public virtual void schedule() {
        }

        /// <summary>
        /// 处理所有消息，请子类化后覆盖此函数
        /// msg:
        ///    msg_id  ->  消息id
        ///    msg_type_id  ->  消息类型id
        ///    user  ->  发送消息的账号id
        ///    content  ->  消息内容
        /// </summary>
        /// <param name="msg">收到的消息</param>
        public virtual void handle_msg_all(JObject msg) {
        }

        /// <summary>
        /// 处理用户加好友请求
        /// </summary>
        /// <param name="RecommendInfo"></param>
        /// <returns></returns>
        public bool apply_userAdd_requests(JObject RecommendInfo) {
            string url = base_uri + "/webwxverifyuser?r=" + UserFunc.GetTimeStampWithThousand().ToString() + "&lang=zh_CN";
            JObject _params = createJObject(new Dictionary<string, object>() {
                {"BaseRequest", base_request },
                {"Opcode", 3},
                {"VerifyUserListSize", 1},
                {"VerifyUserList", new JArray(
                    new JObject[]{
                        createJObject(new Dictionary<string, object>() {
                            { "Value", RecommendInfo["UserName"] },
                            { "VerifyUserTicket", RecommendInfo["Ticket"] },
                        })
                    }
                )},
                {"VerifyContent", "" },
                {"SceneListCount", 1 },
                {"SceneList", new int[] { 33 } },
                {"skey", base_request["Skey"] },
            });

            try {
                JObject init_result = fetchObject(url, _params);
                return init_result != null && init_result["BaseResponse"]["Ret"].ToString() == "0";
            }
            catch {
                return false;
            }
        }

        #region 群组管理

        public void set_welcome_content(string gid, string _content)
        {
            var room = this.chatroomMgr[gid];
            if (room != null) {
                room.welcome_content = _content;
            }
        }

        public bool set_group_name(string room, string name)
        {
            var rm = this.chatroomMgr[room];
            if (rm != null)
            {
                return rm.setName(name);
            }
            return false;
        }

        /// <summary>
        /// 创建聊天室后，或者聊天室发生变化后，更新指定聊天室成员列表
        /// </summary>
        /// <param name="roomName"></param>
        private void batch_get_group_members(string roomName)
        {
            var ar = new JArray();
            ar.Add(createJObject(new Dictionary<string, object>() {
                        { "UserName", roomName },
                        { "ChatRoomId", "" },
                        { "EncryChatRoomId", "" },
                    }));
            batch_get_group_members(ar);
        }

        /// <summary>
        /// 批量更新所有聊天室成员列表
        /// </summary>
        private void batch_get_group_members() {
            var list = this.chatroomMgr.roomList.ToJArray((item) =>
            {
                //创建聊天室后，WebChat调用时填写的是ChatRoomId，而不是EncryChatRoomId，有什么区别呢？
                return createJObject(new Dictionary<string, object>() {
                        { "UserName", item.Id },
                        { "ChatRoomId", "" },
                        { "EncryChatRoomId", "" },
                    });
            });
            batch_get_group_members(list);
        }

        /// <summary>
        /// 根据指定的聊天室名称列表，批量更新成员列表
        /// </summary>
        private void batch_get_group_members(JArray curArray)
        {
            string url = String.Format(base_uri + "/webwxbatchgetcontact?type=ex&r={0}&pass_ticket={1}", UserFunc.GetTimeStampWithThousand(), pass_ticket);

            /*
            {
                "BaseRequest":{
                    "Uin":1071396966,
                    "Sid":"fzb9nb4pXESo3/09",
                    "Skey":"@crypt_5d15675a_044351569aeff8ab0b220900eb56a9ac",
                    "DeviceID":"e721054111681408"
                },
                "Count":9,
                "List":[
                    {"UserName":"@@799aece340ef70805cbce9dcf4807a9e1dbff227fcfc6c07a7d81fe4825303bf","EncryChatRoomId":""},
                    {"UserName":"@@0bc4b192a9ed7ce4811df3fcb5942c8b408e7605c833c3a10e14eb1e7955b4d5","ChatRoomId":""},
                    {"UserName":"@@b9942d72633e7d26613bde754194b974d9e03819272c432affdfaf1fe88fa8a7","ChatRoomId":""},
                    {"UserName":"@@b83fb4700758a66544ef0dbb2628e4a0d00d33f3dd76aff701ad0f3d008742d3","ChatRoomId":""},
                    {"UserName":"@@a9b92b521574acf33f1840e11f735063ce10c560d084de57b74d3cdf8906da3d","ChatRoomId":""},
                    {"UserName":"@@d06d410bc4e369d49a15b3681342ac9fd10ef3151f18886067a87385cb757f77","ChatRoomId":""},
                    {"UserName":"@@762a12795204bf7ca64248e53c037ff7364846d7fb0a865243040988aa197b76","ChatRoomId":""},
                    {"UserName":"@@8c422fb01c37d3eed443e0ef07c24b2eefc0249e3a4c04b3ad0eb109fc7357b7","ChatRoomId":""},
                    {"UserName":"@@e310c6d2dd7b9d6b365d80cb104fe170e48cc4fabbe15322954a162bdbeb87e9","ChatRoomId":""}
                ]
            }
            */

            JObject _params = createJObject(new Dictionary<string, object>() {
                {"BaseRequest", base_request },
                {"Count", this.chatroomMgr.Count },
                {"List", curArray},
            });

            JObject init_result = BaseService.fetchObject(url, _params);
            /*
            {
            "BaseResponse": 
            {
                "Ret": 0,
                "ErrMsg": ""
            }
            ,
            "Count": 1,
            "ContactList":[
                {
                    "Uin": 0,
                    "UserName": "@@1c8633ff270d898bd95a190ef3ccfbcf5ec6bd6153510d1a24c7c23cfbd34855",
                    "NickName": "",
                    "HeadImgUrl": "/cgi-bin/mmwebwx-bin/webwxgetheadimg?seq=0&username=@@1c8633ff270d898bd95a190ef3ccfbcf5ec6bd6153510d1a24c7c23cfbd34855&skey=",
                    "ContactFlag": 0,
                    "MemberCount": 3,
                    "MemberList": 
                    [
                        {
                            "Uin": 0,
                            "UserName": "@daf9b7150d8f9fd8236abf0a72ba61ace015ae0d36613d873054d415ad460ff0",
                            "NickName": "çžŽæŽ°",
                            "AttrStatus": 102437,
                            "PYInitial": "",
                            "PYQuanPin": "",
                            "RemarkPYInitial": "",
                            "RemarkPYQuanPin": "",
                            "MemberStatus": 0,
                            "DisplayName": "",
                            "KeyWord": ""
                        },
                    ],
                    "RemarkName": "",
                    "HideInputBarFlag": 0,
                    "Sex": 0,
                    "Signature": "",
                    "VerifyFlag": 0,
                    "OwnerUin": 1071396966,
                    "PYInitial": "",
                    "PYQuanPin": "",
                    "RemarkPYInitial": "",
                    "RemarkPYQuanPin": "",
                    "StarFriend": 0,
                    "AppAccountFlag": 0,
                    "Statues": 1,
                    "AttrStatus": 0,
                    "Province": "",
                    "City": "",
                    "Alias": "",
                    "SnsFlag": 0,
                    "UniFriend": 0,
                    "DisplayName": "",
                    "ChatRoomId": 0,
                    "KeyWord": "",
                    "EncryChatRoomId": "@bbe3bbc3323453f1e0a3a90dd4b764d0"
                }
            ]
            }
             */

            if (init_result == null) {
                return;
            }
            this.chatroomMgr.roomList.DictionaryForEach((room) => {
                room.ClearMemberList();
                return true;
            });
            foreach (JObject group in init_result["ContactList"])
            {
                string gid = group["UserName"].ToString();
                var room = this.chatroomMgr[gid];
                if (room != null) {
                    foreach (var item in (JArray)group["MemberList"]) {
                        room.AddContact((JObject)item);
                    }
                    room.EncryChatRoomId = group["EncryChatRoomId"].ToString();
                }
            }
        }

        /// <summary>
        /// 添加所有人，自动发送欢迎语
        /// </summary>
        public void createChatRoomForAll(string _msg = "")
        {
            List<string> list = new List<string>();
            foreach (var it in this.contactList.Where((item) => { return item.Value.type == ContactTypeEnum.Contact; }))
            {
                list.Add(it.Value.Id);
            }
            ResultOfCreateChatroom ret = this.createChatRoom(list);

            if (ret.code == 0)
            {
                //将群保存下来
                var room = new Chatroom(this, ret.ChatroomName);
                this.chatroomMgr.AddChatroom(room);
                batch_get_group_members(ret.ChatroomName);

                //致欢迎语
                send_msg_by_uid(string.Format("@all {0}", _msg == "" ? this.welcome_content : _msg), ret.ChatroomName);

                //删除所有人
                //foreach (var it in this.contactList.Where((item) => { return item.Value.type == ContactTypeEnum.Contact; }))
                //{
                //    var room = this.chatroomMgr[ret.ChatroomName];
                //    if (room != null) {
                //        room.delete_user_from_group(it.Value.Id);
                //    }
                //}
            }
        }

        /// <summary>
        /// 主动向群内人员打招呼，提交添加好友请求
        /// uid-群内人员的uid VerifyContent-好友招呼内容
        /// 慎用此接口！封号后果自负！慎用此接口！封号后果自负！慎用此接口！封号后果自负！
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="VerifyContent"></param>
        public bool addFriendByUid(string uid, string VerifyContent)
        {
            if (is_contact(uid))
            {
                return true;
            }
            string url = base_uri + "/webwxverifyuser?r=" + UserFunc.GetTimeStampWithThousand().ToString() + "&lang=zh_CN";
            JObject _params = BaseService.createJObject(new Dictionary<string, object>() {
                {"BaseRequest", base_request },
                {"Opcode", 2 },
                {"VerifyUserListSize", 1 },
                {"VerifyUserList", new JObject[] {
                    BaseService.createJObject(new Dictionary<string, object>() {
                        {"Value", uid },
                        {"VerifyUserTicket", "" },
                    }),
                } },
                {"VerifyContent", VerifyContent },
                {"SceneListCount", 1 },
                {"SceneList", new int[] { 33} },
                {"skey", base_request["Skey"] },
            });
            try
            {
                JObject init_result = BaseService.fetchObject(url, _params);
                return init_result != null && init_result["BaseResponse"]["Ret"].ToString() == "0";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建聊天室
        /// </summary>
        /// <returns></returns>
        public ResultOfCreateChatroom createChatRoom(List<string> userList)
        {
            ResultOfCreateChatroom ret = new ResultOfCreateChatroom();

            string url = base_uri + String.Format("/webwxcreatechatroom?r={0}&pass_ticket={1}", UserFunc.GetTimeStampWithThousand(), pass_ticket);

            JArray ml = new JArray();
            ml.AddItem("UserName", getMyAccount().Id);
            userList.ForEach(it => ml.AddItem("UserName", it));
            JObject _params = createJObject(new Dictionary<string, object>(){
                { "BaseRequest", base_request },
                { "Topic", ""},
                { "MemberCount", ml.Count},
                { "MemberList", ml},
            });
            try
            {
                // Content - Type:text / plain
                //{
                //    "BaseResponse": {
                //        "Ret": 0,
                //        "ErrMsg": "Everything is OK"
                //     }
                //    ,
                //    "Topic": "",
                //    "PYInitial": "",
                //    "QuanPin": "",
                //    "MemberCount": 2,
                //    "MemberList": [
                //        {
                //        "Uin": 0,
                //        "UserName": "@daf9b7150d8f9fd8236abf0a72ba61ace015ae0d36613d873054d415ad460ff0",
                //        "NickName": "çžŽæŽ°",
                //        "AttrStatus": 0,
                //        "PYInitial": "",
                //        "PYQuanPin": "",
                //        "RemarkPYInitial": "",
                //        "RemarkPYQuanPin": "",
                //        "MemberStatus": 0,
                //        "DisplayName": "",
                //        "KeyWord": ""
                //        },
                //        {
                //        "Uin": 0,
                //        "UserName": "@91a61fa98881e621dfc42ebccb2c696a",
                //        "NickName": "ç™¾æ™“ç”Ÿ",
                //        "AttrStatus": 0,
                //        "PYInitial": "",
                //        "PYQuanPin": "",
                //        "RemarkPYInitial": "",
                //        "RemarkPYQuanPin": "",
                //        "MemberStatus": 0,
                //        "DisplayName": "",
                //        "KeyWord": ""
                //        }
                //    ],
                //    "ChatRoomName": "@@1c8633ff270d898bd95a190ef3ccfbcf5ec6bd6153510d1a24c7c23cfbd34855",
                //    "BlackList": ""
                //}

                JObject init_result = BaseService.fetchObject(url, _params);
                if (init_result != null) {
                    ret.code = init_result["BaseResponse"]["Ret"].ToString().ToInt32();
                    ret.ChatroomName = init_result["ChatRoomName"].ToString();
                    ret.MemberList = (JArray)init_result["MemberList"];
                }
            }
            catch
            {
                ret.code = -1;
            }
            return ret;
        }

        #endregion

        #region 发送消息相关函数

        /// <summary>
        /// 发送消息，暂不实现文件传输
        /// </summary>
        /// <param name="name"></param>
        /// <param name="word"></param>
        /// <param name="isfile"></param>
        /// <returns></returns>
        public bool send_msg(string name, string word, bool isfile = false)
        {
            string uid = get_user_id(name);
            if (!String.IsNullOrEmpty(uid))
            {
                word = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes((word)));
                return send_msg_by_uid(word, uid);
            }
            else
            {
                Console.WriteLine("[ERROR] This user does not exist .");
                return true;
            }
        }

        public bool send_img_msg(string name, string fpath) {
            string uid = get_user_id(name);
            if (!String.IsNullOrEmpty(uid)) {
                return send_img_msg_by_uid(fpath, uid);
            }
            return false;
        }

        /// <summary>
        /// 发送图片
        /// </summary>
        public bool send_img_msg_by_uid(string fpath, string uid) {
            string mid = upload_media(fpath, uid, true);
            if (String.IsNullOrWhiteSpace(mid)) {
                return false;
            }
            string url = String.Format(base_uri + "/webwxsendmsgimg?fun=async&f=json&pass_ticket={0}", pass_ticket);
            string msg_id = (UserFunc.GetTimeStampWithThousand() + (new Random()).Next()).ToString().Substring(0, 5).Replace(".", "");
            JObject _params = createJObject(new Dictionary<string, object>(){
                    { "BaseRequest", base_request },
                    { "Msg", createJObject(new Dictionary<string, object>() {
                        { "Type", 3 },
                        { "MediaId", mid},
                        { "FromUserName", getMyAccount().Id},
                        { "ToUserName", uid },
                        { "LocalID", msg_id },
                        { "ClientMsgId", msg_id }
                        })
                    },
                    { "Scene", 0},
                });

            if (fpath.Substring(fpath.Length - 4) == ".gif") {
                url = String.Format(base_uri + "/webwxsendemoticon?fun=sys&f=json&pass_ticket={0}", pass_ticket);
                _params["Msg"]["Type"] = 47;
                _params["Msg"]["EmojiFlag"] = 2;
            }
            JObject init_result = fetchObject(url, _params);
            return init_result != null && init_result["BaseResponse"]["Ret"].ToString() == "0";
        }

        /// <summary>
        /// 本地会话中发送文件的连续编号
        /// </summary>
        private int file_index = 0;

        public bool send_file_msg_by_uid(string fpath, string uid) {
            string mid = upload_media(fpath, uid);
            if (string.IsNullOrWhiteSpace(mid)) {
                return false;
            }
            string url = base_uri + "/webwxsendappmsg?fun=async&f=json&pass_ticket=" + pass_ticket;
            string msg_id = UserFunc.GetTimeStampWithThousand() + (new Random()).Next().ToString().Substring(0, 5).Replace(".", "");

            FileInfo f = new FileInfo(fpath);
            string flen = f.Length.ToString();                                                  //文件长度
            string filename = System.IO.Path.GetFileName(fpath);                                //文件名  “Default.aspx”
            string extension = System.IO.Path.GetExtension(fpath);                              //扩展名 “.aspx”
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fpath);// 没有扩展名的文件名 “Default”
            string content_str = string.Format("<appmsg appid='wxeb7ec651dd0aefa9' sdkver=''><title>{0}</title><des></des><action></action><type>6</type><content></content><url></url><lowurl></lowurl><appattach><totallen>{1}</totallen><attachid>{2}</attachid><fileext>{3}</fileext></appattach><extinfo></extinfo></appmsg>", filename, flen, mid, extension);
            JObject _params = createJObject(new Dictionary<string, object>(){
                    { "BaseRequest", base_request },
                    { "Msg", createJObject(new Dictionary<string, object>() {
                        { "Type", 6 },
                        { "MediaId", mid},
                        { "FromUserName", getMyAccount().Id},
                        { "ToUserName", uid },
                        { "LocalID", msg_id },
                        { "ClientMsgId", msg_id },
                        { "Content", content_str },
                        })
                    },
                    { "Scene", 0},
                });

            try {
                JObject init_result = fetchObject(url, _params);
                return init_result != null && init_result["BaseResponse"]["Ret"].ToString() == "0";
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// 上传图片
        /// </summary>
        /// <param name="fpath">文件全名</param>
        /// <param name="dstUid">目标用户ID，如果是群则填写群ID</param>
        /// <param name="is_img">是否是图片</param>
        /// <returns></returns>
        public string upload_media(string fpath, string dstUid, bool is_img = false)
        {
            if (!File.Exists(fpath)) {
                Console.WriteLine("[ERROR] File not exists.");
                return "";
            }

            string url_1 = "https://file." + base_host + "/cgi-bin/mmwebwx-bin/webwxuploadmedia?f=json";
            string url_2 = "https://file2." + base_host + "/cgi-bin/mmwebwx-bin/webwxuploadmedia?f=json";

            FileInfo f = new FileInfo(fpath);
            string flen = f.Length.ToString();                                                  //文件长度
            string filename = System.IO.Path.GetFileName(fpath);                                //文件名  “Default.aspx”
            string extension = System.IO.Path.GetExtension(fpath);                              //扩展名 “.aspx”
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fpath);// 没有扩展名的文件名 “Default”

            //计算文件的MD5
            string fileMd5 = "";
            using (FileStream fs = new FileStream(fpath, FileMode.Open)) {
                using (BinaryReader r = new BinaryReader(fs)) {
                    fileMd5 = baseFunc.md5(r.ReadBytes((int)f.Length));
                }
            }

            JObject data = createJObject(new Dictionary<string, object>() {
                { "id", String.Format("WU_FILE_{0}", file_index)},
                {"name", filename},
                {"type", MimeMapping.GetMimeMapping(fpath)},
                {"lastModifiedDate", f.LastWriteTime.ToString("%m/%d/%Y, %H:%M:%S GMT+0800 (CST)") },
                {"size", flen },
                {"mediatype", is_img ? "pic" : "doc"},
                {"uploadmediarequest", createJObject(new Dictionary<string, object>() {
                    { "UploadType", 2},
                    { "BaseRequest", base_request},
                    { "ClientMediaId", UserFunc.GetTimeStampWithThousand() },
                    { "TotalLen", flen},
                    { "StartPos", 0 },
                    { "DataLen", flen },
                    { "MediaType", 4},
                    { "FromUserName", my_account.Id},
                    { "ToUserName", dstUid},
                    { "FileMd5", fileMd5},
                    }) },
                { "webwx_data_ticket", BaseService.GetCookie("webwx_data_ticket").Value},
                { "pass_ticket", pass_ticket },
                { "filename", filename },
            });
            file_index += 1;
            try {
                JObject ret = BaseService.UploadFile(url_1, data, fpath);
                if (ret == null || ret["BaseResponse"]["Ret"].ToString() != "0") {
                    //当file返回值不为0时则为上传失败，尝试第二服务器上传
                    ret = BaseService.UploadFile(url_2, data, fpath);
                }
                if (ret == null || ret["BaseResponse"]["Ret"].ToString() != "0") {
                    Console.WriteLine("[ERROR] Upload media failure.");
                    return "";
                }
                return ret["MediaId"].ToString();
            }
            catch {
                return "";
            }
        }

        /// <summary>
        /// 根据昵称返回标识
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string get_user_id(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return "";
            }
            name = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(name));
            var contact = this.FindByName(name);
            if (contact != null) {
                return contact.Id;
            }

            return "";
        }

        /// <summary>
        /// 向dst指定的用户发送消息
        /// </summary>
        /// <param name="word"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public bool send_msg_by_uid(string word, string dst = "filehelper")
        {
            string url = String.Format(base_uri + "/webwxsendmsg?sid={0}pass_ticket={1}&lang=zh_CN", base_request["Sid"], pass_ticket);
            string msg_id = (UserFunc.GetTimeStampWithThousand() + (new Random()).Next()).ToString().Substring(0, 5).Replace(".", "");
            JObject _params = createJObject(new Dictionary<string, object>(){
                { "BaseRequest", base_request },
                { "Msg", createJObject(new Dictionary<string, object>() {
                    { "Type", 1 },
                    { "Content", word },
                    { "FromUserName", getMyAccount().Id},
                    { "ToUserName", dst },
                    { "LocalID", msg_id },
                    { "ClientMsgId", msg_id }
                }) }, });

            JObject init_result = fetchObject(url, _params);
            return init_result != null && init_result["BaseResponse"]["Ret"].ToString() == "0";
        }

        #endregion

        #region 多媒体消息解析

        /// <summary>
        /// 根据msg_id返回相应的图片下载地址
        /// </summary>
        /// <param name="msgid"></param>
        /// <returns></returns>
        public string get_msg_img_url(string msgid)
        {
            return base_uri + String.Format("/webwxgetmsgimg?MsgID={0}&skey={1}", msgid, base_request["Skey"]);
        }

        /// <summary>
        /// 获取图片消息，下载图片到本地
        /// </summary>
        /// <param name="msgid">消息id</param>
        /// <returns>保存的本地图片文件路径</returns>
        public string get_msg_img(string msgid)
        {
            return BaseService.fetchFile(
                String.Format(base_uri + "/webwxgetmsgimg?MsgID={0}&skey={1}", msgid, base_request["Skey"]),
                temp_pwd + "\\img_" + msgid + ".jpg");
        }

        /// <summary>
        /// 根据msg_id返回相应的语音下载地址
        /// </summary>
        /// <param name="msgid"></param>
        /// <returns></returns>
        public string get_voice_url(string msgid)
        {
            return String.Format(base_uri + "/webwxgetvoice?msgid={0}&skey={1}", msgid, base_request["Skey"]);
        }

        /// <summary>
        /// 获取语音消息，下载语音到本地
        /// </summary>
        /// <param name="msgid">语音消息id</param>
        /// <returns>保存的本地语音文件路径</returns>
        public string get_voice(string msgid) {
            return BaseService.fetchFile(
                string.Format(base_uri + "/webwxgetvoice?msgid={0}&skey={1}", msgid, base_request["Skey"]), 
                temp_pwd + "\\img_voice_" + msgid + ".mp3");
        }

        #endregion

        #region 暂时没有启用的功能

        /// <summary>
        /// 测试多个push服务器的可用性
        /// </summary>
        /// <returns></returns>
        public bool test_sync_check()
        {
            string[] list = { "webpush.", "webpush1." };

            foreach (string host1 in list)
            {
                string retcode = "";
                sync_host = host1;
                try
                {
                    retcode = sync_check()[0].ToString();
                }
                catch
                {
                    retcode = "-1";
                }
                if (retcode == "0")
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    } 

    /// <summary>
    /// 登录操作结果集
    /// </summary>
    public class loginResult
    {
        public string code = SyncStatus.UNKONWN;
        public Image image = null;
        public string url = "";
        public void Init()
        {
            code = SyncStatus.UNKONWN;
            image = null;
            url = "";
        }
    }

    /// <summary>
    /// 创建聊天室返回结果
    /// </summary>
    public class ResultOfCreateChatroom
    {
        public int code = 0;
        public string ChatroomName = "";
        public JArray MemberList = new JArray();
    }

    /// <summary>
    /// 同步状态枚举
    /// </summary>
    public class SyncStatus
    {
        public static string UNKONWN = "unkonwn";
        public static string SUCCESS = "200";
        public static string SCANED = "201";
        public static string TIMEOUT = "408";
    }

    /// <summary>
    /// 消息体类型枚举
    /// </summary>
    public enum ContentTypeEnum
    {
        Text = 0,
        Location = 1,
        Image = 3,
        Voice = 4,
        Recommend = 5,
        RecommendInfo = 37, //?什么鬼
        Animation = 6,
        Share = 7,
        Video = 8,
        VideoCall = 9,
        Redraw = 10,
        Empty = 11,
        Unknown = 99,
    }

    /// <summary>
    /// 消息类型
    /// </summary>
    public enum MsgSrcTypeEnum
    {
        /// <summary>
        /// 初始化消息
        /// </summary>
        Init = 0,
        /// <summary>
        ///  加好友请求
        /// </summary>
        FriendReq = 37,
        /// <summary>
        /// 自己的消息
        /// </summary>
        Self = 1,
        /// <summary>
        /// 文件助手发送的消息
        /// </summary>
        FileHelper = 2,
        /// <summary>
        /// 群消息
        /// </summary>
        Group = 3,
        /// <summary>
        /// 联系人消息
        /// </summary>
        Contact = 4,
        /// <summary>
        /// 公众号消息
        /// </summary>
        Public = 5,
        /// <summary>
        /// 特殊号
        /// </summary>
        Special = 6,
        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 99,
    }
}
