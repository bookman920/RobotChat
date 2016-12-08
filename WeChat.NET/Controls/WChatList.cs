﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using WeChat.NET.Objects;
using System.Drawing;
using WeChat.NET.Protocol;
using WeChat.NET.Logic.Object;

namespace WeChat.NET.Controls
{
    /// <summary>
    /// 聊天列表控件
    /// </summary>
    class WChatList : ListBox
    {
        private System.ComponentModel.IContainer components;
        private int _mouse_over = -1;
        public event StartChatEventHandler StartChat;
        /// <summary>
        /// 
        /// </summary>
        public WChatList()
        {
            InitializeComponent();
            DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            UpdateStyles();
            BorderStyle = System.Windows.Forms.BorderStyle.None;
            Cursor = Cursors.Hand;

        }
        /// <summary>
        /// 重绘
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle bound;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            for (int i = 0; i < Items.Count; ++i)
            {
                BaseContact user = Items[i] as BaseContact;
                if (user != null)
                {
                    bound = GetItemRectangle(i);
                    if (!ClientRectangle.IntersectsWith(bound))
                    {
                        continue;
                    }
                    if (_mouse_over == i)
                    {
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(20, 37, 37, 37)), bound);
                    }
                    if (SelectedIndex == i)
                    {
                        e.Graphics.FillRectangle(Brushes.LightGray, bound);
                    }
                    e.Graphics.DrawLine(new Pen(Color.FromArgb(50,Color.Black)), new Point(bound.X, bound.Y + 70), new Point(bound.X + Width, bound.Y + 70));
                    using (Font f = new System.Drawing.Font("微软雅黑", 11))
                    {
                        e.Graphics.DrawString(user.ShowName, f, Brushes.Black, new PointF(bound.X + 70, bound.Y + 8)); //昵称
                    }
                    List<WXMsg> list_unread = user.GetUnReadMsg();
                    WXMsg latest = user.GetLatestMsg();
                    if (list_unread != null)  //有未读消息
                    {
                        using (Font f2 = new Font("微软雅黑", 10))
                        {
                            string time_str = list_unread[list_unread.Count - 1].Time.ToShortTimeString();
                            Size time_size = TextRenderer.MeasureText(time_str, f2);
                            e.Graphics.DrawString(time_str, f2, new SolidBrush(Color.FromArgb(37, 37, 37)), new PointF(bound.X + Width - time_size.Width - 13, bound.Y + 8)); //最后一条未读消息时间

                            string latest_msg = list_unread[list_unread.Count - 1].Msg.ToString();  //最后一条未读消息
                            latest_msg = latest_msg.Length <= 10 ? latest_msg : latest_msg.Substring(0, 10) + "...";
                            latest_msg = "[" + list_unread.Count + "条] " + latest_msg;
                            Size latest_msg_size = TextRenderer.MeasureText(latest_msg, f2);
                            e.Graphics.DrawString(latest_msg, f2, new SolidBrush(Color.FromArgb(37, 37, 37)), new PointF(bound.X + 70, bound.Y + 40));
                        }
                        using (Font f3 = new Font("微软雅黑", 8))  //未读消息条数  小红圆点
                        {
                            e.Graphics.FillEllipse(Brushes.Red, new Rectangle(bound.X + 10 + 50 - 9, bound.Y + 10 - 9, 18, 18));
                            e.Graphics.DrawString((list_unread.Count < 10 ? list_unread.Count.ToString() : "*"), f3, Brushes.White, new PointF(bound.X + 10 + 50 - 5, bound.Y + 10 - 7));
                        }
                    }
                    else //否则 显示最新的一条消息
                    {
                        if (latest != null)
                        {
                            using (Font f2 = new Font("微软雅黑", 10))
                            {
                                string time_str = latest.Time.ToShortTimeString();
                                Size time_size = TextRenderer.MeasureText(time_str, f2);
                                e.Graphics.DrawString(time_str, f2, new SolidBrush(Color.FromArgb(37, 37, 37)), new PointF(bound.X + Width - time_size.Width - 13, bound.Y + 8)); //最后一条未读消息时间

                                string latest_msg = latest.Msg.ToString();  //最新一条消息
                                latest_msg = latest_msg.Length <= 10 ? latest_msg : latest_msg.Substring(0, 10) + "...";
                                Size latest_msg_size = TextRenderer.MeasureText(latest_msg, f2);
                                e.Graphics.DrawString(latest_msg, f2, new SolidBrush(Color.FromArgb(37, 37, 37)), new PointF(bound.X + 70, bound.Y + 40));
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 设置每项高度
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            base.OnMeasureItem(e);
            e.ItemHeight = 70;
        }
        /// <summary>
        /// 选择变化
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }
        /// <summary>
        /// 鼠标双击
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            Invalidate();
            if (SelectedIndex != -1)
            {
                //开启聊天
                if (StartChat != null)
                {
                    StartChat(Items[SelectedIndex] as BaseContact);
                }
            }
        }
        /// <summary>
        /// 鼠标移动
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            for (int i = 0; i < Items.Count; ++i)
            {
                if (GetItemRectangle(i).Contains(e.Location))
                {
                    _mouse_over = i;
                    Invalidate();
                    return;
                }
            }
            _mouse_over = -1;
        }
        /// <summary>
        /// 鼠标移出
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _mouse_over = -1;
            Invalidate();
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();
            this.ResumeLayout(false);

        }
    }
    /// <summary>
    /// 表示处理开启聊天事件的方法
    /// </summary>
    /// <param name="user"></param>
    public delegate void StartChatEventHandler(BaseContact user);
}
