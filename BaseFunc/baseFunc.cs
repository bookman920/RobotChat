using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json.Linq;

namespace GameSystem.BaseFunc
{
    /// <summary>
    /// 类呼叫中心管理器
    /// 三类最重要的操作：
    /// 1、exit：退出整个系统
    /// 2、#：打断当前操作并立即返回上一级菜单
    /// 3、*：打断当前操作
    ///     1）、要求你连续输入多张卡牌ID，你可以在输入两张后输入*表示没有更多卡牌信息输入
    ///     2）、要求你输入身份证号进行查询，而你此时想放弃这次查询操作，可以直接输入*
    /// </summary>
    public static class MenuManager
    {
        /// <summary>
        /// 退出系统标记
        /// </summary>
        private static bool isExist = false;
        /// <summary>
        /// 本次操作是否继续
        /// </summary>
        private static bool CurrentOperation = true;
        /// <summary>
        /// 是否需要继续执行当前操作
        /// </summary>
        public static bool OperactionActive
        {
            get { return !isExist && CurrentOperation; }
        }
        /// <summary>
        /// 刷新菜单标记
        /// </summary>
        private static bool RefreshMenu = true;

        /// <summary>
        /// 菜单栈
        /// </summary>
        private static Stack<Dictionary<string, BXS_MenuItem>> TransactionList = new Stack<Dictionary<string, BXS_MenuItem>>(10);
        /// <summary>
        /// 菜单列表，从外部传入
        /// </summary>
        private static Dictionary<string, BXS_MenuItem> MainMenu = null;

        private static string welcome = "百晓生呼叫中心";
        /// <summary>
        /// 启动菜单系统
        /// </summary>
        /// <param name="_MainMenu">系统菜单字典</param>
        /// <param name="_welcome">欢迎语</param>
        /// <param name="end">设置关闭动作</param>
        public static void Start(Dictionary<string, BXS_MenuItem> _MainMenu, string _welcome, Action end)
        {
            isExist = false;

            MainMenu = _MainMenu;

            if (!String.IsNullOrEmpty(_welcome))
            {
                welcome = _welcome;
            }

            if (MainMenu != null)
            {
            }
            //推入初始菜单
            TransactionList.Push(MainMenu);

            Console.WriteLine(" ---------------------------------------------------------------");
            Console.WriteLine("| 欢迎来到" + welcome + "，请输入您的选择（exit退出 #回上一级）：|");
            Console.WriteLine(" ---------------------------------------------------------------");
            while (!isExist)
            {
                //复位操作继续标记
                CurrentOperation = true;
                PrintPopMenu();
                Console.WriteLine("--------------------------------------");
                string cmd = "";
                UserInput(ref cmd);
                Execute(cmd);
            }

            if (end != null)
            {
                end();
            }
        }

        public static void PrintPopMenu()
        {
            if (RefreshMenu)
            {
                foreach (KeyValuePair<string, BXS_MenuItem> vi in TransactionList.Peek())
                {
                    Console.WriteLine(vi.Key + "、" + vi.Value.Title);
                }
            }
        }

        /// <summary>
        /// 获取用户输入
        /// </summary>
        /// <param name="command">记录用户输入内容</param>
        /// <returns>决定是否继续循环等待用户输入</returns>
        public static bool UserInput(ref string command)
        {
            if (!CurrentOperation) return false;

            command = Console.ReadLine();

            switch (command.ToLower())
            {
                case "exit":
                case "quit":
                    //标记需要退出系统
                    isExist = true;
                    //标记需要刷新菜单
                    RefreshMenu = false;
                    //不再循环等待
                    return false;

                case "*":
                    //标记需要刷新菜单
                    RefreshMenu = true;
                    //不再循环等待
                    return false;

                case "#":
                    if (TransactionList.Count > 1)
                    {
                        //回到上一级菜单


                        TransactionList.Pop();
                    }
                    //标记需要刷新菜单


                    RefreshMenu = true;
                    //本次操作不再继续
                    CurrentOperation = false;
                    //不再循环等待
                    return false;

                default:
                    //本次输入后，还要继续循环等待
                    return true;
            }
        }

        public static void Execute(string command)
        {
            Dictionary<string, BXS_MenuItem> CurMenu = TransactionList.Peek();
            if (CurMenu.ContainsKey((command)))
            {
                if (CurMenu[command].Handle != null)
                {
                    Console.WriteLine("---欢迎进入【" + CurMenu[command].Title + "】模块，*退出---");
                    RefreshMenu = CurMenu[command].Handle(CurMenu[command], command);
                }
                else if (CurMenu[command].subMenu != null)
                {
                    TransactionList.Push(CurMenu[command].subMenu);
                    RefreshMenu = true;
                }
                else
                {
                    Console.WriteLine("【提示】该功能尚未开放，请重新选择（Exit退出 #回上一级）");
                    RefreshMenu = false;
                }
            }
            else
            {
                if (command != "#")
                {
                    Console.WriteLine("【提示】输入有误，请重新输入（Exit退出 #回上一级）");
                }
                else
                {
                    if (TransactionList.Count == 1)
                    {
                        Console.WriteLine("【提示】已到达顶层菜单（Exit退出）");
                    }
                }
                RefreshMenu = true;
            }
        }
    }

    /// <summary>
    /// 菜单项
    /// </summary>
    public class BXS_MenuItem
    {
        /// <summary>
        /// 菜单标题
        /// </summary>
        public string Title = "";
        /// <summary>
        /// 菜单执行动作句柄
        /// </summary>
        public Func<BXS_MenuItem, string, bool> Handle = null;
        /// <summary>
        /// 子菜单
        /// </summary>
        public Dictionary<string, BXS_MenuItem> subMenu = null;
        /// <summary>
        /// 菜单项构造函数
        /// </summary>
        /// <param name="_Id">菜单标识</param>
        /// <param name="_Title">菜单提示信息</param>
        /// <param name="_Handle">执行句柄</param>
        public BXS_MenuItem(string _Title, Func<BXS_MenuItem, string, bool> _Handle, Dictionary<string, BXS_MenuItem> _subMenu)
        {
            Title = _Title;
            Handle = _Handle;
            subMenu = _subMenu;
        }
    }

    public static class ExpireTime
    {
        /// <summary>
        /// 计算指定时间和标准时间（2012-1-1）之间的时差（秒）
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static UInt32 ToNumber(DateTime t)
        {
            return (UInt32)(((TimeSpan)(t - DateTime.Parse("2012-1-1 0:00:00"))).TotalSeconds);
        }
        /// <summary>
        /// 返回2012-1-1之后指定秒数后的时间
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static DateTime ToExpireTime(UInt32 m)
        {
            return DateTime.Parse("2012-1-1 0:00:00").AddSeconds(m);
        }
    }

    /// <summary>
    /// 通用状态管理类
    /// </summary>
    /// <typeparam name="T">状态类型</typeparam>
    public class StatusManager<T>
    {
        /// <summary>
        /// 基本构造函数
        /// </summary>
        public StatusManager() { }
        /// <summary>
        /// 带初始状态的构造函数
        /// </summary>
        /// <param name="DescStatus"></param>
        public StatusManager(T DescStatus)
        {
            Set(DescStatus);
        }
        /// <summary>
        /// 带初始状态的构造函数
        /// </summary>
        /// <param name="st"></param>
        public StatusManager(uint st)
        {
            this.Status = this.Status | st;
        }

        /// <summary>
        /// 状态持续回合数
        /// </summary>
        private Dictionary<T, int> ValidPeriod = new Dictionary<T, int>();

        /// <summary>
        /// 状态初始化
        /// </summary>
        public void Init()
        {
            this.Status = 0;

            //初始化状态持续回合数
            if (ValidPeriod != null)
            {
                for (int i = 0; i < ValidPeriod.Count; i++)
                {
                    ValidPeriod[ValidPeriod.ElementAt(i).Key] = 0;
                }
            }
        }

        #region 下面一组函数用于实现带持续回合的状态管理 状态检测仍旧统一调用Check

