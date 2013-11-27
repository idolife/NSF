using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NSF.Share;

namespace NSF.Framework.Net.RPC
{
    public class RpcServer
    {
        TcpListener _Listener;
        RpcInterface _RpcImpl;

        public void Init(IPEndPoint local, RpcInterface impl)
        {
            _RpcImpl = impl;
            _Listener = new TcpListener(local);
            _Listener.Start();
            Log.Debug("RPC server open success on {0}.", local);


            Task.Run(async () => await Svc());
        }

        protected async Task Svc()
        {
            while (true)
            {
                try
                {
                    // 接收一个新的连接
                    TcpClient client = await _Listener.AcceptTcpClientAsync();

                    /// 创建连接处理者
                    RpcClient handler = new RpcClient(client, _RpcImpl);
                    Log.Debug("RPC client open on {0}.", client.Client.RemoteEndPoint);
                }
                catch (SocketException e)
                {
                    /// 当处理接收过程中排队的连接断开后会发生异常
                    /// 此异常不应该导致侦听服务中断服务
                    Log.Debug("RpcServer exception: {0}", e);
                }
            }
        }
    }
}
