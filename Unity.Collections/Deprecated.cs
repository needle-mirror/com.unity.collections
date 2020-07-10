using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    ///
    /// </summary>
#if UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE
    [Obsolete("UnsafeUtilityEx is deprecated use UnsafeUtility instead. (RemovedAfter 2020-08-12). -- please remove the UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE define in the Unity.Collections assembly definition file if this message is unexpected and you want to attempt an automatic upgrade.", false)]
#else
    [Obsolete("UnsafeUtilityEx is deprecated use UnsafeUtility instead. (RemovedAfter 2020-08-12). (UnityUpgradable) -> UnsafeUtility", false)]
#endif
    public unsafe static class UnsafeUtilityEx
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="from"></param>
        /// <returns></returns>
        public static ref T As<U, T>(ref U from)
        {
            return ref UnsafeUtility.As<U, T>(ref from);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static ref T AsRef<T>(void* ptr) where T : struct
        {
            return ref UnsafeUtility.AsRef<T>(ptr);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ptr"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static ref T ArrayElementAsRef<T>(void* ptr, int index) where T : struct
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(ptr, index);
        }
    }
}

namespace Unity.Collections
{
    /// <summary>
    ///
    /// </summary>
    [Obsolete("IJobNativeMultiHashMapMergedSharedKeyIndices is obsolete. (RemovedAfter 2020-07-07)", false)]
    [JobProducerType(typeof(JobNativeMultiHashMapUniqueHashExtensions.JobNativeMultiHashMapMergedSharedKeyIndicesProducer<>))]
    public interface IJobNativeMultiHashMapMergedSharedKeyIndices
    {
        /// <summary>
        /// The first time each key (=hash) is encountered, ExecuteFirst() is invoked with corresponding value (=index).
        /// </summary>
        /// <param name="index"></param>
        void ExecuteFirst(int index);

        /// <summary>
        /// For each subsequent instance of the same key in the bucket, ExecuteNext() is invoked with the corresponding
        /// value (=index) for that key, as well as the value passed to ExecuteFirst() the first time this key
        /// was encountered (=firstIndex).
        /// </summary>
        /// <param name="firstIndex"></param>
        /// <param name="index"></param>
        void ExecuteNext(int firstIndex, int index);
    }

    /// <summary>
    ///
    /// </summary>
    [Obsolete("JobNativeMultiHashMapUniqueHashExtensions is obsolete. (RemovedAfter 2020-07-07)", false)]
    public static class JobNativeMultiHashMapUniqueHashExtensions
    {
        internal struct JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob>
            where TJob : struct, IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            [ReadOnly] public NativeMultiHashMap<int, int> HashMap;
            internal TJob JobData;

            private static IntPtr s_JobReflectionData;

            internal static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return s_JobReflectionData;
            }

            delegate void ExecuteJobFunction(ref JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob> jobProducer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            /// <summary>
            ///
            /// </summary>
            /// <param name="jobProducer"></param>
            /// <param name="additionalPtr"></param>
            /// <param name="bufferRangePatchData"></param>
            /// <param name="ranges"></param>
            /// <param name="jobIndex"></param>
            public static unsafe void Execute(ref JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob> jobProducer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    var bucketData = jobProducer.HashMap.GetUnsafeBucketData();
                    var buckets = (int*)bucketData.buckets;
                    var nextPtrs = (int*)bucketData.next;
                    var keys = bucketData.keys;
                    var values = bucketData.values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<int>(keys, entryIndex);
                            var value = UnsafeUtility.ReadArrayElement<int>(values, entryIndex);
                            int firstValue;

                            NativeMultiHashMapIterator<int> it;
                            jobProducer.HashMap.TryGetFirstValue(key, out firstValue, out it);

                            // [macton] Didn't expect a usecase for this with multiple same values
                            // (since it's intended use was for unique indices.)
                            // https://forum.unity.com/threads/ijobnativemultihashmapmergedsharedkeyindices-unexpected-behavior.569107/#post-3788170
                            if (entryIndex == it.EntryIndex)
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobProducer), value, 1);
#endif
                                jobProducer.JobData.ExecuteFirst(value);
                            }
                            else
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                var startIndex = math.min(firstValue, value);
                                var lastIndex = math.max(firstValue, value);
                                var rangeLength = (lastIndex - startIndex) + 1;

                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobProducer), startIndex, rangeLength);
