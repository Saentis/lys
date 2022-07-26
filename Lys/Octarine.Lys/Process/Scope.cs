/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Octarine.Lys.Language;

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Represents an execution scope which keeps track of variables.
    /// </summary>
    public class Scope
    {
        /// <summary>
        /// Initializes a global scope.
        /// </summary>
        public Scope()
        {
        }

        /// <summary>
        /// Initializes a child scope.
        /// </summary>
        public Scope(Scope parent)
        {
            _parent = parent;
        }

        private Scope? _parent = null;
        private Dictionary<string, IType> _vars = new Dictionary<string, IType>();
        private List<string[]> _imports = new List<string[]>();

        /// <summary>
        /// Registers a variable within this scope.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="type">The variable type.</param>
        /// <exception cref="System.ArgumentException">Throws an ArgumentException if such a variable already exists.</exception>
        /// <exception cref="System.ArgumentNullException">Throws an ArgumentNullException if any parameter is null.</exception>
        public void RegisterVariable(string name, IType type)
        {
            if (object.ReferenceEquals(null, name)) throw new ArgumentNullException("name");
            if (object.ReferenceEquals(null, type)) throw new ArgumentNullException("type");

            if (_vars.ContainsKey(name))
                throw new ArgumentException("Duplicate variable: " + name);
            _vars.Add(name, type);
        }

        /// <summary>
        /// Checks whether a variable is defined.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <exception cref="System.ArgumentNullException">Throws an ArgumentNullException if any parameter is null.</exception>
        public bool HasVariable(string name)
        {
            if (object.ReferenceEquals(null, name)) throw new ArgumentNullException("name");

            return _vars.ContainsKey(name) || (_parent != null && _parent.HasVariable(name));
        }

        /// <summary>
        /// Gets the type of the specified variable.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <exception cref="System.ArgumentNullException">Throws an ArgumentNullException if any parameter is null.</exception>
        public IType GetVariableType(string name)
        {
            if (object.ReferenceEquals(null, name)) throw new ArgumentNullException("name");

            if (_vars.ContainsKey(name))
                return _vars[name];
            else if (_parent != null)
                return _parent.GetVariableType(name);
            else
                throw new ArgumentException("Variable not defined: " + name);
        }

        /// <summary>
        /// Imports a namespace so that function in it can be resolved.
        /// </summary>
        /// <param name="ns">The absolute path to the namespace.</param>
        public void ImportNamespace(string[] ns)
        {
            if (ns == null) throw new ArgumentNullException("ns");
            if (ns.Length == 0) throw new ArgumentException("Namespace path must not be empty");
            _imports.Add(ns);
        }

        /// <summary>
        /// Gets all imported namespaces in this and all parent scopes.
        /// The most important ones occur at the very beginning of the enumeration.
        /// </summary>
        public IEnumerable<string[]> GetImportedNamespaces()
        {
            return this.GetImportedNamespacesWithDuplicates().Distinct();
        }

        /// <summary>
        /// Gets all imported namespaces in this and all parent scopes by just concatenating them.
        /// </summary>
        private IEnumerable<string[]> GetImportedNamespacesWithDuplicates()
        {
            if (_parent == null)
                return _imports;
            else
                return _imports.Concat(_parent.GetImportedNamespacesWithDuplicates());
        }

    }
}