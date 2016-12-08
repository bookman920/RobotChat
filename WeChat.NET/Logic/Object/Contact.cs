using GameSystem.BaseFunc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WeChat.NET.Logic.Robot;
using WeChat.NET.Objects;
using WeChat.NET.Protocol;

namespace WeChat.NET.Logic.Object
{
    /// <summary>
    /// 联系人类型枚举
    /// </summary>
    public enum ContactTypeEnum
    {
        /// <summary>
        /// 自己
        /// </summary>
        Self = 1,
        /// <summary>
        /// 群
        /// </summary>
        Group = 3,
        /// <summary>
        /// 联系人
        /// </summary>
        Contact = 4,
        /// <summary>
        /// 公众号
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

    /// <summary>
    /// 群组（聊天室）
    /// </summary>
    public class Chatroom : BaseContact
    {
        /// <summary>
        /// 存储群聊的EncryChatRoomId，获取群内成员头像时需要用到
        /// </summary>
        public string EncryChatRoomId = "";

        /// <summary>
        /// 入群欢迎语
        /// </summary>
        public string welcome_content = "您好:)";

        /// <summary>
        /// 索引器 - 根据用户标识返回用户对象
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public BaseContact this[string uid]
        {
            get
            {
                if (this.memberList.ContainsKey(uid))
                {
                    return this.memberList[uid];
                }
                return null;
            }
        }

        public Chatroom(RobotOfBase _parent, JObject obj) : base(_parent, obj)
        {
            this.type = ContactTypeEnum.Group;
        }

        public Chatroom(RobotOfBase _parent, string name) : base(_parent, name)
        {
            this.type = ContactTypeEnum.Group;
        }