#endif
                                jobProducer.JobData.ExecuteNext(firstValue, value);
                            }

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="hashMap"></param>
        /// <param name="minIndicesPerJobCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static unsafe JobHandle Schedule<TJob>(this TJob jobData, NativeMultiHashMap<int, int> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            var jobProducer = new JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer)
                , JobNativeMultiHashMapMergedSharedKeyIndicesProducer<TJob>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
            );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.GetUnsafeBucketData().bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Obsolete("IJobNativeMultiHashMapVisitKeyValue is obsolete. (RemovedAfter 2020-07-07)", false)]
    [JobProducerType(typeof(JobNativeMultiHashMapVisitKeyValue.JobNativeMultiHashMapVisitKeyValueProducer<,,>))]
    public interface IJobNativeMultiHashMapVisitKeyValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void ExecuteNext(TKey key, TValue value);
    }

    /// <summary>
    ///
    /// </summary>
    [Obsolete("JobNativeMultiHashMapVisitKeyValue is obsolete. (RemovedAfter 2020-07-07)", false)]
    public static class JobNativeMultiHashMapVisitKeyValue
    {
        internal struct JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>
            where TJob : struct, IJobNativeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            [ReadOnly] public NativeMultiHashMap<TKey, TValue> HashMap;
            internal TJob JobData;

            static IntPtr s_JobReflectionData;

            internal static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return s_JobReflectionData;
            }

            internal delegate void ExecuteJobFunction(ref JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            /// <summary>
            ///
            /// </summary>
            /// <param name="producer"></param>
            /// <param name="additionalPtr"></param>
            /// <param name="bufferRangePatchData"></param>
            /// <param name="ranges"></param>
            /// <param name="jobIndex"></param>
            public static unsafe void Execute(ref JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    UnsafeHashMapData* hashMapData = producer.HashMap.m_MultiHashMapData.m_Buffer;

                    var bucketData = producer.HashMap.GetUnsafeBucketData();
                    var buckets = (int*)bucketData.buckets;
                    var nextPtrs = (int*)bucketData.next;
                    var keys = bucketData.keys;
                    var values = bucketData.values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);
                            var value = UnsafeUtility.ReadArrayElement<TValue>(values, entryIndex);

                            producer.JobData.ExecuteNext(key, value);

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="hashMap"></param>
        /// <param name="minIndicesPerJobCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(this TJob jobData, NativeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobNativeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var jobProducer = new JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer)
                , JobNativeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
            );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.GetUnsafeBucketData().bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Obsolete("IJobNativeMultiHashMapVisitKeyMutableValue is obsolete. (RemovedAfter 2020-07-07)", false)]
    [JobProducerType(typeof(JobNativeMultiHashMapVisitKeyMutableValue.JobNativeMultiHashMapVisitKeyMutableValueProducer<,,>))]
    public interface IJobNativeMultiHashMapVisitKeyMutableValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void ExecuteNext(TKey key, ref TValue value);
    }

    /// <summary>
    ///
    /// </summary>
    [Obsolete("JobNativeMultiHashMapVisitKeyMutableValue is obsolete. (RemovedAfter 2020-07-07)", false)]
    public static class JobNativeMultiHashMapVisitKeyMutableValue
    {
        internal struct JobNativeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue>
            where TJob : struct, IJobNativeMultiHashMapVisitKeyMutableValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            [NativeDisableContainerSafetyRestriction]
            internal NativeMultiHashMap<TKey, TValue> HashMap;
            internal TJob JobData;

            static IntPtr s_JobReflectionData;

            internal static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobNativeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return s_JobReflectionData;
            }

            internal delegate void ExecuteJobFunction(ref JobNativeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref JobNativeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    var bucketData = producer.HashMap.GetUnsafeBucketData();
                    var buckets = (int*)bucketData.buckets;
                    var nextPtrs = (int*)bucketData.next;
                    var keys = bucketData.keys;
                    var values = bucketData.values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);

                            producer.JobData.ExecuteNext(key, ref UnsafeUtility.ArrayElementAsRef<TValue>(values, entryIndex));

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="hashMap"></param>
        /// <param name="minIndicesPerJobCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(this TJob jobData, NativeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobNativeMultiHashMapVisitKeyMutableValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var jobProducer = new JobNativeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer)
                , JobNativeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
            );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.GetUnsafeBucketData().bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }

    /// <summary>
    ///
    /// </summary>
    [Obsolete("IJobUnsafeMultiHashMapMergedSharedKeyIndices is obsolete. (RemovedAfter 2020-07-07)", false)]
    [JobProducerType(typeof(JobUnsafeMultiHashMapUniqueHashExtensions.JobUnsafeMultiHashMapMergedSharedKeyIndicesProducer<>))]
    public interface IJobUnsafeMultiHashMapMergedSharedKeyIndices
    {
        /// <summary>
        /// The first time each key (=hash) is encountered, ExecuteFirst() is invoked with corresponding value (=index).
        /// </summary>
        /// <param name="index"></param>
        void ExecuteFirst(int index);

        /// <summary>
        /// For each subsequent instance of the same key in the bucket, ExecuteNext() is invoked with the corresponding
        /// value (=index) for that key, as well as the value passed to ExecuteFirst() the first time this key
        /// was encountered (=firstIndex).
        /// </summary>
        /// <param name="firstIndex"></param>
        /// <param name="index"></param>
        void ExecuteNext(int firstIndex, int index);
    }

    /// <summary>
    ///
    /// </summary>
    [Obsolete("JobUnsafeMultiHashMapUniqueHashExtensions is obsolete. (RemovedAfter 2020-07-07)", false)]
    public static class JobUnsafeMultiHashMapUniqueHashExtensions
    {
        internal struct JobUnsafeMultiHashMapMergedSharedKeyIndicesProducer<TJob>
            where TJob : struct, IJobUnsafeMultiHashMapMergedSharedKeyIndices
        {
            [ReadOnly] public UnsafeMultiHashMap<int, int> HashMap;
            internal TJob JobData;

            static IntPtr s_JobReflectionData;

            internal static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobUnsafeMultiHashMapMergedSharedKeyIndicesProducer<TJob>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return s_JobReflectionData;
            }

            private delegate void ExecuteJobFunction(ref JobUnsafeMultiHashMapMergedSharedKeyIndicesProducer<TJob> jobProducer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            internal static unsafe void Execute(ref JobUnsafeMultiHashMapMergedSharedKeyIndicesProducer<TJob> jobProducer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    UnsafeHashMapData* hashMapData = jobProducer.HashMap.m_Buffer;
                    var buckets = (int*)hashMapData->buckets;
                    var nextPtrs = (int*)hashMapData->next;
                    var keys = hashMapData->keys;
                    var values = hashMapData->values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<int>(keys, entryIndex);
                            var value = UnsafeUtility.ReadArrayElement<int>(values, entryIndex);
                            int firstValue;

                            NativeMultiHashMapIterator<int> it;
                            jobProducer.HashMap.TryGetFirstValue(key, out firstValue, out it);

                            // [macton] Didn't expect a usecase for this with multiple same values
                            // (since it's intended use was for unique indices.)
                            // https://forum.unity.com/threads/ijobnativemultihashmapmergedsharedkeyindices-unexpected-behavior.569107/#post-3788170
                            if (entryIndex == it.EntryIndex)
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobProducer), value, 1);
#endif
                                jobProducer.JobData.ExecuteFirst(value);
                            }
                            else
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                var startIndex = math.min(firstValue, value);
                                var lastIndex = math.max(firstValue, value);
                                var rangeLength = (lastIndex - startIndex) + 1;

                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobProducer), startIndex, rangeLength);
