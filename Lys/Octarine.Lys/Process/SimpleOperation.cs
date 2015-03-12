/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System.Collections.Generic;

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Represents a simple operation with no additional parameters.
    /// </summary>
    public class SimpleOperation : IOperation
    {
        /// <summary>
        /// Initializes a new simple operation.
        /// </summary>
        /// <param name="type">The operation type.</param>
        /// <param name="sourcePosition">The position of the associated instruction in the source code.</param>
        public SimpleOperation(OperationType type, long sourcePosition)
        {
            this.Type = type;
            this.SourcePosition = sourcePosition;
        }

        /// <summary>
        /// Gets the position of the associated instruction in the source code.
        /// This is used by the compiler to produce more accurate error messages.
        /// </summary>
        public long SourcePosition { get; private set; }

        /// <summary>
        /// Gets the type of operation.
        /// </summary>
        public OperationType Type { get; private set; }

        /// <summary>
        /// Gets meta data associated to this operation (for instance, parameters).
        /// </summary>
        /// <remarks>This method always returns an empty collection.</remarks>
        public virtual IEnumerable<object> GetMetaData()
        {
            yield break;
        }

        public override string ToString()
        {
            return Type.ToString();
        }

    }

}
