using System;
using System.Net.Sockets;
using NSF.Framework.Net;

namespace NSF.GameServer
{
    public class EchoService : StreamAcceptor
    {
        public override StreamHandler MakeHandler(TcpClient client)
        {
            return new EchoHandler(client);
        }
    }
}
