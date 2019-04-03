using System;
using UnityEngine.Assertions;

namespace Unity.Collections.LowLevel.Unsafe
{
    unsafe public static class UnsafeUtilityEx
    {
        public static ref T AsRef<T>(void* ptr) where T : struct
        {
            return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>(ptr);
        }

        public static ref T ArrayElementAsRef<T>(void* ptr, int index) where T : struct
        {
            return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>((byte*)ptr + index * UnsafeUtility.SizeOf<T>());
        }

        public static void* RestrictNoAlias(void* ptr)
        {
            return ptr;
        }

        public static void MemSet(void* destination, byte value, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                ((byte*) destination)[i] = value;
            }
        }            
    }
}
