/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys.Parse
{
    /// <summary>
    /// Class for a general token.
    /// A token is a unit of information in a scripting language
    /// (like, a single operator, a number or a string).
    /// </summary>
    public class Token
    {
        /// <summary>
        /// Initializes a new token.
        /// </summary>
        /// <param name="type">The token type.</param>
        /// <param name="position">The position of the token in the source</param>
        public Token(TokenType type, long position)
        {
            this.Type = type;
            this.Position = position;
        }

        /// <summary>
        /// Gets the token type.
        /// </summary>
        public TokenType Type { get; private set; }

        /// <summary>
        /// Gets the position of the token in the source.
        /// </summary>
        public long Position { get; private set; }

        public override string ToString()
        {
            return Type.ToString();
        }

    }
}