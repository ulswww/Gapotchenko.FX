﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

#if !TFF_SWITCHEXPRESSIONEXCEPTION

namespace System.Runtime.CompilerServices
{
    using Resources = Gapotchenko.FX.Properties.Resources;

    /// <summary>
    /// <para>
    /// Indicates that a switch expression that was non-exhaustive failed to match its input at runtime.
    /// The exception optionally contains an object representing the unmatched value.
    /// </para>
    /// <para>
    /// This is a polyfill provided by Gapotchenko.FX.
    /// </para>
    /// </summary>
    [Serializable]
    public sealed class SwitchExpressionException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchExpressionException"/> class.
        /// </summary>
        public SwitchExpressionException() :
            base(Resources.SwitchExpressionException_Message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchExpressionException"/> class with a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SwitchExpressionException(Exception innerException) :
            base(Resources.SwitchExpressionException_Message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchExpressionException"/> class with an unmatched switch value.
        /// </summary>
        /// <param name="unmatchedValue">The switch value that does not match any switch cases.</param>
        public SwitchExpressionException(object unmatchedValue) :
            this()
        {
            UnmatchedValue = unmatchedValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchExpressionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public SwitchExpressionException(string message) :
            base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchExpressionException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SwitchExpressionException(string message, Exception innerException) :
            base(message, innerException)
        {
        }

        /// <summary>
        /// Gets the unmatched value associated with the exception.
        /// </summary>
        public object UnmatchedValue { get; }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with the parameter name and additional exception information.
        /// </summary>
        /// <param name="info">Serialization information.</param>
        /// <param name="context">Streaming context.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(UnmatchedValue), UnmatchedValue, typeof(object));
        }

        /// <summary>
        /// Gets the exception message.
        /// </summary>
        public override string Message
        {
            get
            {
                if (UnmatchedValue is null)
                {
                    return base.Message;
                }
                else
                {
                    string valueMessage = string.Format(Resources.SwitchExpressionException_UnmatchedValue, UnmatchedValue);
                    return base.Message + Environment.NewLine + valueMessage;
                }
            }
        }
    }
}

#else

[assembly: TypeForwardedTo(typeof(SwitchExpressionException))]

#endif