/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys.Language
{
    /// <summary>
    /// Describes a function context.
    /// </summary>
    public struct FunctionContext
    {
        /// <summary>
        /// The signature of the function.
        /// </summary>
        public FunctionSignature Signature;

        /// <summary>
        /// The table of all types which have been defined.
        /// </summary>
        public ITypeTable TypeTable;
    }
}