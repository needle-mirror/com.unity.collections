#if !UNITY_JOBS_LESS_THAN_0_7
using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using System.Diagnostics;
using Unity.Burst;
using Unity.Mathematics;

namespace Unity.Jobs
{
    [JobProducerType(typeof(JobParallelIndexListExtensions.JobParallelForFilterProducer<>))]
    public interface IJobParallelForFilter
    {
        bool Execute(int index);
    }

    public static class JobParallelIndexListExtensions
    {
        internal struct JobParallelForFilterProducer<T> where T : struct, IJobParallelForFilter
        {
            public struct JobWrapper
            {
                [NativeDisableParallelForRestriction]
                public NativeList<int> outputIndices;
                public int appendCount;
                public T JobData;
            }

            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobParallelForFilterProducer<T>>();

            [Preserve]
            public static void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(JobWrapper), typeof(T), (ExecuteJobFunction)Execute);
            }

            public delegate void ExecuteJobFunction(ref JobWrapper jobWrapper, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static void Execute(ref JobWrapper jobWrapper, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (jobWrapper.appendCount == -1)
                    ExecuteFilter(ref jobWrapper, bufferRangePatchData);
                else
                    ExecuteAppend(ref jobWrapper, bufferRangePatchData);
            }

            public static unsafe void ExecuteAppend(ref JobWrapper jobWrapper, System.IntPtr bufferRangePatchData)
            {
                int oldLength = jobWrapper.outputIndices.Length;
                jobWrapper.outputIndices.Capacity = math.max(jobWrapper.appendCount + oldLength, jobWrapper.outputIndices.Capacity);

                int* outputPtr = (int*)jobWrapper.outputIndices.GetUnsafePtr();
                int outputIndex = oldLength;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobWrapper),
                    0, jobWrapper.appendCount);
#endif
                for (int i = 0; i != jobWrapper.appendCount; i++)
                {
                    if (jobWrapper.JobData.Execute(i))
                    {
                        outputPtr[outputIndex] = i;
                        outputIndex++;
                    }
                }

                jobWrapper.outputIndices.ResizeUninitialized(outputIndex);
            }

            public static unsafe void ExecuteFilter(ref JobWrapper jobWrapper, System.IntPtr bufferRangePatchData)
            {
                int* outputPtr = (int*)jobWrapper.outputIndices.GetUnsafePtr();
                int inputLength = jobWrapper.outputIndices.Length;

                int outputCount = 0;
                for (int i = 0; i != inputLength; i++)
                {
                    int inputIndex = outputPtr[i];

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobWrapper), inputIndex, 1);
#endif

                    if (jobWrapper.JobData.Execute(inputIndex))
                    {
                        outputPtr[outputCount] = inputIndex;
                        outputCount++;
                    }
                }

                jobWrapper.outputIndices.ResizeUninitialized(outputCount);
            }
        }

        /// <summary>
        /// This method is only to be called by automatically generated setup code.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void EarlyJobInit<T>()
            where T : struct, IJobParallelForFilter
        {
            JobParallelForFilterProducer<T>.Initialize();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReflectionDataCorrect(IntPtr reflectionData)
        {
            if (reflectionData == IntPtr.Zero)
                throw new InvalidOperationException("Reflection data was not set up by a call to Initialize()");
        }

        public static unsafe JobHandle ScheduleAppend<T>(this T jobData, NativeList<int> indices, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForFilter
        {
            JobParallelForFilterProducer<T>.JobWrapper jobWrapper = new JobParallelForFilterProducer<T>.JobWrapper()
            {
                JobData = jobData,
                outputIndices = indices,
                appendCount = arrayLength
            };

            var reflectionData = JobParallelForFilterProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobWrapper), reflectionData, dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        public static unsafe JobHandle ScheduleFilter<T>(this T jobData, NativeList<int> indices, int innerloopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForFilter
        {
            JobParallelForFilterProducer<T>.JobWrapper jobWrapper = new JobParallelForFilterProducer<T>.JobWrapper()
            {
                JobData = jobData,
                outputIndices = indices,
                appendCount = -1
            };

            var reflectionData = JobParallelForFilterProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobWrapper), reflectionData, dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        public static unsafe void RunAppend<T>(this T jobData, NativeList<int> indices, int arrayLength) where T : struct, IJobParallelForFilter
        {
            JobParallelForFilterProducer<T>.JobWrapper jobWrapper = new JobParallelForFilterProducer<T>.JobWrapper()
            {
                JobData = jobData,
                outputIndices = indices,
                appendCount = arrayLength
            };

            var reflectionData = JobParallelForFilterProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobWrapper), reflectionData, new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }

        public static unsafe void RunFilter<T>(this T jobData, NativeList<int> indices) where T : struct, IJobParallelForFilter
        {
            JobParallelForFilterProducer<T>.JobWrapper jobWrapper = new JobParallelForFilterProducer<T>.JobWrapper()
            {
                JobData = jobData,
                outputIndices = indices,
                appendCount = -1
            };

            var reflectionData = JobParallelForFilterProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobWrapper), reflectionData, new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }
    }
}
#endif
