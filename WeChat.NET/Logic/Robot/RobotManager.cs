using GameSystem.BaseFunc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeChat.NET.Objects;
using WeChat.NET.Protocol;

namespace WeChat.NET.Logic
{
    class RobotManager
    {
        #region 单态实现
        private RobotManager(){}
        private static RobotManager _instance = null;
        /// <summary>
        /// 获取单态对象
        /// </summary>
        /// <returns></returns>
        public static RobotManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RobotManager();
                }
                return _instance;
            }
        }
        #endregion

        /// <summary>
        /// 机器人编码管理
        /// </summary>
        private IdentityManager idMgr = new IdentityManager(1);
        /// <summary>
        /// 默认的机器人
        /// </summary>
        public RobotOfBase Default {
            get {
                return DefaultRobot;
            }
        }
        private RobotOfBase DefaultRobot = null;
        /// <summary>
        /// 机器人列表
        /// </summary>
        private Dictionary<int, RobotOfBase> robotList = new Dictionary<int, RobotOfBase>();
        /// <summary>
        /// 指定机器人类型，创建、注册、返回新的机器人
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isDefault"></param>
        /// <returns></returns>
        public RobotOfBase Register(Action<RobotOfBase> onRefreshContact, Action<RobotOfBase, WXMsg> onMsg, RobotEnum type, bool isDefault = false)
        {
            RobotOfBase rt = null;
            switch (type) {
                case RobotEnum.Tuling:
                    {
                        rt = new RobotOfTuling(idMgr.CurrentIdentity);
                        break;
                    }

                default:
                    {
                        rt = new RobotOfBase(idMgr.CurrentIdentity);
                        break;
                    }
            }
            if (rt != null) {
                robotList[rt.robotId] = rt;
                if (isDefault || this.DefaultRobot == null) {
                    DefaultRobot = rt;
                }
                //执行登录流程，等待用户扫描二维码登录
                rt.LoginCheck(onRefreshContact, onMsg);
            }
            return rt;
        }
    }
}
