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
    /// Represents an iterator for a collection of operations.
    /// </summary>
    public interface IOperationCollectionIterator
    {
        /// <summary>
        /// Moves the pointer to the next operation in the collection.
        /// The first call moves it to the very first element.
        /// </summary>
        /// <returns>true if the pointer is pointing at an actual operation, false if it reached the end.</returns>
        bool Next();

        /// <summary>
        /// Moves the pointer to the previous operation in the collection.
        /// </summary>
        /// <returns>true if the pointer is pointing at an actual operation, false if it reached the beginning.</returns>
        bool Back();

        /// <summary>
        /// Gets the operation which the pointer currently is pointing at.
        /// </summary>
        IOperation Current { get; }
    }

}
