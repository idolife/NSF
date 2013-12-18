using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using NSF.Share;

namespace NSF.Framework.Net.RPC
{
    /// <summary>
    /// 客户端使用的RPC管理对象。
    /// </summary>
    public class RpcService
    {
        ConcurrentDictionary<String, RpcProxy> _RPCs = new ConcurrentDictionary<String,RpcProxy>();
        /// <summary>
        /// 全局实例。
        /// </summary>
        private static RpcService _Default = null;
        /// <summary>
        /// 默认全局实例。
        /// </summary>
        public static RpcService Default
        {
            get
            {
                /// FIXME:应该处理现场安全问题
                if (_Default == null)
                    _Default = new RpcService();
                return _Default;
            }
        }

        /// <summary>
        /// 注册一个可以用来后续使用的RPC代理。
        /// </summary>
        /// <param name="remote">RPC访问的远端地址。</param>
        /// <param name="port">RPC访问的远端端口。</param>
        /// <param name="name">用来获取该代理的名称。</param>
        public void RegisterRPC(String remote, Int32 port, String name)
        {
            Log.Debug("RpcService, Register RPC = [{0}, {1}:{2}]", name, remote, port);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(remote), port);
            RpcProxy rpc = new RpcProxy(remoteEP);
            if (_RPCs.TryAdd(name, rpc) == false)
            {
                Log.Debug("RpcService, Register RPC({0}) is already registered.");
            }
        }

        /// <summary>
        /// 根据提供的名称获取RPC代理对象。
        /// </summary>
        /// <param name="name">RPC代理名称。</param>
        /// <returns></returns>
        public RpcProxy GetRPC(String name)
        {
            RpcProxy rpc = null;
            if (_RPCs.TryGetValue(name, out rpc) == false)
            {
                Log.Debug("RpcService, RPC({0}) not exist.");
            }
            ///
            return rpc;
        }
    }
}
