using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using NSF.Framework.Base;
using NSF.Share;    

namespace NSF.Framework.Net.RPC
{
    public class RpcProxy : ConcurrentTaskAtor
    {
        TcpClient _Peer;
        IPEndPoint _Remote;
        ByteBlock _PullCache;
        List<RpcCallStub> _Waiting;

        public RpcProxy(IPEndPoint remote)
        {
            _Remote = remote;
            _PullCache = new ByteBlock(1024 * 8);
            _Waiting = new List<RpcCallStub>();

            /// 创建一个自动连接的任务
            BuildReConnect(0);
        }

        public Task<R> CallAsync<R>(Object req) where R : class
        {
            /// 创建一个调用任务进入队列
            RpcCallJob<R> job = Util.BuildRpcCallJob<R>(req);
            /// 创建一个关联任务的超时任务
            _Waiting.Add(job);
            Put(Task.Delay(1000), job, OnTimeout);

            if (_Peer != null && _Peer.Connected)
            {
                ByteBlock push = new ByteBlock(1024);
                /// 创建一个关联任务的发送任务
                Util.EncodeRpcCallInfo(job.Info, push);
                Task sendTask = _Peer.GetStream().WriteAsync(push.Buffer, push.ReadPosition, push.Length);
                Put(sendTask, job, OnSend);
                Log.Debug("RPC call#{0} job schedued.", job.Id);
            }
            else
            {
                Log.Debug("RPC call#{0} job not schedued because of not ready.", job.Id);
            }
            ///
            return job.State;
        }

        //public void Call(String method, String param)
        //{
        //    /// 创建一个调用任务进入队列
        //    RpcCallJob job = Util.BuildRpcCallJob(method, param);
        //    _Pending.Enqueue(job);

        //    /// 如果不在分发状态则发起一个
        //    if (Interlocked.CompareExchange(ref _Dispatching, 1, 0) == 0)
        //    {
        //        Put(Task.Delay(0), null, OnPush);
        //    }
        //}

        protected Task OnTimeout(Task finishTask, Object act)
        {
            RpcCallStub job = act as RpcCallStub;
            if (!job.IsCompleted)
            {
                /// 使用"空"数据来表示超时
                job.Complete(null);
                Log.Debug("RPC call#{0} job timeout.", job.Id);
            }
            _Waiting.Remove(job);
            return Task.FromResult(0);
        }

        //protected Task OnPush(Task finishTask, Object nil)
        //{
        //    /// 连接失效
        //    if (_Peer == null || !_Peer.Connected)
        //    {
        //        Log.Debug("Try to push but rpc connection not ready.");

        //        /// 清除外发状态
        //        Interlocked.CompareExchange(ref _Dispatching, 0, 1);
        //        return Task.FromResult(0);
        //    }

        //    ///  公平竞争模式
        //    RpcCallStub job;
        //    if (!_Pending.TryDequeue(out job))
        //    {
        //        Log.Debug("Try to push but no rpc call pending.");

        //        /// 清除外发状态
        //        Interlocked.CompareExchange(ref _Dispatching, 0, 1);
        //        return Task.FromResult(0);
        //    }

        //    _Waiting.Add(job);
        //    Util.EncodeRpcCallInfo(job.Info, _PushCache);
        //    Task sendTask = _Peer.GetStream().WriteAsync(_PushCache.Buffer, _PushCache.ReadPosition, _PushCache.Length);
        //    Put(sendTask, job, OnSend);
        //    Log.Debug("RPC call#{0} job schedued.", job.Id);

        //    return Task.FromResult(0);
        //}

        protected Task OnSend(Task finishTask, Object with)
        {
            RpcCallStub job = with as RpcCallStub;
            Log.Debug("RPC call#{0} job sended.", job.Id);
            return Task.FromResult(0);
        }

        protected async Task OnConnect(Task finishTask, Object nil)
        {
            try
            {
                await finishTask;
                Log.Debug("RPC({0}) connection success.", _Remote);
            }
            catch (Exception e)
            {
                Log.Debug("RPC({0}) connection failed:{1}.", _Remote, e.Message);
                BuildReConnect();
                return;
            }

            /// 发起第一个接收任务
            BuildRecvTask();
        }

        protected void BuildRecvTask()
        {
            Task<Int32> recvTask = _Peer.GetStream().ReadAsync(_PullCache.Buffer, _PullCache.WritePosition, _PullCache.Space);
            Put(recvTask, null, OnRecv);
        }

        protected async Task OnRecv(Task finishTask, Object nil)
        {
            Task<Int32> recvTask = finishTask as Task<Int32>;
            try
            {
                /// 完成接收任务
                Int32 recvLen = await recvTask;
                if (recvLen == 0)
                {
                    Log.Debug("RPC({0}) connection closed.", _Remote);
                    BuildReConnect();
                    return;
                }
                /// 更新接收缓存
                _PullCache.WriteOffset(recvLen);
                Log.Debug("RPC({0}) recv data = {0}.", _Remote, recvLen);
                /// 处理接收消息
                ProcessRecv();
                /// 发起下一个接收任务
                BuildRecvTask();
            }
            catch (Exception e)
            {
                Log.Debug("RPC({0}) connection breaked:{1}.", _Remote, e.Message);
                BuildReConnect();
                return;
            }
        }

        protected void ProcessRecv()
        {
            /// 一次处理接收的所有应答消息
            while (true)
            {
                /// 解析应答消息
                RpcRespInfo resp = Util.DecodeRpcRespInfo(_PullCache);
                /// 解析完毕或者当前不足一个完整消息
                if (resp == null)
                    break;
                Log.Debug("RPC call#{0} job respone = {1}.", resp.Id, resp.Result);
                /// 从等待队列中找到相应的请求消息
                RpcCallStub job = _Waiting.Find(x => x.Id == resp.Id);
                if (job == null)
                {
                    /// 已经超时移除了
                    Log.Debug("RPC call#{0} job respone arrived but not in waiting queue anymore.", resp.Id);
                }
                else
                {
                    /// 设置其完成并移出等待队列
                    job.Complete(resp.Result);
                    _Waiting.Remove(job);
                    Log.Debug("RPC call#{0} job respone arrive and complete succuessful.", resp.Id);
                }
            }
            _PullCache.Crunch();
        }

        protected void BuildReConnect(Int32 timeout = 5000)
        {
            if (_Peer != null)
                _Peer.Close();
            _Peer = new TcpClient();
            Put(Task.Delay(timeout), null, ReConnect);
        }

        protected Task ReConnect(Task finishTask, Object nil)
        {
            Task connectTask = _Peer.ConnectAsync(_Remote.Address, _Remote.Port);
            Put(connectTask, null, OnConnect);
            return Task.FromResult(0);
        }

        protected override Task OnException(Exception e)
        {
            Log.Debug("RpcProxy connection break: {0}.", e);
            BuildReConnect();
            return Task.FromResult(0);
        }
    }
}
