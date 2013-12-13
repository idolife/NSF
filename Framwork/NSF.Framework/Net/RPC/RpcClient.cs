using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Newtonsoft.Json;
using NSF.Framework.Base;
using NSF.Share;    

namespace NSF.Framework.Net.RPC
{
    class RpcClient : ConcurrentTaskAtor
    {
        TcpClient _Peer;
        RpcInterface _Impl;
        ConcurrentQueue<RpcRespJob> _Pending;
        List<RpcRespJob> _Waiting;

        public RpcClient(TcpClient client, RpcInterface impl)
        {
            _Pending = new ConcurrentQueue<RpcRespJob>();
            _Waiting = new List<RpcRespJob>();
            _Impl = impl;
            _Peer = client;

            ByteBlock block = new ByteBlock(2048);
            /// 发起第一个接受任务
            Task<Int32> recvTask = _Peer.GetStream().ReadAsync(block.Buffer, block.WritePosition, block.Space);
            Put(recvTask, block, OnRecv);
        }

        protected Task OnRecv(Task finishTask, Object finishData)
        {
            ByteBlock block = finishData as ByteBlock;
            /// 使用异常促使底层循环退出
            Task<Int32> recvTask = finishTask as Task<Int32>;
            /// 完成接收任务
            Int32 recvLen = recvTask.Result;
            if (recvLen == 0)
                throw new SocketException();
            /// 更新接收缓存
            block.WriteOffset(recvLen);
            Log.Debug("RPC recv data = {0}.", recvLen);

            /// 一次处理接收的所有应答消息
            while (true)
            {
                /// 解析应答消息
                RpcCallInfo rpc = Util.DecodeRpcCallInfo(block);
                /// 解析完毕或者当前不足一个完整消息
                if (rpc == null)
                    break;
                /// 创建一个调用任务
                Log.Debug("RPC recv call = [{0}].", rpc.Id);
                /// 创建逻辑调用任务
                Task<Object> callTask = _Impl.OnCall(rpc.Method, rpc.Args);
                Put(callTask, rpc, OnResp);
            }

            /// 发起下一个接收任务
            block.Crunch();
            recvTask = _Peer.GetStream().ReadAsync(block.Buffer, block.WritePosition, block.Space);
            Put(recvTask, block, OnRecv);

            return Task.FromResult(0);
        }

        protected Task OnResp(Task finishTask, Object job)
        {
            /// 调用完成
            RpcCallInfo call = job as RpcCallInfo;
            Task<Object> callTask = finishTask as Task<Object>;
            Object result = callTask.Result;
            Log.Debug("RPC call#{0} resp = [{1}].", call.Id, result);

            /// 返回结果为“空”表示此调用无应答
            if (result != null)
            {
                /// 应答信息
                RpcRespInfo resp = new RpcRespInfo
                {
                    Id = call.Id,
                    Result = JsonConvert.SerializeObject(result),
                };

                ByteBlock push = new ByteBlock(1024);
                /// 创建一个应答消息
                Util.EncodeRpcRespInfo(resp, push);
                Task sendTask = _Peer.GetStream().WriteAsync(push.Buffer, push.ReadPosition, push.Length);
                Put(sendTask, resp, OnSend);
                Log.Debug("RPC call#{0} resp scheduled.", resp.Id);
            }
            return Task.FromResult(0);
        }

        protected Task OnSend(Task finishTask, Object job)
        {
            RpcRespInfo resp = job as RpcRespInfo;
            Log.Debug("RPC call#{0} resp sended.", resp.Id);
            return Task.FromResult(0);
        }

        protected override Task OnException(Exception e)
        {
            Log.Debug("RPC connection break: {0}.", e);
            _Peer.Close();
            return Task.FromResult(0);
        }
    }
}
