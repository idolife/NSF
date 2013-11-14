using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NSF.Share;

namespace NSF.Framework.Net
{
	/// <summary>
	/// 处理连接接收的对象。
	/// </summary>
    public class StreamAcceptor
    {
		/// <summary>
		/// 开启连接器服务。
		/// </summary>
		/// <param name="port">服务绑定的端口。</param>
		public void Init(int port)
		{
			try
			{
				/// 开启侦听
				TcpListener listener = new TcpListener(IPAddress.Any, port);
				listener.Start();

				/// 开启侦听主循环线程
				Task.Run(async () => await Svc(listener))
					.ContinueWith(OnListenerException, TaskContinuationOptions.OnlyOnFaulted);

                Log.Info("Listen service success at {0}", listener.LocalEndpoint.ToString());
			}
			catch (Exception e)
			{
                Log.Error("Listen service failed at {0} : {1}", port, e.ToString());
			}		
		}

		/// <summary>
		/// 创建连接器实例。
		/// </summary>
		public virtual StreamHandler MakeHandler(TcpClient client)
		{
			return
				new StreamHandler(client);
		}

		/// <summary>
		/// 侦听服务主循环。
		/// </summary>
		private async Task Svc(TcpListener listener)
		{
			while (true)
			{
				try
				{
					// 接收一个新的连接
					TcpClient client;
					client = await listener.AcceptTcpClientAsync();

					/// 创建连接处理者
					StreamHandler handler = MakeHandler(client);
					/// 创建连接处理者任务
#pragma warning disable 4014
					Task.Run(async () => await handler.Svc())
						.ContinueWith(OnClientException, TaskContinuationOptions.OnlyOnFaulted);
#pragma warning restore 4014
				}
				catch (SocketException e)
				{
					/// 当处理接收过程中排队的连接断开后会发生异常
					/// 此异常不应该导致侦听服务中断服务
					Log.Debug("Service exception: {0}", e.ToString());
				}
			}
		}

		/// <summary>
		/// 侦听器未处理异常回调。
		/// </summary>
		private void OnListenerException(Task t)
		{
			Exception e = t.Exception;
			Log.Error(String.Format("Exception unhandled in listener task: {0}", e.ToString()));
		}

		/// <summary>
		/// 连接处理器未处理异常回调。
		/// </summary>
		private void OnClientException(Task t)
		{
			Exception e = t.Exception;
			Log.Error(String.Format("Exception unhandled in handler task: {0}", e.ToString()));
		}
    }
}
