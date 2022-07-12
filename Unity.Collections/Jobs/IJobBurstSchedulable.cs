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
    [JobProducerType(typeof(IJobBurstSchedulableExtensions.JobBurstSchedulableProducer<>))]
    public interface IJobBurstSchedulable
    {
        void Execute();
    }

    public static class IJobBurstSchedulableExtensions
    {
        internal struct JobBurstSchedulableProducer<T> where T : struct, IJobBurstSchedulable
        {
            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobBurstSchedulableProducer<T>>();

            [Preserve]
            internal static void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
            }

            internal delegate void ExecuteJobFunction(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static void Execute(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                data.Execute();
            }
        }
        
        /// <summary>
        /// This method is only to be called by automatically generated setup code.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void EarlyJobInit<T>()
            where T : struct, IJobBurstSchedulable
        {
            JobBurstSchedulableProducer<T>.Initialize();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReflectionDataCorrect(IntPtr reflectionData)
        {
            if (reflectionData == IntPtr.Zero)
                throw new InvalidOperationException("Reflection data was not set up by an Initialize() call");
        }

        unsafe public static JobHandle Schedule<T>(this T jobData, JobHandle dependsOn = new JobHandle()) where T : struct, IJobBurstSchedulable
        {
            var reflectionData = JobBurstSchedulableProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        unsafe public static void Run<T>(this T jobData) where T : struct, IJobBurstSchedulable
        {
            var reflectionData = JobBurstSchedulableProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }
    }
}
#endif