#endif
                                jobProducer.JobData.ExecuteNext(firstValue, value);
                            }

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="hashMap"></param>
        /// <param name="minIndicesPerJobCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static unsafe JobHandle Schedule<TJob>(this TJob jobData, UnsafeMultiHashMap<int, int> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobUnsafeMultiHashMapMergedSharedKeyIndices
        {
            var jobProducer = new JobUnsafeMultiHashMapMergedSharedKeyIndicesProducer<TJob>
            {
                HashMap = hashMap,
                JobData = jobData,
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer)
                , JobUnsafeMultiHashMapMergedSharedKeyIndicesProducer<TJob>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
            );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.m_Buffer->bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Obsolete("IJobUnsafeMultiHashMapVisitKeyValue is obsolete. (RemovedAfter 2020-07-07)", false)]
    [JobProducerType(typeof(JobUnsafeMultiHashMapVisitKeyValue.JobUnsafeMultiHashMapVisitKeyValueProducer<,,>))]
    public interface IJobUnsafeMultiHashMapVisitKeyValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void ExecuteNext(TKey key, TValue value);
    }

    /// <summary>
    ///
    /// </summary>
    [Obsolete("JobUnsafeMultiHashMapVisitKeyValue is obsolete. (RemovedAfter 2020-07-07)", false)]
    public static class JobUnsafeMultiHashMapVisitKeyValue
    {
        internal struct JobUnsafeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>
            where TJob : struct, IJobUnsafeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            [ReadOnly] public UnsafeMultiHashMap<TKey, TValue> HashMap;
            internal TJob JobData;

            static IntPtr s_JobReflectionData;

            internal static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobUnsafeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return s_JobReflectionData;
            }

            internal delegate void ExecuteJobFunction(ref JobUnsafeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            internal static unsafe void Execute(ref JobUnsafeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    UnsafeHashMapData* hashMapData = producer.HashMap.m_Buffer;
                    var buckets = (int*)hashMapData->buckets;
                    var nextPtrs = (int*)hashMapData->next;
                    var keys = hashMapData->keys;
                    var values = hashMapData->values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);
                            var value = UnsafeUtility.ReadArrayElement<TValue>(values, entryIndex);

                            producer.JobData.ExecuteNext(key, value);

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="hashMap"></param>
        /// <param name="minIndicesPerJobCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(this TJob jobData, UnsafeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobUnsafeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var jobProducer = new JobUnsafeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer)
                , JobUnsafeMultiHashMapVisitKeyValueProducer<TJob, TKey, TValue>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
            );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.m_Buffer->bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Obsolete("IJobUnsafeMultiHashMapVisitKeyMutableValue is obsolete. (RemovedAfter 2020-07-07)", false)]
    [JobProducerType(typeof(JobUnsafeMultiHashMapVisitKeyMutableValue.JobUnsafeMultiHashMapVisitKeyMutableValueProducer<,,>))]
    public interface IJobUnsafeMultiHashMapVisitKeyMutableValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void ExecuteNext(TKey key, ref TValue value);
    }

    /// <summary>
    ///
    /// </summary>
    [Obsolete("JobUnsafeMultiHashMapVisitKeyMutableValue is obsolete. (RemovedAfter 2020-07-07)", false)]
    public static class JobUnsafeMultiHashMapVisitKeyMutableValue
    {
        internal struct JobUnsafeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue>
            where TJob : struct, IJobUnsafeMultiHashMapVisitKeyMutableValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            [NativeDisableContainerSafetyRestriction]
            internal UnsafeMultiHashMap<TKey, TValue> HashMap;
            internal TJob JobData;

            static IntPtr s_JobReflectionData;

            internal static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobUnsafeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return s_JobReflectionData;
            }

            internal delegate void ExecuteJobFunction(ref JobUnsafeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            internal static unsafe void Execute(ref JobUnsafeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue> producer, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    var buckets = (int*)producer.HashMap.m_Buffer->buckets;
                    var nextPtrs = (int*)producer.HashMap.m_Buffer->next;
                    var keys = producer.HashMap.m_Buffer->keys;
                    var values = producer.HashMap.m_Buffer->values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);

                            producer.JobData.ExecuteNext(key, ref UnsafeUtility.ArrayElementAsRef<TValue>(values, entryIndex));

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="hashMap"></param>
        /// <param name="minIndicesPerJobCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(this TJob jobData, UnsafeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobUnsafeMultiHashMapVisitKeyMutableValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var jobProducer = new JobUnsafeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer)
                , JobUnsafeMultiHashMapVisitKeyMutableValueProducer<TJob, TKey, TValue>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
            );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.m_Buffer->bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }
}

