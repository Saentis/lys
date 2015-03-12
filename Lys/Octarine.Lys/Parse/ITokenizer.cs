/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys.Parse
{
    /// <summary>
    /// Interface for reading script language tokens from a source.
    /// </summary>
    public interface ITokenizer
    {
        /// <summary>
        /// Reads the next token from the source,
        /// or returns null if the end of the source has been reached.
        /// </summary>
        Token Read();

        /// <summary>
        /// Pushes back a token.
        /// </summary>
        void PushBack(Token token);
    }
}