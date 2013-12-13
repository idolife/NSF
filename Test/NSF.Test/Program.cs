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
            /// Intialize JsonConvert
            var ___1 = JsonConvert.SerializeObject(new RpcIncrementReq { Op = 100, Xx = "bb" });
            var ___2 = JsonConvert.DeserializeObject<RpcIncrementReq>(___1);
            var ___3 = JsonConvert.SerializeObject(new RpcIncrementAck { Po = 100, Xx = "bb" });
            var ___4 = JsonConvert.DeserializeObject<RpcIncrementAck>(___3);

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
                RpcIncrementAck ack = rpc.CallAsync<RpcIncrementAck>(req).Result;
                if (ack == null)
                    Log.Debug("RPC = timeout.");
                else
                    Log.Debug("RPC = {0}.", ack);
                Console.ReadKey();
            }
        }
    }
}
