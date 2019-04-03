namespace Unity.Collections
{
    internal static class CollectionHelper
    {
        /// <summary>
        /// Returns the smallest power of two that is greater than or equal to the given integer
        /// </summary>
        public static int CeilPow2(int i)
        {
            i -= 1;
            i |= i >> 1;
            i |= i >> 2;
            i |= i >> 4;
            i |= i >> 8;
            i |= i >> 16;
            return i + 1;
        }
    }
}