        /// <summary>
        /// 设置带持续回合数的状态
        /// 注意：调用Increase(T, -1)或者Set(T)，都会设置永久有效的Buff。保留Increase(T, -1)这种形式是为了将设置权交给配置表
        /// </summary>
        /// <param name="DescStatus">要设置的状态位</param>
        /// <param name="cnt">持续的回合数，如果为-1表示持久有效</param>
        public void Increase(T DescStatus, int cnt)
        {
            if (cnt == 0) { return; }

            if (ValidPeriod.ContainsKey(DescStatus))
            {
                if (cnt == -1)
                {
                    //新增回合数为永久有效，最终设置为永久有效
                    ValidPeriod[DescStatus] = cnt;
                }
                else
                {
                    if (ValidPeriod[DescStatus] != -1 && cnt > 0)
                    {
                        //如果原先已经是永久有效就不处理，否则叠加新增持续回合数
                        ValidPeriod[DescStatus] += cnt;
                    }
                }
            }
            else
            {
                //先前没有持续回合标志，进行添加（无论是否永久）
                ValidPeriod[DescStatus] = cnt;
            }
            Set(DescStatus);
        }

        /// <summary>
        /// 返回指定状态剩余持续回合数（-1表示永久持续）
        /// </summary>
        /// <param name="DescStatus">指定的检测状态</param>
        /// <returns>剩余持续回合数，-1表示永久持续</returns>
        public int LeftCount(T DescStatus)
        {
            if (!ValidPeriod.ContainsKey(DescStatus))
            {
                if (Check(DescStatus))
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return ValidPeriod[DescStatus];
            }
        }

        /// <summary>
        /// 扣减指定状态的持续回合数，如果回合数降为0则消除此状态
        /// 注意：如果没有设置持续回合数，或者持续回合数被设置为-1，则此状态为永久持续状态,多次调用Descrease也不会消除该状态
        /// 永久持续状态必须直接调用UnSet来消除。
        /// </summary>
        /// <param name="DescStatus">指定的状态类型</param>
        public bool Descrease(T DescStatus)
        {
            if (ValidPeriod.ContainsKey(DescStatus) && ValidPeriod[DescStatus] > 0)
            {
                ValidPeriod[DescStatus] -= 1;
            }

            if (ValidPeriod.ContainsKey(DescStatus) && ValidPeriod[DescStatus] == 0)
            {
                UnSet(DescStatus);
                return true;
            }
            return false;
        }

        #endregion

        #region 下面一组函数用于设定/取消永久有效的状态 状态检测仍旧统一调用Check
        /// <summary>
        /// 将指定状态叠加到状态集中
        /// </summary>
        /// <param name="AddedStatus"></param>
        public void Set(T DescStatus)
        {
            this.Status = this.Status | Convert.ToUInt32(DescStatus);
        }

        /// <summary>
        /// 从状态集中去除指定的状态
        /// </summary>
        /// <param name="DescStatus"></param>
        public void UnSet(T DescStatus)
        {
            this.Status = this.Status & ~Convert.ToUInt32(DescStatus);
            //清除持续回合信息
            ValidPeriod.Remove(DescStatus);
        }
        #endregion

        /// <summary>
        /// 检测状态集中是否已经设置了指定状态
        /// </summary>
        /// <param name="ai"></param>
        /// <returns></returns>
        public bool Check(T DescStatus)
        {
            uint test = Convert.ToUInt32(DescStatus);
            return (this.Status & test) == test;
        }

        /// <summary>
        /// 检测指定状态集中是否已经设置了指定状态
        /// 静态调用方法
        /// </summary>
        /// <param name="Ori">指定状态集</param>
        /// <param name="DescStatus">指定状态</param>
        /// <returns>True 已设置 False 未设置</returns>
        public static bool Check(T Ori, T DescStatus)
        {
            StatusManager<T> checker = new StatusManager<T>(Ori);
            return checker.Check(DescStatus);
        }

        /// <summary>
        /// 列表所有已经设置的状态位
        /// </summary>
        /// <param name="StatusListHandle"></param>
        /// <returns></returns>
        public List<T> List()
        {
            List<T> ret = new List<T>();
            foreach (T ai in Enum.GetValues(typeof(T)))
            {
                if (this.Check(ai))
                {
                    ret.Add(ai);
                }
            }
            return ret;
        }
        /// <summary>
        /// 状态保存变量
        /// </summary>
        private uint Status = 0;

        /// <summary>
        /// 是否为空状态集（所有状态均未设置）
        /// </summary>
        public bool isNull
        {
            get
            {
                return Status == 0;
            }
        }

        /// <summary>
        /// 获取数值
        /// </summary>
        public uint Value
        {
            get
            {
                return this.Status;
            }
        }

        /// <summary>
        /// 查询数值
        /// </summary>
        /// <returns></returns>
        public uint GetStatusValue()
        {
            return Status;
        }
    }

    /// <summary>
    /// 日志管理
    /// </summary>
    public class LogInfoManager
    {
        ///<summary>
        ///保存日志的文件夹
        ///</summary>
        public static string LogPath
        {
            get
            {
                if (String.IsNullOrEmpty(_LogPath))
                {
                    Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                    _LogPath = asm.Location.Replace(asm.GetName().Name + ".dll", "") + "\\log\\";
                }
                return _LogPath;
            }
        }
        public static string _LogPath = "";

        ///<summary>
        ///日志文件前缀
        ///</summary>
        public static string LogFielPrefix = "BXS_";
        /// <summary>
        /// 自增序列
        /// </summary>
        private static IdentityManager SerialNo = new IdentityManager(1);
        /// <summary>
        /// 版本 当一个日志文件到达指定大小时候，版本号会 +1
        /// </summary>
        public static int version = 0;
        /// <summary>
        /// 日期
        /// </summary>
        public static string date = "";
        /// <summary>
        /// 获取完整文件路径 + 文件名
        /// </summary>
        /// <param name="logFile">日志类型 Error/Buff/Info..</param>
        /// <returns></returns>
        public static string fileName(string logFile)
        {
            if (!date.Equals(DateTime.Now.ToString("yyyyMMdd")))
            {
                date = DateTime.Now.ToString("yyyyMMdd");
                SerialNo.Reset();   //日期变化后，序列重置
                version = 0;
            }
            return LogPath + LogFielPrefix + logFile + " " + date + "-" + version + ".Log";
        }
        /// <summary>
        /// 写日志
        /// </summary>
        private static void WriteLog(string logFile, string msg)
        {
            try
            {
                using (StreamWriter sw = File.AppendText(fileName(logFile)))
                {
                    if (sw.BaseStream.Length < 1024 * 1024 * 10) //可以独立出来配置 目前日志限定 10M左右  
                    {
                        sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss: ") + msg);
                    }
                    else
                    {
                        sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss: ") + msg);
                        version = SerialNo.CurrentIdentity; //版本号 +1
                    }
                    sw.Close();
                }
            }
            catch
            { }
        }

        /// <summary>
        /// 写日志
        /// </summary>
        public static void WriteLog(LogFile logFile, string msg)
        {
            Console.WriteLine(msg);
            switch (logFile)
            {
                case LogFile.Error:
                    WriteLog(logFile.ToString(), msg);
                    break;
                case LogFile.Trace:
                    WriteLog(logFile.ToString(), msg);
                    break;
                case LogFile.Buff:
                    WriteLog(logFile.ToString(), msg);
                    break;
                default:
                    WriteLog(logFile.ToString(), msg);
                    break;
            }
        }

        /// <summary>
        /// 日志类型
        /// </summary>
        public enum LogFile
        {
            Trace,
            Warning,
            Error,
            Buff,
            SQL,
            Battle,
            BattleSubmit
        }
    }

    public static class IExtendOfDateTime
    {
        public static DateTime SetUTC(this DateTime t)
        { 
            return DateTime.SpecifyKind(t, DateTimeKind.Utc);
        }
    }

