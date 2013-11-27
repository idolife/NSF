using System;
using System.Diagnostics;

namespace NSF.Framework.Base
{
    /// <summary>
    /// 字节数组缓存管理类。
    /// </summary>
    class ByteBlock
    {
        Byte[] _Cache;
        Int32 _RdPtr;
        Int32 _WrPtr;
        /// <summary>
        /// 获取当前读指针的偏移量。
        /// </summary>
        public Int32 ReadPosition { get { return _RdPtr; } }
        /// <summary>
        /// 移动当前读指针的位置。
        /// </summary>
        /// <param name="offset">移动的偏移量。</param>
        /// <returns>当前的读指针偏移量。</returns>
        public Int32 ReadOffset(Int32 offset)
        {
            /// 不允许移动到超出当前数据的末尾。
            Debug.Assert(_RdPtr + offset <= _WrPtr);
            /// 不允许移动到存储开始位置之前
            Debug.Assert(_RdPtr + offset >= 0);

            _RdPtr += offset;
            return _RdPtr;
        }
        /// <summary>
        /// 获取当前写指针的偏移量。
        /// </summary>
        public Int32 WritePosition { get { return _WrPtr; } }
        /// <summary>
        /// 移动当前写指针的位置。
        /// </summary>
        /// <param name="offset">移动的偏移量。</param>
        /// <returns>当前的写指针的偏移量。</returns>
        public Int32 WriteOffset(Int32 offset)
        {
            /// 不允许移动到当前读指针之前。
            Debug.Assert(_WrPtr + offset >= _RdPtr);
            /// 不允许移动到存储结束位置之后。
            Debug.Assert(_WrPtr + offset <= _Cache.Length);

            _WrPtr += offset;
            return _WrPtr;
        }
        /// <summary>
        /// 获取完整空间的长度。
        /// </summary>
        public Int32 Total
        {
            get
            {
                return _Cache.Length;
            }
        }
        /// <summary>
        /// 获取当前数据长度。
        /// </summary>
        public Int32 Length
        {
            get
            {
                return (_WrPtr - _RdPtr);
            }
        }
        /// <summary>
        /// 获取当前当前可连续写的空间长度。
        /// </summary>
        public Int32 Space
        {
            get
            {
                /// 仅仅返回可连续写入的空间长度
                return (_Cache.Length - _WrPtr);
            }
        }
        /// <summary>
        /// 获取底层缓存。
        /// </summary>
        public Byte[] Buffer
        {
            get
            {
                /// 需要通过Offset及Length属性来工作
                return (_Cache);
            }
        }
        /// <summary>
        /// 默认构造函数。
        /// </summary>
        /// <param name="space">可容纳的字节数。</param>
        public ByteBlock(Int32 space)
        {
            _Cache = new Byte[space];
            _RdPtr = 0;
            _WrPtr = 0;
        }
        /// <summary>
        /// 整理缓存以获得更大的连续写空间。
        /// </summary>
        public void Crunch()
        {
            if (_RdPtr == 0)
                return;

            /// 简单"移动"
            if (Length == 0)
            {
                _RdPtr = 0;
                _WrPtr = 0;
            }
            /// 真实移动
            else
            {
                Int32 length = Length;
                /// 重叠移动安全
                Array.Copy(_Cache, _RdPtr, _Cache, 0, length);
                _RdPtr = 0;
                _WrPtr = length;
            }
        }

        public void Reset()
        {
            _RdPtr = 0;
            _WrPtr = 0;
        }
    }    

}
