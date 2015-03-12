/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using Octarine.Lys.Language;
using Octarine.Lys.Parse;

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Factory which creates InstructionInterpreter instances.
    /// </summary>
    public class InstructionInterpreterFactory : IInterpreterFactory
    {
        public IInterpreter Create(FunctionContext function, TokenIterator tokenIterator)
        {
            if (object.ReferenceEquals(null, tokenIterator)) throw new ArgumentNullException("tokenIterator");
            return new InstructionInterpreter(function, tokenIterator);
        }
    }
}