#if !UNITY_JOBS_LESS_THAN_0_7
using System;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using System.Diagnostics;
using Unity.Jobs;

namespace Unity.Jobs
{
    [JobProducerType(typeof(IJobParallelForExtensionsBurstSchedulable.JobParallelForBurstSchedulableProducer<>))]
    public interface IJobParallelForBurstSchedulable
    {
        void Execute(int index);
    }

    public static class IJobParallelForExtensionsBurstSchedulable
    {
        internal struct JobParallelForBurstSchedulableProducer<T> where T : struct, IJobParallelForBurstSchedulable
        {
            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobParallelForBurstSchedulableProducer<T>>();

            [Preserve]
            internal static void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
            }

            internal delegate void ExecuteJobFunction(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                        break;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);
#endif

                    var endThatCompilerCanSeeWillNeverChange = end;
                    for (var i = begin; i < endThatCompilerCanSeeWillNeverChange; ++i)
                        jobData.Execute(i);
                }
            }
        }

        /// <summary>
        /// This method is only to be called by automatically generated setup code.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void EarlyJobInit<T>()
            where T : struct, IJobParallelForBurstSchedulable
        {
            JobParallelForBurstSchedulableProducer<T>.Initialize();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReflectionDataCorrect(IntPtr reflectionData)
        {
            if (reflectionData == IntPtr.Zero)
                throw new InvalidOperationException("Reflection data was not set up by a call to Initialize()");
        }

        unsafe public static JobHandle Schedule<T>(this T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBurstSchedulable
        {
            var reflectionData = JobParallelForBurstSchedulableProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, dependsOn, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, innerloopBatchCount);
        }

        unsafe public static void Run<T>(this T jobData, int arrayLength) where T : struct, IJobParallelForBurstSchedulable
        {
            var reflectionData = JobParallelForBurstSchedulableProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, new JobHandle(), ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }
    }
}
#endif
