#if ENABLE_UNITY_COLLECTIONS_CHECKS
#define ENABLE_UNITY_ALLOCATION_CHECKS
#endif
#pragma warning disable 0649

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AOT;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Unity.Collections
{
    /// <summary>
    /// Support for management of custom memory allocators
    /// </summary>
    public static class AllocatorManager
    {           
        internal static Block AllocateBlock<T>(ref this T t, int sizeOf, int alignOf, int items) where T : unmanaged, IAllocator
        {
            CheckValid(t.Handle);
            Block block = default;
            block.Range.Pointer = IntPtr.Zero;
            block.Range.Items = items;
            block.Range.Allocator = t.Handle;
            block.BytesPerItem = sizeOf;
            block.Alignment = alignOf;
            var error = t.Try(ref block);
            CheckFailedToAllocate(error);
            return block;
        } 

        internal static Block AllocateBlock<T,U>(ref this T t, U u, int items) where T : unmanaged, IAllocator where U : unmanaged
        {
            return AllocateBlock(ref t, UnsafeUtility.SizeOf<U>(), UnsafeUtility.AlignOf<U>(), items);
        } 

        internal static unsafe void* Allocate<T>(ref this T t, int sizeOf, int alignOf, int items) where T : unmanaged, IAllocator
        {
            return (void*)AllocateBlock(ref t, sizeOf, alignOf, items).Range.Pointer;
        } 

        internal static unsafe U* Allocate<T,U>(ref this T t, U u, int items) where T : unmanaged, IAllocator where U : unmanaged
        {
            return (U*)Allocate(ref t, UnsafeUtility.SizeOf<U>(), UnsafeUtility.AlignOf<U>(), items);
        } 

        internal static unsafe void FreeBlock<T>(ref this T t, ref Block block) where T : unmanaged, IAllocator
        {
            CheckValid(t.Handle);
            block.Range.Items = 0;
            var error = t.Try(ref block);
            CheckFailedToFree(error);
        } 

        internal static unsafe void Free<T>(ref this T t, void* pointer, int sizeOf, int alignOf, int items) where T : unmanaged, IAllocator
        {
            if (pointer == null)
                return;
            Block block = default;
            block.AllocatedItems = items;
            block.Range.Pointer = (IntPtr)pointer;
            block.BytesPerItem = sizeOf;
            block.Alignment = alignOf;
            t.FreeBlock(ref block);
        } 

        internal static unsafe void Free<T,U>(ref this T t, U* pointer, int items) where T : unmanaged, IAllocator where U : unmanaged
        {
            Free(ref t, pointer, UnsafeUtility.SizeOf<U>(), UnsafeUtility.AlignOf<U>(), items);
        } 
        
        /// <summary>
        /// Allocate memory from an allocator
        /// </summary>
        /// <param name="handle">Handle to allocator to allocate from</param>
        /// <param name="itemSizeInBytes">Number of bytes to allocate</param>
        /// <param name="alignmentInBytes">Required alignment in bytes (must be a power of two)</param>
        /// <param name="items">Number of elements to allocate</param>
        /// <returns>A pointer to the allocated memory block</returns>
        public unsafe static void* Allocate(AllocatorHandle handle, int itemSizeInBytes, int alignmentInBytes, int items = 1)
        {
            return handle.Allocate(itemSizeInBytes, alignmentInBytes, items);
        }

        /// <summary>
        /// Allocate memory suitable for a type {T} from an allocator
        /// </summary>
        /// <typeparam name="T">Element type to allocate</typeparam>
        /// <param name="handle">Handle to allocator to allocate from</param>
        /// <param name="items">Number of elements to allocate</param>
        /// <returns>A pointer to the allocated memory block</returns>
        public unsafe static T* Allocate<T>(AllocatorHandle handle, int items = 1) where T : unmanaged
        {
            return handle.Allocate(default(T), items);
        }

        /// <summary>
        /// Free previously allocated memory
        /// </summary>
        /// <param name="handle">Handle to allocator to free memory in</param>
        /// <param name="pointer">Previously allocated pointer</param>
        /// <param name="itemSizeInBytes">Size used at allocation time</param>
        /// <param name="alignmentInBytes">Alignment used at allocation time</param>
        /// <param name="items">ignored</param>
        public unsafe static void Free(AllocatorHandle handle, void* pointer, int itemSizeInBytes, int alignmentInBytes, int items = 1)
        {
            handle.Free(pointer, itemSizeInBytes, alignmentInBytes, items);
        }

        /// <summary>
        /// Free previously allocated memory
        /// </summary>
        /// <param name="handle">Handle to allocator to free memory in</param>
        /// <param name="pointer">Previously allocated pointer</param>
        public unsafe static void Free(AllocatorHandle handle, void* pointer)
        {
            handle.Free((byte*)pointer, 1);
        }

        /// <summary>
        /// Free previously allocated memory
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="handle">Handle to allocator to free memory in</param>
        /// <param name="pointer">Previously allocated pointer</param>
        /// <param name="items">Number of elements to free (must be same as at allocation time)</param>
        public unsafe static void Free<T>(AllocatorHandle handle, T* pointer, int items = 1) where T : unmanaged
        {
            handle.Free(pointer, items);
        }

        /// <summary>
        /// Corresponds to Allocator.Invalid.
        /// </summary>
        public static readonly AllocatorHandle Invalid = new AllocatorHandle { Index = 0 };

        /// <summary>
        /// Corresponds to Allocator.None.
        /// </summary>
        public static readonly AllocatorHandle None = new AllocatorHandle { Index = 1 };

        /// <summary>
        /// Corresponds to Allocator.Temp.
        /// </summary>
        public static readonly AllocatorHandle Temp = new AllocatorHandle { Index = 2 };

        /// <summary>
        /// Corresponds to Allocator.TempJob.
        /// </summary>
        public static readonly AllocatorHandle TempJob = new AllocatorHandle { Index = 3 };

        /// <summary>
        /// Corresponds to Allocator.Persistent.
        /// </summary>
        public static readonly AllocatorHandle Persistent = new AllocatorHandle { Index = 4 };

        /// <summary>
        /// Corresponds to Allocator.AudioKernel.
        /// </summary>
        public static readonly AllocatorHandle AudioKernel = new AllocatorHandle { Index = 5 };

        /// <summary>
        /// Delegate used for calling an allocator's allocation function.
        /// </summary>
        public delegate int TryFunction(IntPtr allocatorState, ref Block block);

        /// <summary>
        /// Which allocator a Block's Range allocates from.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AllocatorHandle : IAllocator
        {
            internal ref TableEntry TableEntry => ref SharedStatics.TableEntry.Ref.Data.ElementAt(Index);
            internal bool IsInstalled => ((SharedStatics.IsInstalled.Ref.Data.ElementAt(Index>>6) >> (Index&63)) & 1) != 0;

            internal void Rewind()
            {
#if ENABLE_UNITY_ALLOCATION_CHECKS
                InvalidateDependents();
                ++OfficialVersion;
#endif
            }
            
            internal void Install(TableEntry tableEntry)
            {            
#if ENABLE_UNITY_ALLOCATION_CHECKS
                // if this allocator has never been visited before, then the unsafelists for its child allocators
                // and child safety handles are uninitialized, which means their allocator is Allocator.Invalid.
                // rectify that here.
                if(ChildSafetyHandles.Allocator.Value != (int)Allocator.Persistent)
                {
                    ChildSafetyHandles = new UnsafeList<AtomicSafetyHandle>(0, Allocator.Persistent);
                    ChildAllocators = new UnsafeList<AllocatorHandle>(0, Allocator.Persistent);
                }
#endif                    
                Rewind();
                TableEntry = tableEntry;
            }                
            
#if ENABLE_UNITY_ALLOCATION_CHECKS
            internal ref ushort OfficialVersion => ref SharedStatics.Version.Ref.Data.ElementAt(Index);
            internal ref UnsafeList<AtomicSafetyHandle> ChildSafetyHandles => ref SharedStatics.ChildSafetyHandles.Ref.Data.ElementAt(Index);
            internal ref UnsafeList<AllocatorHandle> ChildAllocators => ref SharedStatics.ChildAllocators.Ref.Data.ElementAt(Index);
            internal ref AllocatorHandle Parent => ref SharedStatics.Parent.Ref.Data.ElementAt(Index);
            internal ref int IndexInParent => ref SharedStatics.IndexInParent.Ref.Data.ElementAt(Index);
            
            internal bool IsCurrent => (Version == 0) || (Version == OfficialVersion);
            internal bool IsValid => (Index < FirstUserIndex) || (IsInstalled && IsCurrent);
            
            /// <summary>
            ///   <para>Determines if the handle is still valid, because we intend to release it if it is.</para>
            /// </summary>
            /// <param name="handle">Safety handle.</param>
            internal static unsafe bool CheckExists(AtomicSafetyHandle handle)
            {
#if UNITY_DOTSRUNTIME
                int* versionNode = (int*) (void*) handle.nodePtr;
#else        
                int* versionNode = (int*) (void*) handle.versionNode;
#endif            
                return handle.version == (*versionNode & -8);
            }
            
            internal static unsafe bool AreTheSame(AtomicSafetyHandle a, AtomicSafetyHandle b)
            {
                if(a.version != b.version)
                    return false;
#if UNITY_DOTSRUNTIME
                if(a.nodePtr != b.nodePtr)
#else
                if(a.versionNode != b.versionNode)
#endif
                    return false;
                return true;
            }            

            internal static bool AreTheSame(AllocatorHandle a, AllocatorHandle b)
            {
                if(a.Index != b.Index)
                    return false;
                if(a.Version != b.Version)
                    return false;
                return true;
            }            
            
            internal bool NeedsUseAfterFreeTracking()
            {            
                if(IsValid == false)
                    return false;
                if(ChildSafetyHandles.Allocator.Value != (int)Allocator.Persistent)
                    return false; 
                return true;
            }
            /// <summary>
            /// <undoc/>
            /// </summary>
            public const int InvalidChildSafetyHandleIndex = -1;

            internal int AddSafetyHandle(AtomicSafetyHandle handle)
            {
                if(!NeedsUseAfterFreeTracking())
                    return InvalidChildSafetyHandleIndex;
                var result = ChildSafetyHandles.Length;
                ChildSafetyHandles.Add(handle);
                return result;
            }
            internal bool TryRemoveSafetyHandle(AtomicSafetyHandle handle, int safetyHandleIndex)
            {
                if(!NeedsUseAfterFreeTracking())
                    return false;
                if(safetyHandleIndex == InvalidChildSafetyHandleIndex)
                    return false;
                safetyHandleIndex = math.min(safetyHandleIndex, ChildSafetyHandles.Length - 1);
                while(safetyHandleIndex >= 0)
                {
                    unsafe
                    {
                        var safetyHandle = ChildSafetyHandles.Ptr + safetyHandleIndex;
                        if(AreTheSame(*safetyHandle, handle))
                        {
                            ChildSafetyHandles.RemoveAtSwapBack(safetyHandleIndex);
                            return true;
                        }
                    }
                    --safetyHandleIndex;
                }
                return false;
            }

            /// <summary>
            /// <undoc/>
            /// </summary>
            public const int InvalidChildAllocatorIndex = -1;            

            internal int AddChildAllocator(AllocatorHandle handle)
            {
                if(!NeedsUseAfterFreeTracking())
                    return InvalidChildAllocatorIndex;
                var result = ChildAllocators.Length;
                ChildAllocators.Add(handle);
                handle.Parent = this;
                handle.IndexInParent = result;
                return result;
            }
            internal bool TryRemoveChildAllocator(AllocatorHandle handle, int childAllocatorIndex)
            {
                if(!NeedsUseAfterFreeTracking())
                    return false;
                if(childAllocatorIndex == InvalidChildAllocatorIndex)
                    return false;
                childAllocatorIndex = math.min(childAllocatorIndex, ChildAllocators.Length - 1);
                while(childAllocatorIndex >= 0)
                {
                    unsafe
                    {
                        var allocatorHandle = ChildAllocators.Ptr + childAllocatorIndex;
                        if(AreTheSame(*allocatorHandle, handle))
                        {
                            ChildAllocators.RemoveAtSwapBack(childAllocatorIndex);
                            return true;
                        }
                    }
                    --childAllocatorIndex;
                }
                return false;
            }
            internal void InvalidateDependents()
            {
                if(!NeedsUseAfterFreeTracking())
                    return;
                for(var i = 0; i < ChildSafetyHandles.Length; ++i)
                {
                    unsafe
                    {
                        AtomicSafetyHandle* handle = ChildSafetyHandles.Ptr + i;
                        if(CheckExists(*handle))
                            AtomicSafetyHandle.Release(*handle);
                    }
                }
                ChildSafetyHandles.Clear();
                if(Parent.IsValid)
                    Parent.TryRemoveChildAllocator(this, IndexInParent);
                Parent = default;
                IndexInParent = InvalidChildAllocatorIndex;
                for(var i = 0; i < ChildAllocators.Length; ++i)
                {
                    unsafe
                    {
                        AllocatorHandle* handle = (AllocatorHandle*)ChildAllocators.Ptr + i;
                        if(handle->IsValid)
                            handle->Unregister();
                    }
                }
                ChildAllocators.Clear();
            }
            
#endif
        
            /// <summary>
            /// Allows implicit conversion from allocator to allocator handle.
            /// </summary>
            /// <param name="a">Allocator to convert</param>
            /// <returns>A handle to the allocator</returns>
            public static implicit operator AllocatorHandle(Allocator a) => new AllocatorHandle { Index = (ushort)a };

            /// <summary>
            /// Index into a function table of allocation functions.
            /// </summary>
            public ushort Index;
            /// <summary>
            /// Version of the allocator at time of creation.
            /// </summary>
            public ushort Version;
            
            /// <summary>
            /// Return the index
            /// </summary>
            public int Value => Index;

            /// <summary>
            /// Allocates a Block of memory from this allocator with requested number of items of a given type.
            /// </summary>
            /// <typeparam name="T">Type of item to allocate.</typeparam>
            /// <param name="block">Block of memory to allocate within.</param>
            /// <param name="Items">Number of items to allocate.</param>
            /// <returns>Error code from the given Block's allocate function.</returns>
            public int TryAllocateBlock<T>(out Block block, int Items) where T : struct
            {
                block = new Block
                {
                    Range = new Range { Items = Items, Allocator = this },
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
            public Block AllocateBlock<T>(int Items) where T : struct
            {
                CheckValid(this);
                var error = TryAllocateBlock<T>(out Block block, Items);
                CheckAllocatedSuccessfully(error);
                return block;
            }

            [Conditional("ENABLE_UNITY_ALLOCATION_CHECKS")]
            static void CheckAllocatedSuccessfully(int error)
            {
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Allocate");
            }

            public TryFunction Function => default;
            public int Try(ref Block block)
            {
                block.Range.Allocator = this;
                var error = AllocatorManager.Try(ref block);
                return error;            
            }

            public AllocatorHandle Handle { get { return this; } set { this = value; } }

            public void Dispose()
            {
                Unregister(ref this);
            }
        }

        /// <summary>
        /// A handle to a block
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BlockHandle
        {
            /// <summary>
            /// Represents the handle
            /// </summary>
            public ushort Value;
        }

        /// <summary>
        /// Pointer for the beginning of a block of memory, number of items in it, which allocator it belongs to, and which block this is.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Range : IDisposable
        {
            /// <summary>
            /// Pointer to memory
            /// </summary>
            public IntPtr Pointer; //  0

            /// <summary>
            /// Number of items
            /// </summary>
            public int Items; //  8

            /// <summary>
            /// The allocator
            /// </summary>
            public AllocatorHandle Allocator; // 12

            /// <summary>
            /// Dispose the block corresponding to this range
            /// </summary>
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
            /// <summary>
            ///
            /// </summary>
            public Range Range;

            /// <summary>
            /// Number of bytes in each item requested.
            /// </summary>
            public int BytesPerItem;

            /// <summary>
            /// How many items were actually allocated.
            /// </summary>
            public int AllocatedItems;

            /// <summary>
            /// (1 &lt;&lt; this) is the byte alignment.
            /// </summary>
            public byte Log2Alignment;

            /// <summary>
            /// Unused.
            /// </summary>
            public byte Padding0;

            /// <summary>
            /// Unused.
            /// </summary>
            public ushort Padding1;

            /// <summary>
            /// Unused.
            /// </summary>
            public uint Padding2;

            /// <summary>
            /// Compute and return the total number of bytes required.
            /// </summary>
            public long Bytes => BytesPerItem * Range.Items;

            /// <summary>
            /// Compute and return the total number of bytes already allocated.
            /// </summary>
            public long AllocatedBytes => BytesPerItem * AllocatedItems;

            /// <summary>
            /// Compute and return the alignment
            /// </summary>
            public int Alignment
            {
                get => 1 << Log2Alignment;
                set => Log2Alignment = (byte)(32 - math.lzcnt(math.max(1, value) - 1));
            }

            /// <summary>
            /// Dispose the memory block
            /// </summary>
            public void Dispose()
            {
                TryFree();
            }

            /// <summary>
            /// Attempt to allocate the block
            /// </summary>
            /// <returns>An error code</returns>
            public int TryAllocate()
            {
                Range.Pointer = IntPtr.Zero;
                return Try(ref this);
            }

            /// <summary>
            /// Attempt to free the block
            /// </summary>
            /// <returns>An error code</returns>
            public int TryFree()
            {
                Range.Items = 0;
                return Try(ref this);
            }

            /// <summary>
            /// Allocate the block, throwing if not successful when checks are enabled
            /// </summary>
            public void Allocate()
            {
                var error = TryAllocate();
                CheckFailedToAllocate(error);
            }

            /// <summary>
            /// Free the block, throwing if not successful when checks are enabled
            /// </summary>
            public void Free()
            {
                var error = TryFree();
                CheckFailedToFree(error);
            }

            [Conditional("ENABLE_UNITY_ALLOCATION_CHECKS")]
            void CheckFailedToAllocate(int error)
            {
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Allocate {this}");
            }

            [Conditional("ENABLE_UNITY_ALLOCATION_CHECKS")]
            void CheckFailedToFree(int error)
            {
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Free {this}");
            }
        }

        /// <summary>
        /// An allocator with a tryable allocate/free/realloc function pointer.
        /// </summary>
        public interface IAllocator : IDisposable
        {
            /// <summary>
            /// The function associated with the allocator
            /// </summary>
            TryFunction Function { get; }
            /// <summary>
            /// Invoke the allocator's function to allocate, free or reallocate a block of memory
            /// </summary>
            /// <param name="block">The block to work on</param>
            /// <returns>An error code</returns>
            int Try(ref Block block);
            
            AllocatorHandle Handle { get; set; }
        }        

        internal static Allocator LegacyOf(AllocatorHandle handle)
        {
            if (handle.Value >= FirstUserIndex)
                return Allocator.Persistent;
            return (Allocator) handle.Value;
        }

        static unsafe int TryLegacy(ref Block block)
        {
            if (block.Range.Pointer == IntPtr.Zero) // Allocate
            {
                block.Range.Pointer = (IntPtr)Memory.Unmanaged.Allocate(block.Bytes, block.Alignment, LegacyOf(block.Range.Allocator));
                block.AllocatedItems = block.Range.Items;
                return (block.Range.Pointer == IntPtr.Zero) ? -1 : 0;
            }
            if (block.Bytes == 0) // Free
            {
                if(LegacyOf(block.Range.Allocator) != Allocator.None)
                    Memory.Unmanaged.Free((void*) block.Range.Pointer, LegacyOf(block.Range.Allocator));
                block.Range.Pointer = IntPtr.Zero;
                block.AllocatedItems = 0;
                return 0;
            }
            // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
            return -1;
        }

        /// <summary>
        /// Looks up an allocator's allocate, free, or realloc function pointer from a table and invokes the function.
        /// </summary>
        /// <param name="block">Block to allocate memory for.</param>
        /// <returns>Error code of invoked function.</returns>
        public static unsafe int Try(ref Block block)
        {
            if (block.Range.Allocator.Value < FirstUserIndex)
                return TryLegacy(ref block);
            TableEntry tableEntry = default;
            tableEntry = block.Range.Allocator.TableEntry;
            var function = new FunctionPointer<TryFunction>(tableEntry.function);
#if ENABLE_UNITY_ALLOCATION_CHECKS            
            // if the allocator being passed in has a version of 0, that means "whatever the current version is."
            // so we patch it here, with whatever the current version is...
            if(block.Range.Allocator.Version == 0)
                block.Range.Allocator.Version = block.Range.Allocator.OfficialVersion;
#endif                
            // this is really bad in non-Burst C#, it generates garbage each time we call Invoke
            return function.Invoke(tableEntry.state, ref block);
        }

        /// <summary>
        /// Stack allocator with no backing storage.
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
        internal struct StackAllocator : IAllocator, IDisposable
        {
            public AllocatorHandle Handle { get { return m_handle; } set { m_handle = value; } }
            internal AllocatorHandle m_handle;
                        
            internal Block m_storage;
            internal long m_top;

            public StackAllocator(Block storage)
            {
                this = default;
                m_storage = storage;
                m_top = 0;
#if ENABLE_UNITY_ALLOCATION_CHECKS
                Register(ref this); 
                m_storage.Range.Allocator.AddChildAllocator(Handle);
#endif                
            }

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
                    m_top += block.Bytes;
                    return 0;
                }

                if (block.Bytes == 0) // Free
                {
                    if ((byte*)block.Range.Pointer - (byte*)m_storage.Range.Pointer == (long)(m_top - block.AllocatedBytes))
                    {
                        m_top -= block.AllocatedBytes;
                        var blockSizeInBytes = block.AllocatedItems * block.BytesPerItem;
                        block.Range.Pointer = IntPtr.Zero;
                        block.AllocatedItems = 0;
                        return 0;
                    }

                    return -1;
                }

                // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
                return -1;
            }

            [BurstCompile(CompileSynchronously = true)]
			[MonoPInvokeCallback(typeof(TryFunction))]
            public static unsafe int Try(IntPtr allocatorState, ref Block block)
            {
                return ((StackAllocator*)allocatorState)->Try(ref block);
            }

            public TryFunction Function => Try;

            public void Dispose()
            {
                Unregister(ref this);
            }
        }

        /// <summary>
        /// Slab allocator with no backing storage.
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
        internal struct SlabAllocator : IAllocator, IDisposable
        {
            public AllocatorHandle Handle { get { return m_handle; } set { m_handle = value; } }
            internal AllocatorHandle m_handle;
        
            internal Block Storage;
            internal int Log2SlabSizeInBytes;
            internal FixedListInt4096 Occupied;
            internal long budgetInBytes;
            internal long allocatedBytes;

            public long BudgetInBytes => budgetInBytes;

            public long AllocatedBytes => allocatedBytes;

            internal int SlabSizeInBytes
            {
                get => 1 << Log2SlabSizeInBytes;
                set => Log2SlabSizeInBytes = (byte)(32 - math.lzcnt(math.max(1, value) - 1));
            }

            internal int Slabs => (int)(Storage.Bytes >> Log2SlabSizeInBytes);

            internal SlabAllocator(Block storage, int slabSizeInBytes, long budget)
            {
                this = default;           
#if ENABLE_UNITY_ALLOCATION_CHECKS 
                Register(ref this);
                storage.Range.Allocator.AddChildAllocator(Handle);
#endif                            
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

            [BurstCompile(CompileSynchronously = true)]
			[MonoPInvokeCallback(typeof(TryFunction))]
            public static unsafe int Try(IntPtr allocatorState, ref Block block)
            {
                return ((SlabAllocator*)allocatorState)->Try(ref block);
            }

            public TryFunction Function => Try;

            public void Dispose()
            {
                Unregister(ref this);
            }
        }

        internal struct TableEntry
        {
            internal IntPtr function;
            internal IntPtr state;
        }
                        
        internal struct Array16<T> where T : unmanaged
        {
            internal T f0, f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15;
        }
        internal struct Array256<T> where T : unmanaged
        {
            internal Array16<T> f0, f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15;
        }
        internal struct Array4096<T> where T : unmanaged
        {
            internal Array256<T> f0, f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15;
        }
        internal struct Array32768<T> : IIndexable<T> where T : unmanaged
        {
            internal Array4096<T> f0, f1, f2, f3, f4, f5, f6, f7;
            public int Length { get { return 32768; } set {} }
            public ref T ElementAt(int index)
            {
                unsafe { fixed(Array4096<T>* p = &f0) { return ref UnsafeUtility.AsRef<T>((T*)p + index); } }
            }
        }        

        /// <summary>
        /// SharedStatic that holds array of allocation function pointers for each allocator.
        /// </summary>
        internal sealed class SharedStatics
        {
            internal sealed class IsInstalled { internal static readonly SharedStatic<Long1024> Ref = SharedStatic<Long1024>.GetOrCreate<IsInstalled>(); }
            internal sealed class TableEntry { internal static readonly SharedStatic<Array32768<AllocatorManager.TableEntry>> Ref = SharedStatic<Array32768<AllocatorManager.TableEntry>>.GetOrCreate<TableEntry>(); }
#if ENABLE_UNITY_ALLOCATION_CHECKS
            internal sealed class Version { internal static readonly SharedStatic<Array32768<ushort>> Ref = SharedStatic<Array32768<ushort>>.GetOrCreate<Version>(); }
            internal sealed class ChildSafetyHandles { internal static readonly SharedStatic<Array32768<UnsafeList<AtomicSafetyHandle>>> Ref = SharedStatic<Array32768<UnsafeList<AtomicSafetyHandle>>>.GetOrCreate<ChildSafetyHandles>(); }
            internal sealed class ChildAllocators { internal static readonly SharedStatic<Array32768<UnsafeList<AllocatorHandle>>> Ref = SharedStatic<Array32768<UnsafeList<AllocatorHandle>>>.GetOrCreate<ChildAllocators>(); }
            internal sealed class Parent { internal static readonly SharedStatic<Array32768<AllocatorHandle>> Ref = SharedStatic<Array32768<AllocatorHandle>>.GetOrCreate<Parent>(); }
            internal sealed class IndexInParent { internal static readonly SharedStatic<Array32768<int>> Ref = SharedStatic<Array32768<int>>.GetOrCreate<IndexInParent>(); }
#endif            
        }

        /// <summary>
        /// Initializes SharedStatic allocator function table and allocator table, and installs default allocators.
        /// </summary>
        public static void Initialize()
        {
        }

        /// <summary>
        /// Creates and saves allocators' function pointers into function table.
        /// </summary>
        /// <param name="handle">AllocatorHandle to allocator to install function for.</param>
        /// <param name="allocatorState">IntPtr to allocator's custom state.</param>
        /// <param name="functionPointer">Function pointer to create or save in function table.</param>
        internal static void Install(AllocatorHandle handle, IntPtr allocatorState, FunctionPointer<TryFunction> functionPointer)
        {
            if(functionPointer.Value == IntPtr.Zero)
                handle.Unregister();
            else
            {
                int error = ConcurrentMask.TryAllocate(ref SharedStatics.IsInstalled.Ref.Data, handle.Value, 1); 
                if(ConcurrentMask.Succeeded(error))
                    handle.Install(new TableEntry { state = allocatorState, function = functionPointer.Value });
            }
        }

        /// <summary>
        /// Creates and saves allocators' function pointers into function table.
        /// </summary>
        /// <param name="handle">AllocatorHandle to allocator to install function for.</param>
        /// <param name="allocatorState">IntPtr to allocator's custom state.</param>
        /// <param name="function">Function pointer to create or save in function table.</param>
        internal static void Install(AllocatorHandle handle, IntPtr allocatorState, TryFunction function)
        {
            var functionPointer = (function == null)
                ? new FunctionPointer<TryFunction>(IntPtr.Zero)
                : BurstCompiler.CompileFunctionPointer(function);
            Install(handle, allocatorState, functionPointer);
        }

        /// <summary>
        /// Threadsafely finds a slot for an allocator that isn't currently in use, and installs the
        /// user-provided function and state pointers into that slot.
        /// </summary>
        /// <param name="allocatorState">IntPtr to allocator's custom state.</param>
        /// <param name="functionPointer">Function pointer to create or save in function table.</param>
        /// <returns>An AllocatorHandle that refers to the newly-allocated slot.</returns>
        internal static AllocatorHandle Register(IntPtr allocatorState, FunctionPointer<TryFunction> functionPointer)
        {
            var tableEntry = new TableEntry { state = allocatorState, function = functionPointer.Value };
            var error = ConcurrentMask.TryAllocate(ref SharedStatics.IsInstalled.Ref.Data, out int offset, (FirstUserIndex+63)>>6, SharedStatics.IsInstalled.Ref.Data.Length, 1);
            AllocatorHandle handle = default;
            if(ConcurrentMask.Succeeded(error))
            {
                handle.Index = (ushort)offset;
                handle.Install(tableEntry);
#if ENABLE_UNITY_ALLOCATION_CHECKS                
                handle.Version = handle.OfficialVersion;
#endif                
            }                    
            return handle;
        }

        /// <summary>
        /// Type safe version of Register
        /// </summary>
        /// <typeparam name="T">The type of allocator to register</typeparam>
        /// <param name="t">Reference to the allocator</param>
        /// <returns>A handle to the allocator</returns>
        public static unsafe void Register<T>(ref this T t) where T : unmanaged, IAllocator
        {
            var functionPointer = (t.Function == null)
                ? new FunctionPointer<TryFunction>(IntPtr.Zero)
                : BurstCompiler.CompileFunctionPointer(t.Function);
            t.Handle = Register((IntPtr)UnsafeUtility.AddressOf(ref t), functionPointer);
        }

        public static void Unregister<T>(ref this T t) where T : unmanaged, IAllocator
        {
            if(t.Handle.IsInstalled)
            {
                t.Handle.Install(default);
                ConcurrentMask.TryFree(ref SharedStatics.IsInstalled.Ref.Data, t.Handle.Value, 1);
            }
        }
                                
        /// <summary>
        /// Shut down
        /// </summary>
        public static void Shutdown()
        {
        }

        /// <summary>
        /// User-defined allocator index.
        /// </summary>
        public const ushort FirstUserIndex = 64;

        [Conditional("ENABLE_UNITY_ALLOCATION_CHECKS")]
        internal static void CheckFailedToAllocate(int error)
        {
            if (error != 0)
                throw new ArgumentException("failed to allocate");
        }

        [Conditional("ENABLE_UNITY_ALLOCATION_CHECKS")]
        internal static void CheckFailedToFree(int error)
        {
            if (error != 0)
                throw new ArgumentException("failed to free");
        }
        
        [Conditional("ENABLE_UNITY_ALLOCATION_CHECKS")]
        internal static void CheckValid(AllocatorHandle handle)
        {
#if ENABLE_UNITY_ALLOCATION_CHECKS        
            if(handle.IsValid == false)
                throw new ArgumentException("allocator handle is not valid.");
#endif                
        }
    }
}

#pragma warning restore 0649
