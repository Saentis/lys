/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System.Collections.Generic;

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Interface for a basic compiler instruction.
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// Gets the type of operation.
        /// </summary>
        OperationType Type { get; }

        /// <summary>
        /// Gets the position of the associated instruction in the source code.
        /// This is used by the compiler to produce more accurate error messages.
        /// </summary>
        long SourcePosition { get; }

        /// <summary>
        /// Gets meta data associated to this operation (for instance, parameters).
        /// </summary>
        IEnumerable<object> GetMetaData();
    }
}