#if !UNITY_DOTSRUNTIME
namespace Unity.Collections.Experimental
{
    internal unsafe struct StructLayoutData4
    {
        public const int ChunkSizeBytes = 32;
        public const int ElementSize = 4;
        public const int ElementsPerChunk = ChunkSizeBytes / ElementSize;

        internal struct FieldInfo
        {
            public int Offset;
            //public int Size;
        }

        public bool IsCreated => m_Fields != null;
        public int FieldCount => m_Fields.Length;

        private FieldInfo[] m_Fields;

        public StructLayoutData4(Type t)
        {
            m_Fields = ComputeFieldInfo(t);
        }

        /// <summary>
        /// Compute how many chunks of ChunkSizeBytes are needed to store count elements of data.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public int ChunksNeeded(int count)
        {
            return (count + ElementsPerChunk - 1) >> 3;
        }

        public int ChunkIndex(int element)
        {
            return element >> 3;
        }

        public int ChunkOffset(int element)
        {
            return element & (ElementsPerChunk - 1);
        }

        public FieldInfo GetFieldInfo(int attrIndex)
        {
            return m_Fields[attrIndex];
        }

        private static FieldInfo[] ComputeFieldInfo(Type t)
        {
            var result = new List<FieldInfo>();
            FindFields(result, t, 0);
            return result.ToArray();
        }

