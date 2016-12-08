using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using GameSystem.BaseFunc;

namespace WeChat.NET.Logic.Packet
{
    /// <summary>
    /// 1009报文, 获取所有聊天室列表
    /// 访问用例：http://127.0.0.1:9988/?act=1009
    /// </summary>
    class PW1009 : PacketOfWeb
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
        }
    }
}
