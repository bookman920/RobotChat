using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace WeChat.NET.Logic.Common
{
    class UserFunc
    {
        /// <summary>
        /// 启动控制台
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        /// <summary>
        /// 释放控制台
        /// </summary>
        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();

        public static void PaintBar(Func<int> callback, string welcome = "载入中，请稍候...")
        {
            //先绘制出进度条的底色。
            Console.WriteLine(welcome + ":" + DateTime.Now.ToLongDateString() + DateTime.Now.ToLongTimeString());
            //绘制一个进度条背景
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            for (int i = 0; i <= 50; i++)
            {
                Console.Write(" ");
            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine();
            //根据计算线程的进度绘制进度条
            for (int p = 0; p <= 50;)
            {
                if (callback() < p)
                {
                    continue;
                }
                else
                {
                    p++;
                }
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.SetCursorPosition(p, Console.CursorTop - 1);
                Console.Write(" ");
                System.Threading.Thread.Sleep(100);

                Console.BackgroundColor = ConsoleColor.Black;
                Console.SetCursorPosition(0, Console.CursorTop + 1);
                Console.Write("{0}%", p * 2);
            }
        }

        public static string UrlEncode(string str)
        {
            StringBuilder sb = new StringBuilder();
            byte[] byStr = System.Text.Encoding.UTF8.GetBytes(str); //默认是System.Text.Encoding.Default.GetBytes(str)
            for (int i = 0; i < byStr.Length; i++)
            {
                sb.Append(@"%" + Convert.ToString(byStr[i], 16));
            }

            return (sb.ToString());
        }

        /// <summary>
        /// 返回当前时间戳（秒级）
        /// </summary>
        /// <returns></returns>
        public static Int64 GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        /// <summary>
        /// 返回当前时间戳（秒级）
        /// </summary>
        /// <returns></returns>
        public static Int64 GetTimeStampWithThousand() {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds * 1000);
        }

        public static void Log(string from, string to, string welcome) {
            string path = @"d:\MyTest1.txt";
            if (!File.Exists(path))
            { // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(to);
                    sw.WriteLine(from);
                    sw.WriteLine(welcome);
                }
            }
            else
            {// Open the file to read from.
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine(to);
                    sw.WriteLine(from);
                    sw.WriteLine(welcome);
                }
            }
        }

        public static void Test() {
            //string m_str = "\"雷振国\"邀请\"小天\"加入了群聊";
            //string[] sArray = m_str.Split('\"');
            //m_str = System.String.Format("@{0}wer0", sArray[2]);

            //string to = "", from = "";
            //ResulText rst = new ResulText();
            //rst.adddic(rst.mallstr);
            //Dictionary<string, string> m_dic = rst.mytxt;
            //foreach (string key in m_dic.Keys)
            //{
            //    WXService.Instance.SendMsg(m_dic[key].ToString(), to, from, 1);
            //    break;
            //}

            //wsendmsgBLL m_bll = new wsendmsgBLL();
            //List<sendmsgModel> m_model = m_bll.getlist(1, 10);
            //foreach (sendmsgModel key in m_model)
            //{
            //    WXService.Instance.SendMsg(key.msg, to, from, 1);
            //}
        }
    }
}
