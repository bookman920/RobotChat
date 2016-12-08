using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WeChat.NET.Logic.Common
{
    class Func
    {
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
            //    wxs.SendMsg(m_dic[key].ToString(), to, from, 1);
            //    break;
            //}

            //wsendmsgBLL m_bll = new wsendmsgBLL();
            //List<sendmsgModel> m_model = m_bll.getlist(1, 10);
            //foreach (sendmsgModel key in m_model)
            //{
            //    wxs.SendMsg(key.msg, to, from, 1);
            //}

            string path = @"d:\MyTest1.txt", from = "Mary", to = "John", welcome = "hello";
            if (!File.Exists(path)) { // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(to);
                    sw.WriteLine(from);
                    sw.WriteLine(welcome);
                }
            }
            else {// Open the file to read from.
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine(to);
                    sw.WriteLine(from);
                    sw.WriteLine(welcome);
                }
            }
        }
    }
}
