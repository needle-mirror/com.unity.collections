using System;
using System.Diagnostics;
using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// </summary>
    public enum FormatError
    {
        /// <summary>
        /// </summary>
        None,

        /// <summary>
        /// </summary>
        Overflow,
    }

    /// <summary>
    /// </summary>
    public enum ParseError
    {
        /// <summary>
        /// </summary>
        None,

        /// <summary>
        /// </summary>
        Syntax,

        /// <summary>
        /// </summary>
        Overflow,

        /// <summary>
        /// </summary>
        Underflow,
    }

    /// <summary>
    /// </summary>
    public enum CopyError
    {
        /// <summary>
        /// </summary>
        None,

        /// <summary>
        /// </summary>
        Truncation,
    }

    /// <summary>
    /// </summary>
    public enum ConversionError
    {
        /// <summary>
        /// </summary>
        None,

        /// <summary>
        /// </summary>
        Overflow,

        /// <summary>
        /// </summary>
        Encoding,

        /// <summary>
        /// </summary>
        CodePoint,
    }

    /// <summary>
    ///
    /// </summary>
    [BurstCompatible]
    public unsafe struct Unicode
    {
        /// <summary>
        ///
        /// </summary>
        [BurstCompatible]
        public struct Rune
        {
            /// <summary>
            ///
            /// </summary>
            public int value;

            /// <summary>
            /// Construct a rune for the given unicode code point.  No validation
            /// is done to check whether the code point is valid.
            /// </summary>
            /// <param name="codepoint">The codepoint</param>
            public Rune(int codepoint)
            {
                value = codepoint;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            public static explicit operator Rune(char c) => new Rune { value = c };

            /// <summary>
            ///
            /// </summary>
            /// <param name="c"></param>
            /// <returns></returns>
            public static bool IsDigit(Rune c)
            {
                return c.value >= '0' && c.value <= '9';
            }

            /// <summary>
            /// Returns the number of UTF-8 bytes required to encode this Rune.  If the Rune's codepoint
            /// value is invalid, returns 4 (maximum possible encoding length).
            /// </summary>
            /// <returns>Number of bytes required to encode this Rune as UTF-8.</returns>
            public int LengthInUtf8Bytes()
            {
                if (value < 0)
                    return 4; // invalid codepoint
                if (value <= 0x7F)
                    return 1;
                if (value <= 0x7FF)
                    return 2;
                if (value <= 0xFFFF)
                    return 3;
                if (value <= 0x1FFFFF)
                    return 4;
                // invalid codepoint, max size.
                return 4;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ucs"></param>
        /// <returns></returns>
        public static bool IsValidCodePoint(int ucs)
        {
            if (ucs > 0x10FFFF) // maximum valid code point
                return false;
//            if (ucs >= 0xD800 && ucs <= 0xDFFF) // surrogate pair
//                return false;
            if (ucs < 0) // negative?
                return false;
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool NotTrailer(byte b)
        {
            return (b & 0xC0) != 0x80;
        }

        /// <summary>
        ///
        /// </summary>
        public static Rune ReplacementCharacter => new Rune { value = 0xFFFD };

        /// <summary>
        ///
        /// </summary>
        public static Rune BadRune => new Rune { value = 0 };

        /// <summary>
        ///
        /// </summary>
        /// <param name="rune"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static ConversionError Utf8ToUcs(out Rune rune, byte* buffer, ref int offset, int capacity)
        {
            int code = 0;
            rune = ReplacementCharacter;
            if (offset + 1 > capacity)
            {
                return ConversionError.Overflow;
            }

            if ((buffer[offset] & 0b10000000) == 0b00000000) // if high bit is 0, 1 byte
            {
                rune.value = buffer[offset + 0];
                offset += 1;
                return ConversionError.None;
            }

            if ((buffer[offset] & 0b11100000) == 0b11000000) // if high 3 bits are 110, 2 bytes
            {
                if (offset + 2 > capacity)
                {
                    offset += 1;
                    return ConversionError.Overflow;
                }
                code = (buffer[offset + 0] & 0b00011111);
                code = (code << 6) | (buffer[offset + 1] & 0b00111111);
                if (code < (1 << 7) || NotTrailer(buffer[offset + 1]))
                {
                    offset += 1;
                    return ConversionError.Encoding;
                }
                rune.value = code;
                offset += 2;
                return ConversionError.None;
            }

            if ((buffer[offset] & 0b11110000) == 0b11100000) // if high 4 bits are 1110, 3 bytes
            {
                if (offset + 3 > capacity)
                {
                    offset += 1;
                    return ConversionError.Overflow;
                }
                code = (buffer[offset + 0] & 0b00001111);
                code = (code << 6) | (buffer[offset + 1] & 0b00111111);
                code = (code << 6) | (buffer[offset + 2] & 0b00111111);
                if (code < (1 << 11) || !IsValidCodePoint(code) || NotTrailer(buffer[offset + 1]) || NotTrailer(buffer[offset + 2]))
                {
                    offset += 1;
                    return ConversionError.Encoding;
                }
                rune.value = code;
                offset += 3;
                return ConversionError.None;
            }

            if ((buffer[offset] & 0b11111000) == 0b11110000) // if high 5 bits are 11110, 4 bytes
            {
                if (offset + 4 > capacity)
                {
                    offset += 1;
                    return ConversionError.Overflow;
                }
                code = (buffer[offset + 0] & 0b00000111);
                code = (code << 6) | (buffer[offset + 1] & 0b00111111);
                code = (code << 6) | (buffer[offset + 2] & 0b00111111);
                code = (code << 6) | (buffer[offset + 3] & 0b00111111);
                if (code < (1 << 16) || !IsValidCodePoint(code) || NotTrailer(buffer[offset + 1]) || NotTrailer(buffer[offset + 2]) || NotTrailer(buffer[offset + 3]))
                {
                    offset += 1;
                    return ConversionError.Encoding;
                }
                rune.value = code;
                offset += 4;
                return ConversionError.None;
            }

            offset += 1;
            return ConversionError.Encoding;
        }

        static bool IsLeadingSurrogate(char c)
        {
            return c >= 0xD800 && c <= 0xDBFF;
        }

        static bool IsTrailingSurrogate(char c)
        {
            return c >= 0xDC00 && c <= 0xDFFF;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="rune"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static ConversionError Utf16ToUcs(out Rune rune, char* buffer, ref int offset, int capacity)
        {
            int code = 0;
            rune = ReplacementCharacter;
            if (offset + 1 > capacity)
                return ConversionError.Overflow;
            if (!IsLeadingSurrogate(buffer[offset]) || (offset + 2 > capacity))
            {
                rune.value = buffer[offset];
                offset += 1;
                return ConversionError.None;
            }
            code =                (buffer[offset + 0] & 0x03FF);
            char next = buffer[offset + 1];
            if (!IsTrailingSurrogate(next))
            {
                rune.value = buffer[offset];
                offset += 1;
                return ConversionError.None;
            }
            code = (code << 10) | (buffer[offset + 1] & 0x03FF);
            code += 0x10000;
            rune.value = code;
            offset += 2;
            return ConversionError.None;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="capacity"></param>
        /// <param name="rune"></param>
        /// <returns></returns>
        public static ConversionError UcsToUtf8(byte* buffer, ref int offset, int capacity, Rune rune)
        {
            if (!IsValidCodePoint(rune.value))
            {
                return ConversionError.CodePoint;
            }

            if (offset + 1 > capacity)
            {
                return ConversionError.Overflow;
            }

            if (rune.value <= 0x7F)
            {
                buffer[offset++] = (byte)rune.value;
                return ConversionError.None;
            }

            if (rune.value <= 0x7FF)
            {
                if (offset + 2 > capacity)
                {
                    return ConversionError.Overflow;
                }

                buffer[offset++] = (byte)(0xC0 | (rune.value >> 6));
                buffer[offset++] = (byte)(0x80 | ((rune.value >> 0) & 0x3F));
                return ConversionError.None;
            }

            if (rune.value <= 0xFFFF)
            {
                if (offset + 3 > capacity)
                {
                    return ConversionError.Overflow;
                }

                buffer[offset++] = (byte)(0xE0 | (rune.value >> 12));
                buffer[offset++] = (byte)(0x80 | ((rune.value >> 6) & 0x3F));
                buffer[offset++] = (byte)(0x80 | ((rune.value >> 0) & 0x3F));
                return ConversionError.None;
            }

            if (rune.value <= 0x1FFFFF)
            {
                if (offset + 4 > capacity)
                {
                    return ConversionError.Overflow;
                }

                buffer[offset++] = (byte)(0xF0 | (rune.value >> 18));
                buffer[offset++] = (byte)(0x80 | ((rune.value >> 12) & 0x3F));
                buffer[offset++] = (byte)(0x80 | ((rune.value >> 6) & 0x3F));
                buffer[offset++] = (byte)(0x80 | ((rune.value >> 0) & 0x3F));
                return ConversionError.None;
            }

            return ConversionError.Encoding;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="capacity"></param>
        /// <param name="rune"></param>
        /// <returns></returns>
        public static ConversionError UcsToUtf16(char* buffer, ref int offset, int capacity, Rune rune)
        {
            if (!IsValidCodePoint(rune.value))
            {
                return ConversionError.CodePoint;
            }

            if (offset + 1 > capacity)
            {
                return ConversionError.Overflow;
            }

            if (rune.value >= 0x10000)
            {
                if (offset + 2 > capacity)
                {
                    return ConversionError.Overflow;
                }

                int code = rune.value - 0x10000;
                if (code >= (1 << 20))
                {
                    return ConversionError.Encoding;
                }

                buffer[offset++] = (char)(0xD800 | (code >> 10));
                buffer[offset++] = (char)(0xDC00 | (code & 0x3FF));
                return ConversionError.None;
            }

            buffer[offset++] = (char)rune.value;
            return ConversionError.None;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="utf16_buffer"></param>
        /// <param name="utf16_length"></param>
        /// <param name="utf8_buffer"></param>
        /// <param name="utf8_length"></param>
        /// <param name="utf8_capacity"></param>
        /// <returns></returns>
        public static ConversionError Utf16ToUtf8(char* utf16_buffer, int utf16_length, byte* utf8_buffer, out int utf8_length, int utf8_capacity)
        {
            utf8_length = 0;
            for (var utf16_offset = 0; utf16_offset < utf16_length;)
            {
                Utf16ToUcs(out var ucs, utf16_buffer, ref utf16_offset, utf16_length);
                if (UcsToUtf8(utf8_buffer, ref utf8_length, utf8_capacity, ucs) == ConversionError.Overflow)
                    return ConversionError.Overflow;
            }
            return ConversionError.None;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="src_buffer"></param>
        /// <param name="src_length"></param>
        /// <param name="dest_buffer"></param>
        /// <param name="dest_length"></param>
        /// <param name="dest_capacity"></param>
        /// <returns></returns>
        public static ConversionError Utf8ToUtf8(byte* src_buffer, int src_length, byte* dest_buffer, out int dest_length, int dest_capacity)
        {
            if (dest_capacity >= src_length)
            {
                UnsafeUtility.MemCpy(dest_buffer, src_buffer, src_length);
                dest_length = src_length;
                return ConversionError.None;
            }
            // TODO even in this case, it's possible to MemCpy all but the last 3 bytes that fit, and then by looking at only
            // TODO the high bits of the last 3 bytes that fit, decide how many of the 3 to append. but that requires a
            // TODO little UNICODE presence of mind that nobody has today.
            dest_length = 0;
            for (var src_offset = 0; src_offset < src_length;)
            {
                Utf8ToUcs(out var ucs, src_buffer, ref src_offset, src_length);
                if (UcsToUtf8(dest_buffer, ref dest_length, dest_capacity, ucs) == ConversionError.Overflow)
                    return ConversionError.Overflow;
            }
            return ConversionError.None;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="utf8_buffer"></param>
        /// <param name="utf8_length"></param>
        /// <param name="utf16_buffer"></param>
        /// <param name="utf16_length"></param>
        /// <param name="utf16_capacity"></param>
        /// <returns></returns>
        public static ConversionError Utf8ToUtf16(byte* utf8_buffer, int utf8_length, char* utf16_buffer, out int utf16_length, int utf16_capacity)
        {
            utf16_length = 0;
            for (var utf8_offset = 0; utf8_offset < utf8_length;)
            {
                Utf8ToUcs(out var ucs, utf8_buffer, ref utf8_offset, utf8_length);
                if (UcsToUtf16(utf16_buffer, ref utf16_length, utf16_capacity, ucs) == ConversionError.Overflow)
                    return ConversionError.Overflow;
            }
            return ConversionError.None;
        }
    }

    sealed class WordStorageDebugView
    {
        WordStorage m_wordStorage;

        public WordStorageDebugView(WordStorage wordStorage)
        {
            m_wordStorage = wordStorage;
        }

        public FixedString128[] Table
        {
            get
            {
                var table = new FixedString128[m_wordStorage.Entries];
                for (var i = 0; i < m_wordStorage.Entries; ++i)
                    m_wordStorage.GetFixedString(i, ref table[i]);
                return table;
            }
        }
    }

    sealed class WordStorageStatic
    {
        private WordStorageStatic()
        {
        }
        public struct Thing
        {
            public WordStorage Data;
        }
        public static Thing Ref = default;
    }

    /// <summary>
    ///
    /// </summary>
    [DebuggerTypeProxy(typeof(WordStorageDebugView))]
    [BurstCompatible]
    public struct WordStorage
    {
        struct Entry
        {
            public int offset;
            public int length;
        }

        NativeArray<byte> buffer; // all the UTF-8 encoded bytes in one place
        NativeArray<Entry> entry; // one offset for each text in "buffer"
        NativeMultiHashMap<int, int> hash; // from string hash to table entry
        int chars; // bytes in buffer allocated so far
        int entries; // number of strings allocated so far

        /// <summary>
        ///
        /// </summary>
        [NotBurstCompatible]
        public static ref WordStorage Instance
        {
            get
            {
                Initialize();
                return ref WordStorageStatic.Ref.Data;
            }
        }

        const int kMaxEntries = 16 << 10;
        const int kMaxChars = kMaxEntries * 128;

        /// <summary>
        ///
        /// </summary>
        public const int kMaxCharsPerEntry = 4096;

        /// <summary>
        ///
        /// </summary>
        public int Entries => entries;

        [NotBurstCompatible]
        public static void Initialize()
        {
            if (WordStorageStatic.Ref.Data.buffer.IsCreated)
                return;
            WordStorageStatic.Ref.Data.buffer = new NativeArray<byte>(kMaxChars, Allocator.Persistent);
            WordStorageStatic.Ref.Data.entry = new NativeArray<Entry>(kMaxEntries, Allocator.Persistent);
            WordStorageStatic.Ref.Data.hash = new NativeMultiHashMap<int, int>(kMaxEntries, Allocator.Persistent);
            Clear();
#if !UNITY_DOTSRUNTIME
            // Free storage on domain unload, which happens when iterating on the Entities module a lot.
            AppDomain.CurrentDomain.DomainUnload += (_, __) => { Shutdown(); };
#endif
        }

        /// <summary>
        ///
        /// </summary>
        [NotBurstCompatible]
        public static void Shutdown()
        {
            if (!WordStorageStatic.Ref.Data.buffer.IsCreated)
                return;
            WordStorageStatic.Ref.Data.buffer.Dispose();
            WordStorageStatic.Ref.Data.entry.Dispose();
            WordStorageStatic.Ref.Data.hash.Dispose();
            WordStorageStatic.Ref.Data = default;
        }

        [NotBurstCompatible]
        public static void Clear()
        {
            Initialize();
            WordStorageStatic.Ref.Data.chars = 0;
            WordStorageStatic.Ref.Data.entries = 0;
            WordStorageStatic.Ref.Data.hash.Clear();
            var temp = new FixedString32();
            WordStorageStatic.Ref.Data.GetOrCreateIndex(ref temp); // make sure that Index=0 means empty string
        }

        /// <summary>
        ///
        /// </summary>
        [NotBurstCompatible]
        public static void Setup()
        {
            Clear();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(FixedString32) })]
        public unsafe void GetFixedString<T>(int index, ref T temp)
        where T : IUTF8Bytes, INativeList<byte>
        {
            Assert.IsTrue(index < entries);
            var e = entry[index];
            Assert.IsTrue(e.length <= kMaxCharsPerEntry);
            temp.Length = e.length;
            UnsafeUtility.MemCpy(temp.GetUnsafePtr(), (byte*)buffer.GetUnsafePtr() + e.offset, temp.Length);
        }

        [BurstCompatible(GenericTypeArguments = new [] { typeof(FixedString32) })]
        public int GetIndexFromHashAndFixedString<T>(int h, ref T temp)
        where T : IUTF8Bytes, INativeList<byte>
        {
            Assert.IsTrue(temp.Length <= kMaxCharsPerEntry); // about one printed page of text
            int itemIndex;
            NativeMultiHashMapIterator<int> iter;
            if (hash.TryGetFirstValue(h, out itemIndex, out iter))
            {
                do
                {
                    var e = entry[itemIndex];
                    Assert.IsTrue(e.length <= kMaxCharsPerEntry);
                    if (e.length == temp.Length)
                    {
                        int matches;
                        for (matches = 0; matches < e.length; ++matches)
                            if (temp[matches] != buffer[e.offset + matches])
                                break;
                        if (matches == temp.Length)
                            return itemIndex;
                    }
                } while (hash.TryGetNextValue(out itemIndex, ref iter));
            }
            return -1;
        }

        [BurstCompatible(GenericTypeArguments = new [] { typeof(FixedString32) })]
        public bool Contains<T>(ref T value)
        where T : IUTF8Bytes, INativeList<byte>
        {
            int h = value.GetHashCode();
            return GetIndexFromHashAndFixedString(h, ref value) != -1;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [NotBurstCompatible]
        public unsafe bool Contains(string value)
        {
            FixedString512 temp = value;
            return Contains(ref temp);
        }

        [BurstCompatible(GenericTypeArguments = new [] { typeof(FixedString32) })]
        public int GetOrCreateIndex<T>(ref T value)
        where T : IUTF8Bytes, INativeList<byte>
        {
            int h = value.GetHashCode();
            var itemIndex = GetIndexFromHashAndFixedString(h, ref value);
            if (itemIndex != -1)
                return itemIndex;
            Assert.IsTrue(entries < kMaxEntries);
            Assert.IsTrue(chars + value.Length <= kMaxChars);
            var o = chars;
            var l = (ushort)value.Length;
            for (var i = 0; i < l; ++i)
                buffer[chars++] = value[i];
            entry[entries] = new Entry { offset = o, length = l };
            hash.Add(h, entries);
            return entries++;
        }
    }


    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// A "Words" is an integer that refers to 4,096 or fewer chars of UTF-16 text in a global storage blob.
    /// Each should refer to *at most* about one printed page of text.
    ///
    /// If you need more text, consider using one Words struct for each printed page's worth.
    ///
    /// Each Words instance that you create is stored in a single, internally-managed WordStorage object,
    /// which can hold up to 16,384 Words entries. Once added, the entries in WordStorage cannot be modified
    /// or removed.
    /// </remarks>
    public struct Words
    {
        int Index;

        public void ToFixedString<T>(ref T value)
        where T : IUTF8Bytes, INativeList<byte>
        {
            WordStorage.Instance.GetFixedString(Index, ref value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            FixedString512 temp = default;
            ToFixedString(ref temp);
            return temp.ToString();
        }

        public void SetFixedString<T>(ref T value)
        where T : IUTF8Bytes, INativeList<byte>
        {
            Index = WordStorage.Instance.GetOrCreateIndex(ref value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        public unsafe void SetString(string value)
        {
            FixedString512 temp = value;
            SetFixedString(ref temp);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// A "NumberedWords" is a "Words", plus possibly a string of leading zeroes, followed by
    /// possibly a positive integer.
    /// The zeroes and integer aren't stored centrally as a string, they're stored as an int.
    /// Therefore, 1,000,000 items with names from FooBarBazBifBoo000000 to FooBarBazBifBoo999999
    /// Will cost 8MB + a single copy of "FooBarBazBifBoo", instead of ~48MB.
    /// They say that this is a thing, too.
    /// </remarks>
    [BurstCompatible]
    public struct NumberedWords
    {
        int Index;
        int Suffix;

        const int kPositiveNumericSuffixShift = 0;
        const int kPositiveNumericSuffixBits = 29;
        const int kMaxPositiveNumericSuffix = (1 << kPositiveNumericSuffixBits) - 1;
        const int kPositiveNumericSuffixMask = (1 << kPositiveNumericSuffixBits) - 1;

        const int kLeadingZeroesShift = 29;
        const int kLeadingZeroesBits = 3;
        const int kMaxLeadingZeroes = (1 << kLeadingZeroesBits) - 1;
        const int kLeadingZeroesMask = (1 << kLeadingZeroesBits) - 1;

        int LeadingZeroes
        {
            get => (Suffix >> kLeadingZeroesShift) & kLeadingZeroesMask;
            set
            {
                Suffix &= ~(kLeadingZeroesMask << kLeadingZeroesShift);
                Suffix |= (value & kLeadingZeroesMask) << kLeadingZeroesShift;
            }
        }

        int PositiveNumericSuffix
        {
            get => (Suffix >> kPositiveNumericSuffixShift) & kPositiveNumericSuffixMask;
            set
            {
                Suffix &= ~(kPositiveNumericSuffixMask << kPositiveNumericSuffixShift);
                Suffix |= (value & kPositiveNumericSuffixMask) << kPositiveNumericSuffixShift;
            }
        }

        bool HasPositiveNumericSuffix => PositiveNumericSuffix != 0;

        [NotBurstCompatible]
        string NewString(char c, int count)
        {
            char[] temp = new char[count];
            for (var i = 0; i < count; ++i)
                temp[i] = c;
            return new string(temp, 0, count);
        }

        [NotBurstCompatible]
        public int ToFixedString<T>(ref T result)
        where T : IUTF8Bytes, INativeList<byte>
        {
            unsafe
            {
                var positiveNumericSuffix = PositiveNumericSuffix;
                var leadingZeroes = LeadingZeroes;

                WordStorage.Instance.GetFixedString(Index, ref result);
                if(positiveNumericSuffix == 0 && leadingZeroes == 0)
                    return 0;

                // print the numeric suffix, if any, backwards, as ASCII, to a little buffer.
                const int maximumDigits = kMaxLeadingZeroes + 10;
                var buffer = stackalloc byte[maximumDigits];
                var firstDigit = maximumDigits;
                while(positiveNumericSuffix > 0)
                {
                    buffer[--firstDigit] = (byte)('0' + positiveNumericSuffix % 10);
                    positiveNumericSuffix /= 10;
                }
                while(leadingZeroes-- > 0)
                    buffer[--firstDigit] = (byte)'0';

                // make space in the output for leading zeroes if any, followed by the positive numeric index if any.
                var dest = result.GetUnsafePtr() + result.Length;
                result.Length += maximumDigits - firstDigit;
                while(firstDigit < maximumDigits)
                    *dest++ = buffer[firstDigit++];
                return 0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        [NotBurstCompatible]
        public override string ToString()
        {
            FixedString512 temp = default;
            ToFixedString(ref temp);
            return temp.ToString();
        }

        bool IsDigit(byte b)
        {
            return b >= '0' && b <= '9';
        }

        [NotBurstCompatible]
        public void SetString<T>(ref T value)
        where T : IUTF8Bytes, INativeList<byte>
        {
            int beginningOfDigits = value.Length;

            // as long as there are digits at the end,
            // look back for more digits.

            while (beginningOfDigits > 0 && IsDigit(value[beginningOfDigits - 1]))
                --beginningOfDigits;

            // as long as the first digit is a zero, it's not the beginning of the positive integer - it's a leading zero.

            var beginningOfPositiveNumericSuffix = beginningOfDigits;
            while (beginningOfPositiveNumericSuffix < value.Length && value[beginningOfPositiveNumericSuffix] == '0')
                ++beginningOfPositiveNumericSuffix;

            // now we know where the leading zeroes begin, and then where the positive integer begins after them.
            // but if there are too many leading zeroes to encode, the excess ones become part of the string.

            var leadingZeroes = beginningOfPositiveNumericSuffix - beginningOfDigits;
            if (leadingZeroes > kMaxLeadingZeroes)
            {
                var excessLeadingZeroes = leadingZeroes - kMaxLeadingZeroes;
                beginningOfDigits += excessLeadingZeroes;
                leadingZeroes -= excessLeadingZeroes;
            }

            // if there is a positive integer after the zeroes, here's where we compute it and store it for later.

            PositiveNumericSuffix = 0;
            {
                int number = 0;
                for (var i = beginningOfPositiveNumericSuffix; i < value.Length; ++i)
                {
                    number *= 10;
                    number += value[i] - '0';
                }

                // an intrepid user may attempt to encode a positive integer with 20 digits or something.
                // they are rewarded with a string that is encoded wholesale without any optimizations.

                if (number <= kMaxPositiveNumericSuffix)
                    PositiveNumericSuffix = number;
                else
                {
                    beginningOfDigits = value.Length;
                    leadingZeroes = 0; // and your dog Toto, too.
                }
            }

            // set the leading zero count in the Suffix member.

            LeadingZeroes = leadingZeroes;

            // truncate the string, if there were digits at the end that we encoded.
            var truncated = value;
            int length = truncated.Length;
            if (beginningOfDigits != truncated.Length)
                truncated.Length = beginningOfDigits;

            // finally, set the string to its index in the global string blob thing.

            unsafe
            {
                Index = WordStorage.Instance.GetOrCreateIndex(ref truncated);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        [NotBurstCompatible]
        public void SetString(string value)
        {
            FixedString512 temp = value;
            SetString(ref temp);
        }
    }
}
