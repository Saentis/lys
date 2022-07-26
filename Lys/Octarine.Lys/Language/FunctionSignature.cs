/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys.Language
{
    /// <summary>
    /// Describes a function by specifying its signature.
    /// </summary>
    public struct FunctionSignature
    {
        /// <summary>
        /// The function name as invoked in source code.
        /// </summary>
        public string Name;

        /// <summary>
        /// Indicates whether this is a built-in function.
        /// </summary>
        public bool IsBuiltin;

        /// <summary>
        /// The index of the function.
        /// Functions with the same name (but different arguments) are guaranteed to have a different index so as to distinguish them.
        /// </summary>
        public int Index;

        /// <summary>
        /// The function argument variables.
        /// </summary>
        public Variable[] Arguments;

        /// <summary>
        /// The return type of the function, or null if it does not have any.
        /// </summary>
        public IType? ReturnType;

        /// <summary>
        /// The path of the namespace which this function is contained in.
        /// </summary>
        public string[] Namespace;
    }
}