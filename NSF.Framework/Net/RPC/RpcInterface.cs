using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NSF.Share;

namespace NSF.Framework.Net.RPC
{
    internal class OnCallProxy
    {
        public Type RealRequestType { get; private set; }
        public Type RealResponeType { get; private set; }
        public Func<Object, Task<Object>> RealCall { get; private set; }

        public OnCallProxy(Type reqType, Type rspType, Func<Object, Task<Object>> realCall)
        {
            RealRequestType = reqType;
            RealResponeType = rspType;
            RealCall = realCall;
        }
    }

    public class RpcInterface
    {
        Dictionary<String, OnCallProxy> _Handlers = new Dictionary<String, OnCallProxy>();

        /// <summary>
        /// 当某调用无应答时返回"空".
        /// </summary>
        public Task<Object> OnCall(String method, String args)
        {
            if (_Handlers.ContainsKey(method))
            {
                OnCallProxy proxyCall = _Handlers[method];
                Object paramInst = JsonConvert.DeserializeObject(args, proxyCall.RealRequestType);
                return proxyCall.RealCall(paramInst);
            }
            else
            {
                Log.Error("RPC, No handler register for {0}.", method);
                return null;
            }
        }

        public void RegisterCall<T, R>(Func<T, Task<R>> func) 
            where T : class
            where R : class
        {
            Func<Object, Task<Object>> fackCall =
                (Object r) =>
                {
                    T t = r as T;
                    Task<R> r0 = func(t);
                    Task<Object> r1 = new Task<Object>(() => 
                    {
                        R rr = r0.Result; 
                        return (rr as Object);
                    });
                    r1.Start();
                    return (r1);
                };

            if (_Handlers.ContainsKey(typeof(T).ToString()))
            {
                Log.Warn("RPC, Duplicate handler register for {0}.", typeof(T).ToString());
            }

            OnCallProxy proxyCall = new OnCallProxy(typeof(T), typeof(R), fackCall);
            _Handlers[typeof(T).ToString()] = proxyCall;
        }
    }
}