        /// <summary>
        /// 设置群聊名称
        /// </summary>
        /// <param name="gname"></param>
        public bool setName(string gname)
        {
            string url = String.Format(parent.base_uri + "/webwxupdatechatroom?fun=modtopic&pass_ticket={0}", parent.pass_ticket);
            JObject _params = BaseService.createJObject(new Dictionary<string, object>(){
                { "NewTopic", gname },
                { "ChatRoomName", this.Id },
                { "BaseRequest", parent.base_request },
            });

            try
            {
                JObject init_result = BaseService.fetchObject(url, _params);
                if (init_result != null && init_result["BaseResponse"]["Ret"].ToString() == "0") {
                    this.NickName = gname;
                    this.DisplayName = gname;
                    this.RemarkName = gname;
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// 获取成员的名称信息
        /// return: 名称信息，类似 {"display_name": "test_user", "nickname": "test", "remark_name": "for_test" }
        /// </summary>
        /// <param name="gid"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public JObject get_group_member_name(string uid)
        {
            if (this.memberList.ContainsKey(uid))
            {
                return this.memberList[uid].get_name();
            }
            return null;
        }

        public string get_group_member_prefer_name(JObject name)
        {
            string ret = "";
            if (name != null)
            {
                if (name.hasKey("remark_name"))
                {
                    ret = name.Property("remark_name").Value.ToString();
                }
                else if (name.hasKey("display_name"))
                {
                    ret = name.Property("display_name").Value.ToString();
                }
                else if (name.hasKey("nickname"))
                {
                    ret = name.Property("nickname").Value.ToString();
                }
            }
            return ret;
        }

        /// <summary>
        /// 群组联系人列表
        /// </summary>
        public Dictionary<string, Contact> memberList = new Dictionary<string, Contact>();
        /// <summary>
        /// 添加聊天室成员
        /// </summary>
        /// <param name="obj"></param>
        public void AddContact(JObject obj)
        {
            var con = new Contact(this.parent, obj);
            this.memberList[con.Id] = con;
        }
        /// <summary>
        /// 清空联系人列表
        /// </summary>
        public void ClearMemberList()
        {
            this.memberList.Clear();
        }
        public BaseContact Find(string nickName)
        {
            foreach (var user in this.memberList.Values)
            {
                if (user.NickName == nickName)
                {
                    return user;
                }
            }
            return null;
        }
        /// <summary>
        /// 将群用户从群中剔除，只有群管理员有权限
        /// </summary>
        /// <param name="uname"></param>
        /// <param name="gid"></param>
        /// <returns></returns>
        public bool delete_user_from_group(string uid)
        {
            var member = this[uid];
            if (member == null)
            {
                return false;
            }

            string url = String.Format(parent.base_uri + "/webwxupdatechatroom?fun={0}&pass_ticket={1}", "delmember", parent.pass_ticket);
            JObject _params = BaseService.createJObject(new Dictionary<string, object>(){
                { "DelMemberList", member.Id },
                { "ChatRoomName", this.Id },
                { "BaseRequest", parent.base_request },
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
    }

    /// <summary>
    /// 联系人基类
    /// </summary>
    public class BaseContact
    {
        /// <summary>
        /// 联系人类型
        /// </summary>
        public ContactTypeEnum type = ContactTypeEnum.Contact;
        /// <summary>
        /// 显示名？
        /// </summary>
        public string DisplayName = "";
        /// <summary>
        /// 标识
        /// </summary>
        public string Id = "";
        /// <summary>
        /// 昵称
        /// </summary>
        public string NickName { get; set; }
        /// <summary>
        /// 头像url
        /// </summary>
        public string HeadImgUrl { get; set; }
        /// <summary>
        /// 备注名
        /// </summary>
        public string RemarkName { get; set; }
        /// <summary>
        /// 性别 男1 女2 其他0
        /// </summary>
        public string Sex { get; set; }
        /// <summary>
        /// 签名
        /// </summary>
        public string Signature { get; set; }
        /// <summary>
        /// 城市
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// 省份
        /// </summary>
        public string Province { get; set; }
        /// <summary>
        /// 昵称全拼
        /// </summary>
        public string PYQuanPin { get; set; }
        /// <summary>
        /// 备注名全拼
        /// </summary>
        public string RemarkPYQuanPin { get; set; }

        #region 获取用户头像，服务端不建议调用
        public Image Icon
        {
            get
            {
                if (_icon == null && !_loading_icon)
                {
                    _loading_icon = true;
                    ((Action)(delegate ()
                    {
                        if (Id.Contains("@@"))  //讨论组
                        {
                            _icon = BaseService.fetchImage(WXConfig._getheadimg_url + Id);
                        }
                        else 
                        {
                            _icon = BaseService.fetchImage(WXConfig._geticon_url + Id);
                        }
                        _loading_icon = false;
                    })).BeginInvoke(null, null);
                }
                return _icon;
            }
        }
        private Image _icon = null;
        private bool _loading_icon = false;
        #endregion

        /// <summary>
        /// 显示名称
        /// </summary>
        public string ShowName
        {
            get
            {
                return String.IsNullOrEmpty(RemarkName) ? NickName : RemarkName;
            }
        }
        /// <summary>
        /// 显示的拼音全拼
        /// </summary>
        public string ShowPinYin
        {
            get
            {
                return String.IsNullOrEmpty(RemarkPYQuanPin) ? PYQuanPin : RemarkPYQuanPin;
            }
        }

        //发送给对方的消息  
        private List<WXMsg> _sentMsg = new List<WXMsg>();
        public List<WXMsg> SentMsg
        {
            get
            {
                return _sentMsg;
            }
        }
        //收到对方的消息
        private List<WXMsg> _recvedMsg = new List<WXMsg>();
        public List<WXMsg> RecvedMsg
        {
            get
            {
                return _recvedMsg;
            }
        }

        public event MsgSentEventHandler MsgSent;
        public event MsgRecvedEventHandler MsgRecved;

        /// <summary>
        /// 接收来自该用户的消息
        /// </summary>
        /// <param name="msg"></param>
        public void ReceiveMsg(WXMsg msg)
        {
            _recvedMsg.Add(msg);

            if (MsgRecved != null)
            {
                MsgRecved(msg);
            }
        }
        /// <summary>
        /// 向该用户发送消息
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(WXMsg msg)
        {
            _sentMsg.Add(msg);
            if (MsgSent != null)
            {
                MsgSent(msg);
            }
        }
        /// <summary>
        /// 获取该用户发送的未读消息
        /// </summary>
        /// <returns></returns>
        public List<WXMsg> GetUnReadMsg()
        {
            List<WXMsg> list = null;
            foreach (var p in _recvedMsg)
            {
                if (!p.Readed)
                {
                    if (list == null)
                    {
                        list = new List<WXMsg>();
                    }
                    list.Add(p);
                }
            }

            return list;
        }
        /// <summary>
        /// 获取最近的一条消息
        /// </summary>
        /// <returns></returns>
        public WXMsg GetLatestMsg()
        {
            WXMsg msg = null;
            if (_sentMsg.Count > 0 && _recvedMsg.Count > 0)
            {
                msg = _sentMsg.Last().Time > _recvedMsg.Last().Time ? _sentMsg.Last() : _recvedMsg.Last();
            }
            else if (_sentMsg.Count > 0)
            {
                msg = _sentMsg.Last();
            }
            else if (_recvedMsg.Count > 0)
            {
                msg = _recvedMsg.Last();
            }
            else
            {
                msg = null;
            }
            return msg;
        }

        /// <summary>
        /// 根据网络数据，构造聊天室对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public BaseContact(RobotOfBase _parent, JObject obj)
        {
            this.parent = _parent;
            this.info = obj;

            this.Id = obj["UserName"].ToString(); ;
            this.RemarkName = obj.GetValue("RemarkName") != null ? obj["RemarkName"].ToString() : ""; 
            this.NickName = obj.GetValue("NickName") != null ? obj["NickName"].ToString() : ""; 
            this.DisplayName = obj.GetValue("DisplayName") != null ? obj["DisplayName"].ToString() : ""; 
            this.City = obj.GetValue("City") != null ? obj["City"].ToString() : "";
            this.HeadImgUrl = obj.GetValue("HeadImgUrl") != null ? obj["HeadImgUrl"].ToString() : "";
            this.Province = obj.GetValue("Province") != null ? obj["Province"].ToString() : "";
            this.PYQuanPin = obj.GetValue("PYQuanPin") != null ? obj["PYQuanPin"].ToString() : "";
            this.RemarkPYQuanPin = obj.GetValue("RemarkPYQuanPin") != null ? obj["RemarkPYQuanPin"].ToString() : "";
            this.Sex = obj.GetValue("Sex") != null ? obj["Sex"].ToString() : "";
            this.Signature = obj.GetValue("Signature") != null ? obj["Signature"].ToString() : "";
        }

        public BaseContact(RobotOfBase _parent, string name) {
            JObject obj = new JObject();
            obj["UserName"] = name;

            this.parent = _parent;
            this.info = obj;
            this.Id = obj["UserName"].ToString(); ;
        }
        /// <summary>
        /// 获取名称信息
        /// return: 名称信息，类似 {"display_name": "test_user", "nickname": "test", "remark_name": "for_test" }
        /// </summary>
        /// <returns></returns>
        public JObject get_name()
        {
            JObject names = new JObject();
            if (this.RemarkName != "")
            {
                names["remark_name"] = this.RemarkName;
            }
            if (this.NickName != "")
            {
                names["nickname"] = this.NickName;
            }
            if (this.DisplayName != null)
            {
                names["display_name"] = this.DisplayName;
            }
            return names;
        }
        protected RobotOfBase parent = null;
        /// <summary>
        /// 原始数据
        /// </summary>
        protected JObject info = null;
    }

    /// <summary>
    /// 联系人对象
    /// </summary>
    public class Contact : BaseContact
    {
        public Contact(RobotOfBase _parent, JObject obj) : base(_parent, obj)
        {
            this.type = ContactTypeEnum.Contact;
        }
    }

    public class SpecialContact : BaseContact
    {
        public SpecialContact(RobotOfBase _parent, JObject obj) : base(_parent, obj)
        {
            this.type = ContactTypeEnum.Special;
        }
    }

    public class SelfContact : BaseContact
    {
        public SelfContact(RobotOfBase _parent, JObject obj) : base(_parent, obj)
        {
            this.type = ContactTypeEnum.Self;
        }
    }

    public class PublicContact : BaseContact
    {
        public PublicContact(RobotOfBase _parent, JObject obj) : base(_parent, obj)
        {
            this.type = ContactTypeEnum.Public;
        }
    }

    /// <summary>
    /// 表示处理消息发送完成事件的方法
    /// </summary>
    /// <param name="msg"></param>
    public delegate void MsgSentEventHandler(WXMsg msg);
    /// <summary>
    /// 表示处理接收到新消息事件的方法
    /// </summary>
    /// <param name="msg"></param>
    public delegate void MsgRecvedEventHandler(WXMsg msg);
}
