﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NSF.Share;
using NSF.Framework.Net.RPC;
using NSF.Test.RPC.Protocol;

namespace NSF.Test.RPC.Perf.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            RpcService.Default.RegisterRPC("127.0.0.1", 7000, "Test");
            RpcProxy rpc = RpcService.Default.GetRPC("Test");
            Log.Debug("Waiting 3000 ms to wait rpc proxy connection ready ...");
            Thread.Sleep(3000);
            Log.Debug("Waiting 3000 ms to wait rpc proxy connection done.");

            Log.Debug("Main, Build rpc request task ...");
            /// Currently, RpcProxy is not thread safe.
            for (Int32 i = 1; i <= 1; ++i)
            {
                Task.Run(() => RpcRequest(rpc, i));
            }
            Log.Debug("Main, Build rpc request task done.");
            Console.ReadKey();
        }

        static async void RpcRequest(RpcProxy rpc, Int32 id)
        {
            for (int i = 1; i <= 10000; ++i)
            {
                RpcEchoReq req = new RpcEchoReq
                {
                    localId = id * 100000 + i,
                    LocalTimestamp = Util.DateTimeToTimestamp(DateTime.Now),
                    LocalMessage = String.Format("Helo svc, i'm rpc#{0}.", id),
                };

                Log.Debug("#[{0, 2}.{1, 02}]RPC request posting ...", id, i);
                RpcEchoAck ack = await rpc.CallAsync<RpcEchoAck>(req);
                if (ack == null)
                {
                    Log.Debug("#[{0, 2}.{1, 02}]RPC request timeout.", id, i);
                }
                else
                {
                    Log.Debug("#[{0, 2}.{1, 02}]RPC respone = [{2},{3},{4},{5}].", id, i, ack.localId, ack.LocalTimestamp, ack.RemoteTimestamp, ack.RemoteMessage);
                }
            }
        }
    }
}
