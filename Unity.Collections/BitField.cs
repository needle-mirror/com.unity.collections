using System;
using System.Diagnostics;
using Unity.Mathematics;

namespace Unity.Collections
{
    [BurstCompatible]
    internal unsafe struct Bitwise
    {
        internal static int AlignDown(int value, int alignPow2)
        {
            return value & ~(alignPow2 - 1);
        }

        internal static int AlignUp(int value, int alignPow2)
        {
            return AlignDown(value + alignPow2 - 1, alignPow2);
        }

        internal static int FromBool(bool value)
        {
            return value ? 1 : 0;
        }

        // 32-bit uint

        internal static uint ExtractBits(uint input, int pos, uint mask)
        {
            var tmp0 = input >> pos;
            return tmp0 & mask;
        }

        internal static uint ReplaceBits(uint input, int pos, uint mask, uint value)
        {
            var tmp0 = (value & mask) << pos;
            var tmp1 = input & ~(mask << pos);
            return tmp0 | tmp1;
        }

        internal static uint SetBits(uint input, int pos, uint mask, bool value)
        {
            return ReplaceBits(input, pos, mask, (uint)-FromBool(value));
        }

        // 64-bit ulong

        internal static ulong ExtractBits(ulong input, int pos, ulong mask)
        {
            var tmp0 = input >> pos;
            return tmp0 & mask;
        }

        internal static ulong ReplaceBits(ulong input, int pos, ulong mask, ulong value)
        {
            var tmp0 = (value & mask) << pos;
            var tmp1 = input & ~(mask << pos);
            return tmp0 | tmp1;
        }

        internal static ulong SetBits(ulong input, int pos, ulong mask, bool value)
        {
            return ReplaceBits(input, pos, mask, (ulong)-(long)FromBool(value));
        }

        internal static int lzcnt(byte value)
        {
            return math.lzcnt((uint)value) - 24;
        }

        internal static int tzcnt(byte value)
        {
            return math.min(8, math.tzcnt((uint)value));
        }

        internal static int lzcnt(ushort value)
        {
            return math.lzcnt((uint)value) - 16;
        }

        internal static int tzcnt(ushort value)
        {
            return math.min(16, math.tzcnt((uint)value));
        }

        static int FindUlong(ulong* ptr, int beginBit, int endBit, int numBits)
        {
            var bits = ptr;
            var numSteps = (numBits + 63) >> 6;
            var numBitsPerStep = 64;
            var maxBits = numSteps * numBitsPerStep;

            for (int i = beginBit / numBitsPerStep, end = AlignUp(endBit, numBitsPerStep) / numBitsPerStep; i < end; ++i)
            {
                if (bits[i] != 0)
                {
                    continue;
                }

                var idx = i * numBitsPerStep;
                var num = math.min(idx + numBitsPerStep, endBit) - idx;

                if (idx != beginBit)
                {
                    var test = bits[idx / numBitsPerStep - 1];
                    var newIdx = math.max(idx - math.lzcnt(test), beginBit);

                    num += idx - newIdx;
                    idx = newIdx;
                }

                for (++i; i < end; ++i)
                {
                    if (num >= numBits)
                    {
                        return idx;
                    }

                    var test = bits[i];
                    var pos = i * numBitsPerStep;
                    num += math.min(pos + math.tzcnt(test), endBit) - pos;

                    if (test != 0)
                    {
                        break;
                    }
                }

                if (num >= numBits)
                {
                    return idx;
                }
            }

            return endBit;
        }

