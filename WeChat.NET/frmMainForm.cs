using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using WeChat.NET.Controls;
using WeChat.NET.Logic;
using WeChat.NET.Logic.Object;

using GameSystem.BaseFunc;

namespace WeChat.NET
{
    /// <summary>
    /// 主界面
    /// </summary>
    public partial class frmMainForm : Form
    {
        /// <summary>
        /// 主界面等待提示
        /// </summary>
        private Label _lblWait;
        /// <summary>
        /// 聊天对话框
        /// </summary>
        private WChatBox _chat2friend;
        /// <summary>
        /// 好友信息框
        /// </summary>
        private WPersonalInfo _friendInfo;
        /// <summary>
        /// 构造方法
        /// </summary>
        public frmMainForm()
        {
            InitializeComponent();

            _chat2friend = new WChatBox();
            _chat2friend.Dock = DockStyle.Fill;
            _chat2friend.Visible = false;
            _chat2friend.FriendInfoView += new FriendInfoViewEventHandler(_chat2friend_FriendInfoView);
            Controls.Add(_chat2friend);

            _friendInfo = new WPersonalInfo();
            _friendInfo.Dock = DockStyle.Fill;
            _friendInfo.Visible = false;
            _friendInfo.StartChat += new StartChatEventHandler(_friendInfo_StartChat);
            Controls.Add(_friendInfo);

            _lblWait = new Label();
            _lblWait.Text = "数据加载...";
            _lblWait.AutoSize = false;
            _lblWait.Size = this.ClientSize;
            _lblWait.TextAlign = ContentAlignment.MiddleCenter;
            _lblWait.Location = new Point(0, 0);
            Controls.Add(_lblWait);
        }

        #region  事件处理程序
        /// <summary>
        /// 窗体加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void frmMainForm_Load(object sender, EventArgs e)
        {
            _lblWait.BringToFront();
            
            //每个机器人使用单独的线程来运行
            ((Action)(delegate (){
                //注册默认机器人
                RobotManager.Instance.Register(
                    (service) => { //初始化完成后的回调，设置可视化联系人列表
                        this.BeginInvoke((Action)(delegate () {//等待结束
                            _lblWait.Visible = false;

                            //刷新近期联系人
                            wChatList1.Items.Clear();
                            wChatList1.Items.AddRange(service.contact_latest.ToArray());
                            //刷新通讯录
                            wFriendsList1.Items.Clear();
                            wFriendsList1.Items.AddRange(service.contactList.ToValueList().OrderBy(it => it.ShowPinYin).ToArray());  
                            //设置当前登录账号
                            wpersonalinfo.FriendUser = service.getMyAccount();
                        }));
                    },
                    (service, msg) => { //消息处理回调，设置可视化消息内容
                        this.BeginInvoke((Action)delegate () {
                            bool exist_latest_contact = false;
                            foreach (BaseContact user in wChatList1.Items)
                            {
                                if (user != null)
                                {
                                    if (msg.From == user.Id && msg.To == service.getMyAccount().Id)
                                    {//接收别人消息
                                        wChatList1.Items.Remove(user);
                                        wChatList1.Items.Insert(0, user);
                                        exist_latest_contact = true;
                                        user.ReceiveMsg(msg);
                                        break;
                                    }
                                    else if (msg.From == service.getMyAccount().Id && msg.To == user.Id)
                                    {//同步自己在其他设备上发送的消息
                                        wChatList1.Items.Remove(user);
                                        wChatList1.Items.Insert(0, user);
                                        exist_latest_contact = true;
                                        user.SendMsg(msg);
                                        break;
                                    }
                                }
                            }

                            if (!exist_latest_contact)
                            {
                                foreach (BaseContact friend in wFriendsList1.Items)
                                {
                                    if (friend != null)
                                    {
                                        if (msg.From == friend.Id && msg.To == service.getMyAccount().Id)
                                        {
                                            wChatList1.Items.Insert(0, friend);
                                            friend.ReceiveMsg(msg);
                                            break;
                                        }
                                        if (msg.From == service.getMyAccount().Id && msg.To == friend.Id)
                                        {
                                            wChatList1.Items.Insert(0, friend);
                                            friend.SendMsg(msg);
                                            break;
                                        }
                                    }
                                }
                            }
                            wChatList1.Invalidate();
                        });
                    },
                    RobotEnum.Tuling, true);
            })).BeginInvoke(null, null);
        }

        /// <summary>
        /// 好友信息框中点击 聊天
        /// </summary>
        /// <param name="user"></param>
        void _friendInfo_StartChat(BaseContact user)
        {
            _chat2friend.Visible = true;
            _chat2friend.BringToFront();
            _chat2friend.MeUser = RobotManager.Instance.Default.getMyAccount();
            _chat2friend.FriendUser = user;
        }
        /// <summary>
        /// 聊天对话框中点击 好友信息
        /// </summary>
        /// <param name="user"></param>
        void _chat2friend_FriendInfoView(BaseContact user)
        {
            _friendInfo.FriendUser = user;
            _friendInfo.Visible = true;
            _friendInfo.BringToFront();
        }
        /// <summary>
        /// 聊天列表点击好友   开始聊天
        /// </summary>
        /// <param name="user"></param>
        private void wchatlist_StartChat(BaseContact user)
        {
            _chat2friend.Visible = true;
            _chat2friend.BringToFront();
            _chat2friend.MeUser = RobotManager.Instance.Default.getMyAccount();
            _chat2friend.FriendUser = user;
        }
        /// <summary>
        /// 通讯录中点击好友 查看好友信息
        /// </summary>
        /// <param name="user"></param>
        private void wfriendlist_FriendInfoView(BaseContact user)
        {
            _friendInfo.FriendUser = user;
            _friendInfo.Visible = true;
            _friendInfo.BringToFront();
        }

        private void frmMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.StopService = true;
            Environment.Exit(0);
        }
        #endregion
    }
}