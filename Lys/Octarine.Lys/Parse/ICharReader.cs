/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

namespace Octarine.Lys.Parse
{
    /// <summary>
    /// Interface for reading characters from a source.
    /// </summary>
    public interface ICharReader
    {
        /// <summary>
        /// Gets the zero-based position of the reader in the source.
        /// </summary>
        long Position { get; }

        /// <summary>
        /// Reads a character from the underlying source and returns its Unicode value,
        /// or returns -1 if the end has been reached.
        /// </summary>
        int Read();

        /// <summary>
        /// Pushes a character back to the source and moves the pointer back to that character,
        /// so that it is returned on the next call of Read().
        /// </summary>
        /// <param name="character">The character to be pushed back. Must not be negative.</param>
        void PushBack(int character);
    }
}
