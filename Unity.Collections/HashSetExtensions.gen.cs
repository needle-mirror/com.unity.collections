using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    public unsafe static class HashSetExtensions
    {
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this NativeHashSet<T> container, FixedList128<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this NativeHashSet<T> container, FixedList128<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this NativeHashSet<T> container, FixedList128<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this NativeHashSet<T> container, FixedList32<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this NativeHashSet<T> container, FixedList32<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this NativeHashSet<T> container, FixedList32<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this NativeHashSet<T> container, FixedList4096<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this NativeHashSet<T> container, FixedList4096<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this NativeHashSet<T> container, FixedList4096<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this NativeHashSet<T> container, FixedList512<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this NativeHashSet<T> container, FixedList512<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this NativeHashSet<T> container, FixedList512<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this NativeHashSet<T> container, FixedList64<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this NativeHashSet<T> container, FixedList64<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this NativeHashSet<T> container, FixedList64<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this NativeHashSet<T> container, NativeArray<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this NativeHashSet<T> container, NativeArray<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this NativeHashSet<T> container, NativeArray<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this NativeHashSet<T> container, NativeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this NativeHashSet<T> container, NativeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this NativeHashSet<T> container, NativeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this NativeHashSet<T> container, NativeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this NativeHashSet<T> container, NativeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this NativeHashSet<T> container, NativeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
    }
}

namespace Unity.Collections.LowLevel.Unsafe
{
    public unsafe static class HashSetExtensions
    {
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this NativeHashSet<T> container, UnsafeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this NativeHashSet<T> container, UnsafeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this NativeHashSet<T> container, UnsafeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this NativeHashSet<T> container, UnsafeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this NativeHashSet<T> container, UnsafeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this NativeHashSet<T> container, UnsafeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }

        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this UnsafeHashSet<T> container, FixedList128<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this UnsafeHashSet<T> container, FixedList128<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this UnsafeHashSet<T> container, FixedList128<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this UnsafeHashSet<T> container, FixedList32<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this UnsafeHashSet<T> container, FixedList32<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this UnsafeHashSet<T> container, FixedList32<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this UnsafeHashSet<T> container, FixedList4096<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this UnsafeHashSet<T> container, FixedList4096<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this UnsafeHashSet<T> container, FixedList4096<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this UnsafeHashSet<T> container, FixedList512<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this UnsafeHashSet<T> container, FixedList512<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this UnsafeHashSet<T> container, FixedList512<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this UnsafeHashSet<T> container, FixedList64<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this UnsafeHashSet<T> container, FixedList64<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this UnsafeHashSet<T> container, FixedList64<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this UnsafeHashSet<T> container, NativeArray<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this UnsafeHashSet<T> container, NativeArray<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this UnsafeHashSet<T> container, NativeArray<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this UnsafeHashSet<T> container, NativeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this UnsafeHashSet<T> container, NativeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this UnsafeHashSet<T> container, NativeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this UnsafeHashSet<T> container, NativeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this UnsafeHashSet<T> container, NativeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this UnsafeHashSet<T> container, NativeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this UnsafeHashSet<T> container, UnsafeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this UnsafeHashSet<T> container, UnsafeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this UnsafeHashSet<T> container, UnsafeHashSet<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
        /// <summary>
        /// Modifies this container to remove all values that are present in the other container.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void ExceptWith<T>(this UnsafeHashSet<T> container, UnsafeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Remove(item);
            }
        }

        /// <summary>
        /// Modifies this container to keep only values that are present in both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void IntersectWith<T>(this UnsafeHashSet<T> container, UnsafeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            var result = new UnsafeList<T>(container.Count(), Allocator.Temp);

            foreach (var item in other)
            {
                if (container.Contains(item))
                {
                    result.Add(item);
                }
            }

            container.Clear();
            container.UnionWith(result);

            result.Dispose();
        }

        /// <summary>
        /// Modifies this container to contain values from both containers.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="container">Container to modify.</param>
        /// <param name="other">The container to compare to this container.</param>
        public static void UnionWith<T>(this UnsafeHashSet<T> container, UnsafeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            foreach (var item in other)
            {
                container.Add(item);
            }
        }
    }
}
