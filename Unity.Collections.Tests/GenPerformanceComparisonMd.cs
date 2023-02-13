using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using System.Collections.Generic;
using System;
using System.Text;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

internal struct StopWatch
{
    double m_Start;

    internal void Start()
    {
        m_Start = UnityEngine.Time.realtimeSinceStartupAsDouble;
    }
    internal static StopWatch StartNew()
    {
        var sw = new StopWatch();
        sw.Start();
        return sw;
    }

    internal double Current => UnityEngine.Time.realtimeSinceStartupAsDouble;

    internal double Elapsed => Current - m_Start;
}

internal struct BenchmarkReport
    : IDisposable
{
    struct Benchmark
    {
        internal FixedString512Bytes name;
        internal double elapsed;
        internal double speedup;
        internal int count;
    }

    UnsafeList<Benchmark> m_Report;
    StopWatch m_stopWatch;
    Benchmark m_current;
    int m_split;

    internal void Init()
    {
        m_split = -1;
        m_Report = new UnsafeList<Benchmark>(1, Allocator.Persistent);
        m_Report.Clear();
        m_stopWatch.Start();
    }

    public void Dispose()
    {
        m_Report.Dispose();
    }

    internal void Start(string name)
    {
        m_current.name = name;
        m_current.count = 0;
        m_stopWatch.Start();
    }

    internal void End(int count = 1)
    {
        m_current.elapsed = m_stopWatch.Elapsed;
        m_current.count = count;

        if (m_split == -1)
        {
            m_current.speedup = 1.0;
            m_Report.Add(m_current);
        }
        else
        {
            UnityEngine.Debug.Assert(m_Report[m_split].name == m_current.name);
            UnityEngine.Debug.Assert(m_Report[m_split].count == m_current.count);

            m_current.speedup = m_Report[m_split].elapsed / m_current.elapsed;

            m_Report[m_split] = m_current;

            ++m_split;
        }

        m_current = default;
    }

    internal static BenchmarkReport StartNew()
    {
        var br = new BenchmarkReport();
        br.Init();
        return br;
    }

    internal void Split()
    {
        m_split = 0;
    }

    internal static double toMiliseconds(double seconds)
    {
        return seconds * 1000.0;
    }

    internal void Dump(ref StringBuilder sb)
    {
        sb.Append("| Benchmark                                      | Elapsed [ms]  | Count      | Average [ms] | Speed-up    |\n");
        sb.Append("| ---------------------------------------------- | ------------- | ---------- | ------------ | ----------- |\n");

        foreach (var report in m_Report)
        {
            unsafe
            {
                sb.AppendFormat("| {0,-46} | {1,13:F6} | {2,10} | {3,12:F6} | {4,10:F3}x |\n"
                    , report.name
                    , toMiliseconds(report.elapsed)
                    , report.count
                    , toMiliseconds(report.elapsed) / report.count
                    , report.speedup
                    );
            }
        }

        sb.Append("\n");
    }
}

internal interface IHashMapWrapper<TKey, TValue>
    : IDisposable
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    int GetCount();
    void Add(TKey key, TValue item);
    bool TryGetValue(TKey key, out TValue item);
    bool Remove(TKey key);
    void Clear();
}

internal interface IListWrapper<T>
    : IDisposable
    where T : unmanaged
{
    public T ElementAt(int index);
    int GetCount();
    void Add(T item);
    void Insert(int index, T item);
    void RemoveAt(int index);
    void RemoveAtSwapBack(int index);
    void TrimExcess();
    void Clear();
}

internal struct BenchTest
{
    internal static void HashMapRepeatInsert<T>(ref T container, int count)
        where T : IHashMapWrapper<int, int>
    {
        for (int i = 0; i < count; i++)
        {
            container.Add(i, i);
        }
    }

