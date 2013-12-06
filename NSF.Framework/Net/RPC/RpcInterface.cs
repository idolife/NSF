using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSF.Share;

namespace NSF.Framework.Net.RPC
{
    public class RpcInterface
    {
        Dictionary<String, Func<Object, Task<Object>>> _Handlers = new Dictionary<String, Func<Object, Task<Object>>>();
        Dictionary<String, Type> _ObjectTypes = new Dictionary<String, Type>();

        /// <summary>
        /// 当某调用无应答时返回"空".
        /// NULL
        /// </summary>
        public Task<Object> OnCall(String method, String args)
        {
            if (_Handlers.ContainsKey(method))
            {
                Type paramType = _ObjectTypes[method];
                Object paramInst = JsonConvert.DeserializeObject(args, paramType);
                return _Handlers[method](paramInst);
            }
            else
            {
                Log.Error("RPC, No handler register for {0}.", method);
                return null;
            }
        }

        public void RegisterCall<T, R>(Func<T, Task<R>> func)
        {

        }

        public void RegisterCall(Type reqType, Func<Object, Task<Object>> func)
        {
            if (_Handlers.ContainsKey(reqType.ToString()))
            {
                Log.Warn("RPC, Duplicate handler register for {0}.", reqType.ToString());
            }

            _Handlers[reqType.ToString()] = func;
            _ObjectTypes[reqType.ToString()] = reqType;
        }
    }
}
