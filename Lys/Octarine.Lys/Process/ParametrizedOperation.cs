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
    /// Represents an operation with one additional parameter.
    /// </summary>
    public class ParametrizedOperation<T> : SimpleOperation
    {
        /// <summary>
        /// Initializes a new parametrized operation.
        /// </summary>
        /// <param name="type">The operation type.</param>
        /// <param name="parameter">The additional parameter.</param>
        /// <param name="sourcePosition">The position of the associated instruction in the source code.</param>
        public ParametrizedOperation(OperationType type, long sourcePosition, T parameter)
            : base(type, sourcePosition)
        {
            if (parameter == null) throw new ArgumentNullException("parameter");
            this.Parameter = parameter;
        }

        /// <summary>
        /// Gets the additional operation parameter.
        /// </summary>
        public T Parameter { get; private set; }

        /// <summary>
        /// Gets meta data associated to this operation (for instance, parameters).
        /// </summary>
        /// <remarks>This method returns a collection consisting of this.Parameter.</remarks>
        public override IEnumerable<object> GetMetaData()
        {
            yield return this.Parameter;
        }

        public override string ToString()
        {
            if (this.Parameter is Array)
            {
                Array array = this.Parameter as Array;
                object[] arrayCopy = new object[array.Length];
                array.CopyTo(arrayCopy, 0);
                return base.ToString() + " (" + string.Join(", ", Array.ConvertAll(arrayCopy, x => x.ToString())) + ")";
            }
            else
            {
                return base.ToString() + " (" + this.Parameter.ToString() + ")";
            }
        }

    }
}