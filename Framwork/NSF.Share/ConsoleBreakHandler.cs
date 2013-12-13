using System;
using System.Threading;

namespace NSF.Share
{
	public class ConsoleBreakHandler
	{
		/// <summary>
		/// 中断信号。
		/// </summary>
		AutoResetEvent _BreakEvent;

		/// <summary>
		/// 默认构造。
		/// </summary>
		public ConsoleBreakHandler()
		{
			/// 接收控制台中断信号退出服务
			_BreakEvent = new AutoResetEvent(false);
			Console.CancelKeyPress += new ConsoleCancelEventHandler(BreakHandler);
		}

		/// <summary>
		/// 等待控制台中断信号。
		/// </summary>
		public void Wait()
		{
			_BreakEvent.WaitOne();
		}

		/// <summary>
		/// 中断处理函数。
		/// </summary>
		public void BreakHandler(object sender, ConsoleCancelEventArgs args)
		{
			_BreakEvent.Set();
		}
	}
}
