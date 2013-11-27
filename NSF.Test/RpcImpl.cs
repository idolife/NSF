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
        Dictionary<String, Func<String, Task<String>>> _Handlers = new Dictionary<String, Func<String, Task<String>>>();
        public async Task<string> OnCall(string method, string args)
        {
            if (_Handlers.ContainsKey(method))
                return
                    await _Handlers[method](args);
            else
                return null;            
        }

        public RpcDemoImpl()
        {
            _Handlers[typeof(RpcIncrementReq).ToString()] = OnRpcIncrementReq;
        }

        protected Task<String> OnRpcIncrementReq(String args)
        {
            RpcIncrementReq req = JsonConvert.DeserializeObject<RpcIncrementReq>(args);
            if (req == null)
            {
                Log.Debug("OnRpcIncrementReq, Deserialize request failed.");
                return Task.FromResult((String)(null));
            }

            RpcIncrementAck ack = new RpcIncrementAck
            {
                Po = req.Op + 1,
                Xx = req.Xx,
            };

            return Task.FromResult(JsonConvert.SerializeObject(ack));
        }
    }
}
