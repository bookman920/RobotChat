using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GameSystem.BaseFunc
{
    /// <summary>
    /// 安全的缓存管理器
    /// </summary>
    public class CacheManager
    {
        /// <summary>
        /// 私有的存储队列
        /// </summary>
        private DeliverList<AutoExecuter> List = new DeliverList<AutoExecuter>();

        /// <summary>
        /// 添加新的缓存对象
        /// </summary>
        /// <param name="data"></param>
        public void Push(AutoExecuter data)
        {
            List.Push(data);
        }

        private AutoExecuter cacheObj = null;

        private bool GameOver = false;

        public CacheManager()
        {
            new Thread(Execute).Start();
        }

        public void Stop()
        {
            GameOver = true;

            int recy = 0;
            while (List.Count > 0 && recy++ < 100)
            {
                //最大等待10秒，处理剩余队列数据，超时或队列处理完成立即退出                Thread.Sleep(100);
            }
            List.Release();
        }

        private void Execute()
        {
            while (!GameOver)
            {
                try
                {
                    if (cacheObj == null)
                    {
                        cacheObj = List.Remove();
                    }

                    if (cacheObj != null && cacheObj.Execute())
                    {
                        cacheObj = null;
                    }
                    else
                    {
                        Thread.Sleep(300);
                    }
                }
                catch
                {
                    Thread.Sleep(300);
                }
            }
        }
    }

    /// <summary>
    /// 自动执行基类
    /// </summary>
    public class AutoExecuter
    {
        public virtual bool Execute() { return true; }
    }

    /// <summary>
    /// 延迟类执行事件    /// </summary>
    public class DelayDelegate : AutoExecuter
    {
        /// <summary>
        /// 执行时间
        /// </summary>
        public DateTime EndTime = DateTime.Now;
        /// <summary>
        /// 委托
        /// </summary>
        public Action action;

        /// <summary>
        /// 事务排序算法
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static int CompareByEndTime(DelayDelegate a, DelayDelegate b)
        {
            if (a.EndTime > b.EndTime)
            {
                return 1;
            }
            else if (a.EndTime < b.EndTime)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// 延迟时间到点后需要执行的委托
        /// </summary>
        /// <param name="_action">委托方法</param>
        /// <param name="time">延迟时间(秒)</param>
        public DelayDelegate(Action _action, double time)
        {
            this.action = _action;
            this.EndTime = DateTime.Now.AddSeconds(time);
        }

        public override bool Execute()
        {
            try
            {
                action();
            }
            catch { }
            return true;
        }
    }
}