    internal static int HashMapRepeatLookup<T>(ref T container, int count)
        where T : IHashMapWrapper<int, int>
    {
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            int x;
            container.TryGetValue(i, out x);
            sum += x;
        }
        return sum;
    }

    internal static int HashMapForEach<T>(ref T container)
        where T : IHashMapWrapper<int, int>
    {
        int sum = 0;
        //        foreach (var kv in container)
        //        {
        //            sum += kv.Value;
        //        }
        return sum;
    }

    internal static void HashMapRepeatRemove<T>(ref T container, int count)
        where T : IHashMapWrapper<int, int>
    {
        for (int i = 0; i < count; i++)
        {
            container.Remove(i);
        }
    }

    internal static int HashMapRepeatGetCount<T>(ref T container, int count)
        where T : IHashMapWrapper<int, int>
    {
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += container.GetCount();
        }
        return sum;
    }

    internal static int ListRepeatLookup<T>(ref T container, int count)
        where T : IListWrapper<int>
    {
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += container.ElementAt(i);
        }
        return sum;
    }

    internal static void ListRepeatAdd<T>(ref T container, int count)
        where T : IListWrapper<int>
    {
        for (int i = 0; i < count; i++)
        {
            container.Add(i);
        }
    }

    internal static void ListRepeatInsert<T>(ref T container, int count)
        where T : IListWrapper<int>
    {
        for (int i = 0; i < count; i++)
        {
            container.Insert(0, i);
        }
    }

    internal static void ListRepeatRemove<T>(ref T container, int count)
        where T : IListWrapper<int>
    {
        for (int i = 0; i < count; i++)
        {
            container.RemoveAt(0);
        }
    }

    internal static void ListRepeatRemoveAtSwapBack<T>(ref T container, int count)
        where T : IListWrapper<int>
    {
        for (int i = 0; i < count; i++)
        {
            container.RemoveAtSwapBack(0);
        }
    }
}

internal struct DictionaryWrapper<TKey, TValue>
    : IHashMapWrapper<TKey, TValue>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    internal Dictionary<TKey, TValue> Container;

    public DictionaryWrapper(int capacity, AllocatorManager.AllocatorHandle allocator)
    {
        Container = new Dictionary<TKey, TValue>(capacity);
    }

    public int GetCount() => Container.Count;
    public void Add(TKey key, TValue item) => Container.Add(key, item);
    public bool TryGetValue(TKey key, out TValue item) => Container.TryGetValue(key, out item);
    public bool Remove(TKey key) => Container.Remove(key);
    public void Clear() => Container.Clear();
    public void Dispose() { }
}

internal struct UnsafeHashMapWrapper<TKey, TValue>
    : IHashMapWrapper<TKey, TValue>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    internal UnsafeHashMap<TKey, TValue> Container;

    public UnsafeHashMapWrapper(int capacity, AllocatorManager.AllocatorHandle allocator)
    {
        Container = new UnsafeHashMap<TKey, TValue>(capacity, allocator);
    }

    public int GetCount() => Container.Count;
    public void Add(TKey key, TValue item) => Container.Add(key, item);
    public bool TryGetValue(TKey key, out TValue item) => Container.TryGetValue(key, out item);
    public bool Remove(TKey key) => Container.Remove(key);
    public void Clear() => Container.Clear();
    public void Dispose() => Container.Dispose();
}

internal struct NativeHashMapWrapper<TKey, TValue>
    : IHashMapWrapper<TKey, TValue>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    internal NativeHashMap<TKey, TValue> Container;

    public NativeHashMapWrapper(int capacity, AllocatorManager.AllocatorHandle allocator)
    {
        Container = new NativeHashMap<TKey, TValue>(capacity, allocator);
    }

    public int GetCount() => Container.Count;
    public void Add(TKey key, TValue item) => Container.Add(key, item);
    public bool TryGetValue(TKey key, out TValue item) => Container.TryGetValue(key, out item);
    public bool Remove(TKey key) => Container.Remove(key);
    public void Clear() => Container.Clear();
    public void Dispose() => Container.Dispose();
}

