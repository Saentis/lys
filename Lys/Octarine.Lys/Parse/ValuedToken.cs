/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys.Parse
{
    /// <summary>
    /// Class for a token with an associated value.
    /// </summary>
    public class ValuedToken<T> : Token
    {
        public ValuedToken(TokenType type, long position, T value)
            : base(type, position)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets the token value.
        /// </summary>
        public T Value { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Type, Value.ToString());
        }

    }
}