/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Text;

namespace Octarine.Lys.Parse
{
    /// <summary>
    /// Class for reading characters from a string.
    /// </summary>
    public class StringCharReader : ICharReader
    {
        /// <summary>
        /// Initializes a new string character reader.
        /// </summary>
        /// <param name="str">The string to read from.</param>
        /// <exception cref="System.ArgumentNullException">Throws an ArgumentNullException if any parameter is null.</exception>
        public StringCharReader(string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            _str = str;
        }

        private string _str;
        private int _pointer = 0;

        /// <summary>
        /// Overwrites the internal string and resets the pointer.
        /// </summary>
        /// <param name="str"></param>
        protected void SetString(string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            _str = str;
            _pointer = 0;
        }

        /// <summary>
        /// Gets the zero-based position of the reader in the source.
        /// </summary>
        public long Position
        {
            get { return _pointer - 1; }
        }

        /// <summary>
        /// Reads a character (uint16) from the underlying source,
        /// or returns -1, if the end has been reached.
        /// </summary>
        public int Read()
        {
            if (_pointer >= _str.Length)
                return -1;
            else
                return _str[_pointer++];
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
            if (_pointer <= 0)
                throw new InvalidOperationException("Cannot push back a character before the beginning of the string.");

            // Move to previous character
            _pointer--;

            // Build new string
            StringBuilder sb = new StringBuilder();
            sb.Append(_str, 0, _pointer);
            sb.Append((char)character);
            sb.Append(_str, _pointer + 1, _str.Length - _pointer - 1);
            _str = sb.ToString();
        }
    }
}
