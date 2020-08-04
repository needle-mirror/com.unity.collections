#if UNITY_2020_1_OR_NEWER || UNITY_DOTSRUNTIME
using System;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Baselib.LowLevel.Binding;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// A range of virtual memory with a pointer to the beginning of a range of bytes, log2 of the page size in bytes, and number of pages in this allocation.
    /// </summary>
    public struct VMRange
    {
        /// <summary>
        /// Pointer to the beginning of an address range.
        /// </summary>
        public IntPtr ptr;

        /// <summary>
        /// 2 ^ log2PageSize = virtual memory page size in bytes
        /// </summary>
        public byte log2PageSize;

        /// <summary>
        /// Number of pages in this address range.
        /// </summary>
        public uint pageCount;

        /// <summary>
        /// Virtual memory page size in bytes for this address range.
        /// </summary>
        public uint PageSizeInBytes
        {
            get => (uint)(1 << log2PageSize);
            set => log2PageSize = (byte)(32 - math.lzcnt(math.max(1, (int)value) - 1));
        }

        /// <summary>
        /// Number of bytes contained in this range.
        /// </summary>
        public ulong SizeInBytes => (ulong)PageSizeInBytes * (ulong)pageCount;

        /// <summary>
        ///
        /// </summary>
        /// <param name="rangePtr"></param>
        /// <param name="rangeLog2PageSize"></param>
        /// <param name="rangePageCount"></param>
        public VMRange(IntPtr rangePtr, byte rangeLog2PageSize, uint rangePageCount)
        {
            ptr = rangePtr;
            log2PageSize = rangeLog2PageSize;
            pageCount = rangePageCount;
        }
    }

    /// <summary>
    /// File name, function name, and line number in source code for where a Baselib_ErrorState came from.
    /// </summary>
    public unsafe struct BaselibSourceLocation
    {
        /// <summary>
        /// File name. A const char* in native code.
        /// </summary>
        public byte* file;

        /// <summary>
        /// Function name. A const char* in native code.
        /// </summary>
        public byte* function;

        /// <summary>
        /// Line number in source code file where the error appeared.
        /// </summary>
        public uint lineNumber;
    }

    /// <summary>
    /// C# analog of Baselib_ErrorState.
    /// </summary>
    public struct BaselibErrorState
    {
        /// <summary>
        /// Error code
        /// </summary>
        public uint code;

        /// <summary>
        /// Type of error code
        /// </summary>
        public byte nativeErrorCodeType;

        /// <summary>
        /// Platform-specific error code.
        /// </summary>
        public ulong nativeErrorCode;

        /// <summary>
        /// Where in the source code this error came from.
        /// </summary>
        public BaselibSourceLocation sourceLocation;

        /// <summary>
        /// Returns true if result of recorded operation was success.
        /// </summary>
        public bool Success => code == (uint)Baselib_ErrorCode.Success;

        /// <summary>
        /// Returns true when the recorded error state shows failure due to running out of memory.
        /// </summary>
        public bool OutOfMemory => code == (uint)Baselib_ErrorCode.OutOfMemory;

        /// <summary>
        /// Returns true when the recorded error state shows failure due to accessing an invalid address range.
        /// </summary>
        public bool InvalidAddressRange => code == (uint)Baselib_ErrorCode.InvalidAddressRange;
    }

    /// <summary>
    ///
    /// </summary>
    public unsafe struct VirtualMemoryUtility
    {
        unsafe internal struct PageSizeInfo
        {
            byte log2DefaultPageSize;
            public uint DefaultPageSizeInBytes
            {
                get => (uint)(1 << log2DefaultPageSize);
            }

            // Bitfield where a 1 means that place is a log2 of a page size.
            // Example bit pattern: 0001 0000 0000 0000
            // The 12th bit in the pattern is a 1, 2^12 is 4096, which is the default page size on Windows.
            public int availableLog2PageSizes;
            public int AvailablePageSizeCount => math.countbits(availableLog2PageSizes); // popcnt

            public PageSizeInfo(ulong defaultPageSize, ulong* availablePageSizes, ulong numAvailablePageSizes)
            {
                log2DefaultPageSize = (byte)(64 - math.lzcnt(math.max(1, defaultPageSize) - 1));
                availableLog2PageSizes = 1 << log2DefaultPageSize;
                for (int i = 1; i < (int)numAvailablePageSizes; i++)
                {
                    availableLog2PageSizes |= 1 << (int)availablePageSizes[i];
                }
            }
        }

        internal sealed class StaticPageSizeInfo
        {
            StaticPageSizeInfo() { }
            public static readonly SharedStatic<PageSizeInfo> Ref = SharedStatic<PageSizeInfo>.GetOrCreate<PageSizeInfo>();
        }

        internal static PageSizeInfo GetPageSizeInfo()
        {
            return StaticPageSizeInfo.Ref.Data;
        }

        /// <summary>
        /// Gets the default virtual memory page size in bytes for this platform.
        /// </summary>
        public static uint DefaultPageSizeInBytes
        {
            get
            {
                if (GetPageSizeInfo().DefaultPageSizeInBytes == 1)
                {
                    Baselib_Memory_PageSizeInfo pageSizeInfo = default;
                    Baselib_Memory_GetPageSizeInfo(&pageSizeInfo);
                    StaticPageSizeInfo.Ref.Data = new PageSizeInfo(pageSizeInfo.defaultPageSize, &pageSizeInfo.pageSizes0, pageSizeInfo.pageSizesLen);
                }
                return StaticPageSizeInfo.Ref.Data.DefaultPageSizeInBytes;
            }
        }

        /// <summary>
        /// Logs baselib errors to the console.
        /// </summary>
        /// <param name="wrappedErrorState">Wrapped copy of Baselib_ErrorState.</param>
        public static void ReportWrappedBaselibError(BaselibErrorState wrappedErrorState)
        {
            if ((Baselib_ErrorCode)wrappedErrorState.code == Baselib_ErrorCode.Success)
                return;

            Baselib_ErrorState errorState = default;
            errorState.code = (Baselib_ErrorCode)wrappedErrorState.code;
            errorState.nativeErrorCodeType = (Baselib_ErrorState_NativeErrorCodeType)wrappedErrorState.nativeErrorCodeType;
            errorState.nativeErrorCode = wrappedErrorState.nativeErrorCode;

            errorState.sourceLocation.file = wrappedErrorState.sourceLocation.file;
            errorState.sourceLocation.function = wrappedErrorState.sourceLocation.function;
            errorState.sourceLocation.lineNumber = wrappedErrorState.sourceLocation.lineNumber;

            FixedString512 errorString = "Baselib error: ";
            byte* errorStringNext = errorString.GetUnsafePtr() + errorString.Length;
            int errorStringRemainingCap = errorString.Capacity - errorString.Length;

            var bytesReturned = Baselib_ErrorState_Explain(&errorState, errorStringNext, (uint) errorStringRemainingCap, Baselib_ErrorState_ExplainVerbosity.ErrorType_SourceLocation_Explanation);
            if (bytesReturned > errorStringRemainingCap)
            {
                byte* bytes = stackalloc byte[(int)bytesReturned];
                Baselib_ErrorState_Explain(&errorState, bytes, bytesReturned, Baselib_ErrorState_ExplainVerbosity.ErrorType_SourceLocation_Explanation);
                errorString.Append(bytes, errorStringRemainingCap);
            }
            else
            {
                errorString.Length += (int) bytesReturned;
            }

            Debug.LogError(errorString);
        }

        static BaselibErrorState CreateWrappedBaselibErrorState(Baselib_ErrorState errorState)
        {
            BaselibSourceLocation baselibSourceLocation = default;
            baselibSourceLocation.file = errorState.sourceLocation.file;
            baselibSourceLocation.function = errorState.sourceLocation.function;
            baselibSourceLocation.lineNumber = errorState.sourceLocation.lineNumber;

            BaselibErrorState baselibErrorState = default;
            baselibErrorState.code = (uint)errorState.code;
            baselibErrorState.nativeErrorCodeType = (byte)errorState.nativeErrorCodeType;
            baselibErrorState.nativeErrorCode = errorState.nativeErrorCode;
            baselibErrorState.sourceLocation = baselibSourceLocation;

            return baselibErrorState;
        }

        /// <summary>
        /// Reserves a contiguous range of address space.
        /// </summary>
        /// <param name="sizeOfAddressRangeInBytes">Size of a virtual address range to reserve, in bytes.</param>
        /// <param name="pageSizeInBytes">Size of a page of virtual memory, in bytes.</param>
        /// <param name="BaselibErrorState">Wrapped copy of Baselib_ErrorState.</param>
        /// <returns>A VMRange. Upon failure, the returned VMRange will have a null pointer, 0 sized pages, and 0 page count.</returns>
        public static VMRange ReserveAddressSpace(ulong sizeOfAddressRangeInPages, ulong pageSizeInBytes, out BaselibErrorState outErrorState)
        {
            ulong alignmentInMultipleOfPageSize = 1;
            Baselib_Memory_PageState pageState = Baselib_Memory_PageState.Reserved;
            Baselib_ErrorState errorState = default;

            // Returns an allocation with null pointer and 0 for page size and page count when it fails.
            var reservedAddressRange = Baselib_Memory_AllocatePages(pageSizeInBytes, sizeOfAddressRangeInPages, alignmentInMultipleOfPageSize, pageState, &errorState);

            outErrorState = CreateWrappedBaselibErrorState(errorState);

            return new VMRange { ptr = reservedAddressRange.ptr, PageSizeInBytes = (uint)reservedAddressRange.pageSize, pageCount = (uint)reservedAddressRange.pageCount };
        }

        /// <summary>
        /// Commits memory from reserved address space.
        /// </summary>
        /// <param name="rangeToCommit">Reserved virtual address range from which to allocate memory.</param>
        /// <param name="BaselibErrorState">Wrapped copy of Baselib_ErrorState.</param>
        public static void CommitMemory(VMRange rangeToCommit, out BaselibErrorState outErrorState)
        {
            Baselib_ErrorState errorState = default;
            Baselib_Memory_SetPageState(rangeToCommit.ptr, rangeToCommit.PageSizeInBytes, rangeToCommit.pageCount, Baselib_Memory_PageState.ReadWrite, &errorState);

            outErrorState = CreateWrappedBaselibErrorState(errorState);
        }

        /// <summary>
        /// Decommits committed memory from reserved address space.
        /// </summary>
        /// <param name="rangeToFree">Virtual address range from which to free allocated memory.</param>
        /// <param name="BaselibErrorState">Wrapped copy of Baselib_ErrorState.</param>
        public static void DecommitMemory(VMRange rangeToFree, out BaselibErrorState outErrorState)
        {
            Baselib_ErrorState errorState = default;
            Baselib_Memory_SetPageState(rangeToFree.ptr, rangeToFree.PageSizeInBytes, rangeToFree.pageCount, Baselib_Memory_PageState.Reserved, &errorState);

            outErrorState = CreateWrappedBaselibErrorState(errorState);
        }

        /// <summary>
        /// Frees reserved address space.
        /// </summary>
        /// <param name="reservedAddressRange">Virtual address range to release.</param>
        /// <param name="BaselibErrorState">Wrapped copy of Baselib_ErrorState.</param>
        public static void FreeAddressSpace(VMRange reservedAddressRange, out BaselibErrorState outErrorState)
        {
            var pages = new Baselib_Memory_PageAllocation { ptr = reservedAddressRange.ptr, pageSize = reservedAddressRange.PageSizeInBytes, pageCount = reservedAddressRange.pageCount };
            Baselib_ErrorState errorState = default;
            Baselib_Memory_ReleasePages(pages, &errorState);
            outErrorState = CreateWrappedBaselibErrorState(errorState);
        }
    }
}
#endif
