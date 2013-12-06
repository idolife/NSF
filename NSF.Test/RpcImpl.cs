using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSF.Share;
using NSF.Framework.Net.RPC;

namespace NSF.Test
{
    class RpcIncrementReq
    {
        public Int32 Op;
        public String Xx;
    }

    class RpcIncrementAck
    {
        public Int32 Po;
        public String Xx;
    }

    class RpcDemoImpl : RpcInterface
    {
        public RpcDemoImpl()
        {
            RegisterCall(typeof(RpcIncrementReq), OnRpcIncrementReq);
        }

        protected Task<Object> OnRpcIncrementReq(Object args)
        {
            if (args == null)
            {
                Log.Debug("OnRpcIncrementReq, Request parameter is null.");
                return Task.FromResult((Object)null);
            }
            RpcIncrementReq req = (RpcIncrementReq)args;

            RpcIncrementAck ack = new RpcIncrementAck
            {
                Po = req.Op + 1,
                Xx = req.Xx,
            };

            return Task.FromResult((Object)ack);
        }
    }
}