    public static class IExtendOfString
    {
        /// <summary>
        /// 将字符串转换为List
        /// </summary>
        /// <typeparam name="T">范型</typeparam>
        /// <param name="value">待转化的字符串</param>
        /// <param name="split">分割符</param>
        /// <param name="ac">T的序列化方法</param>
        /// <returns>转换后的List</returns>
        public static List<T> ToList<T>(this string value, char split, Func<string, T> ac)
        {
            List<T> returnvalue = new List<T>();
            string[] heroCardStr = value.Split(split);
            for (int i = 0; i < heroCardStr.Length; i++)
            {
                if (!String.IsNullOrEmpty(heroCardStr[i]))
                {
                    T hcObj = ac(heroCardStr[i].Trim());
                    if (hcObj != null)
                    {//成功反序列化，将得到的对象添加到列表
                        returnvalue.Add(hcObj);
                    }
                }
            }
            return returnvalue;
        }

        /// <summary>
        /// 将字符串转换为数据字典
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="split"></param>
        /// <param name="ac"></param>
        /// <returns></returns>
        public static Dictionary<K, T> ToDictionary<K, T>(this string value, char split, Func<string, KeyValuePair<K,T>> ac)
        {
            Dictionary<K, T> returnvalue = new Dictionary<K, T>();
            string[] heroCardStr = value.Split(split);
            for (int i = 0; i < heroCardStr.Length; i++)
            {
                if (!String.IsNullOrEmpty(heroCardStr[i]))
                {
                    KeyValuePair<K, T> hcObj = ac(heroCardStr[i].Trim());
                    if (hcObj.Value != null && !returnvalue.ContainsKey(hcObj.Key)) {
                        returnvalue.Add(hcObj.Key, hcObj.Value);
                    }
                }
            }
            return returnvalue;
        }

