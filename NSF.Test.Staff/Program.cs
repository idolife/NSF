using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSF.Share;

namespace NSF.Test.Staff
{
    class Program
    {
        static void Main(string[] args)
        {
            /// 测试GZip压缩/解压
            var v1 = Util.GZipCompressString("这个是要测试的中文字符串", Encoding.UTF8);
            var v2 = Util.GZipDecompressString(v1, Encoding.UTF8);

            dynamic d;
        }
    }
}