        static int FindUint(ulong* ptr, int beginBit, int endBit, int numBits)
        {
            var bits = (uint*)ptr;
            var numSteps = (numBits + 31) >> 5;
            var numBitsPerStep = 32;
            var maxBits = numSteps * numBitsPerStep;

            for (int i = beginBit / numBitsPerStep, end = AlignUp(endBit, numBitsPerStep) / numBitsPerStep; i < end; ++i)
            {
                if (bits[i] != 0)
                {
                    continue;
                }

                var idx = i * numBitsPerStep;
                var num = math.min(idx + numBitsPerStep, endBit) - idx;

                if (idx != beginBit)
                {
                    var test = bits[idx / numBitsPerStep - 1];
                    var newIdx = math.max(idx - math.lzcnt(test), beginBit);

                    num += idx - newIdx;
                    idx = newIdx;
                }

                for (++i; i < end; ++i)
                {
                    if (num >= numBits)
                    {
                        return idx;
                    }

                    var test = bits[i];
                    var pos = i * numBitsPerStep;
                    num += math.min(pos + math.tzcnt(test), endBit) - pos;

                    if (test != 0)
                    {
                        break;
                    }
                }

                if (num >= numBits)
                {
                    return idx;
                }
            }

            return endBit;
        }

        static int FindUshort(ulong* ptr, int beginBit, int endBit, int numBits)
        {
            var bits = (ushort*)ptr;
            var numSteps = (numBits + 15) >> 4;
            var numBitsPerStep = 16;
            var maxBits = numSteps * numBitsPerStep;

            for (int i = beginBit / numBitsPerStep, end = AlignUp(endBit, numBitsPerStep) / numBitsPerStep; i < end; ++i)
            {
                if (bits[i] != 0)
                {
                    continue;
                }

                var idx = i * numBitsPerStep;
                var num = math.min(idx + numBitsPerStep, endBit) - idx;

                if (idx != beginBit)
                {
                    var test = bits[idx / numBitsPerStep - 1];
                    var newIdx = math.max(idx - lzcnt(test), beginBit);

                    num += idx - newIdx;
                    idx = newIdx;
                }

                for (++i; i < end; ++i)
                {
                    if (num >= numBits)
                    {
                        return idx;
                    }

                    var test = bits[i];
                    var pos = i * numBitsPerStep;
                    num += math.min(pos + tzcnt(test), endBit) - pos;

                    if (test != 0)
                    {
                        break;
                    }
                }

                if (num >= numBits)
                {
                    return idx;
                }
            }

            return endBit;
        }

        static int FindByte(ulong* ptr, int beginBit, int endBit, int numBits)
        {
            var bits = (byte*)ptr;
            var numSteps = (numBits + 7) >> 3;
            var numBitsPerStep = 8;
            var maxBits = numSteps * numBitsPerStep;

            for (int i = beginBit / numBitsPerStep, end = AlignUp(endBit, numBitsPerStep) / numBitsPerStep; i < end; ++i)
            {
                if (bits[i] != 0)
                {
                    continue;
                }

                var idx = i * numBitsPerStep;
                var num = math.min(idx + numBitsPerStep, endBit) - idx;

                if (idx != beginBit)
                {
                    var test = bits[idx / numBitsPerStep - 1];
                    var newIdx = math.max(idx - lzcnt(test), beginBit);

                    num += idx - newIdx;
                    idx = newIdx;
                }

                for (++i; i < end; ++i)
                {
                    if (num >= numBits)
                    {
                        return idx;
                    }

                    var test = bits[i];
                    var pos = i * numBitsPerStep;
                    num += math.min(pos + tzcnt(test), endBit) - pos;

                    if (test != 0)
                    {
                        break;
                    }
                }

                if (num >= numBits)
                {
                    return idx;
                }
            }

            return endBit;
        }

        static int FindUpto14bits(ulong* ptr, int beginBit, int endBit, int numBits)
        {
            var bits = (byte*)ptr;

            var bit = (byte)(beginBit & 7);
            byte beginMask = (byte)~(0xff << bit);

            var lz = 0;
            for (int begin = beginBit / 8, end = AlignUp(endBit, 8) / 8, i = begin; i < end; ++i)
            {
                var test = bits[i];
                test |= i == begin ? beginMask : (byte)0;

                if (test == 0xff)
                {
                    continue;
                }

                var pos = i * 8;
                var tz = math.min(pos + tzcnt(test), endBit) - pos;

                if (lz + tz >= numBits)
                {
                    return pos - lz;
                }

                lz = lzcnt(test);

                var idx = pos + 8;
                var newIdx = math.max(idx - lz, beginBit);
                lz = math.min(idx, endBit) - newIdx;

                if (lz >= numBits)
                {
                    return newIdx;
                }
            }

            return endBit;
        }

