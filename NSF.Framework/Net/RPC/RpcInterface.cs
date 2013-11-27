using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSF.Framework.Net.RPC
{
    public interface RpcInterface
    {
        /// <summary>
        /// 当某调用无应答时返回null.
        /// </summary>
        Task<String> OnCall(String method, String args);
    }
}