internal struct UnsafeParallelHashMapWrapper<TKey, TValue>
    : IHashMapWrapper<TKey, TValue>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    internal UnsafeParallelHashMap<TKey, TValue> Container;

    public UnsafeParallelHashMapWrapper(int capacity, AllocatorManager.AllocatorHandle allocator)
    {
        Container = new UnsafeParallelHashMap<TKey, TValue>(capacity, allocator);
    }

    public int GetCount() => Container.Count();
    public void Add(TKey key, TValue item) => Container.Add(key, item);
    public bool TryGetValue(TKey key, out TValue item) => Container.TryGetValue(key, out item);
    public bool Remove(TKey key) => Container.Remove(key);
    public void Clear() => Container.Clear();
    public void Dispose() => Container.Dispose();
}

internal struct NativeParallelHashMapWrapper<TKey, TValue>
    : IHashMapWrapper<TKey, TValue>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    internal NativeParallelHashMap<TKey, TValue> Container;

    public NativeParallelHashMapWrapper(int capacity, AllocatorManager.AllocatorHandle allocator)
    {
        Container = new NativeParallelHashMap<TKey, TValue>(capacity, allocator);
    }

    public int GetCount() => Container.Count();
    public void Add(TKey key, TValue item) => Container.Add(key, item);
    public bool TryGetValue(TKey key, out TValue item) => Container.TryGetValue(key, out item);
    public bool Remove(TKey key) => Container.Remove(key);
    public void Clear() => Container.Clear();
    public void Dispose() => Container.Dispose();
}

internal struct ListWrapper<T>
    : IListWrapper<T>
    where T : unmanaged
{
    internal List<T> Container;

    public ListWrapper(int capacity, AllocatorManager.AllocatorHandle allocator)
    {
        Container = new List<T>(capacity);
    }

    public T ElementAt(int index) => Container[index];
    public int GetCount() => Container.Count;
    public void Add(T item) => Container.Add(item);
    public void Insert(int index, T item) => Container.Insert(index, item);
    public void RemoveAt(int index) => Container.RemoveAt(index);
    public void RemoveAtSwapBack(int index) => Container.RemoveAt(index);
    public void TrimExcess() => Container.TrimExcess();
    public void Clear() => Container.Clear();
    public void Dispose() { }
}

internal struct UnsafeListWrapper<T>
    : IListWrapper<T>
    where T : unmanaged
{
    internal UnsafeList<T> Container;

    public UnsafeListWrapper(int capacity, AllocatorManager.AllocatorHandle allocator)
    {
        Container = new UnsafeList<T>(capacity, allocator);
    }

    public T ElementAt(int index) => Container[index];
    public int GetCount() => Container.Length;
    public void Add(T item) => Container.Add(item);
    public void Insert(int index, T item) { Container.InsertRange(index, 1); Container[index] = item; }
    public void RemoveAt(int index) => Container.RemoveAt(index);
    public void RemoveAtSwapBack(int index) => Container.RemoveAtSwapBack(index);
    public void TrimExcess() => Container.TrimExcess();
    public void Clear() => Container.Clear();
    public void Dispose() => Container.Dispose();
}

internal struct NativeListWrapper<T>
    : IListWrapper<T>
    where T : unmanaged
{
    internal NativeList<T> Container;

    public NativeListWrapper(int capacity, AllocatorManager.AllocatorHandle allocator)
    {
        Container = new NativeList<T>(capacity, allocator);
    }

    public T ElementAt(int index) => Container[index];
    public int GetCount() => Container.Length;
    public void Add(T item) => Container.Add(item);
    public void Insert(int index, T item) { Container.InsertRange(index, 1); Container[index] = item; }
    public void RemoveAt(int index) => Container.RemoveAt(index);
    public void RemoveAtSwapBack(int index) => Container.RemoveAtSwapBack(index);
    public void TrimExcess() => Container.TrimExcess();
    public void Clear() => Container.Clear();
    public void Dispose() => Container.Dispose();
}

