using Griffin.WebServer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Web;

namespace WeChat.NET.Logic.Packet
{
    /// <summary>
    /// Web报文基类
    /// </summary>
    class PacketOfWeb : JObject
    {
        /// <summary>
        /// 报文字典 - ！！！所有报文必须在此注册
        /// </summary>
        private static Dictionary<ushort, Type> PacketTypeList = new Dictionary<ushort, Type>() {
            { 1001, typeof(PW1001)},
            { 1002, typeof(PW1002)},
            { 1003, typeof(PW1003)},
            { 1004, typeof(PW1004)},
            { 1005, typeof(PW1005)},
            { 1006, typeof(PW1006)},
            { 1007, typeof(PW1007)},
            { 1008, typeof(PW1008)},
            { 1009, typeof(PW1009)},
        };

        public static PacketOfWeb FactoryOfPacket(int cmdId)
        {
            PacketOfWeb ret = null;

            Type tp = null;
            PacketTypeList.TryGetValue((ushort)cmdId, out tp);
            if (tp != null)
            {
                ret = (PacketOfWeb)Activator.CreateInstance(tp);
            }
            else {
                ret = new PacketOfWeb();
            }
            ret.Add("act", cmdId.ToString());
            return ret;
        }

        public static JObject createJObject(Dictionary<string, object> list)
        {
            JObject ret = new JObject();
            foreach (KeyValuePair<string, object> item in list)
            {
                ret[item.Key] = JToken.FromObject(item.Value);
            }
            return ret;
        }

        /// <summary>
        /// 分析上行参数并自动赋值
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public PacketOfWeb Analyze(IHttpContext context) {
            NameValueCollection list = HttpUtility.ParseQueryString(context.Request.Uri.Query);
            foreach (string pm in list.AllKeys) {
                this[pm] = list.Get(pm);
            }
            return this;
        }

        /// <summary>
        /// 执行上行报文对应的异步任务
        /// </summary>
        /// <returns></returns>
        public virtual string Execute()
        {
            return "";
        }

        /// <summary>
        /// 执行上行报文对应的同步任务
        /// </summary>
        public virtual void SyncOper() {
        }
    }
}