        static int FindUpto6bits(ulong* ptr, int beginBit, int endBit, int numBits)
        {
            var bits = (byte*)ptr;

            byte beginMask = (byte)~(0xff << (beginBit & 7));
            byte endMask = (byte)~(0xff >> ((8-(endBit & 7) & 7)));

            var mask = 1 << numBits - 1;

            for (int begin = beginBit / 8, end = AlignUp(endBit, 8) / 8, i = begin; i < end; ++i)
            {
                var test = bits[i];
                test |= i == begin ? beginMask : (byte)0;
                test |= i == end - 1 ? endMask : (byte)0;

                if (test == 0xff)
                {
                    continue;
                }

                for (int pos = i * 8, posEnd = pos + 7; pos < posEnd; ++pos)
                {
                    var tz = tzcnt((byte)(test^0xff));
                    test >>= tz;

                    pos += tz;

                    if ((test & mask) == 0)
                    {
                        return pos;
                    }

                    test >>= 1;
                }
            }

            return endBit;
        }

        internal static int FindWithBeginEnd(ulong* ptr, int beginBit, int endBit, int numBits)
        {
            int idx;

            if (numBits >= 127)
            {
                idx = FindUlong(ptr, beginBit, endBit, numBits);
                if (idx != endBit)
                {
                    return idx;
                }
            }

            if (numBits >= 63)
            {
                idx = FindUint(ptr, beginBit, endBit, numBits);
                if (idx != endBit)
                {
                    return idx;
                }
            }

            if (numBits >= 128)
            {
                // early out - no smaller step will find this gap
                return int.MaxValue;
            }

            if (numBits >= 31)
            {
                idx = FindUshort(ptr, beginBit, endBit, numBits);
                if (idx != endBit)
                {
                    return idx;
                }
            }

            if (numBits >= 64)
            {
                // early out - no smaller step will find this gap
                return int.MaxValue;
            }

            idx = FindByte(ptr, beginBit, endBit, numBits);
            if (idx != endBit)
            {
                return idx;
            }

            if (numBits < 15)
            {
                idx = FindUpto14bits(ptr, beginBit, endBit, numBits);

                if (idx != endBit)
                {
                    return idx;
                }

                if (numBits < 7)
                {
                    // The worst case scenario when every byte boundary bit is set (pattern 0x81),
                    // and we're looking for 6 or less bits. It will rescan byte-by-byte to find
                    // any inner byte gap.
                    idx = FindUpto6bits(ptr, beginBit, endBit, numBits);

                    if (idx != endBit)
                    {
                        return idx;
                    }
                }
            }

            return int.MaxValue;
        }

        internal static int Find(ulong* ptr, int pos, int count, int numBits) => FindWithBeginEnd(ptr, pos, pos + count, numBits);
    }

    /// <summary>
    /// Fixed size 32-bit array of bits.
    /// </summary>
    [DebuggerTypeProxy(typeof(BitField32DebugView))]
    [BurstCompatible]
    public struct BitField32
    {
        /// <summary>
        /// 32-bit data storage.
        /// </summary>
        public uint Value;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initialValue">Initial value of bit field. Default is 0.</param>
        public BitField32(uint initialValue = 0u)
        {
            Value = initialValue;
        }

        /// <summary>
        /// Clear all bits to 0.
        /// </summary>
        public void Clear()
        {
            Value = 0u;
        }

        /// <summary>
        /// Set single bit to desired boolean value.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-31).</param>
        /// <param name="value">Value of bits to set.</param>
        public void SetBits(int pos, bool value)
        {
            CheckArgs(pos, 1);
            Value = Bitwise.SetBits(Value, pos, 1, value);
        }

