using System;
using System.Net;
using System.Threading.Tasks;
using NSF.Share;
using NSF.Framework.Net.RPC;
using NSF.Test.RPC.Protocol;

namespace NSF.Test.RPC.Perf.Svc
{
    class Program
    {
        static void Main(string[] args)
        {
            RpcEchoProtocol impl = new RpcEchoProtocol();
            RpcServer svc = new RpcServer();
            svc.Init(new IPEndPoint(IPAddress.Any, 7000), impl);
            Console.ReadKey();
        }
    }
}
