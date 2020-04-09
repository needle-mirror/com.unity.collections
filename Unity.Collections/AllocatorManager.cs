#if !NET_DOTS // can't use Burst function pointers from DOTS runtime (yet)
#define CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
#endif

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Collections
{
    public static class AllocatorManager
    {
        /// <summary>
        /// Corresponds to Allocator.Invalid.
        /// </summary>
        public static readonly AllocatorHandle Invalid = new AllocatorHandle{Value = 0};

        /// <summary>
        /// Corresponds to Allocator.None.
        /// </summary>
        public static readonly AllocatorHandle None = new AllocatorHandle{Value = 1};

        /// <summary>
        /// Corresponds to Allocator.Temp.
        /// </summary>
        public static readonly AllocatorHandle Temp = new AllocatorHandle{Value = 2};

        /// <summary>
        /// Corresponds to Allocator.TempJob.
        /// </summary>
        public static readonly AllocatorHandle TempJob = new AllocatorHandle{Value = 3};

        /// <summary>
        /// Corresponds to Allocator.Persistent.
        /// </summary>
        public static readonly AllocatorHandle Persistent = new AllocatorHandle{Value = 4};

        /// <summary>
        /// Corresponds to Allocator.AudioKernel.
        /// </summary>
        public static readonly AllocatorHandle AudioKernel = new AllocatorHandle{Value = 5};

        #region Allocator Parts
        /// <summary>
        /// Delegate used for calling an allocator's allocation function.
        /// </summary>
        public delegate int TryFunction(IntPtr allocatorState, ref Block block);

        /// <summary>
        /// Which allocator a Block's Range allocates from.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AllocatorHandle
        {
            /// <summary>
            /// Index into a function table of allocation functions.
            /// </summary>
            public ushort Value;

            /// <summary>
            /// Allocates a Block of memory from this allocator with requested number of items of a given type.
            /// </summary>
            /// <typeparam name="T">Type of item to allocate.</typeparam>
            /// <param name="block">Block of memory to allocate within.</param>
            /// <param name="Items">Number of items to allocate.</param>
            /// <returns>Error code from the given Block's allocate function.</returns>
            public int TryAllocate<T>(out Block block, int Items) where T : struct
            {
                block = new Block
                {
                    Range = new Range { Items = Items, Allocator = new AllocatorHandle { Value = Value } },
                    BytesPerItem = UnsafeUtility.SizeOf<T>(),
                    Alignment = 1 << math.min(3, math.tzcnt(UnsafeUtility.SizeOf<T>()))
                };
                var returnCode = Try(ref block);
                return returnCode;
            }

            /// <summary>
            /// Allocates a Block of memory from this allocator with requested number of items of a given type.
            /// </summary>
            /// <typeparam name="T">Type of item to allocate.</typeparam>
            /// <param name="Items">Number of items to allocate.</param>
            /// <returns>A Block of memory.</returns>
            public Block Allocate<T>(int Items) where T : struct
            {
                var error = TryAllocate<T>(out Block block, Items);
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Allocate {block}");
                return block;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BlockHandle { public ushort Value; }

        /// <summary>
        /// Pointer for the beginning of a block of memory, number of items in it, which allocator it belongs to, and which block this is.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Range : IDisposable
        {
            public IntPtr Pointer; //  0
            public int Items; //  8
            public AllocatorHandle Allocator; // 12
            public BlockHandle Block; // 14

            public void Dispose()
            {
                Block block = new Block { Range = this };
                block.Dispose();
                this = block.Range;
            }
        }

        /// <summary>
        /// A block of memory with a Range and metadata for size in bytes of each item in the block, number of allocated items, and alignment.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Block : IDisposable
        {
            public Range Range;
            public int BytesPerItem; // number of bytes in each item requested
            public int AllocatedItems; // how many items were actually allocated
            public byte Log2Alignment; // (1 << this) is the byte alignment
            public byte Padding0;
            public ushort Padding1;
            public uint Padding2;

            public long Bytes => BytesPerItem * Range.Items;

            public int Alignment
            {
                get => 1 << Log2Alignment;
                set => Log2Alignment = (byte)(32 - math.lzcnt(math.max(1, value) - 1));
            }

            public void Dispose()
            {
                TryFree();
            }

            public int TryAllocate()
            {
                Range.Pointer = IntPtr.Zero;
                return Try(ref this);
            }

            public int TryFree()
            {
                Range.Items = 0;
                return Try(ref this);
            }

            public void Allocate()
            {
                var error = TryAllocate();
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Allocate {this}");
            }

            public void Free()
            {
                var error = TryFree();
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Free {this}");
            }

        }

        /// <summary>
        /// An allocator with a tryable allocate/free/realloc function pointer.
        /// </summary>
        public interface IAllocator
        {
            TryFunction Function { get; }
            int Try(ref Block block);
            long BudgetInBytes { get; }
            long AllocatedBytes { get; }
        }

        /// <summary>
        /// Looks up an allocator's allocate, free, or realloc function pointer from a table and invokes the function.
        /// </summary>
        /// <param name="block">Block to allocate memory for.</param>
        /// <returns>Error code of invoked function.</returns>
        public static unsafe int Try(ref Block block)
        {
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            var functionTable = (IntPtr*)StaticFunctionTable.Ref.Data;
            var stateTable = (IntPtr*)StaticAllocatorState.Ref.Data;
            var function = new FunctionPointer<TryFunction>(functionTable[block.Range.Allocator.Value]);
            // this is really bad in non-Burst C#, it generates garbage each time we call Invoke
            return function.Invoke(stateTable[block.Range.Allocator.Value], ref block);
#else
            return StaticFunctionTable.Array[block.Range.Allocator.Value](StaticAllocatorState.Array[block.Range.Allocator.Value], ref block);
#endif
        }
        #endregion
        #region Allocators
        /// <summary>
        /// Allocator that uses UnsafeUtility.Malloc for its backing storage.
        /// </summary>
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
        [BurstCompile(CompileSynchronously = true)]
#endif
        internal struct MallocAllocator : IAllocator, IDisposable
        {
            public Allocator m_allocator;

            /// <summary>
            /// Upper limit on how many bytes this allocator is allowed to allocate.
            /// </summary>
            public long budgetInBytes;
            public long BudgetInBytes => budgetInBytes;

            /// <summary>
            /// Number of currently allocated bytes for this allocator.
            /// </summary>
            public long allocatedBytes;
            public long AllocatedBytes => allocatedBytes;

            public unsafe int Try(ref Block block)
            {
                if (block.Range.Pointer == IntPtr.Zero) // Allocate
                {
                    block.Range.Pointer =
                        (IntPtr)UnsafeUtility.Malloc(block.Bytes, block.Alignment, m_allocator);
                    block.AllocatedItems = block.Range.Items;
                    allocatedBytes += block.Bytes;
                    return (block.Range.Pointer == IntPtr.Zero) ? -1 : 0;
                }

                if (block.Bytes == 0) // Free
                {
                    UnsafeUtility.Free((void*)block.Range.Pointer, m_allocator);
                    var blockSizeInBytes = block.AllocatedItems * block.BytesPerItem;
                    allocatedBytes -= blockSizeInBytes;
                    block.Range.Pointer = IntPtr.Zero;
                    block.AllocatedItems = 0;
                    return 0;
                }

                // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
                return -1;
            }

#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            [BurstCompile(CompileSynchronously = true)]
#endif
            public static unsafe int Try(IntPtr allocatorState, ref Block block)
            {
                return ((MallocAllocator*)allocatorState)->Try(ref block);
            }

            public TryFunction Function => Try;

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Stack allocator with no backing storage.
        /// </summary>
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
        [BurstCompile(CompileSynchronously = true)]
#endif
        internal struct StackAllocator : IAllocator, IDisposable
        {
            public Block m_storage;
            public long m_top;

            /// <summary>
            /// Upper limit on how many bytes this allocator is allowed to allocate.
            /// </summary>
            public long budgetInBytes;
            public long BudgetInBytes => budgetInBytes;

            /// <summary>
            /// Number of currently allocated bytes for this allocator.
            /// </summary>
            public long allocatedBytes;
            public long AllocatedBytes => allocatedBytes;

            public unsafe int Try(ref Block block)
            {
                if (block.Range.Pointer == IntPtr.Zero) // Allocate
                {
                    if (m_top + block.Bytes > m_storage.Bytes)
                    {
                        return -1;
                    }

                    block.Range.Pointer = (IntPtr)((byte*)m_storage.Range.Pointer + m_top);
                    block.AllocatedItems = block.Range.Items;
                    allocatedBytes += block.Bytes;
                    m_top += block.Bytes;
                    return 0;
                }

                if (block.Bytes == 0) // Free
                {
                    if ((byte*)block.Range.Pointer - (byte*)m_storage.Range.Pointer == (long)(m_top - block.Bytes))
                    {
                        m_top -= block.Bytes;
                        var blockSizeInBytes = block.AllocatedItems * block.BytesPerItem;
                        allocatedBytes -= blockSizeInBytes;
                        block.Range.Pointer = IntPtr.Zero;
                        block.AllocatedItems = 0;
                        return 0;
                    }

                    return -1;
                }

                // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
                return -1;
            }
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            [BurstCompile(CompileSynchronously = true)]
#endif
            public static unsafe int Try(IntPtr allocatorState, ref Block block)
            {
                return ((StackAllocator*)allocatorState)->Try(ref block);
            }

            public TryFunction Function => Try;

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Slab allocator with no backing storage.
        /// </summary>
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
        [BurstCompile(CompileSynchronously = true)]
#endif
        internal struct SlabAllocator : IAllocator, IDisposable
        {
            public Block Storage;
            public int Log2SlabSizeInBytes;
            public FixedListInt4096 Occupied;

            /// <summary>
            /// Upper limit on how many bytes this allocator is allowed to allocate.
            /// </summary>
            public long budgetInBytes;
            public long BudgetInBytes => budgetInBytes;

            /// <summary>
            /// Number of currently allocated bytes for this allocator.
            /// </summary>
            public long allocatedBytes;
            public long AllocatedBytes => allocatedBytes;

            public int SlabSizeInBytes
            {
                get => 1 << Log2SlabSizeInBytes;
                set => Log2SlabSizeInBytes = (byte)(32 - math.lzcnt(math.max(1, value) - 1));
            }

            public int Slabs => (int)(Storage.Bytes >> Log2SlabSizeInBytes);

            public SlabAllocator(Block storage, int slabSizeInBytes, long budget)
            {
                Assert.IsTrue((slabSizeInBytes & (slabSizeInBytes - 1)) == 0);
                Storage = storage;
                Log2SlabSizeInBytes = 0;
                Occupied = default;
                budgetInBytes = budget;
                allocatedBytes = 0;
                SlabSizeInBytes = slabSizeInBytes;
                Occupied.Length = (Slabs + 31) / 32;
            }

            public int Try(ref Block block)
            {
                if (block.Range.Pointer == IntPtr.Zero) // Allocate
                {
                    if (block.Bytes + allocatedBytes > budgetInBytes)
                        return -2; //over allocator budget
                    if (block.Bytes > SlabSizeInBytes)
                        return -1;
                    for (var wordIndex = 0; wordIndex < Occupied.Length; ++wordIndex)
                    {
                        var word = Occupied[wordIndex];
                        if (word == -1)
                            continue;
                        for (var bitIndex = 0; bitIndex < 32; ++bitIndex)
                            if ((word & (1 << bitIndex)) == 0)
                            {
                                Occupied[wordIndex] |= 1 << bitIndex;
                                block.Range.Pointer = Storage.Range.Pointer +
                                                       (int)(SlabSizeInBytes * (wordIndex * 32U + bitIndex));
                                block.AllocatedItems = SlabSizeInBytes / block.BytesPerItem;
                                allocatedBytes += block.Bytes;
                                return 0;
                            }
                    }

                    return -1;
                }

                if (block.Bytes == 0) // Free
                {
                    var slabIndex = ((ulong)block.Range.Pointer - (ulong)Storage.Range.Pointer) >>
                                    Log2SlabSizeInBytes;
                    int wordIndex = (int)(slabIndex >> 5);
                    int bitIndex = (int)(slabIndex & 31);
                    Occupied[wordIndex] &= ~(1 << bitIndex);
                    block.Range.Pointer = IntPtr.Zero;
                    var blockSizeInBytes = block.AllocatedItems * block.BytesPerItem;
                    allocatedBytes -= blockSizeInBytes;
                    block.AllocatedItems = 0;
                    return 0;
                }

                // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
                return -1;
            }
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            [BurstCompile(CompileSynchronously = true)]
#endif
            public static unsafe int Try(IntPtr allocatorState, ref Block block)
            {
                return ((SlabAllocator*)allocatorState)->Try(ref block);
            }

            public TryFunction Function => Try;

            public void Dispose()
            {
            }
        }
        #endregion
        #region AllocatorManager state and state functions
        /// <summary>
        /// Mapping between a Block, AllocatorHandle, and an IAllocator.
        /// </summary>
        /// <typeparam name="T">Type of allocator to install functions for.</typeparam>
        public struct AllocatorInstallation<T> : IDisposable
            where T : unmanaged, IAllocator, IDisposable
        {
            public Block MBlock;
            public AllocatorHandle m_handle;
            private unsafe T* t => (T*)MBlock.Range.Pointer;

            public ref T Allocator
            {
                get
                {
                    unsafe
                    {
                        return ref UnsafeUtilityEx.AsRef<T>(t);
                    }
                }
            }

            /// <summary>
            /// Creates a Block for an allocator, associates that allocator with an AllocatorHandle, then installs the allocator's function into the function table.
            /// </summary>
            /// <param name="Handle">Index into function table at which to install this allocator's function pointer.</param>
            public AllocatorInstallation(AllocatorHandle Handle)
            {
                // Allocate an allocator of type T using UnsafeUtility.Malloc with Allocator.Persistent.
                MBlock = Persistent.Allocate<T>(1);
                m_handle = Handle;
                unsafe
                {
                    UnsafeUtility.MemSet(t, 0, UnsafeUtility.SizeOf<T>());
                }

                unsafe
                {
                    Install(m_handle, (IntPtr)t, t->Function);
                }
            }

            public void Dispose()
            {
                Install(m_handle, IntPtr.Zero, null);
                unsafe
                {
                    t->Dispose();
                }

                MBlock.Dispose();
            }
        }

        /// <summary>
        /// SharedStatic that holds array of allocation function pointers for each allocator.
        /// </summary>
        private sealed class StaticFunctionTable
        {
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            public static FunctionPointer<TryFunction>[] Array = new FunctionPointer<TryFunction>[65536];
            public static GCHandle Handle;
            public static readonly SharedStatic<IntPtr> Ref =
                SharedStatic<IntPtr>.GetOrCreate<StaticFunctionTable>();
#else
            public static TryFunction[] Array = new TryFunction[65536];
#endif
        }

        /// <summary>
        /// SharedStatic that holds array of pointers to custom allocator state.
        /// </summary>
        private sealed class StaticAllocatorState
        {
            public static IntPtr[] Array = new IntPtr[65536];
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            public static GCHandle Handle;
            public static readonly SharedStatic<IntPtr> Ref =
                SharedStatic<IntPtr>.GetOrCreate<StaticAllocatorState>();
#endif
        }

        /// <summary>
        /// Malloc allocators for each of the built-in C++ allocators (e.g. Allocator.Persistent).
        /// </summary>
        private sealed class MallocAllocators
        {
            public static MallocAllocator[] Array = new MallocAllocator[6];
            public static GCHandle Handle;
        }

        /// <summary>
        /// Initializes SharedStatic allocator function table and allocator table, and installs default allocators.
        /// </summary>
        public static void Initialize()
        {
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            StaticAllocatorState.Handle = GCHandle.Alloc(StaticAllocatorState.Array, GCHandleType.Pinned);
            StaticFunctionTable.Handle = GCHandle.Alloc(StaticFunctionTable.Array, GCHandleType.Pinned);
            StaticAllocatorState.Ref.Data = StaticAllocatorState.Handle.AddrOfPinnedObject();
            StaticFunctionTable.Ref.Data = StaticFunctionTable.Handle.AddrOfPinnedObject();
#endif
            MallocAllocators.Handle = GCHandle.Alloc(MallocAllocators.Array, GCHandleType.Pinned);
            for (ushort i = 0; i < 6; ++i)
            {
                MallocAllocators.Array[i].m_allocator = (Allocator)i;
                Install(new AllocatorHandle { Value = i }, MallocAllocators.Handle.AddrOfPinnedObject() + UnsafeUtility.SizeOf<MallocAllocator>() * i,
                    MallocAllocator.Try);
            }
        }

        /// <summary>
        /// Creates and saves allocators' function pointers into function table.
        /// </summary>
        /// <param name="handle">AllocatorHandle to allocator to install function for.</param>
        /// <param name="allocatorState">IntPtr to allocator's custom state.</param>
        /// <param name="function">Function pointer to create or save in function table.</param>
        public static void Install(AllocatorHandle handle, IntPtr allocatorState, TryFunction function)
        {
            StaticAllocatorState.Array[handle.Value] = allocatorState;
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            StaticFunctionTable.Array[handle.Value] = (function == null)
                ? new FunctionPointer<TryFunction>(IntPtr.Zero)
                : BurstCompiler.CompileFunctionPointer(function);
#else
            StaticFunctionTable.Array[handle.Value] = function;
#endif
        }

        public static void Shutdown()
        {
            MallocAllocators.Handle.Free();
#if CUSTOM_ALLOCATOR_BURST_FUNCTION_POINTER
            StaticFunctionTable.Handle.Free();
            StaticAllocatorState.Handle.Free();
#endif
        }
        #endregion
        /// <summary>
        /// User-defined allocator index.
        /// </summary>
        public const ushort FirstUserIndex = 32;
    }
}