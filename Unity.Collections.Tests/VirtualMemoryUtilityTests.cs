using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.Tests;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;
#if !UNITY_PORTABLE_TEST_RUNNER
using System.Text.RegularExpressions;
#endif
using Assert = FastAssert;

#pragma warning disable 618 // disable obsolete warnings

class VirtualMemoryUtilityTests : CollectionsTestCommonBase
{
#if !(UNITY_DOTSRUNTIME && UNITY_WEBGL) // https://unity3d.atlassian.net/browse/DOTSR-2039
    [Test]
    public void VirtualMemory_Reserve1Page()
    {
        // Reserve 1 page
        BaselibErrorState errorState = default;
        var addressSpace = VirtualMemoryUtility.ReserveAddressSpace(1, VirtualMemoryUtility.DefaultPageSizeInBytes, out errorState);
        Assert.AreEqual(addressSpace.pageCount, 1u);
        VirtualMemoryUtility.FreeAddressSpace(addressSpace, out errorState);
    }

#if !UNITY_DOTSRUNTIME
    [Test]
    public void VirtualMemory_TryToReserveInvalidPageSize()
    {
        // Reserve 1 page with invalid page size
        BaselibErrorState errorState = default;
        var addressSpace = VirtualMemoryUtility.ReserveAddressSpace(1, 69420, out errorState);
        VirtualMemoryUtility.ReportWrappedBaselibError(errorState);
        LogAssert.Expect(LogType.Error, new Regex("Baselib error:*"));
        VirtualMemoryUtility.FreeAddressSpace(addressSpace, out errorState);
    }
#endif

    [Test]
    public unsafe void VirtualMemory_Allocate()
    {
        BaselibErrorState errorState = default;

        // Reserve 4GB
        var addressSpace = VirtualMemoryUtility.ReserveAddressSpace(1024ul * 1024ul, VirtualMemoryUtility.DefaultPageSizeInBytes, out errorState);
        {
            // Commit 1 page for an allocator
            VMRange page = new VMRange { ptr = addressSpace.ptr, log2PageSize = addressSpace.log2PageSize, pageCount = 1 };
            VirtualMemoryUtility.CommitMemory(page, out errorState);

            // 1KB allocator
            var allocator = new UnsafeScratchAllocator((void*)page.ptr, 1024);

            var numbers0 = (int*)allocator.Allocate<int>(256);
            for(int i = 0; i < 256; i++)
            {
                numbers0[i] = i;
            }

            var anotherAllocator = new UnsafeScratchAllocator((void*)((IntPtr)page.ptr + 1024), 1024);
            var numbers1 = (int*)anotherAllocator.Allocate<int>(256);
            for (int i = 0; i < 256; i++)
            {
                numbers1[i] = numbers0[i];
            }
        }
        VirtualMemoryUtility.FreeAddressSpace(addressSpace, out errorState);
    }

#if !UNITY_DOTSRUNTIME
    [Test]
    [Ignore("This doesn't work on all platforms (e.g. MacOS) because baselib might be able to commit memory just fine (or commit does nothing, e.g. WebGL) and then you won't get a NullReferenceException.")]
    public unsafe void VirtualMemory_OvercommitReservedMemory()
    {
        BaselibErrorState errorState = default;

        // Reserve 1 page
        var addressSpace = VirtualMemoryUtility.ReserveAddressSpace(1, VirtualMemoryUtility.DefaultPageSizeInBytes, out errorState);
        {
            // Try to commit 2 pages (will fail).
            VMRange pages = new VMRange { ptr = addressSpace.ptr, log2PageSize = addressSpace.log2PageSize, pageCount = 2 };
            VirtualMemoryUtility.CommitMemory(pages, out errorState);
            VirtualMemoryUtility.ReportWrappedBaselibError(errorState);
            LogAssert.Expect(LogType.Error, new Regex("Baselib error:*"));

            var coolByte = (byte*)pages.ptr;

            // Try to write to uncommitted memory (will fail and throw an exception).
            Assert.Throws<NullReferenceException>(() => *coolByte = 0);
        }
        VirtualMemoryUtility.FreeAddressSpace(addressSpace, out errorState);
    }
#endif

#if !UNITY_DOTSRUNTIME    // DOTS-Runtime safety system throws fatally on the NullReferenceException.
    [Test]
    public unsafe void VirtualMemory_Decommit()
    {
        BaselibErrorState errorState = default;

        // Reserve 4GB
        var addressSpace = VirtualMemoryUtility.ReserveAddressSpace(1024ul * 1024ul, VirtualMemoryUtility.DefaultPageSizeInBytes, out errorState);
        {
            // Commit 4KB for an allocator
            VMRange page = new VMRange { ptr = addressSpace.ptr, log2PageSize = addressSpace.log2PageSize, pageCount = 1 };
            VirtualMemoryUtility.CommitMemory(page, out errorState);
            var allocator = new UnsafeScratchAllocator((void*)page.ptr, 4096);

            var numbers0 = (int*)allocator.Allocate<int>(256);
            for (int i = 0; i < 256; i++)
            {
                numbers0[i] = i;
            }

            // Decommit and try to use
            VirtualMemoryUtility.DecommitMemory(page, out errorState);
            Assert.Throws<NullReferenceException>(() => numbers0[1] = 1);
        }
        VirtualMemoryUtility.FreeAddressSpace(addressSpace, out errorState);
    }
#endif

