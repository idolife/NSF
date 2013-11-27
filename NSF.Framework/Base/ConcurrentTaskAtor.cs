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
        ConcurrentBag<Actor> _Jobs;
        /// <summary>
        /// 任务取消对象。
        /// </summary>
        CancellationTokenSource _CancelTokenSource;
        /// <summary>
        /// 默认构造。
        /// </summary>
        public ConcurrentTaskAtor()
        {
            _Jobs = new ConcurrentBag<Actor>();
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
            _Jobs.Add(act);
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
            /// 每间隔刷新任务队列时常（毫秒）
            Int32 TIMEOUT = 200;
            /// 循环逻辑异常的异常数据。
            Exception E = null;
            /// 当前的执行队列。
            List<Actor> waitActs = new List<Actor>();
            /// 我们总是希望该对象有东西可做
            /// 所以这里就不适用信号机制来通知有新任务到达
            try
            {
                while (true)
                {
                    /// 创建一个超时任务用来实现等待超时
                    /// （每隔定时切换一次排期从而使等待过程中产生的新任务加入）
                    while (_Jobs.Count > 0)
                    {
                        Actor act;
                        if (!_Jobs.TryTake(out act))
                            break;
                        waitActs.Add(act);
                    }

                    /// 空任务队列
                    if (waitActs.Count == 0)
                    {
                        /// 超时设定作为休眠时间
                        await Task.Delay(TIMEOUT);
                        continue;
                    }

                    /// 等待任意一个任务完成
                    List<Task> waitTasks = waitActs.Select(x => x.Task).ToList();
                    Task timeoutTask = Task.Delay(TIMEOUT);
                    waitTasks.Add(timeoutTask);
                    Task finishTask = await Task.WhenAny(waitTasks);
                    /// 有任务完成
                    /// （非超时）
                    if (finishTask != timeoutTask)
                    {
                        /// 回调完成处理
                        Actor job = waitActs.Find(x => x.Task == finishTask);
                        await job.Callback(finishTask, job.Object);

                        /// 移除完成的任务
                        waitActs.Remove(job);
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
