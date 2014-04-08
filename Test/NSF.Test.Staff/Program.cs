using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using NSF.Share;
using NSF.Framework.Net;

namespace NSF.Test.Staff
{
    class Program
    {
        static async Task HttpModuleBilling(HttpListenerResponse resp, String context)
        {
            Log.Debug("HttpModuleBilling, {0}", context);
            String respInfo = "{\"status\":\"1\"}";
            byte[] respBuff = Encoding.UTF8.GetBytes(respInfo);
            resp.ContentType = "text/json";
            await resp.OutputStream.WriteAsync(respBuff, 0, respBuff.Length);            
        }

        static void Main(string[] args)
        {
            ///// 测试GZip压缩/解压
            //var v1 = Util.GZipCompressString("这个是要测试的中文字符串", Encoding.UTF8);
            //var v2 = Util.GZipDecompressString(v1, Encoding.UTF8);

            HttpAcceptor svc = new HttpAcceptor();
            svc.Init();
            svc.RegisterService("/billing/", HttpModuleBilling);
            (new ConsoleBreakHandler()).Wait();
        }
    }
}
