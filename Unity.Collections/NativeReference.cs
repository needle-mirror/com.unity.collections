using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Collections
{
    /// <summary>
    /// An unmanaged, reference container.
    /// </summary>
    /// <typeparam name="T">The type of the reference in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    public unsafe struct NativeReference<T>
        : INativeDisposable
        , IEquatable<NativeReference<T>>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;

        static readonly SharedStatic<int> s_SafetyId = SharedStatic<int>.GetOrCreate<NativeReference<T>>();

        [BurstDiscard]
        static void CreateStaticSafetyId()
        {
            s_SafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeReference<T>>();
        }

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        internal Allocator m_AllocatorLabel;

        /// <summary>
        /// Constructs a new reference container using the specified type of memory allocation.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <param name="options">A member of the
        /// [Unity.Collections.NativeArrayOptions](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArrayOptions.html) enumeration.</param>
        public NativeReference(Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(allocator, out this);
            if (options == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(m_Data, UnsafeUtility.SizeOf<T>());
            }
        }

        /// <summary>
        /// Constructs a new reference container using the specified type of memory allocation, and initialize it to specific value.
        /// </summary>
        /// <param name="value">The value of this container.</param>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public NativeReference(T value, Allocator allocator)
        {
            Allocate(allocator, out this);
            *(T*)m_Data = value;
        }

        static void Allocate(Allocator allocator, out NativeReference<T> reference)
        {
            CheckAllocator(allocator);

            reference = default;
            reference.m_Data = Memory.Unmanaged.Allocate(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
            reference.m_AllocatorLabel = allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out reference.m_Safety, out reference.m_DisposeSentinel, 1, allocator);

            if (s_SafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref reference.m_Safety, s_SafetyId.Data);
#endif
        }

        /// <summary>
        /// The value of this container.
        /// </summary>
        public T Value
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return *(T*)m_Data;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                *(T*)m_Data = value;
            }
        }

        /// <summary>
        /// Determine whether or not this container is created.
        /// </summary>
        /// <remarks>
        /// *Warning:* the `IsCreated` property can't be used to determine whether a copy of a container is still valid.
        /// If you dispose any copy of the container, the container storage is deallocated. However, the properties of
        /// the other copies of the container (including the original) are not updated. As a result the `IsCreated` property
        /// of the copies still return `true` even though the container storage has been deallocated.
        /// Accessing the data of a native container that has been disposed throws a <see cref='InvalidOperationException'/> exception.
        /// </remarks>
        public bool IsCreated => m_Data != null;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            CheckNotDisposed();

            if (CollectionHelper.ShouldDeallocate(m_AllocatorLabel))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
                Memory.Unmanaged.Free(m_Data, m_AllocatorLabel);
                m_AllocatorLabel = Allocator.Invalid;
            }

            m_Data = null;
        }

        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>You can call this function dispose of the container immediately after scheduling the job.
        /// Pass the [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) returned by
        /// the [Job.Schedule](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html)
        /// method using the `jobHandle` parameter so the job scheduler can dispose the container after all jobs using it have run.</remarks>
        /// <param name="inputDeps">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that deletes the container.</returns>
        [BurstCompatible(RequiredUnityDefine = "UNITY_2020_2_OR_NEWER") /* Due to job scheduling on 2020.1 using statics */]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            CheckNotDisposed();

            if (CollectionHelper.ShouldDeallocate(m_AllocatorLabel))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
                // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
                // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
                // will check that no jobs are writing to the container).
                DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif

                var jobHandle = new NativeReferenceDisposeJob
                {
                    Data = new NativeReferenceDispose
                    {
                        m_Data = m_Data,
                        m_AllocatorLabel = m_AllocatorLabel,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        m_Safety = m_Safety
#endif
                    }
                }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(m_Safety);
