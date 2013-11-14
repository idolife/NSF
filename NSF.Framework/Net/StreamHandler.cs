using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using NSF.Share;


namespace NSF.Framework.Net
{
	/// <summary>
	/// 处理连接事件的对象。
    /// （基础处理）
	/// </summary>
	public class StreamHandler
	{
		private TcpClient _Peer;
		private int _BufferSize;

		public StreamHandler(TcpClient client, int bufferSize = 1024*2)
		{
			_Peer = client;
			_BufferSize = bufferSize;

            Log.Debug("[{0, 8}] StreamHandler, Ctor", this.GetHashCode());
		}

		~StreamHandler()
		{			
			_Peer.Close();

            Log.Debug("[{0, 8}] StreamHandler, Dtor", this.GetHashCode());
		}


		public async Task Svc()
		{
            try
            {
                /// 触发连接完成事件
                await this.OnReady();

                /// 接收数据包
                byte[] buffer = new byte[_BufferSize];
                while (true)
                {
                    /// 发起一个异步读
                    int tranLength = await _Peer.GetStream().ReadAsync(buffer, 0, buffer.Length);

                    /// 接收长度为0表示远端断开
                    if (tranLength == 0)
                    {
                        Log.Debug("[{0, 8}] StreamHandler, transfrered == 0", this.GetHashCode());
                        break;
                    }

                    /// 触发数据到达消息
                    await this.OnData(buffer, tranLength);
                }
            }
            catch (Exception /*e*/)
            {
                //Log.Debug("[{0, 8}] StreamHandler, exception: {1}", this.GetHashCode(), e.ToString());
            }	

			/// 触发连接断开事件
			await this.OnBreak();
		}

        protected async Task Send(byte[] buffer, int length)
		{
			await _Peer.GetStream().WriteAsync(buffer, 0, buffer.Length);

            Log.Debug("[{0, 8}] StreamHandler, Send, length={1}", this.GetHashCode(), length);
		}

        protected void Close()
		{
			if (_Peer != null)
			{
				_Peer.Close();
			}
		}

        protected virtual Task OnReady()
		{
            Log.Debug("[{0, 8}] StreamHandler, OnReady", this.GetHashCode());

			return
				Task.FromResult<object>(0);
		}

        protected virtual Task OnBreak()
		{
            Log.Debug("[{0, 8}] StreamHandler, OnBreak", this.GetHashCode());

			return
				Task.FromResult<object>(0);
		}

        protected virtual Task OnData(byte[] buffer, int length)
		{
            Log.Debug("[{0, 8}] StreamHandler, OnData, length={1}", this.GetHashCode(), length);

			return
				Task.FromResult<object>(0);
		}
	}
}