        /// <summary>
        /// Set bits to desired boolean value.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-31).</param>
        /// <param name="value">Value of bits to set.</param>
        /// <param name="numBits">Number of bits to set (must be 1-32).</param>
        public void SetBits(int pos, bool value, int numBits)
        {
            CheckArgs(pos, numBits);
            var mask = 0xffffffffu >> (32 - numBits);
            Value = Bitwise.SetBits(Value, pos, mask, value);
        }

        /// <summary>
        /// Returns all bits in range as uint.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-31).</param>
        /// <param name="numBits">Number of bits to get (must be 1-32).</param>
        /// <returns>Returns requested range of bits.</returns>
        public uint GetBits(int pos, int numBits = 1)
        {
            CheckArgs(pos, numBits);
            var mask = 0xffffffffu >> (32 - numBits);
            return Bitwise.ExtractBits(Value, pos, mask);
        }

        /// <summary>
        /// Returns true is bit at position is set.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-31).</param>
        /// <returns>Returns true if bit is set.</returns>
        public bool IsSet(int pos)
        {
            return 0 != GetBits(pos);
        }

        /// <summary>
        /// Returns true if none of bits in range are set.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-31).</param>
        /// <param name="numBits">Number of bits to test (must be 1-32).</param>
        /// <returns>Returns true if none of bits are set.</returns>
        public bool TestNone(int pos, int numBits = 1)
        {
            return 0u == GetBits(pos, numBits);
        }

        /// <summary>
        /// Returns true if any of bits in range are set.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-31).</param>
        /// <param name="numBits">Number of bits to test (must be 1-32).</param>
        /// <returns>Returns true if at least one bit is set.</returns>
        public bool TestAny(int pos, int numBits = 1)
        {
            return 0u != GetBits(pos, numBits);
        }

        /// <summary>
        /// Returns true if all of bits in range are set.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-31).</param>
        /// <param name="numBits">Number of bits to test (must be 1-32).</param>
        /// <returns>Returns true if all bits are set.</returns>
        public bool TestAll(int pos, int numBits = 1)
        {
            CheckArgs(pos, numBits);
            var mask = 0xffffffffu >> (32 - numBits);
            return mask == Bitwise.ExtractBits(Value, pos, mask);
        }

        /// <summary>
        /// Calculate number of set bits.
        /// </summary>
        /// <returns>Number of set bits.</returns>
        public int CountBits()
        {
            return math.countbits(Value);
        }

        /// <summary>
        /// Calculate number of leading zeros.
        /// </summary>
        /// <returns>Number of leading zeros</returns>
        public int CountLeadingZeros()
        {
            return math.lzcnt(Value);
        }

        /// <summary>
        /// Calculate number of trailing zeros.
        /// </summary>
        /// <returns>Number of trailing zeros</returns>
        public int CountTrailingZeros()
        {
            return math.tzcnt(Value);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckArgs(int pos, int numBits)
        {
            if (pos > 31
                || numBits == 0
                || numBits > 32
                || pos + numBits > 32)
            {
                throw new ArgumentException($"BitField32 invalid arguments: pos {pos} (must be 0-31), numBits {numBits} (must be 1-32).");
            }
        }
    }

    sealed class BitField32DebugView
    {
        BitField32 BitField;

        public BitField32DebugView(BitField32 bitfield)
        {
            BitField = bitfield;
        }

        public bool[] Bits
        {
            get
            {
                var array = new bool[32];
                for (int i = 0; i < 32; ++i)
                {
                    array[i] = BitField.IsSet(i);
                }
                return array;
            }
        }
    }

    /// <summary>
    /// Fixed size 64-bit array of bits.
    /// </summary>
    [DebuggerTypeProxy(typeof(BitField64DebugView))]
    [BurstCompatible]
    public struct BitField64
    {
        /// <summary>
        /// 64-bit data storage.
        /// </summary>
        public ulong Value;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="initialValue">Initial value of bit field. Default is 0.</param>
        public BitField64(ulong initialValue = 0ul)
        {
            Value = initialValue;
        }

