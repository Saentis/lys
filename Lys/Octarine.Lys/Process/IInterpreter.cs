/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System.Collections.Generic;

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Interface for interpreting code as operations.
    /// </summary>
    public interface IInterpreter
    {
        /// <summary>
        /// Interpretes the next instruction.
        /// </summary>
        IOperationCollection InterpreteNext();
    }
}
