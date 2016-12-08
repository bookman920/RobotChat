using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using GameSystem.BaseFunc;

namespace WeChat.NET.Logic.Packet
{
    /// <summary>
    /// 1008报文
    /// </summary>
    class PW1008 : PacketOfWeb
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
            RobotManager.Instance.Default.Flow_Logout();
        }
    }
}
