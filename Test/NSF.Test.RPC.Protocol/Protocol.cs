using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSF.Share;
using NSF.Framework.Net.RPC;

namespace NSF.Test.RPC.Protocol
{
    public class RpcEchoReq
    {
        public Int32 localId;
        public Int32 LocalTimestamp;
        public String LocalMessage;
    }

    public class RpcEchoAck
    {
        public Int32 localId;
        public Int32 LocalTimestamp;
        public Int32 RemoteTimestamp;
        public String RemoteMessage;
    }

    public class RpcEchoProtocol : RpcInterface
    {
        public RpcEchoProtocol()
        {
            RegisterCall<RpcEchoReq, RpcEchoAck>(OnRpcEchoReq);
        }

        protected Task<RpcEchoAck> OnRpcEchoReq(RpcEchoReq reg)
        {
            /// 参数检查
            if (reg == null)
            {
                Log.Debug("OnRpcIncrementReq, Request parameter is null.");
                return Task.FromResult<RpcEchoAck>(null);
            }

            /// 逻辑处理
            RpcEchoAck ack = new RpcEchoAck
            {
                localId = reg.localId,
                LocalTimestamp = reg.LocalTimestamp,
                RemoteTimestamp = Util.DateTimeToTimestamp(DateTime.Now),
                RemoteMessage = String.Format("Hello, {0} !", reg.localId),
            };
            return Task.FromResult<RpcEchoAck>(ack);
        }
    }
}
