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
    /// Represents an operation with two additional parameters.
    /// </summary>
    public class FourParametrizedOperation<T1, T2, T3, T4> : SimpleOperation
    {
        /// <summary>
        /// Initializes a new parametrized operation.
        /// </summary>
        /// <param name="type">The operation type.</param>
        /// <param name="sourcePosition">The position of the associated instruction in the source code.</param>
        /// <param name="parameter1">The first additional parameter.</param>
        /// <param name="parameter2">The second additional parameter.</param>
        /// <param name="parameter3">The third additional parameter.</param>
        /// <param name="parameter4">The forth additional parameter.</param>
        public FourParametrizedOperation(OperationType type, long sourcePosition, T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4)
            : base(type, sourcePosition)
        {
            if (parameter1 == null) throw new ArgumentNullException("parameter1");
            if (parameter2 == null) throw new ArgumentNullException("parameter2");
            if (parameter3 == null) throw new ArgumentNullException("parameter3");
            if (parameter4 == null) throw new ArgumentNullException("parameter4");

            this.Parameter1 = parameter1;
            this.Parameter2 = parameter2;
            this.Parameter3 = parameter3;
            this.Parameter4 = parameter4;
        }

        /// <summary>
        /// Gets the first additional operation parameter.
        /// </summary>
        public T1 Parameter1 { get; private set; }

        /// <summary>
        /// Gets the second additional operation parameter.
        /// </summary>
        public T2 Parameter2 { get; private set; }

        /// <summary>
        /// Gets the third additional operation parameter.
        /// </summary>
        public T3 Parameter3 { get; private set; }

        /// <summary>
        /// Gets the forth additional operation parameter.
        /// </summary>
        public T4 Parameter4 { get; private set; }

        /// <summary>
        /// Gets meta data associated to this operation (for instance, parameters).
        /// </summary>
        /// <remarks>This method returns a collection consisting of this.Parameter1, this.Parameter2, this.Parameter3 and this.Parameter4.</remarks>
        public override IEnumerable<object> GetMetaData()
        {
            yield return this.Parameter1;
            yield return this.Parameter2;
            yield return this.Parameter3;
            yield return this.Parameter4;
        }

        public override string ToString()
        {
            return base.ToString() + " (" + this.Parameter1 + ", " + this.Parameter2 + ", " + this.Parameter3 + ", " + this.Parameter4 + ")";
        }

    }
}