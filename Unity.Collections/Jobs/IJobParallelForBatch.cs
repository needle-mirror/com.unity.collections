#if !UNITY_JOBS_LESS_THAN_0_7
using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using System.Diagnostics;
using Unity.Burst;

namespace Unity.Jobs
{
    [JobProducerType(typeof(IJobParallelForBatchExtensions.JobParallelForBatchProducer<>))]
    public interface IJobParallelForBatch
    {
        void Execute(int startIndex, int count);
    }

    public static class IJobParallelForBatchExtensions
    {
        internal struct JobParallelForBatchProducer<T> where T : struct, IJobParallelForBatch
        {
            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobParallelForBatchProducer<T>>();

            [Preserve]
            internal static void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
            }

            internal delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
            public unsafe static void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(
                        ref ranges,
                        jobIndex, out int begin, out int end))
                        return;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);
#endif

                    jobData.Execute(begin, end - begin);
                }
            }
        }

        /// <summary>
        /// This method is only to be called by automatically generated setup code.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void EarlyJobInit<T>()
            where T : struct, IJobParallelForBatch
        {
            JobParallelForBatchProducer<T>.Initialize();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReflectionDataCorrect(IntPtr reflectionData)
        {
            if (reflectionData == IntPtr.Zero)
                throw new InvalidOperationException("Reflection data was not set up by an Initialize() call");
        }

        public static unsafe JobHandle ScheduleBatch<T>(this T jobData, int arrayLength, int minIndicesPerJobCount,
            JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBatch
        {
            var reflectionData = JobParallelForBatchProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, dependsOn, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, minIndicesPerJobCount);
        }

        public static unsafe void RunBatch<T>(this T jobData, int arrayLength) where T : struct, IJobParallelForBatch
        {
            var reflectionData = JobParallelForBatchProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, new JobHandle(), ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }
    }
}
#endif