        private static void FindFields(List<FieldInfo> result, Type type, int parentOffset)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                int offset = parentOffset + UnsafeUtility.GetFieldOffset(field);

                if (field.FieldType.IsPrimitive || field.FieldType.IsPointer)
                {
                    int sizeOf = -1;

                    if (field.FieldType.IsPointer)
                        sizeOf = UnsafeUtility.SizeOf<IntPtr>();
                    else
                        sizeOf = UnsafeUtility.SizeOf(field.FieldType);

                    if ((sizeOf & (sizeOf - 1)) != 0)
                    {
                        throw new ArgumentException($"Field {type}.{field} is of size {sizeOf} which is not a power of two");
                    }

                    if (sizeOf != ElementSize)
                    {
                        throw new ArgumentException($"Field {type}.{field} is of size {sizeOf}; currently only types of size {ElementSize} bytes are allowed");
                    }

                    result.Add(new FieldInfo { Offset = offset /*, Size = sizeOf */ });
                }
                else
                {
                    FindFields(result, field.FieldType, offset);
                }
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("NativeArrayChunked8 is obsolete. (RemovedAfter 2020-08-07)", false)]
    public unsafe struct NativeArrayChunked8<T> : IDisposable where T : struct
    {
        private static StructLayoutData4 ms_CachedLayout;

        private byte* m_Base;
        private int m_Length;
        private Allocator m_Allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        /// <summary>
        ///
        /// </summary>
        public int Length => m_Length;

