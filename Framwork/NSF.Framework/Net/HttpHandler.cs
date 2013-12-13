using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using NSF.Share;

namespace NSF.Framework.Net
{
    public class HttpHandler : StreamHandler
    {
        public HttpHandler(TcpClient client) :
            base(client)
		{
            Log.Debug("[{0, 8}] HttpHandler, Ctor", this.GetHashCode());
		}

        ~HttpHandler()
		{
            Log.Debug("[{0, 8}] HttpHandler, Dtor", this.GetHashCode());
		}

        protected override Task OnData(byte[] buffer, int length)
		{
            Log.Debug("[{0, 8}] HttpHandler, OnData, length={1}", this.GetHashCode(), length);
            String requestString = Encoding.UTF8.GetString(buffer, 0, length);
            Log.Debug("[{0, 8}] HttpHandler, OnData, requst={1}", this.GetHashCode(), requestString);
            return Task.FromResult(0);
		}
    }
}
