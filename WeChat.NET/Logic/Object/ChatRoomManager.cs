using GameSystem.BaseFunc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeChat.NET.Logic.Common;
using WeChat.NET.Protocol;

namespace WeChat.NET.Logic.Object
{
    /// <summary>
    /// 群组（聊天室）管理 
    /// </summary>
    public class ChatroomManager
    {
        private RobotOfBase parent = null;
        public ChatroomManager(RobotOfBase _parent)
        {
            this.parent = _parent;
        }
        private Dictionary<string, Chatroom> List = new Dictionary<string, Chatroom>();
        /// <summary>
        /// 获取所有聊天室的键值对列表
        /// </summary>
        public Dictionary<string, Chatroom> roomList
        {
            get
            {
                return this.List;
            }
        }
        /// <summary>
        /// 索引器 - 根据群组标识，返回群组对象
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Chatroom this[string id]
        {
            get
            {
                if (List.ContainsKey(id))
                {
                    return List[id];
                }
                return null;
            }
        }
        public int Count
        {
            get
            {
                return this.List.Count;
            }
        }
        /// <summary>
        /// 根据网络对象obj 添加一个聊天室对象 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        public void AddChatroom(Chatroom room)
        {
            this.List[room.Id] = room;
        }

        /// <summary>
        /// 将好友加入到群聊中
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="group_name"></param>
        /// <returns></returns>
        public bool add_friend_to_group(string gid, string uid)
        {
            Chatroom room = this[gid];
            if (room == null)
            {
                return false;
            }
            if (room[uid] != null)
            {
                //# 已经在群里面了,不用加了
                return true;
            }
            string url = String.Format(parent.base_uri + "/webwxupdatechatroom?fun=addmember&pass_ticket={0}", parent.pass_ticket);
            JObject _params = BaseService.createJObject(new Dictionary<string, object>(){
                { "AddMemberList", uid },
                { "ChatRoomName", room.Id },
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

        /// <summary>
        /// 根据聊天室昵称，获取聊天室对象
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Chatroom FindByName(string name)
        {
            foreach (var group in this.List.Values)
            {
                if (group.RemarkName == name)
                {
                    return group;
                }
                if (group.NickName == name)
                {
                    return group;
                }
                if (group.DisplayName == name)
                {
                    return group;
                }
            }
            return null;
        }
    }
}
