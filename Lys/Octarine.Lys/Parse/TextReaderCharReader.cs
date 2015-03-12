/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace Octarine.Lys.Parse
{
    /// <summary>
    /// Class for reading characters from a text reader.
    /// </summary>
    public class TextReaderCharReader : ICharReader
    {
        /// <summary>
        /// Initializes a new stream character reader.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <exception cref="System.ArgumentNullException">Throws an ArgumentNullException if any parameter is null.</exception>
        public TextReaderCharReader(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            _reader = reader;
        }

        private TextReader _reader;
        private char[] _buffer = new char[1024];
        private int _bufferPosition = 0;
        private int _bufferSize = 0;
        private Stack<char> _pushedBack = new Stack<char>();
        private long _globalPointer = 0;

        /// <summary>
        /// Gets the zero-based position of the reader in the source.
        /// </summary>
        public long Position
        {
            get { return _globalPointer - 1; }
        }

        /// <summary>
        /// Reads a character (uint16) from the underlying source,
        /// or returns -1, if the end has been reached.
        /// </summary>
        public int Read()
        {
            // First read from stack of pushed back characters
            if (_pushedBack.Count > 0)
            {
                _globalPointer++;
                return _pushedBack.Pop();
            }
            
            // Fill buffer if we need more data
            if (_bufferPosition >= _bufferSize)
            {
                if (_reader.Peek() < 0) return -1;
                _bufferSize = _reader.ReadBlock(_buffer, 0, _buffer.Length);
                _bufferPosition = 0;

                // Check if we reached the end
                if (_bufferSize == 0) return -1;
            }

            // Return character
            _globalPointer++;
            return _buffer[_bufferPosition++];
        }

        /// <summary>
        /// Pushes a character back to the source and moves the pointer back to that character,
        /// so that it is returned on the next call of Read().
        /// </summary>
        /// <param name="character">The character to be pushed back. Must be </param>
        /// <exception cref="System.ArgumentOutOfRangeException">Throws an ArgumentOutOfRangeException if the value of character is not in the range of a System.Char.</exception>
        /// <exception cref="System.InvalidOperationException">Throws an InvalidOperationException if the pointer is at the beginning of the string.</exception>
        public void PushBack(int character)
        {
            if (character < char.MinValue || character > char.MaxValue)
                throw new ArgumentOutOfRangeException("character");

            // Push it to the stack, as we cannot write back to the stream
            _globalPointer--;
            _pushedBack.Push((char)character);
        }

    }
}
