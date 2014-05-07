using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using NSF.Share;

namespace NSF.Framework.Net
{
    /// <summary>
    /// 处理HTTP连接接收的对象。
    /// </summary>
    public class HttpAcceptor
    {
        /// <summary>
        /// 注册的服务回调函数表。
        /// </summary>
        ConcurrentDictionary<String, Func<HttpListenerResponse, String, Task>> _Service =
            new ConcurrentDictionary<String, Func<HttpListenerResponse, String, Task>>();

        /// <summary>
        /// 注册服务回调。
        /// </summary>
        public void RegisterService(String path, Func<HttpListenerResponse, String, Task> callback)
        {
            if (_Service.TryAdd(path.ToUpper(), callback))
                Log.Debug("Register service({0}) success.", path, callback);
            else
                Log.Debug("Register service({0}) failed.", path, callback);
        }

        /// <summary>
        /// 服务初始化函数。
        /// </summary>
        public void Init(String[] prefixes = null)
        {
            /// 确定操作系统支持
            if (!HttpListener.IsSupported)
            {
                throw new NotImplementedException("HttpListener");
            }

            /// 开启服务目录
            /// （默认开启127.0.0.1:80）
            if (prefixes == null || prefixes.Length ==0)
            {
                prefixes = new String[1];
                prefixes[0] = "http://127.0.0.1:80/";
            }
            
            /// 创建HTTP侦听器
            HttpListener listener = new HttpListener();
            foreach (var v in prefixes)
                listener.Prefixes.Add(v);
            listener.Start();
            Log.Info("HttpListener is start at :");
            foreach(var v in prefixes)
            {
                Log.Info("\t({0})", v);
            }

            /// 开启侦听主循环线程
            Task.Run(async () => await Svc(listener))
                .ContinueWith(OnListenerException, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// 循环获取客户端的请求。
        /// </summary>
        private async Task Svc(HttpListener listener)
        {
            while (true)
            {
                try
                {
                    /// 接收一个请求
                    HttpListenerContext context = await listener.GetContextAsync();
#pragma warning disable 4014
                    /// 创建一个处理
                    /// （独立线程）
                    Task.Run(async () => await Proc(context))
                        .ContinueWith(OnClientException, TaskContinuationOptions.OnlyOnFaulted);
#pragma warning restore 4014

                }
                catch(Exception e)
                {
                    Log.Debug("HttpListener exception:{0}", e);
                }
            }

            ///// 停止服务
            //listener.Stop();
        }

        /// <summary>
        /// 每请求的处理逻辑。
        /// </summary>
        private async Task Proc(HttpListenerContext http)
        {
            Log.Debug("---{0,08}-------------------------------", http.GetHashCode());

            HttpListenerRequest request = http.Request;
            Log.Debug("{0}", request.RemoteEndPoint);            
            Log.Debug("{0}", request.Url.AbsolutePath);
            String context = "";
            if (request.ContentLength64 > 0)
            {   

                Stream body = request.InputStream;
                Encoding encoding = request.ContentEncoding;
                StreamReader reader = new StreamReader(body, encoding);
                context = await reader.ReadToEndAsync();
                Log.Debug("{0}", context);
            }

            do
            {
                /// 只支持POST方法
                if (request.HttpMethod == "GET")
                {
                    Log.Warn("GET method not support.");
                    http.Response.StatusCode = (Int32)HttpStatusCode.NotFound;                    
                    break;
                }

                /// 回调注册的服务
                Func<HttpListenerResponse, String, Task> callback;
                if (_Service.TryGetValue(request.Url.AbsolutePath.ToUpper(), out callback))
                {
                    await callback(http.Response, context);
                }
                else
                {
                    Log.Warn("No service register for {0}.", request.Url.AbsolutePath);
                    http.Response.StatusCode = (Int32)HttpStatusCode.NotImplemented;
                    break;
                }

            } while (false);

            ///http.Response.Close();
            Log.Debug("-------------------------------{0,08}---", http.GetHashCode());
        }

        /// <summary>
        /// 连接字符串。
        /// </summary>
        private String ConcatString(System.Collections.Specialized.NameValueCollection col, String sep0, String sep1, String sep2)
        {
            if (col == null)
                return "";

            StringBuilder build = new StringBuilder();
            for(Int32 i = 0; i < col.Count; ++i)
            {
                if (i != 0)
                    build.Append(sep1);
                build.Append(col.GetKey(i));
                build.Append(sep0);
                var val = col.GetValues(i);
                if (val == null)
                {
                    build.Append("(null)");
                }
                else
                {
                    for(Int32 j = 0; j < val.Length; ++j)
                    {
                        if (j != 0)
                            build.Append(sep2);
                        build.Append(val[j]);
                    }
                }
            }
            return build.ToString();
        }

        /// <summary>
        /// 侦听器未处理异常回调。
        /// </summary>
        private void OnListenerException(Task t)
        {
            Exception e = t.Exception;
            Log.Debug(String.Format("Exception unhandled in listener task: {0}", e.ToString()));
        }

        /// <summary>
        /// 连接处理器未处理异常回调。
        /// </summary>
        private void OnClientException(Task t)
        {
            Exception e = t.Exception;
            Log.Debug(String.Format("Exception unhandled in handler task: {0}", e.ToString()));
        }
    }
}
