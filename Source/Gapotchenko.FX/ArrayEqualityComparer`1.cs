﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gapotchenko.FX
{
    /// <summary>
    /// Optimized and fast equality comparer for one-dimensional arrays.
    /// </summary>
    public abstract partial class ArrayEqualityComparer<T> : EqualityComparer<T[]>
    {
        static class DefaultFactory
        {
            public static readonly ArrayEqualityComparer<T> Instance = ArrayEqualityComparer.Create<T>(null);
        }

        /// <summary>
        /// Returns a default equality comparer for one-dimensional array with an element type specified by the generic argument <typeparamref name="T"/>.
        /// </summary>
        public new static ArrayEqualityComparer<T> Default => DefaultFactory.Instance;
    }
}
