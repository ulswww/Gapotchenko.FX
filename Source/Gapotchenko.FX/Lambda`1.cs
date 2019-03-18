﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gapotchenko.FX
{
    /// <summary>
    /// Provides lambda calculus primitives for type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to provide lambda calculus primitives for.</typeparam>
    public static class Lambda<T>
    {
        static class Factory
        {
            public static readonly Func<T> Default = Lambda.Default<T>;
            public static readonly Func<T, T> Identity = Lambda.Identity;
        }

        /// <summary>
        /// Gets a delegate to a pure function that returns a default value of type <typeparamref name="T"/>, e.g. f() = default(T).
        /// </summary>
        public static Func<T> Default { get; } = Factory.Default;

        /// <summary>
        /// Gets a delegate to a pure identity function that returns a value of its single argument, e.g. f(x) = x.
        /// </summary>
        public static Func<T, T> Identity { get; } = Factory.Identity;
    }
}