internal struct NotBursted
{
    internal static int BenchTest_IHashMapWrapper<T>(ref BenchmarkReport br, ref T container, int count)
        where T : IHashMapWrapper<int, int>
    {
        var sum = 0;
        container.Clear();

        br.Start("Repeat Insert");
        BenchTest.HashMapRepeatInsert(ref container, count);
        br.End(count);

        br.Start("Repeat Lookup");
        sum += BenchTest.HashMapRepeatLookup(ref container, count);
        br.End(count);

//        br.Start("ForEach");
//        sum += BenchTest.HashMapForEach(ref container);
//        br.End(count);

        br.Start("Repeat Get Count (Full)");
        sum += BenchTest.HashMapRepeatGetCount(ref container, count);
        br.End(count);

        br.Start("Repeat Remove");
        BenchTest.HashMapRepeatRemove(ref container, count);
        br.End(count);

        container.Clear();

        br.Start("Repeat Get Count (Empty)");
        sum += BenchTest.HashMapRepeatGetCount(ref container, count);
        br.End(count);

        BenchTest.HashMapRepeatInsert(ref container, count);

        br.Start("Clear");
        container.Clear();
        br.End();

        container.Dispose();

        return sum;
    }

    internal static int BenchTest_Dictionary(ref BenchmarkReport br, int count)
    {
        var container = new DictionaryWrapper<int, int>(count, Allocator.Persistent);
        return BenchTest_IHashMapWrapper(ref br, ref container, count);
    }

    internal static int BenchTest_UnsafeHashMap(ref BenchmarkReport br, int count)
    {
        var container = new UnsafeHashMapWrapper<int, int>(count, Allocator.Persistent);
        return BenchTest_IHashMapWrapper(ref br, ref container, count);
    }

    internal static int BenchTest_UnsafeParallelHashMap(ref BenchmarkReport br, int count)
    {
        var container = new UnsafeParallelHashMapWrapper<int, int>(count, Allocator.Persistent);
        return BenchTest_IHashMapWrapper(ref br, ref container, count);
    }

    internal static int BenchTest_NativeHashMap(ref BenchmarkReport br, int count)
    {
        var container = new NativeHashMapWrapper<int, int>(count, Allocator.Persistent);
        return BenchTest_IHashMapWrapper(ref br, ref container, count);
    }

    internal static int BenchTest_NativeParallelHashMap(ref BenchmarkReport br, int count)
    {
        var container = new NativeParallelHashMapWrapper<int, int>(count, Allocator.Persistent);
        return BenchTest_IHashMapWrapper(ref br, ref container, count);
    }


    internal static int BenchTest_IListWrapper<T>(ref BenchmarkReport br, ref T container, int count)
        where T : IListWrapper<int>
    {
        var sum = 0;
        container.Clear();

        br.Start("Repeat Add");
        BenchTest.ListRepeatAdd(ref container, count);
        br.End(count);

        br.Start("Repeat Lookup");
        BenchTest.ListRepeatLookup(ref container, count);
        br.End(count);   

        br.Start("Repeat Insert");
        BenchTest.ListRepeatInsert(ref container, count);
        br.End(count);

        br.Start("Repeat Remove");
        BenchTest.ListRepeatRemove(ref container, count);
        br.End(count);

        br.Start("Repeat RemoveAtSwapBack");
        BenchTest.ListRepeatRemoveAtSwapBack(ref container, count);
        br.End(count);

        br.Start("Clear");
        container.Clear();
        br.End();

        container.Dispose();
        return sum;
    }

    internal static int BenchTest_List(ref BenchmarkReport br, int count)
    {
        var container = new ListWrapper<int>(count, Allocator.Persistent);
        return BenchTest_IListWrapper(ref br, ref container, count);
    }

    internal static int BenchTest_UnsafeList(ref BenchmarkReport br, int count)
    {
        var container = new UnsafeListWrapper<int>(count, Allocator.Persistent);
        return BenchTest_IListWrapper(ref br, ref container, count);
    }

