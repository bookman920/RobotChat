using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using GameSystem.BaseFunc;

namespace WeChat.NET.Logic.Packet
{
    /// <summary>
    /// 1002报文
    /// 访问用例：http://127.0.0.1:9988/?act=1002&content=%E6%9D%8E%E5%9B%9B
    /// </summary>
    class PW1002 : PacketOfWeb
    {
        public override string Execute()
        {
            JObject ret = createJObject(new Dictionary<string, object>() {
                {"code", 0 },
                {"items", RobotManager.Instance.Default.chatroomMgr.roomList.ToJArray((item)=> {
                    return createJObject(new Dictionary<string, object>() {
                        { "编号", item.Id },
                        { "欢迎语", item.welcome_content },
                    });
                })},
            });
            return ret.ToUTF8String();
        }

        public override void SyncOper()
        {
            Console.WriteLine("设置群欢迎语：" + this["content"]);
            RobotManager.Instance.Default.set_welcome_content(this["room"].ToString(), this["content"].ToString());
        }
    }
}
