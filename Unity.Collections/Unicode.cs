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


}
