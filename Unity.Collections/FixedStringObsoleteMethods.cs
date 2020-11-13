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
    [BurstCompatible]
    public unsafe static partial class FixedStringMethods
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        //[Obsolete("Format with a single argument has been removed.  Please use Clear() if necessary followed by Append(). (RemovedAfter 2020-09-01)", false)]
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
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
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
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
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError Format<T>(ref this T fs, float input, char decimalSeparator = '.')
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            return fs.Append(input, decimalSeparator);
        }
    }
}
