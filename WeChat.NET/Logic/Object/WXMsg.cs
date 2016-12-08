using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace WeChat.NET.Objects
{
    /// <summary>
    /// 微信消息
    /// </summary>
    public class WXMsg
    {
        /// <summary>
        /// 消息发送方
        /// </summary>
        public string From{get;set;}
        /// <summary>
        /// 消息接收方
        /// </summary>
        public string To{get; set; }
        /// <summary>
        /// 消息发送时间
        /// </summary>
        public DateTime Time{get;set;}
        /// <summary>
        /// 是否已读
        /// </summary>
        public bool Readed{get;set;}
        /// <summary>
        /// 消息内容
        /// </summary>
        public string Msg{get;set;}
        /// <summary>
        /// 消息类型
        /// </summary>
        public int Type{get;set;}
        /// <summary>
        /// 状态
        /// </summary>
        public int Status { get; set; }

        public WXMsg()
        { }

        public WXMsg(JObject m) {
            string from = m["FromUserName"].ToString();
            string to = m["ToUserName"].ToString();
            string content = m["Content"].ToString();
            string type = m["MsgType"].ToString();

            this.From = from;
            //this.Msg = type == "1" ? content : "请在其他设备上查看消息";  //只接受文本消息
            this.Msg = content;
            this.Readed = false;
            this.Time = DateTime.Now;
            this.To = to;
            this.Type = int.Parse(type);
            this.Status = int.Parse(m["Status"].ToString());
        }
    }
}
