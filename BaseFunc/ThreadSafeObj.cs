using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GameSystem.BaseFunc
{
    /// <summary>
    /// 线程安全操作基类
    /// </summary>
    public class ThreadSafeObj
    {
        /// <summary>
        /// 对象级别同步锁
        /// </summary>
        protected ReaderWriterLock _RWLockObject = new ReaderWriterLock();
        /// <summary>
        /// 对象级别线程安全写操作，有返回
        /// </summary>
        /// <param name="ac"></param>
        /// <returns></returns>
        public T threadSafeExecuteReturnValue<T>(Func<T> ac)
        {
            if (_RWLockObject.IsReaderLockHeld)
            {
                LockCookie lc = _RWLockObject.UpgradeToWriterLock(-1);
                try
                {
                    return ac();
                }
                finally
                {
                    _RWLockObject.DowngradeFromWriterLock(ref lc);
                }
            }
            else if (!_RWLockObject.IsWriterLockHeld)
            {
                _RWLockObject.AcquireWriterLock(-1);
                try
                {
                    return ac();
                }
                finally
                {
                    _RWLockObject.ReleaseWriterLock();
                }
            }
            else
            {
                return ac();
            }
        }
        /// <summary>
        /// 对象级别线程安全写操作，无返回
        /// </summary>
        /// <param name="ac"></param>
        public void threadSafeExecute(Action ac)
        {
            if (_RWLockObject.IsReaderLockHeld)
            {
                LockCookie lc = _RWLockObject.UpgradeToWriterLock(-1);
                try
                {
                    ac();
                }
                finally
                {
                    _RWLockObject.DowngradeFromWriterLock(ref lc);
                }
            }
            else if (!_RWLockObject.IsWriterLockHeld)
            {
                _RWLockObject.AcquireWriterLock(-1);
                try
                {
                    ac();
                }
                finally
                {
                    _RWLockObject.ReleaseWriterLock();
                }
            }
            else
            {
                ac();
            }
        }
        /// <summary>
        /// 对象级别线程安全读操作，有返回
        /// </summary>
        /// <param name="ac"></param>
        /// <returns></returns>
        public T threadReadReturnValue<T>(Func<T> ac)
        {
            if (!_RWLockObject.IsReaderLockHeld && !_RWLockObject.IsWriterLockHeld)
            {
                _RWLockObject.AcquireReaderLock(-1);
                try
                {
                    return ac();
                }
                finally
                {
                    _RWLockObject.ReleaseReaderLock();
                }
            }
            else
            {
                return ac();
            }
        }
        /// <summary>
        /// 对象级别线程安全读操作，无返回
        /// </summary>
        /// <param name="ac"></param>
        public void threadRead(Action ac)
        {
            if (!_RWLockObject.IsReaderLockHeld && !_RWLockObject.IsWriterLockHeld)
            {
                _RWLockObject.AcquireReaderLock(-1);
                try
                {
                    ac();
                }
                finally
                {
                    _RWLockObject.ReleaseReaderLock();
                }
            }
            else
            {
                ac();
            }
        }
        /// <summary>
        /// 按名称检索全局级别同步锁
        /// </summary>
        protected static ReaderWriterLock GetRWLockGlobal(string className)
        {
            if (String.IsNullOrEmpty(className))
            {
                return _RWLockGlobalList["DefaultLocker"];
            }
            if (!_RWLockGlobalList.ContainsKey(className))
            {
                _RWLockGlobalList.Add(className, new ReaderWriterLock());
            }
            return _RWLockGlobalList[className];
        }
     
        /// <summary>
        ///全局级别同步锁字典
        /// </summary>
        protected static Dictionary<string, ReaderWriterLock> _RWLockGlobalList = new Dictionary<string, ReaderWriterLock>()
        {
            {"DefaultLocker",new ReaderWriterLock()}
        };

        /// <summary>
        /// 全局级别线程安全读操作，有返回
        /// </summary>
        /// <param name="ac"></param>
        /// <returns></returns>
        public static T threadStaticReadReturnValue<T>(string className, Func<T> ac)
        {
            ReaderWriterLock _RWLockGlobal = GetRWLockGlobal(className);
            if (!_RWLockGlobal.IsReaderLockHeld && !_RWLockGlobal.IsWriterLockHeld)
            {
                _RWLockGlobal.AcquireReaderLock(-1);
                try
                {
                    return ac();
                }
                finally
                {
                    _RWLockGlobal.ReleaseReaderLock();
                }
            }
            else
            {
                return ac();
            }
        }

        /// <summary>
        /// 全局级别线程安全读操作，无返回
        /// </summary>
        /// <param name="ac"></param>
        public static void threadStaticRead(string className, Action ac)
        {
            ReaderWriterLock _RWLockGlobal = GetRWLockGlobal(className);
            if (!_RWLockGlobal.IsReaderLockHeld && !_RWLockGlobal.IsWriterLockHeld)
            {
                _RWLockGlobal.AcquireReaderLock(-1);
                try
                {
                    ac();
                }
                finally
                {
                    _RWLockGlobal.ReleaseReaderLock();
                }
            }
            else
            {
                ac();
            }
        }

        /// <summary>
        /// 全局级别线程安全写操作，有返回
        /// </summary>
        /// <param name="ac"></param>
        /// <returns></returns>
        public static T threadStaticSafeExecuteReturnValue<T>(string className, Func<T> ac)
        {
            ReaderWriterLock _RWLockGlobal = GetRWLockGlobal(className);
            if (_RWLockGlobal.IsReaderLockHeld)
            {
                LockCookie lc = _RWLockGlobal.UpgradeToWriterLock(-1);
                try
                {
                    return ac();
                }
                finally
                {
                    _RWLockGlobal.DowngradeFromWriterLock(ref lc);
                }
            }
            else if (!_RWLockGlobal.IsWriterLockHeld)
            {
                _RWLockGlobal.AcquireWriterLock(-1);
                try
                {
                    return ac();
                }
                finally
                {
                    _RWLockGlobal.ReleaseWriterLock();
                }
            }
            else
            {
                return ac();
            }
        }

        /// <summary>
        /// 全局级别线程安全写操作，无返回
        /// </summary>
        /// <param name="ac"></param>
        public static void threadStaticSafeExecute(string className, Action ac)
        {
            ReaderWriterLock _RWLockGlobal = GetRWLockGlobal(className);
            if (_RWLockGlobal.IsReaderLockHeld)
            {
                LockCookie lc = _RWLockGlobal.UpgradeToWriterLock(-1);
                try
                {
                    ac();
                }
                finally
                {
                    _RWLockGlobal.DowngradeFromWriterLock(ref lc);
                }
            }
            else if (!_RWLockGlobal.IsWriterLockHeld)
            {
                _RWLockGlobal.AcquireWriterLock(-1);
                try
                {
                    ac();
                }
                finally
                {
                    _RWLockGlobal.ReleaseWriterLock();
                }
            }
            else
            {
                ac();
            }
        }
    }
}
