using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using NSF.Share;
using NSF.Framework.Net;


namespace NSF.GameServer
{
	/// <summary>
	/// 处理连接事件的对象。
	/// </summary>
	public class EchoHandler : StreamHandler
	{
		public EchoHandler(TcpClient client, int bufferSize = 8192) :
            base(client, bufferSize)
		{
            Log.Debug("[{0, 8}] EchoHandler, Ctor", this.GetHashCode());
		}

        ~EchoHandler()
		{
            Log.Debug("[{0, 8}] EchoHandler, Dtor", this.GetHashCode());
		}

		protected override Task OnReady()
		{
            Log.Debug("[{0, 8}] EchoHandler, OnReady", this.GetHashCode());

			return
				Task.FromResult<object>(null);
		}

        protected override Task OnBreak()
		{
            Log.Debug("[{0, 8}] EchoHandler, OnBreak", this.GetHashCode());

			return
				Task.FromResult<object>(0);
		}

        protected override async Task OnData(byte[] buffer, int length)
		{
            Log.Debug("[{0, 8}] EchoHandler, OnData, length={1}", this.GetHashCode(), length);

            await Send(buffer, length);
		}
	}
}
