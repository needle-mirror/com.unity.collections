using System;

namespace Unity.Collections
{
    //
    // NOTE
    //
    // All methods in this file are obsolete.  Some of them have commented out Obsolete attributes because:
    //    1. Unity.Serialization (JsonStringBuffer) uses foo.Format() as an interim step so that it's compatible with both old
    //       and new FixedString API.  We can't auto-upgrade this, because we don't know if Clear is necessary.
    //    2. If we mark this as obsolete, then warnings-as-errors in Samples project kicks in for errors in serialization, and errors out
    //
    // This should be fixed with an upcoming release of Serialization, and then we can make this obsolete.
    public unsafe static partial class FixedStringMethods
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        [Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        public static FormatError Format<T>(ref this T fs, Unicode.Rune a)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return fs.Append(a);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        [Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        public static FormatError Format<T>(ref this T fs, char a)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return fs.Append(a);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="a"></param>
        /// <returns></returns>
        [Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        public static FormatError FormatRawByte<T>(ref this T fs, byte a)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return fs.AppendRawByte(a);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="rune"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        public static FormatError Format<T>(ref this T fs, Unicode.Rune rune, int count)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return fs.Append(rune, count);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        //[Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        public static FormatError Format<T>(ref this T fs, long input)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return fs.Append(input);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        //[Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        public static FormatError Format<T>(ref this T fs, int input)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return fs.Append((long)input);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="input"></param>
        /// <param name="decimalSeparator"></param>
        /// <returns></returns>
        //[Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        public static FormatError Format<T>(ref this T fs, float input, char decimalSeparator = '.')
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return fs.Append(input, decimalSeparator);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        [Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        public static FormatError Format<T, T2>(ref this T fs, in T2 input)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return Append(ref fs, input);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="utf8Bytes"></param>
        /// <param name="utf8BytesLength"></param>
        /// <returns></returns>
        [Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        public unsafe static FormatError Format<T>(ref this T fs, byte* utf8Bytes, int utf8BytesLength)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return Append(ref fs, utf8Bytes, utf8BytesLength);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        [Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        public unsafe static FormatError Format<T>(ref this T fs, string s)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return Append(ref fs, s);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        [Obsolete("AppendFrom is now Append. (RemovedAfter 2020-09-01) (UnityUpgradable) -> Append(*)")]
        public static CopyError AppendFrom<T, T2>(ref this T fs, in T2 input)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            var fe = Append(ref fs, input);
            if (fe != FormatError.None)
                return CopyError.Truncation;
            return CopyError.None;
        }
    }
}
