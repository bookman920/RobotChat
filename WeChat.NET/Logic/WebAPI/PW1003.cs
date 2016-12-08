using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using GameSystem.BaseFunc;

namespace WeChat.NET.Logic.Packet
{
    /// <summary>
    /// 1003报文
    /// 访问用例：http://127.0.0.1:9988/?act=1001&content=%E6%9D%8E%E5%9B%9B
    /// </summary>
    class PW1003 : PacketOfWeb
    {
        public override string Execute()
        {
            JObject ret = createJObject(new Dictionary<string, object>() {
                {"code", 0 },
            });
            return ret.ToUTF8String();
        }

        public override void SyncOper()
        {
            Console.WriteLine("自动建群：" + this["content"]);
            RobotManager.Instance.Default.createChatRoomForAll();
        }
    }
}