        /// <summary>
        ///
        /// </summary>
        public Allocator Allocator => m_Allocator;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadAccess(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range in NativeArrayChunked8 of '{1}' Length.", index, m_Length));
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWriteAccess(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range in NativeArrayChunked8 of '{1}' Length.", index, m_Length));
#endif
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="length"></param>
        /// <param name="label"></param>
        public NativeArrayChunked8(int length, Allocator label)
            : this(length, label, 2)
        {
        }

        NativeArrayChunked8(int length, Allocator label, int disposeSentinelStackDepth)
        {
            CollectionHelper.CheckIsUnmanaged<T>();
            if (!ms_CachedLayout.IsCreated)
            {
                ms_CachedLayout = new StructLayoutData4(typeof(T));
            }

            m_Base = (byte*)UnsafeUtility.Malloc(StructLayoutData4.ChunkSizeBytes * ms_CachedLayout.ChunksNeeded(length) * ms_CachedLayout.FieldCount, StructLayoutData4.ChunkSizeBytes, label);
            m_Length = length;
            m_Allocator = label;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, label);
#endif
        }

        /// <summary>
        ///
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

            if (m_Base != null)
            {
                UnsafeUtility.Free(m_Base, m_Allocator);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                CheckReadAccess(index);

                T result = default(T);

                int chunkIndex = ms_CachedLayout.ChunkIndex(index);
                int chunkOffset = ms_CachedLayout.ChunkOffset(index);

                int fieldCount = ms_CachedLayout.FieldCount;

                uint* bp = (uint*)(m_Base + StructLayoutData4.ChunkSizeBytes * ms_CachedLayout.FieldCount * chunkIndex + StructLayoutData4.ElementSize * chunkOffset);
                uint* target = (uint*)UnsafeUtility.AddressOf(ref result);

                for (int field = 0; field < fieldCount; ++field)
                {
                    var fieldInfo = ms_CachedLayout.GetFieldInfo(field);
                    target[fieldInfo.Offset / 4] = *bp;
                    bp += StructLayoutData4.ElementsPerChunk;
                }

                return result;
            }

            set
            {
                CheckWriteAccess(index);

                int chunkIndex = ms_CachedLayout.ChunkIndex(index);
                int chunkOffset = ms_CachedLayout.ChunkOffset(index);

                int fieldCount = ms_CachedLayout.FieldCount;

                uint* bp = (uint*)UnsafeUtility.AddressOf(ref value);
                uint* target = (uint*)(m_Base + StructLayoutData4.ChunkSizeBytes * ms_CachedLayout.FieldCount * chunkIndex + StructLayoutData4.ElementSize * chunkOffset);

                for (int field = 0; field < fieldCount; ++field)
                {
                    var fieldInfo = ms_CachedLayout.GetFieldInfo(field);
                    *target = bp[fieldInfo.Offset / 4];
                    target += StructLayoutData4.ElementsPerChunk;
                }
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Obsolete("NativeArrayFullSOA is obsolete. (RemovedAfter 2020-08-07)", false)]
    public unsafe struct NativeArrayFullSOA<T> : IDisposable where T : struct
    {
        private static StructLayoutData4 ms_CachedLayout;

        [NativeDisableUnsafePtrRestriction]
        private byte* m_Base;
        private int m_Length;
        private Allocator m_Allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        /// <summary>
        ///
        /// </summary>
        public int Length => m_Length;

        /// <summary>
        ///
        /// </summary>
        public Allocator Allocator => m_Allocator;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckReadAccess(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range in NativeArrayFullSOA of '{1}' Length.", index, m_Length));
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWriteAccess(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            if ((uint)index >= (uint)m_Length)
                throw new System.IndexOutOfRangeException(string.Format("Index {0} is out of range in NativeArrayFullSOA of '{1}' Length.", index, m_Length));
#endif
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="length"></param>
        /// <param name="label"></param>
        public NativeArrayFullSOA(int length, Allocator label)
            : this(length, label, 2)
        {
        }

        NativeArrayFullSOA(int length, Allocator label, int disposeSentinelStackDepth)
        {
            CollectionHelper.CheckIsUnmanaged<T>();
            if (!ms_CachedLayout.IsCreated)
            {
                ms_CachedLayout = new StructLayoutData4(typeof(T));
            }

            m_Base = (byte*)UnsafeUtility.Malloc(4 * length * ms_CachedLayout.FieldCount, StructLayoutData4.ChunkSizeBytes, label);
            m_Length = length;
            m_Allocator = label;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, label);
#endif
        }

        /// <summary>
        ///
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

            if (m_Base != null)
            {
                UnsafeUtility.Free((void*)m_Base, m_Allocator);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                CheckReadAccess(index);

                T result = default(T);
                uint* target = (uint*)UnsafeUtility.AddressOf(ref result);

                int fieldCount = ms_CachedLayout.FieldCount;
                int stride = m_Length;

                uint* bp = (uint*)(m_Base + 4 * index);
                for (int field = 0; field < fieldCount; ++field)
                {
                    var fieldInfo = ms_CachedLayout.GetFieldInfo(field);
                    target[fieldInfo.Offset / 4] = *bp;
                    bp += stride;
                }

                return result;
            }

            set
            {
                CheckWriteAccess(index);

                int fieldCount = ms_CachedLayout.FieldCount;

                int stride = m_Length;

                uint* bp = (uint*)UnsafeUtility.AddressOf(ref value);
                uint* target = (uint*)(m_Base + 4 * index);

                for (int field = 0; field < fieldCount; ++field)
                {
                    var fieldInfo = ms_CachedLayout.GetFieldInfo(field);
                    *target = bp[fieldInfo.Offset / 4];
                    target += stride;
                }
            }
        }
    }
}
#endif
