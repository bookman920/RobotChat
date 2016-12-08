﻿using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using GameSystem.BaseFunc;

namespace WeChat.NET.Logic.Packet
{
    /// <summary>
    /// 1004报文
    /// 访问用例：http://127.0.0.1:9988/?act=1001&content=%E6%9D%8E%E5%9B%9B
    /// </summary>
    class PW1004 : PacketOfWeb
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
            string cont = this["content"].ToString().ReplaceHtml();
            Console.WriteLine("添加违禁词汇：" + cont);
            if (!String.IsNullOrWhiteSpace(cont)) {
                RobotManager.Instance.Default.blackWords.ListAdd(cont);
            }
        }
    }
}
