using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NSF.Framework.Base
{
    /// <summary>
    /// 并发多任务执行器。
    /// </summary>
    public class ConcurrentTaskAtor
    {
        /// <summary>
        /// 某项执行任务的包装。
        /// </summary>
        class Actor
        {
            /// <summary>
            /// 底层等待执行的任务对象。
            /// </summary>
            public Task Task { get; private set; }
            /// <summary>
            /// 执行任务时附带的参数对象。
            /// </summary>
            public Object Object { get; private set; }
            /// <summary>
            /// 执行的任务完成时回调对象。
            /// </summary>
            public Func<Task, Object, Task> Callback { get; private set; }
            /// <summary>
            /// 默认构造函数。
            /// </summary>
            /// <param name="t">需要执行的任务。</param>
            /// <param name="o">附带的参数对象。</param>
            /// <param name="cb">任务完成时回调对象。</param>
            public Actor(Task t, Object o, Func<Task, Object, Task> cb)
            {
                Task = t;
                Object = o;
                Callback = cb;
            }
            /// <summary>
            /// 重置环境。
            /// </summary>
            /// <param name="t">需要执行的任务。</param>
            /// <param name="o">附带的参数对象。</param>
            /// <param name="cb">任务完成时回调对象。</param>
            public void Set(Task t, Object o, Func<Task, Object, Task> cb)
            {
                Task = t;
                Object = o;
                Callback = cb;
            }
        }

        /// <summary>
        /// 任务容器。
        /// </summary>
        WaitableConcurrentQueue<Actor> _Jobs;
        /// <summary>
        /// 任务取消对象。
        /// </summary>
        CancellationTokenSource _CancelTokenSource;
        /// <summary>
        /// 默认构造。
        /// </summary>
        public ConcurrentTaskAtor()
        {
            _Jobs = new WaitableConcurrentQueue<Actor>();
            _CancelTokenSource = new CancellationTokenSource();
            Task.Run(async () => await Proc(), _CancelTokenSource.Token);
        }
        /// <summary>
        /// 默认协构。
        /// </summary>
        ~ConcurrentTaskAtor()
        {
            _CancelTokenSource.Cancel();
        }
        /// <summary>
        /// 添加需执行的任务。
        /// </summary>
        /// <param name="t">需要执行的任务。</param>
        /// <param name="o">附带的参数对象。</param>
        /// <param name="cb">任务完成时回调对象。</param>
        public void Put(Task t, Object o, Func<Task, Object, Task> cb)
        {
            Actor act = new Actor(t, o, cb);
            _Jobs.Enqueue(act);
        }

        /// <summary>
        /// 执行器执行循环发生异常时回调函数。
        /// </summary>
        protected virtual Task OnException(Exception e)
        {
            return Task.FromResult(0);
        }
        /// <summary>
        /// 执行器的驱动函数。
        /// </summary>
        /// <returns></returns>
        private async Task Proc()
        {
            /// 循环逻辑异常的异常数据。
            Exception E = null;
            /// 当前的执行队列。
            List<Actor> jobWait = new List<Actor>();
            /// 创建一个新任务通知
            Task<Actor> jobNtf = _Jobs.Dequeue();
            try
            {
                while (true)
                {
                    /// 等待任意一个任务完成
                    List<Task> jobDisp = jobWait.Select(x => x.Task).ToList();
                    jobDisp.Add(jobNtf);
                    Task finishTask = await Task.WhenAny(jobDisp);
                    /// 有任务完成
                    if (finishTask != jobNtf)
                    {
                        /// 关联完成的任务对象
                        Actor job = jobWait.Find(x => x.Task == finishTask);
                        /// 不是本地回调则创建分发任务完成实际回调
                        if (job.Callback != null)
                        {
                            Put(job.Callback(finishTask, job.Object), null, null);
                        }
                        /// 移除正在回调的任务
                        jobWait.Remove(job);
                    }
                    /// 有任务到达
                    else
                    {
                        /// 添加新任务到等待队列
                        Actor job = jobNtf.Result;
                        jobWait.Add(job);
                        /// 创建一个新任务通知
                        jobNtf = _Jobs.Dequeue();
                    }
                }
            }
            /// 只要出现异常我们任务该对象就没有必要运行下去了
            catch (Exception e)
            {
                E = e;
            }

            /// 驱动逻辑最后的异常处理回调
            await OnException(E);
        }
    }
}
