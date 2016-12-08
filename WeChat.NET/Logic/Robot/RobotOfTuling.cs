using GameSystem.BaseFunc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeChat.NET.Logic.Robot;
using WeChat.NET.Protocol;

namespace WeChat.NET.Logic
{
    /// <summary>
    /// 带图灵回复功能的机器人
    /// </summary>
    class RobotOfTuling : RobotOfBase
    {
        public RobotOfTuling(int _id) : base(_id)
        {
        }

        private string tuling_auto_reply(string uid, string msg)
        {
            if (!String.IsNullOrEmpty(WXConfig.tuling_key))
            {
                string url = "http://www.tuling123.com/openapi/api";
                string user_id = uid.Replace("@", "").Substring(0, 32);
                JObject body = RobotOfBase.createJObject(new Dictionary<string, object>(){
                    { "key", WXConfig.tuling_key },
                    { "info", msg },
                    { "userid", user_id },
                });
                string result = "知道啦";
                string init_str = BaseService.fetchString(url, body);
                if (!String.IsNullOrEmpty(init_str))
                {
                    JObject respond = JsonConvert.DeserializeObject(init_str) as JObject;
                    if (respond["code"].ToString() == "100000")
                    {
                        result = respond["text"].ToString().Replace("<br>", "  ");
                    }
                    else if (respond["code"].ToString() == "200000")
                    {
                        result = respond["url"].ToString();
                    }
                    else if (respond["code"].ToString() == "302000")
                    {
                        foreach (JObject k in respond["list"])
                        {
                            result = result + "【" + k["source"] + "】 " + k["article"] + "\t" + k["detailurl"] + "\n";
                        }
                    }
                    else
                    {
                        result = respond["text"].ToString().Replace("<br>", "  ");
                    }
                }
                return result;
            }
            else
            {
                return "知道啦";
            }
        }

        /// <summary>
        /// 重写基类方法
        /// </summary>
        public override void schedule()
        {
            base.schedule();
        }

        /// <summary>
        /// 重写基类方法
        /// </summary>
        /// <param name="msg"></param>
        public override void handle_msg_all(JObject msg)
        {
            if (msg["msg_type_id"].ToString() == "1" && msg["content"]["type"].ToString() == "0")
            {//# reply to self
            }
            else if (msg["msg_type_id"].ToString() == "4" && msg["content"]["type"].ToString() == "0")
            {//# text message from contact
                //自动回复
                send_msg_by_uid(tuling_auto_reply(msg["user"]["id"].ToString(), msg["content"]["data"].ToString()), msg["user"]["id"].ToString());
            }
            else if (msg["msg_type_id"].ToString() == "3" && msg["content"]["type"].ToString() == "0")
            {//# group text message
                JToken ttt = msg["content"];
                if (null != msg["content"]["detail"])
                {
                    var room = this.chatroomMgr[msg["user"]["id"].ToString()];
                    if (room == null) {
                        return;
                    }
                    JObject my_names = room.get_group_member_name(getMyAccount().Id);
                    if (my_names == null)
                    {
                        my_names = new JObject();
                    }
                    if (!String.IsNullOrEmpty(getMyAccount().NickName))
                    {
                        my_names["nickname2"] = getMyAccount().NickName;
                    }
                    if (!String.IsNullOrEmpty(getMyAccount().RemarkName))
                    {
                        my_names["remark_name2"] = getMyAccount().RemarkName;
                    }

                    bool is_at_me = false;
                    JArray details = (JArray)msg["content"]["detail"];
                    foreach (JObject detail in details)
                    {
                        if (detail["type"].ToString() == "at")
                        {
                            foreach (var k in my_names)
                            {
                                if (k.Value.ToUTF8String() == detail["value"].ToUTF8String())
                                {
                                    is_at_me = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (is_at_me || true)
                    {
                        string src_name = msg["content"]["user"]["name"].ToString();
                        string reply = "@" + src_name + " ";
                        if (msg["content"]["type"].ToString() == "0")
                        {//  # text message
                            string content = msg["content"]["desc"].ToString();
                            this.blackWords.ListSafeForEach((word) =>
                            {
                                if (content.IndexOf(word) != -1)
                                {
                                    //违禁、踢人
                                    room.delete_user_from_group(msg["content"]["user"]["id"].ToString());
                                    content = "";
                                    return false;
                                }
                                return true;
                            });
                            if (content != "") {
                                send_msg_by_uid(reply + tuling_auto_reply(msg["content"]["user"]["id"].ToString(), content), msg["user"]["id"].ToString());
                            }
                        }
                        else
                        {
                            send_msg_by_uid(reply + "对不起，只认字，其他杂七杂八的我都不认识，,,Ծ‸Ծ,,", msg["user"]["id"].ToString());
                        }
                    }
                }
            }
        }
    }
}
