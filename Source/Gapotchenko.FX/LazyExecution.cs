﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Gapotchenko.FX
{
    /// <summary>
    /// Provides a strategy which delays the execution of an action until its explicitly asserted with <see cref="EnsureExecuted"/> method.
    /// </summary>
    /// <remarks>
    /// <see cref="LazyExecution"/> is not thread-safe.
    /// For thread-safe lazy execution, please use <see cref="Threading.ExecuteOnce"/> struct.
    /// </remarks>
    [DebuggerDisplay("IsExecuted={IsExecuted}")]
    public struct LazyExecution
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LazyExecution"/> struct.
        /// </summary>
        /// <param name="action">The action.</param>
        public LazyExecution(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _Action = Empty.Nullify(action);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        Action _Action;

        /// <summary>
        /// Ensures that the action was executed.
        /// </summary>
        public void EnsureExecuted()
        {
            var emptyAction = Lambda.Empty;
            if (_Action != emptyAction)
            {
                _Action?.Invoke();
                _Action = emptyAction;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the action was executed.
        /// </summary>
        public bool IsExecuted => _Action == Lambda.Empty;
    }
}
