using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WeChat.NET.Logic;
using WeChat.NET.Logic.Object;

namespace WeChat.NET.Controls
{
    /// <summary>
    /// 个人信息面板
    /// </summary>
    public partial class WPersonalInfo : UserControl
    {
        public event StartChatEventHandler StartChat;

        private bool _showTopPanel = true;
        /// <summary>
        /// 是否显示最上端栏
        /// </summary>
        public bool ShowTopPanel
        {
            set
            {
                _showTopPanel = value;
                if (_showTopPanel)
                {
                    plTop.Visible = true;
                    btnSendMsg.Visible = true;
                }
                else
                {
                    plTop.Visible = false;
                    btnSendMsg.Visible = false;
                }
            }
            get
            {
                return _showTopPanel;
            }
        }

        /// <summary>
        /// 好友
        /// </summary>
        private BaseContact _friendUser;
        public BaseContact FriendUser
        {
            get
            {
                return _friendUser;
            }
            set
            {
                _friendUser = value;
                if (_friendUser != null)
                {
                    lblNick.Text = _friendUser.NickName;
                    lblArea.Text = _friendUser.City + "，" + _friendUser.Province;
                    lblSignature.Text = _friendUser.Signature;
                    picSexImage.Image = _friendUser.Sex == "1" ? Properties.Resources.male : Properties.Resources.female;
                    picSexImage.Location = new Point(lblNick.Location.X + lblNick.Width + 4, picSexImage.Location.Y);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public WPersonalInfo()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 界面大小改变  调整显示位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WPersonalInfo_Resize(object sender, EventArgs e)
        {
            plCenter.Location = new Point((Width - plCenter.Width) / 2, (Height + plTop.Height - plCenter.Height) / 2);

        }
        /// <summary>
        /// 返回
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBack_Click(object sender, EventArgs e)
        {
            Visible = false;
        }
        /// <summary>
        /// 发送消息 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            if (StartChat != null)
            {
                StartChat(_friendUser);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                RobotManager.Instance.Default.send_img_msg_by_uid(dialog.FileName, _friendUser.Id);
            }
        }
    }
}
