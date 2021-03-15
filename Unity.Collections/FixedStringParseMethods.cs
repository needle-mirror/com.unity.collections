using System;
using System.Runtime.CompilerServices;

namespace Unity.Collections
{
    /// <summary>
    /// <undoc />
    /// </summary>
    [BurstCompatible]
    public unsafe static partial class FixedStringMethods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        private static bool ParseLongInternal<T>(ref T fs, ref int offset, out long value)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            int resetOffset = offset;
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

            int digitOffset = offset;
            value = 0;
            while (offset < fs.Length && Unicode.Rune.IsDigit(fs.Peek(offset)))
            {
                value *= 10;
                value += fs.Read(ref offset).value - '0';
            }
            value = sign * value;

            // If there was no number parsed, revert the offset since it's a syntax error and we might
            // have erroneously parsed a '-' or '+'
            if (offset == digitOffset)
            {
                offset = resetOffset;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parse an int from this string, at the given byte offset. The resulting value
        /// is intended to be bitwise-identical to the output of int.Parse().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="offset">The zero-based byte offset from the beginning of the string, updated if a value is parsed.</param>
        /// <param name="output">The int parsed, if any.</param>
        /// <returns>An error code, if any, in the case that the parse fails.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static ParseError Parse<T>(ref this T fs, ref int offset, ref int output)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            if (!ParseLongInternal(ref fs, ref offset, out long value))
                return ParseError.Syntax;
            if (value > int.MaxValue)
                return ParseError.Overflow;
            if (value < int.MinValue)
                return ParseError.Overflow;
            output = (int)value;
            return ParseError.None;
        }

        /// <summary>
        /// Parse a uint from this string, at the given byte offset. The resulting value
        /// is intended to be bitwise-identical to the output of uint.Parse().
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="offset">The zero-based byte offset from the beginning of the string, updated if a value is parsed.</param>
        /// <param name="output">The uint parsed, if any.</param>
        /// <returns>An error code, if any, in the case that the parse fails.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static ParseError Parse<T>(ref this T fs, ref int offset, ref uint output)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            if (!ParseLongInternal(ref fs, ref offset, out long value))
                return ParseError.Syntax;
            if (value > uint.MaxValue)
                return ParseError.Overflow;
            if (value < uint.MinValue)
                return ParseError.Overflow;
            output = (uint)value;
            return ParseError.None;
        }

        /// <summary>
        /// Parse a float from this string, at the byte offset indicated. The resulting float
        /// is intended to be bitwise-identical to the output of System.Single.Parse(), with the following exceptions:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// Values which overflow return ParseError.Overflow rather than assigning "Infinity"
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// Values which underflow return ParseError.Underflow rather than assigning "0"
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// Values in exponent form 1e-39 to 1e-45 will be considered underflowed values rather than parsing as
        /// greater than 0 in order to avoid creating floating point numbers that may generate unexpected
        /// denormal problems in user code.
        /// </description>
        /// </item>
        /// </list>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="offset">The zero-based byte offset from the beginning of the string.</param>
        /// <param name="output">The float parsed, if any.</param>
        /// <param name="decimalSeparator">The character used to separate the integral part from the fractional part.
        /// Defaults to a period.</param>
        /// <returns>An error code, if any, in the case that the parse fails.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static ParseError Parse<T>(ref this T fs, ref int offset, ref float output, char decimalSeparator = '.')
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            int resetOffset = offset;
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
            if (fs.Found(ref offset, 'n', 'a', 'n'))
            {
                FixedStringUtils.UintFloatUnion ufu = new FixedStringUtils.UintFloatUnion();
                ufu.uintValue = 4290772992U;
                output = ufu.floatValue;
                return ParseError.None;
            }
            if (fs.Found(ref offset, 'i', 'n', 'f', 'i', 'n', 'i', 't', 'y'))
            {
                output = (sign == 1) ? Single.PositiveInfinity : Single.NegativeInfinity;
                return ParseError.None;
            }

            ulong decimalMantissa = 0;
            int significantDigits = 0;
            int digitsAfterDot = 0;
            int mantissaDigits = 0;
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
            {
                // Reset offset in case '+' or '-' was erroneously parsed
                offset = resetOffset;
                return ParseError.Syntax;
            }
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
                int digitOffset = offset;
                while (offset < fs.Length && Unicode.Rune.IsDigit(fs.Peek(offset)))
                {
                    decimalExponent = decimalExponent * 10 + (fs.Peek(offset).value - '0');
                    fs.Read(ref offset);
                }
                if (offset == digitOffset)
                {
                    // Reset offset in case '+' or '-' was erroneously parsed
                    offset = resetOffset;
                    return ParseError.Syntax;
                }
                if (decimalExponent > 38)
                {
                    if (decimalExponentSign == 1)
                        return ParseError.Overflow;
                    else
                        return ParseError.Underflow;
                }
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
