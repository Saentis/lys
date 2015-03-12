/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys.Language
{
    /// <summary>
    /// Interface for a type look-up table.
    /// </summary>
    public interface ITypeTable
    {
        /// <summary>
        /// Checks whether the given type has been defined.
        /// </summary>
        /// <param name="path">The full type path (including namespaces, separated by '::').</param>
        /// <returns>true if the type is defined.</returns>
        bool Has(string path);

        /// <summary>
        /// Looks up the type at the given path.
        /// </summary>
        /// <param name="path">The full type path (including namespaces, separated by '::').</param>
        /// <returns>the type.</returns>
        /// <exception cref="System.ArgumentException">Throws an ArgumentException if the type is not defined.</exception>
        IType Lookup(string path);

        /// <summary>
        /// Defines the type at the given path.
        /// </summary>
        /// <param name="path">The full type path (including namespaces, separated by '::').</param>
        /// <param name="type">The type to be defined.</param>
        /// <exception cref="System.ArgumentException">Throws an ArgumentException if the type is already defined.</exception>
        void Define(string path, IType type);
    }
}
