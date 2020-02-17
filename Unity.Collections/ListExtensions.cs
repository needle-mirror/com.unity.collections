using System;
using System.Collections.Generic;

namespace Unity.Collections
{
    public static class ListExtensions
    {
        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">List to perform removal.</param>
        /// <param name="item">Item value to remove.</param>
        /// <returns>Returns true if item is removed, if item was not in the container returns false.</returns>
        public static bool RemoveSwapBack<T>(this List<T> list, T item)
        {
            int index = list.IndexOf(item);
            if (index < 0)
                return false;

            RemoveAtSwapBack(list, index);
            return true;
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">List to perform removal.</param>
        /// <param name="matcher"></param>
        /// <returns>Returns true if item is removed, if item was not in the container returns false.</returns>
        public static bool RemoveSwapBack<T>(this List<T> list, Predicate<T> matcher)
        {
            int index = list.FindIndex(matcher);
            if (index < 0)
                return false;

            RemoveAtSwapBack(list, index);
            return true;
        }

        /// <summary>
        /// Truncates the list by replacing the item at the specified index with the last item in the list. The list
        /// is shortened by one.
        /// </summary>
        /// <typeparam name="T">Source type of elements</typeparam>
        /// <param name="list">List to perform removal.</param>
        /// <param name="index">The index of the item to delete.</param>
        public static void RemoveAtSwapBack<T>(this List<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);
        }
    }

    public static class ArrayExtensions
    {
        public static int IndexOf<T>(this NativeArray<T> array, T value) where T : struct, IComparable<T>
        {
            for (int i = 0; i != array.Length; i++)
            {
                if (array[i].CompareTo(value) == 0)
                    return i;
            }
            return -1;
        }
    }
}
