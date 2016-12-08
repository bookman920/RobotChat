using System.Collections;
using System.Threading;

namespace GameSystem.BaseFunc
{
    /// <summary>
    /// 定义了可异步耦合的队列，用于在前后台线程间交换报文
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DeliverList<T>
    {
        private Queue deliverList = new Queue(10000);

        /// <summary>
        /// 向 Deliver 队列加入数据
        /// </summary>
        /// <param name="obj"></param>
        public void Push(T obj)
        {
            lock (deliverList)
            {
                deliverList.Enqueue(obj);
                Monitor.PulseAll(deliverList);
            }
        }

        /// <summary>
        /// 释放队列
        /// </summary>
        public void Release()
        {
            lock (deliverList) {
                deliverList.Clear();
                Monitor.PulseAll(deliverList);
            }
        }

        /// <summary>
        /// 取出 Deliver 队列中第一条数据, 注意该函数为阻塞模式（如果没有数据则挂起，当前线程进入休眠直到有新数据进入）
        /// 如果其他线程调用了Monitor.PulseAll或Monitor.Pulse（例如调用了Release方法），那么本函数可能在队列没有数据的情况下返回空对象
        /// </summary>
        /// <returns></returns>
        public T Remove()
        {
            lock (deliverList)
            {
                if (deliverList.Count == 0)
                {
                    Monitor.Wait(deliverList);
                }
                if (deliverList.Count > 0)
                {
                    return (T)deliverList.Dequeue();
                }
                else { return default(T); }
            }
        }

        /// <summary>
        /// 队列中数据条数
        /// </summary>
        public int Count
        {
            get {
                lock (deliverList) {
                    return deliverList.Count;
                }
            }
        }
    }
}
