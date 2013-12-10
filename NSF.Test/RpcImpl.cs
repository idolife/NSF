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
            RegisterCall<RpcIncrementReq, RpcIncrementAck>(OnRpcIncrementReq);
        }

        protected Task<Object> OnRpcIncrementReq(Object args)
        {
            RpcIncrementReq req = args as RpcIncrementReq;
            if (req == null)
            {
                Log.Debug("OnRpcIncrementReq, Request parameter is null.");
                return Task.FromResult((Object)null);
            }

            RpcIncrementAck ack = new RpcIncrementAck
            {
                Po = req.Op + 1,
                Xx = req.Xx,
            };

            return Task.FromResult((Object)ack);
        }

        protected Task<RpcIncrementAck> OnRpcIncrementReq(RpcIncrementReq args)
        {
            if (args == null)
            {
                Log.Debug("OnRpcIncrementReq, Request parameter is null.");
                return Task.FromResult((RpcIncrementAck)null);
            }
            RpcIncrementReq req = (RpcIncrementReq)args;

            RpcIncrementAck ack = new RpcIncrementAck
            {
                Po = req.Op + 1,
                Xx = req.Xx,
            };

            return Task.FromResult((RpcIncrementAck)ack);
        }
    }
}
