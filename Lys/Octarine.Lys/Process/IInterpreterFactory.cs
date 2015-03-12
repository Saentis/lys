/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using Octarine.Lys.Language;

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Interface for an IInterpreter factory.
    /// </summary>
    public interface IInterpreterFactory
    {
        /// <summary>
        /// Creates a new interpreter instance.
        /// </summary>
        /// <param name="function">The function description in the context of which the instructions are interpreted.</param>
        /// <param name="tokenIterator">The token iterator which shall be used.</param>
        IInterpreter Create(FunctionContext function, Parse.TokenIterator tokenIterator);
    }
}
