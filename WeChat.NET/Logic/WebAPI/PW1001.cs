using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using GameSystem.BaseFunc;

namespace WeChat.NET.Logic.Packet
{
    /// <summary>
    /// 1001报文
    /// 访问用例：http://127.0.0.1:9988/?act=1001&content=&room=
    /// </summary>
    class PW1001 : PacketOfWeb
    {
        /// <summary>
        /// 异步执行的方法
        /// </summary>
        /// <returns></returns>
        public override string Execute()
        {
            JObject ret = createJObject(new Dictionary<string, object>() {
                {"code", 0 },
                {"items", RobotManager.Instance.Default.chatroomMgr.roomList.ToJArray((item)=> {
                    return createJObject(new Dictionary<string, object>() {
                        { "编号", item.Id },
                        { "昵称", item.NickName },
                    });
                })},
            });
            return ret.ToUTF8String();
        }

        /// <summary>
        /// 同步执行的方法
        /// </summary>
        public override void SyncOper()
        {
            Console.WriteLine("设置群名称：" + this["room"] + "," + this["content"]);
            RobotManager.Instance.Default.set_group_name(this["room"].ToString(), this["content"].ToString());
        }
    }
}
