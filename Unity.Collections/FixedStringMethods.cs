using System;
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
        /// Search this string for the first occurrence of a given run of bytes
        /// and return the index of where it was found, if any.  Return -1 if not found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="bytes"></param>
        /// <param name="bytesLen"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static int IndexOf<T>(ref this T fs, byte* bytes, int bytesLen)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var dst = fs.GetUnsafePtr();
            var dstLen = fs.Length;
            for (var i = 0; i <= dstLen - bytesLen; ++i)
            {
                for (var j = 0; j < bytesLen; ++j)
                    if (dst[i + j] != bytes[j])
                        goto end_of_loop;
                return i;
                end_of_loop : {}
            }
            return -1;
        }

        /// <summary>
        /// Search this string for the first occurrence of a given run of bytes
        /// and return the index of where it was found, if any.  Return -1 if not found.
        /// The search starts at the given startIndex and goes for an optional distance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="bytes"></param>
        /// <param name="bytesLen"></param>
        /// <param name="startIndex"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static int IndexOf<T>(ref this T fs, byte* bytes, int bytesLen, int startIndex, int distance = Int32.MaxValue)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var dst = fs.GetUnsafePtr();
            var dstLen = fs.Length;
            var searchrange = Math.Min(distance - 1, dstLen - bytesLen);
            for (var i = startIndex; i <= searchrange; ++i)
            {
                for (var j = 0; j < bytesLen; ++j)
                    if (dst[i + j] != bytes[j])
                        goto end_of_loop;
                return i;
                end_of_loop : {}
            }
            return -1;
        }

        /// <summary>
        /// Search this string for the first occurrence of another FixedString
        /// and return the index of where it was found, if any.  Return -1 if not found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128), typeof(FixedString128) })]
        public static int IndexOf<T,T2>(ref this T fs, in T2 other)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.IndexOf(oref.GetUnsafePtr(), oref.Length);
        }

        /// <summary>
        /// Search this string for the first occurrence of another FixedString
        /// and return the index of where it was found, if any.  Return -1 if not found.
        /// The search starts at the given startIndex and goes for an optional distance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="other"></param>
        /// <param name="startIndex"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128), typeof(FixedString128) })]
        public static int IndexOf<T,T2>(ref this T fs, in T2 other, int startIndex, int distance = Int32.MaxValue)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.IndexOf(oref.GetUnsafePtr(), oref.Length, startIndex, distance);
        }

        /// <summary>
        /// Search this string for the given other FixedString.  Return
        /// a boolean indicating whether it was found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128), typeof(FixedString128) })]
        public static bool Contains<T,T2>(ref this T fs, in T2 other)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            return fs.IndexOf(in other) != -1;
        }

        /// <summary>
        /// Search this string backwards for the last occurrence of a given run of bytes
        /// and return the index of where it was found, if any.  Return -1 if not found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="bytes"></param>
        /// <param name="bytesLen"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static int LastIndexOf<T>(ref this T fs, byte* bytes, int bytesLen)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var dst = fs.GetUnsafePtr();
            var dstLen = fs.Length;
            for (var i = dstLen - bytesLen; i >= 0; --i)
            {
                for (var j = 0; j < bytesLen; ++j)
                    if (dst[i + j] != bytes[j])
                        goto end_of_loop;
                return i;
                end_of_loop : {}
            }
            return -1;
        }

        /// <summary>
        /// Search this string backwards for the last occurrence of a given run of bytes
        /// and return the index of where it was found, if any.  Return -1 if not found.
        /// The search starts at the given startIndex, and goes backwards for the given distance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="bytes"></param>
        /// <param name="bytesLen"></param>
        /// <param name="startIndex"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static int LastIndexOf<T>(ref this T fs, byte* bytes, int bytesLen, int startIndex, int distance = int.MaxValue)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var dst = fs.GetUnsafePtr();
            var dstLen = fs.Length;
            startIndex = Math.Min(dstLen - bytesLen, startIndex);
            var searchrange = Math.Max(0, startIndex - distance);
            for (var i = startIndex; i >= searchrange; --i)
            {
                for (var j = 0; j < bytesLen; ++j)
                    if (dst[i + j] != bytes[j])
                        goto end_of_loop;
                return i;
                end_of_loop : {}
            }
            return -1;
        }

        /// <summary>
        /// Search this string backwards for the last occurrence of another FixedString
        /// and return the index of where it was found, if any.  Return -1 if not found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128), typeof(FixedString128) })]
        public static int LastIndexOf<T,T2>(ref this T fs, in T2 other)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.LastIndexOf(oref.GetUnsafePtr(), oref.Length);
        }

        /// <summary>
        /// Search this string backwards for the last occurrence of another FixedString
        /// and return the index of where it was found, if any.  Return -1 if not found.
        /// The search starts at the given startIndex and goes for an optional distance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="other"></param>
        /// <param name="startIndex"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128), typeof(FixedString128) })]
        public static int LastIndexOf<T,T2>(ref this T fs, in T2 other, int startIndex, int distance = Int32.MaxValue)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.LastIndexOf(oref.GetUnsafePtr(), oref.Length, startIndex, distance);
        }

        /// <summary>
        /// Compare this string to the given byte run.  Return an integer that indicates whether this
        /// instance precedes the given parameter (less than 0), is equal to the given parameter (0), or comes
        /// after the given parameter (greater than 0).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="bytes"></param>
        /// <param name="bytesLen"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static int CompareTo<T>(ref this T fs, byte* bytes, int bytesLen)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var a = fs.GetUnsafePtr();
            var aa = fs.Length;
            int chars = aa < bytesLen ? aa : bytesLen;
            for (var i = 0; i < chars; ++i)
            {
                if (a[i] < bytes[i])
                    return -1;
                if (a[i] > bytes[i])
                    return 1;
            }
            if (aa < bytesLen)
                return -1;
            if (aa > bytesLen)
                return 1;
            return 0;
        }

        /// <summary>
        /// Compare this string to the given FixedString.  Return an integer that indicates whether this
        /// instance precedes the given parameter (less than 0), is equal to the given parameter (0), or comes
        /// after the given parameter (greater than 0).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128), typeof(FixedString128) })]
        public static int CompareTo<T,T2>(ref this T fs, in T2 other)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.CompareTo(oref.GetUnsafePtr(), oref.Length);
        }

        /// <summary>
        /// Compare this string to the given byte run.  Return whether they are equal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="bytes"></param>
        /// <param name="bytesLen"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static bool Equals<T>(ref this T fs, byte* bytes, int bytesLen)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var a = fs.GetUnsafePtr();
            var aa = fs.Length;
            if (aa != bytesLen)
                return false;
            if (a == bytes)
                return true;
            return fs.CompareTo(bytes, bytesLen) == 0;
        }

        /// <summary>
        /// Compare this string to the given FixedString.  Return whether they are equal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="fs"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128), typeof(FixedString128) })]
        public static bool Equals<T,T2>(ref this T fs, in T2 other)
            where T : struct, INativeList<byte>, IUTF8Bytes
            where T2 : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.Equals(oref.GetUnsafePtr(), oref.Length);
        }

        /// <summary>
        /// Read a Unicode.Rune at the given byte offset in this string.  This function
        /// will decode any utf8 encoding found at that offset and return the actual Unicode codepoint value.
        /// If the offset is invalid or a conversion error occurs, Unicode.BadRune is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static Unicode.Rune Peek<T>(ref this T fs, int offset)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            if (offset >= fs.Length)
                return Unicode.BadRune;
            Unicode.Utf8ToUcs(out var rune, fs.GetUnsafePtr(), ref offset, fs.Capacity);
            return rune;
        }

        /// <summary>
        /// Read a Unicode.Rune at the given byte offset in this string, and increment offset by the
        /// number of bytes that the rune at that offset occupied.  This function
        /// will decode any utf8 encoding found at that offset and return the actual Unicode
        /// codepoint value.  Calling this function until it returns Unicode.BadRune
        /// allows for iterating through unicode code points in the FixedString.
        /// If the offset is invalid or a conversion error occurs, Unicode.BadRune is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static Unicode.Rune Read<T>(ref this T fs, ref int offset)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            if (offset >= fs.Length)
                return Unicode.BadRune;
            Unicode.Utf8ToUcs(out var rune, fs.GetUnsafePtr(), ref offset, fs.Capacity);
            return rune;
        }

        /// <summary>
        /// Write a Unicode.Rune at the given byte offset in this string, and increment offset by the
        /// number of bytes that the rune occupies when encoded as UTF-8.
        /// If the offset is invalid or if there is not enough space to encode the rune, FormatError.Overflow
        /// is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <param name="offset"></param>
        /// <param name="rune"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static FormatError Write<T>(ref this T fs, ref int offset, Unicode.Rune rune)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var err = Unicode.UcsToUtf8(fs.GetUnsafePtr(), ref offset, fs.Capacity, rune);
            if (err != ConversionError.None)
                return FormatError.Overflow;
            return FormatError.None;
        }

        /// <summary>
        /// Convert this FixedString to a System.String.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <returns></returns>
        [NotBurstCompatible]
        public static String ConvertToString<T>(ref this T fs)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            var c = stackalloc char[fs.Length * 2];
            int length = 0;
            Unicode.Utf8ToUtf16(fs.GetUnsafePtr(), fs.Length, c, out length, fs.Length * 2);
            return new String(c, 0, length);
        }

        /// <summary>
        /// Compute a hashcode for this FixedString.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <returns></returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static int ComputeHashCode<T>(ref this T fs)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            return (int)CollectionHelper.Hash(fs.GetUnsafePtr(), fs.Length);
        }

        /// <summary>
        /// Returns the effective size of this struct in bytes, considering only the bytes that
        /// are actually used to hold data. Since the string may be shorter or longer, the
        /// effective size may be smaller than the UnsafeUtility.SizeOf() sizeof.  The first byte
        /// after the effective size is always 0, unless this FixedString's storage is malformed.
        /// Any following bytes are undefined.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fs"></param>
        /// <returns>The effective size of this struct in bytes.</returns>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(FixedString128) })]
        public static int EffectiveSizeOf<T>(ref this T fs)
            where T : struct, INativeList<byte>, IUTF8Bytes
        {
            return sizeof(ushort) + fs.Length + 1;
        }
    }
}