    [BurstCompile]
    unsafe struct CommitJob : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction]
        public IntPtr jobAddressRangePtr;
        public byte jobLog2PageSize;
        public uint jobPageCount;

        public NativeArray<BaselibErrorState> jobErrorStates;

        public void Execute(int index)
        {
            BaselibErrorState errorState = default;
            VMRange allocation = new VMRange { ptr = jobAddressRangePtr + index * (int)VirtualMemoryUtility.DefaultPageSizeInBytes, log2PageSize = jobLog2PageSize, pageCount = jobPageCount };
            // Commit a page from the address range
            VirtualMemoryUtility.CommitMemory(allocation, out errorState);
            jobErrorStates[index] = errorState;

            // 1 page of ints
            var allocator = new UnsafeScratchAllocator((void*)allocation.ptr, (int)VirtualMemoryUtility.DefaultPageSizeInBytes);
            var numbers0 = (int*)allocator.Allocate<int>((int)VirtualMemoryUtility.DefaultPageSizeInBytes / sizeof(int));
            for (int i = 0; i < (int)VirtualMemoryUtility.DefaultPageSizeInBytes / sizeof(int); i++)
            {
                numbers0[i] = i;
            }
        }
    }

    [BurstCompile]
    unsafe struct DecommitJob : IJobParallelFor
    {
        [NativeDisableUnsafePtrRestriction]
        public IntPtr jobAddressRangePtr;
        public byte jobLog2PageSize;
        public uint jobPageCount;

        public NativeArray<BaselibErrorState> jobErrorStates;
        public void Execute(int index)
        {
            BaselibErrorState errorState = default;
            VMRange addressRange = new VMRange { ptr = jobAddressRangePtr + index, log2PageSize = jobLog2PageSize, pageCount = jobPageCount };
            VirtualMemoryUtility.DecommitMemory(addressRange, out errorState);
            jobErrorStates[index] = errorState;
        }
    }

    [Test]
    public unsafe void VirtualMemory_AllocateAndFreeFromBurst()
    {
        BaselibErrorState errorState = default;

        // Reserve 1GB
        var addressSpace = VirtualMemoryUtility.ReserveAddressSpace(1024ul * 256ul, VirtualMemoryUtility.DefaultPageSizeInBytes, out errorState);
        {
            // 100 pages of ints
            const int allocationCount = 100;

            var errorStates = new NativeArray<BaselibErrorState>(allocationCount, Allocator.Persistent);
            {
                for (int i = 0; i < allocationCount; i++)
                {
                    errorStates[i] = default;
                }

                var commitJob = new CommitJob
                {
                    jobAddressRangePtr = addressSpace.ptr,
                    jobLog2PageSize = addressSpace.log2PageSize,
                    jobPageCount = 1,
                    jobErrorStates = errorStates
                };
                commitJob.Schedule(allocationCount, 1).Complete();

                for (int i = 0; i < allocationCount; i++)
                {
                    VirtualMemoryUtility.ReportWrappedBaselibError(errorStates[i]);
                }

                // for each page allocated
                for (int i = 0; i < allocationCount; i++)
                {
                    var page = (void*)((ulong)addressSpace.ptr + (ulong)i * VirtualMemoryUtility.DefaultPageSizeInBytes);
                    var allocator = new UnsafeScratchAllocator((void*)addressSpace.ptr, (int)VirtualMemoryUtility.DefaultPageSizeInBytes * allocationCount);

                    var intCount = ((int)VirtualMemoryUtility.DefaultPageSizeInBytes / sizeof(int));
                    var numbersInPage = (int*)allocator.Allocate<int>(intCount);
                    // for each int in the allocated page
                    for (int j = 0; j < intCount; j++)
                    {
                        Assert.AreEqual(j, numbersInPage[j]);
                    }
                }

                var decommitJob = new DecommitJob
                {
                    jobAddressRangePtr = addressSpace.ptr,
                    jobLog2PageSize = addressSpace.log2PageSize,
                    jobPageCount = 1,
                    jobErrorStates = errorStates
                };
                decommitJob.Schedule(allocationCount, 1).Complete();

            }
            errorStates.Dispose();
        }

        VirtualMemoryUtility.FreeAddressSpace(addressSpace, out errorState);
    }
#endif
}
