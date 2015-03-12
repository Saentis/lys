/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;

namespace Octarine.Lys.Process
{
    /// <summary>
    /// Represents a collection of operations.
    /// </summary>
    public interface IOperationCollection
    {
        /// <summary>
        /// Gets the amount of operations in this collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Appends a single operation to the end of the collection.
        /// </summary>
        void Append(IOperation operation);

        /// <summary>
        /// Prepends a single operation to the beginning of the collection.
        /// </summary>
        void Prepend(IOperation operation);

        /// <summary>
        /// Appends another collection to the end of the collection.
        /// </summary>
        void Append(IOperationCollection collection);

        /// <summary>
        /// Appends another collection to the beginning of the collection.
        /// </summary>
        void Prepend(IOperationCollection collection);

        /// <summary>
        /// Executes an action for each of the operations.
        /// </summary>
        void ForEach(Action<IOperation> handler);

        /// <summary>
        /// Gets an iterator for this collection.
        /// </summary>
        IOperationCollectionIterator GetIterator();

        /// <summary>
        /// Gets the last IOperation of this collection.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Raises an InvalidOperationException if the collection is empty.</exception>
        IOperation Last { get; }
    }

}