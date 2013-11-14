using System;
using System.Net.Sockets;
using NSF.Framework.Net;

namespace NSF.GameServer
{
    public class HttpService : StreamAcceptor
    {
        public override StreamHandler MakeHandler(TcpClient client)
        {
            return new HttpHandler(client);
        }
    }
}
