using System;
using System.Threading;
using NSF.Framework.Net;
using NSF.Share;

namespace NSF.GameServer
{
	class Program
	{
        static void DoGC()
        {
            while (true)
            {
                Thread.Sleep(1000);
                GC.Collect();
            }
        }

        static void OpenGC()
        {
            Thread gcThread = new Thread(DoGC);
            gcThread.Start();
        }

		static void Main(string[] args)
		{
            EchoService svc1 = new EchoService();
            svc1.Init(8001);
            HttpService svc2 = new HttpService();
            svc2.Init(8000);

			ConsoleBreakHandler consoleDone = new ConsoleBreakHandler();
			consoleDone.Wait();
		}
	}
}
