/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;

namespace Octarine.Lys.Language
{
    /// <summary>
    /// Default implementation of a type look-up table.
    /// </summary>
    public class TypeTable : ITypeTable
    {
        private Dictionary<string, IType> _dict = new Dictionary<string, IType>();

        /// <summary>
        /// Checks whether the given type has been defined.
        /// </summary>
        /// <param name="path">The full type path (including namespaces, separated by '::').</param>
        /// <returns>true if the type is defined.</returns>
        public bool Has(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            return _dict.ContainsKey(path);
        }

        /// <summary>
        /// Looks up the type at the given path.
        /// </summary>
        /// <param name="path">The full type path (including namespaces, separated by '::').</param>
        /// <returns>the type.</returns>
        /// <exception cref="System.ArgumentException">Throws an ArgumentException if the type is not defined.</exception>
        public IType Lookup(string path)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (_dict.ContainsKey(path))
                return _dict[path];
            else
                throw new ArgumentException("Type '" + path + "' is not defined");
        }

        /// <summary>
        /// Defines the type at the given path.
        /// </summary>
        /// <param name="path">The full type path (including namespaces, separated by '::').</param>
        /// <param name="type">The type to be defined.</param>
        /// <exception cref="System.ArgumentException">Throws an ArgumentException if the type is already defined.</exception>
        public void Define(string path, IType type)
        {
            if (path == null) throw new ArgumentNullException("path");
            if (object.ReferenceEquals(null, type)) throw new ArgumentNullException("type");
            if (!_dict.ContainsKey(path))
                _dict.Add(path, type);
            else
                throw new ArgumentException("Type '" + path + "' is already defined");
        }
    }
}