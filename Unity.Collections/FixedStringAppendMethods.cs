using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// <undoc />
    /// </summary>
    [BurstCompatible]
    public unsafe static partial class FixedStringMethods
    {
        /// <summary>
        /// Append the given Unicode code point to this IUTF8Bytes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError Append<T>(ref this T fs, Unicode.Rune a)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var len = fs.Length;
            var runeLen = a.LengthInUtf8Bytes();
            if (!fs.TryResize(len + runeLen, NativeArrayOptions.UninitializedMemory))
                return FormatError.Overflow;
            return fs.Write(ref len, a);
        }

        /// <summary>
        /// Append a character to this IUTF8Bytes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError Append<T>(ref this T fs, char a)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            return fs.Append((Unicode.Rune) a);
        }

        /// <summary>
        /// Append a raw byte to this IUTF8Bytes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError AppendRawByte<T>(ref this T fs, byte a)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var origLength = fs.Length;
            if (!fs.TryResize(origLength + 1, NativeArrayOptions.UninitializedMemory))
                return FormatError.Overflow;
            fs.GetUnsafePtr()[origLength] = a;
            return FormatError.None;
        }

        /// <summary>
        /// Append the given Unicode.Rune to this FixedString repeated count times.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs">A FixedString-like type</param>
        /// <param name="rune">The Unicode.Rune to repeat</param>
        /// <param name="count">The number of times to repeat the Unicode.Rune</param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError Append<T>(ref this T fs, Unicode.Rune rune, int count)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var origLength = fs.Length;

            if (!fs.TryResize(origLength + rune.LengthInUtf8Bytes() * count, NativeArrayOptions.UninitializedMemory))
                return FormatError.Overflow;

            var cap = fs.Capacity;
            var b = fs.GetUnsafePtr();
            int offset = origLength;
            for (int i = 0; i < count; ++i)
            {
                var error = Unicode.UcsToUtf8(b, ref offset, cap, rune);
                if (error != ConversionError.None)
                    return FormatError.Overflow;
            }

            return FormatError.None;
        }

        /// <summary>
        /// Append the UTF-8 representation of a given long integer to the contents of this IUTF8Bytes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="input">The long integer to append as UTF-8 to the contents of this IUTF8Bytes</param>
        /// <returns>An error code, if any, in the case that the append fails.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError Append<T>(ref this T fs, long input)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            const int maximumDigits = 20;
            var temp = stackalloc byte[maximumDigits];
            int offset = maximumDigits;
            if (input >= 0)
            {
                do
                {
                    var digit = (byte)(input % 10);
                    temp[--offset] = (byte)('0' + digit);
                    input /= 10;
                }
                while (input != 0);
            }
            else
            {
                do
                {
                    var digit = (byte)(input % 10);
                    temp[--offset] = (byte)('0' - digit);
                    input /= 10;
                }
                while (input != 0);
                temp[--offset] = (byte)'-';
            }

            return fs.Append(temp + offset, maximumDigits - offset);
        }

        /// <summary>
        /// Append the UTF-8 representation of a given integer to the contents of this IUTF8Bytes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="input">The long integer to append as UTF-8 to the contents of this IUTF8Bytes</param>
        /// <returns>An error code, if any, in the case that the append fails.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError Append<T>(ref this T fs, int input)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            return fs.Append((long)input);
        }

        /// <summary>
        /// Append the UTF-8 representation of a given unsigned long integer to the contents of this IUTF8Bytes.
        /// </summary>
        /// <param name="input">The long integer to append as UTF-8 to the contents of this IUTF8Bytes</param>
        /// <returns>An error code, if any, in the case that the append fails.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError Append<T>(ref this T fs, ulong input)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            const int maximumDigits = 20;
            var temp = stackalloc byte[maximumDigits];
            int offset = maximumDigits;
            do
            {
                var digit = (byte)(input % 10);
                temp[--offset] = (byte)('0' + digit);
                input /= 10;
            }
            while (input != 0);

            return fs.Append(temp + offset, maximumDigits - offset);
        }

        /// <summary>
        /// Append the UTF-8 representation of a given unsigned integer to the contents of this IUTF8Bytes.
        /// </summary>
        /// <param name="input">The long integer to append as UTF-8 to the contents of this IUTF8Bytes</param>
        /// <returns>An error code, if any, in the case that the append fails.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError Append<T>(ref this T fs, uint input)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            return fs.Append((ulong)input);
        }

        /// <summary>
        /// Append the UTF-8 representation of a given float to the contents of this IUTF8Bytes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="input">The float to append as UTF-8 to the contents of this IUTF8Bytes</param>
        /// <param name="decimalSeparator">The character used to separate the integral part from the fractional part.
        /// A period by default.</param>
        /// <returns>An error code, if any, in the case that the format fails.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError Append<T>(ref this T fs, float input, char decimalSeparator = '.')
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            FixedStringUtils.UintFloatUnion ufu = new FixedStringUtils.UintFloatUnion();
            ufu.floatValue = input;
            var sign = ufu.uintValue >> 31;
            ufu.uintValue &= ~(1 << 31);
            FormatError error;
            if ((ufu.uintValue & 0x7F800000) == 0x7F800000)
            {
                if (ufu.uintValue == 0x7F800000)
                {
                    if (sign != 0 && ((error = fs.Append('-')) != FormatError.None))
                        return error;
                    return fs.Append('I', 'n', 'f', 'i', 'n', 'i', 't', 'y');
                }
                return fs.Append('N', 'a', 'N');
            }
            if (sign != 0 && ufu.uintValue != 0) // C# prints -0 as 0
                if ((error = fs.Append('-')) != FormatError.None)
                    return error;
            ulong decimalMantissa = 0;
            int decimalExponent = 0;
            FixedStringUtils.Base2ToBase10(ref decimalMantissa, ref decimalExponent, ufu.floatValue);
            var backwards = stackalloc char[9];
            int decimalDigits = 0;
            do
            {
                if (decimalDigits >= 9)
                    return FormatError.Overflow;
                var decimalDigit = decimalMantissa % 10;
                backwards[8 - decimalDigits++] = (char)('0' + decimalDigit);
                decimalMantissa /= 10;
            }
            while (decimalMantissa > 0);
            char *ascii = backwards + 9 - decimalDigits;
            var leadingZeroes = -decimalExponent - decimalDigits + 1;
            if (leadingZeroes > 0)
            {
                if (leadingZeroes > 4)
                    return fs.AppendScientific(ascii, decimalDigits, decimalExponent, decimalSeparator);
                if ((error = fs.Append('0', decimalSeparator)) != FormatError.None)
                    return error;
                --leadingZeroes;
                while (leadingZeroes > 0)
                {
                    if ((error = fs.Append('0')) != FormatError.None)
                        return error;
                    --leadingZeroes;
                }
                for (var i = 0; i < decimalDigits; ++i)
                {
                    if ((error = fs.Append(ascii[i])) != FormatError.None)
                        return error;
                }
                return FormatError.None;
            }
            var trailingZeroes = decimalExponent;
            if (trailingZeroes > 0)
            {
                if (trailingZeroes > 4)
                    return fs.AppendScientific(ascii, decimalDigits, decimalExponent, decimalSeparator);
                for (var i = 0; i < decimalDigits; ++i)
                {
                    if ((error = fs.Append(ascii[i])) != FormatError.None)
                        return error;
                }
                while (trailingZeroes > 0)
                {
                    if ((error = fs.Append('0')) != FormatError.None)
                        return error;
                    --trailingZeroes;
                }
                return FormatError.None;
            }
            var indexOfSeparator = decimalDigits + decimalExponent;
            for (var i = 0; i < decimalDigits; ++i)
            {
                if (i == indexOfSeparator)
                    if ((error = fs.Append(decimalSeparator)) != FormatError.None)
                        return error;
                if ((error = fs.Append(ascii[i])) != FormatError.None)
                    return error;
            }
            return FormatError.None;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="inputIn"></param>
        /// <returns>An error code, if any, in the case that the append fails.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128), typeof(FixedString128) })]
        public static FormatError Append<T,T2>(ref this T fs, in T2 inputIn)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var input = ref UnsafeUtilityExtensions.AsRef(inputIn);
            return fs.Append(input.GetUnsafePtr(), input.Length);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128), typeof(FixedString128) })]
        public static CopyError CopyFrom<T, T2>(ref this T fs, in T2 input)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            var fe = Append(ref fs, input);
            if (fe != FormatError.None)
                return CopyError.Truncation;
            return CopyError.None;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="utf8Bytes"></param>
        /// <param name="utf8BytesLength"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public unsafe static FormatError Append<T>(ref this T fs, byte* utf8Bytes, int utf8BytesLength)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var origLength = fs.Length;
            if (!fs.TryResize(origLength + utf8BytesLength, NativeArrayOptions.UninitializedMemory))
                return FormatError.Overflow;
            UnsafeUtility.MemCpy(fs.GetUnsafePtr() + origLength, utf8Bytes, utf8BytesLength);
            return FormatError.None;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="s"></param>
        /// <returns>An error code, if any, in the case that the append fails.</returns>
        [NotBurstCompatible]
        public unsafe static FormatError Append<T>(ref this T fs, string s)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            // we don't know how big the expansion from UTF16 to UTF8 will be, so we account for worst case.
            int worstCaseCapacity = s.Length * 4;
            byte* utf8Bytes = stackalloc byte[worstCaseCapacity];
            int utf8Len;

            fixed (char* chars = s)
            {
                var err = UTF8ArrayUnsafeUtility.Copy(utf8Bytes, out utf8Len, worstCaseCapacity, chars, s.Length);
                if (err != CopyError.None)
                {
                    return FormatError.Overflow;
                }
            }

            return fs.Append(utf8Bytes, utf8Len);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        [NotBurstCompatible]
        public static CopyError CopyFrom<T>(ref this T fs, string s)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            var fe = Append(ref fs, s);
            if (fe != FormatError.None)
                return CopyError.Truncation;
            return CopyError.None;
        }
    }
}