    internal static int BenchTest_NativeList(ref BenchmarkReport br, int count)
    {
        var container = new NativeListWrapper<int>(count, Allocator.Persistent);
        return BenchTest_IListWrapper(ref br, ref container, count);
    }
}

[BurstCompile]
internal struct Bursted
{
    [BurstCompile]
    internal static int BenchTest_UnsafeHashMap(ref BenchmarkReport br, int count)
    {
        var container = new UnsafeHashMapWrapper<int, int>(count, Allocator.Persistent);
        return NotBursted.BenchTest_IHashMapWrapper(ref br, ref container, count);
    }

    [BurstCompile]
    internal static int BenchTest_UnsafeParallelHashMap(ref BenchmarkReport br, int count)
    {
        var container = new UnsafeParallelHashMapWrapper<int, int>(count, Allocator.Persistent);
        return NotBursted.BenchTest_IHashMapWrapper(ref br, ref container, count);
    }

    [BurstCompile]
    internal static int BenchTest_NativeHashMap(ref BenchmarkReport br, int count)
    {
        var container = new NativeHashMapWrapper<int, int>(count, Allocator.Persistent);
        return NotBursted.BenchTest_IHashMapWrapper(ref br, ref container, count);
    }

    [BurstCompile]
    internal static int BenchTest_NativeParallelHashMap(ref BenchmarkReport br, int count)
    {
        var container = new NativeParallelHashMapWrapper<int, int>(count, Allocator.Persistent);
        return NotBursted.BenchTest_IHashMapWrapper(ref br, ref container, count);
    }

    [BurstCompile]
    internal static int BenchTest_UnsafeList(ref BenchmarkReport br, int count)
    {
        var container = new UnsafeListWrapper<int>(count, Allocator.Persistent);
        return NotBursted.BenchTest_IListWrapper(ref br, ref container, count);
    }

    [BurstCompile]
    internal static int BenchTest_NativeList(ref BenchmarkReport br, int count)
    {
        var container = new NativeListWrapper<int>(count, Allocator.Persistent);
        return NotBursted.BenchTest_IListWrapper(ref br, ref container, count);
    }
}

