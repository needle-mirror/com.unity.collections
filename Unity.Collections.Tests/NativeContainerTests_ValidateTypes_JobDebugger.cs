using Unity.Jobs;
using NUnit.Framework;
using Unity.Collections;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
public class NativeContainderTests_ValidateTypes_JobDebugger : NativeContainerTests_ValidateTypesFixture
{
	struct WriteOnlyHashMapParallelForJob : IJobParallelFor
	{
#pragma warning disable 0169 // "never used" warning
        [WriteOnly]
		NativeHashMap<int, int> value;
#pragma warning restore 0169

		public void Execute(int index) {}
	}
	
	struct ReadWriteMultiHashMapParallelForJob : IJobParallelFor
	{
#pragma warning disable 0169 // "never used" warning
		NativeMultiHashMap<int, int> value;
#pragma warning restore 0169

		public void Execute(int index) {}
	}
	
	struct DeallocateOnJobCompletionOnUnsupportedType : IJob
	{
#pragma warning disable 0169 // "never used" warning
		[DeallocateOnJobCompletion]
		NativeList<float> value;
#pragma warning restore 0169

		public void Execute() {}
	}

	[Test]
	public void ValidatedUnsupportedTypes()
	{
        CheckNativeContainerReflectionExceptionParallelFor<WriteOnlyHashMapParallelForJob> ("WriteOnlyHashMapParallelForJob.value is not declared [ReadOnly] in a IJobParallelFor job. The container does not support parallel writing. Please use a more suitable container type.");
		CheckNativeContainerReflectionException<DeallocateOnJobCompletionOnUnsupportedType> ("DeallocateOnJobCompletionOnUnsupportedType.value uses [DeallocateOnJobCompletion] but the native container does not support deallocation of the memory from a job.");

		// ReadWrite against atomic write only container
		CheckNativeContainerReflectionExceptionParallelFor<ReadWriteMultiHashMapParallelForJob> ("ReadWriteMultiHashMapParallelForJob.value is not declared [ReadOnly] in a IJobParallelFor job. The container does not support parallel writing. Please use a more suitable container type.");
	}
}
#endif
