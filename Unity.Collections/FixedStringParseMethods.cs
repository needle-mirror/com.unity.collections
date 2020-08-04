using System;

namespace Unity.Collections
{
    /// <summary>
    /// <undoc />
    /// </summary>
    public unsafe static partial class FixedStringMethods
    {
        /// <summary>
        /// Parse an int from this string, at the given byte offset. The resulting value
        /// is intended to be bitwise-identical to the output of int.Parse().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="offset">The zero-based byte offset from the beginning of the string.</param>
        /// <param name="output">The int parsed, if any.</param>
        /// <returns>An error code, if any, in the case that the parse fails.</returns>
        public static ParseError Parse<T>(ref this T fs, ref int offset, ref int output)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            long value = 0;
            int sign = 1;
            int digits = 0;
            if (offset < fs.Length)
            {
                if (fs.Peek(offset).value == '+')
                    fs.Read(ref offset);
                else if (fs.Peek(offset).value == '-')
                {
                    sign = -1;
                    fs.Read(ref offset);
                }
            }
            while (offset < fs.Length && Unicode.Rune.IsDigit(fs.Peek(offset)))
            {
                value *= 10;
                value += fs.Read(ref offset).value - '0';
                if (value >> 32 != 0)
                    return ParseError.Overflow;
                ++digits;
            }
            if (digits == 0)
                return ParseError.Syntax;
            value = sign * value;
            if (value > Int32.MaxValue)
                return ParseError.Overflow;
            if (value < Int32.MinValue)
                return ParseError.Overflow;
            output = (int)value;
            return ParseError.None;
        }

        /// <summary>
        /// Parse a float from this string, at the byte offset indicated. The resulting float
        /// is intended to be bitwise-identical to the output of System.Single.Parse().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="offset">The zero-based byte offset from the beginning of the string.</param>
        /// <param name="output">The float parsed, if any.</param>
        /// <param name="decimalSeparator">The character used to separate the integral part from the fractional part.
        /// Defaults to a period.</param>
        /// <returns>An error code, if any, in the case that the parse fails.</returns>
        public static ParseError Parse<T>(ref this T fs, ref int offset, ref float output, char decimalSeparator = '.')
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            if (fs.Found(ref offset, 'n', 'a', 'n'))
            {
                FixedStringUtils.UintFloatUnion ufu = new FixedStringUtils.UintFloatUnion();
                ufu.uintValue = 4290772992U;
                output = ufu.floatValue;
                return ParseError.None;
            }
            int sign = 1;
            if (offset < fs.Length)
            {
                if (fs.Peek(offset).value == '+')
                    fs.Read(ref offset);
                else if (fs.Peek(offset).value == '-')
                {
                    sign = -1;
                    fs.Read(ref offset);
                }
            }
            ulong decimalMantissa = 0;
            int significantDigits = 0;
            int digitsAfterDot = 0;
            int mantissaDigits = 0;
            if (fs.Found(ref offset, 'i', 'n', 'f', 'i', 'n', 'i', 't', 'y'))
            {
                output = (sign == 1) ? Single.PositiveInfinity : Single.NegativeInfinity;
                return ParseError.None;
            }
            while (offset < fs.Length && Unicode.Rune.IsDigit(fs.Peek(offset)))
            {
                ++mantissaDigits;
                if (significantDigits < 9)
                {
                    var temp = decimalMantissa * 10 + (ulong)(fs.Peek(offset).value - '0');
                    if (temp > decimalMantissa)
                        ++significantDigits;
                    decimalMantissa = temp;
                }
                else
                    --digitsAfterDot;
                fs.Read(ref offset);
            }
            if (offset < fs.Length && fs.Peek(offset).value == decimalSeparator)
            {
                fs.Read(ref offset);
                while (offset < fs.Length && Unicode.Rune.IsDigit(fs.Peek(offset)))
                {
                    ++mantissaDigits;
                    if (significantDigits < 9)
                    {
                        var temp = decimalMantissa * 10 + (ulong)(fs.Peek(offset).value - '0');
                        if (temp > decimalMantissa)
                            ++significantDigits;
                        decimalMantissa = temp;
                        ++digitsAfterDot;
                    }
                    fs.Read(ref offset);
                }
            }
            if (mantissaDigits == 0)
                return ParseError.Syntax;
            int decimalExponent = 0;
            int decimalExponentSign = 1;
            if (offset < fs.Length && (fs.Peek(offset).value | 32) == 'e')
            {
                fs.Read(ref offset);
                if (offset < fs.Length)
                {
                    if (fs.Peek(offset).value == '+')
                        fs.Read(ref offset);
                    else if (fs.Peek(offset).value == '-')
                    {
                        decimalExponentSign = -1;
                        fs.Read(ref offset);
                    }
                }
                int exponentDigits = 0;
                while (offset < fs.Length && Unicode.Rune.IsDigit(fs.Peek(offset)))
                {
                    ++exponentDigits;
                    decimalExponent = decimalExponent * 10 + (fs.Peek(offset).value - '0');
                    if (decimalExponent > 38)
                        if (decimalExponentSign == 1)
                            return ParseError.Overflow;
                        else
                            return ParseError.Underflow;
                    fs.Read(ref offset);
                }
                if (exponentDigits == 0)
                    return ParseError.Syntax;
            }
            decimalExponent = decimalExponent * decimalExponentSign - digitsAfterDot;
            var error = FixedStringUtils.Base10ToBase2(ref output, decimalMantissa, decimalExponent);
            if (error != ParseError.None)
                return error;
            output *= sign;
            return ParseError.None;
        }
    }
}
