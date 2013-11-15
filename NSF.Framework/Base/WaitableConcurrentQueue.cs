using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NSF.Framework.Base
{
    /// <summary>
    /// 可等待的一部对象。
    /// </summary>
    /// <typeparam name="T">数据类型的模板参数。</typeparam>
    public class WaitableConcurrentQueue<T>
    {
        /// <summary>
        /// 数据队列。
        /// </summary>
        ConcurrentQueue<T> _ItemQueue = new ConcurrentQueue<T>();
        /// <summary>
        /// 等待队列。
        /// </summary>
        ConcurrentQueue<TaskCompletionSource<T>> _WaitQueue = new ConcurrentQueue<TaskCompletionSource<T>>();
        /// <summary>
        /// 推入队列。
        /// </summary>
        /// <param name="item">数据项。</param>
        public void Enqueue(T item)
        {
            TaskCompletionSource<T> tcs;
            if (_WaitQueue.TryDequeue(out tcs))
            {
                /// 由于SetResult的奇葩线程模型
                /// 这里新起一个Task
                Task.Run(() => tcs.SetResult(item));
            }
            else
            {
                _ItemQueue.Enqueue(item);
            }
        }
        /// <summary>
        /// 弹出队列。
        /// </summary>
        /// <returns>数据项或者的可等待的任务。</returns>
        public Task<T> Dequeue()
        {
            T item;
            if (_ItemQueue.TryDequeue(out item))
            {
                return Task.FromResult(item);
            }
            else
            {
                TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
                _WaitQueue.Enqueue(tcs);
                return tcs.Task;
            }
        }
    }
}