        /// <summary>
        /// 对格式化字符串中，各个对象信息进行特定处理，并返回处理结果
        /// </summary>
        /// <param name="value">包含多个对象信息的字符串</param>
        /// <param name="split">对象信息分隔符</param>
        /// <param name="ac">对单个对象信息的处理方法, 参数1为分组编号（base0），参数2为分组字符串内容</param>
        public static string OperRecyList(this string value, char split, Func<int, string, string> ac)
        {
            string ret = "";
            if (!String.IsNullOrEmpty(value))
            {
                string[] sList = value.Trim().Split(new char[] { split });
                for (int i = 0; i < sList.Length; i++)
                {
                    if (!String.IsNullOrEmpty(sList[i]))
                    {
                        sList[i] = ac(i, sList[i]);
                    }
                }

                foreach (string s in sList)
                {
                    if (!String.IsNullOrEmpty(s))
                    {
                        if (ret != "") { ret += split; }
                        ret += s;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// 对格式化字符串中，各个对象信息进行依次处理
        /// </summary>
        /// <param name="value">包含多个对象信息的字符串</param>
        /// <param name="split">对象信息分隔符</param>
        /// <param name="ac">对单个对象信息的处理方法, 参数1为分组编号（base0），参数2为分组字符串内容</param>
        public static void OperRecyListNotReturn(this string value, char split, Action<int, string> ac)
        {
            if (!String.IsNullOrEmpty(value))
            {
                string[] sList = value.Trim().Split(new char[] { split });
                for (int i = 0; i < sList.Length; i++)
                {
                    if (!String.IsNullOrEmpty(sList[i]))
                    {
                        ac(i, sList[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 过滤html
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string ReplaceHtml(this string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }
            message = message.Trim();
            message = Regex.Replace(message, @"[\s]{2,}", " ");
            message = Regex.Replace(message, @"(<[b|B][r|R]/*>)+|(<[p|P](.|\n)*?>)", " ");
            message = Regex.Replace(message, @"(\s*&[n|N][b|B][s|S][p|P];\s*)+", " ");
            message = Regex.Replace(message, @"<(.|\n)*?>", string.Empty);
            message = Regex.Replace(message, @"/<\/?[^>]*>/g", string.Empty);
            message = Regex.Replace(message, @"/[    | ]* /g", string.Empty);
            message = message.Replace("'", "''");
            message = Regex.Replace(message, @"/ [\s| |    ]* /g", string.Empty);

            return message;
        }
    }

    /// <summary>
    /// ArrayList 扩展
    /// </summary>
    public static class ArrayListExtension
    {
        public static ArrayList CloneArrayList(this ArrayList arr)
        {
            ArrayList ret = new ArrayList();
            foreach (var val in arr)
            {
                ret.Add(val);
            }
            return ret;
        }
    }

    public static class IExtendOfDictionary
    {
        /// <summary>
        /// 将列表序列化
        /// </summary>
        /// <typeparam name="T">泛型成员类型</typeparam>
        /// <param name="list">泛型list，调用者</param>
        /// <param name="ac">列表中单个成员对象的序列化方法，例如泛型类型为userObject，则序列化方法为userObject.uid，或者userObject.ToString()</param>
        /// <param name="split">各成员对象间的分隔符</param>
        /// <returns>包含泛型列表全部成员对象序列化串的列表字符串，各对象序列化串用split进行区隔，末尾不含split</returns>
        public static string DictionaryToString<K, T>(this Dictionary<K, T> list, Func<T, string> ac, string split)
        {
            string res = "";
            foreach (var item in list.Values)
            {
                if (res != "") { res += split; }
                res += ac(item);
            }
            return res;
        }

        public static List<V> ToValueList<K, V>(this Dictionary<K, V> ab)
        {
            List<V> ret = new List<V>();
            foreach (V item in ab.Values)
            {
                ret.Add(item);
            }
            return ret;
        }

        /// <summary>
        /// 利用传入的委托，将列表翻译为JArray对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static JArray ToJArray<K, T>(this Dictionary<K, T> a, Func<T, JObject> f)
        {
            JArray gms = new JArray();
            a.DictionaryForEach((T item) => {
                gms.Add(f(item));
                return true;
            });
            return gms;
        }

        /// <summary>
        /// 字典同步方法封装 - 列表拷贝
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ab"></param>
        /// <returns></returns>
        public static Dictionary<K, V> DictionaryClone<K, V>(this Dictionary<K, V> ab)
        {
            Dictionary<K, V> ret = new Dictionary<K, V>();
            if (Monitor.TryEnter(ab, 1000))
            {
                try
                {
                    foreach (var item in ab)
                    {
                        if (!ret.ContainsKey(item.Key))
                        {
                            ret.Add(item.Key, item.Value);
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(ab);
                }
            }
            return ret;
        }

        /// <summary>
        /// 字典遍历（线程安全），可根据委托返回值提前结束遍历
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="ab"></param>
        /// <param name="ac"></param>
        public static void DictionarySafeForEach<K, V>(this Dictionary<K, V> ab, Func<V, bool> ac)
        {
            Dictionary<K, V> recy = ab.DictionaryClone<K, V>();
            foreach (V item in recy.Values)
            {
                if (!ac(item))
                {
                    break;
                }
            }
            recy.Clear();
            recy = null;
        }

        /// <summary>
        /// 字典遍历（非线程安全），可根据委托返回值提前结束遍历
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="ab"></param>
        /// <param name="ac"></param>
        public static void DictionaryForEach<K, V>(this Dictionary<K, V> ab, Func<V, bool> ac)
        {
            foreach (V item in ab.Values)
            {
                if (!ac(item))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 检索字典中符合条件的对象列表
        /// </summary>
        /// <param name="ac">判断条件</param>
        /// <param name="RequireLock">是否需要同步锁</param>
        /// <returns></returns>
        public static List<V> DictionaryFindAll<K, V>(this Dictionary<K, V> ab, Func<V, bool> ac, bool RequireLock)
        {
            List<V> retu = new List<V>();
            if (RequireLock)
            {
                lock (ab)
                {
                    IEnumerable<KeyValuePair<K, V>> ret = ab.Where<KeyValuePair<K, V>>(d => ac(d.Value));
                    foreach (var cur in ret)
                    {
                        retu.Add(cur.Value);
                    }
                }
            }
            else
            {
                IEnumerable<KeyValuePair<K, V>> ret = ab.Where<KeyValuePair<K, V>>(d => ac(d.Value));
                foreach (var cur in ret)
                {
                    retu.Add(cur.Value);
                }
            }
            return retu;
        }

        /// <summary>
        /// 检索字典中符合条件的对象列表
        /// </summary>
        /// <param name="ac">判断条件</param>
        /// <param name="RequireLock">是否需要同步锁</param>
        /// <returns></returns>
        public static List<K> DictionaryFindAllIdx<K, V>(this Dictionary<K, V> ab, Func<V, bool> ac, bool RequireLock)
        {
            List<K> retu = new List<K>();
            if (RequireLock)
            {
                lock (ab)
                {
                    IEnumerable<KeyValuePair<K, V>> ret = ab.Where<KeyValuePair<K, V>>(d => ac(d.Value));
                    foreach (var cur in ret)
                    {
                        retu.Add(cur.Key);
                    }
                }
            }
            else
            {
                IEnumerable<KeyValuePair<K, V>> ret = ab.Where<KeyValuePair<K, V>>(d => ac(d.Value));
                foreach (var cur in ret)
                {
                    retu.Add(cur.Key);
                }
            }
            return retu;
        }

        /// <summary>
        /// 根据查询条件，返回值、域对列表
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="ab"></param>
        /// <param name="ac"></param>
        /// <param name="RequireLock"></param>
        /// <returns></returns>
        public static List<KeyValuePair<K, V>> DictionaryFindKeyValuePair<K, V>(this Dictionary<K, V> ab, Func<KeyValuePair<K, V>, bool> ac, bool RequireLock)
        {
            List<KeyValuePair<K, V>> retu;
            if (RequireLock)
            {
                lock (ab)
                {
                    retu = ab.Where<KeyValuePair<K, V>>(d => ac(d)).ToList();
                }
            }
            else
            {
                retu = ab.Where<KeyValuePair<K, V>>(d => ac(d)).ToList();
            }
            return retu;
        }

        /// <summary>
        /// 移除全部符合条件的子项
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="ab"></param>
        /// <param name="pre"></param>
        public static void DictionaryRemoveAll<K, V>(this Dictionary<K, V> ab, Func<V, bool> ac)
        {
            lock (ab)
            {
                List<K> retu = new List<K>();
                IEnumerable<KeyValuePair<K, V>> ret = ab.Where<KeyValuePair<K, V>>(d => ac(d.Value));
                foreach (var cur in ret)
                {
                    retu.Add(cur.Key);
                }
                retu.ForEach(ri => ab.Remove(ri));
            }
        }

        /// <summary>
        /// 列表同步方法封装 - 添加项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ab"></param>
        /// <param name="item"></param>
        public static void DictionaryAdd<K, V>(this Dictionary<K, V> ab, K id, V val)
        {
            if (val != null)
            {
                lock (ab)
                {
                    if (!ab.ContainsKey(id))
                    {
                        ab.Add(id, val);
                    }
                }
            }
        }

        /// <summary>
        /// 列表同步方法封装 - 替换或添加项
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="ab"></param>
        /// <param name="id"></param>
        /// <param name="val"></param>
        public static void DictionaryExchange<K, V>(this Dictionary<K, V> ab, K id, V val)
        {
            if (val != null)
            {
                lock (ab)
                {
                    if (!ab.ContainsKey(id))
                    {
                        ab.Add(id, val);
                    }
                    else
                    {
                        ab[id] = val;
                    }
                }
            }
        }

        /// <summary>
        /// 列表同步方法封装 - 删除项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ab"></param>
        /// <param name="item"></param>
        public static bool DictionaryRemove<K, V>(this Dictionary<K, V> ab, K id)
        {
            lock (ab)
            {
                return ab.Remove(id);
            }
        }

        /// <summary>
        /// 列表同步方法封装 - 查找项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ab"></param>
        /// <param name="item"></param>
        public static V DictionaryFind<K, V>(this Dictionary<K, V> ab, K id)
        {
            lock (ab)
            {
                if (ab.ContainsKey(id))
                {
                    return ab[id];
                }
                else
                {
                    return default(V);
                }
            }
        }

        public static void DictionaryClear<K, V>(this Dictionary<K, V> ab)
        {
            lock (ab)
            {
                ab.Clear();
            }
        }
    }

    public static class IExtendOfList
    {
        /// <summary>
        /// 擴展方法，從數組中獲取隨機對象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="va"></param>
        /// <returns></returns>
        public static T RandObject<T>(this List<T> a)
        {
            if (a.Count > 0)
            {
                return a[a.Count.RandomLength()];
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// 利用传入的委托，将列表翻译为JArray对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static JArray ToJArray<T>(this List<T> a, Func<T, JObject> f)
        {
            JArray gms = new JArray();
            a.ListForEach((T item) => {
                gms.Add(f(item));
                return true;
            });
            return gms;
        }

        /// <summary>
        /// 擴展方法，從數組中獲取隨機對象并移除已选
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="va"></param>
        /// <returns></returns>
        public static T RandObjectRemove<T>(this List<T> a)
        {
            if (a.Count > 0)
            {
                T ret = a[a.Count.RandomLength()];
                a.Remove(ret);

                return ret;
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// 擴展方法，從數組中獲取隨機N个對象
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="a">原数组</param>
        /// <param name="sum">需要的对象个数</param>
        /// <returns>随机元素列表</returns>
        public static List<T> RandObject<T>(this List<T> a, int sum)
        {
            List<T> ret = new List<T>();
            if (a.Count > 0)
            {
                //最多能满足的列表数量
                int _sum = (a.Count >= sum ? sum : a.Count);
                while (ret.Count < _sum)
                {
                    T it = a.RandObject();
                    if (it != null && !ret.Contains(it))
                    {
                        ret.Add(it);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// 判断b组的元素是否都在a组
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Contains<T>(this List<T> a, List<T> b)
        {
            for (int i = 0; i < b.Count; i++)
            {
                if (a.Contains(b[i]) != true)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 判断b元素是否在a列表中
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Contains<T>(this List<T> a, T b)
        {
            foreach (var item in a)
            {
                if (item.Equals(b))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 列表序列化：默认“,”分割符
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="list">泛型List，调用者</param>
        /// <param name="ac">从泛型中获取Id的委托，例如泛型为userObject，则委托为userObject.uid.ToString()或userObject.ToString()</param>
        /// <returns>包含泛型列表全部元素Id的列表字符串，用“,”进行分割，末尾不含“,”</returns>
        public static string ToListString<T>(this List<T> list, Func<T, string> ac)
        {
            return ToListString<T>(list, ac, ",");
        }

        /// <summary>
        /// 列表序列化
        /// </summary>
        /// <typeparam name="T">泛型成员类型</typeparam>
        /// <param name="list">泛型list，调用者</param>
        /// <param name="ac">列表中单个成员对象的序列化方法，例如泛型类型为userObject，则序列化方法为userObject.uid，或者userObject.ToString()</param>
        /// <param name="split">各成员对象间的分隔符</param>
        /// <returns>包含泛型列表全部成员对象序列化串的列表字符串，各对象序列化串用split进行分割，末尾不含split</returns>
        public static string ToListString<T>(this List<T> list, Func<T, string> ac, string split)
        {
            string res = "";
            list.ForEach(u =>
            {
                if (res != "") { res += split; }
                res += ac(u);
            });
            return res;
        }

        /// <summary>
        /// 对List进行随机排序
        /// </summary>
        /// <param name="ListT"></param>
        /// <returns></returns>
        public static List<T> ListRandomSort<T>(this List<T> ListT)
        {
            Random random = new Random();
            for (int i = 0; i < ListT.Count; i++)
            {
                ListT.Reverse(0, Math.Max(1, random.Next(ListT.Count + 1)));
                ListT.Reverse(0, Math.Max(1, random.Next(ListT.Count + 1)));
            }
            return ListT;
        }

        /// <summary>
        /// List分页算法
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="ListT">调用者</param>
        /// <param name="PageNo">页码,必须从1开始</param>
        /// <param name="PageSize">页尺寸</param>
        /// <param name="PageCount">总页数</param>
        /// <param name="Sum">总条目数</param>
        /// <returns>分页列表</returns>
        public static List<T> GetPageList<T>(this List<T> ListT, ref int PageNo, int PageSize, ref int PageCount, ref int Sum)
        {
            Sum = ListT.Count;
            return GetPageList(ListT, ref PageNo, PageSize, ref PageCount);
        }

        /// <summary>
        /// List分页算法
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="ListT">调用者</param>
        /// <param name="PageNo">页码,必须从1开始</param>
        /// <param name="PageSize">页尺寸</param>
        /// <param name="PageCount">总页数</param>
        /// <returns>分页列表</returns>
        public static List<T> GetPageList<T>(this List<T> ListT, ref int PageNo, int PageSize, ref int PageCount)
        {
            List<T> newList = new List<T>();

            if (ListT.Count > 0)
            {
                //进行必要的数据修正
                PageSize = Math.Max(1, PageSize);
                PageCount = (ListT.Count / PageSize) + (ListT.Count % PageSize > 0 ? 1 : 0);
                PageNo = Math.Min(PageCount, Math.Max(1, PageNo));

                int startnum = PageSize * (PageNo - 1);
                int endnum = (PageSize * PageNo) > ListT.Count ? ListT.Count : (PageSize * PageNo);
                for (int i = startnum; i < endnum; i++)
                {
                    newList.Add(ListT[i]);
                }
            }

            return newList;
        }

        /// <summary>
        /// 列表同步方法封装 - 列表拷贝
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ab"></param>
        /// <returns></returns>
        public static List<T> ListClone<T>(this List<T> ab)
        {
            List<T> ret = new List<T>();
            if (Monitor.TryEnter(ab, 1000))
            {
                try
                {
                    ab.ForEach(item => ret.Add(item));
                }
                finally
                {
                    Monitor.Exit(ab);
                }
            }
            return ret;
        }
        /// <summary>
        /// 列表扩展方法：线程安全的循环调用委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ab"></param>
        /// <param name="ac"></param>
        public static void ListSafeForEach<T>(this List<T> ab, Func<T, bool> ac)
        {
            List<T> recy = ab.ListClone<T>();
            foreach (T item in recy)
            {
                if (!ac(item))
                {
                    break;
                }
            }
            recy.Clear();
            recy = null;
        }
        /// <summary>
        /// 列表扩展方法：非线程安全的循环调用委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ab"></param>
        /// <param name="ac"></param>
        public static void ListForEach<T>(this List<T> ab, Func<T, bool> ac)
        {
            foreach (T item in ab)
            {
                if (!ac(item))
                {
                    break;
                }
            }
        }
        /// <summary>
        /// 列表同步方法封装 - 添加项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ab"></param>
        /// <param name="item"></param>
        public static void ListAdd<T>(this List<T> ab, T item)
        {
            if (item != null)
            {
                lock (ab)
                {
                    ab.Add(item);
                }
            }
        }
        /// <summary>
        /// 列表同步方法封装 - 删除项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ab"></param>
        /// <param name="item"></param>
        public static void ListRemove<T>(this List<T> ab, T item)
        {
            if (item != null)
            {
                lock (ab)
                {
                    ab.Remove(item);
                }
            }
        }
        public static void ListClear<T>(this List<T> ab)
        {
            lock (ab)
            {
                ab.Clear();
            }
        }
        public static int ListCount<T>(this List<T> ab)
        {
            lock (ab)
            {
                return ab.Count;
            }
        }
    }

    public static class IExtendOfCommon
    {
        /// <summary>
        /// 重复执行随机函数，组成一个指定长度的字符串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Repeat(this Random value, int min, int max, int len)
        {
            string ret = "";
            for (int i = 0; i < len; i++) {
                ret += value.Next(min, max).ToString();
            }
            return ret;
        }

        /// <summary>
        /// 将指定的JObject对象，转化为UTF8字符串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToUTF8String(this JObject value)
        {
            return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value.ToString().Replace("\r\n", "").Trim()));
        }

        public static string ToUTF8String(this JArray value)
        {
            return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value.ToString().Replace("\r\n", "").Trim()));
        }

        public static string ToUTF8String(this JToken value)
        {
            return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value.ToString().Replace("\r\n", "").Trim()));
        }

        public static JObject AddItem(this JObject value, string propertyName, JToken va) {
            value.Add(propertyName, va);
            return value;
        }

        public static JArray AddItem(this JArray value, string propertyName, JToken va) {
            value.Add((new JObject()).AddItem(propertyName, va));
            return value;
        }

        public static string ToUrl(this JObject value) {
            string ret = "";
            foreach (var item in value.Properties()) {
                if (ret == "") {
                    ret += "?";
                }
                else {
                    ret += "&";
                }
                ret += Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(item.Name.ToString())) 
                    + "=" + WebUtility.UrlEncode(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(item.Value.ToString())));
            }
            
            return ret;
        }

        /// <summary>
        /// 判断JObject对象是否拥有指定键值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool hasKey(this JObject value, string key) {
            if (value.Property(key) == null || value.Property(key).Value.ToString() == "") {
                return false;
            }
            return true;
        }

        public static string ToUTF8String(this string value)
        {
            return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(value));
        }

        public static bool Contains<T>(this T[] list, T obj)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].Equals(obj))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断b组的元素是否都在a组
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Contains(string[] a, string[] b)
        {
            for (int i = 0; i < b.Length; i++)
            {
                if (Contains<string>(a, b[i]) != true)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 对象序列化扩展方法，以rex为分割符添加以data为内容的num次后缀
        /// </summary>
        /// <param name="num">次数</param>
        /// <param name="rex">分隔符</param>
        /// <param name="data">数据</param>
        /// <returns>添加了指定后缀的字符串</returns>
        public static string AddDefineString(this string result, int num, string rex, string data)
        {
            if (num > 0)
            {
                for (int i = 0; i < num; i++)
                {
                    result += data;
                    if (i < num - 1)
                    {
                        result += rex;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 测试目标对象和原对象的域值是否完全一致
        /// 适用于进行两个结构的赋值一致性检测
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <returns></returns>
        public static bool isFieldVaulesEquals(this object src, object dst)
        {
            try
            {
                return src.Serialize() == dst.Serialize();
            }
            catch { }
            return false;
        }
    }

    /// <summary>
    /// 扩展方法库, 在既有类中透明增加新的方法
    /// </summary>
    public static class ISerializer
    {
        /// <summary>
        /// 序列化系统对象
        /// </summary>
        private static JavaScriptSerializer _Serializer = new JavaScriptSerializer(new SimpleTypeResolver());
        /// <summary>
        /// 对象深度Copy
        /// </summary>
        /// <param name="armsInfoList"></param>
        /// <returns></returns>
        public static T DeepCopy<T>(this T obj)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, obj);
            memoryStream.Position = 0;
            return (T)formatter.Deserialize(memoryStream);
        }

        /// <summary>
        /// 屏幕打印配置信息对象（有特定格式）
        /// 如需打印普通对象，该函数需要略加改写
        /// </summary>
        /// <param name="o">要打印的配置信息对象</param>
        /// <returns>返回的XML格式的字符串</returns>
        public static string TransObjToXml(this object o)
        {
            StringBuilder ret = new StringBuilder();
            WriteNode(o, ret);
            return ret.ToString();
        }

        /// <summary>
        /// 将当前对象的成员以XML的格式输出
        /// </summary>
        /// <param name="str"></param>
        private static void WriteNode(object o, StringBuilder str)
        {
            //生成标签头
            str.Append("<" + o.GetType().Name.ToString());

            //为范型对象预留字符串
            StringBuilder DicStr = new StringBuilder();

            //取出对象属性


            FieldInfo[] fInfos = o.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField);
            foreach (FieldInfo fInfo in fInfos)
            {
                Type fType = fInfo.FieldType;

                //取出值
                object val = o.GetType().InvokeMember(fInfo.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField, null, o, null);
                if (val == null)
                {
                    continue;
                }

                //判断是否是泛型
                if (fType.IsGenericType && (fType.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                {
                    int n = (int)val.GetType().InvokeMember("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty, null, val, null);
                    IEnumerator m = (IEnumerator)val.GetType().GetMethod("GetEnumerator").Invoke(val, null);
                    while (m.MoveNext())
                    {
                        object o1 = m.Current.GetType().InvokeMember("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty, null, m.Current, null);
                        WriteNode(o1, DicStr);
                    }
                }
                else
                {
                    str.Append(" " + fInfo.Name + "='");

                    if (fType == typeof(string))
                        str.Append(val.ToString());
                    else if (fType == typeof(DateTime))
                        str.Append(((DateTime)val).ToString("YYYY-MM-DD hh:mm:ss.fff"));
                    else if (fType == typeof(int))
                        str.Append(val.ToString());
                    else if (fType == typeof(byte))
                        str.Append(val.ToString());
                    else if (fType == typeof(long))
                        str.Append(val.ToString());
                    else if (fType == typeof(decimal))
                        str.Append(val.ToString());
                    else if (fType == typeof(double))
                        str.Append(val.ToString());
                    else if (fType == typeof(float))
                        str.Append(val.ToString());
                    else if (fType == typeof(bool))
                    {
                        if (val == (object)true)
                            str.Append("True");
                        else
                            str.Append("False");
                    }

                    str.Append("'");
                }
            }

            //关闭属性标签头
            str.Append(">");

            //添加子节点
            str.Append(DicStr.ToString());

            //添加标签尾
            str.Append("</" + o.GetType().Name.ToString() + ">");
        }

        /// <summary>
        /// 供TransXmlNodeToObject调用的可递归函数
        /// </summary>
        /// <param name="node">XML节点</param>
        /// <param name="objType">要转化的对象的类型</param>
        /// <returns>转化完毕的对象</returns>
        private static Object TransNodeToObject(XmlNode node, Type objType)
        {
            Object obj = Activator.CreateInstance(objType);

            if (obj == null) return null;

            //将节点的直接属性，转化为对象的成员
            for (int i = 0; i < node.Attributes.Count; i++)
            {
                FieldInfo fInfo = objType.GetField(node.Attributes[i].Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField);
                if (fInfo == null)
                {
                    //没有对应的名称，忽略
                    continue;
                }

                Type fType = fInfo.FieldType;

                object val = null;
                //对简单类型的值进行转换处理


                if (fType == typeof(string))
                {
                    val = node.Attributes[i].InnerText;
                }
                else if (fType == typeof(DateTime))
                {
                    val = Convert.ToDateTime(node.Attributes[i].InnerText);
                }
                else if (fType == typeof(int))
                {
                    string iText = node.Attributes[i].InnerText;
                    if (iText.Length > 0)
                    {
                        val = Convert.ToInt32(iText);
                    }
                    else
                    {
                        val = 0;
                    }
                }
                else if (fType == typeof(long))
                {
                    val = Convert.ToInt64(node.Attributes[i].InnerText);
                }
                else if (fType == typeof(decimal))
                {
                    val = Convert.ToDecimal(node.Attributes[i].InnerText);
                }
                else if (fType == typeof(double))
                {
                    val = Convert.ToDouble(node.Attributes[i].InnerText);
                }
                else if (fType == typeof(float))
                {
                    val = (float)Convert.ToDouble(node.Attributes[i].InnerText);
                }
                else if (fType == typeof(bool))
                {
                    val = Convert.ToBoolean(node.Attributes[i].InnerText);
                }
                else if (fType == typeof(byte))
                {
                    val = Convert.ToByte(node.Attributes[i].InnerText);
                }

                if (val != null)
                {
                    objType.InvokeMember(node.Attributes[i].Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField, null, obj, new object[] { val });
                }
            }

            //将节点的子节点，转化为对象的范型成员的多个条目
            if (node.ChildNodes.Count > 0)
            {
                //取第一个子节点的Name属性，取对象中对应的范型成员


                Object Dic = objType.InvokeMember(node.ChildNodes[0].Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField, null, obj, null);
                if (Dic != null)
                {
                    Type ft = Dic.GetType();
                    if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        //成功取得范型成员
                        Type Dic_Key = ft.GetGenericArguments()[0];
                        Type Dic_Value = ft.GetGenericArguments()[1];

                        for (int i = 0; i < node.ChildNodes.Count; i++)
                        {
                            //对所有子节点进行遍历
                            if (node.ChildNodes[i].Attributes.Count > 0)
                            {
                                //首先获取Dictionary的Key字段的值（强制将子节点的第一个属性值作为Key），支持整形或字符串型
                                //可支持多个不同子节点用不同的Key zcl 14 8 20

                                Object Dic2 = objType.InvokeMember(node.ChildNodes[i].Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField, null, obj, null);
                                Type ft2 = Dic2.GetType();
                                if (ft2.IsGenericType && ft2.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                                {
                                    Dic_Key = ft2.GetGenericArguments()[0];
                                    Dic_Value = ft2.GetGenericArguments()[1];
                                    if (node.ChildNodes[0].Name != node.ChildNodes[i].Name)
                                    {
                                        Dic = Dic2;
                                        ft = ft2;
                                    }
                                }

                                Object _Key = null;

                                if (Dic_Key == typeof(int))
                                    _Key = Convert.ToInt32(node.ChildNodes[i].Attributes[0].InnerText);
                                else if (Dic_Key == typeof(long))
                                    _Key = Convert.ToInt64(node.ChildNodes[i].Attributes[0].InnerText);
                                else if (Dic_Key == typeof(decimal))
                                    _Key = Convert.ToDecimal(node.ChildNodes[i].Attributes[0].InnerText);
                                else if (Dic_Key == typeof(double))
                                    _Key = Convert.ToDouble(node.ChildNodes[i].Attributes[0].InnerText);
                                else if (Dic_Key == typeof(string))
                                    _Key = node.ChildNodes[i].Attributes[0].InnerText;

                                if (_Key != null)
                                {
                                    //获取Dictionary的Value字段的值
                                    Object _Value = TransNodeToObject(node.ChildNodes[i], Dic_Value);

                                    ft.InvokeMember("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod, null, Dic, new object[] { _Key, _Value });
                                }
                            }
                        }
                    }
                }
            }

            return obj;
        }

        /// <summary>
        /// 将携带配置信息的XML节点转化为由范型指定的配置信息对象
        /// </summary>
        /// <typeparam name="T">范型类型</typeparam>
        /// <param name="node">待转化的XML节点</param>
        /// <returns>转化完毕的携带配置信息的对象</returns>
        public static T TransXmlNodeToObject<T>(this XmlNode node)
        {
            Object ret = TransNodeToObject(node, typeof(T));
            if (ret == null)
            {
                return default(T);
            }
            return (T)ret;
        }

        /// <summary>
        /// 拷贝指定对象的属性到自身
        /// </summary>
        /// <param name="dest">目标对象</param>
        /// <param name="src">原对象</param>
        private static void CopyProperties<T>(this T dest, T src)
        {
            FieldInfo[] FieldInfoArray = typeof(T).GetFields();

            for (int j = 0; j < FieldInfoArray.Length; j++)
            {
                FieldInfoArray[j].SetValue(dest, FieldInfoArray[j].GetValue(src));
            }
        }

        /// <summary>
        /// 从调用对象复制一个新对象，拷贝其全部公开域数值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ori"></param>
        /// <returns></returns>
        public static T CloneObject<T>(this T ori)
            where T : new()
        {
            T ret = (T)Activator.CreateInstance(typeof(T));
            ret.CopyProperties(ori);
            return ret;
        }


        public static string JsonWrite(this object o)
        {
            DataContractJsonSerializer dcjs = new DataContractJsonSerializer(o.GetType());

            MemoryStream ms = new MemoryStream();
            dcjs.WriteObject(ms, o);
            ms.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static T JsonRead<T>(this string s)
            where T : new()
        {
            try
            {
                if (String.IsNullOrEmpty(s))
                {
                    return new T();
                }
                else
                {
                    MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(s));
                    DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(T));
                    return (T)dcjs.ReadObject(ms);
                }
            }
            catch
            {
                return new T();
            }
        }



        /// <summary>
        /// 对象序列化扩展方法，任意对象均可直接调用
        /// 2011-4-30 Liub
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string Serialize(this object o)
        {
            try
            {
                string s1 = _Serializer.Serialize(o);

                const string f1 = "\"__type\"";
                const string f2 = "PublicKeyToken=null\",";
                int p0 = 0;

                StringBuilder sb = new StringBuilder();
                int p1 = s1.IndexOf(f1, p0);
                while (p1 != -1)
                {
                    //将type之前的内容附加到s2上


                    sb.Append(s1.Substring(p0, p1 - p0));
                    //查找f2
                    int p2 = s1.IndexOf(f2, p0);
                    //位置移动到f2之后
                    p0 = p2 + f2.Length;

                    //查找下一个


                    p1 = s1.IndexOf(f1, p0);
                }
                sb.Append(s1.Substring(p0));

                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }
        /// <summary>
        /// 对象反序列化扩展方法，任意字符串均可直接调用
        /// 2011-4-30 Liub
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public static T Unserialize<T>(this object s)
            where T : new()
        {
            try
            {
                if ((s == null) || (s.ToString().Trim() == ""))
                    return new T();
                else
                    return _Serializer.Deserialize<T>(s.ToString());
            }
            catch
            {
                return new T();
            }
        }
    }

    public class Logger
    {
        /// <summary>
        /// 表示处理特定事件的方法，该事件由应用程序域不处理的异常引发
        /// </summary>
        /// <param name="sender">未处理的异常事件的源</param>
        /// <param name="e">包含事件数据的 UnhandledExceptionEventArgs</param>
        public static void MyUnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error("UnhandledException", (Exception)e.ExceptionObject);
        }

        public static void Error(string msg, Exception e)
        {
            if (e != null)
            {
                LogInfoManager.WriteLog(LogInfoManager.LogFile.Error, msg + ":" + e.ToString());
            }
            else
            {
                LogInfoManager.WriteLog(LogInfoManager.LogFile.Error, msg);
            }
        }

        public static void Error(string msg)
        {
            LogInfoManager.WriteLog(LogInfoManager.LogFile.Error, msg);
        }

        public static void Info(string msg)
        {
            LogInfoManager.WriteLog(LogInfoManager.LogFile.Trace, msg);
        }

        public static void Error(Exception e)
        {
            LogInfoManager.WriteLog(LogInfoManager.LogFile.Error, e.ToString());
        }

        public static void Buff(string msg)
        {
            LogInfoManager.WriteLog(LogInfoManager.LogFile.Buff, msg);
        }

    }

    /// <summary>
    /// 线程安全的ID管理器
    /// </summary>
    public class IdentityManager : IdentityManagerNoLock
    {
        /// <summary>
        /// 私有锁
        /// </summary>
        private object _lockObj = new object();
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_StartValue">起始值</param>
        public IdentityManager(int _StartValue) : base(_StartValue) { }
        public IdentityManager(int _StartValue, int _EndValue) : base(_StartValue, _EndValue) { }
        /// <summary>
        /// 获取并重新计算当前值(线程安全)
        /// </summary>
        public override int CurrentIdentity
        {
            get
            {
                lock (_lockObj)
                {
                    return base.CurrentIdentity;
                }
            }
        }

        /// <summary>
        /// 获取当前值但不重新计算(线程安全)
        /// </summary>
        public override int CurrentNum
        {
            get
            {
                lock (_lockObj)
                {
                    return base.CurrentNum;
                }
            }
        }

        public override void Set(int _value)
        {
            lock (_lockObj)
            {
                base.Set(_value);
            }
        }

        public override void Reset()
        {
            lock (_lockObj)
            {
                base.Reset();
            }
        }
    }

    /// <summary>
    /// 线程安全的ID管理器
    /// </summary>
    public class IdentityManagerNoLock
    {
        /// <summary>
        /// 起始值设定
        /// </summary>
        private int StartValue = 0;
        /// <summary>
        /// 最大值设定
        /// </summary>
        private int EndValue = Int32.MaxValue;
        /// <summary>
        /// 当前值
        /// </summary>
        private int CurrentValue = 0;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_StartValue">起始值</param>
        public IdentityManagerNoLock(int _StartValue)
        {
            StartValue = _StartValue;
            CurrentValue = _StartValue;
        }
        public IdentityManagerNoLock(int _StartValue, int _EndValue)
        {
            StartValue = _StartValue;
            CurrentValue = _StartValue;
            EndValue = _EndValue;
        }

        /// <summary>
        /// 获取并重新计算当前值(无锁)
        /// </summary>
        public virtual int CurrentIdentity
        {
            get
            {
                if (CurrentValue >= EndValue)
                {
                    CurrentValue = StartValue;
                }
                return CurrentValue++;
            }
        }

        /// <summary>
        /// 获取但并不重新计算当前值(无锁)
        /// </summary>
        public virtual int CurrentNum
        {
            get
            {
                if (CurrentValue >= EndValue)
                {
                    CurrentValue = StartValue;
                }
                return CurrentValue;
            }
        }

        public virtual void Set(int _value)
        {
            CurrentValue = Math.Min(_value, EndValue);
        }

        public virtual void Reset()
        {
            CurrentValue = StartValue;
        }
    }

    /// <summary>
    /// 刷新时间管理器
    /// </summary>
    public class RefreshTimeManager
    {
        public DateTime refreshTime = DateTime.Now;
        private object Locker = new object();
        private int delay = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_delay">延迟刷新时间(秒)</param>
        public RefreshTimeManager(int _delay)
        {
            delay = Math.Max(1, Math.Abs(_delay));
            Instant();
        }
        /// <summary>
        /// 判断超时计数是否已经到点 线程安全
        /// </summary>
        public bool RequestRefresh
        {
            get
            {
                if (Monitor.TryEnter(Locker, 300))
                {
                    try
                    {
                        if (refreshTime.AddSeconds(delay) < DateTime.Now)
                        {
                            refreshTime = DateTime.Now;
                            return true;
                        }
                    }
                    finally { Monitor.Exit(Locker); }
                }
                return false;
            }
        }

        /// <summary>
        /// 判断超时计数是否已经到点 非线程安全
        /// </summary>
        public bool RequestRefreshNoLock
        {
            get
            {
                if (refreshTime.AddSeconds(delay) < DateTime.Now)
                {
                    refreshTime = DateTime.Now;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 设置为立即超时
        /// </summary>
        public void Instant()
        {
            refreshTime = DateTime.Now.AddSeconds(-2 * delay);
        }
        /// <summary>
        /// 设置为重新倒计时
        /// </summary>
        public void Reset()
        {
            refreshTime = DateTime.Now;
        }

        /// <summary>
        /// 查询距下次刷新的时间（秒）
        /// </summary>
        public int LeftTimeOfSecond
        {
            get
            {
                return (delay - (int)((TimeSpan)(DateTime.Now - refreshTime)).TotalSeconds);
            }
        }
    }

    /// <summary>
    /// baseFunc 的摘要说明。
    /// </summary>
    public static class baseFunc
    {
        public static Random rand = new Random();

        public static string md5(string str)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] res = md5.ComputeHash(Encoding.Default.GetBytes(str), 0, str.Length);
            char[] temp = new char[res.Length];
            System.Array.Copy(res, temp, res.Length);
            return new String(temp);
        }

        public static string md5(byte[] buf) {
            using (MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider())
            {
                byte[] data = md5Hasher.ComputeHash(buf);
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString().ToLower();
            }
        }

        public static ArrayList RandomSortList(this ArrayList ListT, int count)
        {
            Random random = new Random();
            ArrayList newList = new ArrayList();
            if (count <= 0)
            {
                foreach (object item in ListT)
                {
                    newList.Insert(random.Next(newList.Count), item);
                }
            }
            else
            {
                int i = 0;
                foreach (object item in ListT)
                {
                    newList.Insert(random.Next(newList.Count), item);
                    i++;
                    if (i >= count)
                        break;
                }
            }
            return newList;
        }

        public static string ListToString(this List<int> ar)
        {
            string result = "";
            if (ar.Count > 0)
            {
                ar.ForEach(item =>
                {
                    if (string.IsNullOrEmpty(result))
                        result += item.ToString();
                    else
                        result += "," + item.ToString();
                });
            }
            return result;
        }

        /// <summary>
        /// 延迟执行委托(强烈建议改用eventManager.DelayDelegate)
        /// </summary>
        /// <param name="ac">待执行的委托函数</param>
        /// <param name="iv">延迟执行的秒数</param>
        public static void DelayDelegate(System.Action ac, double iv)
        {
            System.Timers.Timer closeOperation = new System.Timers.Timer();
            closeOperation.Interval = iv * 1000;
            closeOperation.Elapsed += new System.Timers.ElapsedEventHandler(delegate(object s, System.Timers.ElapsedEventArgs e) { ac(); });
            closeOperation.AutoReset = false;
            closeOperation.Start();
        }

        /// <summary>
        /// 将不合法字符去除（SQL）


        /// </summary>
        /// <param name="Temp"></param>
        /// <returns></returns>
        public static string ReplaceDangerCode(string Temp)
        {
            if (String.IsNullOrEmpty(Temp))
            {
                return "";
            }
            Temp = Temp.Replace("'", "");
            Temp = Temp.Replace("’", "");
            Temp = Temp.Replace("“”", "");
            Temp = Temp.Replace("=", "");
            Temp = Temp.Replace("\"", "");
            Temp = Temp.Replace("&", "");
            Temp = Temp.Replace("*", "");
            Temp = Temp.Replace(" select ", "");
            Temp = Temp.Replace(" insert ", "");
            Temp = Temp.Replace(" delete ", "");
            Temp = Temp.Replace(" count(", "");
            Temp = Temp.Replace("drop table ", "");
            Temp = Temp.Replace(" update ", "");
            Temp = Temp.Replace(" truncate ", "");
            Temp = Temp.Replace(" asc(", "");
            Temp = Temp.Replace(" mid(", "");
            Temp = Temp.Replace(" char(", "");
            Temp = Temp.Replace(" xp_cmdshell", "");
            Temp = Temp.Replace(" exec master", "");
            Temp = Temp.Replace("net localgroup administrators", "");
            Temp = Temp.Replace(" and ", "");
            Temp = Temp.Replace("net user", "");
            Temp = Temp.Replace(" or ", "");
            Temp = Temp.Replace(" @", "");
            Temp = Temp.Replace(" in ", "");
            Temp = Temp.Replace(" between ", "");
            Temp = Temp.Replace("char(13)", "");
            return Temp.Trim();
        }

        public static string ReplaceCode(string Temp)
        {
            if (String.IsNullOrEmpty(Temp))
            {
                return "";
            }
            Temp = Temp.Replace("'", "`").Trim();
            return Temp;
        }

        /// <summary>
        /// 获取远程服务器ATN结果
        /// </summary>
        /// <param name="a_strUrl"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static String Get_Http(String a_strUrl, int timeout)
        {
            string strResult;
            try
            {
                HttpWebRequest myReq = (HttpWebRequest)HttpWebRequest.Create(a_strUrl);
                myReq.Timeout = timeout;
                HttpWebResponse HttpWResp = (HttpWebResponse)myReq.GetResponse();
                Stream myStream = HttpWResp.GetResponseStream();
                StreamReader sr = new StreamReader(myStream, Encoding.Default);
                StringBuilder strBuilder = new StringBuilder();
                while (-1 != sr.Peek())
                {
                    strBuilder.Append(sr.ReadLine());
                }

                strResult = strBuilder.ToString();
            }
            catch (Exception exp)
            {
                strResult = "错误：" + exp.Message;
            }

            return strResult;
        }

        /// <summary>
        /// 概率测算函数
        /// </summary>
        /// <param name="p">介乎0-1之间的浮点数</param>
        /// <returns>按照p指定的概率进行判断，返回布尔值</returns>
        public static bool TestRandom(double p)
        {
            if (p <= 0) { return false; }
            if (p >= 1) { return true; }

            return rand.Next(0, 101) <= (int)Math.Min(100, p * 100);
        }


        public static bool isInt32(string s)
        {
            Regex re = new Regex(@"^(\-|\+|)([\d]*)$");
            if ((s != null) && (s != "") && (re.IsMatch(s)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool isMail(string s)
        {
            if (s == null || s == "")
            {
                return false;
            }
            else
            {
                Regex re = new Regex(@"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
                return re.IsMatch(s);
            }
        }

        private static IdentityManager RandomParam = new IdentityManager(0, 5);
        /// <summary>
        /// 概率测算函数
        /// </summary>
        /// <param name="p">介乎0-1之间的浮点数, 表示 100*p %的概率</param>
        /// <returns>按照p指定的概率进行判断，返回布尔值</returns>
        public static bool RandomTest(double p)
        {
            return new Random((int)DateTime.Now.Ticks + RandomParam.CurrentIdentity * 1000).Next(0, 101) <= Math.Min(100, p * 100);
        }
        /// <summary>
        /// 随机取0到MaxValue-1间的任意整数值
        /// </summary>
        /// <param name="MaxValue"></param>
        /// <returns></returns>
        public static int RandomLength(this int MaxValue)
        {
            return new Random((int)DateTime.Now.Ticks + RandomParam.CurrentIdentity * 1000).Next(0, MaxValue);
        }

        /// <summary>
        /// 擴展方法，從數組中獲取隨機對象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="va"></param>
        /// <returns></returns>
        public static T RandObject<T>(this T[] va)
        {
            if (va.Length > 0)
            {
                return va[va.Length.RandomLength()];
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// 擴展方法，將字符串轉換為整形
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int ToInt32(this string s)
        {
            int ret = 0;
            Int32.TryParse(s.Trim(), out ret);
            return ret;
        }

        public static bool ToBool(this string s)
        {
            bool ret = false;
            bool.TryParse(s.Trim(), out ret);
            return ret;
        }

        public static Double ToDouble(this string s)
        {
            double ret = 0;
            Double.TryParse(s.Trim(), out ret);
            return ret;
        }

        public static byte ToByte(this string s)
        {
            byte ret = 0;
            Byte.TryParse(s, out ret);
            return ret;
        }

        public static string TimeDiff(int seconds1)
        {
            if (seconds1 > 0)
            {
                TimeSpan ts = new TimeSpan(0, 0, seconds1);
                return (((ts.Days * 24) + ts.Hours) + ":" + ts.Minutes.ToString("d2") + ":" + ts.Seconds.ToString("d2"));
            }
            else
            {
                return ("0:00:0?");
            }
        }

        public static string TimeDiff(DateTime d1)
        {
            return TimeDiff(d1, DateTime.Now);
        }

        public static string TimeDiff(DateTime d1, DateTime d2)
        {
            TimeSpan ts = d1 - d2;
            return TimeDiff(ts);
        }

        public static TimeSpan TimeDiff1(DateTime d1)
        {
            TimeSpan ts = DateTime.Now - d1;
            return ts;
        }

        public static string TimeDiff(TimeSpan ts)
        {
            if (ts.TotalSeconds > 0)
            {
                return (((ts.Days * 24) + ts.Hours) + ":" + ts.Minutes.ToString("d2") + ":" + ts.Seconds.ToString("d2"));
            }
            else
            {
                return ("0:00:0?");
            }
        }

        /// <summary>
        /// 把北京时间转成当前格林威治时间的时间戳  毫秒
        /// </summary>
        /// <param name="dt">当前本地时间</param>
        /// <returns>格林威治时间的时间戳</returns>
        public static long SecondsTimeStamp(DateTime dt)
        {
            DateTime d1 = new DateTime(1970, 1, 1);
            DateTime d2 = dt.ToUniversalTime();
            TimeSpan ts = new TimeSpan(d2.Ticks - d1.Ticks);
            return (long)ts.TotalMilliseconds;
        }

        /// <summary>
        /// 把格林威治时间的时间戳转成北京时间
        /// </summary>
        /// <param name="Ticks">格林威治时间的时间戳</param>
        /// <returns>北京时间</returns>
        public static DateTime getGMTTime(long Ticks)
        {
            DateTime d1 = new DateTime(1970, 1, 1).AddSeconds(Ticks);
            return d1;
        }
    }
}