#endif

                m_Data = null;
                m_AllocatorLabel = Allocator.Invalid;

                return jobHandle;
            }

            m_Data = null;

            return inputDeps;
        }

        /// <summary>
        /// Copy this container from another container.
        /// </summary>
        /// <param name="reference">The container to copy from.</param>
        public void CopyFrom(NativeReference<T> reference)
        {
            Copy(this, reference);
        }

        /// <summary>
        /// Copy this container to another container.
        /// </summary>
        /// <param name="reference">The container to copy to.</param>
        public void CopyTo(NativeReference<T> reference)
        {
            Copy(reference, this);
        }

        /// <summary>
        /// Determine whether this container is equal to another container.
        /// </summary>
        /// <param name="other">A container to compare with this container.</param>
        /// <returns><see langword="true"/> if this container is equal to the <paramref name="other"/> parameter, otherwise <see langword="false"/>.</returns>
        [NotBurstCompatible]
        public bool Equals(NativeReference<T> other)
        {
            return Value.Equals(other.Value);
        }

        /// <summary>
        /// Determine whether this object is equal to another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this object.</param>
        /// <returns><see langword="true"/> if this object is equal to the <paramref name="obj"/> parameter, otherwise <see langword="false"/>.</returns>
        [NotBurstCompatible]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is NativeReference<T> && Equals((NativeReference<T>)obj);
        }

        /// <summary>
        /// Returns the hash code for this container.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Determine whether a container is equal to another container.
        /// </summary>
        /// <param name="left">Left-hand side container.</param>
        /// <param name="right">Right-hand side container.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> parameter is equal to the <paramref name="right"/> parameter, otherwise <see langword="false"/>.</returns>
        public static bool operator ==(NativeReference<T> left, NativeReference<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determine whether a container is not equal to another container.
        /// </summary>
        /// <param name="left">Left-hand side container.</param>
        /// <param name="right">Right-hand side container.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> parameter is not equal to the <paramref name="right"/> parameter, otherwise <see langword="false"/>.</returns>
        public static bool operator !=(NativeReference<T> left, NativeReference<T> right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Copy source container to destination container.
        /// </summary>
        /// <param name="dst">The destination reference.</param>
        /// <param name="src">The source reference.</param>
        public static void Copy(NativeReference<T> dst, NativeReference<T> src)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
#endif
            UnsafeUtility.MemCpy(dst.m_Data, src.m_Data, UnsafeUtility.SizeOf<T>());
        }

        /// <summary>
        /// Retrieve this container as read-only.
        /// </summary>
        /// <returns>A read-only reference container.</returns>
        public ReadOnly AsReadOnly()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ReadOnly(m_Data, ref m_Safety);
#else
            return new ReadOnly(m_Data);
#endif
        }

        /// <summary>
        /// An unmanaged, read-only reference container.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public unsafe struct ReadOnly
        {
            [NativeDisableUnsafePtrRestriction]
            readonly void* m_Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle m_Safety;

            internal ReadOnly(void* data, ref AtomicSafetyHandle safety)
            {
                m_Data = data;
                m_Safety = safety;
            }
#else
            internal ReadOnly(void* data)
            {
                m_Data = data;
            }
#endif

            /// <summary>
            /// The value of this container.
            /// </summary>
            public T Value
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return *(T*)m_Data;
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAllocator(Allocator allocator)
        {
            // Native allocation is only valid for Temp, Job and Persistent.
            if (allocator <= Allocator.None)
                throw new ArgumentException($"Allocator must be {Allocator.Temp}, {Allocator.TempJob} or {Allocator.Persistent}", nameof(allocator));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckNotDisposed()
        {
            if (m_Data == null)
                throw new ObjectDisposedException("The NativeReference is already disposed.");
        }
    }

    [NativeContainer]
    unsafe struct NativeReferenceDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* m_Data;

        internal Allocator m_AllocatorLabel;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        public void Dispose()
        {
            Memory.Unmanaged.Free(m_Data, m_AllocatorLabel);
        }
    }

    [BurstCompile]
    struct NativeReferenceDisposeJob : IJob
    {
        internal NativeReferenceDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }
}

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// <see cref="NativeReference{T}"/> unsafe utilities.
    /// </summary>
    [BurstCompatible]
    public static class NativeReferenceUnsafeUtility
    {
        /// <summary>
        /// Retrieve the data pointer of this container and check for write access.
        /// </summary>
        /// <typeparam name="T">The type of the reference in the container.</typeparam>
        /// <param name="reference">The reference container.</param>
        /// <returns>The data pointer.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static unsafe void* GetUnsafePtr<T>(this NativeReference<T> reference)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(reference.m_Safety);
#endif
            return reference.m_Data;
        }

        /// <summary>
        /// Retrieve the data pointer of this container and check for read access.
        /// </summary>
        /// <typeparam name="T">The type of the reference in the container.</typeparam>
        /// <param name="reference">The reference container.</param>
        /// <returns>The data pointer.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this NativeReference<T> reference)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(reference.m_Safety);
#endif
            return reference.m_Data;
        }

        /// <summary>
        /// Retrieve the data pointer of this container without read/write access checks.
        /// </summary>
        /// <typeparam name="T">The type of the reference in the container.</typeparam>
        /// <param name="reference">The reference container.</param>
        /// <returns>The data pointer.</returns>
        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        public static unsafe void* GetUnsafePtrWithoutChecks<T>(NativeReference<T> reference)
            where T : unmanaged
        {
            return reference.m_Data;
        }
    }
}
