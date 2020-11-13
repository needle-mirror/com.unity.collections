using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// <undoc />
    /// </summary>
    [BurstCompatible]
    public static unsafe class UTF8ArrayUnsafeUtility
    {
        /// <summary>
        /// Copy the given src char (UCS2) array pointer to the destination UTF-8 byte array, converting
        /// to UTF-8 along the way.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destLength"></param>
        /// <param name="destUTF8MaxLengthInBytes"></param>
        /// <param name="src"></param>
        /// <param name="srcLength"></param>
        /// <returns></returns>
        public static CopyError Copy(byte *dest, out int destLength, int destUTF8MaxLengthInBytes, char *src, int srcLength)
        {
            var error = Unicode.Utf16ToUtf8(src, srcLength, dest, out destLength, destUTF8MaxLengthInBytes);
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Copy the given src char (UCS2) array pointer to the destination UTF-8 byte array, converting
        /// to UTF-8 along the way.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destLength"></param>
        /// <param name="destUTF8MaxLengthInBytes"></param>
        /// <param name="src"></param>
        /// <param name="srcLength"></param>
        /// <returns></returns>
        public static CopyError Copy(byte *dest, out ushort destLength, ushort destUTF8MaxLengthInBytes, char *src, int srcLength)
        {
            var error = Unicode.Utf16ToUtf8(src, srcLength, dest, out var temp, destUTF8MaxLengthInBytes);
            destLength = (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Copy the given src UTF-8 byte array to the destination UTF-8 byte array.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destLength"></param>
        /// <param name="destUTF8MaxLengthInBytes"></param>
        /// <param name="src"></param>
        /// <param name="srcLength"></param>
        /// <returns></returns>
        public static CopyError Copy(byte *dest, out int destLength, int destUTF8MaxLengthInBytes, byte *src, int srcLength)
        {
            destLength = srcLength > destUTF8MaxLengthInBytes ? destUTF8MaxLengthInBytes : srcLength;
            UnsafeUtility.MemCpy(dest, src, destLength);
            return destLength == srcLength ? CopyError.None : CopyError.Truncation;
        }

        /// <summary>
        /// Copy the given src UTF-8 byte array to the destination UTF-8 byte array.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destLength"></param>
        /// <param name="destUTF8MaxLengthInBytes"></param>
        /// <param name="src"></param>
        /// <param name="srcLength"></param>
        /// <returns></returns>
        public static CopyError Copy(byte *dest, out ushort destLength, ushort destUTF8MaxLengthInBytes, byte *src, ushort srcLength)
        {
            var error = Unicode.Utf8ToUtf8(src, srcLength, dest, out var temp, destUTF8MaxLengthInBytes);
            destLength = (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Copy the given UTF-8 byte array pointer to the destination char (UCS2) array pointer, converting
        /// UTF-8 to UCS2 along the way.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destLength"></param>
        /// <param name="destUTF8MaxLengthInBytes"></param>
        /// <param name="src"></param>
        /// <param name="srcLength"></param>
        /// <returns></returns>
        public static CopyError Copy(char *dest, out int destLength, int destUTF8MaxLengthInBytes, byte *src, int srcLength)
        {
            if (ConversionError.None == Unicode.Utf8ToUtf16(src, srcLength, dest, out destLength, destUTF8MaxLengthInBytes))
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destLength"></param>
        /// <param name="destUTF8MaxLengthInBytes"></param>
        /// <param name="src"></param>
        /// <param name="srcLength"></param>
        /// <returns></returns>
        public static CopyError Copy(char *dest, out ushort destLength, ushort destUTF8MaxLengthInBytes, byte *src, ushort srcLength)
        {
            var error = Unicode.Utf8ToUtf16(src, srcLength, dest, out var temp, destUTF8MaxLengthInBytes);
            destLength = (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Append the given src UTF-8 byte array to the destination UTF-8 byte array.
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destLength"></param>
        /// <param name="destUTF8MaxLengthInBytes"></param>
        /// <param name="src"></param>
        /// <param name="srcLength"></param>
        /// <returns></returns>
        public static CopyError Append(byte *dest, ref ushort destLength, ushort destUTF8MaxLengthInBytes, byte *src, ushort srcLength)
        {
            var error = Unicode.Utf8ToUtf8(src, srcLength, dest + destLength, out var temp, destUTF8MaxLengthInBytes - destLength);
            destLength += (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destLength"></param>
        /// <param name="destUTF8MaxLengthInBytes"></param>
        /// <param name="src"></param>
        /// <param name="srcLength"></param>
        /// <returns></returns>
        public static CopyError Append(byte *dest, ref ushort destLength, ushort destUTF8MaxLengthInBytes, char *src, int srcLength)
        {
            var error = Unicode.Utf16ToUtf8(src, srcLength, dest + destLength, out var temp, destUTF8MaxLengthInBytes - destLength);
            destLength += (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destLength"></param>
        /// <param name="destUTF8MaxLengthInBytes"></param>
        /// <param name="src"></param>
        /// <param name="srcLength"></param>
        /// <returns></returns>
        public static CopyError Append(char *dest, ref ushort destLength, ushort destUTF8MaxLengthInBytes, byte *src, ushort srcLength)
        {
            var error = Unicode.Utf8ToUtf16(src, srcLength, dest + destLength, out var temp, destUTF8MaxLengthInBytes - destLength);
            destLength += (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="aBytes"></param>
        /// <param name="aLength"></param>
        /// <param name="bBytes"></param>
        /// <param name="bLength"></param>
        /// <returns></returns>
        public static bool EqualsUTF8Bytes(byte* aBytes, int aLength, byte* bBytes, int bLength)
        {
            if (aLength != bLength)
                return false;

            return UnsafeUtility.MemCmp(aBytes, bBytes, aLength) == 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="destOffset"></param>
        /// <param name="destCapacity"></param>
        /// <param name="src"></param>
        /// <param name="srcLength"></param>
        /// <returns></returns>
        public static FormatError AppendUTF8Bytes(byte* dest, ref int destOffset, int destCapacity, byte* src, int srcLength)
        {
            if (destOffset + srcLength > destCapacity)
                return FormatError.Overflow;
            UnsafeUtility.MemCpy(dest + destOffset, src, srcLength);
            destOffset += srcLength;
            return FormatError.None;
        }
    }
}
