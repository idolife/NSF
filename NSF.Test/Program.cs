using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using NSF.Share;
using NSF.Framework.Net.RPC;

namespace NSF.Test
{ 
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                /// server
                RpcDemoImpl impl = new RpcDemoImpl();
                RpcServer svc = new RpcServer();
                svc.Init(new IPEndPoint(IPAddress.Any, 7000), impl);
                Console.ReadKey();
            }
            else
            {
                /// client
                RpcProxy rpc = new RpcProxy(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7000));
                Log.Debug("Waiting rpc proxy connection ready.");
                Thread.Sleep(1000);

                Log.Debug("Post rpc.");
                RpcIncrementReq req = new RpcIncrementReq
                {
                    Op = 10000,
                    Xx = "Hello RPC",
                };
                String result = rpc.CallAsync(req.GetType().ToString(), JsonConvert.SerializeObject(req)).Result;
                if (result == null)
                    Log.Debug("RPC = timeout.");
                else
                    Log.Debug("RPC = {0}.", result);
                Console.ReadKey();
            }
        }
    }
}
