using System;
using System.Net;
using System.Threading;
using System.Windows.Forms;

using WeChat.NET.Logic;
using WeChat.NET.Logic.Common;
using WeChat.NET.Logic.Packet;

using GameSystem.BaseFunc;

using Griffin.WebServer;

namespace WeChat.NET
{
    /// <summary>
    /// 微信机器人管理程序，设计意图：
    /// 1、可以在电脑上管理多个微信机器人同时登陆。当前版本暂时只支持单个机器人
    /// 2、可以捕获、分析机器人的好友发送给机器人的各类消息，并做出合适的应答，也可以主动发起点对点聊天
    /// 3、可以对接各类开放式聊天机器人
    /// 4、群管理：可以建群、拉人，对入群者发送欢迎语，剔除不合适的群成员
    /// 5、群聊：可以对群内@自己的消息做出回应，可以对群内特定发言做出合适的应答，也可以主动推送合适的消息
    /// 
    /// 项目瓶颈：
    /// 1、（接口层面）只是针对非开放协议进行分析、包装，受制于原厂商的地方很多，例如不定期的协议修改，增加额外的流程控制等。
    /// 2、（接口层面）机器人的行为会受到抵制（例如拉黑）、封号（官方行动）
    /// 3、语义分析门槛较高，难以实现符合图灵标准的对话机制
    /// 4、到底是服务（被动应答），还是推广（spam），还是营销（互动），定位比较模糊
    /// 
    /// 项目意义：
    /// 1、结合Siri的语音识别和引导，以及一定程度的语义识别，可以实现友好的人机交互界面。例如，通过Siri说“给优小秘发微信 有一辆本田思域急售 5万里程 车况良好”
    /// 2、人脉维持功能，例如群管理、联系人点赞/发送问候语等
    /// </summary>
    static class Program
    {
        /// <summary>
        /// 关闭服务标志
        /// </summary>
        public static bool StopService = false;
        /// <summary>
        /// Web服务器对象
        /// </summary>
        public static HttpServer server = null;
        /// <summary>
        /// 测试模式
        /// </summary>
        public static bool isDebug = true;
        /// <summary>
        /// 用户上行报文队列
        /// </summary>
        public static DeliverList<PacketOfWeb> QueueWorker = new DeliverList<PacketOfWeb>();

        /// <summary>
        /// 微信机器人程序入口
        /// </summary>
        [STAThread]
        static void Main()
        {
            //开启控制台监控
            UserFunc.AllocConsole();

            // Create Module manager that handles all modules in the server
            var moduleManager = new ModuleManager();
            // Add the LogicModule
            moduleManager.Add(new CommOfWebModule());
            // Start the WebServer.
            server = new HttpServer(moduleManager);
            server.Start(IPAddress.Any, 9988);
            Console.WriteLine("WebServer Listened On PORT " + server.LocalPort);

            //开启任务队列
            new Thread(delegate () {
                while (StopService == false)
                {
                    try
                    {
                        if (QueueWorker != null)
                        {
                            PacketOfWeb data = QueueWorker.Remove();
                            if (data != null)
                            {
                                data.SyncOper();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                QueueWorker.Release();
            }).Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMainForm());

            //关闭线程服务
            StopService = true;

            //End the WebServer
            server.Stop();

            //关闭控制台
            UserFunc.FreeConsole();
        }
    }
}