internal class GenerateMarkdownTests
{
    [MenuItem("DOTS/Unity.Collections/Generate Benchmark")]
    public static void GeneratePerformanceComparisonMarkdown()
    {
        var insertions = 10000;
        var sb = new StringBuilder();

        sb.Append("# Performance comparison\n");
        sb.Append("\n");

        sb.Append("> **Note**  \n");
        sb.Append(">  \n");
        sb.Append($"> This file is generated on {SystemInfo.processorType}.  \n");
        sb.Append("> To regenerate this file locally use: DOTS -> Unity.Collections -> Generate Benchmark menu.  \n");
        sb.Append("\n");

        sb.Append("## Dictionary\n");
        sb.Append("\n");

        {
            sb.Append("### Dictionary (Reference)\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_Dictionary(ref br, insertions);

            sb.Append("Using `System.Collections.Generic.Dictionary<int, int>`.\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### NativeHashMap (vs Dictionary) / No-Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_Dictionary(ref br, insertions);

            br.Split();
            NotBursted.BenchTest_NativeHashMap(ref br, insertions);

            sb.Append("Using `Unity.Collections.NativeHashMap<int, int>` vs `System.Collections.Generic.Dictionary<int, int>`.\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### UnsafeHashMap (vs NativeHashMap) / No-Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_NativeHashMap(ref br, insertions);

            br.Split();
            NotBursted.BenchTest_UnsafeHashMap(ref br, insertions);

            sb.Append("Using `Unity.Collections.UnsafeHashMap<int, int>` vs `Unity.Collections.NativeHashMap<int, int>`.\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### NativeParallelHashMap (vs Dictionary) / No-Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_Dictionary(ref br, insertions);

            br.Split();
            NotBursted.BenchTest_NativeParallelHashMap(ref br, insertions);

            sb.Append("Using `Unity.Collections.NativeParallelHashMap<int, int>` vs `System.Collections.Generic.Dictionary<int, int>`.\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### UnsafeParallelHashMap (vs Dictionary) / No-Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_Dictionary(ref br, insertions);

            br.Split();
            NotBursted.BenchTest_UnsafeParallelHashMap(ref br, insertions);

            sb.Append("Using `Unity.Collections.UnsafeParallelHashMap<int, int>` vs `System.Collections.Generic.Dictionary<int, int>`.\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### NativeHashMap (vs Dictionary) + Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_Dictionary(ref br, insertions);

            br.Split();
            Bursted.BenchTest_NativeHashMap(ref br, insertions);

            sb.Append("Using `Unity.Collections.NativeHashMap<int, int>` (compiled with Burst) vs `System.Collections.Generic.Dictionary<int, int>`.\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### NativeParallelHashMap (vs Dictionary) + Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_Dictionary(ref br, insertions);

            br.Split();
            Bursted.BenchTest_NativeParallelHashMap(ref br, insertions);

            sb.Append("Using `Unity.Collections.NativeParallelHashMap<int, int>` (compiled with Burst) vs `System.Collections.Generic.Dictionary<int, int>`.\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### UnsafeHashMap(vs NativeHashMap) + Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            Bursted.BenchTest_NativeHashMap(ref br, insertions);

            br.Split();
            Bursted.BenchTest_UnsafeHashMap(ref br, insertions);

            sb.Append("Using `Unity.Collections.UnsafeHashMap<int, int>` (compiled with Burst) vs `Unity.Collections.NativeHashMap<int, int>` (compiled with Burst).\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### UnsafeParallelHashMap (vs Dictionary) + Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_Dictionary(ref br, insertions);

            br.Split();
            Bursted.BenchTest_UnsafeHashMap(ref br, insertions);

            sb.Append("Using `Unity.Collections.UnsafeParallelHashMap<int, int>` (compiled with Burst) vs `System.Collections.Generic.Dictionary<int, int>` (compiled with Burst).\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        sb.Append("## List\n");
        sb.Append("\n");

        {
            sb.Append("### List (Reference)\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_List(ref br, insertions);

            sb.Append("Using `System.Collections.Generic.List<int>`.\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### NativeList (vs List) / No-Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_List(ref br, insertions);

            br.Split();
            NotBursted.BenchTest_NativeList(ref br, insertions);

            sb.Append("Using `Unity.Collections.NativeList<int>` vs `System.Collections.Generic.List<int>`.\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### UnsafeList (vs NativeList) / No-Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_NativeList(ref br, insertions);

            br.Split();
            NotBursted.BenchTest_UnsafeList(ref br, insertions);

            sb.Append("Using `Unity.Collections.UnsafeList<int>` vs `Unity.Collections.NativeList<int>`.\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### NativeList (vs List) + Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            NotBursted.BenchTest_List(ref br, insertions);

            br.Split();
            Bursted.BenchTest_NativeList(ref br, insertions);

            sb.Append("Using `Unity.Collections.NativeList<int>` vs `System.Collections.Generic.List<int>` (compiled with Burst).\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        {
            sb.Append("### UnsafeList (vs NativeList) + Burst\n");
            sb.Append("\n");

            var br = BenchmarkReport.StartNew();

            Bursted.BenchTest_NativeList(ref br, insertions);

            br.Split();
            Bursted.BenchTest_UnsafeList(ref br, insertions);

            sb.Append("Using `Unity.Collections.UnsafeList<int>` vs `Unity.Collections.NativeList<int>` (compiled with Burst).\n");
            sb.Append("\n");

            br.Dump(ref sb);
            br.Dispose();
        }

        File.WriteAllText("../../Packages/com.unity.collections/Documentation~/performance-comparison.md", sb.ToString());
    }
}

#endif // UNITY_EDITOR
