using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using NSF.Framework.Base;
using NSF.Share;    

namespace NSF.Framework.Net.RPC
{
    class RpcCallInfo
    {
        public Int32 Id;
        public String Method;
        public String Args;
    }

    class RpcRespInfo
    {
        public Int32 Id;
        public String Result;
    }

    class RpcCallJob
    {
        TaskCompletionSource<String> _TCS = new TaskCompletionSource<String>();
        public Task<String> State { get { return _TCS.Task; } }
        public Int32 Id { get { return Info.Id; } }
        public RpcCallInfo Info { get; private set; }
        public Boolean IsCompleted { get { return _TCS.Task.IsCompleted; } }

        public RpcCallJob(RpcCallInfo info)
        {
            Info = info;
        }

        public void Complete(String v)
        {
            Task.Run(() => _TCS.SetResult(v));
        }
    }

    class RpcRespJob
    {
        TaskCompletionSource<String> _TCS = new TaskCompletionSource<String>();
        public Task<String> State { get { return _TCS.Task; } }
        public Int32 Id { get { return Call.Id; } }
        public RpcCallInfo Call { get; private set; }
        public RpcRespInfo Resp { get; private set; }
        public String Args { get { return Call.Args; } }
        public Boolean IsCompleted { get { return _TCS.Task.IsCompleted; } }

        public RpcRespJob(RpcCallInfo info)
        {
            Call = info;
            Resp = new RpcRespInfo
            {
                Id = info.Id,
            };
        }
        public void Complete(String v)
        {
            Task.Run(() => _TCS.SetResult(v));
            Resp.Result = v;
        }
    }

    class Util
    {
        static Byte RPC_H = 0x00;
        static Byte RPC_T = 0xff;
        static Int32 SEED = 0;


        public static RpcRespJob BuildRpcRespJob(RpcCallInfo req)
        {
            RpcRespJob job = new RpcRespJob(req);
            return job;
        }

        public static RpcCallJob BuildRpcCallJob(String method, String param)
        {
            RpcCallInfo info = new RpcCallInfo
            {
                Id = Interlocked.Increment(ref SEED),
                Method = method,
                Args = param,
            };

            RpcCallJob job = new RpcCallJob(info);
            return job;
        }

        public static ByteBlock EncodeRpcCallInfo(RpcCallInfo info, ByteBlock buff)
        {
            String reqJson = JsonConvert.SerializeObject(info);
            String reqBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(reqJson));
            Byte[] reqBytes = Encoding.UTF8.GetBytes(reqBase64);

            MemoryStream wStream = new MemoryStream(buff.Buffer, buff.WritePosition, buff.Space);
            wStream.WriteByte(RPC_H);
            wStream.Write(reqBytes, 0, reqBytes.Length);
            wStream.WriteByte(RPC_T);
            buff.WriteOffset((Int32)wStream.Position);

            return buff;
        }

        public static RpcCallInfo DecodeRpcCallInfo(ByteBlock buff)
        {
            Byte[] cache = buff.Buffer;
            Int32 offset = buff.ReadPosition;
            Int32 total = buff.Length;

            /// 不足一个包的长度
            if (total < sizeof(byte)*2)
                return null;

            Int32 h = -1;
            Int32 t = -1;
            for(Int32 i = 0; i < total; ++i)
            {
                /// 检索包头标记
                if (cache[offset + i] == RPC_H)
                    h = i;

                if (cache[offset + i] == RPC_T)
                    t = i;

                if (h >= 0 && t >= 0)
                    break;
            }

            if (h < 0 || t < 0)
                return null;
            if (t <= h + 1)
                throw new InvalidDataException("RPC Stream Invalid(Location)");

            Int32 length = t - h - 1;
            Byte[] reqBytes = new byte[length];
            Array.Copy(cache, offset + h + 1, reqBytes, 0, length);
            buff.ReadOffset(length + 2);
            String reqBase64 = Encoding.UTF8.GetString(reqBytes);
            String reqJson = Encoding.UTF8.GetString(Convert.FromBase64String(reqBase64));
            RpcCallInfo rpcCallInfo = JsonConvert.DeserializeObject<RpcCallInfo>(reqJson);
            if (rpcCallInfo == null)
                throw new InvalidDataException("RPC Stream Invalid(Decode)");
            return rpcCallInfo;
        }

        public static ByteBlock EncodeRpcRespInfo(RpcRespInfo resp, ByteBlock buff)
        {

            MemoryStream wStream = new MemoryStream(buff.Buffer, buff.WritePosition, buff.Space);
            wStream.WriteByte(RPC_H);
            String respJson = JsonConvert.SerializeObject(resp);
            String respBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(respJson));
            Byte[] respBytes = Encoding.UTF8.GetBytes(respBase64);
            wStream.Write(respBytes, 0, respBytes.Length);
            wStream.WriteByte(RPC_T);
            buff.WriteOffset((Int32)wStream.Position);

            return buff;
        }

        //public static Byte[] EncodeRpcRespInfo(RpcRespInfo resp)
        //{

        //    MemoryStream wStream = new MemoryStream();
        //    wStream.WriteByte(RPC_H);
        //    String respJson = JsonConvert.SerializeObject(resp);
        //    String respBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(respJson));
        //    Byte[] respBytes = Encoding.UTF8.GetBytes(respBase64);
        //    wStream.Write(respBytes, 0, respBytes.Length);
        //    wStream.WriteByte(RPC_T);

        //    Byte[] buff = new Byte[wStream.Position];
        //    Array.Copy(wStream.GetBuffer(), 0, buff, 0, wStream.Position);
        //    return buff;
        //}

        public static RpcRespInfo DecodeRpcRespInfo(ByteBlock buff)
        {
            Byte[] cache = buff.Buffer;
            Int32 offset = buff.ReadPosition;
            Int32 total = buff.Length;

            /// 不足一个包的长度
            if (total < sizeof(byte) * 2)
                return null;

            Int32 h = -1;
            Int32 t = -1;
            for (Int32 i = 0; i < total; ++i)
            {
                /// 检索包头标记
                if (cache[offset + i] == RPC_H)
                    h = i;

                if (cache[offset + i] == RPC_T)
                    t = i;

                if (h >= 0 && t >= 0)
                    break;
            }

            if (h < 0 || t < 0)
                return null;
            if (t <= h + 1)
                throw new InvalidDataException("RPC Stream Invalid(Location)");

            Int32 length = t - h - 1;
            Byte[] respBytes = new byte[length];
            Array.Copy(cache, offset + h + 1, respBytes, 0, length);
            buff.ReadOffset(length + 2);

            String respBase64 = Encoding.UTF8.GetString(respBytes, 0, length);
            String respJson = Encoding.UTF8.GetString(Convert.FromBase64String(respBase64));
            RpcRespInfo resp = JsonConvert.DeserializeObject<RpcRespInfo>(respJson);
            if (resp == null)
                throw new InvalidDataException("RPC Stream Invalid(Decode)");
            return resp;
        }
    }
}
