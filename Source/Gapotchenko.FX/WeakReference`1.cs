﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;

#if !TFF_WEAKREFERENCE_1

namespace System
{
    /// <summary>
    /// <para>
    /// Represents a typed weak reference, which references an object while still allowing that object to be reclaimed by garbage collection.
    /// </para>
    /// <para>
    /// This is a polyfill provided by Gapotchenko.FX.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of the object referenced.</typeparam>
    [Serializable]
    public sealed class WeakReference<T> : ISerializable
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeakReference{T}"/> class that references the specified object.
        /// </summary>
        /// <param name="target">The object to reference, or null.</param>
        public WeakReference(T target) :
            this(target, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeakReference{T}"/> class that references the specified object and
        /// uses the specified resurrection tracking.
        /// </summary>
        /// <param name="target">The object to reference, or null.</param>
        /// <param name="trackResurrection">
        /// <c>true</c> to track the object after finalization;
        /// <c>false</c> to track the object only until finalization.
        /// </param>
        public WeakReference(T target, bool trackResurrection)
        {
            m_WR = new WeakReference(target, trackResurrection);
        }

        WeakReference(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var target = (T)info.GetValue("TrackedObject", typeof(T));
            bool trackResurrection = info.GetBoolean("TrackResurrection");

            m_WR = new WeakReference(target, trackResurrection);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        WeakReference m_WR;

        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> object with all the data necessary to serialize the current <see cref="WeakReference{T}"/> object.
        /// </summary>
        /// <param name="info">An object that holds all the data necessary to serialize or deserialize the current <see cref="WeakReference{T}"/> object.</param>
        /// <param name="context">The location where serialized data is stored and retrieved.</param>
        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("TrackedObject", m_WR.Target, typeof(T));
            info.AddValue("TrackResurrection", m_WR.TrackResurrection);
        }

        /// <summary>
        /// Sets the target object that is referenced by this <see cref="WeakReference{T}"/> object.
        /// </summary>
        /// <param name="target">The new target object.</param>
        public void SetTarget(T target) => m_WR.Target = target;

        /// <summary>
        /// Tries to retrieve the target object that is referenced by the current <see cref="WeakReference{T}"/> object.
        /// </summary>
        /// <param name="target">
        /// When this method returns, contains the target object, if it is available.
        /// This parameter is treated as uninitialized.
        /// </param>
        /// <returns><c>true</c> if the target was retrieved; otherwise, <c>false</c>.</returns>
        public bool TryGetTarget(out T target)
        {
            var t = (T)m_WR.Target;
            target = t;
            return t != null;
        }
    }
}

#else

[assembly: TypeForwardedTo(typeof(WeakReference<>))]

#endif