        /// <summary>
        /// Clear all bits to 0.
        /// </summary>
        public void Clear()
        {
            Value = 0ul;
        }

        /// <summary>
        /// Set bits to desired boolean value.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-63).</param>
        /// <param name="value">Value of bits to set.</param>
        /// <param name="numBits">Number of bits to set (must be 1-64).</param>
        public void SetBits(int pos, bool value, int numBits = 1)
        {
            CheckArgs(pos, numBits);
            var mask = 0xfffffffffffffffful >> (64 - numBits);
            Value = Bitwise.SetBits(Value, pos, mask, value);
        }

        /// <summary>
        /// Returns all bits in range as uint.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-63).</param>
        /// <param name="numBits">Number of bits to set (must be 1-64).</param>
        /// <returns>Returns requested range of bits.</returns>
        public ulong GetBits(int pos, int numBits = 1)
        {
            CheckArgs(pos, numBits);
            var mask = 0xfffffffffffffffful >> (64 - numBits);
            return Bitwise.ExtractBits(Value, pos, mask);
        }

        /// <summary>
        /// Returns true is bit at position is set.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-31).</param>
        /// <returns>Returns true if bit is set.</returns>
        public bool IsSet(int pos)
        {
            return 0ul != GetBits(pos);
        }

        /// <summary>
        /// Returns true if none of bits in range are set.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-63).</param>
        /// <param name="numBits">Number of bits to set (must be 1-64).</param>
        /// <returns>Returns true if none of bits are set.</returns>
        public bool TestNone(int pos, int numBits = 1)
        {
            return 0ul == GetBits(pos, numBits);
        }

        /// <summary>
        /// Returns true if any of bits in range are set.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-63).</param>
        /// <param name="numBits">Number of bits to set (must be 1-64).</param>
        /// <returns>Returns true if at least one bit is set.</returns>
        public bool TestAny(int pos, int numBits = 1)
        {
            return 0ul != GetBits(pos, numBits);
        }

        /// <summary>
        /// Returns true if all of bits in range are set.
        /// </summary>
        /// <param name="pos">Position in bitfield (must be 0-63).</param>
        /// <param name="numBits">Number of bits to set (must be 1-64).</param>
        /// <returns>Returns true if all bits are set.</returns>
        public bool TestAll(int pos, int numBits = 1)
        {
            CheckArgs(pos, numBits);
            var mask = 0xfffffffffffffffful >> (64 - numBits);
            return mask == Bitwise.ExtractBits(Value, pos, mask);
        }

        /// <summary>
        /// Calculate number of set bits.
        /// </summary>
        /// <returns>Number of set bits.</returns>
        public int CountBits()
        {
            return math.countbits(Value);
        }

        /// <summary>
        /// Calculate number of leading zeros.
        /// </summary>
        /// <returns>Number of leading zeros</returns>
        public int CountLeadingZeros()
        {
            return math.lzcnt(Value);
        }

        /// <summary>
        /// Calculate number of trailing zeros.
        /// </summary>
        /// <returns>Number of trailing zeros</returns>
        public int CountTrailingZeros()
        {
            return math.tzcnt(Value);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckArgs(int pos, int numBits)
        {
            if (pos > 63
                || numBits == 0
                || numBits > 64
                || pos + numBits > 64)
            {
                throw new ArgumentException($"BitField32 invalid arguments: pos {pos} (must be 0-63), numBits {numBits} (must be 1-64).");
            }
        }
    }

    sealed class BitField64DebugView
    {
        BitField64 Data;

        public BitField64DebugView(BitField64 data)
        {
            Data = data;
        }

        public bool[] Bits
        {
            get
            {
                var array = new bool[64];
                for (int i = 0; i < 64; ++i)
                {
                    array[i] = Data.IsSet(i);
                }
                return array;
            }
        }
    }
}
