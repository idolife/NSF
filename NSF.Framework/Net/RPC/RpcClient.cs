using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using NSF.Framework.Base;
using NSF.Share;    

namespace NSF.Framework.Net.RPC
{
    class RpcClient : ConcurrentTaskAtor
    {
        TcpClient _Peer;
        ByteBlock _PullCache;
        RpcInterface _Impl;
        ConcurrentQueue<RpcRespJob> _Pending;
        List<RpcRespJob> _Waiting;
        Int32 _Dispatching;
        ByteBlock _PushCache;

        public RpcClient(TcpClient client, RpcInterface impl)
        {
            _Dispatching = 0;
            _Pending = new ConcurrentQueue<RpcRespJob>();
            _Waiting = new List<RpcRespJob>();
            _Impl = impl;
            _Peer = client;
            _PullCache = new ByteBlock(1024 * 8);
            _PushCache = new ByteBlock(1024 * 8);

            /// 发起第一个接受任务
            BuildRecvTask();
        }

        protected void BuildRecvTask()
        {
            Task<Int32> recvTask = _Peer.GetStream().ReadAsync(_PullCache.Buffer, _PullCache.WritePosition, _PullCache.Space);
            Put(recvTask, null, OnRecv);
        }
        protected async Task OnRecv(Task finishTask, Object nil)
        {
            /// 使用异常促使底层循环退出
            ///
            Task<Int32> recvTask = finishTask as Task<Int32>;
            /// 完成接收任务
            Int32 recvLen = await recvTask;
            if (recvLen == 0)
                throw new SocketException();
            /// 更新接收缓存
            _PullCache.WriteOffset(recvLen);
            Log.Debug("RpcClient, Recv data = {0}.", recvLen);
            /// 处理接收消息
            ProcessRecv();
            /// 发起下一个接收任务
            BuildRecvTask();
        }

        protected void ProcessRecv()
        {
            /// 一次处理接收的所有应答消息
            while (true)
            {
                /// 解析应答消息
                RpcCallInfo rpc = Util.DecodeRpcCallInfo(_PullCache);
                /// 解析完毕或者当前不足一个完整消息
                if (rpc == null)
                    break;
                /// 创建一个调用任务
                Log.Debug("RpcClient, Recv call = [{0}].", rpc.Id);
                BuildCallTask(rpc);
            }
            _PullCache.Crunch();
        }

        protected void BuildCallTask(RpcCallInfo call)
        {
            RpcRespJob resp = Util.BuildRpcRespJob(call);
            _Pending.Enqueue(resp);

            /// 如果不在分发状态则发起一个
            if (Interlocked.CompareExchange(ref _Dispatching, 1, 0) == 0)
            {
                Put(Task.Delay(0), null, OnInvoke);
            }
        }

        protected Task OnInvoke(Task finishTask, Object nil)
        {
            ///  公平竞争模式
            RpcRespJob job;
            if (!_Pending.TryDequeue(out job))
            {
                /// 清除分发状态
                Interlocked.CompareExchange(ref _Dispatching, 0, 1);
                return Task.FromResult(0);
            }

            _Waiting.Add(job);
            Log.Debug("RpcClient, Invoke call = [{0}].", job.Call.Id);

            /// 创建逻辑调用任务
            Task<String> callTask = _Impl.OnCall(job.Call.Method, job.Call.Args);
            Put(callTask, job, OnResp);

            return Task.FromResult(0);
        }

        protected async Task OnResp(Task finishTask, Object job)
        {
            /// 调用完成
            RpcRespJob resp = job as RpcRespJob;
            Task<String> callTask = finishTask as Task<String>;
            String result = await callTask;
            resp.Complete(result);
            Log.Debug("RpcClient, [{0}], Call resp = [{1}].", resp.Call.Id, result);

            /// 移除等待应答任务
            _Waiting.Remove(resp);

            /// 返回结果为“空”表示此调用无应答
            if (result != null)
            {
                /// 创建一个应答消息
                Util.EncodeRpcRespInfo(resp.Resp, _PushCache);
                /// 此任务不放在底层调度的原因是
                /// 为了保证外发缓存的安全
                /// 由此带来的副作用是：当某个调用返回时只有其应答消息外发完成后才能分发其他的调用
                await _Peer.GetStream().WriteAsync(_PushCache.Buffer, _PushCache.ReadPosition, _PushCache.Length);
                Log.Debug("RpcClient, [{0}], Call resp send = [{1}].", resp.Call.Id, _PushCache.Length);
                _PullCache.Reset();
            }

            /// 如果有必要发起下一个调用任务
            if (_Pending.Count > 0)
            {
                Put(Task.Delay(0), null, OnInvoke);
            }
            else
            {
                /// 清除分发状态
                Interlocked.CompareExchange(ref _Dispatching, 0, 1);
            }
        }
    }
}
