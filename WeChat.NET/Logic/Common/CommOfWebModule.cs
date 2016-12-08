using Griffin.WebServer;
using Griffin.WebServer.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WeChat.NET.Logic.Packet;
using GameSystem.BaseFunc;

namespace WeChat.NET.Logic
{
    class CommOfWebModule : IWorkerModule
    {
        public void BeginRequest(IHttpContext context)
        {

        }

        public void EndRequest(IHttpContext context)
        {

        }

        public void HandleRequestAsync(IHttpContext context, Action<IAsyncModuleResult> callback)
        {
            // Since this module only supports sync
            callback(new AsyncModuleResult(context, HandleRequest(context)));
        }

        public ModuleResult HandleRequest(IHttpContext context)
        {
            if (context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {//Get
                Match getAct = (new Regex("act=(\\d+)")).Match(context.Request.Uri.Query);
                if (getAct.Groups.Count > 1)
                {
                    int aNo = 0;
                    int.TryParse(getAct.Groups[1].Value, out aNo);
                    if (aNo > 0)
                    {
                        PacketOfWeb pac = PacketOfWeb.FactoryOfPacket(aNo);

                        //执行异步操作并返回结果
                        var sb = new StringBuilder();
                        sb.Append(pac.Analyze(context).Execute());
                        context.Response.Body = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
                        context.Response.AddHeader("Content-Type", "application/json");
                        context.Response.AddHeader("Access-Control-Allow-Origin", "*");

                        //执行同步操作
                        Program.QueueWorker.Push(pac);
                    }
                }
                else {
                    var sb = new StringBuilder();
                    sb.Append(string.Format(@"
                        <img style='-webkit-user-select:none' src='{0}'><br/>
                        <form action='http://localhost:9988' method='get'>
                            <input type='hidden' name='act' value='1007' />
                            <input type = 'submit' value = '获取二维码地址' />
                        </form>
                    ", RobotManager.Instance.Default.getQrCodeUrl()))
                    .Append(@"
                        <form action='http://localhost:9988' method='get'>
                            <input type='hidden' name='act' value='1009' />
                            <input type = 'submit' value = '获取群列表' />
                        </form>
                        <form action='http://localhost:9988' method='get'>
                            <input type='hidden' name='act' value='1003' />
                            <input type = 'submit' value = '自动组群' />
                        </form>
                        <form action='http://localhost:9988' method='get'>
                            <input type='hidden' name='act' value='1004' />
                            请输入违禁语: <input type='text' name='content' value='' /><br/>
                            <input type = 'submit' value = '添加违禁语' />
                        </form>
                    ");
                    foreach (var room in RobotManager.Instance.Default.chatroomMgr.roomList.Values)
                    {
                        sb.Append(string.Format(@"
                            <form action='http://localhost:9988' method='get'>
                                <input type='hidden' name='act' value='1001' />
                                <input type='hidden' name='room' value='{1}' />
                                请输入群名称: <input type='text' name='content' value='{0}' /><br/>
                                <input type = 'submit' value = '修改群名称' />
                            </form>
                        ", room.NickName, room.Id));
                    }
                    foreach (var room in RobotManager.Instance.Default.chatroomMgr.roomList.Values)
                    {
                        sb.Append(string.Format(@"
                            <form action='http://localhost:9988' method='get'>
                                {2}<br/>
                                <input type='hidden' name='act' value='1002' />
                                <input type='hidden' name='room' value='{1}' />
                                请输入群欢迎语: <input type='text' name='content' value='{0}' /><br/>
                                <input type = 'submit' value = '修改群欢迎语' />
                            </form>
                        ", room.welcome_content, room.Id, room.NickName));
                    }
                    foreach (var room in RobotManager.Instance.Default.chatroomMgr.roomList.Values)
                    {
                        sb.Append(string.Format(@"
                            <form action='http://localhost:9988' method='get'>
                                {1}<br/>
                                <input type='hidden' name='act' value='1006' />
                                <input type='hidden' name='room' value='{0}' />
                                请输入发送内容: <input type='text' name='content' value='' /><br/>
                                <input type = 'submit' value = '群发消息' />
                            </form>
                        ", room.Id, room.NickName));
                    }

                    //签退功能：签退后重新登录有问题，暂时封存
                    //<form action='http://localhost:9988' method='get'>
                    //    <input type='hidden' name='act' value='1008' />
                    //    <input type = 'submit' value = '退出登录' />
                    //</form>
                    RobotManager.Instance.Default.blackWords.ListSafeForEach((word) => {
                        sb.Append(string.Format(@"
                        <form action='http://localhost:9988' method='get'>
                            <input type='hidden' name='act' value='1005' /><input type='text' name='content' value={0} /><input type = 'submit' value = '删除' />
                        </form>
                        ", word));
                        return true;
                    });
                    context.Response.Body = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
                    context.Response.AddHeader("Content-Type", "text/html");
                }
            }
            else { //Post
                //todo
            }

            return ModuleResult.Continue;
        }
    }
}
